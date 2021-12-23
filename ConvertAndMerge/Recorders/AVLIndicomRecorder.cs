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
    public class AVLIndicomRecorder : Recorder
    {
        public AVLIndicomRecorder() { }
        public AVLIndicomRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsAVLIndicomRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);

                return line1.StartsWith("Time\t") &&
                    line1.EndsWith("DATE\tTIMESTAMP\t");
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
            return '\t';
        }

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = ReadSeparator();

            DateTime? startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
            if (startTime.HasValue)
                StartAbsoluteTime = startTime.Value;
                //the sDate variable needs to be retrieved first before going inside here
            else
            {
                string line3 = StreamReaderExtensions.ReadLine(_sourceFilePath, 3).TrimEnd('\t');
                string[] tokens = line3.Split(_separator);

                string sDateAndTime = tokens.Last();

                //eg:20200227095329405
                Match m = Regex.Match(sDateAndTime,
                    @"^(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})(?<hour>\d{2})(?<minute>\d{2})(?<second>\d{2})");
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
            }

            //SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
            else
                StartAbsoluteTime = StartAbsoluteTime.SetDate(File.GetLastWriteTime(_sourceFilePath).Date);
        }

        protected override void ReadVariableInfos()
        {
            var lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 1, 2);

            string[] names = lines[1].TrimEnd(_separator).Split(_separator);
            string[] units = lines[2].Split(_separator);

            for (int i = 1; i < names.Length - 2; i++) //exclude the timestamp and date variable
            {
                string name = names[i] , unit = units[i];

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
