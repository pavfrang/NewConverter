using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Globalization;
using Paulus.IO;
using Paulus;
using Paulus.Common;
using System.Diagnostics;

namespace ConvertMerge
{
    public class EEPSRecorder : Recorder
    {
        public EEPSRecorder() { }
        public EEPSRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsEEPSRecorder(string filePath)
        {
            try
            {
                string line5 = StreamReaderExtensions.ReadLine(filePath, 5);
                return line5.StartsWith("Instrument Label");
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
                int iLine = 0;
                using (StreamReader reader = new StreamReader(_sourceFilePath))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine(); iLine++;
                        if (line.StartsWith("\"Elapsed")) break;
                    }
                }

                return iLine;
            }
        }

        protected override char readSeparator()
        {
            return '\t';
        }

        protected internal override void ReadStartingTime()
        {
            //force reading the separator
            _separator = readSeparator();
            _sourceTimeUnit = "s";

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

            string line = StreamReaderExtensions.ReadLine(_sourceFilePath, 2);

            string[] tokens = line.Split(_separator);
            string sDateTime = tokens.Last().Trim('"');

            bool parsedTime = DateTime.TryParse(sDateTime, GR, System.Globalization.DateTimeStyles.AssumeLocal, out starttime);
            if (parsedTime) StartAbsoluteTime = starttime;
            else
                StartAbsoluteTime = new DateTime();
            //string t = StartAbsoluteTime.ToString("dddd, dd MMMM yyyy  hh:mm:ss tt",GR);

            SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!
        }

        class Category
        {
            public int FromColumn, ToColumn; public string Description, Unit;
            public Category(int fromColumn, string description)
            { FromColumn = fromColumn; Description = description; }

            public override string ToString()
            {
                return Description;
            }
        }

        protected override void ReadVariableInfos()
        {
            int l = linesToOmitBeforeData;
            var lines = StreamReaderExtensions.ReadLines(_sourceFilePath, l - 2, l - 1);


            string[] tokens1 = lines[l - 2].Split(_separator);

            List<Category> categories = new List<Category>();
            int lastCategory = tokens1.ToList().FindIndex(t => t.StartsWith("Total Concentration"));

            for (int i = 0; i <= lastCategory; i++)
            {
                if (!string.IsNullOrEmpty(tokens1[i]))
                    categories.Add(new Category(i, tokens1[i]));
            }
            for (int i = 0; i < categories.Count - 1; i++)
                categories[i].ToColumn = categories[i + 1].FromColumn - 2;
            categories[categories.Count - 1].ToColumn = categories[categories.Count - 1].FromColumn + 1;
            //add the unit to the Total Concentration column (bug)
            categories[categories.Count - 1].Description += $" {tokens1[lastCategory + 1]}";

            foreach (Category c in categories)
            {
                var vu = StringExtensions.GetVariableNameAndUnit(c.Description, '[', ']');
                c.Description = vu.Name; c.Unit = vu.Unit; //.Replace("Β", ""); //replace greek Beta;
            }


            //now get all units names (all are diameters except the last category)
            string[] tokens = lines[l - 1].Split(_separator);

            foreach (Category c in categories)
            {
                for (int i = c.FromColumn + 1; i <= c.ToColumn; i++)
                {
                    string token = tokens[i].Trim();
                    double size; bool parsed = double.TryParse(token, out size);
                    if (parsed) token += " nm";

                    VariableInfo v;
                    v = new VariableInfo<double>()
                    {
                        Name = !string.IsNullOrEmpty(token) ?
                        $"{Prefix}EEPS {c.Description} {token}{Postfix}": $"{Prefix}EEPS {c.Description}{Postfix}",
                        ColumnInSourceFile = i,
                        TimeColumn = c.FromColumn,
                        Unit = c.Unit,
                        Recorder = this
                    };
                    _variables.Add(v);
                }

            }

            //for (int i = 1; i < tokens.Length; i++)
            //{
            //    string token = tokens[i];

            //    VariableInfo v;
            //    v = new VariableInfo<double>()
            //    {
            //        Name = Prefix + "EEPS " + token + Postfix,
            //        ColumnInSourceFile = i,
            //        TimeColumn = 0,
            //        Unit = "nm",
            //        Recorder = this
            //    };

            //    _variables.Add(v);
            //}

            ////manually add the total concentration variable
            //VariableInfo vTotal;
            //vTotal = new VariableInfo<double>()
            //{
            //    Name = "EEPS Total Concentration" + Postfix,
            //    ColumnInSourceFile = _variables.Last().ColumnInSourceFile + 3,
            //    TimeColumn = _variables.Last().ColumnInSourceFile + 2,
            //    Unit = "#/cm³",
            //    Recorder = this
            //};
            //_variables.Add(vTotal);
        }

    }


}
