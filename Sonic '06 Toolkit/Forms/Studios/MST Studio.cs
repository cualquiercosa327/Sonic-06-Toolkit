﻿using System;
using System.IO;
using System.Windows.Forms;

namespace Sonic_06_Toolkit
{
    public partial class MST_Studio : Form
    {
        public MST_Studio()
        {
            InitializeComponent();
        }

        void MST_Studio_Load(object sender, EventArgs e)
        {
            Tools.Global.setState = "export";
            btn_Convert.Text = "Export";

            clb_MSTs.Items.Clear();

            if (Directory.GetFiles(Tools.Global.currentPath, "*.mst").Length > 0)
            {
                combo_Mode.SelectedIndex = 0;
            }
            else if (Directory.GetFiles(Tools.Global.currentPath, "*.xml").Length > 0)
            {
                combo_Mode.SelectedIndex = 1;
            }
            else { MessageBox.Show("There are no convertable files in this directory.", "No files available", MessageBoxButtons.OK, MessageBoxIcon.Information); Close(); }
        }

        void Btn_SelectAll_Click(object sender, EventArgs e)
        {
            //Checks all available checkboxes.
            for (int i = 0; i < clb_MSTs.Items.Count; i++) clb_MSTs.SetItemChecked(i, true);
            btn_Convert.Enabled = true;
        }

        void Btn_DeselectAll_Click(object sender, EventArgs e)
        {
            //Unchecks all available checkboxes.
            for (int i = 0; i < clb_MSTs.Items.Count; i++) clb_MSTs.SetItemChecked(i, false);
            btn_Convert.Enabled = false;
        }

        void Clb_MSTs_SelectedIndexChanged(object sender, EventArgs e)
        {
            clb_MSTs.ClearSelected(); //Removes the blue highlight on recently checked boxes.

            //Enables/disables the Convert button, depending on whether a box has been checked.
            if (clb_MSTs.CheckedItems.Count > 0)
            {
                btn_Convert.Enabled = true;
            }
            else
            {
                btn_Convert.Enabled = false;
            }
        }

        void Btn_Decode_Click(object sender, EventArgs e)
        {
            //In the odd chance that someone is ever able to click Export without anything selected, this will prevent that.
            if (clb_MSTs.CheckedItems.Count == 0) MessageBox.Show("Please select a file.", "No files specified", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (Tools.Global.mstState == "export")
            {
                try
                {
                    foreach (string selectedMST in clb_MSTs.CheckedItems)
                    {
                        Tools.Global.mstState = "mst";
                        Tools.MST.Export(string.Empty, selectedMST);
                    }
                }
                catch
                {
                    MessageBox.Show("An error occurred when converting the MSTs.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Tools.Notification.Dispose();
                }
            }
            else if (Tools.Global.mstState == "import")
            {
                try
                {
                    foreach (string selectedXML in clb_MSTs.CheckedItems)
                    {
                        Tools.Global.mstState = "xml";
                        Tools.MST.Import(string.Empty, selectedXML);
                    }
                }
                catch
                {
                    MessageBox.Show("An error occurred when converting the XMLs.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Tools.Notification.Dispose();
                }
            }
        }

        void MST_Studio_FormClosing(object sender, FormClosingEventArgs e)
        {
            Tools.Global.mstState = null;
        }

        void Combo_Mode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (combo_Mode.SelectedIndex == 0)
            {
                Tools.Global.mstState = "export";
                btn_Convert.Text = "Export";

                clb_MSTs.Items.Clear();

                foreach (string MST in Directory.GetFiles(Tools.Global.currentPath, "*.mst", SearchOption.TopDirectoryOnly))
                {
                    if (File.Exists(MST))
                    {
                        clb_MSTs.Items.Add(Path.GetFileName(MST));
                    }
                }

                //Checks if there are any CSBs in the directory.
                if (clb_MSTs.Items.Count == 0)
                {
                    MessageBox.Show("There are no MSTs to export in this directory.", "No MSTs available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                }
            }
            else if (combo_Mode.SelectedIndex == 1)
            {
                Tools.Global.mstState = "import";
                btn_Convert.Text = "Import";

                clb_MSTs.Items.Clear();

                foreach (string XML in Directory.GetFiles(Tools.Global.currentPath, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    if (File.Exists(XML))
                    {
                        clb_MSTs.Items.Add(Path.GetFileName(XML));
                    }
                }

                //Checks if there are any CSBs in the directory.
                if (clb_MSTs.Items.Count == 0)
                {
                    MessageBox.Show("There are no XMLs to import in this directory.", "No XMLs available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    combo_Mode.SelectedIndex = 0;
                }
            }
        }
    }
}
