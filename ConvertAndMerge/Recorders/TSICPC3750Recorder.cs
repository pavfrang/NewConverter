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
    //e.g. "D:\lat\dev\NewConverter\recorders\CPC (TSI 3750)\cpc3750_200206_01.csv"
    public class TSICPC3750Recorder : Recorder
    {
        public TSICPC3750Recorder() { }
        public TSICPC3750Recorder(string sourcePath) : base(sourcePath) { }

        public static bool IsTSICPC3750Recorder(string filePath)
        {
            try
            {
                string line = StreamReaderExtensions.ReadLine(filePath, 17);
                return line.StartsWith("Date-Time,Elapsed Time(s),");
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
                return 17;
            }
        }

        protected override char ReadSeparator()
        {
            return ',';
        }

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = ReadSeparator();

            DateTime? startTime = _xmlRecord?.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
            if (startTime != null)
                StartAbsoluteTime = startTime.Value;
            else
            {
                string line4 = StreamReaderExtensions.ReadLine(_sourceFilePath, 4);
                string sDateAndTime = line4.Substring(line4.LastIndexOf(',') + 1);

                DateTime starttime;
                bool parsedTime = DateTime.TryParseExact(sDateAndTime, "yyyy-MM-dd HH:mm:ss",
                   GR, DateTimeStyles.AssumeLocal, out starttime);
                if (parsedTime) StartAbsoluteTime = starttime;
                else
                    StartAbsoluteTime = new DateTime();
            }

            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
        }

        protected override void ReadVariableInfos()
        {
            //skip date-time, elapsed time
            string[] namesAndUnits = StreamReaderExtensions.ReadLine(_sourceFilePath, 17).Split(_separator).ToArray();

            for (int i = 2; i < namesAndUnits.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(namesAndUnits[i])) continue;

                var vu = namesAndUnits[i].GetVariableNameAndUnit('(', ')', "-");

                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + vu.Name.Trim() + Postfix, //SHOULD BE APPLIED WHEN WRITING TO FILE (this is temporary here)
                    ColumnInSourceFile = i,
                    TimeColumn = 1,
                    Unit = vu.Unit,
                    Recorder = this
                };

                _variables.Add(v);
            }
        }

    }


}
