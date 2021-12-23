using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Globalization;
using Paulus.IO;
using Paulus;
using Paulus.Common;

namespace ConvertMerge
{
    public class LabviewRecorder : Recorder
    {
        public LabviewRecorder() { }
        public LabviewRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsLabviewRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
                return line1.StartsWith("LabVIEW Measurement");
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
                return 22;
            }
        }

        protected override char ReadSeparator()
        {
            return '\t';
        }

        protected internal override void ReadStartingTime()
        {
            //force reading the separator
            _separator = ReadSeparator();
            _sourceTimeUnit = "s";

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

            string line = StreamReaderExtensions.ReadLine(_sourceFilePath, 17);

            string[] tokens = line.Split(_separator);
            string sTime = tokens[1].Substring(0, 11);

            bool parsedTime = DateTime.TryParseExact(sTime, new string[] { "HH:mm:ss", "HH:mm:ss.ff" }, EN, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();

            SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
            else
            {
                string line16 = StreamReaderExtensions.ReadLine(_sourceFilePath, 16);
                tokens = line16.Split(_separator);
                string sDate = tokens[1];
                DateTime date;
                bool parsedDate = DateTime.TryParseExact(sDate, "yyyy/MM/dd", EN, System.Globalization.DateTimeStyles.AssumeLocal, out date);
                if (parsedDate) StartAbsoluteTime = StartAbsoluteTime.SetDate(date);

            }


        }

        protected override void ReadVariableInfos()
        {
            string line = StreamReaderExtensions.ReadLine(_sourceFilePath, linesToOmitBeforeData);
            string[] tokens = line.Split(_separator);

            for (int i = 1; i < tokens.Length - 1; i++) //ignore the Comment variable
            {
                string token = tokens[i];

                bool hasUnit = token.Contains("/");
                string name, unit;
                if (hasUnit)
                {
                    string[] parts = token.Split('/');
                    name = parts[0].Trim();
                    unit = parts[1].Trim();
                }
                else
                {
                    name = token.Trim();
                    unit = "-";
                }

                VariableInfo v;
                v = new VariableInfo<double>()
                {
                    Name = Prefix + name + Postfix,
                    ColumnInSourceFile = i,
                    TimeColumn = 0,
                    Unit = unit,
                    Recorder = this
                };

                //else
                //{
                //    v = new VariableInfo<string>()
                //    {
                //        Name = vu.Name,
                //        ColumnInSourceFile = i,
                //        TimeColumn = 0,
                //        Unit = vu.Unit,
                //        Recorder = this
                //    };
                //}

                _variables.Add(v);
            }
        }

    }


}
