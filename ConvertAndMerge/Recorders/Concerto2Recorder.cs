using Paulus.Common;
using Paulus.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using static Paulus.IO.StreamReaderExtensions;

namespace ConvertMerge
{
    public class Concerto2Recorder : Recorder
    {
        public Concerto2Recorder(string sourceFilePath) : base(sourceFilePath)
        {    }

        protected override int linesToOmitBeforeData => 2;

        protected override char ReadSeparator() => '\t';

        public static bool IsConcerto2Recorder(string filePath)
        {
            return StreamReaderExtensions.ReadLine(filePath, 1)
                 .StartsWith("Absolute_Time\tTime\t");
        }

        protected override void ReadVariableInfos()
        {
            _separator =  ReadSeparator();

            var lines = ReadLines(_sourceFilePath, 1, 2);

            string[] names = lines[1].Trim().Split(_separator);
            string[] units = lines[2].Trim().Split(_separator);

            for (int i = 2; i < names.Length; i++)
            {
                string name = names[i], unit = units[i];

                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + name + Postfix, //SHOULD BE APPLIED WHEN WRITING TO FILE (this is temporary here)
                    ColumnInSourceFile = i,
                    TimeColumn = 1,
                    Unit = unit,
                    Recorder = this
                };

                _variables.Add(v);
            }
        }

        protected internal override void ReadStartingTime()
        {
               _sourceTimeUnit = "s";
        
            string line = ReadLine(_sourceFilePath, 3);


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

            string[] tokens = line.Split(_separator);
            string sTime = tokens[0];
            bool parsedTime = DateTime.TryParseExact(sTime, "yyyy-MM-dd HH:mm:ss.fff", EN, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();

            SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!

        }

   
    }
}
