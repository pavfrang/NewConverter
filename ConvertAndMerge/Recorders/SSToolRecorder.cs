using System;
using System.Globalization;
using Paulus.IO;
using Paulus;
using Paulus.Common;
using System.IO;
using System.Diagnostics;

namespace ConvertMerge
{
    public class SSToolRecorder : Recorder
    {
        public SSToolRecorder() { }
        public SSToolRecorder(string sourceFilePath) : base(sourceFilePath) { }

        public static bool IsSSToolRecorder(string filePath)
        {
            string line = StreamReaderExtensions.ReadLine(filePath, 1);
            return line.Contains("Time;PID") || line.Contains("Time,PID");
        }


        protected override void ReadVariableInfos()
        {
            var lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 2, 3);


            //the same may occur with comma separated
            //Time;Engine speed;Vehicle speed;Air flow rate from mass air flow sensor;Commanded EGR
            //msec;1/min;km/h;g/sec;%

            string variableNamesLine = lines[2];
            string unitsLine = lines[3];
            string[] variableNameTokens = variableNamesLine.Split(_separator);
            string[] unitTokens = unitsLine.Split(_separator);

            int count = variableNamesLine.CharacterCount(_separator);
            for (int iVariable = 0; iVariable < count; iVariable++)
            {
                VariableInfo<double> v = new VariableInfo<double>()
                {
                    Name = Prefix +  variableNameTokens[iVariable + 1] + Postfix,
                    ColumnInSourceFile = iVariable + 1,
                    TimeColumn = 0,
                    Unit = unitTokens[iVariable + 1],
                    Recorder = this
                };

                _variables.Add(v);
            }
        }

        protected override int linesToOmitBeforeData
        {
            get { return 3; }
        }

        protected override char ReadSeparator()
        {
            //"Time,PID 0C
            string line = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);

            return line[4];
        }

        protected override bool LoadDataFromLine(string[] tokens, ref int iLine)
        {
            //WATCH !! (happens)
            if (tokens.Length != _variables.Count + 1)
                return true;

            foreach (VariableInfo<double> v in _variables)
            {
                double previousRelativeTimeInSeconds = currentRelativeTimes[v];

                double relativeTime;
                bool parsed = double.TryParse(tokens[v.TimeColumn].Trim(), NumberStyles.Any, EN, out relativeTime);
                relativeTime /= 1000;

                //WATCH!! (happens)
                if (relativeTime <= previousRelativeTimeInSeconds)
                    continue;
                else
                    currentRelativeTimes[v] = relativeTime;

                if (parsed)
                {
                    double value;
                    parsed = double.TryParse(tokens[v.ColumnInSourceFile].Trim(), NumberStyles.Any, EN, out value);
                    //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                    //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                    v.RelativeTimesInSecondsBeforeTimeStepChange.Add(currentRelativeTimes[v]);
                    v.ValuesBeforeTimeStepChange.Add(value);
                }
            }

            return true;
        }

        protected internal override void ReadStartingTime()
        {
            DateTime? startTime = null;
            if (_xmlRecord != null) startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");

            if (startTime.HasValue)
                StartAbsoluteTime = startTime.Value;
            else
            {
                DateTime lastTime = File.GetLastWriteTime(SourceFilePath);
                string line = StreamReaderExtensions.ReadLastNonEmptyLine(SourceFilePath);
                string[] tokens = line.Split(_separator);
                StartAbsoluteTime = lastTime.AddMilliseconds(-int.Parse(tokens[0]));
            }

            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }
    }
}
