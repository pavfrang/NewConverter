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

using ConvertMerge;
using Paulus.Forms;

namespace WinConvertMerge
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            fileDropper = new FileDragDropper(lvwRecorders);
            fileDropper.DragDropFiles += fileDropper_DragDropFiles;

        }

        Dictionary<ListViewItem, string> listViewRecords;
        Dictionary<ListViewItem, Recorder> listViewRecorders;

        void fileDropper_DragDropFiles(object sender, FilesEventArgs e)
        {
            foreach (string f in e.Files)
            {
                if (!File.Exists(f)) continue;

                ListViewItem item =
                    lvwRecorders.Items.Add(Path.GetFileName(f));

                RecorderFileType fileType = Recorder.GetRecorderFileType(f);
                item.SubItems.Add(Recorder.RecorderFileTypeToString(fileType));

                if (fileType != RecorderFileType.Unknown)
                {

                    if (listViewRecorders == null) listViewRecorders = new Dictionary<ListViewItem, Recorder>();
                    Recorder r = Recorder.Create(f);
                    listViewRecorders.Add(item, r);

                    item.SubItems.Add(r.PeekStartingTime().ToString("yyyy-MMM-dd , HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));

                }

                if (listViewRecords == null) listViewRecords = new Dictionary<ListViewItem, string>();
                listViewRecords.Add(item, f);
            }

            lvwRecorders.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        FileDragDropper fileDropper;

        private void removeUnknownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in lvwRecorders.Items)
            {
                if (item.SubItems[1].Text == "Unknown")
                {
                    listViewRecords.Remove(item);
                    lvwRecorders.Items.Remove(item);
                }
            }
        }

        private void removeSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvwRecorders.SelectedItems.Count > 0)
            {
                DialogResult reply = MessageBox.Show("Are you sure you want to remove the selected recorders?", "WinConvertMerge", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

                if (reply == System.Windows.Forms.DialogResult.Yes)
                    foreach (ListViewItem item in lvwRecorders.SelectedItems)
                    {
                        listViewRecords.Remove(item);
                        lvwRecorders.Items.Remove(item);
                    }
            }
        }

        private void removeallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvwRecorders.Items.Count > 0)
            {
                DialogResult reply = MessageBox.Show("Are you sure you want to remove all recorders?", "WinConvertMerge", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);

                if (reply == System.Windows.Forms.DialogResult.Yes)
                    lvwRecorders.Items.Clear();
            }
        }

        private void lvwRecorders_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                removeSelectedToolStripMenuItem_Click(sender, e);
        }

        private void lvwRecorders_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvwRecorders.SelectedIndices.Count > 0)
            {
                ListViewItem selectedItem = lvwRecorders.SelectedItems[0];
                if ((listViewRecorders?.Count ?? 0) == 0) return;

                if (listViewRecorders.ContainsKey(selectedItem))
                {
                    Recorder r = listViewRecorders[selectedItem];
                    propRecorder.SelectedObject = r;
                }
            }

        }

        private void loadVariablesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lvwRecorders.SelectedIndices.Count > 0)
            {
                Recorder r = listViewRecorders[lvwRecorders.SelectedItems[0]];
                r.LoadDataFromSource(null);
                propRecorder.Refresh();
            }
        }

    }
}
