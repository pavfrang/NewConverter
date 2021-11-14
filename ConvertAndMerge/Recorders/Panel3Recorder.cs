using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Globalization;
using Paulus.IO;
using Paulus.Common;

namespace ConvertMerge
{
    public class Panel3Recorder : Recorder
    {
        public Panel3Recorder() { }
        public Panel3Recorder(string sourcePath) : base(sourcePath) { }

        public static bool IsPanel3Recorder(string filePath)
        {
            try
            {
                string line1 = StreamReaderExtensions.ReadLine(filePath, 1);

                return line1.StartsWith("Time[msec],") ||
                    line1.StartsWith("Time[sec],");
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
                return 1;
            }
        }

        protected override char readSeparator()
        {
            return ',';
        }

        public string TimeFilePath { get; set; }

        protected internal override void ReadStartingTime()
        {
            //ensure that the separator is read
            _separator = readSeparator();

            if (_xmlRecord != null)
            {
                DateTime? startTime = _xmlRecord.GetAttributeOrElementDateTime("starttime", "HH:mm:ss");
                if (startTime.HasValue)
                {
                    StartAbsoluteTime = startTime.Value;
                    //the sDate variable needs to be retrieved first before going inside here

                    //CORRECT THE STARTABSOLUTE TIME IF A CONTINUE FILE IS USED
                    if (_xmlRecord?.HasAttribute("continueFile") ?? false)
                    {
                        string localPath = _xmlRecord.GetAttributeOrElementText("continueFile");
                        ContinueOtherFile = localPath;
                        string absolutePath = Path.Combine(Path.GetDirectoryName(this._sourceFilePath), localPath);

                        Panel3Recorder firstRecorder =
                            _parent.Recorders.Where(r => Panel3Recorder.IsPanel3Recorder(r.SourceFilePath)).
                            Cast<Panel3Recorder>().First(r => r.SourceFilePath == absolutePath);
                        //Panel3Recorder firstRecorder = new Panel3Recorder(absolutePath);
                        //firstRecorder.ReadStartingTime();


                        DateTime oldStartAbsoluteTime = firstRecorder.StartAbsoluteTime;

                        timeOffset = (StartAbsoluteTime.SetDate(oldStartAbsoluteTime.Date)
                            - oldStartAbsoluteTime).TotalSeconds;
                        StartAbsoluteTime = oldStartAbsoluteTime;
                    }


                    goto SetMeasurementDate;
                }
                else
                {
                    string timefile = _xmlRecord.GetAttributeOrElementText("timefile");

                    //the file is retrieved from puma time
                    this.TimeFilePath = PathExtensions.IsPathAbsolute(timefile) ? timefile :
                        Path.Combine(Path.GetDirectoryName(_sourceFilePath), timefile);

                    if (!File.Exists(TimeFilePath)) throw new FileNotFoundException("Cannot find time file.", TimeFilePath);

                    //read the puma recorder time
                    PUMARecorder pumaRecorder = new PUMARecorder(TimeFilePath);
                    pumaRecorder.ReadStartingTime();
                    this.StartAbsoluteTime = pumaRecorder.StartAbsoluteTime;
                    pumaRecorder = null;

                    //continus to SetMeasurementDate
                }
            }

        SetMeasurementDate:
            if (MeasurementDate.HasValue)
                StartAbsoluteTime = StartAbsoluteTime.SetDate(MeasurementDate.Value);
        }

        protected override void ReadVariableInfos()
        {
            string[] names = StreamReaderExtensions.ReadLine(_sourceFilePath, 1).Split(_separator);

            // return line1.StartsWith("Time[msec],") || line1.StartsWith("Time[sec],");
            if (names[0].Contains("[msec]"))
                _sourceTimeUnit = "ms";
            else
                _sourceTimeUnit = "s";


            for (int i = 1; i < names.Length; i++)
            {
                string name = names[i];

                VariableInfo v;

                if (name != "TriggerData")
                    v =
                    new VariableInfo<double>()
                    {
                        Name = Prefix + name + Postfix,
                        ColumnInSourceFile = i,
                        TimeColumn = 0,
                        Unit = "-",
                        Recorder = this
                    };
                else v =
                    new VariableInfo<string>()
                    {
                        Name = Prefix + name + Postfix,
                        ColumnInSourceFile = i,
                        TimeColumn = 0,
                        Unit = "-",
                        Recorder = this
                    };

                _variables.Add(v);
            }
        }


    }


}
