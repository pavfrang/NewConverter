using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Paulus.IO;
using Paulus.Common;

namespace ConvertMerge
{
    public class PUMARecorder : Recorder
    {
        public PUMARecorder() { }
        public PUMARecorder(string sourceFilePath) : base(sourceFilePath) { }

        public static bool IsPUMARecorder(string filePath)
        {
            string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
            return line1.Contains("$iStartTime") && line1.Length > 30;
        }

        protected internal override void ReadStartingTime()
        {
            if(!ReadStartTimeFromXmlRecord())
            {
                string variablesLine = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);

                int startTimeLocation = variablesLine.Split(new char[] { '\t' }).ToList().IndexOf("$iStartTime");

                string firstRecordLine = StreamReaderExtensions.ReadLine(base._sourceFilePath, 3);

                //get all values
                string[] sNumbers = firstRecordLine.Split(new char[] { '\t' }, StringSplitOptions.None);

                string sRecordDateTime = sNumbers[startTimeLocation];

                if (sRecordDateTime.Contains("/"))
                {
                    //2016/04/05 10:18:36
                    StartAbsoluteTime = DateTime.ParseExact(sRecordDateTime, @"yyyy/MM/dd HH:mm:ss", EN);
                }
                else //eg:20091015141633
                {
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
                }
            }
            CheckContinueFileRecord<PUMARecorder>();

            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        protected override void ReadVariableInfos()
        {
            Dictionary<int, string> lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 1, 2);


            //Time	AIRFLHUB	AIRFLOW	ALPHA_d	EGRPOSIT	INJTOT2	MFC2FLAN	MFC2FLTN	MFC3FLAN	MFC3FLTN	P_CHAN1	P_CHAN3	P_CHAN4	SPEED	SPEEDECU	T_CHAN1	T_CHAN2	T_CHAN5	T_CHAN6	T_CHAN7	T_CHAN8	T_CHAN9	T_CHAN10	T_CHAN21	T_CHAN22	T_CHAN23	T_CHAN24	T_CHAN29	TCOOLECU	TORQUE	TOTINJS	TVPOSIT	TWO	$iStartTime	
            string variableNamesLine = lines[1].TrimEnd('\t');


            //the last units may be empty so avoid trim
            string unitsLine = lines[2];

            string[] variableNameTokens = variableNamesLine.Split('\t');
            string[] unitTokens = unitsLine.Split('\t');

            _sourceTimeUnit = unitTokens[0];

            //omit last column
            int count = variableNameTokens.Length; // - 1;

            //remove the first and the last token (Time and $iStartTime)
            for (int i = 1; i < count; i++)
            {
                VariableInfo v;

                string name = variableNameTokens[i];

                //omit the variable if it is a $iStartTime
                if (name == "$iStartTime" || name=="Absolute_Time") continue;

                if (xmlVariables != null &&
                    xmlVariables.ContainsKey(name) &&
                    xmlVariables[name].Type == typeof(string))
                {
                    v = new VariableInfo<string>()
                    {
                        Name = Prefix + name + Postfix,
                        ColumnInSourceFile = i,
                        TimeColumn = 0,
                        Unit = unitTokens[i],
                        Recorder = this
                    };
                }
                else
                //if (xmlVariables[name].Type == typeof(double))
                {
                    v = new VariableInfo<double>()
                    {
                        Name = Prefix + name + Postfix,
                        ColumnInSourceFile = i,
                        TimeColumn = 0,
                        Unit = unitTokens[i],
                        Recorder = this
                    };
                }


                if (v.Unit == "[ - ]" || string.IsNullOrWhiteSpace(v.Unit)) v.Unit = "-";

                _variables.Add(v);

            }

        }

        protected override int linesToOmitBeforeData
        {
            get { return 2; }
        }

        protected override char readSeparator()
        {
            return '\t';
        }

        protected override bool loadDataFromLine(string[] tokens, ref int iLine)
        {
            return base.loadDataFromLine(tokens, ref iLine);
        }


    }
}
