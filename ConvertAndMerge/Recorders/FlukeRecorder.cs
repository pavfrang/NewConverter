using Paulus.IO;
using Paulus.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ConvertMerge
{
    public class FlukeRecorder : Recorder
    {
        #region Constructors
        public FlukeRecorder(string sourcePath) : base(sourcePath) { }
        public FlukeRecorder() : base() { }
        #endregion


        public static bool IsFlukeRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
                return line1.StartsWith("FLUKE");
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
                return 10;
            }
        }

        protected override char ReadSeparator()
        {
            return ';';
        }

        protected string timeFormat = @"d/M/yyyy H:mm:ss.f";

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

            //ensure that the separator is read
            _separator = ReadSeparator();

            string firstValueLine = StreamReaderExtensions.ReadLine(_sourceFilePath, 11);
            string[] tokens = firstValueLine.Split(_separator);
            string dateToken = tokens[3];

            StartAbsoluteTime = DateTime.ParseExact(dateToken,
               timeFormat, EN);


            SetMeasurementDate:
            if (MeasurementDate.HasValue) StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);



        }

        protected override void ReadVariableInfos()
        {
            string[] names = StreamReaderExtensions.ReadLine(_sourceFilePath, 10).
                Split(new char[] { _separator }, StringSplitOptions.None);


            //add the 'Reading' variable
            VariableInfo v = new VariableInfo<double>()
            {
                Name = Prefix +  names[0] + Postfix,
                ColumnInSourceFile = 0,
                TimeColumn = 3,
                Unit = "-",
                InterpolationMode = InterpolationMode.Next,
                Recorder = this
            };
            _variables.Add(v);

            for (int i = 1; i < names.Length; i++)
            {
                string name = names[i];
                //empty names show the unit of the previous name
                if (name == "")
                    name = names[i - 1] + " Unit";

                if (name.Contains("Time"))
                    v = new VariableInfo<DateTime>()
                    {
                        Name = Prefix + name + Postfix,
                        ColumnInSourceFile = i,
                        TimeColumn = 3,
                        Unit = "-",
                        InterpolationMode = InterpolationMode.Next,
                        Recorder = this
                    };
                else //if (name.Contains("Unit"))
                    v = new VariableInfo<string>()
                    {
                        Name = Prefix + name + Postfix,
                        ColumnInSourceFile = i,
                        TimeColumn = 3,
                        Unit = "-",
                        InterpolationMode = InterpolationMode.Next,
                        Recorder = this
                    };


                _variables.Add(v);
            }
        }

        protected override string PreProcessLineBeforeSplit(string rawLine)
        {
            if (rawLine.Contains("Logging Stopped"))
                return "";
            else
                return rawLine.Replace("  OL", "");
        }

        protected override bool LoadDataFromLine(string[] tokens, ref int iLine)
        {

            foreach (VariableInfo v in _variables)
            {
                //ASSUME SECONDS

                if (v.TimeColumn > tokens.Length - 1 || v.ColumnInSourceFile > tokens.Length - 1) return false;

                DateTime absoluteTime;

                bool parsed = DateTime.TryParseExact(tokens[v.TimeColumn], timeFormat, EN, DateTimeStyles.None, out absoluteTime);
                double relativeTimeInSeconds = (absoluteTime - StartAbsoluteTime).TotalSeconds;

                //ignore rows with invalid time (there are cases with the same time)
                var relativeTimes = v.RelativeTimesInSecondsBeforeTimeStepChange;
                if (relativeTimes.Count > 0 &&
                    relativeTimeInSeconds <= relativeTimes[relativeTimes.Count - 1])
                    return true;

                if (parsed)
                {
                    if (v.GetType() == typeof(VariableInfo<string>))
                    {
                        //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                        //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                        (v as VariableInfo<string>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                        (v as VariableInfo<string>).ValuesBeforeTimeStepChange.Add(tokens[v.ColumnInSourceFile]);
                    }
                    else if (v.GetType() == typeof(VariableInfo<DateTime>))
                    {
                        DateTime value;
                        parsed = DateTime.TryParseExact(tokens[v.ColumnInSourceFile], timeFormat, EN, DateTimeStyles.None, out value);
                        if (parsed) //only if the value can be parsed it is valid to add the time/value pair
                        {
                            //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                            //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                            (v as VariableInfo<DateTime>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                            (v as VariableInfo<DateTime>).ValuesBeforeTimeStepChange.Add(value);
                        }

                    }
                    else if (v.GetType() == typeof(VariableInfo<double>))
                    {
                        int value;
                        parsed = int.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);
                        if (parsed) //only if the value can be parsed it is valid to add the time/value pair
                        {
                            //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                            //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                            (v as VariableInfo<double>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                            (v as VariableInfo<double>).ValuesBeforeTimeStepChange.Add(value);
                        }
                    }
                }
            }

            return true;
        }


    }
}
