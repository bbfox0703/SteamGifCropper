using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public partial class MergeGifsDialog : Form
    {
        public List<string> SelectedFilePaths { get; private set; }
        public string OutputFilePath { get; private set; }

        private ListBox lstGifFiles;
        private Button btnAddFiles;
        private Button btnRemoveSelected;
        private Button btnMoveUp;
        private Button btnMoveDown;
        private TextBox txtOutputPath;
        private Button btnBrowseOutput;
        private Button btnOK;
        private Button btnCancel;
        private Label lblGifFiles;
        private Label lblOutput;
        private Label lblInstructions;

        public MergeGifsDialog()
        {
            SelectedFilePaths = new List<string>();
            InitializeComponent();
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            bool isDarkMode = WindowsThemeManager.IsDarkModeEnabled();
            
            if (isDarkMode)
            {
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32);
                ForeColor = System.Drawing.Color.White;
                ApplyDarkThemeToControls(this.Controls);
            }
            else
            {
                BackColor = System.Drawing.SystemColors.Control;
                ForeColor = System.Drawing.SystemColors.ControlText;
                ApplyLightThemeToControls(this.Controls);
            }
            
            if (IsHandleCreated)
            {
                WindowsThemeManager.SetDarkModeForWindow(this.Handle, isDarkMode);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            
            if (value && IsHandleCreated)
            {
                bool isDarkMode = WindowsThemeManager.IsDarkModeEnabled();
                WindowsThemeManager.SetDarkModeForWindow(this.Handle, isDarkMode);
            }
        }

        private void ApplyDarkThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Label label)
                {
                    label.BackColor = System.Drawing.Color.Transparent;
                    label.ForeColor = System.Drawing.Color.White;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
                    textBox.ForeColor = System.Drawing.Color.White;
                }
                else if (control is Button button)
                {
                    button.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
                    button.ForeColor = System.Drawing.Color.White;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(128, 128, 128);
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
                    listBox.ForeColor = System.Drawing.Color.White;
                }
                
                if (control.HasChildren)
                {
                    ApplyDarkThemeToControls(control.Controls);
                }
            }
        }

        private void ApplyLightThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Label label)
                {
                    label.BackColor = System.Drawing.Color.Transparent;
                    label.ForeColor = System.Drawing.SystemColors.ControlText;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = System.Drawing.SystemColors.Window;
                    textBox.ForeColor = System.Drawing.SystemColors.WindowText;
                }
                else if (control is Button button)
                {
                    button.BackColor = System.Drawing.SystemColors.Control;
                    button.ForeColor = System.Drawing.SystemColors.ControlText;
                    button.FlatStyle = FlatStyle.Standard;
                    button.UseVisualStyleBackColor = true;
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = System.Drawing.SystemColors.Window;
                    listBox.ForeColor = System.Drawing.SystemColors.WindowText;
                }
                
                if (control.HasChildren)
                {
                    ApplyLightThemeToControls(control.Controls);
                }
            }
        }

        private void InitializeComponent()
        {
            lblInstructions = new Label();
            lblGifFiles = new Label();
            lstGifFiles = new ListBox();
            btnAddFiles = new Button();
            btnRemoveSelected = new Button();
            btnMoveUp = new Button();
            btnMoveDown = new Button();
            lblOutput = new Label();
            txtOutputPath = new TextBox();
            btnBrowseOutput = new Button();
            btnOK = new Button();
            btnCancel = new Button();
            SuspendLayout();
            
            // lblInstructions
            lblInstructions.Location = new System.Drawing.Point(8, 8);
            lblInstructions.Name = "lblInstructions";
            lblInstructions.Size = new System.Drawing.Size(450, 30);
            lblInstructions.TabIndex = 0;
            lblInstructions.Text = SteamGifCropper.Properties.Resources.Message_SelectGifFiles;
            
            // lblGifFiles
            lblGifFiles.Location = new System.Drawing.Point(8, 45);
            lblGifFiles.Name = "lblGifFiles";
            lblGifFiles.Size = new System.Drawing.Size(150, 15);
            lblGifFiles.TabIndex = 1;
            lblGifFiles.Text = "GIF Files (2-5):";
            
            // lstGifFiles
            lstGifFiles.Location = new System.Drawing.Point(8, 65);
            lstGifFiles.Name = "lstGifFiles";
            lstGifFiles.Size = new System.Drawing.Size(350, 150);
            lstGifFiles.TabIndex = 2;
            lstGifFiles.SelectionMode = SelectionMode.One;
            
            // btnAddFiles
            btnAddFiles.Location = new System.Drawing.Point(370, 65);
            btnAddFiles.Name = "btnAddFiles";
            btnAddFiles.Size = new System.Drawing.Size(80, 25);
            btnAddFiles.TabIndex = 3;
            btnAddFiles.Text = "Add Files...";
            btnAddFiles.UseVisualStyleBackColor = true;
            btnAddFiles.Click += BtnAddFiles_Click;
            
            // btnRemoveSelected
            btnRemoveSelected.Location = new System.Drawing.Point(370, 95);
            btnRemoveSelected.Name = "btnRemoveSelected";
            btnRemoveSelected.Size = new System.Drawing.Size(80, 25);
            btnRemoveSelected.TabIndex = 4;
            btnRemoveSelected.Text = "Remove";
            btnRemoveSelected.UseVisualStyleBackColor = true;
            btnRemoveSelected.Click += BtnRemoveSelected_Click;
            
            // btnMoveUp
            btnMoveUp.Location = new System.Drawing.Point(370, 130);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Size = new System.Drawing.Size(80, 25);
            btnMoveUp.TabIndex = 5;
            btnMoveUp.Text = "Move Up";
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += BtnMoveUp_Click;
            
            // btnMoveDown
            btnMoveDown.Location = new System.Drawing.Point(370, 160);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Size = new System.Drawing.Size(80, 25);
            btnMoveDown.TabIndex = 6;
            btnMoveDown.Text = "Move Down";
            btnMoveDown.UseVisualStyleBackColor = true;
            btnMoveDown.Click += BtnMoveDown_Click;
            
            // lblOutput
            lblOutput.Location = new System.Drawing.Point(8, 230);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(150, 15);
            lblOutput.TabIndex = 7;
            lblOutput.Text = "Output GIF file:";
            
            // txtOutputPath
            txtOutputPath.Location = new System.Drawing.Point(8, 250);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.ReadOnly = true;
            txtOutputPath.Size = new System.Drawing.Size(350, 23);
            txtOutputPath.TabIndex = 8;
            
            // btnBrowseOutput
            btnBrowseOutput.Location = new System.Drawing.Point(370, 250);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new System.Drawing.Size(80, 25);
            btnBrowseOutput.TabIndex = 9;
            btnBrowseOutput.Text = SteamGifCropper.Properties.Resources.Button_Browse;
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            
            // btnOK
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new System.Drawing.Point(295, 290);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 25);
            btnOK.TabIndex = 10;
            btnOK.Text = "Merge";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            
            // btnCancel
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(375, 290);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 25);
            btnCancel.TabIndex = 11;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            
            // MergeGifsDialog
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(460, 330);
            Controls.Add(lblInstructions);
            Controls.Add(lblGifFiles);
            Controls.Add(lstGifFiles);
            Controls.Add(btnAddFiles);
            Controls.Add(btnRemoveSelected);
            Controls.Add(btnMoveUp);
            Controls.Add(btnMoveDown);
            Controls.Add(lblOutput);
            Controls.Add(txtOutputPath);
            Controls.Add(btnBrowseOutput);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MergeGifsDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = SteamGifCropper.Properties.Resources.Title_MergeGifs;
            ResumeLayout(false);
            PerformLayout();
        }

        private void BtnAddFiles_Click(object sender, EventArgs e)
        {
            if (lstGifFiles.Items.Count >= 5)
            {
                MessageBox.Show(SteamGifCropper.Properties.Resources.Message_GifFileCount, 
                               SteamGifCropper.Properties.Resources.Title_Warning, 
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select GIF files to merge",
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        if (lstGifFiles.Items.Count >= 5) break;
                        
                        if (!lstGifFiles.Items.Cast<string>().Contains(fileName))
                        {
                            lstGifFiles.Items.Add(fileName);
                        }
                    }
                    
                    UpdateOutputPath();
                }
            }
        }

        private void BtnRemoveSelected_Click(object sender, EventArgs e)
        {
            if (lstGifFiles.SelectedIndex >= 0)
            {
                lstGifFiles.Items.RemoveAt(lstGifFiles.SelectedIndex);
                UpdateOutputPath();
            }
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstGifFiles.SelectedIndex;
            if (selectedIndex > 0)
            {
                var item = lstGifFiles.Items[selectedIndex];
                lstGifFiles.Items.RemoveAt(selectedIndex);
                lstGifFiles.Items.Insert(selectedIndex - 1, item);
                lstGifFiles.SelectedIndex = selectedIndex - 1;
            }
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            int selectedIndex = lstGifFiles.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < lstGifFiles.Items.Count - 1)
            {
                var item = lstGifFiles.Items[selectedIndex];
                lstGifFiles.Items.RemoveAt(selectedIndex);
                lstGifFiles.Items.Insert(selectedIndex + 1, item);
                lstGifFiles.SelectedIndex = selectedIndex + 1;
            }
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Save merged GIF as...",
                FileName = txtOutputPath.Text
            })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputPath.Text = saveFileDialog.FileName;
                }
            }
        }

        private void UpdateOutputPath()
        {
            if (lstGifFiles.Items.Count > 0)
            {
                string firstFile = lstGifFiles.Items[0].ToString();
                string directory = Path.GetDirectoryName(firstFile);
                string baseName = Path.GetFileNameWithoutExtension(firstFile);
                txtOutputPath.Text = Path.Combine(directory, $"{baseName}_merged.gif");
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (lstGifFiles.Items.Count < 2 || lstGifFiles.Items.Count > 5)
            {
                MessageBox.Show(SteamGifCropper.Properties.Resources.Message_GifFileCount, 
                               SteamGifCropper.Properties.Resources.Title_Warning, 
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (string filePath in lstGifFiles.Items.Cast<string>())
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show($"File not found: {Path.GetFileName(filePath)}", 
                                   SteamGifCropper.Properties.Resources.Title_Error, 
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (string.IsNullOrEmpty(txtOutputPath.Text))
            {
                MessageBox.Show("Please specify an output GIF file.", 
                               SteamGifCropper.Properties.Resources.Title_Warning, 
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var outputDir = Path.GetDirectoryName(txtOutputPath.Text);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Cannot create output directory:\n{ex.Message}", 
                                   SteamGifCropper.Properties.Resources.Title_Error, 
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            SelectedFilePaths = lstGifFiles.Items.Cast<string>().ToList();
            OutputFilePath = Path.GetFullPath(txtOutputPath.Text);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}