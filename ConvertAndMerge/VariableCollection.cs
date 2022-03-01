using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;

using System.ComponentModel;
using System.Windows.Forms;
using Paulus.IO;
using Paulus.Excel;

namespace ConvertMerge
{
    public enum SyncModes
    {
        InvalidOrMissing,
        KeepEarliestStartTime = 1,
        KeepLatestStartTime = 2,
        KeepEarliestEndTime = 4,
        KeepLatestEndTime = 8,

        KeepAll = KeepEarliestStartTime | KeepLatestEndTime,
        Crop = KeepLatestStartTime | KeepEarliestEndTime,
        KeepAllStartCropEnd = KeepEarliestStartTime | KeepEarliestEndTime,
        CropStartKeepAllEnd = KeepLatestStartTime | KeepLatestEndTime
    }

    public class VariableCollection : IEnumerable<VariableInfo>
    {
        #region Constructors
        public VariableCollection() { _variables = new List<VariableInfo>(); AutoCreateDirectories = true; }

        public VariableCollection(IEnumerable<VariableInfo> variables, double mergeTimeStepInSeconds, SyncModes mergeMode, string mergeFilePath)
            : this()
        {
            _variables.AddRange(variables);

            ExportTimeStep = mergeTimeStepInSeconds;
            SyncMode = mergeMode;
            MergeFilePath = mergeFilePath;
        }

        public VariableCollection(IEnumerable<IEnumerable<VariableInfo>> variableLists, double mergeTimeStepInSeconds, SyncModes mergeMode, string mergeFilePath)
            : this()
        {
            //_variables.AddRangeLists(variableLists);
            foreach (IEnumerable<VariableInfo> variableList in variableLists) _variables.AddRange(variableList);

            ExportTimeStep = mergeTimeStepInSeconds;
            SyncMode = mergeMode;
            MergeFilePath = mergeFilePath;
        }

        #endregion

        #region Main properties required for the merge operations
        public double? ForcedTimeStep { get; set; }

        public bool ForceTimeStep { get; set; }

        protected List<VariableInfo> _variables;
        public List<VariableInfo> Variables { get { return _variables; } }

        protected double _exportTimeStep;
        /// <summary>
        /// The export time step in seconds.
        /// </summary>
        public double ExportTimeStep
        {
            get { return _exportTimeStep; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("ExportTimeStep", "The time step must be larger than 0.");
                _exportTimeStep = value;
            }
        }

        protected SyncModes _syncMode;

        public SyncModes SyncMode
        {
            get { return _syncMode; }
            set
            {
                bool isValidValue =
                    (value.HasFlag(SyncModes.KeepEarliestStartTime) ^ value.HasFlag(SyncModes.KeepLatestStartTime)) &&
                    (value.HasFlag(SyncModes.KeepEarliestEndTime) ^ value.HasFlag(SyncModes.KeepLatestEndTime));

                if (!isValidValue) throw new ArgumentOutOfRangeException("SyncMode", "The value is out of range. Use a valid combination of MergeModes enum flags.");

                _syncMode = value;
            }
        }


        public bool AutoCreateDirectories { get; set; }

        protected string _mergeFilePath;
        [Category("Files")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string MergeFilePath
        {
            get { return _mergeFilePath; }
            set
            {
                char[] invalidChars = Path.GetInvalidPathChars();
                if (!PathExtensions.IsPathValid(value)) throw new ArgumentException("Merge file path is not valid.", "MergeFilePath");

                string parentDirectoryPath = Path.GetDirectoryName(value);
                if (!Directory.Exists(parentDirectoryPath))
                {
                    if (AutoCreateDirectories)
                        try
                        {
                            Directory.CreateDirectory(parentDirectoryPath);
                        }
                        catch
                        {
                            throw;
                        }
                    else
                        throw new ArgumentException(string.Format("The {0} directory does not exist.", parentDirectoryPath), "MergeFilePath");
                }

                _mergeFilePath = value;
            }
        }

        [Category("Files")]
        public string XlsFilePath
        {
            get
            {
                return Path.ChangeExtension(_mergeFilePath, "xlsx");
            }
        }

        //only THIS, is optional
        public bool ShouldSaveToFile { get; set; }

        public bool ShouldSaveToPumaFile { get; set; }
        public bool ShouldReserveColumns { get; set; }
        #endregion



        #region Merge functions


        public virtual void Merge()
        {
            CheckIfRequiredValuesAreSet();

            SetTimeStep(_exportTimeStep);

            UpdateTimeInfoAfterTimeStepChange();

            //exportToFiles(); (good for debugging)

            CalculateStartEndVariableIndices();

            ResetVariableIndices();

            if (ShouldSaveToPumaFile)
                WriteToPumaFile();
            else if (ShouldSaveToFile)
                WriteToFile();
        }


        /// <summary>
        /// Sets the order of the variables to be exported at the text file. Must be called prior to Merge.
        /// </summary>
        protected void ReorderVariables(Dictionary<string, XmlVariable> xmlVariables)
        {
            //get max columnindex for the registered variables (i.e. declared in the variables xml file)
            int maxColumnIndex = xmlVariables.Max(v => v.Value.ColumnIndex); // _variables.Max(v => v.ColumnInTargetFile);
            if (maxColumnIndex == 0) maxColumnIndex = 2; //the first two columns are reserved for time and absolute time

            //fill variable column target for the missing ones
            foreach (VariableInfo v in _variables)
                if (v.ColumnInTargetFile == 0)
                    v.ColumnInTargetFile = ++maxColumnIndex;

            //θα μπορούσα να βάλω IComparable σε κάθε μεταβλητή αλλά δεν είναι και πολύ intuitive (θα το τσεκάρω αυτό)
            _variables.Sort((v1, v2) => (v1.ColumnInTargetFile - v2.ColumnInTargetFile));
        }

        private void CheckIfRequiredValuesAreSet()
        {
            //check if the required variables are set
            if (_variables.Count == 0) throw new InvalidOperationException("The variables list has not been set.");
            if (_exportTimeStep == 0) throw new ArgumentOutOfRangeException("MergeTimeStepInSeconds", "The time step has not been set.");
            if (_syncMode == SyncModes.InvalidOrMissing) throw new ArgumentOutOfRangeException("MergeMode", "Use a valid combination of MergeModes enum flags.");
            if (string.IsNullOrWhiteSpace(_mergeFilePath)) throw new ArgumentException("Merge file path has not been set.", "MergeFilePath");
        }

        private void SetTimeStep(double timeStep)
        {
            foreach (VariableInfo v in _variables)
                v.setExportTimeStep(timeStep);
        }

        #region Calculating time properties after changing the time step
        //updated by updateTimeInfoAfterTimeStepChange()
        public DateTime EarliestStartAbsoluteTime, LatestStartAbsoluteTime, EarliestEndAbsoluteTime, LatestEndAbsoluteTime;
        public double EarliestStartRelativeTime, LatestStartRelativeTime, EarliestEndRelativeTime, LatestEndRelativeTime;

        private void UpdateTimeInfoAfterTimeStepChange()
        {
            if (ContainsAtLeastOneNonEmptyVariable())
            {
                var variables = _variables.Where(v => v.ContainsData);

                var absoluteTimes = variables.Select(v => v.StartAbsoluteTimeAfterTimeStepChange);
                EarliestStartAbsoluteTime = absoluteTimes.Min(); LatestStartAbsoluteTime = absoluteTimes.Max();

                absoluteTimes = variables.Select(v => v.EndAbsoluteTimeAfterTimeStepChange);
                EarliestEndAbsoluteTime = absoluteTimes.Min(); LatestEndAbsoluteTime = absoluteTimes.Max();

                var relativeTimes = variables.Select(v => v.StartRelativeTimeAfterTimeStepChange);
                EarliestStartRelativeTime = relativeTimes.Min(); LatestStartRelativeTime = relativeTimes.Max();

                relativeTimes = variables.Select(v => v.EndRelativeTimeAfterTimeStepChange);
                EarliestEndRelativeTime = relativeTimes.Min(); LatestEndRelativeTime = relativeTimes.Max();
            }
        }

        public bool ContainsAtLeastOneNonEmptyVariable() => _variables.Where(v => v.ContainsData).Any();


        private DateTime _startSyncAbsoluteTime;
        /// <summary>
        /// Updated by calculateStartEndVariableIndices().
        /// </summary>
        //public DateTime StartSyncAbsoluteTime { get { return _startSyncAbsoluteTime; } }
        public DateTime StartSyncAbsoluteTime { get => _startSyncAbsoluteTime; }

        //updated by calculateStartEndVariableIndices()
        private DateTime _endSyncAbsoluteTime;
        public DateTime EndSyncAbsoluteTime { get { return _endSyncAbsoluteTime; } }

        ////updated by resetVariableIndices(), addVariableValuesToRecordLine()
        private Dictionary<VariableInfo, SingleVariableSynchronizerInfo> variableSynchronizers;

        private void CalculateStartEndVariableIndices()
        {

            //those are needed for the synchronization between recorders
            _startSyncAbsoluteTime = _syncMode.HasFlag(SyncModes.KeepEarliestStartTime) ? EarliestStartAbsoluteTime : LatestStartAbsoluteTime;
            _endSyncAbsoluteTime = _syncMode.HasFlag(SyncModes.KeepEarliestEndTime) ? EarliestEndAbsoluteTime : LatestEndAbsoluteTime;

            variableSynchronizers = new Dictionary<VariableInfo, SingleVariableSynchronizerInfo>();
            foreach (VariableInfo v in _variables)
            {
                SingleVariableSynchronizerInfo sync = new SingleVariableSynchronizerInfo(v);
                variableSynchronizers.Add(v, sync);
                sync.SetSync(_startSyncAbsoluteTime, _endSyncAbsoluteTime);
            }

            //foreach (VariableInfo v in _variables)
            //    v.setMergeProperties(_startMergeAbsoluteTime, _endMergeAbsoluteTime);
        }

        private void ResetVariableIndices()
        {
            foreach (KeyValuePair<VariableInfo, SingleVariableSynchronizerInfo> entry in variableSynchronizers)
                entry.Value.ResetSyncIndex(_syncMode);

            //foreach (VariableInfo v in _variables)
            //    v.resetMergeIndex(_mergeMode);
        }
        #endregion

        #region Writing to file


        #region Write to ASCII file
        private int iLine;

        private void WriteToFile()
        {
            string header = GetVariablesHeader();

            iLine = 0;

            using (StreamWriter writer = new StreamWriter(_mergeFilePath))
            {
                writer.WriteLine(header);

                StringBuilder lineBuilder = new StringBuilder();

                while (true)
                {
                    lineBuilder.Clear();

                    if (!AddTimeToRecordLine(lineBuilder)) break;

                    if (ShouldReserveColumns)
                        AddVariableValuesToRecordLine(lineBuilder);
                    else
                        AddVariableValuesToPumaRecordLine(lineBuilder);

                    writer.WriteLine(lineBuilder.ToString());

                    iLine++;
                }
            }
        }

        private string GetVariablesHeader()
        {

            if (ShouldReserveColumns)
            {
                //the first two columns are always Time and Absolute Time
                StringBuilder lineBuilder = new StringBuilder("Time [s]\tAbsolute Time\t");
                int previousColumn = 2;
                int emptyColumnIndex = 0;
                for (int iV = 0; iV < _variables.Count; iV++)
                {
                    VariableInfo v = _variables[iV];

                    //add the required reserved columns
                    int currentColumn = v.ColumnInTargetFile;
                    for (int c = 0; c < currentColumn - previousColumn - 1; c++)
                        lineBuilder.AppendFormat("Reserved{0}\t", ++emptyColumnIndex);

                    lineBuilder.Append(v);

                    bool isLastVariable = iV == _variables.Count - 1;
                    if (!isLastVariable) lineBuilder.Append('\t');

                    previousColumn = currentColumn;
                }
                return lineBuilder.ToString();
            }
            else
                return "Time [s]\tAbsolute Time\t" + string.Join("\t", Variables.Select(v => v.ToString()));


        }

        private bool AddTimeToRecordLine(StringBuilder lineBuilder)
        {
            bool addedTime = false;
            foreach (VariableInfo v in _variables)
            {
                SingleVariableSynchronizerInfo vSync = variableSynchronizers[v];

                if (!v.ContainsData) continue;

                int index = vSync.SyncIndex;

                if (index >= vSync.StartIndex && index <= vSync.LastIndex)
                {
                    //lineBuilder.AppendFormat(Experiment.DateTimeRecordFormat, v.RelativeTimesInSecondsAfterTimeStepChange[index], v.AbsoluteTimesAfterTimeStepChange[index]);
                    lineBuilder.AppendFormat(ExperimentManager.DateTimeRecordFormat, iLine * _exportTimeStep, v.AbsoluteTimesAfterTimeStepChange[index]);
                    addedTime = true;
                    break;
                }
            }
            return addedTime;
        }

        private void AddVariableValuesToRecordLine(StringBuilder lineBuilder)
        {
            int previousColumn = 2;

            for (int iV = 0; iV < _variables.Count; iV++)
            {
                VariableInfo v = _variables[iV];
                SingleVariableSynchronizerInfo vSync = variableSynchronizers[v];

                int currentColumn = v.ColumnInTargetFile;
                for (int c = 0; c < currentColumn - previousColumn - 1; c++)
                    lineBuilder.Append('\t');

                if (v.ContainsData)
                {
                    int index = vSync.SyncIndex;

                    if (index >= vSync.StartIndex && index <= vSync.LastIndex)
                        lineBuilder.AppendFormat("{0}", v[index]); //format could be customized

                    vSync.IncrementSyncIndex();
                }

                bool isLastVariable = iV == _variables.Count - 1;
                if (!isLastVariable) lineBuilder.Append('\t');

                previousColumn = currentColumn;
            }
        }
        #endregion

        #region Write to PUMA file

        private void WriteToPumaFile()
        {
            //string pumaFilePath = Path.Combine(Path.GetDirectoryName(_mergeFilePath), Path.GetFileNameWithoutExtension(_mergeFilePath) + "_PUMA.txt");
            string pumaFilePath = _mergeFilePath;

            iLine = 0;


            using (StreamWriter writer = new StreamWriter(pumaFilePath))
            {
                //writer.WriteLine(header);

                //variable names
                writer.Write("Time\tAbsolute_Time\t" + string.Join("\t", from v in Variables select v.TranslatedName ?? v.Name) + "\t");
                writer.WriteLine("$iStartTime\t");
                //unit line
                writer.Write("s\t-\t" + string.Join("\t", from v in Variables select v.Unit) + "\t");
                writer.WriteLine("\t"); //for the recording time

                StringBuilder lineBuilder = new StringBuilder();

                bool firstTime = true;
                while (true)
                {
                    lineBuilder.Clear();

                    if (!AddTimeToRecordLine(lineBuilder)) break;

                    AddVariableValuesToPumaRecordLine(lineBuilder); //the first time we should add the start time!
                    writer.Write(lineBuilder.ToString());

                    if (firstTime)
                    {
                        writer.WriteLine($"\t{GetPumaRecordingTime()}\t");
                        firstTime = false;
                    }
                    else
                        writer.WriteLine("\t\t");

                    iLine++;

                }
            }
        }
        protected string GetPumaRecordingTime() => $"{StartSyncAbsoluteTime:yyyyMMddHHmmss}";



        private void AddVariableValuesToPumaRecordLine(StringBuilder lineBuilder)
        {
            for (int iV = 0; iV < _variables.Count; iV++)
            {
                VariableInfo v = _variables[iV];
                SingleVariableSynchronizerInfo vSync = variableSynchronizers[v];

                if (v.ContainsData)
                {
                    int index = vSync.SyncIndex;

                    if (index >= vSync.StartIndex && index <= vSync.LastIndex)
                        lineBuilder.AppendFormat("{0}", v[index]); //format could be customized

                    vSync.IncrementSyncIndex();
                }

                bool isLastVariable = iV == _variables.Count - 1;
                if (!isLastVariable) lineBuilder.Append('\t');
            }
        }


        #endregion


        public virtual void SaveToExcelFile()
        {
            string xlsx = this.XlsFilePath;
            if (File.Exists(xlsx)) File.Delete(xlsx);

            ComDisposer d = new ComDisposer();

            ExcelInfo info;
            Excel.Application excel;
            Excel.Workbook wb;

            if (File.Exists(xlsx))
            {
                try
                {
                    File.Delete(this.XlsFilePath);
                }
                catch
                {
                    MessageBox.Show("Please check if the excel file is already opened and close it.", "NewConverter", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            //if (!File.Exists(xlsx))
            //{
            excel = ExcelExtensions.OpenExcel(out info, ChangeSettingsMode.ChangeSettingsForSpeed, false); d.Enqueue(excel);
            excel.Workbooks.OpenText(_mergeFilePath);
            wb = excel.ActiveWorkbook;
            //}
            //else
            //    excel = ExcelExtensions.OpenExcel(xlsx, out info, out wb, ChangeSettingsMode.ChangeSettingsForSpeed, true);

            d.Enqueue(wb);

            ChangeTimeFormatAndCorrectDegrees(excel, wb);

            wb.SaveAs(xlsx, FileFormat: Excel.XlFileFormat.xlOpenXMLWorkbook);

            try
            {
                wb.Close(false);
                excel.QuitOrRestoreSettings(ref info);
            }
            catch { }
            finally
            {
                //excel.Quit();

                //ERRORS HERE!

                //release COM objects
                d.Dispose();
            }
            //File.Delete(_mergeFilePath);
        }

        private void ChangeTimeFormatAndCorrectDegrees(Excel.Application excel, Excel.Workbook wb)
        {
            //Excel.Application excel = wb.Application;

            ComDisposer d = new ComDisposer();
            Excel.Worksheet sh = wb.Worksheets[1]; d.Enqueue(sh);

            Excel.Range firstTimeCell = sh.Range["B2"]; d.Enqueue(firstTimeCell);
            Excel.Range lastTimeCell = firstTimeCell.End[Excel.XlDirection.xlDown]; d.Enqueue(lastTimeCell);
            Excel.Range timeColumn = sh.Range[firstTimeCell, lastTimeCell]; d.Enqueue(timeColumn);

            timeColumn.NumberFormat = @"[$-F400]h:mm:ss AM/PM";

            //Excel.Range allCells = sh.Rows[1].Cells;  d.Enqueue(allCells);
            //allCells.Replace(What: @"Β°C", Replacement: "°C", LookAt: Excel.XlLookAt.xlPart,
            //    SearchOrder: Excel.XlSearchOrder.xlByRows, MatchCase: false, SearchFormat: false,
            //        ReplaceFormat: false);
            Excel.Range a1 = sh.Range["a1"]; d.Enqueue(a1);
            Excel.Range aLast = a1.End[Excel.XlDirection.xlToRight]; d.Enqueue(aLast);
            Excel.Range a = sh.Range[a1, aLast]; d.Enqueue(a);
            foreach(Excel.Range c in a)
                c.Value = ((string)c.Value).Replace("Β", "").Replace("Â","");

            sh.Name = "synced";

            d.Dispose();
        }

        private Dictionary<VariableInfo, string> variablePaths;
        private void WriteToFiles()
        {
            variablePaths = new Dictionary<VariableInfo, string>();
            string tmp = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "NewConverter"));
            if (!Directory.Exists(tmp)) Directory.CreateDirectory(tmp);

            foreach (VariableInfo v in _variables)
            {
                string vpath = v.ExportToDirectory(tmp);
                variablePaths.Add(v, vpath);
            }
        }
        #endregion

        #endregion

        #region IEnumerable interface
        public IEnumerator<VariableInfo> GetEnumerator()
        {
            return _variables.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<VariableInfo> IEnumerable<VariableInfo>.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }
}
