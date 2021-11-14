using Paulus.IO;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Paulus.Common;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConvertMerge
{
    public class HoribaPems2Recorder : Recorder
    {
        public HoribaPems2Recorder() { }
        public HoribaPems2Recorder(string sourcePath) : base(sourcePath) { }

        public static bool IsHoribaPems2Recorder(string filePath)
        {
            try
            {
                string line3 = StreamReaderExtensions.ReadLine(filePath, 3);
                return line3.Contains("SYS_TestSequenceNumber");
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
            return ',';
        }

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = readSeparator();

            DateTime? startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
            if (startTime.HasValue)
                StartAbsoluteTime = startTime.Value;
                //the sDate variable needs to be retrieved first before going inside here
            else
            {
                //trim trailing comma!
                string line4 = StreamReaderExtensions.ReadLine(_sourceFilePath, 4).TrimEnd(',');
                string[] tokens = line4.Split(_separator);

                string sDateAndTime = tokens[0];

                //06/08/2021 14:19:28.1418304

                Match m = Regex.Match(sDateAndTime,
                    @"(?<month>\d{2})/(?<day>\d{2})/(?<year>\d{4}) (?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})\.(?<ms>\d{3})");
                int year = int.Parse(m.Groups["year"].Value),
                    month = int.Parse(m.Groups["month"].Value),
                    day = int.Parse(m.Groups["day"].Value),
                    hour = int.Parse(m.Groups["hour"].Value),
                    minute = int.Parse(m.Groups["minute"].Value),
                    second = int.Parse(m.Groups["second"].Value),
                    millisecond = int.Parse(m.Groups["ms"].Value);

                StartAbsoluteTime = new DateTime(year, month, day, hour, minute, second,millisecond);
            }

            //SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
            else
                StartAbsoluteTime = StartAbsoluteTime.SetDate(File.GetLastWriteTime(_sourceFilePath).Date);

        }

        protected override void ReadVariableInfos()
        {
            var lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 1, 2, 3);

            string[] names = lines[3].TrimEnd(',').Split(_separator);
            string[] units = lines[2].Split(_separator).Select(u=>u.Trim('[',']')).ToArray();

            for (int i = 2; i < names.Length - 1; i++) //exclude the timestamp variable
            {
                //string name = names[i] == names2[i] ?
                //    names[i] : $"{names[i]}|{names2[i]}", unit = units[i];
                
                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + names[i] + Postfix, //SHOULD BE APPLIED WHEN WRITING TO FILE (this is temporary here)
                    ColumnInSourceFile = i,
                    TimeColumn = 1,
                    Unit = units[i],
                    Recorder = this
                };

                _variables.Add(v);
            }
        }
    }
}
