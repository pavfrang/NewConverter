using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;

using System.ComponentModel;


using System.Drawing.Design;

using System.Windows.Forms;
using System.Windows.Forms.Design;
using Paulus.IO;
using Paulus.Common;

namespace ConvertMerge
{
    public enum RecorderFileType
    {
        Unknown,
        ScanMaster,
        INCA,
        PCAN,
        PPS,
        CANalyzer,
        SSTool,
        Thermostar,
        PUMA,
        PUMA2, //format of cell #3 (has separate $iStartTime block)
        MSS,
        MSS2, //newer format (18/05/2015) (resembles to APCRecorder format)
        LAT,
        APC,
        Panel3,
        Fluke, // (20/04/2016),
        AVLCPC, //APC compatible (07-Oct-2019)
        TSICPC6776, // (07-Oct-2019)
        //Concerto, // (07-Oct-2019) (SAME AS AVLPems (keep AVLPems)
        Labview, // (07-Oct-2019)
        EEPS, //(07-Oct-2019)
        NewConverter, //(07-Oct-2019)
        FTIR, //22-Oct-2019
        TSICPC2, //04-Dec-2019
        TSICPC3790, //04-Dec-2019
        Horiba, //04-Dec-2019
        AVLPems, //05-Feb-2020
        TSICPC3750, //13-Feb-2020
        AVLIndicom, //28-Feb-2020
        Ntk, //17-Jul-2020
        Sems, //01-Sep-2020
        CANalyzerV2, //"canalyzer2"  14-Oct-2020
        HoribaPems2, //07-Oct-2021
        AVLPems2, //07-Oct-2021
    }

    public enum ReadLineBehavior
    {
        Success,
        Ignore,
        Break
    }

    //ScanMasterRecorder requires the start date to be set before exporting
    public abstract class Recorder : VariableCollection
    {

        public Recorder() : base() { }

        public Recorder(string sourceFilePath)
            : this()
        {
            SourceFilePath = sourceFilePath;
        }

        [Browsable(false)]
        public Experiment Experiment { get; set; }

        #region Main (extended) properties required for the read operations
        protected XmlElement _xmlRecord;
        [Browsable(false)]
        public XmlElement XMLRecord
        {
            get { return _xmlRecord; }
            set
            {
                _xmlRecord = value;

                //if set then if an error occurs, this time step will be considered
                ForcedTimeStep = _xmlRecord.GetAttributeOrElementDouble("sourcetimestep");

                //if set to true then the forced time step will be set independently of whether there is an error or not    
                ForceTimeStep = _xmlRecord.GetAttributeOrElementBool("forcetimestep") ?? false;
            }
        }

        [Browsable(false)]
        public DateTime? MeasurementDate { get; set; }

        //this must be set EITHER externally (e.g. reading a settings file or reading another file) OR from inside the file
        [Browsable(true)]
        [DisplayName("Start time")]
        //[Editor(typeof(TimePickerEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(DateWithTimeTypeConverter))]
        public DateTime StartAbsoluteTime { get; set; }


        [Browsable(true)]
        public string ContinueOtherFile { get; set; } = null;
        protected double timeOffset; //is used when a "continue other thermostartfile" is used!


        internal class DateWithTimeTypeConverter : DateTimeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (destinationType == typeof(string))
                    return true;

                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                    return string.Format("{0:yyyy-MM-dd HH:mm:ss}", value);
                return base.ConvertTo(value, destinationType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    return DateTime.ParseExact((string)value,
                        new string[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.f" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }
        }

        internal class TimePickerEditor : UITypeEditor
        {

            IWindowsFormsEditorService editorService;
            DateTimePicker picker = new DateTimePicker();
            string time;

            public TimePickerEditor()
            {

                picker.Format = DateTimePickerFormat.Custom;
                picker.CustomFormat = "HH:mm:ss";
                picker.ShowUpDown = true;

            }

            public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
            {

                if (provider != null)
                {
                    this.editorService = provider.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
                }

                if (this.editorService != null)
                {
                    if (value == null)
                    {
                        time = DateTime.Now.ToString("HH:mm:ss");
                    }

                    this.editorService.DropDownControl(picker);

                    value = picker.Value.ToString("HH:mm:ss");
                }

                return value;

            }

            public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.DropDown;
            }

        }

        protected string _sourceTimeUnit;
        [Category("Time")]
        public string SourceTimeUnit { get { return _sourceTimeUnit; } }

        protected string _sourceFilePath;
        [Category("Files")]
        [EditorAttribute(typeof(System.Windows.Forms.Design.FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string SourceFilePath
        {
            get { return _sourceFilePath; }
            set
            {
                //we check only for invalid characters
                char[] invalidChars = Path.GetInvalidPathChars();
                if (!PathExtensions.IsPathValid(value)) throw new ArgumentException("Source file path contains invalid characters.", "SourceFilePath");

                //string parentDirectoryPath = Path.GetDirectoryName(value);
                //if (!Directory.Exists(parentDirectoryPath)) throw new ArgumentException(string.Format("The {0} directory does not exist.", parentDirectoryPath), "SourceFilePath");

                _sourceFilePath = value;
            }
        }

        protected Experiment _parent;
        [Browsable(false)]
        public Experiment Parent { get { return _parent; } }

        #endregion

        #region Abstract members
        //needed for recorder files that need extra input to export
        protected internal abstract void ReadStartingTime();

        //temp and should be probably removed
        public DateTime PeekStartingTime()
        {
            _separator = readSeparator();

            ReadStartingTime();
            return StartAbsoluteTime;
        }

        protected abstract void ReadVariableInfos();

        protected abstract int linesToOmitBeforeData { get; }

        protected char _separator;
        [Browsable(false)]
        public char Separator { get { return _separator; } }

        /// <summary>
        /// Postfix is a string which is appended to all variable names.
        /// </summary>
        public string Postfix { get; set; }
        public string Prefix { get; set; }

        protected abstract char readSeparator();

        protected Dictionary<string, XmlVariable> xmlVariables;



        public void LoadDataFromSource(Dictionary<string, XmlVariable> xmlVariables)
        {
            this.xmlVariables = xmlVariables;

            _separator = readSeparator();

            ReadVariableInfos();

            setVariableParameters();

            ReadStartingTime();

            loadDataFromSource();

            forceSourceTimeStepOnError();
        }

        private void setVariableParameters()
        {
            InterpolationMode defaultInterpolation = ExperimentManager.DefaultInterpolationMode;

            foreach (VariableInfo v in _variables)
                if (xmlVariables != null && xmlVariables.ContainsKey(v.Name))
                {
                    v.TranslatedName = xmlVariables[v.Name].TranslatedName;
                    v.ColumnInTargetFile = xmlVariables[v.Name].ColumnIndex;

                    v.InterpolationMode = xmlVariables[v.Name].Interpolation;
                }
                else if (v is VariableInfo<string> &&
                    defaultInterpolation != InterpolationMode.Next && defaultInterpolation != InterpolationMode.Previous)
                    v.InterpolationMode = InterpolationMode.Nearest;
                else if (v is VariableInfo<DateTime> &&
                    defaultInterpolation != InterpolationMode.Next && defaultInterpolation != InterpolationMode.Previous)
                    v.InterpolationMode = InterpolationMode.Nearest;
                else if (v.InterpolationMode == InterpolationMode.Undefined)
                    v.InterpolationMode = defaultInterpolation;
        }

        /// <summary>
        /// 
        /// </summary>
        private void forceSourceTimeStepOnError()
        {
            foreach (VariableInfo v in _variables)
                v.ForceSourceTimeStepOnError(ForcedTimeStep.HasValue ? ForcedTimeStep.Value : 1.0);
        }

        //stores the current relative time for each variable in seconds
        //the time is updated each time a new line is read from the source file
        //it is mainly used for debugging reasons, to check if the time is monotonically 
        protected Dictionary<VariableInfo, double> currentRelativeTimes;

        //called at the beginning by loadDataFromSource
        protected virtual void initializeCurrentRelativeTimes()
        {
            currentRelativeTimes = new Dictionary<VariableInfo, double>();
            foreach (VariableInfo v in _variables)
                currentRelativeTimes.Add(v, -1.0);
        }

        //the function should return TRUE if the current line should be omitted based on "cropstarttime"
        //if the function returns FALSE
        protected virtual bool omitLinesBasedOnCropStartTime(string[] tokens, ref int iLine)
        {
            return false;
        }


        private void loadDataFromSource()
        {
            initializeCurrentRelativeTimes();

            using (StreamReader reader = new StreamReader(_sourceFilePath, Encoding.Default))
            {
                reader.OmitLines(linesToOmitBeforeData);

                int iLine = 0;
                string line; string[] tokens=null;

                while (!reader.EndOfStream)
                {
                    iLine++;

                    line = preProcessLineBeforeSplit(reader.ReadLine());
                    tokens = line.Split(new char[] { _separator }, StringSplitOptions.None);

                    if (!omitLinesBasedOnCropStartTime(tokens, ref iLine)) break;
                }


                while (!reader.EndOfStream)
                {
                    if (!loadDataFromLine(tokens, ref iLine)) break;

                    iLine++;
                    line = preProcessLineBeforeSplit(reader.ReadLine());
                    tokens = line.Split(new char[] { _separator }, StringSplitOptions.None);

                }
            }
        }

        //e.g. replace ';;' with ';' before splitting to tokens
        protected virtual string preProcessLineBeforeSplit(string rawLine) { return rawLine; }

        protected virtual bool loadDataFromLineBeforeTimeOffset(string[] tokens, ref int iLine)
        {
   /////used in inca
            //for (int itoken = 0; itoken < tokens.Length; itoken++) tokens[itoken] = tokens[itoken].Replace(',', '.');

            foreach (VariableInfo v in _variables)
            {
                //DateTime absoluteTime;
                //ASSUME SECONDS

                if (v.TimeColumn > tokens.Length - 1 || v.ColumnInSourceFile > tokens.Length - 1) return false;

                double relativeTimeInSeconds;
                bool parsed = double.TryParse(tokens[v.TimeColumn], out relativeTimeInSeconds);

                if (_sourceTimeUnit == "ms") relativeTimeInSeconds *= 0.001;

                //absoluteTime = StartAbsoluteTime.AddSeconds(relativeTimeInSeconds);

                if (parsed)
                {
                    if (v.GetType() == typeof(VariableInfo<double>))
                    {
                        double value;
                        parsed = double.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);
                        if (parsed) //only if the value can be parsed it is valid to add the time/value pair
                        {
                            //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                            //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                            (v as VariableInfo<double>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                            (v as VariableInfo<double>).ValuesBeforeTimeStepChange.Add(value);
                        }
                        //else //just add zero (26-Jun-2018)
                        //{
                        //    (v as VariableInfo<double>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                        //    (v as VariableInfo<double>).ValuesBeforeTimeStepChange.Add(0.0);
                        //}

                    }
                    else if (v.GetType() == typeof(VariableInfo<string>))
                    {
                        //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                        //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                        (v as VariableInfo<string>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                        (v as VariableInfo<string>).ValuesBeforeTimeStepChange.Add(tokens[v.ColumnInSourceFile]);
                    }
                }
            }

            return true;
        }

        protected virtual bool loadDataFromLine(string[] tokens, ref int iLine)
        {
            bool added = loadDataFromLineBeforeTimeOffset(tokens, ref iLine);
            if (timeOffset == 0.0 || !added) return added;

            foreach (var v in Variables)
            {
                if (v.GetType() == typeof(VariableInfo<double>))
                {
                    var tl = (v as VariableInfo<double>).RelativeTimesInSecondsBeforeTimeStepChange;
                    tl[tl.Count - 1] += timeOffset;
                }
                else if (v.GetType() == typeof(VariableInfo<string>))
                {
                    var tl = (v as VariableInfo<string>).RelativeTimesInSecondsBeforeTimeStepChange;
                    tl[tl.Count - 1] += timeOffset;
                }
                else if (v.GetType() ==typeof(VariableInfo<int>))
                {
                    var tl = (v as VariableInfo<int>).RelativeTimesInSecondsBeforeTimeStepChange;
                    tl[tl.Count - 1] += timeOffset;
                }

            }
            return added;
        }

        #endregion

        ////used when the time is given in relative units
        ////e.g. not used in ScanMasterRecorder
        //public string SourceTimeUnit;

        protected static CultureInfo EN = CultureInfo.InvariantCulture;
        protected static CultureInfo GR = CultureInfo.GetCultureInfo("el");

        public static string RecorderFileTypeToString(RecorderFileType fileType)
        {
            return fileType.EnumValueExists() ? fileType.ToString() : "Unknown";
        }

        public static string RecorderFileTypeToString(string filePath)
        {
            RecorderFileType fileType = GetRecorderFileType(filePath);
            return RecorderFileTypeToString(fileType);
        }

        public static RecorderFileType GetRecorderFileType(string filePath)
        {
            if (ScanMasterRecorder.IsScanMasterRecorder(filePath))
                return RecorderFileType.ScanMaster;
            else if (LATRecorder.IsLATRecorder(filePath))
                return RecorderFileType.LAT;
            else if (INCARecorder.IsIncaRecorder(filePath))
                return RecorderFileType.INCA;
            else if (PCANRecorder.IsPCANRecorder(filePath))
                return RecorderFileType.PCAN;
            else if (PPSRecorder.IsPPSRecorder(filePath))
                return RecorderFileType.PPS;
            else if (SSToolRecorder.IsSSToolRecorder(filePath))
                return RecorderFileType.SSTool;
            else if (CANRecorder.IsCANRecorder(filePath))
                return RecorderFileType.CANalyzer;
            else if (ThermostarRecorder.IsThermostarRecorder(filePath))
                return RecorderFileType.Thermostar;
            else if (PUMA2Recorder.IsPUMA2Recorder(filePath))
                return RecorderFileType.PUMA2;
            else if (PUMARecorder.IsPUMARecorder(filePath))
                return RecorderFileType.PUMA;
            else if (MSSRecorder.IsMSSRecorder(filePath))
                return RecorderFileType.MSS;
            else if (APCRecorder.IsAPCRecorder(filePath))
                return RecorderFileType.APC;
            else if (MSS2Recorder.IsMSS2Recorder(filePath))
                return RecorderFileType.MSS2;
            else if (Panel3Recorder.IsPanel3Recorder(filePath))
                return RecorderFileType.Panel3;
            else if (FlukeRecorder.IsFlukeRecorder(filePath))
                return RecorderFileType.Fluke;
            else if (AVLCPCRecorder.IsAVLCPCRecorder(filePath))
                return RecorderFileType.AVLCPC;
            else if (TSICPC3776Recorder.IsTSICPC3776Recorder(filePath))
                return RecorderFileType.TSICPC6776;
            //else if (ConcertoRecorder.IsConcertoRecorder(filePath))
            //    return RecorderFileType.Concerto;
            else if (LabviewRecorder.IsLabviewRecorder(filePath))
                return RecorderFileType.Labview;
            else if (EEPSRecorder.IsEEPSRecorder(filePath))
                return RecorderFileType.EEPS;
            else if (NewConverterRecorder.IsNewConverterRecorder(filePath))
                return RecorderFileType.NewConverter;
            else if (FTIRRecorder.IsFTIRRecorder(filePath))
                return RecorderFileType.FTIR;
            else if (TSICPC2Recorder.IsTSICPC2Recorder(filePath))
                return RecorderFileType.TSICPC2;
            else if (TSICPC3790Recorder.IsTSICPC3790Recorder(filePath))
                return RecorderFileType.TSICPC3790;
            else if (HoribaRecorder.IsHoribaRecorder(filePath))
                return RecorderFileType.Horiba;
            else if (AVLPemsRecorder.IsAVLPemsRecorder(filePath))
                return RecorderFileType.AVLPems;
            else if (TSICPC3750Recorder.IsTSICPC3750Recorder(filePath))
                return RecorderFileType.TSICPC3750;
            else if (AVLIndicomRecorder.IsAVLIndicomRecorder(filePath)) //avl_indicom
                return RecorderFileType.AVLIndicom;
            else if (NtkRecorder.IsNtkRecorder(filePath)) //ntk (17/07/2020)
                return RecorderFileType.Ntk;
            else if (SemsRecorder.IsSemsRecorder(filePath))
                return RecorderFileType.Sems;
            else if (CANRecorderV2.IsCANRecorderV2(filePath))
                return RecorderFileType.CANalyzerV2;
            else if (HoribaPems2Recorder.IsHoribaPems2Recorder(filePath))
                return RecorderFileType.HoribaPems2;
            else if (AVLPems2Recorder.IsAVLPems2Recorder(filePath))
                return RecorderFileType.AVLPems2;
            else
                return RecorderFileType.Unknown;
        }

        public static Recorder Create(string recorderPath)
        {
            switch (GetRecorderFileType(recorderPath))
            {
                case RecorderFileType.ScanMaster:
                    return new ScanMasterRecorder(recorderPath);
                case RecorderFileType.INCA:
                    return new INCARecorder(recorderPath);
                case RecorderFileType.PCAN:
                    return new PCANRecorder(recorderPath);
                case RecorderFileType.PPS:
                    return new PPSRecorder(recorderPath);
                case RecorderFileType.SSTool:
                    return new SSToolRecorder(recorderPath);
                case RecorderFileType.CANalyzer:
                    return new CANRecorder(recorderPath);
                case RecorderFileType.Thermostar:
                    return new ThermostarRecorder(recorderPath);
                case RecorderFileType.PUMA2:
                    return new PUMA2Recorder(recorderPath);
                case RecorderFileType.PUMA:
                    return new PUMARecorder(recorderPath);
                case RecorderFileType.MSS:
                    return new MSSRecorder(recorderPath);
                case RecorderFileType.LAT:
                    return new LATRecorder(recorderPath);
                case RecorderFileType.APC:
                    return new APCRecorder(recorderPath);
                case RecorderFileType.MSS2:
                    return new MSS2Recorder(recorderPath);
                case RecorderFileType.Panel3:
                    return new Panel3Recorder(recorderPath);
                case RecorderFileType.Fluke:
                    return new FlukeRecorder(recorderPath);
                case RecorderFileType.AVLCPC:
                    return new AVLCPCRecorder(recorderPath);
                case RecorderFileType.TSICPC6776:
                    return new TSICPC3776Recorder(recorderPath);
                //case RecorderFileType.Concerto:
                //    return new ConcertoRecorder(recorderPath);
                case RecorderFileType.Labview:
                    return new LabviewRecorder(recorderPath);
                case RecorderFileType.EEPS:
                    return new EEPSRecorder(recorderPath);
                case RecorderFileType.NewConverter:
                    return new NewConverterRecorder(recorderPath);
                case RecorderFileType.FTIR:
                    return new FTIRRecorder(recorderPath);
                case RecorderFileType.TSICPC2:
                    return new TSICPC2Recorder(recorderPath);
                case RecorderFileType.TSICPC3790:
                    return new TSICPC3790Recorder(recorderPath);
                case RecorderFileType.Horiba:
                    return new HoribaRecorder(recorderPath);
                case RecorderFileType.AVLPems:
                    return new AVLPemsRecorder(recorderPath);
                case RecorderFileType.TSICPC3750:
                    return new TSICPC3750Recorder(recorderPath);
                case RecorderFileType.AVLIndicom:
                    return new AVLIndicomRecorder(recorderPath);
                case RecorderFileType.Ntk:
                    return new NtkRecorder(recorderPath);
                case RecorderFileType.Sems:
                    return new SemsRecorder(recorderPath);
                case RecorderFileType.CANalyzerV2:
                    return new CANRecorderV2(recorderPath);
                case RecorderFileType.HoribaPems2:
                    return new HoribaPems2Recorder(recorderPath);
                case RecorderFileType.AVLPems2:
                    return new AVLPems2Recorder(recorderPath);
                default:
                    return null;
            }
        }

        public static Recorder CreateFromXML(XmlElement xmlRecorder, Experiment exp, ExperimentManager em)
        {
            //source is obligatory
            string source = xmlRecorder.GetAttributeOrElementText("source");
            if (source == null) return null; //SHOULD LOG THE ERROR
            if (!PathExtensions.IsPathAbsolute(source)) source = Path.GetFullPath(Path.Combine(exp.SourceDirectory, source));

            //this returns the type
            Recorder r = Recorder.Create(source); //SHOULD LOG THE ERROR
            if (r == null)
                throw new InvalidOperationException($"File '{source}' is not in recognizable format.");
            r._parent = exp;

            //@07-Oct-2019 index allows files of the same type to be added
            //postfix for all variable names
            r.Postfix = xmlRecorder.GetAttributeOrElementText("postfix", "");
            //22/10/2018
            r.Prefix = xmlRecorder.GetAttributeOrElementText("prefix", "");

            //type SHOULD BE VALIDATED BEFORE USE (if set)
            string type = xmlRecorder.GetAttributeOrElementText("type");

            //target is not required if the export is set to false
            string target = xmlRecorder.GetAttributeOrElementText("target");
            if (target == null)
            {
                //this needs to be re-retrieved in case the absolute source path is given
                string sourceDirectory = Path.GetDirectoryName(source);
                string sourceFilenameWithoutExtension = Path.GetFileNameWithoutExtension(source);
                target = sourceFilenameWithoutExtension + "_" + type + ".txt";
            }
            r.MergeFilePath = PathExtensions.IsPathAbsolute(target) ? target : Path.GetFullPath(Path.Combine(exp.SourceDirectory, target));

            double? forcedTimeStep = xmlRecorder.GetAttributeOrElementDouble("sourcetimestep");
            if (forcedTimeStep.HasValue) r.ForcedTimeStep = forcedTimeStep;

            #region Inherited attributes from recorder_defaults (export, mergemode)
            bool? export = xmlRecorder.GetAttributeOrElementBool("export");
            r.ShouldSaveToFile = export.HasValue ? export.Value : ExperimentManager.DefaultShouldExportRecorder;

            SyncModes mergemode = XmlSettings.getMergeMode(xmlRecorder);
            r.SyncMode = mergemode != SyncModes.InvalidOrMissing ? mergemode : ExperimentManager.DefaultRecorderMergeMode;
            #endregion

            #region Inherited from experiment

            r.ExportTimeStep = exp.ExportTimeStep;
            if (exp.MeasurementDate.HasValue) r.MeasurementDate = exp.MeasurementDate;

            #endregion

            r.Experiment = exp;
            r.XMLRecord = xmlRecorder;

            return r;
        }

        public bool ReadStartTimeFromXmlRecord()
        {
            if (_xmlRecord?.HasAttribute("starttime") ?? false)
            {
                DateTime? startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
                if (startTime.HasValue)
                    StartAbsoluteTime = startTime.Value;
                return true;
            }
            return false;
        }

        public void CheckContinueFileRecord<T>() where T:Recorder,new()
        {
            //CORRECT THE STARTABSOLUTE TIME IF A CONTINUE FILE IS USED
            if (_xmlRecord?.HasAttribute("continueFile") ?? false)
            {
                string localPath = _xmlRecord.GetAttributeOrElementText("continueFile");
                ContinueOtherFile = localPath;
                string absolutePath = Path.Combine(Path.GetDirectoryName(this._sourceFilePath), localPath);
                T firstRecorder = new T();
                firstRecorder.SourceFilePath = absolutePath;

                firstRecorder.ReadStartingTime();
                DateTime oldStartAbsoluteTime = firstRecorder.StartAbsoluteTime;
                timeOffset = (StartAbsoluteTime.SetDate(oldStartAbsoluteTime) - oldStartAbsoluteTime).TotalSeconds;
                StartAbsoluteTime = oldStartAbsoluteTime;
            }
        }
    }
}
