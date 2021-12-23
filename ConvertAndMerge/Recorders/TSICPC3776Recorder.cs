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
    public class TSICPC3776Recorder : Recorder
    {
        public TSICPC3776Recorder() { }
        public TSICPC3776Recorder(string sourcePath) : base(sourcePath) { }

        public static bool IsTSICPC3776Recorder(string filePath)
        {
            try
            {
                string line = StreamReaderExtensions.ReadLine(filePath, 7);
                return line.Contains("Sample Length");
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
                return 18;
            }
        }

        protected override char ReadSeparator()
        {
            //separator may be tab or ;
            string line5 = StreamReaderExtensions.ReadLine(_sourceFilePath, 5);
            return line5.Last();
        }

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = ReadSeparator();

            string line5 = StreamReaderExtensions.ReadLine(_sourceFilePath, 5);
            string line6 = StreamReaderExtensions.ReadLine(_sourceFilePath, 6);

            string[] tokens = line5.Split(_separator);
            string sDate = tokens[1];
            tokens = line6.Split(_separator);
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
            bool parsedTime = DateTime.TryParseExact(sTime, "HH:mm:ss", EN, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();

            SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
            else
            {
                DateTime measurementDate;
                bool parsedDate = DateTime.TryParseExact(sDate, "dd/MM/yy", EN, DateTimeStyles.AssumeLocal, out measurementDate);
                if (parsedDate)
                    StartAbsoluteTime = StartAbsoluteTime.SetDate(measurementDate);
                else
                    StartAbsoluteTime = StartAbsoluteTime.SetDate(File.GetLastWriteTime(_sourceFilePath).Date);
            }

        }

        protected override void ReadVariableInfos()
        {
            string[] namesAndUnits = StreamReaderExtensions.ReadLine(_sourceFilePath, 18).Trim().Split(_separator);

            for (int i = 1; i < namesAndUnits.Length; i++)
            {
                var vu = namesAndUnits[i].GetVariableNameAndUnit('(', ')', "-");


                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + vu.Name + Postfix , //SHOULD BE APPLIED WHEN WRITING TO FILE (this is temporary here)
                    ColumnInSourceFile = i,
                    TimeColumn =0,
                    Unit = vu.Unit,
                    Recorder = this
                };

                _variables.Add(v);
            }
        }


        int iTime = 0;
        protected override bool LoadDataFromLine(string[] tokens, ref int iLine)
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
                    parsedTime = DateTime.TryParseExact(tokens[v.TimeColumn], "HH:mm:ss", EN, DateTimeStyles.AssumeLocal, out absoluteTime);

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
