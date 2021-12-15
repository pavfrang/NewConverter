using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using ConvertMerge;

namespace NewConverter
{

    class Program
    {
        protected static TraceSource trace = new TraceSource("ExperimentManager", SourceLevels.All);

        //this is git test 2
        static ExperimentManager exp;


        static void Main(string[] args)
        {
            //trace.TraceEvent(TraceEventType.Verbose, (int)ProgramEventID.SettingCultureToUS, "Setting the culture to en-US.");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            if (args.Length > 0)
            {
                string firstFilePath = args[0];
                if (ExperimentManager.IsXmlConfigurationFile(firstFilePath))
                {
                    //trace.TraceEvent(TraceEventType.Verbose, (int)ConvertMerge.ExperimentManagerID.CreatingExperimentManager, "Initializing Experiment Manager from XML file.");
                    try
                    {
                        exp = ExperimentManager.CreateFromXMLConfigurationFile(firstFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                           ex.Message, "NewConverter",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return;
                    }
                }
                else
                {
                    //trace.TraceEvent(TraceEventType.Verbose, (int)ConvertMerge.ExperimentManagerID.CreatingExperimentManager, "Initializing Experiment Manager from recorder files.");
                    try
                    {
                        exp = ExperimentManager.CreateFromRecorderFiles(args);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                           ex.Message, "NewConverter",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return;
                    }
                }

                exp.LoadDataFromSource(true);

                exp.MergeExperiments(false);

                exp.SaveToExcelFiles(false);

                Console.WriteLine("Finished!");
            }
            else
                MessageBox.Show("No Xml merge configuration file configured.", "NewConverter", MessageBoxButtons.OK, MessageBoxIcon.Warning);


            //#endif
        }


    }




    //public class FormattingStreamWriter : StreamWriter
    //{
    //    private readonly IFormatProvider formatProvider;

    //    public FormattingStreamWriter(string path, IFormatProvider formatProvider)
    //        : base(path)
    //    {
    //        this.formatProvider = formatProvider;
    //    }
    //    public override IFormatProvider FormatProvider
    //    {
    //        get
    //        {
    //            return this.formatProvider;
    //        }
    //    }
    //}
}
