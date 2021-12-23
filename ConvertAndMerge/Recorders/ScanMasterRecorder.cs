using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Globalization;
using System.IO;

using System.Diagnostics;
using Paulus.IO;
using Paulus;
using Paulus.Common;

namespace ConvertMerge
{
    public enum ScanMasterRecorderType
    {
        Unknown,
        WithQuotes,
        WithoutQuotesGR,
        WithoutQuotesHH
    }

    public class ScanMasterRecorder : Recorder
    {
        #region Constructors
        public ScanMasterRecorder() { }

        public ScanMasterRecorder(string sourcePath) : base(sourcePath) { }
        #endregion

        #region Recorder type
        protected ScanMasterRecorderType _type;
        public ScanMasterRecorderType Type { get { return _type; } }

        public ScanMasterRecorderType GetScanMasterType() { return GetScanMasterType(_sourceFilePath); }

        public static ScanMasterRecorderType GetScanMasterType(string filePath)
        {
            string line4 = StreamReaderExtensions.ReadLine(filePath, 4, Encoding.Default);
            if (line4.StartsWith("\"")) return ScanMasterRecorderType.WithQuotes;
            else
            {
                line4 = line4.Replace("\"", "");
                string[] tokens = line4.Split(';');
                if (tokens[0].Contains("μ")) //μμ / πμ
                    return ScanMasterRecorderType.WithoutQuotesGR;
                else if (tokens[0].Length == 8)
                    return ScanMasterRecorderType.WithoutQuotesHH;
                else
                    return ScanMasterRecorderType.Unknown;
            }
        }

        public static bool IsScanMasterRecorder(string filePath)
        {
            try
            {
                var lines = StreamReaderExtensions.ReadLines(filePath, 1, 2, 3);
                return lines[1].StartsWith("\"Time\";\"") || lines[1].StartsWith("Time;") && lines[2].StartsWith("Time;") && lines[3].StartsWith("Time;");
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
            if (_type == ScanMasterRecorderType.Unknown) _type = GetScanMasterType();

            var lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 2, 3);

            //"Time";"04 - Calculated Load Value";"Time";"05 - Engine Coolant Temperature";"Time";"0B - Intake Manifold Absolute Pressure";"Time";"0C - Engine RPM";"Time";"0D - Vehicle Speed";"Time";"0F - Intake Air Temperature";"Time";"10 - Air Flow Rate";"Time";"23 - Fuel Rail Pressure";"Time";"2C - Commanded EGR";
            //"Time";"%";"Time";"°C";"Time";"kPa";"Time";"rpm";"Time";"km/h";"Time";"°C";"Time";"g/s";"Time";"kPa";"Time";"%";
            //remove the quotation marks
            string variableNamesLine = lines[2].Replace("\"", "");
            string unitsLine = lines[3].Replace("\"", "");
            string[] variableNameTokens = variableNamesLine.Split(';');
            string[] unitTokens = unitsLine.Split(';');

            int count = (variableNamesLine.CharacterCount(';') + 1) / 2;
            for (int iVariable = 0; iVariable < count; iVariable++)
            {
                VariableInfo<double> v = new VariableInfo<double>()
                {
                    Name = Prefix + variableNameTokens[2 * iVariable + 1] + Postfix,
                    ColumnInSourceFile = 2 * iVariable + 1,
                    TimeColumn = 2 * iVariable,
                    Unit = unitTokens[2 * iVariable + 1],
                    Recorder=this
                };
                if ((_type == ScanMasterRecorderType.WithoutQuotesGR || _type == ScanMasterRecorderType.WithoutQuotesHH) && v.TimeColumn == 0)
                    v.TimeColumn = 2;

                _variables.Add(v);
            }
        }

        /// <summary>
        /// The MeasurementDate must have been preset before doing anything.
        /// </summary>
        protected internal override void ReadStartingTime()
        {
            //SHOULD CHECK THE SCANMASTERTYPE
            if (_type == ScanMasterRecorderType.Unknown) _type = GetScanMasterType(_sourceFilePath);

            string line4 = StreamReaderExtensions.ReadLine(_sourceFilePath, 4).Replace("\"", "");
            string[] tokens = line4.Split(';');

            DateTime startTime;
            bool parsed = tryParseTime(tokens, 0, out startTime);

            StartAbsoluteTime = startTime;

            if(MeasurementDate.HasValue)
            StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        protected override int linesToOmitBeforeData
        {
            get { return 3; }
        }

        protected override string PreProcessLineBeforeSplit(string rawLine)
        {
            return rawLine.Replace("\"", "");
        }

        protected override char ReadSeparator()
        {
            return ';';
        }

        protected override bool LoadDataFromLine(string[] tokens, ref int iLine)
        {
            foreach (VariableInfo<double> v in _variables)
            {
                DateTime absoluteTime;
                bool parsed = readTimeFromLine(tokens, v, out absoluteTime);

                if (parsed)
                {
                    double relativeTimeInSeconds = (absoluteTime - StartAbsoluteTime).TotalSeconds;
                    double value; parsed = double.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);
                    if (parsed) //only if the value can be parsed it is valid to add the time/value pair
                    {
                        //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                        //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                        v.RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                        v.ValuesBeforeTimeStepChange.Add(value);
                    }
                }
            }

            return true;
        }


        #region Read time for each variable/line
        //returns true if the date-time has been read
        private bool readTimeFromLine(string[] tokens, VariableInfo v, out DateTime absoluteTime)
        {
            //if the value at the column of the variable does not exist then return
            //the absoluteTime value is not taken into account or it does not exist
            //not an error but should report it
            if (v.ColumnInSourceFile >= tokens.Length) { absoluteTime = default(DateTime); return false; }

            bool parsed = tryParseTime(tokens, v.TimeColumn, out absoluteTime);

            if (_type == ScanMasterRecorderType.WithoutQuotesGR || _type == ScanMasterRecorderType.WithoutQuotesHH)
            {
                DateTime time0;
                parsed = tryParseTime(tokens, 0, out time0);

                if ((absoluteTime - time0).TotalMinutes >= 1) //first SPECIAL case
                    //17:00:00;68.23529412;59:59.6;84;59:59.7;107;59:59.8;1490;00:00.0;
                    //convert 17:59:59.8 to 16:59:59.8
                    absoluteTime = absoluteTime.AddHours(-1);
                else if ((time0 - absoluteTime).TotalMinutes >= 1) //second SPECIAL case
                    //17:59:59;55.68627451;59:58.8;88;59:59.0;101;59:59.1;1349;00:00.1;
                    //convert 17:00:00.1 to 18:00:00.1
                    absoluteTime = absoluteTime.AddHours(1);
            }

            absoluteTime= absoluteTime.SetDate(MeasurementDate.Value);
            return parsed;
        }

        private bool tryParseTime(string[] tokens, int timeColumn, out DateTime absoluteTime)
        {
            string timeString = getTimeString(tokens, timeColumn);
            return DateTime.TryParseExact(timeString, getTimeStringFormat(timeColumn == 0), GR, DateTimeStyles.AssumeLocal, out absoluteTime);
        }

        private string getTimeStringFormat(bool forFirstToken = false)
        {
            switch (_type)
            {
                case ScanMasterRecorderType.WithoutQuotesHH:
                    return !forFirstToken ? "HH:mm:ss.f" : "HH:mm:ss";
                case ScanMasterRecorderType.WithoutQuotesGR:
                    return !forFirstToken ? "h:mm:ss.f tt" : "h:mm:ss tt";
                case ScanMasterRecorderType.WithQuotes:
                    return "HH:mm:ss.fff";
                default:
                    return null;
            }
        }

        private string getTimeString(string[] tokens, int timeColumn)
        {
            if (timeColumn == 0) return tokens[0];

            switch (_type)
            {
                case ScanMasterRecorderType.WithoutQuotesHH:
                    return tokens[0].Substring(0, tokens[0].IndexOf(':') + 1) + tokens[timeColumn];
                case ScanMasterRecorderType.WithoutQuotesGR:
                    return tokens[0].Substring(0, tokens[0].IndexOf(':') + 1) + tokens[timeColumn] + tokens[0].Substring(tokens[0].IndexOf(' '));
                case ScanMasterRecorderType.WithQuotes:
                    return tokens[timeColumn];
                default: return null;
            }
        }
        #endregion




    }
}
