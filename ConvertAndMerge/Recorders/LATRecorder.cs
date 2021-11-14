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
    public class LATRecorder : Recorder
    {
        public LATRecorder() { }
        public LATRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsLATRecorder(string filePath)
        {
            try
            {
                Dictionary<int, string> lines = StreamReaderExtensions.ReadLines(filePath, 1, 2);
                //string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
                //string line2 = StreamReaderExtensions.ReadLine(filePath, 2);

                return lines[1].StartsWith("Rec Time (s)") ||
                    lines[2].StartsWith("Rec Time (s)");
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
                string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);
                if (line1.StartsWith("Rec Time (s)")) return 1;
                else return 2;
            }
        }

        protected override char readSeparator()
        {
            string line3 = StreamReaderExtensions.ReadLine(_sourceFilePath, 3);
            if (line3.CharacterCount('\t') > line3.CharacterCount(','))
                return '\t';
            else return ',';
        }

        protected internal override void ReadStartingTime()
        {
            //force reading the separator
            _separator = readSeparator();

            string line = StreamReaderExtensions.ReadLine(_sourceFilePath, linesToOmitBeforeData + 1);

            //int lastComma = line.LastIndexOf(',');
            //string sTime = line.Substring(lastComma + 1);
            string[] tokens = line.Split(_separator);
            string sTime = tokens[tokens.Length - 1];
            if (!sTime.Contains(':')) //get the time from actual hour, actual minute, actual second
                sTime = tokens[tokens.Length - 3] + ":" + tokens[tokens.Length - 2] + ":" + tokens[tokens.Length - 1];

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

            bool parsedTime = DateTime.TryParseExact(sTime, new string[] { "HH:mm:ss", "HH:mm:ss.ff" }, EN, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();

            SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
            else
                StartAbsoluteTime = StartAbsoluteTime.SetDate(File.GetLastWriteTime(_sourceFilePath).Date);

        }

        protected override void ReadVariableInfos()
        {
            string line = StreamReaderExtensions.ReadLine(_sourceFilePath, linesToOmitBeforeData);
            string[] tokens = line.Split(_separator);

            HashSet<string> timeVariables = new HashSet<string>
            { "Actual hour","Actual minute", "Actual second" ,"Actual time"};

            for (int i = 1; i < tokens.Length; i++)
            {
                string token = tokens[i];

                var vu = token.GetVariableNameAndUnit('(', ')', "-");

                VariableInfo v;
                if (timeVariables.Contains(vu.Name)) continue;

                v = new VariableInfo<double>()
                {
                    Name = Prefix + vu.Name + Postfix,
                    ColumnInSourceFile = i,
                    TimeColumn = 0,
                    Unit = vu.Unit,
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
