using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
using Paulus.IO;
using Paulus.Common;

namespace ConvertMerge
{
    public class PUMA2Recorder : Recorder
    {
        public PUMA2Recorder(string sourceFilePath) : base(sourceFilePath) { }

        public static bool IsPUMA2Recorder(string filePath)
        {
            string line1 = StreamReaderExtensions.ReadLine(filePath, 1);
            return line1=="Time\t$iStartTime\t";
        }

        protected internal override void ReadStartingTime()
        {
            string timeLine = StreamReaderExtensions.ReadLine(base._sourceFilePath, 3).Trim('\t');

            //eg:20091015141633 (PUMA1
            //2015/12/10 23:06:49	(PUMA2)

            StartAbsoluteTime = DateTime.ParseExact(timeLine, @"yyyy/MM/dd HH:mm:ss",CultureInfo.InvariantCulture);

            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        protected override void ReadVariableInfos()
        {
            //PUMA(1) uses lines 1 and 2
            //PUMA2 uses lines 5 and 6
            Dictionary<int, string> lines = StreamReaderExtensions.ReadLines(_sourceFilePath, 5, 6);


            //Time	AIRFLHUB	AIRFLOW	ALPHA_d	EGRPOSIT	INJTOT2	MFC2FLAN	MFC2FLTN	MFC3FLAN	MFC3FLTN	P_CHAN1	P_CHAN3	P_CHAN4	SPEED	SPEEDECU	T_CHAN1	T_CHAN2	T_CHAN5	T_CHAN6	T_CHAN7	T_CHAN8	T_CHAN9	T_CHAN10	T_CHAN21	T_CHAN22	T_CHAN23	T_CHAN24	T_CHAN29	TCOOLECU	TORQUE	TOTINJS	TVPOSIT	TWO	$iStartTime	
            string variableNamesLine = lines[5].TrimEnd('\t');
            //the last units may be empty so avoid trim
            string unitsLine = lines[6];

            string[] variableNameTokens = variableNamesLine.Split('\t');
            string[] unitTokens = unitsLine.Split('\t');

            _sourceTimeUnit = unitTokens[0];

            int count = variableNameTokens.Length; //PUMA (1) has variableNametTokens.Length - 1

            //remove the first token only (Time)
            for (int i = 1; i < count; i++)
            {
                VariableInfo v;

                string name = variableNameTokens[i];

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
                        Name =Prefix + name + Postfix,
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
            get { return 6; }
        }

        protected override char ReadSeparator()
        {
            return '\t';
        }


    }
}
