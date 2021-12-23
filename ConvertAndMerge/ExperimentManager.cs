using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using ConvertMerge.Properties;
using Paulus.IO;

namespace ConvertMerge
{
    public enum ProgramEventID : int
    {
        Undefined,
        SettingCultureToUS = 1,
    }

    public enum ExperimentManagerID : int
    {
        CreatingExperimentManager = 100,
        InitializingExperimentList,
        CheckingConfigurationPath,
        ConfigurationPathNotFound,
        ConfigurationPathFound,
        LoadingXMLFile,
        LoadingGlobalSettingsFromXML,
        LoadingExperimentsFromXML
    }

    public class ExperimentManager : IEnumerable<Experiment>
    {
        static TraceSource ts = new TraceSource("ExperimentManager", SourceLevels.All);

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ExperimentManager class.
        /// </summary>
        public ExperimentManager()
        {
            //ts.TraceEvent(TraceEventType.Verbose, (int)ExperimentManagerID.InitializingExperimentList, "Initializing internal experiment list.");
            _experiments = new List<Experiment>();
        }

        //The check should be more detailed.
        public static bool IsXmlConfigurationFile(string path)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                return doc.DocumentElement.Name == "merger";
            }
            catch
            {
                return false;
            }

        }

        public static ExperimentManager CreateFromXMLConfigurationFile(string xmlFilePath)
        {
            ExperimentManager e = new ExperimentManager();
            e.ReadXMLConfigurationFile(xmlFilePath);
            return e;
        }
        public static ExperimentManager CreateFromRecorderFiles(string[] recorderFiles)
        {
            ExperimentManager e = new ExperimentManager();
            e.ReadRecorderFiles(recorderFiles);
            return e;
        }

        #endregion

        #region Experiments management
        private List<Experiment> _experiments;
        /// <summary>
        /// Gets the list of experiments. Each experiment is a collection of recorders.
        /// </summary>
        public List<Experiment> Experiments { get { return _experiments; } }

        /// <summary>
        /// Loads the experiments' recorders' data.
        /// </summary>
        public void LoadDataFromSource(bool omitNonExportedExperiments = true)
        {
            foreach (Experiment exp in _experiments)
            {
                if (omitNonExportedExperiments && !exp.ShouldSaveToFile) continue;

                foreach (Recorder r in exp.Recorders)
                {
                    Console.WriteLine("Reading data of {0}...", Path.GetFileName(r.SourceFilePath));
                    r.LoadDataFromSource(xmlVariables);
                }

                //------------------------

                //updateContinuedFiles(exp);
                updateContinuedFiles<ThermostarRecorder>(exp);
                updateContinuedFiles<Panel3Recorder>(exp);
                updateContinuedFiles<PUMARecorder>(exp);
                updateContinuedFiles<SemsRecorder>(exp);
                updateContinuedFiles<EEPSRecorder>(exp);
                updateContinuedFiles<IlsRecorder>(exp);

                //------------------------------
            }
        }

        private static void updateContinuedFiles<T>(Experiment exp) where T : Recorder
        {
            //get recorders with continue file
            var continueFileRecorders = exp.Recorders.Where(r => r is T).Cast<T>().Where(r => r.ContinueOtherFile != null);
            foreach (T tr in continueFileRecorders)
            {
                T rParent = (T)exp.Recorders.First(
                    r => Path.GetFileName(r.SourceFilePath) == Path.GetFileName(tr.ContinueOtherFile));

                //var doublevs = rParent.Variables.Where(v => v is VariableInfo<double>).Cast<VariableInfo<double>>();
                for (int iv = 0; iv < rParent.Variables.Count; iv++)
                {
                    //(v as VariableInfo<double>).RelativeTimesInSecondsBeforeTimeStepChange.Add(relativeTimeInSeconds);
                    //(v as VariableInfo<double>).ValuesBeforeTimeStepChange.Add(value);

                    rParent.Variables[iv].RelativeTimesInSecondsBeforeTimeStepChange.AddRange(
                      tr.Variables[iv].RelativeTimesInSecondsBeforeTimeStepChange);

                    if (rParent.Variables[iv] is VariableInfo<double>)
                        (rParent.Variables[iv] as VariableInfo<double>).ValuesBeforeTimeStepChange.AddRange(
                            (tr.Variables[iv] as VariableInfo<double>).ValuesBeforeTimeStepChange);
                    else if (rParent.Variables[iv] is VariableInfo<string>)
                        (rParent.Variables[iv] as VariableInfo<string>).ValuesBeforeTimeStepChange.AddRange(
                            (tr.Variables[iv] as VariableInfo<string>).ValuesBeforeTimeStepChange);
                    else if (rParent.Variables[iv] is VariableInfo<int>)
                        (rParent.Variables[iv] as VariableInfo<int>).ValuesBeforeTimeStepChange.AddRange(
                            (tr.Variables[iv] as VariableInfo<int>).ValuesBeforeTimeStepChange);


                }
            }

            //remove secondary thermostar files
            for (int ir = exp.Recorders.Count - 1; ir >= 0; ir--)
            {
                if (continueFileRecorders?.Contains(exp.Recorders[ir]) ?? false)
                    exp.Recorders.RemoveAt(ir);
            }
        }

        public void MergeExperiments(bool omitNonExportedExperiments = true)
        {
            foreach (Experiment exp in _experiments)
            {
                if (omitNonExportedExperiments && !exp.ShouldSaveToFile) continue;

                Console.WriteLine("Merging data of {0}...", Path.GetFileName(exp.MergeFilePath));
                exp.Merge();
            }
        }

        public void SaveToExcelFiles(bool omitNonExportedExperiments = true)
        {
            foreach (Experiment exp in _experiments)
            {
                if (omitNonExportedExperiments && !exp.ShouldSaveToFile) continue;

                Console.WriteLine("Saving excel file {0}...", Path.GetFileName(exp.XlsFilePath));
                exp.SaveToExcelFile();
            }
        }
        #endregion

        #region Configuration XML file

        #region Global settings (XML: <settings><global>)
        //set by readGlobalSettings
        private static string _dateTimeFormat;
        private static string DateTimeFormat
        {
            get { return _dateTimeFormat; }
            set
            {
                _dateTimeFormat = value;
                _dateTimeRecordFormat = "{0}\t{1:" + value + "}\t";
            }
        }

        private static string _dateTimeRecordFormat;
        public static string DateTimeRecordFormat { get { return _dateTimeRecordFormat; } }

        public string BaseDirectory;

        public bool ReorderVariables;
        #endregion

        #region Default values (XML: <defaults>)
        public static double DefaultExperimentTimeStepInSeconds;
        public static bool DefaultShouldExportExperiment;
        public static bool DefaultExportToPuma;
        public static bool DefaultReserveColumns;
        public static bool DefaultShouldExportRecorder;
        public static SyncModes DefaultExperimentMergeMode;
        public static SyncModes DefaultRecorderMergeMode;
        public static InterpolationMode DefaultInterpolationMode;
        public static bool DefaultReorderVariables;
        #endregion


        #region Settings path
        private string _settingsPath, _settingsDirectory;
        public string SettingsPath
        {
            get { return _settingsPath; }
            set
            {
                // ts.TraceEvent(TraceEventType.Verbose, (int)ExperimentManagerID.CheckingConfigurationPath, "Checking if the internal configuration file path exists.", value);

                if (!File.Exists(value))
                {
                    // ts.TraceEvent(TraceEventType.Critical, (int)ExperimentManagerID.ConfigurationPathNotFound, "The configuration file does not exist.", value);
                    throw new FileNotFoundException("File not found.", value);
                }
                else
                {
                    //   ts.TraceEvent(TraceEventType.Information, (int)ExperimentManagerID.ConfigurationPathFound, "The configuration file exists.", value);
                    _settingsPath = value;
                    _settingsDirectory = Path.GetDirectoryName(_settingsPath);
                }
            }
        }
        #endregion

        //set by readGlobalSettingsAndDefaults
        private bool _areGlobalSettingsAndDefaultsRead;
        public bool AreGlobalSettingsAndDefaultsRead { get { return _areGlobalSettingsAndDefaultsRead; } }

        public void ReadRecorderFiles(string[] recorderFiles)
        {
            readExperimentsFromFilepaths(recorderFiles);
        }

        public void SetDefaultSettings()
        {
            Settings s = Settings.Default;
            //global settings
            DateTimeFormat = s.AbsoluteTimeFormat;
            BaseDirectory = s.BaseDirectory;

            //experiment default settings
            DefaultShouldExportExperiment = s.ShouldExportExperiment;
            DefaultExperimentTimeStepInSeconds = s.ExperimentTimeStepInSeconds;
            DefaultExperimentMergeMode = (SyncModes)Enum.Parse(typeof(SyncModes), s.ExperimentMergeMode);
            DefaultReorderVariables = s.ReorderVariables;


            //recorder default settings
            DefaultShouldExportRecorder = s.ShouldExportRecorder;
            DefaultRecorderMergeMode = (SyncModes)Enum.Parse(typeof(SyncModes), s.RecorderMergeMode);

            _areGlobalSettingsAndDefaultsRead = true;

        }


        protected string[] recorderFiles;

        private void readExperimentsFromFilepaths(string[] recorderFiles)
        {
            this.recorderFiles = recorderFiles;
            //create a temp experiment that will hold ALL the recorder files
            //the separation will come after


            string xmlTemplate = Resources.XMLSettingsTemplate;
            StringBuilder sb = new StringBuilder("    <experiment>");
            foreach (string recorderFile in recorderFiles)
                sb.AppendFormat("  <recorder source=\"{0}\"/>\r\n", recorderFile);
            sb.AppendLine("    </experiment>");
            string xmlExperiment = xmlTemplate.Replace("{{experiments}}", sb.ToString());

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlExperiment);

            ReadXMLConfigurationFile(doc);
        }

        public void ReadXMLConfigurationFile(string settingsPath)
        {
            //try
            //{
            this.SettingsPath = settingsPath;

            XmlDocument doc = new XmlDocument();
            doc.Load(settingsPath);

            ReadXMLConfigurationFile(doc);
            //}
            //catch { throw; }
        }

        public void ReadXMLConfigurationFile(XmlDocument doc)
        {
            //ts.TraceEvent(TraceEventType.Information, (int)ExperimentManagerID.LoadingXMLFile, "Loading XML file...");

            readGlobalSettingsAndDefaultsFromXml(doc);

            readVariableTranslationsFromXml(doc);

            readExperimentsFromXml(doc);
        }

        //called by ReadConfigurationFile
        private void readGlobalSettingsAndDefaultsFromXml(XmlDocument doc)
        {
            //ts.TraceEvent(TraceEventType.Information, (int)ExperimentManagerID.LoadingGlobalSettingsFromXML, "Loading global settings from XML file...");

            XmlElement merger = doc["merger"];

            //if empty then attempt to find an import xml
            string baseFilePath = _settingsPath ?? recorderFiles[0];
            XmlElement settings = merger.GetSettingsElementFromRootOrImport(baseFilePath, "settings");

            readSettings(settings);

            _areGlobalSettingsAndDefaultsRead = true;
        }

        private void readSettings(XmlElement settings)
        {
            XmlElement global = settings["global"];
            XmlElement experiment_defaults = settings["experiment_defaults"];
            XmlElement recorder_defaults = settings["recorder_defaults"];

            #region Global settings

            string timeFormat = null;
            if (global != null) timeFormat = global.GetAttributeOrElementText("timeformat");
            DateTimeFormat = timeFormat ?? Settings.Default.AbsoluteTimeFormat;

            string base_directory = null;
            if (global != null) base_directory = global.GetAttributeOrElementText("base_directory");
            BaseDirectory = base_directory ?? Settings.Default.BaseDirectory;
            string fileDirectory = _settingsDirectory ?? Path.GetDirectoryName(recorderFiles[0]);
            if (!PathExtensions.IsPathAbsolute(BaseDirectory)) BaseDirectory = Path.GetFullPath(Path.Combine(fileDirectory, BaseDirectory));

            bool? reorderVariables = null;
            if (global != null) reorderVariables = global.GetAttributeOrElementBool("reorder_variables");
            ReorderVariables = reorderVariables ?? Settings.Default.ReorderVariables;
            #endregion

            #region Experiment defaults
            //timestep
            double? timeStep = experiment_defaults.GetAttributeOrElementDouble("timestep");
            DefaultExperimentTimeStepInSeconds = timeStep.HasValue ? timeStep.Value : Settings.Default.ExperimentTimeStepInSeconds;

            //should export
            bool? export = experiment_defaults.GetAttributeOrElementBool("export");
            DefaultShouldExportExperiment = export.HasValue ? export.Value : Settings.Default.ShouldExportExperiment;

            //10-Jul-2020
            bool? exportToPuma = experiment_defaults.GetAttributeOrElementBool("puma_export");
            DefaultExportToPuma = exportToPuma.HasValue ? exportToPuma.Value : Settings.Default.ExportToPuma;

            //27-Jul-2020
            bool? reserveColumns = experiment_defaults.GetAttributeOrElementBool("reserve_columns");
            DefaultReserveColumns = reserveColumns.HasValue ? reserveColumns.Value : Settings.Default.ReserveColumns;

            //merge mode
            SyncModes mergeMode = XmlSettings.getMergeMode(experiment_defaults);
            DefaultExperimentMergeMode = mergeMode != SyncModes.InvalidOrMissing ? mergeMode :
                (SyncModes)Enum.Parse(typeof(SyncModes), Settings.Default.ExperimentMergeMode);
            #endregion

            #region Recorder defaults
            //should export
            export = recorder_defaults.GetAttributeOrElementBool("export");
            DefaultShouldExportRecorder = export.HasValue ? export.Value : Settings.Default.ShouldExportRecorder;

            //default interpolation mode (default is nearest if missing)
            DefaultInterpolationMode = recorder_defaults.GetAttributeOrElementCustom("interpolation",
                Interpolator.InterpolationDictionary,
                (InterpolationMode)Enum.Parse(typeof(InterpolationMode), Settings.Default.InterpolationMode));

            //merge mode
            mergeMode = XmlSettings.getMergeMode(recorder_defaults);
            DefaultRecorderMergeMode = mergeMode != SyncModes.InvalidOrMissing ? mergeMode :
                (SyncModes)Enum.Parse(typeof(SyncModes), Settings.Default.RecorderMergeMode);

            #endregion

        }

        public Dictionary<string, XmlVariable> xmlVariables;

        private void readVariableTranslationsFromXml(XmlDocument doc)
        {
            XmlElement merger = doc["merger"];

            string fileDirectory = _settingsDirectory ?? Path.GetDirectoryName(recorderFiles[0]);
            XmlElement variables = merger.GetSettingsElementFromRootOrImport(fileDirectory, "variables");
            if (variables == null) return;

            bool ignoreColumns = variables.GetAttributeOrElementBool("ignore_columns") ?? false;

            XmlNodeList list = variables.GetElementsByTagName("variable");

            xmlVariables = XmlVariable.CreateFromXml(list);
        }

        private void readExperimentsFromXml(XmlDocument doc)
        {
            //ts.TraceEvent(TraceEventType.Information, (int)ExperimentManagerID.LoadingExperimentsFromXML, "Loading Experiment settings from XML file.");

            //should be called AFTER readGlobalSettingsAndDefaults()
            if (!_areGlobalSettingsAndDefaultsRead) readGlobalSettingsAndDefaultsFromXml(doc);

            XmlElement experiments = doc["merger"]["experiments"];
            XmlNodeList list = experiments.GetElementsByTagName("experiment");

            foreach (XmlElement experiment in list)
            {
                Experiment exp = Experiment.CreateFromXML(experiment, this);
                if (exp != null) _experiments.Add(exp);
            }
        }
        #endregion


        #region IEnumerable interface
        public IEnumerator<Experiment> GetEnumerator() => _experiments.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _experiments.GetEnumerator();

        IEnumerator<Experiment> IEnumerable<Experiment>.GetEnumerator() => _experiments.GetEnumerator();

        #endregion


    }
}
