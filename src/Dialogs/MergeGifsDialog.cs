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
        public int PaletteSourceIndex { get; private set; }

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
        public CheckBox chkGIFMergeFasterPaletteProcess;
        private Label lblInstructions;
        private Label lblPaletteSource;
        public ComboBox comboBoxPaletteSource;

        public MergeGifsDialog()
        {
            SelectedFilePaths = new List<string>();
            InitializeComponent();
            ApplyTheme();
            // Initialize palette source combo box with empty state
            UpdatePaletteSourceOptions();
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
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
                    comboBox.ForeColor = System.Drawing.Color.White;
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
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = System.Drawing.SystemColors.Window;
                    comboBox.ForeColor = System.Drawing.SystemColors.WindowText;
                }
                
                if (control.HasChildren)
                {
                    ApplyLightThemeToControls(control.Controls);
                }
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MergeGifsDialog));
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
            chkGIFMergeFasterPaletteProcess = new CheckBox();
            lblPaletteSource = new Label();
            comboBoxPaletteSource = new ComboBox();
            SuspendLayout();
            // 
            // lblInstructions
            // 
            lblInstructions.Location = new System.Drawing.Point(14, 9);
            lblInstructions.Margin = new Padding(41, 0, 41, 0);
            lblInstructions.Name = "lblInstructions";
            lblInstructions.Size = new System.Drawing.Size(371, 22);
            lblInstructions.TabIndex = 0;
            lblInstructions.Text = SteamGifCropper.Properties.Resources.MergeDialog_Instructions;
            // 
            // lblGifFiles
            // 
            lblGifFiles.Location = new System.Drawing.Point(14, 42);
            lblGifFiles.Margin = new Padding(41, 0, 41, 0);
            lblGifFiles.Name = "lblGifFiles";
            lblGifFiles.Size = new System.Drawing.Size(108, 20);
            lblGifFiles.TabIndex = 1;
            lblGifFiles.Text = "GIF Files (2-5):";
            // 
            // lstGifFiles
            // 
            lstGifFiles.ItemHeight = 15;
            lstGifFiles.Location = new System.Drawing.Point(14, 61);
            lstGifFiles.Margin = new Padding(41, 19, 41, 19);
            lstGifFiles.Name = "lstGifFiles";
            lstGifFiles.Size = new System.Drawing.Size(420, 169);
            lstGifFiles.TabIndex = 2;
            // 
            // btnAddFiles
            // 
            btnAddFiles.Location = new System.Drawing.Point(440, 61);
            btnAddFiles.Margin = new Padding(41, 19, 41, 19);
            btnAddFiles.Name = "btnAddFiles";
            btnAddFiles.Size = new System.Drawing.Size(101, 28);
            btnAddFiles.TabIndex = 3;
            btnAddFiles.Text = SteamGifCropper.Properties.Resources.MergeDialog_AddFiles;
            btnAddFiles.UseVisualStyleBackColor = true;
            btnAddFiles.Click += BtnAddFiles_Click;
            // 
            // btnRemoveSelected
            // 
            btnRemoveSelected.Location = new System.Drawing.Point(440, 95);
            btnRemoveSelected.Margin = new Padding(41, 19, 41, 19);
            btnRemoveSelected.Name = "btnRemoveSelected";
            btnRemoveSelected.Size = new System.Drawing.Size(101, 25);
            btnRemoveSelected.TabIndex = 4;
            btnRemoveSelected.Text = SteamGifCropper.Properties.Resources.MergeDialog_Remove;
            btnRemoveSelected.UseVisualStyleBackColor = true;
            btnRemoveSelected.Click += BtnRemoveSelected_Click;
            // 
            // btnMoveUp
            // 
            btnMoveUp.Location = new System.Drawing.Point(440, 126);
            btnMoveUp.Margin = new Padding(41, 19, 41, 19);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Size = new System.Drawing.Size(101, 25);
            btnMoveUp.TabIndex = 5;
            btnMoveUp.Text = SteamGifCropper.Properties.Resources.MergeDialog_MoveUp;
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += BtnMoveUp_Click;
            // 
            // btnMoveDown
            // 
            btnMoveDown.Location = new System.Drawing.Point(440, 157);
            btnMoveDown.Margin = new Padding(41, 19, 41, 19);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Size = new System.Drawing.Size(101, 25);
            btnMoveDown.TabIndex = 6;
            btnMoveDown.Text = SteamGifCropper.Properties.Resources.MergeDialog_MoveDown;
            btnMoveDown.UseVisualStyleBackColor = true;
            btnMoveDown.Click += BtnMoveDown_Click;
            // 
            // lblOutput
            // 
            lblOutput.Location = new System.Drawing.Point(14, 293);
            lblOutput.Margin = new Padding(41, 0, 41, 0);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(133, 20);
            lblOutput.TabIndex = 8;
            lblOutput.Text = SteamGifCropper.Properties.Resources.MergeDialog_OutputFile;
            // 
            // txtOutputPath
            // 
            txtOutputPath.Location = new System.Drawing.Point(14, 311);
            txtOutputPath.Margin = new Padding(41, 19, 41, 19);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.ReadOnly = true;
            txtOutputPath.Size = new System.Drawing.Size(420, 23);
            txtOutputPath.TabIndex = 9;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new System.Drawing.Point(440, 308);
            btnBrowseOutput.Margin = new Padding(41, 19, 41, 19);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new System.Drawing.Size(101, 26);
            btnBrowseOutput.TabIndex = 10;
            btnBrowseOutput.Text = SteamGifCropper.Properties.Resources.Button_Browse;
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            // 
            // btnOK
            // 
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new System.Drawing.Point(334, 347);
            btnOK.Margin = new Padding(41, 19, 41, 19);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(100, 25);
            btnOK.TabIndex = 11;
            btnOK.Text = SteamGifCropper.Properties.Resources.MergeDialog_Merge;
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(440, 347);
            btnCancel.Margin = new Padding(41, 19, 41, 19);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(100, 25);
            btnCancel.TabIndex = 12;
            btnCancel.Text = SteamGifCropper.Properties.Resources.MergeDialog_Cancel;
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // chkGIFMergeFasterPaletteProcess
            // 
            chkGIFMergeFasterPaletteProcess.AutoSize = true;
            chkGIFMergeFasterPaletteProcess.Location = new System.Drawing.Point(14, 234);
            chkGIFMergeFasterPaletteProcess.Name = "chkGIFMergeFasterPaletteProcess";
            chkGIFMergeFasterPaletteProcess.Size = new System.Drawing.Size(243, 19);
            chkGIFMergeFasterPaletteProcess.TabIndex = 7;
            chkGIFMergeFasterPaletteProcess.Text = SteamGifCropper.Properties.Resources.CheckBox_FasterPalette;
            chkGIFMergeFasterPaletteProcess.UseVisualStyleBackColor = true;
            // 
            // lblPaletteSource
            // 
            lblPaletteSource.AutoSize = true;
            lblPaletteSource.Location = new System.Drawing.Point(14, 265);
            lblPaletteSource.Name = "lblPaletteSource";
            lblPaletteSource.Size = new System.Drawing.Size(89, 15);
            lblPaletteSource.TabIndex = 13;
            lblPaletteSource.Text = SteamGifCropper.Properties.Resources.MergeDialog_PaletteSource;
            // 
            // comboBoxPaletteSource
            // 
            comboBoxPaletteSource.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxPaletteSource.FormattingEnabled = true;
            comboBoxPaletteSource.Location = new System.Drawing.Point(151, 262);
            comboBoxPaletteSource.Name = "comboBoxPaletteSource";
            comboBoxPaletteSource.Size = new System.Drawing.Size(189, 23);
            comboBoxPaletteSource.TabIndex = 14;
            // 
            // MergeGifsDialog
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(556, 379);
            Controls.Add(comboBoxPaletteSource);
            Controls.Add(chkGIFMergeFasterPaletteProcess);
            Controls.Add(lstGifFiles);
            Controls.Add(lblInstructions);
            Controls.Add(lblGifFiles);
            Controls.Add(btnAddFiles);
            Controls.Add(btnRemoveSelected);
            Controls.Add(btnMoveUp);
            Controls.Add(btnMoveDown);
            Controls.Add(txtOutputPath);
            Controls.Add(btnBrowseOutput);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            Controls.Add(lblOutput);
            Controls.Add(lblPaletteSource);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(41, 19, 41, 19);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MergeGifsDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = SteamGifCropper.Properties.Resources.MergeDialog_Title;
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
                Filter = SteamGifCropper.Properties.Resources.MergeDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.MergeDialog_SelectGifFiles,
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
                    UpdatePaletteSourceOptions();
                }
            }
        }

        private void BtnRemoveSelected_Click(object sender, EventArgs e)
        {
            if (lstGifFiles.SelectedIndex >= 0)
            {
                lstGifFiles.Items.RemoveAt(lstGifFiles.SelectedIndex);
                UpdateOutputPath();
                UpdatePaletteSourceOptions();
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
                UpdatePaletteSourceOptions();
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
                UpdatePaletteSourceOptions();
            }
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.MergeDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.MergeDialog_SaveMergedGif,
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

        private void UpdatePaletteSourceOptions()
        {
            int currentSelection = comboBoxPaletteSource.SelectedIndex;
            comboBoxPaletteSource.Items.Clear();
            
            for (int i = 0; i < lstGifFiles.Items.Count; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(lstGifFiles.Items[i].ToString());
                comboBoxPaletteSource.Items.Add(string.Format(SteamGifCropper.Properties.Resources.MergeDialog_GifNumberFormat, i + 1, fileName));
            }
            
            // Restore selection or default to first item
            if (comboBoxPaletteSource.Items.Count > 0)
            {
                if (currentSelection >= 0 && currentSelection < comboBoxPaletteSource.Items.Count)
                {
                    comboBoxPaletteSource.SelectedIndex = currentSelection;
                }
                else
                {
                    comboBoxPaletteSource.SelectedIndex = 0; // Default to first GIF
                }
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
                    MessageBox.Show(
                        string.Format(SteamGifCropper.Properties.Resources.MergeDialog_FileNotFound,
                                      Path.GetFileName(filePath)),
                        SteamGifCropper.Properties.Resources.Title_Error,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (string.IsNullOrEmpty(txtOutputPath.Text))
            {
                MessageBox.Show(
                    SteamGifCropper.Properties.Resources.MergeDialog_RequireOutput,
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
                    MessageBox.Show(
                        $"{SteamGifCropper.Properties.Resources.MergeDialog_CannotCreateDir}\n{ex.Message}",
                        SteamGifCropper.Properties.Resources.Title_Error,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            SelectedFilePaths = lstGifFiles.Items.Cast<string>().ToList();
            OutputFilePath = Path.GetFullPath(txtOutputPath.Text);
            PaletteSourceIndex = Math.Max(0, comboBoxPaletteSource.SelectedIndex); // Default to 0 if no selection

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}