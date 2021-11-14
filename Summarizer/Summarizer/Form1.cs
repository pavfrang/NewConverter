using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Paulus.Forms;
using Paulus.Extensions;

using System.IO;

using Excel = Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

using Paulus.Excel;

namespace Summarizer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            dropper = new FileDragDropper(this.lvwFiles);
            dropper.DragDropFiles += dropper_DragDropFiles;

            lvwWorksheets.ListViewItemSorter = lvwColumnSorter;
        }

        FileDragDropper dropper;
        Dictionary<ListViewItem, string> listItems = new Dictionary<ListViewItem, string>();


        void dropper_DragDropFiles(object sender, FilesEventArgs e)
        {
            foreach (string f in e.Files)
            {
                string fileName = Path.GetFileName(f);
                if (Path.GetExtension(fileName).EqualsOneOf(new string[] { ".xls", ".xlsx", ".xlsm", ".xlsb" }))
                {
                    string parentDirectory = Path.GetDirectoryName(f);
                    ListViewItem item = lvwFiles.Items.Add(fileName, "excel2013");
                    item.SubItems.Add(parentDirectory);
                    item.Checked = true;
                    //associate added listitem with 
                    listItems.Add(item, f);
                }
            }

            lvwFiles.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvwFiles.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        List<string> worksheetNames = new List<string>();


        private void wizardPage1_PageValidating(object sender, DevExpress.XtraWizard.WizardPageValidatingEventArgs e)
        {
            //LoadWorksheetNamesFromCheckedWorkbooks();

            e.Valid = lvwFiles.CheckedItems.Count > 0;
            if (!e.Valid)
                MessageBox.Show("Add and check the workbooks you want to summarize before proceeding to next step.", "Summarizer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void LoadWorksheetNamesFromCheckedWorkbooks()
        {
            int filesCount = lvwFiles.CheckedItems.Count;
            if (filesCount > 0)
            {
                Cursor = Cursors.WaitCursor;
                List<object> comObjectsToDispose = new List<object>();


                worksheetNames.Clear();

                progressBarControl1.Properties.Maximum = filesCount + 1;
                progressBarControl1.Properties.Step = 1;
                progressBarControl1.Position = 0;

                progressBarControl1.Show();

                Excel.Application excel = new Excel.Application(); comObjectsToDispose.Add(excel);
                Excel.Workbooks workbooks = excel.Workbooks; comObjectsToDispose.Add(workbooks);

                progressBarControl1.PerformStep(); progressBarControl1.Update();

                int iFile = 1;
                foreach (ListViewItem item in lvwFiles.CheckedItems)
                {
                    string file = listItems[item];

                    Excel.Workbook workbook = workbooks.Open(file, ReadOnly: true); comObjectsToDispose.Add(workbook);
                    Excel.Sheets worksheets = workbook.Worksheets; comObjectsToDispose.Add(worksheets);

                    progressBarControl1.PerformStep(); progressBarControl1.Update();

                    foreach (Excel.Worksheet worksheet in worksheets)
                    {
                        worksheetNames.AddIfUniqueAndNotNull(worksheet.Name);
                        comObjectsToDispose.Add(worksheet);
                    }

                    workbook.Close(false);
                }

                excel.Quit();

                //release all COM objects
                GC.Collect(); GC.WaitForPendingFinalizers();
                GC.Collect(); GC.WaitForPendingFinalizers();
                foreach (object o in comObjectsToDispose)
                    Marshal.FinalReleaseComObject(o);

                lvwWorksheets.Items.Clear();
                foreach (string worksheetName in worksheetNames)
                {
                    ListViewItem item = lvwWorksheets.Items.Add(worksheetName, "worksheet");
                    item.Checked = true;
                    item.SubItems.Add(item.Text);
                }

                lvwWorksheets.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                lvwWorksheets.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);


                progressBarControl1.Hide();

                Cursor = Cursors.Default;
            }
        }

        private ListViewColumnSorter lvwColumnSorter = new ListViewColumnSorter();

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            LoadWorksheetNamesFromCheckedWorkbooks();
        }

        private void lvwWorksheets_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
                // Reverse the current sort direction for this column.
                lvwColumnSorter.Order =
                    lvwColumnSorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            lvwWorksheets.Sort();
        }

    }
}
