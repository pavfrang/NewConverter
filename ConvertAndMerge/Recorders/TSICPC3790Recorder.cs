using System;

using System.Globalization;
using Paulus.Common;
using Paulus.IO;

namespace ConvertMerge
{
    public class TSICPC3790Recorder : Recorder
    {
        public TSICPC3790Recorder() { }
        public TSICPC3790Recorder(string sourcePath) : base(sourcePath) { }

        public static bool IsTSICPC3790Recorder(string filePath)
        {
            try
            {
                string line = StreamReaderExtensions.ReadLine(filePath, 1);
                return line.Contains("PuTTY log");
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
            return ',';
        }

        int time0;
        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = readSeparator();

            string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);
            var lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 2,3);

            if(lines[2].StartsWith("U"))
                time0 = int.Parse(lines[2].SplitByComma()[0].Substring(1));
            else
                time0 = int.Parse(lines[3].SplitByComma()[0].Substring(1));


            string sDateAndTime = line1.Substring(34, 19);
            

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
            bool parsedTime = DateTime.TryParseExact(sDateAndTime,
                new string[] { "yyyy.MM.dd HH:mm:ss", "yyyy.MM.dd H:mm:ss" },
                EN, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();

            //override date if specified
            SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }


        protected override void ReadVariableInfos()
        {
            VariableInfo v = new VariableInfo<double>()
            {
                Name =Prefix +  "Concentration" + Postfix,
                ColumnInSourceFile = 2,
                TimeColumn = 0,
                Unit = "#/cm3",
                Recorder = this
            };

            _variables.Add(v);
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

                    if (tokens[0].Length < 2) return false;

                    int time;
                    parsedTime = int.TryParse(tokens[0].Substring(1), out time); //U1301

                    relativeTimeInSeconds = time - time0;
                    absoluteTime = StartAbsoluteTime.AddSeconds(relativeTimeInSeconds);
                    //the measurement date must be correctly set according to the user settings
                    //if (MeasurementDate.HasValue)
                    absoluteTime = absoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!

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
                        bool parsedValue = false;

                        double averageValue = 0.0;
                        for (int i = 1; i <= 10; i++)
                        {
                            parsedValue = double.TryParse(tokens[i], NumberStyles.Any, EN, out value);
                            averageValue += value;

                            if (!parsedValue) break;
                        }
                        value = averageValue / 10.0;

                        if (parsedValue) //only if the value can be parsed it is valid to add the time/value pair
                        {
                            //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                            //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                            (v as VariableInfo<double>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                            (v as VariableInfo<double>).ValuesBeforeTimeStepChange.Add(value);
                        }
                    }
                    //else if (v.GetType() == typeof(VariableInfo<string>))
                    //{
                    //    //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                    //    //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                    //    (v as VariableInfo<string>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                    //    (v as VariableInfo<string>).ValuesBeforeTimeStepChange.Add(tokens[v.ColumnInSourceFile]);
                    //}
                }
            }

            if (ForceTimeStep) iTime++;

            return true;
        }
    }


}
