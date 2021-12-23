using System;
using System.Globalization;
using System.Linq;
using Paulus.IO;
using Paulus;
using Paulus.Common;

namespace ConvertMerge
{
    public class PPSRecorder : Recorder
    {
        public PPSRecorder() { }

        public PPSRecorder(string sourceFilePath) : base(sourceFilePath) { }

        public static bool IsPPSRecorder(string filePath)
        {
            string line = StreamReaderExtensions.ReadLine(filePath, 1);
            return line.Contains("_pA [pA]") && line.Contains("comments");
        }

        protected override void ReadVariableInfos()
        {
            string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);

            string[] variableNameWithUnitTokens = line1.Split('\t');

            int count = line1.CharacterCount('\t');
            int iVariable;

            for (iVariable = 0; iVariable < count - 1; iVariable++)
            {
                string token = variableNameWithUnitTokens[iVariable + 1];
                string unit, name;
                int openBracket = token.IndexOf('[');
                if (openBracket > 0)
                {
                    name = token.Substring(0, openBracket).Trim();
                    unit = token.GetTokensBetweenCharacters('[', ']')[0];
                }
                else
                {
                    name = token; unit = "";
                }

                VariableInfo<double> v = new VariableInfo<double>()
                {
                    Name = Prefix + name + Postfix,
                    ColumnInSourceFile = iVariable + 1,
                    TimeColumn = 0,
                    Unit = unit,
                    Recorder = this
                };

                _variables.Add(v);
            }

            iVariable = count - 1;
            VariableInfo<string> v2 = new VariableInfo<string>()
            {
                Name = Prefix + variableNameWithUnitTokens[iVariable + 1] + Postfix,
                ColumnInSourceFile = iVariable + 1,
                TimeColumn = 0,
                Unit = "",
                Recorder = this
            };
            _variables.Add(v2);
        }

        /// <summary>
        /// The MeasurementDate must have been preset before doing anything AND the ReadVariableInfos.
        /// </summary>
        protected internal override void ReadStartingTime()
        {
            //SHOULD CHECK THE SCANMASTERTYPE
            string line2 = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);
            string[] tokens = line2.Split('\t');


            //15:05:10
            DateTime startTime;

            bool parsed = readTimeFromLine(tokens, 0, out startTime);

            StartAbsoluteTime = startTime;

            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        protected override int linesToOmitBeforeData
        {
            get { return 1; }
        }

        protected override char ReadSeparator()
        {
            return '\t';
        }

        protected override bool LoadDataFromLine(string[] tokens, ref int iLine)
        {
            if (tokens[0].StartsWith("end of data")) return false;

            int iv; DateTime absoluteTime; bool parsed;

            for (iv = 0; iv < _variables.Count - 1; iv++)
            {
                VariableInfo<double> v = (VariableInfo<double>)_variables[iv];
                parsed = readTimeFromLine(tokens, v.TimeColumn, out absoluteTime);

                if (parsed)
                {
                    absoluteTime = absoluteTime.SetDate(MeasurementDate.Value);

                    double previousTimeInSeconds = currentRelativeTimes[v];
                    double relativeTime = (absoluteTime - StartAbsoluteTime).TotalSeconds;
                    //WATCH!!! HAPPENS
                    if (relativeTime <= previousTimeInSeconds)
                        continue;
                    else
                        currentRelativeTimes[v] = relativeTime;

                    double value; parsed = double.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);
                    if (parsed) //only if the value can be parsed it is valid to add the time/value pair
                    {
                        //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                        //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                        v.RelativeTimesInSecondsBeforeTimeStepChange.Add(currentRelativeTimes[v]);
                        v.ValuesBeforeTimeStepChange.Add(value);
                    }
                }
            }

            iv = _variables.Count - 1;
            VariableInfo<string> v2 = (VariableInfo<string>)_variables[iv];
            parsed = readTimeFromLine(tokens, v2.TimeColumn, out absoluteTime);

            if (parsed)
            {
                double previousTimeInSeconds = currentRelativeTimes[v2];
                double relativeTime = (absoluteTime - StartAbsoluteTime).TotalSeconds;
                //WATCH!!! HAPPENS
                if (relativeTime <= previousTimeInSeconds)
                    return true;
                else
                    currentRelativeTimes[v2] = relativeTime;

                v2.RelativeTimesInSecondsBeforeTimeStepChange.Add(currentRelativeTimes[v2]);
                v2.ValuesBeforeTimeStepChange.Add(tokens[v2.ColumnInSourceFile]);
            }

            return true;
        }

        private bool readTimeFromLine(string[] tokens, int timeColumn, out DateTime absoluteTime)
        {
            CultureInfo c = EN;
            //!WATCH!!!!!
            string timeformat = !tokens[timeColumn].Contains('-') ? "HH:mm:ss" : "yyyy-MM-dd HH:mm:ss.f";
            if (tokens[timeColumn].EndsWith("μ"))
            {
                timeformat = "h:mm:ss tt";
                c = GR;
            }


            if (tokens[timeColumn].Contains('-'))
                tokens[timeColumn] = tokens[timeColumn].TrimEnd('0');

            bool parsed = DateTime.TryParseExact(tokens[timeColumn], new string[] { timeformat, "yyyy-MM-dd HH:mm:ss.ffffff" }, c, DateTimeStyles.AssumeLocal, out absoluteTime);
            absoluteTime = absoluteTime.SetDate(MeasurementDate.Value);

            return parsed;
        }
    }
}
