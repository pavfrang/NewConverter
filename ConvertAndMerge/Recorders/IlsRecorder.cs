using Paulus.Common;
using Paulus.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertMerge
{
    public class IlsRecorder : Recorder
    {
        public IlsRecorder() { }

        public IlsRecorder(string sourcePath) : base(sourcePath) { }


        public static bool IsIlsRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);

                //return line1.StartsWith("Date,Time,Mean Conc. PCRF Corr.");
                return line1.Contains("Time\";\"");
            }
            catch
            {
                return false;
            }
        }

        protected override int linesToOmitBeforeData => 1;

        protected override char ReadSeparator() => ';';


        private static string DateTimeFormat = "dd.MM.yy HH:mm:ss.fff";

        protected internal override void ReadStartingTime()
        {
            _separator = ReadSeparator();

            DateTime starttime;
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

            string line = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);

            string[] tokens = line.Split(_separator);
            string sDateTime = tokens.First();

            bool parsedTime = DateTime.TryParseExact(sDateTime,
                DateTimeFormat,
                GR, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) originalStartTime = StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();
            //string t = StartAbsoluteTime.ToString("dddd, dd MMMM yyyy  hh:mm:ss tt",GR);

            CheckContinueFileRecord<IlsRecorder>();

        SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }


        protected override void ReadVariableInfos()
        {
            string[] variableAndUnits = StreamReaderExtensions.ReadLine(_sourceFilePath, 1).Split(_separator).
                Select(v=>v.Trim('"').Replace(" ValueY","")).ToArray();

            int variablesCount = variableAndUnits.Length / 2;

            //"01i2FIC01\GasBottle Time";"01i2FIC01\GasBottle ValueY";
            //"C2H4 Rich Side Actual Concentration [x10E6 ppm] Time";"C2H4 Rich Side Actual Concentration [x10E6 ppm] ValueY";
            for (int i = 0; i < variablesCount; i++)
            {
                var vu = variableAndUnits[2*i+1].GetVariableNameAndUnit('[', ']');
                //if (!vu.Name.Contains("Bottle")) Debugger.Break();
                
                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + vu.Name + Postfix,
                    ColumnInSourceFile = 2*i+1,
                    TimeColumn = 2*i,
                    Unit = vu.Unit,
                    Recorder = this
                };

                _variables.Add(v);
            }

           // Debugger.Break();
        }


        protected override string PreProcessLineBeforeSplit(string rawLine)
        {
            //remove [m.] text
            //m. i. u. are acceptable
            return rawLine.Replace("[m.]", "").Replace("[i.]", "").Replace("[u.]", "");
        }


        int iTime = 0;
        DateTime lastTime = DateTime.MinValue;
        DateTime originalStartTime;


        protected override bool LoadDataFromLineBeforeTimeOffset(string[] tokens, ref int iLine)
        {
            foreach (VariableInfo v in _variables)
            {
                DateTime absoluteTime;

                if (v.TimeColumn > tokens.Length - 1 || v.ColumnInSourceFile > tokens.Length - 1) return false;

                double relativeTimeInSeconds;
                bool parsedTime;
                if (!ForceTimeStep)
                {
                    //bool parsed = double.TryParse(tokens[v.TimeColumn], out relativeTimeInSeconds);
                    parsedTime = DateTime.TryParseExact(tokens[v.TimeColumn], DateTimeFormat, EN, DateTimeStyles.AssumeLocal, out absoluteTime);

                    //the measurement date must be correctly set according to the user settings
                    if (MeasurementDate.HasValue)
                    {
                        absoluteTime = absoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
                        absoluteTime = this.StartAbsoluteTime.Add(absoluteTime - originalStartTime);

                        if ((absoluteTime - this.StartAbsoluteTime).TotalHours < -2)
                            absoluteTime = absoluteTime.AddDays(1.0);
                    }
                    //else
                    //{
                    //    DateTime measurementDate;
                    //    bool parsedDate = DateTime.TryParseExact(tokens[0], DateTimeFormat, EN, DateTimeStyles.AssumeLocal, out measurementDate);
                    //    if (parsedDate)
                    //    {
                    //        absoluteTime = absoluteTime.SetDate(measurementDate);
                    //        absoluteTime = this.StartAbsoluteTime.Add(absoluteTime - originalStartTime);
                    //    }
                    //    else
                    //        absoluteTime = absoluteTime.SetDate(File.GetLastWriteTime(_sourceFilePath).Date);

                    //    //if ((absoluteTime - this.StartAbsoluteTime).TotalHours < 20)
                    //    //    absoluteTime.SetDate(MeasurementDate.Value.AddDays(1));
                    //}

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
                    else if (v.GetType() == typeof(VariableInfo<int>))
                    {
                        int value;
                        parsedTime = int.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);

                        //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                        //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                        (v as VariableInfo<int>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                        (v as VariableInfo<int>).ValuesBeforeTimeStepChange.Add(value);
                    }


                    else Debugger.Break();
                }
            }

            if (ForceTimeStep) iTime++;

            return true;
        }

    }
}
