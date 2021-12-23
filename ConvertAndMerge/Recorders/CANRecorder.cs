using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using Paulus.IO;
using Paulus;
using Paulus.Common;

namespace ConvertMerge
{
    public enum CANRecorderType
    {
        Unknown,
        OneTimeColumnIn10Us,
        MultiTimeColumnsIn10Us,
        MultiTimeColumnsInS,
        OneTimeColumnInS //not viewed yet
    }

    public class CANRecorder : Recorder
    {
        public CANRecorder() { }

        public CANRecorder(string sourceFilePath) : base(sourceFilePath) { }

        #region CANalyzer type
        protected CANRecorderType _type;
        public CANRecorderType Type { get { return _type; } }

        public CANRecorderType GetCANType() { return GetCANType(_sourceFilePath); }

        public static CANRecorderType GetCANType(string filePath)
        {
            string line1 = StreamReaderExtensions.ReadLine(filePath, 1, Encoding.Default);
            if (line1.Contains(";;Time [s]") || line1.Contains(",,Time [s]") || line1.Contains(";;Time[s]") || line1.Contains(",,Time[s]"))
                return CANRecorderType.MultiTimeColumnsInS;
            else if (line1.Contains(";;Time [10 us]") || line1.Contains(",,Time [10 us]") || line1.Contains(";;Time[10 us]") || line1.Contains(",,Time[10 us]"))
                return CANRecorderType.MultiTimeColumnsIn10Us;
            else if (line1.StartsWith("Time [10 us]") || line1.StartsWith("Time[10 us]"))
                return CANRecorderType.OneTimeColumnIn10Us;
            else if (line1.StartsWith("Time [s]") || line1.StartsWith("Time[s]"))
                return CANRecorderType.OneTimeColumnInS;
            else
                return CANRecorderType.Unknown;
        }

        public static bool IsCANRecorder(string filePath)
        {
            try
            {
                string line = StreamReaderExtensions.ReadLine(filePath, 1);
                return line.Contains(";;Time") || line.Contains(",,Time") || line.StartsWith("Time [10 us]") || line.StartsWith("Time[10 us]");
            }
            catch
            {
                return false;
            }
        }


        #endregion

        protected override void ReadVariableInfos()
        {
            //be sure that the scanmastertype is set first
            if (_type == CANRecorderType.Unknown) _type = GetCANType();

            string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);

            switch (_type)
            {
                case CANRecorderType.OneTimeColumnIn10Us:
                case CANRecorderType.OneTimeColumnInS:
                    {
                        string[] variableNameTokens = line1.Split(new char[] { _separator }, StringSplitOptions.None);
                        int count = line1.CharacterCount(_separator);
                        for (int iVariable = 0; iVariable < count; iVariable++)
                        {
                            VariableInfo<double> v = new VariableInfo<double>()
                            {
                                Name =Prefix + variableNameTokens[iVariable + 1] + Postfix,
                                ColumnInSourceFile = iVariable + 1,
                                TimeColumn = 0,
                                Unit = "",
                                Recorder = this
                            };

                            _variables.Add(v);
                        }
                    }
                    break;
                case CANRecorderType.MultiTimeColumnsInS:
                case CANRecorderType.MultiTimeColumnsIn10Us:
                    {
                        //line1 = line1.Replace($"{_separator}{_separator}", _separator.ToString()); //line1.Replace(";;", ";");
                        string[] variableNameTokens = line1.Split(new char[] { _separator },StringSplitOptions.RemoveEmptyEntries);

                        int count = variableNameTokens.Length / 2; //Corrected @ 30/5/2017 because in some cases the line does NOT end with ';;'

                        for (int iVariable = 0; iVariable < count; iVariable++)
                        {
                            VariableInfo<double> v = new VariableInfo<double>()
                            {
                                Name =Prefix +  variableNameTokens[2 * iVariable + 1] + Postfix,
                                ColumnInSourceFile = 3 * iVariable + 1,
                                TimeColumn = 3 * iVariable,
                                Unit = "",
                                Recorder = this
                            };

                            _variables.Add(v);

                        }
                    }
                    break;
            }

        }

        #region Time file
        public string TimeFilePath { get; set; }

        public int TimeIndexInTimeFile { get; set; }

        //returns the time file that is the most probable to be the correspondent for the CANRecorder
        private string findTimeFile()
        {
            //030414_1can.csv
            //searches for all files in the format 030414_1*.txt

            #region Search based on filenames (not used)
            ////search from the end
            //string fileName = Path.GetFileNameWithoutExtension(_sourceFilePath);
            //for (int i = fileName.Length - 1;i>=0; i--)
            //{
            //    if (fileName.Substring(i, 1).IsNumeric()) //stop at the first numeric text
            //    {
            //        string fileNamePart = fileName.Substring(0, i+1);
            //        return Directory.GetFiles(Path.GetDirectoryName(_sourceFilePath), fileNamePart + "*.txt");
            //    }
            //}
            #endregion

            //get all the text files
            string[] textFiles = Directory.GetFiles(Path.GetDirectoryName(_sourceFilePath), "*.txt");


            //filter only the time files
            IEnumerable<string> timeFiles = textFiles.Where(txtfile => isTimeFile(txtfile));
            //return the time file with the closest modified time
            return getTimeFileWithClosestModifiedTime(timeFiles);
        }

        private bool isTimeFile(string path)
        {
            try
            {
                Dictionary<int, string> lines = StreamReaderExtensions.ReadLines(path, 1, 2, 3);
                return lines.All(entry => entry.Value.StartsWith("System\t"));
            }
            catch
            {
                return false;
            }
        }

        private string getTimeFileWithClosestModifiedTime(IEnumerable<string> timeFiles)
        {
            if (timeFiles == null) return null;

            DateTime sourceFileModifiedTime = File.GetLastWriteTime(_sourceFilePath);

            double? minTimeDifference = null;
            string currentTimeFile = null;
            foreach (string timeFile in timeFiles)
            {
                DateTime timeFileModifiedTime = File.GetLastWriteTime(timeFile);
                //ignore the date (time is only taken into account)
                timeFileModifiedTime = timeFileModifiedTime.SetDate(sourceFileModifiedTime);

                double dt = Math.Abs((sourceFileModifiedTime - timeFileModifiedTime).TotalMilliseconds);
                if (!minTimeDifference.HasValue || minTimeDifference.Value > dt)
                {
                    minTimeDifference = dt; currentTimeFile = timeFile;
                }
            }

            return currentTimeFile;
        }

        private int getTimeIndexWithClosestEndOfMeasurementTime(string timeFile)
        {
            DateTime sourceFileModifiedTime = File.GetLastWriteTime(_sourceFilePath);

            int iIndex = 0;
            string ln = "";

            int? closestTimeIndex = 1; double? minTimeDifference = null;
            using (StreamReader reader = new StreamReader(TimeFilePath))
            {
                while (!reader.EndOfStream)
                {
                    ln = reader.ReadLine();
                    if (ln.Contains("End of measurement"))
                    {
                        iIndex++;
                        DateTime t = getTimeFromTimeFileLine(ln);
                        t = t.SetDate(sourceFileModifiedTime.Date);
                        double dt = (t - sourceFileModifiedTime).TotalMilliseconds;
                        if (!minTimeDifference.HasValue || minTimeDifference > dt)
                        {
                            minTimeDifference = dt; closestTimeIndex = iIndex;
                        }
                    }
                }
            }

            return closestTimeIndex.Value;
        }

        private DateTime getTimeFromTimeFileLine(string ln)
        {
            DateTime t;
            //get the last two tokens 
            string[] tokens = ln.Split(' ');
            // 01:32:24 pm
            string sTime = string.Format("{0} {1}", tokens[tokens.Length - 2], tokens[tokens.Length - 1]);
            bool parsed = DateTime.TryParseExact(sTime,
                new string[] { "hh:mm:ss tt", "hh:mm:ss.fff tt" },
                EN, DateTimeStyles.AssumeLocal, out t);
            return t;
        }

        #endregion

        /// <summary>
        /// The MeasurementDate must have been preset before doing anything AND the ReadVariableInfos.
        /// </summary>
        protected internal override void ReadStartingTime()
        {
            if (_xmlRecord != null)
            {
                DateTime? startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
                if (startTime.HasValue)
                {
                    StartAbsoluteTime = startTime.Value;
                    goto SetMeasurementDate;
                }
            }

            string timefile = null;
            if (_xmlRecord != null)
                timefile = _xmlRecord.GetAttributeOrElementText("timefile");

            if (string.IsNullOrWhiteSpace(timefile))
                timefile = findTimeFile(); //_sourceFilePath.Replace("can.csv", "log.txt");

            if (!string.IsNullOrWhiteSpace(timefile))
            {
                this.TimeFilePath =
                   PathExtensions.IsPathAbsolute(timefile) ? timefile :
                    Path.Combine(Path.GetDirectoryName(_sourceFilePath), timefile);

                //string TimeFilePath = _sourceFilePath.Replace("can.csv", "log.txt");
                //if (!File.Exists(TimeFilePath)) TimeFilePath = _sourceFilePath.Replace(" can.csv", "log.txt");
                if (!File.Exists(TimeFilePath)) throw new FileNotFoundException("Cannot find log file.", TimeFilePath);

                double? timeIndex = null;
                if (_xmlRecord != null) timeIndex = _xmlRecord.GetAttributeOrElementDouble("timeindex");
                TimeIndexInTimeFile = timeIndex.HasValue ? (int)timeIndex.Value : getTimeIndexWithClosestEndOfMeasurementTime(TimeFilePath);

                int iIndex = 0;
                string ln = "";
                using (StreamReader reader = new StreamReader(TimeFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        ln = reader.ReadLine();
                        if (ln.Contains("Start of measurement"))
                        {
                            iIndex++;
                            if (iIndex == TimeIndexInTimeFile) break;
                        }
                    }
                }

                StartAbsoluteTime = getTimeFromTimeFileLine(ln);
            }

            SetMeasurementDate: if (MeasurementDate.HasValue && StartAbsoluteTime != null)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
            else
                StartAbsoluteTime = StartAbsoluteTime.SetDate(new FileInfo(_sourceFilePath).LastWriteTime);
        }

        protected override int linesToOmitBeforeData
        {
            get { return 1; }
        }

        protected override char ReadSeparator()
        {
            string line1 = StreamReaderExtensions.ReadLine(this.SourceFilePath, 1);
            if (line1.StartsWith("Time [10 us]"))
                return line1[12];

            return ';';
        }

        protected override bool LoadDataFromLine(string[] tokens, ref int iLine)
        {
            if (_separator == ';')
            {
                bool replaceComma = tokens[0].Contains(',');
                if (replaceComma) for (int itoken = 0; itoken < tokens.Length; itoken++) tokens[itoken] = tokens[itoken].Replace(',', '.');
            }

            //if(_sourceFilePath=="D:\\lat\\phd\\obd\\Stoneridge 2013\\Honda Stoneridge 2013\\2013-11-27\\2013-11-27_1can.csv" &&
            //    iLine==5489)
            //System.Diagnostics.Debugger.Break();

            foreach (VariableInfo<double> v in _variables)
            {
                //WATCH!! HAPPENS
                if (v.TimeColumn >= tokens.Length || v.ColumnInSourceFile >= tokens.Length) continue;
                //WATCH!! HAPPENS AT THE EMPTY CELLS (AT THE END)
                if (tokens[v.TimeColumn].Length == 0 || tokens[v.ColumnInSourceFile].Length == 0) continue;

                double previousTimeInSeconds = currentRelativeTimes[v];

                double relativeTime;
                bool parsed = double.TryParse(tokens[v.TimeColumn], NumberStyles.Any, EN, out relativeTime);
                if (!parsed) continue;

                if (_type == CANRecorderType.OneTimeColumnIn10Us || _type == CANRecorderType.MultiTimeColumnsIn10Us) relativeTime /= 1e5;
                //WATCH!!! HAPPENS
                if (relativeTime <= previousTimeInSeconds)
                {
                    System.Diagnostics.Debugger.Break();
                    continue;
                }
                else
                    currentRelativeTimes[v] = relativeTime;

                if (parsed) //if _type=CANRecorderType.OneTimeColumnIn10Us the token might be empty
                {
                    double value;
                    parsed = double.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);
                    //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                    //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                    v.RelativeTimesInSecondsBeforeTimeStepChange.Add(currentRelativeTimes[v]);
                    v.ValuesBeforeTimeStepChange.Add(value);
                }
            }

            return true;
        }
    }
}
