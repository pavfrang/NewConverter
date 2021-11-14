using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

namespace CleanSemsFile
{
    //v1.1 Allows multiple files to be dragged.
    class Program
    {
        static void Main(string[] args)
        {
            if (!args.Any()) return;

            //string filePath = args[0];
            foreach (string filePath in args)
            {
                Console.WriteLine($"Reading {filePath}...");
                bool isSemsFile = true;
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line = reader.ReadLine();
                    isSemsFile = line.StartsWith("UTC");
                }
                if (!isSemsFile) { Console.WriteLine("Not a SEMS file!"); return; }

                int iLine = 0;
                using (StreamReader reader = new StreamReader(filePath))
                {
                    using (StreamWriter writer = new StreamWriter(Path.GetFileNameWithoutExtension(filePath) + "_c.csv"))
                    {
                        //copy first 2 lines
                        writer.WriteLine(reader.ReadLine()); iLine++;
                        writer.WriteLine(reader.ReadLine()); iLine++;
                        string line = reader.ReadLine(); iLine++;
                        string previousFirstToken = line.Substring(0, line.IndexOf(','));
                        //copy 3rd line
                        writer.WriteLine(line);

                        while (!reader.EndOfStream)
                        {
                            line = reader.ReadLine(); iLine++;
                            string currentFirstToken = line.Substring(0, line.IndexOf(','));
                            //remove lines with same first token (time)
                            if (currentFirstToken != previousFirstToken)
                            {
                                writer.WriteLine(line);
                                previousFirstToken = currentFirstToken;
                            }
                            else
                                Console.WriteLine($"Duplicate time in line ${iLine}.");

                        }

                    }
                }
            }
        }
    }
}
