using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Paulus.IO;

namespace ConvertMerge.Recorders
{
    public class SmokemeterRecorder : Recorder
    {
        protected internal override void ReadStartingTime()
        {
            throw new NotImplementedException();
        }

        protected override void ReadVariableInfos()
        {
            string line1 = StreamReaderExtensions.ReadLine(_sourceFilePath, 1);

        }

        protected override int linesToOmitBeforeData
        {
            get { return 1; }
        }

        protected override char readSeparator()
        {
            return ';';
        }

        protected override bool loadDataFromLine(string[] tokens, ref int iLine)
        {
            throw new NotImplementedException();
        }
    }
}
