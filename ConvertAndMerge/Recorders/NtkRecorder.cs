using Paulus.Common;
using Paulus.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ConvertMerge
{
    public class NtkRecorder : Recorder
    {
        public NtkRecorder() { }
        public NtkRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsNtkRecorder(string filePath)
        {
            try
            {
                string line2 = StreamReaderExtensions.ReadLine(filePath, 2);
                return line2.StartsWith("Time[sec],Main_Code");
            }
            catch
            {
                return false;
            }
        }


        protected override int linesToOmitBeforeData => 2;

        protected internal override void ReadStartingTime()
        {
            //measurement_data_20200714154309.csv
            //ensure that the separator is read
            _separator = readSeparator();

            DateTime? startTime = _xmlRecord?.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
            if (_xmlRecord != null && startTime.HasValue)
                StartAbsoluteTime = startTime.Value;
            //the sDate variable needs to be retrieved first before going inside here
            else
            {
                //read the file name
                //measurement_data_20200714154309
                Regex r = new Regex(@"(\d{4})(\d{2})(\d{2})(\d{2})(\d{2})(\d{2})");
                Match m = r.Match(_sourceFilePath);
                int y = int.Parse(m.Groups[1].Value);
                int month = int.Parse(m.Groups[2].Value);
                int d = int.Parse(m.Groups[3].Value);

                int h = int.Parse(m.Groups[4].Value);
                int min = int.Parse(m.Groups[5].Value);
                int s = int.Parse(m.Groups[6].Value);

                StartAbsoluteTime = new DateTime(y, month, d, h, min, s);
            }

            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
        }


        protected override void ReadVariableInfos()
        {
            string variableLine = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);
            string[] tokens = variableLine.Split(',');

            string firstNumbersLine = StreamReaderExtensions.ReadLine(_sourceFilePath, 3);
            string[] valueTokens = variableLine.Split(',');

            for (int i = 1; i < tokens.Length; i++)
            {
                string token = tokens[i];
                var vu = token.GetVariableNameAndUnit('[', ']', "-");
                if (vu.Name == "---" || vu.Name == "Check sum") continue;

                string value = valueTokens[i];
                bool isNumeric = value.All(c => char.IsNumber(c) || c == '.');

                if (isNumeric)
                {
                    VariableInfo<double> v = new VariableInfo<double>()
                    {
                        Name = Prefix + vu.Name + Postfix,
                        ColumnInSourceFile = i,
                        TimeColumn = 0,
                        Unit = vu.Unit,
                        Recorder = this
                    };
                    _variables.Add(v);
                }
                else
                {
                    VariableInfo<string> v = new VariableInfo<string>()
                    {
                        Name = Prefix + vu.Name + Postfix,
                        ColumnInSourceFile = i,
                        TimeColumn = 0,
                        Unit = vu.Unit,
                        Recorder = this
                    };
                    _variables.Add(v);
                }
            }
        }

        protected override char readSeparator() => ',';


    }
}
