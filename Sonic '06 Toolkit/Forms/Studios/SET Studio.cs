﻿using System;
using System.IO;
using System.Windows.Forms;

namespace Sonic_06_Toolkit
{
    public partial class SET_Studio : Form
    {
        public SET_Studio()
        {
            InitializeComponent();
        }

        void SET_Studio_Load(object sender, EventArgs e)
        {
            Tools.Global.setState = "export";
            btn_Convert.Text = "Export";

            clb_SETs.Items.Clear();

            if (Directory.GetFiles(Tools.Global.currentPath, "*.set").Length > 0)
            {
                modes_Export.Checked = true;
                modes_Import.Checked = false;
                options_DeleteXML.Visible = false;
                options_CreateBackupSET.Visible = false;
            }
            else if (Directory.GetFiles(Tools.Global.currentPath, "*.xml").Length > 0)
            {
                modes_Export.Checked = false;
                modes_Import.Checked = true;
                options_DeleteXML.Visible = true;
                options_CreateBackupSET.Visible = true;
            }
            else { MessageBox.Show("There are no convertable files in this directory.", "No files available", MessageBoxButtons.OK, MessageBoxIcon.Information); Close(); }

            if (Properties.Settings.Default.backupSET == true) options_CreateBackupSET.Checked = true;
            if (Properties.Settings.Default.deleteXML == true) options_DeleteXML.Checked = true;
        }

        void Btn_SelectAll_Click(object sender, EventArgs e)
        {
            //Checks all available checkboxes.
            for (int i = 0; i < clb_SETs.Items.Count; i++) clb_SETs.SetItemChecked(i, true);
            btn_Convert.Enabled = true;
        }

        private void Btn_DeselectAll_Click(object sender, EventArgs e)
        {
            //Checks all available checkboxes.
            for (int i = 0; i < clb_SETs.Items.Count; i++) clb_SETs.SetItemChecked(i, false);
            btn_Convert.Enabled = false;
        }

        void Clb_SETs_SelectedIndexChanged(object sender, EventArgs e)
        {
            clb_SETs.ClearSelected(); //Removes the blue highlight on recently checked boxes.

            //Enables/disables the Convert button, depending on whether a box has been checked.
            if (clb_SETs.CheckedItems.Count > 0)
            {
                btn_Convert.Enabled = true;
            }
            else
            {
                btn_Convert.Enabled = false;
            }
        }

        void Btn_Convert_Click(object sender, EventArgs e)
        {
            //In the odd chance that someone is ever able to click Export without anything selected, this will prevent that.
            if (clb_SETs.CheckedItems.Count == 0) MessageBox.Show("Please select a file.", "No files specified", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (Tools.Global.setState == "export")
            {
                try
                {
                    foreach (string selectedSET in clb_SETs.CheckedItems)
                    {
                        Tools.Global.setState = "export";
                        Tools.SET.Export(string.Empty, selectedSET);
                    }
                }
                catch
                {
                    MessageBox.Show("An error occurred when converting the SETs.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Tools.Notification.Dispose();
                }
            }
            else if (Tools.Global.setState == "import")
            {
                try
                {
                    foreach (string selectedXML in clb_SETs.CheckedItems)
                    {
                        Tools.Global.setState = "import";
                        Tools.SET.Import(string.Empty, selectedXML);
                    }
                }
                catch
                {
                    MessageBox.Show("An error occurred when importing the XMLs.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Tools.Notification.Dispose();
                }
            }
            else
            {
                MessageBox.Show("SET State set to invalid value: " + Tools.Global.setState + "\nLine information: " + new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileLineNumber(), "Developer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void Modes_Export_CheckedChanged(object sender, EventArgs e)
        {
            if (modes_Export.Checked == true)
            {
                modes_Import.Checked = false;
                options_DeleteXML.Visible = false;
                options_CreateBackupSET.Visible = false;
                btn_Convert.Enabled = false;

                Tools.Global.setState = "export";
                btn_Convert.Text = "Export";

                clb_SETs.Items.Clear();

                #region Getting SETs to unpack...
                foreach (string SET in Directory.GetFiles(Tools.Global.currentPath, "*.set", SearchOption.TopDirectoryOnly))
                {
                    if (File.Exists(SET))
                    {
                        clb_SETs.Items.Add(Path.GetFileName(SET));
                    }
                }
                #endregion

                if (Directory.GetFiles(Tools.Global.currentPath, "*.set").Length == 0)
                {
                    MessageBox.Show("There are no SETs to export in this directory.", "No SETs available", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (Directory.GetFiles(Tools.Global.currentPath, "*.xml").Length == 0)
                    {
                        Close();
                    }
                    else
                    {
                        modes_Export.Checked = false;
                        modes_Import.Checked = true;
                    }
                }
            }
        }

        void Modes_Import_CheckedChanged(object sender, EventArgs e)
        {
            if (modes_Import.Checked == true)
            {
                modes_Export.Checked = false;
                options_DeleteXML.Visible = true;
                options_CreateBackupSET.Visible = true;
                btn_Convert.Enabled = false;

                Tools.Global.setState = "import";
                btn_Convert.Text = "Import";

                clb_SETs.Items.Clear();

                #region Getting SETs to unpack...
                foreach (string XML in Directory.GetFiles(Tools.Global.currentPath, "*.xml", SearchOption.TopDirectoryOnly))
                {
                    if (File.Exists(XML))
                    {
                        clb_SETs.Items.Add(Path.GetFileName(XML));
                    }
                }
                #endregion

                if (Directory.GetFiles(Tools.Global.currentPath, "*.xml").Length == 0)
                {
                    MessageBox.Show("There are no XMLs to import in this directory.", "No XMLs available", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    if (Directory.GetFiles(Tools.Global.currentPath, "*.set").Length == 0)
                    {
                        Close();
                    }
                    else
                    {
                        modes_Export.Checked = true;
                        modes_Import.Checked = false;
                    }
                }
            }
        }

        void Options_CreateBackupSET_CheckedChanged(object sender, EventArgs e)
        {
            if (options_CreateBackupSET.Checked == true) Properties.Settings.Default.backupSET = true;
            else Properties.Settings.Default.backupSET = false;
        }

        void Options_DeleteXML_CheckedChanged(object sender, EventArgs e)
        {
            if (options_DeleteXML.Checked == true) Properties.Settings.Default.deleteXML = true;
            else Properties.Settings.Default.deleteXML = false;
        }

        void SET_Studio_FormClosing(object sender, FormClosingEventArgs e)
        {
            Tools.Global.setState = null;
        }
    }
}
