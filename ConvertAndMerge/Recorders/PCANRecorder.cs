using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Paulus.IO;
using System.Globalization;
using System.IO;
using Paulus;
using Paulus.Common;

namespace ConvertMerge
{
    public class PCANRecorder : Recorder
    {
        public PCANRecorder() { }

        public PCANRecorder(string sourceFilePath) : base(sourceFilePath) { }

        public static bool IsPCANRecorder(string filePath)
        {
            try
            {
                string line = StreamReaderExtensions.ReadLine(filePath, 1);
                return line.Contains("(Time)") && line.Contains("(Y)");
            }
            catch
            {
                return false;
            }
        }

        protected override char readSeparator()
        {
            string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);
            return line1.CharacterCount(',') > line1.CharacterCount(';') ? ',' : ';';
        }

        protected override void ReadVariableInfos()
        {
            string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);

            string[] variableNameTokens = line1.Split(_separator);

            int count = (line1.CharacterCount(_separator) + 1) / 2;
            for (int iVariable = 0; iVariable < count; iVariable++)
            {
                VariableInfo<double> v = new VariableInfo<double>()
                {
                    Name = Prefix + variableNameTokens[2 * iVariable + 1] + Postfix,
                    ColumnInSourceFile = 2 * iVariable + 1,
                    TimeColumn = 2 * iVariable,
                    Unit = "",
                    Recorder = this
                };

                _variables.Add(v);
            }
        }

        /// <summary>
        /// The MeasurementDate must have been preset before doing anything AND the ReadVariableInfos.
        /// </summary>
        protected internal override void ReadStartingTime()
        {
            string line2 = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);
            string[] tokens = line2.Split(_separator);

            //13:15:25.520.4,0
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

        protected override bool loadDataFromLine(string[] tokens, ref int iLine)
        {
            foreach (VariableInfo<double> v in _variables)
            {
                DateTime absoluteTime;
                bool parsed = readTimeFromLine(tokens, v.TimeColumn, out absoluteTime);

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

        private bool readTimeFromLine(string[] tokens, int timeColumn, out DateTime absoluteTime)
        {
            bool parsed = DateTime.TryParseExact(tokens[timeColumn].Substring(0, 12), "HH:mm:ss.fff", EN, DateTimeStyles.AssumeLocal, out absoluteTime);
            absoluteTime = absoluteTime.SetDate(MeasurementDate.Value);
            return parsed;

        }
    }
}
