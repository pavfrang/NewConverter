using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

using System.Xml;
using Paulus;
using Paulus.IO;
using Paulus.Common;

namespace ConvertMerge
{
    public enum IncaRecorderType
    {
        Unknown,
        WithIncaTimeVariable, //variables include the Date,UnivTime are included 
        WithDatFile,
        WithAsciiFileTime,
        WithStartTime //if "starttime" attribute is provided
    }

    public class INCARecorder : Recorder
    {
        #region Constructors
        public INCARecorder(string sourcePath) : base(sourcePath) { }
        public INCARecorder() : base() { }
        #endregion

        private IncaRecorderType _type;
        public IncaRecorderType Type { get { return _type; } }

        private IncaRecorderType GetIncaRecorderType()
        {
            //xml record is the criteria if the xml record is provided
            if (_xmlRecord != null)
            {
                if (_xmlRecord.HasAttribute("timefile"))
                    return IncaRecorderType.WithAsciiFileTime;
                else if (_xmlRecord.HasAttribute("datfile"))
                    return IncaRecorderType.WithDatFile;
                else if (_xmlRecord.HasAttribute("timevariable"))
                    return IncaRecorderType.WithIncaTimeVariable;
                else if (_xmlRecord.HasAttribute("starttime"))
                    return IncaRecorderType.WithStartTime;
            }

            return AttemptToGetUnknownRecorderType();
        }

        protected string timeFile, datFile;

        private IncaRecorderType AttemptToGetUnknownRecorderType()
        {
            //R1 INCA829_4.ascii
            string fileName = Path.GetFileNameWithoutExtension(_sourceFilePath);

            //remove the last underscore part
            string strippedFileName = fileName.Substring2(0, fileName.LastIndexOf('_') - 1);

            //first search for ascii file?
            string[] asciiFiles = Directory.GetFiles(Path.GetDirectoryName(_sourceFilePath), strippedFileName + "*.ascii");
            foreach (string asciiFile in asciiFiles)
            {
                if (TryReadStartTimeFromTimeFile(asciiFile))
                {
                    if (_xmlRecord != null)
                    {
                        XmlAttribute timeFileAttribute = _xmlRecord.OwnerDocument.CreateAttribute("timefile");
                        timeFileAttribute.Value = asciiFile;
                        _xmlRecord.Attributes.Append(timeFileAttribute);
                    }

                    //update internal variable
                    timeFile = asciiFile;

                    return IncaRecorderType.WithAsciiFileTime;
                }
            }

            //then assume that the dat file is present
            datFile = Path.Combine(Path.GetDirectoryName(_sourceFilePath), strippedFileName + ".dat");

            if (_xmlRecord != null)
            {
                XmlAttribute datFileAttribute = _xmlRecord.OwnerDocument.CreateAttribute("datfile");
                datFileAttribute.Value = datFile;
                _xmlRecord.Attributes.Append(datFileAttribute);
            }
            return IncaRecorderType.WithDatFile;
        }

        //private INCARecorderType SearchForRecorderType()
        //{
        //    string line3 = StreamReaderExtensions.ReadLine(_sourceFilePath, 3, Encoding.Default);
        //    if (line3.Contains("Date") && line3.Contains("UnivTime"))
        //        return INCARecorderType.WithDateUnivTime;
        //    else
        //        return INCARecorderType.Unknown;
        //}

        //should include a thorough check for all kind of error that might happen
        //we assume a tab that is included in the file
        public static bool IsIncaRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
                return line1.StartsWith("ETASAsciiItemFile\t");
            }
            catch
            {
                return false;
            }
        }

        protected override void ReadVariableInfos()
        {
            //be sure that the scanmastertype is set first
            if (_type == IncaRecorderType.Unknown) _type = GetIncaRecorderType();

            Dictionary<int, string> lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 3, 5, 6);

            //time	Tamb	Date	UnivTime	Pout	DP	Tout	Time	Pin	Tsensor	Tin	
            //"s"	"°C"	"[ - ]"	"[ - ]"	"mbar"	"mbar"	"°C"	"sec"	"mbar"	"°C"	"°C"	
            string variableNamesLine = lines[3].TrimEnd('\t');

            string[] variableNameTokens = variableNamesLine.Split('\t');

            string unitsLine = lines[5].Replace("\"", ""); //no need to remove trailing \t here
            string[] unitTokens = unitsLine.Split('\t');

            double testFirstToken; bool hasUnits = true;
            hasUnits = !double.TryParse(unitTokens[0], out testFirstToken);


            decimal_point = lines[6].CharacterCount(',') > lines[6].CharacterCount('.') ? ',' : '.';

            _sourceTimeUnit = hasUnits ? unitTokens[0] : "s";

            //remove the time variable token at the beginning
            int count = variableNameTokens.Length - 1;
            for (int iVariable = 0; iVariable < count; iVariable++)
            {
                VariableInfo<double> v = new VariableInfo<double>()
                {
                    Name = Prefix + variableNameTokens[iVariable + 1] + Postfix,
                    ColumnInSourceFile = iVariable + 1,
                    TimeColumn = 0,
                    Unit = hasUnits ? unitTokens[iVariable + 1].Trim() : "",
                    Recorder = this
                };
                if (v.Unit == "[ - ]" || string.IsNullOrWhiteSpace(v.Unit)) v.Unit = "-";

                _variables.Add(v);
            }
        }

        protected internal override void ReadStartingTime()
        {
            if (_xmlRecord != null)
            {
                DateTime? startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
                if (startTime.HasValue)
                {
                    StartAbsoluteTime = startTime.Value;
                    //the sDate variable needs to be retrieved first before going inside here
                    goto SetMeasurementDate;
                }
            }

            //be sure that the type is set first
            if (_type == IncaRecorderType.Unknown) _type = GetIncaRecorderType();


            bool success = false;
            if (_type == IncaRecorderType.WithIncaTimeVariable)
            {
                success = TryReadStartTimeFromTimeVariable(_xmlRecord.GetAttributeOrElementText("timevariable"));
            }
            else if (_type == IncaRecorderType.WithDatFile)
            {
                success = TryReadStartTimeFromDatFile(_xmlRecord != null ? _xmlRecord.GetAttributeOrElementText("datfile") : datFile);
            }
            else if (_type == IncaRecorderType.WithAsciiFileTime)
            {
                success = TryReadStartTimeFromTimeFile(_xmlRecord != null ? _xmlRecord.GetAttributeOrElementText("timefile") : timeFile);
            }

        SetMeasurementDate:
            if (MeasurementDate.HasValue) StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        private bool TryReadStartTimeFromTimeVariable(string timeVariable)
        {
            try
            {
                //timeVariable
                int univTimeIndex = (from v in _variables where v.Name == timeVariable select v.ColumnInSourceFile).ToList()[0];

                string line5 = StreamReaderExtensions.ReadLine(_sourceFilePath, 5);
                string line6 = StreamReaderExtensions.ReadLine(_sourceFilePath, 6);

                string unitsLine = line5.Replace("\"", "").TrimEnd('\t');
                string[] candidateUnitTokens = unitsLine.Split('\t');
                double testFirstToken; bool hasUnits = true;
                hasUnits = !double.TryParse(candidateUnitTokens[0], out testFirstToken);

                string[] tokens = hasUnits ? line6.Split('\t') : candidateUnitTokens;
                string timeToken = tokens[univTimeIndex];

                string relativeTimeToken = tokens[0];
                double relativeStartTime = double.Parse(relativeTimeToken, EN);

                timeToken = timeToken.Replace(',', '.');

                //131523.0, 93354.0
                long iTime = (long)double.Parse(timeToken);
                int hour = (int)(iTime / 10000); iTime -= hour * 10000;
                int minute = (int)(iTime / 100); iTime -= minute * 100;
                int second = (int)iTime;
                StartAbsoluteTime = new DateTime(MeasurementDate.Value.Year, MeasurementDate.Value.Month, MeasurementDate.Value.Day, hour, minute, second);

                //CHECK HOW TO VIEW THE CORRECT SOURCE TIMEUNIT
                if (_sourceTimeUnit == "ms") StartAbsoluteTime = StartAbsoluteTime.AddMilliseconds(-relativeStartTime);
                else StartAbsoluteTime = StartAbsoluteTime.AddSeconds(-relativeStartTime);
                return true;
            }
            catch { return false; }
        }

        private bool TryReadStartTimeFromDatFile(string datFilePath)
        {
            //string[] files = Directory.GetFiles(_sourceFilePath, "*.dat");
            //foreach (string datFilePath in files)
            try
            {
                if (!PathExtensions.IsPathAbsolute(datFilePath))
                    datFilePath = Path.Combine(Experiment.SourceDirectory, datFilePath);

                using (FileStream stream = new FileStream(datFilePath, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        reader.ReadBytes(82);
                        char[] c = reader.ReadChars(18);
                        string s = new string(c);
                        StartAbsoluteTime = DateTime.ParseExact(s, "dd:MM:yyyyHH:mm:ss", CultureInfo.InvariantCulture);
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryReadStartTimeFromTimeFile(string timeFilePath)
        {
            try
            {
                //THE EXPERIMENT MUST HAVE BEEN SET
                if (!PathExtensions.IsPathAbsolute(timeFilePath))
                    timeFilePath = Path.Combine(Experiment.SourceDirectory, timeFilePath);

                if (timeFilePath.EndsWith("ascii"))
                {
                    string line5 = StreamReaderExtensions.ReadLine(timeFilePath, 5);
                    string line6 = StreamReaderExtensions.ReadLine(timeFilePath, 6);

                    string line = null;
                    if (line6.Contains("Start event occurred")) line = line6;
                    else if (line5.Contains("Start event occurred")) line = line5;

                    if (line != null)
                    {
                        //2.303300000150443e-005\t"Date: 04.10.2011    Time: 10:38:27  Start event occurred"\t	
                        int startDate = line.IndexOf("Date:");
                        int startTime = line.IndexOf("Time:", startDate + 1);
                        int startStart = line.IndexOf("Start", startTime + 1);
                        string sDate = line.Substring2(startDate + 5, startTime - 1).Trim();
                        string sTime = line.Substring2(startTime + 5, startStart - 1).Trim();
                        //foundDate = true;
                        StartAbsoluteTime = DateTime.ParseExact(sDate + " " + sTime, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        return true;
                    }
                    else return false;
                }
                //else if (timeFilePath.EndsWith("dat"))
                //    return TryReadStartTimeFromDatFile(timeFilePath);
                return false;
            }
            catch { return false; }
        }

        private bool TryReadStartTimeFromNumberOfRecordsAndLastWriteTime()
        {
            try
            {
                //else return an estimate of the time of creation
                //sampleCount	590
                //the second line contains the number of seconds
                string line2 = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);

                int seconds = int.Parse(line2.Substring(12).Trim());
                StartAbsoluteTime = File.GetLastWriteTime(_sourceFilePath).AddSeconds(-seconds);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected override int linesToOmitBeforeData
        {
            get { return 5; }
        }

        protected override char readSeparator()
        {
            return '\t';
        }

        char decimal_point;
        protected override string preProcessLineBeforeSplit(string rawLine)
        {
            return rawLine.Replace(',', '.');
        }
    }
}
