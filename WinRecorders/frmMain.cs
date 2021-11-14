using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using WinRecorders.Properties;

using Microsoft.Office.Interop.Excel;
using Paulus.Forms;
using Paulus.Excel;

namespace WinRecorders
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();

            fileDropper = new FileDragDropper(textBox1);
            fileDropper.DragDropFiles += fileDropper_DragDropFiles;

            FormClosing += frmMain_FormClosing;

        }

        void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (comboBox1.SelectedItem != null)
                Settings.Default.CurrentWorksheet =  ((Worksheet)(comboBox1.SelectedItem)).Name;
            
            Settings.Default.Save();
        }

        #region File Dropper to textbox
        void fileDropper_DragDropFiles(object sender, FilesEventArgs e)
        {
            if (e.Files.Length > 0) textBox1.Text = e.Files[0];
        }

        private FileDragDropper fileDropper;

        private void btnBrowse_Click(object sender, System.EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }
        #endregion

        private void btnLoadFile_Click(object sender, System.EventArgs e)
        {
            string wbPath = textBox1.Text;

        }

        private void LoadWorksheetsToComboBox(Workbook wb)
        {
            comboBox1.Items.Clear();
            comboBox1.DisplayMember = "Name";
            foreach (Worksheet sh in wb.Worksheets) comboBox1.Items.Add(sh);
            string lastUsedWorksheetName = Settings.Default.CurrentWorksheet;
            if (wb.Worksheets.Contains(lastUsedWorksheetName))
                comboBox1.SelectedItem = wb.Worksheets[lastUsedWorksheetName];
        }
    }


}
