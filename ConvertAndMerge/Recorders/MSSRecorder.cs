using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using System.Diagnostics;

using System.IO;
using Paulus.IO;
using Paulus;
using Paulus.Common;

namespace ConvertMerge
{
    public class MSSRecorder : Recorder
    {
        public MSSRecorder() { }
        public MSSRecorder(string sourceFilePath) : base(sourceFilePath) { }

        public static bool IsMSSRecorder(string filePath)
        {
            string line = StreamReaderExtensions.ReadLine(filePath, 2);
            return line.StartsWith("### Micro Soot Sensor");
        }

        protected override void ReadVariableInfos()
        {
            string variableLine = StreamReaderExtensions.ReadLine(_sourceFilePath, linesToOmitBeforeData);
            string[] tokens = variableLine.Split(',');

            for (int i = 2; i < tokens.Length; i++)
            {
                string token = tokens[i];

                var vu = token.GetVariableNameAndUnit('[', ']', "-");
                VariableInfo<double> v = new VariableInfo<double>()
                {
                    Name =Prefix + vu.Name + Postfix,
                    ColumnInSourceFile = i,
                    TimeColumn = 0,
                    Unit = vu.Unit,
                    Recorder = this
                };

                _variables.Add(v);
            }
        }


        protected override int linesToOmitBeforeData
        {
            get
            {
                int linesToOmit=0;
                using (StreamReader reader = new StreamReader(_sourceFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        linesToOmit++;
                        if (reader.ReadLine().StartsWith("Date [")) break;
                    }
                }

                return linesToOmit;
            }
        }

        protected override char ReadSeparator()
        {
            return ',';
        }

        protected internal override void ReadStartingTime()
        {
            
            {
                string line7 = StreamReaderExtensions.ReadLine(_sourceFilePath, 7);
                int colon = line7.IndexOf(':');
                //### Log-File started: 10/4/11, 10:38:29 AM ###
                int numberSign = line7.IndexOf('#', colon + 1);
                string sDateTime = line7.Substring2(colon + 1, numberSign - 1).Trim();
                StartAbsoluteTime = DateTime.ParseExact(sDateTime, @"M/d/yy, h:mm:ss tt", EN);
                //THIS MUST BE SET ALL THE TIMES
                startDateTimeInFile = StartAbsoluteTime; //unchanged measurement date
            }
            if (_xmlRecord != null &&  _xmlRecord.HasAttribute("starttime"))
                StartAbsoluteTime = DateTime.ParseExact(_xmlRecord.GetAttributeOrElementText("starttime"), "HH:mm:ss", EN);
           
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        private DateTime startDateTimeInFile;

        protected override bool LoadDataFromLine(string[] tokens, ref int iLine)
        {
            foreach (VariableInfo<double> v in _variables)
            {
                //DateTime absoluteTime;
                //ASSUME SECONDS

                bool parsed; //= double.TryParse(tokens[v.TimeColumn], out relativeTimeInSeconds);

                DateTime absoluteTime;
                parsed = DateTime.TryParseExact(tokens[0] + " " + tokens[1], "M/d/yy HH:mm:ss.fff", EN, System.Globalization.DateTimeStyles.AssumeLocal, out absoluteTime);

                if (parsed)
                {
                    double relativeTimeInSeconds;
                    relativeTimeInSeconds = Math.Round((absoluteTime - startDateTimeInFile).TotalSeconds, 2);

                    double value; parsed = double.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);
                    if (parsed) //only if the value can be parsed it is valid to add the time/value pair
                    {
                        //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                        //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                        v.RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                        v.ValuesBeforeTimeStepChange.Add(value);
                    }
                }
                //else
                //{
                //    Debugger.Break();
                //}
            }

            return true;
        }


    }
}
