using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Paulus.Collections;

using System.ComponentModel;
using Paulus.Common;

namespace ConvertMerge
{
    public abstract class VariableInfo
    {
        public VariableInfo()
        {
            _relativeTimesInSecondsBeforeTimeStepChange = new List<double>();
            _relativeTimesInSecondsAfterTimeStepChange = new List<double>();
            _absoluteTimesAfterTimeStepChange = new List<DateTime>();
            InterpolationMode = InterpolationMode.Undefined;
        }

        [Category("Data")]
        [DisplayName("Time column")]
        public int TimeColumn {get;set;}
        [Category("Data")]
        [DisplayName("Source column")]
        public int ColumnInSourceFile {get;set;}

        [Category("Data")]
        [DisplayName("Target column")]
        public int ColumnInTargetFile { get; set; }

        [Category("Data")]
        public string Unit {get;set;}

        [Category("Main")]
        [DisplayName("Translated name")]
        public string TranslatedName { get; set; }
        [Category("Main")]
        [DisplayName("Original name")]
        public string Name { get; set; }

        public Recorder Recorder;

        //all relative times are relative to the StartAbsoluteRecorderTime

        public InterpolationMode InterpolationMode { get; set; }

        #region Relative/Absolute times before/after the time step change
        protected double _exportTimeStepInSeconds;
        //set by the ChangeTimeStepTo function

        //the virtual functions MUST case this base function first
        public double ExportTimeStepInSeconds { get { return _exportTimeStepInSeconds; } }

        public virtual void setExportTimeStep(double timeStepInSeconds)
        {
            _exportTimeStepInSeconds = timeStepInSeconds;
        }

        protected List<double> _relativeTimesInSecondsBeforeTimeStepChange;
        public List<double> RelativeTimesInSecondsBeforeTimeStepChange { get { return _relativeTimesInSecondsBeforeTimeStepChange; } }


        protected double _forcedSourceTimeStepOnError;
        public double ForcedSourceTimeStepOnError { get { return _forcedSourceTimeStepOnError; } }

        public void ForceSourceTimeStepOnError(double forcedSourceTimeStepOnError = 1.0)
        {
            if (!_relativeTimesInSecondsBeforeTimeStepChange.IsSortedInIncreasingOrder())
            {
                List<int> wrongIndices = _relativeTimesInSecondsBeforeTimeStepChange.GetIndicesOfNonIncreasingOrder();

                int wrongIndex = wrongIndices[0];
                //foreach(int wrongIndex in wrongIndices)
                Console.WriteLine("First time error for {0} at {1}. Correcting...", Name, wrongIndex);

                _forcedSourceTimeStepOnError = forcedSourceTimeStepOnError;

                double t0 = _relativeTimesInSecondsBeforeTimeStepChange[0];
                for (int i = 1; i < DataCountBeforeTimeStepChange; i++)
                    _relativeTimesInSecondsBeforeTimeStepChange[i] = t0 + i * _forcedSourceTimeStepOnError;
            }
        }


        //should allow it to change in case of string values
        protected List<double> _relativeTimesInSecondsAfterTimeStepChange;
        [Browsable(false)]
        public List<double> RelativeTimesInSecondsAfterTimeStepChange { get { return _relativeTimesInSecondsAfterTimeStepChange; } }
        [Browsable(false)]
        public double StartRelativeTimeAfterTimeStepChange { get { return _relativeTimesInSecondsAfterTimeStepChange[0]; } }
        [Browsable(false)]
        public double EndRelativeTimeAfterTimeStepChange { get { return _relativeTimesInSecondsAfterTimeStepChange[_relativeTimesInSecondsAfterTimeStepChange.Count - 1]; } }

        protected List<DateTime> _absoluteTimesAfterTimeStepChange;
        [Browsable(false)]
        public List<DateTime> AbsoluteTimesAfterTimeStepChange { get { return _absoluteTimesAfterTimeStepChange; } }
        [Browsable(false)]
        public DateTime StartAbsoluteTimeAfterTimeStepChange { get { return _absoluteTimesAfterTimeStepChange[0]; } }
        [Browsable(false)]
        public DateTime EndAbsoluteTimeAfterTimeStepChange { get { return _absoluteTimesAfterTimeStepChange[_absoluteTimesAfterTimeStepChange.Count - 1]; } }

        [Category("Data")]
        [DisplayName("Count")]
        public int DataCountBeforeTimeStepChange { get { return _relativeTimesInSecondsBeforeTimeStepChange.Count; } }
        [Browsable(false)]
        public int DataCountAfterTimeStepChange { get { return _relativeTimesInSecondsAfterTimeStepChange.Count; } }

        [Browsable(false)]
        /// <summary>
        /// Returns True if the source arrays of RelativeTimes and Values are not empty.
        /// </summary>
        public virtual bool ContainsData
        {
            get
            {
                return _relativeTimesInSecondsAfterTimeStepChange != null && _relativeTimesInSecondsAfterTimeStepChange.Count > 0;
            }
        }

        /// <summary>
        /// Returns true if absoluteTime is between (including) the start and end time.
        /// </summary>
        /// <param name="absoluteTime">The time to compare with the start and end time.</param>
        /// <returns></returns>
        public bool HasMeasurementAtTime(DateTime absoluteTime)
        {
            return ContainsData &&
                absoluteTime >= StartAbsoluteTimeAfterTimeStepChange &&
                absoluteTime <= EndAbsoluteTimeAfterTimeStepChange;
        }

        /// <summary>
        /// Returns true if relative time is between (including) the start and end time.
        /// </summary>
        /// <param name="relativeTimeInSeconds"></param>
        /// <returns></returns>
        public bool HasMeasurementAtTime(double relativeTimeInSeconds)
        {
            return ContainsData &&
                relativeTimeInSeconds >= StartRelativeTimeAfterTimeStepChange &&
                relativeTimeInSeconds <= EndRelativeTimeAfterTimeStepChange;
        }
        #endregion

        #region Export to file
        public string GetDefaultExportFilenameWithoutExtension()
        {
            string s = this.ToString();
            return s.RemoveCharacters(Path.GetInvalidFileNameChars());
        }

        public string ExportToDirectory(string directoryPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(directoryPath, GetDefaultExportFilenameWithoutExtension() + ".txt"));
            ExportToFile(fullPath);
            return fullPath;
        }

        public abstract void ExportToFile(string filePath);
        #endregion

        public abstract dynamic this[int i] { get; }

        public override string ToString()
        {
            return (TranslatedName ?? Name) + " [" + (string.IsNullOrWhiteSpace(Unit) ? "-" : Unit) + "]";
        }

    }

    public class VariableInfo<T> : VariableInfo
    {
        public VariableInfo() { _valuesBeforeTimeStepChange = new List<T>(); }

        /// <summary>
        /// Returns the ith value after the time step change.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public override dynamic this[int i]
        {
            get { return _valuesAfterTimeStepChange[i]; }
        }

        private List<T> _valuesBeforeTimeStepChange;
        public List<T> ValuesBeforeTimeStepChange { get { return _valuesBeforeTimeStepChange; } }

        public override bool ContainsData
        {
            get
            {
                return base.ContainsData &&
                    _valuesBeforeTimeStepChange != null && _valuesBeforeTimeStepChange.Count > 0;
            }
        }

        private List<T> _valuesAfterTimeStepChange;
        public List<T> ValuesAfterTimeStepChange { get { return _valuesAfterTimeStepChange; } }


        public override void setExportTimeStep(double exportTimeStepInSeconds)
        {
            base.setExportTimeStep(exportTimeStepInSeconds);

            //align absolute time at the next integer second (an option should be given depending on the timestep)
            double targetStartTime = 1 - (double)Recorder.StartAbsoluteTime.Millisecond / 1000.0;

            //CHECK IF EMPTY VALUES
            if (_relativeTimesInSecondsBeforeTimeStepChange.Count > 0)
            {
                ////TEMP THIS
                ////double tmp=0.0;
                ////InterpolationMode = typeof(T) == tmp.GetType() ? NewConverter.InterpolationMode.Linear : NewConverter.InterpolationMode.Nearest;
                //if (typeof(T) == typeof(string) ) InterpolationMode=InterpolationMode.Nearest;

                TimeStepChanger<T> changer =
                    new TimeStepChanger<T>(targetStartTime, exportTimeStepInSeconds, InterpolationMode, _relativeTimesInSecondsBeforeTimeStepChange, _valuesBeforeTimeStepChange, false);

                changer.ExportWithNewTimeStep();

                _relativeTimesInSecondsAfterTimeStepChange = changer.ExportedTimes.ToList();
                _valuesAfterTimeStepChange = changer.ExportedValues.ToList();

                //fill absolute times too
                _absoluteTimesAfterTimeStepChange = new List<DateTime>(changer.ExportedTimes.Length);
                for (int i = 0; i < _relativeTimesInSecondsAfterTimeStepChange.Count; i++)
                    _absoluteTimesAfterTimeStepChange.Add(Recorder.StartAbsoluteTime.AddSeconds(_relativeTimesInSecondsAfterTimeStepChange[i]));
            }
        }

        public T GetValueAtRelativeTimeAfterTimeStepChange(double relativeTimeInSeconds)
        {
            int index = _relativeTimesInSecondsAfterTimeStepChange.IndexOf(relativeTimeInSeconds);
            //return the value or the default(T)
            if (index == -1) return default(T);
            else
                return _valuesAfterTimeStepChange[index];
        }

        public T GetValueAtAbsoluteTimeAfterTimeStepChange(DateTime absoluteTimeInSeconds)
        {
            int index = _absoluteTimesAfterTimeStepChange.IndexOf(absoluteTimeInSeconds);
            if (index == -1) return default(T);
            else return _valuesAfterTimeStepChange[index];
        }

        public override void ExportToFile(string path)
        {

            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine("Time [s]\tAbsolute Time\t" + this);

                int cnt = _relativeTimesInSecondsAfterTimeStepChange.Count; // this.DataCountAfterTimeStepChange;
                for (int i = 0; i < cnt; i++)
                {
                    writer.Write(ExperimentManager.DateTimeRecordFormat, _relativeTimesInSecondsAfterTimeStepChange[i], _absoluteTimesAfterTimeStepChange[i]);
                    writer.WriteLine("{0}", _valuesAfterTimeStepChange[i]);
                }
            }
        }
    }
}
