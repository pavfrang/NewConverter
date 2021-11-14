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
    public class FTIRRecorder : Recorder
    {
        public FTIRRecorder() { }
        public FTIRRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsFTIRRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);

                //return line1.StartsWith("Date,Time,Mean Conc. PCRF Corr.");
                return line1.StartsWith("TimeLocal");
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
            return '\t';
        }

        DateTime? endTime; //FTIR tag specific
        DateTime? cropStartTime; //FTIR tag specific

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = readSeparator();

            //else retrieve the start time from the file
            string line2 = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);
            string[] tokens = line2.Split(_separator);

            string sDate = tokens[0];


            bool parsedTime = DateTime.TryParseExact(sDate, timeFormat, EN, System.Globalization.DateTimeStyles.AssumeLocal, out startLocalTime);
            if (parsedTime) StartAbsoluteTime = startLocalTime;
            else
                StartAbsoluteTime = new DateTime();

            if (_xmlRecord != null)
            {
                DateTime? startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
                this.endTime = _xmlRecord.GetAttributeOrElementDateTime("endtime", "HH:mm:ss");
                this.cropStartTime = _xmlRecord.GetAttributeOrElementDateTime("cropstarttime", "HH:mm:ss");

                if (startTime.HasValue)
                {
                    StartAbsoluteTime = startTime.Value;
                    //the sDate variable needs to be retrieved first before going inside here
                    goto SetMeasurementDate;
                }
            }

        SetMeasurementDate:
            if (MeasurementDate.HasValue)
            {
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
                if (endTime != null) endTime = endTime.Value.SetDate(MeasurementDate.Value);
                if (cropStartTime != null) cropStartTime = cropStartTime.Value.SetDate(MeasurementDate.Value);
            }

        }


        protected override void ReadVariableInfos()
        {
            string[] names = StreamReaderExtensions.ReadLine(_sourceFilePath, 1).Split(_separator);

            //skip timelocal
            for (int i = 1; i < names.Length; i++)
            {
                string name = names[i];

                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + name + Postfix, //SHOULD BE APPLIED WHEN WRITING TO FILE (this is temporary here)
                    ColumnInSourceFile = i,
                    TimeColumn = 0,
                    Unit = "-",
                    Recorder = this
                };

                _variables.Add(v);
            }
        }
        const string timeFormat = "dd/MM/yyyy HH:mm:ss.fffffff";

        DateTime startLocalTime; //without starttime modification

        protected override bool omitLinesBasedOnCropStartTime(string[] tokens, ref int iLine)
        {
            if (!cropStartTime.HasValue) return false;

            //return base.omitLinesBasedOnCropStartTime(tokens, ref iLine);
            VariableInfo v = _variables[0];

            double relativeTimeInSeconds;
            bool parsedTime;
            DateTime absoluteTime = DateTime.Now; //force setting a value here for the last return statement


            //bool parsed = double.TryParse(tokens[v.TimeColumn], out relativeTimeInSeconds);
            parsedTime = DateTime.TryParseExact(tokens[v.TimeColumn], timeFormat, EN, DateTimeStyles.AssumeLocal, out absoluteTime);

            relativeTimeInSeconds = (absoluteTime - this.startLocalTime).TotalSeconds;

            absoluteTime = this.StartAbsoluteTime.AddSeconds(relativeTimeInSeconds);

            //the measurement date must be correctly set according to the user settings
            if (MeasurementDate.HasValue)
                absoluteTime = absoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!

            return absoluteTime < cropStartTime.Value;

        }

        int iTime = 0;
        protected override bool loadDataFromLine(string[] tokens, ref int iLine)
        {
            DateTime absoluteTime = DateTime.Now; //force setting a value here for the last return statement
            foreach (VariableInfo v in _variables)
            {

                if (v.TimeColumn > tokens.Length - 1 || v.ColumnInSourceFile > tokens.Length - 1) return false;

                double relativeTimeInSeconds;
                bool parsedTime;
                if (!ForceTimeStep)
                {
                    //bool parsed = double.TryParse(tokens[v.TimeColumn], out relativeTimeInSeconds);
                    parsedTime = DateTime.TryParseExact(tokens[v.TimeColumn], timeFormat, EN, DateTimeStyles.AssumeLocal, out absoluteTime);

                    relativeTimeInSeconds = (absoluteTime - this.startLocalTime).TotalSeconds;

                    absoluteTime = this.StartAbsoluteTime.AddSeconds(relativeTimeInSeconds);

                    //the measurement date must be correctly set according to the user settings
                    if (MeasurementDate.HasValue)
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

            return endTime.HasValue ? absoluteTime <= endTime.Value : true;
        }
    }


}
