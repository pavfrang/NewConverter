using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Globalization;
using Paulus.IO;
using Paulus.Common;
using System.Text.RegularExpressions;

namespace ConvertMerge
{
    public class ConcertoRecorder : Recorder
    {
        public ConcertoRecorder() { }
        public ConcertoRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsConcertoRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
                return line1.EndsWith("TIMESTAMP,");
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
                return 3;
            }
        }

        protected override char readSeparator()
        {
            string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);
            return line1[4]; //Date;
                             // return ',';
        }

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = readSeparator();

            string line4 = StreamReaderExtensions.ReadLine(_sourceFilePath, 4);

            string[] tokens = line4.Trim(',').Split(_separator);
            string sRecordDateTime = tokens.Last();

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

            Match m = Regex.Match(sRecordDateTime,
    @"(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})(?<hour>\d{2})(?<minute>\d{2})(?<second>\d{2})");
            int year = int.Parse(m.Groups["year"].Value),
                month = int.Parse(m.Groups["month"].Value),
                day = int.Parse(m.Groups["day"].Value),
                hour = int.Parse(m.Groups["hour"].Value),
                minute = int.Parse(m.Groups["minute"].Value),
                second = int.Parse(m.Groups["second"].Value);
            //int year = int.Parse(sRecordDateTime.Substring(0, 4)),
            //    month = int.Parse(sRecordDateTime.Substring(4, 2)),
            //    day = int.Parse(sRecordDateTime.Substring(6, 2)),
            //    hour = int.Parse(sRecordDateTime.Substring(8, 2)),
            //    minute = int.Parse(sRecordDateTime.Substring(10, 2)),
            //    second = int.Parse(sRecordDateTime.Substring(12, 2));
            StartAbsoluteTime = new DateTime(year, month, day, hour, minute, second);

        SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
        }

        protected override void ReadVariableInfos()
        {
            string[] names = StreamReaderExtensions.ReadLine(_sourceFilePath, 1).Split(_separator);
            string[] units = StreamReaderExtensions.ReadLine(_sourceFilePath, 3).Split(_separator);

            for (int i = 2; i < names.Length - 2; i++)
            {
                string name = names[i], unit = units[i];

                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + name + Postfix, //SHOULD BE APPLIED WHEN WRITING TO FILE (this is temporary here)
                    ColumnInSourceFile = i,
                    TimeColumn = 0,
                    Unit = unit,
                    Recorder = this
                };

                _variables.Add(v);
            }
        }


    }


}
