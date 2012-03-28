/*
Demoder's GUI Compiler
Copyright (c) 2010-2012 Demoder <demoder@demoder.me>

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; version 2 of the License only.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
USA
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Demoder.GUICompiler.DataClasses;
using Demoder.GUICompiler.Helpers.Serialization;

namespace Demoder.GUICompilerGUI
{
    public partial class MainWindow : Form
    {
        private bool configChanged = false;
        private FileInfo configFile = null;


        public MainWindow()
        {
            InitializeComponent();
        }
        #region Button clicks
        private void btnSrcDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Please select a source directory. This directory contains the image files which the Image Archive consist of.";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.sourceDirectory.Text = fbd.SelectedPath;
        }

        private void btnDstDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "Please select a destination directory. The Image Archive will be written to this directory.";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.destinationDirectory.Text = fbd.SelectedPath;
        }

        /// <summary>
        /// Do the GUI compile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCompile_Click(object sender, EventArgs e)
        {
            this.compile(true);
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Ask the user if they want to save the current config if it has changed. Returns true if the user didn't want to save, or if the user successfully saved. False if the user wanted to save but failed.
        /// </summary>
        /// <returns></returns>
        private bool ConfigChangedSave()
        {
            if (this.configChanged)
            {
                DialogResult dr = MessageBox.Show("Configuration has been changed. Save changes?", "Save configuration?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                switch (dr)
                {
                    case System.Windows.Forms.DialogResult.Cancel:
                        return false;
                    case System.Windows.Forms.DialogResult.No:
                        break;
                    case System.Windows.Forms.DialogResult.Yes:
                        this.SaveConfig();
                        if (this.configFile == null)
                            return false;
                        break;
                }

            }
            return true;
        }

        private void SaveConfig(bool saveAs=false)
        {
            if (this.configFile == null || saveAs)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "XML files|*.xml";
                DialogResult dr = sfd.ShowDialog();
                switch (dr)
                {
                    case System.Windows.Forms.DialogResult.OK:
                        this.configFile = new FileInfo(sfd.FileName);
                        break;
                    case System.Windows.Forms.DialogResult.Cancel:
                        return;
                }
            }

            Xml.Serialize<GUIConfig>(this.configFile,
                new GUIConfig
                {
                    ArchiveName = this.archiveName.Text,
                    DestinationDirectory = this.destinationDirectory.Text,
                    SourceDirectory = this.sourceDirectory.Text
                }, false);
            this.configChanged = false;
        }
        #endregion

        private void TextBoxTextChanged(object sender, EventArgs e)
        {
            //Tag as config changed
            this.configChanged = true;
        }


        #region Toolstrip menu buttons
        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ConfigChangedSave())
            {
                this.configFile = null;
                this.archiveName.Text = "";
                this.destinationDirectory.Text = "";
                this.sourceDirectory.Text = "";
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML Files|*.xml";
            DialogResult dr = ofd.ShowDialog();
            switch (dr)
            {
                case System.Windows.Forms.DialogResult.OK:
                    if (this.ConfigChangedSave())
                    {
                        GUIConfig gc = Xml.Deserialize<GUIConfig>(new FileInfo(ofd.FileName), false);
                        this.archiveName.Text = gc.ArchiveName;
                        this.destinationDirectory.Text = gc.DestinationDirectory;
                        this.sourceDirectory.Text = gc.SourceDirectory;
                        this.configFile = new FileInfo(ofd.FileName);
                        this.configChanged = false;

                    }
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                    return;
            }
        }


        void compile(bool Interactive)
        {
                bool error = false;
                if (!Directory.Exists(this.sourceDirectory.Text))
                {
                    if (Interactive)
                        MessageBox.Show("Source directory does not exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    error = true;
                }
                if (!Directory.Exists(this.destinationDirectory.Text))
                {
                    Directory.CreateDirectory(this.destinationDirectory.Text);
                    if (!Directory.Exists(this.destinationDirectory.Text))
                    {
                        if (Interactive)
                            MessageBox.Show("Failed to create destination directory!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        error = true;
                    }
                }
                if (this.archiveName.Text.Length == 0)
                {
                    if (Interactive)
                        MessageBox.Show("Archive name too short! Must be at least one character long.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    error = true;
                }
                if (!error)
                {
                    ImageArchive ia = new ImageArchive();
                    int imgloaded = ia.Add(new DirectoryInfo(this.sourceDirectory.Text));
                    bool saved = ia.Save(new DirectoryInfo(this.destinationDirectory.Text), this.archiveName.Text);
                    if (Interactive)
                    {
                        if (saved)
                            MessageBox.Show("GUI saved to " + this.destinationDirectory.Text, "Compile Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        else
                            MessageBox.Show("GUI was not saved.", "Compile Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }


        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SaveConfig();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SaveConfig(true);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().ShowDialog();
        }

        #endregion


        private void MainWindowFormClosing(object sender, FormClosingEventArgs e)
        {
             e.Cancel = !this.ConfigChangedSave();
        }

        private void MainWindowLoad(object sender, EventArgs e)
        {
            this.Text = "Demoder's GUI Compiler v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }


        #region Very dirty implementation of GUI decompiling.
        private void decompileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.DecompileGuiArchive();
        }

        private void DecompileGuiArchive()
        {
            var fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Graphics Index|*.uvgi";
            var folderDialog = new FolderBrowserDialog();
            folderDialog.Description = "Select a folder to save the images to.";
            var dr = fileDialog.ShowDialog();
            if (dr != DialogResult.OK) { return; }

            dr = folderDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                FileInfo index = new FileInfo(fileDialog.FileName);
                ImageArchive ia = new ImageArchive(index);
                ia.Save(new DirectoryInfo(String.Format("{1}{0}images", Path.DirectorySeparatorChar, folderDialog.SelectedPath)));
                MessageBox.Show("GUI extraction complete.", "GUI extracted", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }        
        #endregion
    }
}
