using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Globalization;
using Paulus.IO;
using Paulus.Common;
using System.Diagnostics;

namespace ConvertMerge
{
    public class SemsRecorder : Recorder
    {
        public SemsRecorder() { }
        public SemsRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsSemsRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
                return line1.StartsWith("UTC,");
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
                return 2;
            }
        }

        protected override char ReadSeparator()
        {
            return ',';
        }

        DateTime originalStartTime;

        const string DateTimeFormat = "yyMMddHHmmss";

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = ReadSeparator();

            string line3 = StreamReaderExtensions.ReadLine(_sourceFilePath, 3);
            string[] tokens = line3.Split(_separator);
            string sDateTime = tokens[0];
            DateTime starttime;
            //200831164036
            bool parsedTime = DateTime.TryParseExact(sDateTime,DateTimeFormat, EN, System.Globalization.DateTimeStyles.AssumeUniversal, out starttime);
            if (parsedTime)
            {
                originalStartTime = StartAbsoluteTime = starttime;
            }
            else
                StartAbsoluteTime = new DateTime();

            if (_xmlRecord != null)
            {
                DateTime? startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
                if (startTime.HasValue)
                StartAbsoluteTime = startTime.Value;
            }
            CheckContinueFileRecord<SemsRecorder>();

            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        protected override void ReadVariableInfos()
        {
            string[] names = StreamReaderExtensions.ReadLine(_sourceFilePath, 1).Split(_separator);
            string[] units = StreamReaderExtensions.ReadLine(_sourceFilePath, 2).Split(_separator);

            for (int i = 1; i < names.Length; i++)
            {
                string name = names[i], unit = units[i].Substring(1,units[i].Length-2);

                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + name + Postfix,
                    ColumnInSourceFile = i,
                    TimeColumn = 0,
                    Unit = unit,
                    Recorder = this
                };

                _variables.Add(v);
            }
        }

        int iTime = 0;
        DateTime lastTime = DateTime.MinValue;

        protected override bool LoadDataFromLineBeforeTimeOffset(string[] tokens, ref int iLine)
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
                    parsedTime = DateTime.TryParseExact(tokens[v.TimeColumn], DateTimeFormat, EN, DateTimeStyles.AssumeUniversal, out absoluteTime);

                    //the measurement date must be correctly set according to the user settings
                    if (MeasurementDate.HasValue)
                    {
                        absoluteTime = absoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
                        absoluteTime = this.StartAbsoluteTime.Add(absoluteTime - originalStartTime);

                        if ((absoluteTime - this.StartAbsoluteTime).TotalHours < -2)
                            absoluteTime = absoluteTime.AddDays(1.0);
                    }
                    else
                    {
                        DateTime measurementDate;
                        bool parsedDate = DateTime.TryParseExact(tokens[0], DateTimeFormat, EN, DateTimeStyles.AssumeUniversal, out measurementDate);
                        if (parsedDate)
                        {
                            absoluteTime = absoluteTime.SetDate(measurementDate);
                            absoluteTime = this.StartAbsoluteTime.Add(absoluteTime - originalStartTime);
                        }
                        else
                            absoluteTime = absoluteTime.SetDate(File.GetLastWriteTime(_sourceFilePath).Date);

                        //if ((absoluteTime - this.StartAbsoluteTime).TotalHours < 20)
                        //    absoluteTime.SetDate(MeasurementDate.Value.AddDays(1));
                    }

                    //if (_sourceTimeUnit == "ms") relativeTimeInSeconds *= 0.001;
                    //absoluteTime = StartAbsoluteTime.AddSeconds(relativeTimeInSeconds);

                    relativeTimeInSeconds = (absoluteTime - this.StartAbsoluteTime).TotalSeconds;

                    //if (lastTime > absoluteTime) Debugger.Break();
                    lastTime = absoluteTime;
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
