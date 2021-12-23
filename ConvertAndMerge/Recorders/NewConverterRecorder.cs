using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Globalization;
using Paulus.IO;
using Paulus;
using Paulus.Common;
using System.Diagnostics;

namespace ConvertMerge
{
    public class NewConverterRecorder : Recorder
    {
        public NewConverterRecorder() { }
        public NewConverterRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsNewConverterRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
                return line1.StartsWith("Time [s]\tAbsolute Time\t");
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

        protected override char ReadSeparator()
        {
            return '\t';
        }

        protected internal override void ReadStartingTime()
        {
            //force reading the separator
            _separator = ReadSeparator();
            string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);
            //force read the time unit
            _sourceTimeUnit = line1.Split('\t')[0].GetVariableNameAndUnit('[', ']').Unit;


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

            string line2 = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);
            string[] tokens = line2.Split(_separator);
            string sTime = tokens[1];
            bool parsedTime = DateTime.TryParseExact(sTime, "yyyy-MM-dd HH:mm:ss.fff", EN, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();

            SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!

        }

        protected override void ReadVariableInfos()
        {
            string line = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);
            string[] tokens = line.Split(_separator);

            for (int i = 2; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (token.StartsWith("Reserved")) continue;
                token = token.Replace("Β", ""); //replace greek Beta

                var vu = token.GetVariableNameAndUnit('[', ']', "-");

                VariableInfo v;
                v = new VariableInfo<double>()
                {
                    Name =Prefix +  vu.Name + Postfix,
                    ColumnInSourceFile = i,
                    TimeColumn = 0,
                    Unit = vu.Unit,
                    Recorder = this
                };

                _variables.Add(v);
            }
        }

    }


}
