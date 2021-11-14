using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Text.RegularExpressions;
using System.Globalization;
using Paulus.IO;
using Paulus;
using Paulus.Common;

namespace ConvertMerge
{
    public class ThermostarRecorder : Recorder
    {
        public ThermostarRecorder() { }

        public ThermostarRecorder(string sourceFilePath) : base(sourceFilePath) { }

        public static bool IsThermostarRecorder(string filePath)
        {
            string firstLine = StreamReaderExtensions.ReadLine(filePath, 1, Encoding.Default);
            return firstLine.StartsWith("Sourcefile");//&& firstLine.EndsWith(".qmp");
        }

        protected override char readSeparator()
        {
            return '\t';
        }


        protected internal override void ReadStartingTime()
        {
            if (!ReadStartTimeFromXmlRecord())
            {
                string line4 = StreamReaderExtensions.ReadLine(_sourceFilePath, 4);
                int tabPosition = line4.IndexOf('\t');
                string sDate = line4.Substring(tabPosition + 1);


                int lastSpacePosition = sDate.LastIndexOf(' ');
                string withoutPM = sDate.Substring(0, lastSpacePosition);
                char[] last4 = sDate.ToCharArray(sDate.Length - 4, 4);
                bool afterNoon;

                if (sDate.EndsWith("AM") || sDate.EndsWith("PM"))
                    afterNoon = sDate.EndsWith("PM");
                else if (sDate.EndsWith("μμ") || sDate.EndsWith("πμ"))
                    afterNoon = sDate.EndsWith("μμ");
                else
                    afterNoon = last4[0] == last4[2] && last4[1] == last4[3];

                //        System.Globalization.CultureInfo en = System.Globalization.CultureInfo.InvariantCulture;
                //       return DateTime.ParseExact(withoutPM + ' ' + (afterNoon ? "PM" : "AM"), "M/d/yyyy h:mm:ss.FFF tt", en);
                //      System.Globalization.CultureInfo gr = new System.Globalization.CultureInfo("el-GR");
                //      return DateTime.ParseExact(withoutPM + ' ' + (afterNoon ? "μμ" : "πμ"), "M/d/yyyy h:mm:ss.FFF tt", gr);
                //}
                //               string[] formats = new string[] { "d/M/yyyy h:mm:ss.FFF tt", "M/d/yyyy h:mm:ss.FFF tt" };
                //    System.Globalization.CultureInfo gr = new System.Globalization.CultureInfo("el-GR");

                Match m = Regex.Match(sDate, @"(?<d1>\d{1,2})/(?<d2>\d{1,2})/(?<year>\d{4}) (?<hour>\d{1,2}):(?<minutes>\d{2}):(?<seconds>\d{2})\.(?<milliseconds>\d{3})");
                int year = int.Parse(m.Groups["year"].Value);
                int minute = int.Parse(m.Groups["minutes"].Value);
                int second = int.Parse(m.Groups["seconds"].Value);
                int millisecond = int.Parse(m.Groups["milliseconds"].Value);

                int hour = int.Parse(m.Groups["hour"].Value);
                if (afterNoon && hour < 12) hour += 12;

                int month, day;

                //avoid parsing d1 and d2 if measurement data is set (added @05 May 2015)
                if (MeasurementDate.HasValue)
                {
                    year = MeasurementDate.Value.Year;
                    month = MeasurementDate.Value.Month;
                    day = MeasurementDate.Value.Day;
                }
                else
                {
                    int d1 = int.Parse(m.Groups["d1"].Value);
                    int d2 = int.Parse(m.Groups["d2"].Value);
                    FileInfo fI = new FileInfo(_sourceFilePath);
                    bool d1IsMonth = d2 > 12 || fI.LastWriteTime.Month == d1 && fI.LastWriteTime.Day == d2;
                    if (d1IsMonth) { month = d1; day = d2; }
                    else { day = d1; month = d2; }
                }

                StartAbsoluteTime = new DateTime(year, month, day, hour, minute, second, millisecond);

                ////CORRECT THE STARTABSOLUTE TIME IF A CONTINUE FILE IS USED
                //if (_xmlRecord?.HasAttribute("continueFile") ?? false)
                //{
                //    string localPath = _xmlRecord.GetAttributeOrElementText("continueFile");
                //    ContinueOtherFile = localPath;
                //    string absolutePath = Path.Combine(Path.GetDirectoryName(this._sourceFilePath), localPath);
                //    ThermostarRecorder firstRecorder = new ThermostarRecorder(absolutePath);
                //    firstRecorder.ReadStartingTime();
                //    DateTime oldStartAbsoluteTime = firstRecorder.StartAbsoluteTime;
                //    timeOffset = (StartAbsoluteTime.SetDate(oldStartAbsoluteTime) - oldStartAbsoluteTime).TotalSeconds;
                //    StartAbsoluteTime = oldStartAbsoluteTime;
                //}

                //CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;            
            }
            CheckContinueFileRecord<ThermostarRecorder>();


            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        protected override void ReadVariableInfos()
        {
            Dictionary<int, string> lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 7, 8);

            _sourceTimeUnit = "s";

            string speciesNamesLine = lines[7];
            string unitsLine = lines[8];

            string[] speciesNames = speciesNamesLine.Split('\t');
            string[] unitTokens = unitsLine.Split('\t');

            //int variablesCount = unitTokens.Length / 3;

            //build variable names by combining the species name and the unit (% or current)
            int iVariable = 0;
            for (int iToken = 0; iToken < speciesNames.Length; iToken++)
            {
                if (speciesNames[iToken].Length > 0)
                {
                    var vu = unitTokens[iToken].GetVariableNameAndUnit('[', ']', "-");

                    VariableInfo<double> v = new VariableInfo<double>()
                    {
                        Name = Prefix + speciesNames[iToken] + " " + vu.Name + Postfix,
                        ColumnInSourceFile = iVariable * 3 + 2,
                        TimeColumn = iVariable * 3 + 1,
                        Unit = vu.Unit,
                        Recorder = this
                    };
                    iVariable++;
                    _variables.Add(v);
                }
            }
        }


        protected override int linesToOmitBeforeData
        {
            get { return 8; }
        }




    }
}
