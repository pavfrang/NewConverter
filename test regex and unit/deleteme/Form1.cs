using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Paulus.Extensions;

namespace deleteme
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string firstLine = @"Time [s];Soot_Value_1,,Time [s],Soot_";
            Match m = Regex.Match(firstLine, @"\[(?<unit>\w{1,2})]", RegexOptions.Singleline);

            string unit = m.Groups["unit"].Value;


            Debugger.Break();
        }
    }
}
