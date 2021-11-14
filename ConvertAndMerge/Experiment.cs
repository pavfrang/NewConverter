using System;
using System.Collections.Generic;
using System.Linq;

using System.IO;
using System.Xml;
using Paulus.IO;
using Paulus.Collections;

namespace ConvertMerge
{
    public class Experiment : VariableCollection
    {
        public Experiment() { _recorders = new List<Recorder>(); }

        private List<Recorder> _recorders;
        public List<Recorder> Recorders { get { return _recorders; } }

        public DateTime? MeasurementDate { get; set; }
        public string SourceDirectory { get; set; }

        protected ExperimentManager _parent;
        public ExperimentManager Parent { get { return _parent; } }

        public override void Merge()
        {
            //export separate files if selected
            foreach (Recorder r in _recorders) r.Merge();

            _variables.AddRangeLists(_recorders);

            if (_parent.ReorderVariables 
                && _parent.xmlVariables != null
                && _parent.xmlVariables.Count>0)
                base.reorderVariables(_parent.xmlVariables);

            base.Merge();
        }

        public static Experiment CreateFromXML(XmlElement experiment, ExperimentManager em)
        {
            Experiment exp = new Experiment();
            exp._parent = em;

            #region Inheritable (optional) properties (export, timestep, mergemode)
            bool? export = experiment.GetAttributeOrElementBool("export");
            exp.ShouldSaveToFile = export.HasValue ? export.Value : ExperimentManager.DefaultShouldExportExperiment;

            //10-July-2020 (added puma export support)
            bool? exportToPuma = experiment.GetAttributeOrElementBool("puma_export");
            exp.ShouldSaveToPumaFile = exportToPuma.HasValue ? exportToPuma.Value : ExperimentManager.DefaultExportToPuma;

            //27-Jul-2021
            bool? reserveColumns = experiment.GetAttributeOrElementBool("reserve_columns");
            exp.ShouldReserveColumns = reserveColumns.HasValue ? reserveColumns.Value : ExperimentManager.DefaultReserveColumns;

            exp.ExportTimeStep = experiment.GetAttributeOrElementDouble("timestep",ExperimentManager.DefaultExperimentTimeStepInSeconds).Value;

            SyncModes mergeMode = XmlSettings.getMergeMode(experiment);
            exp.SyncMode = mergeMode != SyncModes.InvalidOrMissing ? mergeMode : ExperimentManager.DefaultExperimentMergeMode;
            #endregion

            #region Main attributes: source_directory, target and date
            string source_directory = XmlExtensions.GetAttributeOrElementText(experiment, "source_directory");
            if (source_directory != null)
                exp.SourceDirectory =PathExtensions.IsPathAbsolute(source_directory) ? source_directory : Path.GetFullPath(Path.Combine(em.BaseDirectory, source_directory));
            else
                exp.SourceDirectory = em.BaseDirectory;

            //set the file path
            //the target is combined with the source_directory if it is relative
            string target = XmlExtensions.GetAttributeOrElementText(experiment, "target");
            if (target != null) exp.MergeFilePath =PathExtensions.IsPathAbsolute(target) ? target : Path.GetFullPath(Path.Combine(exp.SourceDirectory, target));

            //set the date
            DateTime? date = experiment.GetAttributeOrElementDateTime("date", new string[] { "dd-MM-yyyy", "yyyy-MM-dd" });
            if (date.HasValue)
            {
                exp.MeasurementDate = date.Value;
                //if the target is missing the find the name from the date
                if (target == null)
                {
                    int iExperiment = experiment.ParentNode.GetChildNodePosition(experiment, 1);
                    exp.MergeFilePath = getMergeFilePathFromDate(exp, iExperiment);
                }
            } //else look below after reading the recorders


            //if the date and target are missing they MUST be assumed from the date of at least one recorder
            #endregion

            #region Read recorders
            XmlNodeList list = experiment.GetElementsByTagName("recorder");
            foreach (XmlElement recorder in list)
            {
                Recorder r = Recorder.CreateFromXML(recorder, exp, em);
                if (r != null) exp.Recorders.Add(r);
            }

            #endregion

            if (date.HasValue)
            {
                List<DateTime> startTimes = new List<DateTime>();
                foreach (Recorder r in exp.Recorders)
                {
                    
                    r.ReadStartingTime();
                    startTimes.Add(r.StartAbsoluteTime);
                }

                if (!startTimes.All(t => t.Date == exp.Recorders[0].StartAbsoluteTime.Date))
                    Console.WriteLine("WARNING: Not all files are created the same date.");

                exp.MeasurementDate = startTimes.Min().Date; //new FileInfo(exp.Recorders[0].SourceFilePath).LastWriteTime;

                //FORCE THE MEASUREMENT DATE
                foreach (Recorder r in exp.Recorders)
                    r.MeasurementDate = exp.MeasurementDate;

                int iExperiment = experiment.ParentNode.GetChildNodePosition(experiment, 1);
                while (exp.MergeFilePath == null || File.Exists(exp.MergeFilePath))
                {

                    exp.MergeFilePath = getMergeFilePathFromDate(exp, iExperiment);
                    iExperiment++;
                }
            }

            return exp;
        }

        //called by readExperiment
        private static string getMergeFilePathFromDate(Experiment exp, int iExperiment)
        {
            return Path.Combine(exp.SourceDirectory,
                //string.Format("{0:yyyy-MM-dd}_{1}.txt", exp.MeasurementDate, iExperiment));
                string.Format("R_{0}_{1:dd-MM-yy}.txt", iExperiment, exp.MeasurementDate));
        }

    }
}
