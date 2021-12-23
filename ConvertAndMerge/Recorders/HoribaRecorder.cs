﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Globalization;
using Paulus.IO;
using Paulus.Common;

namespace ConvertMerge
{
    public class HoribaRecorder : Recorder
    {
        public HoribaRecorder() { }
        public HoribaRecorder(string sourcePath) : base(sourcePath) { }

        public static bool IsHoribaRecorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);

                //return line1.StartsWith("Date,Time,Mean Conc. PCRF Corr.");
                return line1.StartsWith(",,TestSequenceNumber,TestStatus");
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

        protected override char ReadSeparator()
        {
            return ',';
        }

        const string timeFormat = "MM/dd/yyyy HH:mm:ss.fffffff", timeFormat2 = "MM/dd/yyyy HH:mm:ss.ffffff";
        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = ReadSeparator();

            //else retrieve the start time from the file
            string line2 = StreamReaderExtensions.ReadLine(_sourceFilePath, 4);
            string[] tokens = line2.Split(_separator);

            string sDate = tokens[0];


            bool parsedTime = DateTime.TryParseExact(sDate,
                new string[] { timeFormat, timeFormat2 }, EN, System.Globalization.DateTimeStyles.AssumeLocal, out startLocalTime);
            if (parsedTime) StartAbsoluteTime = startLocalTime;
            else
                StartAbsoluteTime = new DateTime();

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

        SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!

        }

        protected override void ReadVariableInfos()
        {
            //StreamReaderExtensions.ReadLines(_sourceFilePath, 1, 2, 3);
            string[] names = StreamReaderExtensions.ReadLine(_sourceFilePath, 1).Split(_separator);
            string[] names2 = StreamReaderExtensions.ReadLine(_sourceFilePath, 3).Split(_separator);

            string[] units = StreamReaderExtensions.ReadLine(_sourceFilePath, 2).Split(_separator).Select(u => u.Trim('[', ']')).ToArray();

            //skip timelocal
            for (int i = 2; i < names.Length; i++)
            {
                //string name = names[i] + "|" + names2[i];
                string name = names[i] == names2[i] ?
                names[i] : $"{names[i]}|{names2[i]}", unit = units[i];


                VariableInfo v = new VariableInfo<double>()
                {
                    Name = Prefix + name + Postfix, //SHOULD BE APPLIED WHEN WRITING TO FILE (this is temporary here)
                    ColumnInSourceFile = i,
                    TimeColumn = 0,
                    Unit = units[i],
                    Recorder = this
                };

                _variables.Add(v);
            }
        }

        DateTime startLocalTime; //without starttime modification

        int iTime = 0;
        protected override bool LoadDataFromLine(string[] tokens, ref int iLine)
        {
            foreach (VariableInfo v in _variables)
            {
                DateTime absoluteTime;

                if (v.TimeColumn > tokens.Length - 1 || v.ColumnInSourceFile > tokens.Length - 1) return false;

                double relativeTimeInSeconds;
                bool parsedTime;
                if (!ForceTimeStep)
                {
                    //bool parsed = double.TryParse(tokens[v.TimeColumn], out relativeTimeInSeconds);
                    parsedTime = DateTime.TryParseExact(tokens[v.TimeColumn],
                        new string[] { timeFormat, timeFormat2 },
                        EN, DateTimeStyles.AssumeLocal, out absoluteTime);

                    relativeTimeInSeconds = (absoluteTime - this.startLocalTime).TotalSeconds;

                    absoluteTime = this.StartAbsoluteTime.AddSeconds(relativeTimeInSeconds);

                    //the measurement date must be correctly set according to the user settings
                    if (MeasurementDate.HasValue)
                        absoluteTime = absoluteTime.SetDate(MeasurementDate.Value); //προσοχή για το όταν αλλάζει η μέρα!


                }
                else
                {
                    relativeTimeInSeconds = (double)iTime;
                    parsedTime = true;
                }


                if (parsedTime)
                {
                    if (v.GetType() == typeof(VariableInfo<double>))
                    {
                        double value;
                        parsedTime = double.TryParse(tokens[v.ColumnInSourceFile], NumberStyles.Any, EN, out value);
                        if (parsedTime) //only if the value can be parsed it is valid to add the time/value pair
                        {
                            //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                            //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                            (v as VariableInfo<double>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                            (v as VariableInfo<double>).ValuesBeforeTimeStepChange.Add(value);
                        }
                    }
                    else if (v.GetType() == typeof(VariableInfo<string>))
                    {
                        //αποθηκεύουμε μόνο την πληροφορία που είναι απαραίτητη για το export
                        //το absolute time δεν είναι απαραίτητο παρά μόνο στο τέλος ως extra μεταβλητή
                        (v as VariableInfo<string>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                        (v as VariableInfo<string>).ValuesBeforeTimeStepChange.Add(tokens[v.ColumnInSourceFile]);
                    }
                }
            }

            if (ForceTimeStep) iTime++;

            return true;
        }
    }


}
