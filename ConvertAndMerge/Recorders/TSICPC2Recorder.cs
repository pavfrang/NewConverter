using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Globalization;
using Paulus.IO;
using Paulus.Common;

namespace ConvertMerge
{
    public class TSICPC2Recorder : Recorder
    {
        public TSICPC2Recorder() { }
        public TSICPC2Recorder(string sourcePath) : base(sourcePath) { }

        public static bool IsTSICPC2Recorder(string filePath)
        {
            try
            {
                string line = StreamReaderExtensions.ReadLine(filePath, 1);
                return line.Contains("Date;Time;CPC value;");
            }
            catch
            {
                return false;
            }
        }

        protected override int linesToOmitBeforeData
        {
            get
            {
                return 1;
            }
        }

        protected override char readSeparator()
        {
            return ';';
        }

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = readSeparator();

            string line2 = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);

            string[] tokens = line2.Split(_separator);
            string sDate = tokens[0];
            string sTime = tokens[1];

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

            DateTime starttime;
            bool parsedTime = DateTime.TryParseExact(sTime,
                new string[] { "hh:mm:ss tt", "h:mm:ss tt" },
               GR, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();

            SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
            else
            {
                DateTime measurementDate;
                bool parsedDate = DateTime.TryParseExact(sDate, "dd/MM/yyyy", EN, DateTimeStyles.AssumeLocal, out measurementDate);
                if (parsedDate)
                    StartAbsoluteTime = StartAbsoluteTime.SetDate(measurementDate);
                else
                    StartAbsoluteTime = StartAbsoluteTime.SetDate(File.GetLastWriteTime(_sourceFilePath).Date);
            }

        }

        protected override void ReadVariableInfos()
        {
            //last two headers should be omitted
            string[] names = StreamReaderExtensions.ReadLine(_sourceFilePath, 1).Trim().Split(_separator).Take(4).ToArray();

            for (int i = 2; i < names.Length; i++)
            {
                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + names[i] + Postfix, //SHOULD BE APPLIED WHEN WRITING TO FILE (this is temporary here)
                    ColumnInSourceFile = i,
                    TimeColumn = 1,
                    Unit = "-",
                    Recorder = this
                };

                _variables.Add(v);
            }
        }


        int iTime = 0;
        protected override bool loadDataFromLine(string[] tokens, ref int iLine)
        {
            foreach (VariableInfo v in _variables)
            {
                DateTime absoluteTime;
                //ASSUME SECONDS

                if (v.TimeColumn > tokens.Length - 1 || v.ColumnInSourceFile > tokens.Length - 1) return false;

                double relativeTimeInSeconds;
                bool parsedTime;
                if (!ForceTimeStep)
                {
                    //bool parsed = double.TryParse(tokens[v.TimeColumn], out relativeTimeInSeconds);
                    parsedTime = parsedTime = DateTime.TryParseExact(tokens[v.TimeColumn],
                        new string[] { "hh:mm:ss tt", "h:mm:ss tt" },
                        GR, System.Globalization.DateTimeStyles.AssumeLocal, out absoluteTime);

                    //the measurement date must be correctly set according to the user settings
                    //if (MeasurementDate.HasValue)
                    absoluteTime = absoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!

                    relativeTimeInSeconds = (absoluteTime - this.StartAbsoluteTime).TotalSeconds;
                }
                else
                {
                    relativeTimeInSeconds = (double)iTime;
                    parsedTime = true;
                }


                if (parsedTime)
                {
                    if (v.GetType() == typeof(VariableInfo<double>))
                    {
                        double value;
                        parsedTime = double.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);
                        if (parsedTime) //only if the value can be parsed it is valid to add the time/value pair
                        {
                            //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                            //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                            (v as VariableInfo<double>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                            (v as VariableInfo<double>).ValuesBeforeTimeStepChange.Add(value);
                        }
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

            if (ForceTimeStep) iTime++;

            return true;
        }
    }


}
