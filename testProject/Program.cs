using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Xml;

using System.Globalization;
using System.Diagnostics;
using System.Threading;

using System.Text.RegularExpressions;
using Paulus.Common;
using Paulus.IO;
using Paulus.IO.Settings;

namespace testProject
{
    class Program
    {
        static TraceSource ts = new TraceSource("tracetest", SourceLevels.All);


        static void Main(string[] args)
        {
            testXPath2FromString();



            Console.Read();
        }

        private static void testXPath2FromString()
        {
            string xml = @"<t><ch>first</ch><ch>second</ch></t>";
            XmlDocument d = new XmlDocument();
            d.LoadXml(xml);

            XmlNodeList xmlChildren = d["t"].SelectNodes("ch");
            //XmlNodeList xmlChildren = d.DocumentElement.SelectNodes("ch");
            //XmlNodeList xmlChildren = d.SelectNodes(@"t/ch");

            foreach (XmlElement v in xmlChildren)
                Console.WriteLine(v.InnerText);
        }

        private static void testXPath()
        {
            string pth = @"D:\lat\dev\NewConverter\NewConverter\bin\Release\variables_cell1.xml";
            XmlDocument d = new XmlDocument();
            d.Load(pth);
            //IEnumerable<XmlElement> xmlVariables = d.DocumentElement.SelectNodes("variable").Cast<XmlElement>();
            //IEnumerable<XmlElement> xmlVariables = d.SelectNodes("/variables/variable").Cast<XmlElement>();
            XmlNodeList xmlVariables = d.SelectNodes("/variables/variable");


            //IEnumerable<string> names = from v in xmlVariables select v.GetAttribute("name");

            //IEnumerable<XmlElement> xmlVariables = d["variables"].SelectNodes("variable").Cast<XmlElement>();
            foreach (XmlElement v in xmlVariables)
                //Console.WriteLine(v.GetAttribute("name"));
                //Console.WriteLine(v.SelectSingleNode("@name").Value);
                Console.WriteLine(v.Attributes["name"].Value);
        }


        private static void testXMLByTag()
        {
            string pth = @"C:\Users\User\Desktop\MEASUREMENTS DeNOX Thermostar tests\300114\2014-01-30 merger.xml";
            XmlDocument d = new XmlDocument();
            d.Load(pth);

            XmlNodeList l = d["merger"].GetElementsByTagName("import");

            foreach (XmlElement n in l)
            {
                string file = n.GetAttribute("file");

                switch (n.GetAttribute("type"))
                {
                    case "settings":
                        XmlDocument settingsXML = new XmlDocument();
                        file =PathExtensions.GetAbsolutePath2(pth, file, true);

                        settingsXML.Load(file);
                        XmlElement el = settingsXML["settings"];
                        //Console.WriteLine(el["experiment_defaults"]["timestep"].InnerText);
                        break;
                    case "text":
                        string text = File.ReadAllText(file);
                        //XmlElement el=
                        break;
                    case "variables":
                        break;
                }
            }

            //tha epistrepsei adeio!
            Console.WriteLine(l.Count);
        }


        private static void testRegEx()
        {
            string input = "Date;Start Time;End Time;Soot Dil. Corr. [mg/m3];Filter Smoke Number;Pollution Level [%]";
            //works for all separators!!!!
            input = input.Replace(';', ',');

            //string input2 = " Soot Concentration [  -sflaskdjf  h2347uoijkm,,./    ];Date; Start Time";
            //ignore the spaces:
            //1. at the beginning
            //2. betweeen name and unit if it exists
            //3. 
            string pattern = @"(?<name>\w(\w| )+\w)( *\[ *(?<unit>[^]]*\S) *])?";
            MatchCollection mc = Regex.Matches(input, pattern, RegexOptions.Singleline);
            foreach (Match m in mc)
            {
                Console.WriteLine("|{0}|", m.Groups["name"].Value);
                Console.WriteLine("|{0}|", m.Groups["unit"].Value);
            }

            //αν έχει unit τότε αυτό είναι το πληρέστερο
            string p1 = @"\b(?<name>\b[^\[]*\b)\s*\["; //τουλάχιστον δύο χαρακτήρες
            //αν ΔΕΝ έχει unit τότε:
            string p2 = @"(?<name>\b[^\[]*\b)";

            string p = @"(?(" + p1 + ")" + p1 + "|" + p2 + ")";

            string[] inputs = new string[] { @" as ds/\*,3  [ asd\3 ff ] ; g[o]", "u", "k[s]" };

            //Console.WriteLine(Regex.IsMatch(inputs[0],@"\["));

            foreach (string sinput in inputs)
                foreach (Match m in Regex.Matches(sinput, p1))
                {
                    //Console.WriteLine(Regex.IsMatch(sinput, p1));
                    Console.WriteLine("|{0}|", m.Groups["name"].Value);
                }
        }

        private static void TestCANFiles()
        {
            string p = @"D:\lat\phd\obd\Stoneridge 2013\Honda Stoneridge 2013";
            string[] files = Directory.GetFiles(p, "*can.csv", SearchOption.AllDirectories);

            foreach (string f in files)
            {
                if (f.EndsWith("pcan.csv")) continue;

                string line1 = StreamReaderExtensions.ReadLine(f, 1);

                if (!line1.StartsWith("Time [s];"))
                {
                    Console.Write(Path.GetFileNameWithoutExtension(f));
                    Console.WriteLine(" PATAPTATATTA!!!");
                }
                else
                {
                    Console.Write(Path.GetFileNameWithoutExtension(f));
                    Console.WriteLine(" OK!!!");
                }
            }
        }

        private static void TestCANLogFiles()
        {
            string p = @"D:\lat\phd\obd\Stoneridge 2013\Honda Stoneridge 2013";
            string[] files = Directory.GetFiles(p, "*can.csv", SearchOption.AllDirectories);

            foreach (string f in files)
            {
                if (f.EndsWith("pcan.csv")) continue;

                string TimeFilePath = f.Replace("can.csv", "log.txt");
                if (!File.Exists(TimeFilePath)) TimeFilePath = f.Replace(" can.csv", "log.txt");

                if (!File.Exists(TimeFilePath)) throw new FileNotFoundException("Cannot find log file.", TimeFilePath);

                string line2 = StreamReaderExtensions.ReadLine(TimeFilePath, 2);
                string[] tokens = line2.Split(' ');

                //01:32:24 pm
                string sTime = string.Format("{0} {1}", tokens[tokens.Length - 2], tokens[tokens.Length - 1]);
                DateTime StartAbsoluteTime;
                bool parsed = DateTime.TryParseExact(sTime, "hh:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out StartAbsoluteTime);

                if (!parsed)
                {
                    Console.Write(Path.GetFileNameWithoutExtension(f));
                    Console.WriteLine(" PATAPTATATTA!!!");
                }
                else
                {
                    Console.Write(Path.GetFileNameWithoutExtension(f));
                    Console.WriteLine(StartAbsoluteTime + " OK!!!");
                }
            }
        }

        private static void TestSSToolFiles()
        {
            string p = @"D:\lat\phd\obd\Stoneridge 2013\Honda Stoneridge 2013";
            string[] files = Directory.GetFiles(p, "*sst.csv", SearchOption.AllDirectories);

            foreach (string f in files)
            {
                string line3 = StreamReaderExtensions.ReadLine(f, 3);
                if (!line3.StartsWith("msec"))
                {
                    Console.Write(Path.GetFileNameWithoutExtension(f));
                    Console.WriteLine(" PATAPTATATTA!!!");
                }
                else
                {
                    Console.Write(Path.GetFileNameWithoutExtension(f));
                    Console.WriteLine(" OK!!!");
                }
            }
        }

        private static void exportOldSettings()
        {
            string ps = @"C:\Users\User\Desktop\merge settings.ini";
            SettingsFile f = new SettingsFile(ps, true);
            f.PopulateDictionary();
            using (StreamWriter writer = new StreamWriter(@"C:\Users\User\Desktop\MEASUREMENTS DeNOX Thermostar tests\variables.txt"))
                foreach (var entry in f["variables"].Dictionary)
                {
                    //<variable name="T1" translatedname="Temp1"/>

                    writer.WriteLine(@"<variable name=""{0}"" translatedname=""{1}""/>",
                        entry.Key, entry.Value);
                }
        }

        private static void testUnits()
        {
            string input3 = "Conc.Dil, corrected [mg/m3]";

            var vu = input3.GetVariableNameAndUnit('[', ']');
            Console.WriteLine(vu.Name);
            Console.WriteLine(vu.Unit);
            Console.Read();

        }

        private static void testAbsolutePaths()
        {

            string[] p = new string[] {
                "d:\\temp\\keftedakia\\loukoum",
                "temp\\kefte*dakia\\loukoum",
                "temp\\keftedakia\\loukoum",
                "temp/keftedakia/loukoum" ,
                "d:/temp/keftedakia/loukoum",
                "file:///d:/temp/keftedakia/loukoum"
            };

            foreach (string s in p)
                Console.WriteLine("P: {0}, Valid: {1}, Absolute: {2}, Well", s,PathExtensions.IsPathValid(s),PathExtensions.IsPathAbsolute(s));
        }

        private static void testTrace()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            double t = 6.0;
            Console.WriteLine(t);
            //Console.WriteLine(el.GetAttributeOrElementDouble("timestep",CultureInfo.InvariantCulture));

            //Console.WriteLine(r2.Attributes["type"].Value);

            // TestSSToolFiles();
            ts.TraceInformation("10 mpiftekia irthan");
            ts.TraceEvent(TraceEventType.Information, 10, "11 mpiftekia irthan2");
            ts.TraceEvent(TraceEventType.Warning, 9, "12 mpiftekia irthan2");
            ts.TraceEvent(TraceEventType.Error, 18, "the error stuff!");
            //TestCANLogFiles();

            //TestCANFiles();
        }

    }
}
