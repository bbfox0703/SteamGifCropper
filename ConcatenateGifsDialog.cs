using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public partial class ConcatenateGifsDialog : Form
    {
        public List<string> SelectedFilePaths { get; private set; }
        public string OutputFilePath { get; private set; }
        public GifConcatenationSettings Settings { get; private set; }

        // UI Controls
        private Label lblInstructions;
        private Label lblGifFiles;
        private ListBox lstGifFiles;
        private Button btnAddFiles;
        private Button btnRemoveSelected;
        private Button btnMoveUp;
        private Button btnMoveDown;

        // FPS Settings
        private GroupBox grpFpsSettings;
        private RadioButton rbFpsAutoHighest;
        private RadioButton rbFpsUseReference;
        private RadioButton rbFpsCustom;
        private ComboBox cmbFpsReference;
        private NumericUpDown nudCustomFps;
        private Label lblCustomFps;

        // Palette Settings
        private GroupBox grpPaletteSettings;
        private RadioButton rbPaletteAutoMerge;
        private RadioButton rbPaletteUseReference;
        private ComboBox cmbPaletteReference;
        private CheckBox chkStrongPaletteWeighting;

        // Output Settings
        private Label lblOutputFile;
        private TextBox txtOutputFile;
        private Button btnBrowseOutput;

        // General Settings
        private CheckBox chkUnifyDimensions;
        private CheckBox chkUseFasterPalette;
        private CheckBox chkUseGifsicleOptimization;

        // Action Buttons
        private Button btnOK;
        private Button btnCancel;

        public ConcatenateGifsDialog()
        {
            InitializeComponent();
            InitializeSettings();
            ApplyTheme();
            UpdateReferenceComboBoxes();
        }

        private void InitializeSettings()
        {
            Settings = new GifConcatenationSettings();
            SelectedFilePaths = new List<string>();
        }

        private void ApplyTheme()
        {
            bool isDarkMode = WindowsThemeManager.IsDarkModeEnabled();
            
            if (isDarkMode)
            {
                BackColor = Color.FromArgb(32, 32, 32);
                ForeColor = Color.White;
                ApplyDarkThemeToControls(this.Controls);
            }
            else
            {
                BackColor = SystemColors.Control;
                ForeColor = SystemColors.ControlText;
                ApplyLightThemeToControls(this.Controls);
            }

            if (Environment.OSVersion.Version.Major >= 10)
            {
                WindowsThemeManager.SetDarkModeForWindow(this.Handle, isDarkMode);
            }
        }

        private void ApplyDarkThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Label || control is RadioButton || control is CheckBox)
                {
                    control.BackColor = Color.Transparent;
                    control.ForeColor = Color.White;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = Color.FromArgb(64, 64, 64);
                    textBox.ForeColor = Color.White;
                }
                else if (control is Button button)
                {
                    button.BackColor = Color.FromArgb(64, 64, 64);
                    button.ForeColor = Color.White;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = Color.FromArgb(128, 128, 128);
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = Color.FromArgb(64, 64, 64);
                    listBox.ForeColor = Color.White;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = Color.FromArgb(64, 64, 64);
                    comboBox.ForeColor = Color.White;
                }
                else if (control is NumericUpDown numeric)
                {
                    numeric.BackColor = Color.FromArgb(64, 64, 64);
                    numeric.ForeColor = Color.White;
                }
                else if (control is GroupBox groupBox)
                {
                    groupBox.ForeColor = Color.White;
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
                if (control is Label || control is RadioButton || control is CheckBox)
                {
                    control.BackColor = Color.Transparent;
                    control.ForeColor = SystemColors.ControlText;
                }
                else if (control is TextBox textBox)
                {
                    textBox.BackColor = SystemColors.Window;
                    textBox.ForeColor = SystemColors.WindowText;
                }
                else if (control is Button button)
                {
                    button.BackColor = SystemColors.Control;
                    button.ForeColor = SystemColors.ControlText;
                    button.FlatStyle = FlatStyle.Standard;
                    button.UseVisualStyleBackColor = true;
                }
                else if (control is ListBox listBox)
                {
                    listBox.BackColor = SystemColors.Window;
                    listBox.ForeColor = SystemColors.WindowText;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = SystemColors.Window;
                    comboBox.ForeColor = SystemColors.WindowText;
                }
                else if (control is NumericUpDown numeric)
                {
                    numeric.BackColor = SystemColors.Window;
                    numeric.ForeColor = SystemColors.WindowText;
                }
                else if (control is GroupBox groupBox)
                {
                    groupBox.ForeColor = SystemColors.ControlText;
                }
                
                if (control.HasChildren)
                {
                    ApplyLightThemeToControls(control.Controls);
                }
            }
        }

        private void InitializeComponent()
        {
            // Initialize all controls
            lblInstructions = new Label();
            lblGifFiles = new Label();
            lstGifFiles = new ListBox();
            btnAddFiles = new Button();
            btnRemoveSelected = new Button();
            btnMoveUp = new Button();
            btnMoveDown = new Button();

            grpFpsSettings = new GroupBox();
            rbFpsAutoHighest = new RadioButton();
            rbFpsUseReference = new RadioButton();
            rbFpsCustom = new RadioButton();
            cmbFpsReference = new ComboBox();
            nudCustomFps = new NumericUpDown();
            lblCustomFps = new Label();

            grpPaletteSettings = new GroupBox();
            rbPaletteAutoMerge = new RadioButton();
            rbPaletteUseReference = new RadioButton();
            cmbPaletteReference = new ComboBox();
            chkStrongPaletteWeighting = new CheckBox();

            lblOutputFile = new Label();
            txtOutputFile = new TextBox();
            btnBrowseOutput = new Button();

            chkUnifyDimensions = new CheckBox();
            chkUseFasterPalette = new CheckBox();
            chkUseGifsicleOptimization = new CheckBox();

            btnOK = new Button();
            btnCancel = new Button();

            SuspendLayout();

            // lblInstructions
            lblInstructions.AutoSize = true;
            lblInstructions.Location = new Point(12, 9);
            lblInstructions.Size = new Size(400, 15);
            lblInstructions.TabIndex = 0;
            lblInstructions.Text = "Select GIF files to concatenate (in order):";

            // lblGifFiles
            lblGifFiles.AutoSize = true;
            lblGifFiles.Location = new Point(12, 35);
            lblGifFiles.Size = new Size(120, 15);
            lblGifFiles.TabIndex = 1;
            lblGifFiles.Text = "GIF Files (2 or more):";

            // lstGifFiles
            lstGifFiles.Location = new Point(12, 55);
            lstGifFiles.Size = new Size(400, 120);
            lstGifFiles.TabIndex = 2;
            lstGifFiles.SelectedIndexChanged += LstGifFiles_SelectedIndexChanged;

            // btnAddFiles
            btnAddFiles.Location = new Point(420, 55);
            btnAddFiles.Size = new Size(80, 25);
            btnAddFiles.TabIndex = 3;
            btnAddFiles.Text = "Add Files...";
            btnAddFiles.UseVisualStyleBackColor = true;
            btnAddFiles.Click += BtnAddFiles_Click;

            // btnRemoveSelected
            btnRemoveSelected.Location = new Point(420, 85);
            btnRemoveSelected.Size = new Size(80, 25);
            btnRemoveSelected.TabIndex = 4;
            btnRemoveSelected.Text = "Remove";
            btnRemoveSelected.UseVisualStyleBackColor = true;
            btnRemoveSelected.Click += BtnRemoveSelected_Click;

            // btnMoveUp
            btnMoveUp.Location = new Point(420, 115);
            btnMoveUp.Size = new Size(80, 25);
            btnMoveUp.TabIndex = 5;
            btnMoveUp.Text = "Move Up";
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += BtnMoveUp_Click;

            // btnMoveDown
            btnMoveDown.Location = new Point(420, 145);
            btnMoveDown.Size = new Size(80, 25);
            btnMoveDown.TabIndex = 6;
            btnMoveDown.Text = "Move Down";
            btnMoveDown.UseVisualStyleBackColor = true;
            btnMoveDown.Click += BtnMoveDown_Click;

            // grpFpsSettings
            grpFpsSettings.Location = new Point(12, 185);
            grpFpsSettings.Size = new Size(240, 120);
            grpFpsSettings.TabIndex = 7;
            grpFpsSettings.TabStop = false;
            grpFpsSettings.Text = "FPS Settings";

            // rbFpsAutoHighest
            rbFpsAutoHighest.AutoSize = true;
            rbFpsAutoHighest.Checked = true;
            rbFpsAutoHighest.Location = new Point(6, 22);
            rbFpsAutoHighest.Size = new Size(120, 19);
            rbFpsAutoHighest.TabIndex = 0;
            rbFpsAutoHighest.TabStop = true;
            rbFpsAutoHighest.Text = "Auto (Use Highest)";
            rbFpsAutoHighest.UseVisualStyleBackColor = true;
            rbFpsAutoHighest.CheckedChanged += RbFps_CheckedChanged;

            // rbFpsUseReference
            rbFpsUseReference.AutoSize = true;
            rbFpsUseReference.Location = new Point(6, 47);
            rbFpsUseReference.Size = new Size(120, 19);
            rbFpsUseReference.TabIndex = 1;
            rbFpsUseReference.Text = "Use Reference GIF:";
            rbFpsUseReference.UseVisualStyleBackColor = true;
            rbFpsUseReference.CheckedChanged += RbFps_CheckedChanged;

            // cmbFpsReference
            cmbFpsReference.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFpsReference.Enabled = false;
            cmbFpsReference.Location = new Point(130, 45);
            cmbFpsReference.Size = new Size(100, 23);
            cmbFpsReference.TabIndex = 2;

            // rbFpsCustom
            rbFpsCustom.AutoSize = true;
            rbFpsCustom.Location = new Point(6, 72);
            rbFpsCustom.Size = new Size(100, 19);
            rbFpsCustom.TabIndex = 3;
            rbFpsCustom.Text = "Custom FPS:";
            rbFpsCustom.UseVisualStyleBackColor = true;
            rbFpsCustom.CheckedChanged += RbFps_CheckedChanged;

            // nudCustomFps
            nudCustomFps.Enabled = false;
            nudCustomFps.Location = new Point(130, 70);
            nudCustomFps.Maximum = 60;
            nudCustomFps.Minimum = 1;
            nudCustomFps.Size = new Size(60, 23);
            nudCustomFps.TabIndex = 4;
            nudCustomFps.Value = 30;

            // lblCustomFps
            lblCustomFps.AutoSize = true;
            lblCustomFps.Location = new Point(195, 72);
            lblCustomFps.Size = new Size(25, 15);
            lblCustomFps.TabIndex = 5;
            lblCustomFps.Text = "fps";

            grpFpsSettings.Controls.Add(rbFpsAutoHighest);
            grpFpsSettings.Controls.Add(rbFpsUseReference);
            grpFpsSettings.Controls.Add(cmbFpsReference);
            grpFpsSettings.Controls.Add(rbFpsCustom);
            grpFpsSettings.Controls.Add(nudCustomFps);
            grpFpsSettings.Controls.Add(lblCustomFps);

            // grpPaletteSettings
            grpPaletteSettings.Location = new Point(260, 185);
            grpPaletteSettings.Size = new Size(240, 120);
            grpPaletteSettings.TabIndex = 8;
            grpPaletteSettings.TabStop = false;
            grpPaletteSettings.Text = "Palette Settings";

            // rbPaletteAutoMerge
            rbPaletteAutoMerge.AutoSize = true;
            rbPaletteAutoMerge.Checked = true;
            rbPaletteAutoMerge.Location = new Point(6, 22);
            rbPaletteAutoMerge.Size = new Size(120, 19);
            rbPaletteAutoMerge.TabIndex = 0;
            rbPaletteAutoMerge.TabStop = true;
            rbPaletteAutoMerge.Text = "Auto Merge";
            rbPaletteAutoMerge.UseVisualStyleBackColor = true;
            rbPaletteAutoMerge.CheckedChanged += RbPalette_CheckedChanged;

            // rbPaletteUseReference
            rbPaletteUseReference.AutoSize = true;
            rbPaletteUseReference.Location = new Point(6, 47);
            rbPaletteUseReference.Size = new Size(120, 19);
            rbPaletteUseReference.TabIndex = 1;
            rbPaletteUseReference.Text = "Use Reference GIF:";
            rbPaletteUseReference.UseVisualStyleBackColor = true;
            rbPaletteUseReference.CheckedChanged += RbPalette_CheckedChanged;

            // cmbPaletteReference
            cmbPaletteReference.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaletteReference.Enabled = false;
            cmbPaletteReference.Location = new Point(130, 45);
            cmbPaletteReference.Size = new Size(100, 23);
            cmbPaletteReference.TabIndex = 2;

            // chkStrongPaletteWeighting
            chkStrongPaletteWeighting.AutoSize = true;
            chkStrongPaletteWeighting.Checked = true;
            chkStrongPaletteWeighting.Location = new Point(6, 72);
            chkStrongPaletteWeighting.Size = new Size(200, 19);
            chkStrongPaletteWeighting.TabIndex = 3;
            chkStrongPaletteWeighting.Text = "8x weight for reference palette";
            chkStrongPaletteWeighting.UseVisualStyleBackColor = true;

            grpPaletteSettings.Controls.Add(rbPaletteAutoMerge);
            grpPaletteSettings.Controls.Add(rbPaletteUseReference);
            grpPaletteSettings.Controls.Add(cmbPaletteReference);
            grpPaletteSettings.Controls.Add(chkStrongPaletteWeighting);

            // lblOutputFile
            lblOutputFile.AutoSize = true;
            lblOutputFile.Location = new Point(12, 320);
            lblOutputFile.Size = new Size(100, 15);
            lblOutputFile.TabIndex = 9;
            lblOutputFile.Text = "Output GIF file:";

            // txtOutputFile
            txtOutputFile.Location = new Point(12, 340);
            txtOutputFile.Size = new Size(400, 23);
            txtOutputFile.TabIndex = 10;

            // btnBrowseOutput
            btnBrowseOutput.Location = new Point(420, 340);
            btnBrowseOutput.Size = new Size(80, 23);
            btnBrowseOutput.TabIndex = 11;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            // chkUnifyDimensions
            chkUnifyDimensions.AutoSize = true;
            chkUnifyDimensions.Checked = true;
            chkUnifyDimensions.Location = new Point(12, 375);
            chkUnifyDimensions.Size = new Size(200, 19);
            chkUnifyDimensions.TabIndex = 12;
            chkUnifyDimensions.Text = "Unify dimensions (resize to largest)";
            chkUnifyDimensions.UseVisualStyleBackColor = true;

            // chkUseFasterPalette
            chkUseFasterPalette.AutoSize = true;
            chkUseFasterPalette.Location = new Point(220, 375);
            chkUseFasterPalette.Size = new Size(180, 19);
            chkUseFasterPalette.TabIndex = 13;
            chkUseFasterPalette.Text = "Faster palette (lower quality)";
            chkUseFasterPalette.UseVisualStyleBackColor = true;

            // chkUseGifsicleOptimization
            chkUseGifsicleOptimization.AutoSize = true;
            chkUseGifsicleOptimization.Location = new Point(12, 400);
            chkUseGifsicleOptimization.Size = new Size(160, 19);
            chkUseGifsicleOptimization.TabIndex = 14;
            chkUseGifsicleOptimization.Text = "Use gifsicle optimization";
            chkUseGifsicleOptimization.UseVisualStyleBackColor = true;

            // btnOK
            btnOK.Location = new Point(350, 430);
            btnOK.Size = new Size(75, 25);
            btnOK.TabIndex = 15;
            btnOK.Text = "Concatenate";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;

            // btnCancel
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(430, 430);
            btnCancel.Size = new Size(75, 25);
            btnCancel.TabIndex = 16;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;

            // ConcatenateGifsDialog
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(520, 470);
            Controls.Add(lblInstructions);
            Controls.Add(lblGifFiles);
            Controls.Add(lstGifFiles);
            Controls.Add(btnAddFiles);
            Controls.Add(btnRemoveSelected);
            Controls.Add(btnMoveUp);
            Controls.Add(btnMoveDown);
            Controls.Add(grpFpsSettings);
            Controls.Add(grpPaletteSettings);
            Controls.Add(lblOutputFile);
            Controls.Add(txtOutputFile);
            Controls.Add(btnBrowseOutput);
            Controls.Add(chkUnifyDimensions);
            Controls.Add(chkUseFasterPalette);
            Controls.Add(chkUseGifsicleOptimization);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConcatenateGifsDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Concatenate GIF Files";
            ResumeLayout(false);
            PerformLayout();
        }

        private void BtnAddFiles_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select GIF files to concatenate",
                Multiselect = true
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        if (!lstGifFiles.Items.Cast<string>().Contains(fileName))
                        {
                            lstGifFiles.Items.Add(fileName);
                        }
                    }
                    
                    UpdateReferenceComboBoxes();
                    UpdateOutputFileName();
                }
            }
        }

        private void BtnRemoveSelected_Click(object sender, EventArgs e)
        {
            if (lstGifFiles.SelectedIndex >= 0)
            {
                lstGifFiles.Items.RemoveAt(lstGifFiles.SelectedIndex);
                UpdateReferenceComboBoxes();
                UpdateOutputFileName();
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
                UpdateReferenceComboBoxes();
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
                UpdateReferenceComboBoxes();
            }
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Save concatenated GIF as...",
                FileName = txtOutputFile.Text
            })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputFile.Text = saveFileDialog.FileName;
                }
            }
        }

        private void LstGifFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemoveSelected.Enabled = lstGifFiles.SelectedIndex >= 0;
            btnMoveUp.Enabled = lstGifFiles.SelectedIndex > 0;
            btnMoveDown.Enabled = lstGifFiles.SelectedIndex >= 0 && lstGifFiles.SelectedIndex < lstGifFiles.Items.Count - 1;
        }

        private void RbFps_CheckedChanged(object sender, EventArgs e)
        {
            cmbFpsReference.Enabled = rbFpsUseReference.Checked;
            nudCustomFps.Enabled = rbFpsCustom.Checked;
        }

        private void RbPalette_CheckedChanged(object sender, EventArgs e)
        {
            cmbPaletteReference.Enabled = rbPaletteUseReference.Checked;
        }

        private void UpdateReferenceComboBoxes()
        {
            int fpsSelection = cmbFpsReference.SelectedIndex;
            int paletteSelection = cmbPaletteReference.SelectedIndex;

            cmbFpsReference.Items.Clear();
            cmbPaletteReference.Items.Clear();

            for (int i = 0; i < lstGifFiles.Items.Count; i++)
            {
                string fileName = Path.GetFileNameWithoutExtension(lstGifFiles.Items[i].ToString());
                string displayText = $"GIF {i + 1}: {fileName}";
                cmbFpsReference.Items.Add(displayText);
                cmbPaletteReference.Items.Add(displayText);
            }

            // Restore selections or default to first item
            if (cmbFpsReference.Items.Count > 0)
            {
                cmbFpsReference.SelectedIndex = (fpsSelection >= 0 && fpsSelection < cmbFpsReference.Items.Count) ? 
                                              fpsSelection : 0;
            }

            if (cmbPaletteReference.Items.Count > 0)
            {
                cmbPaletteReference.SelectedIndex = (paletteSelection >= 0 && paletteSelection < cmbPaletteReference.Items.Count) ? 
                                                   paletteSelection : 0;
            }
        }

        private void UpdateOutputFileName()
        {
            if (lstGifFiles.Items.Count > 0 && string.IsNullOrEmpty(txtOutputFile.Text))
            {
                string firstFile = lstGifFiles.Items[0].ToString();
                string directory = Path.GetDirectoryName(firstFile);
                string baseName = Path.GetFileNameWithoutExtension(firstFile);
                txtOutputFile.Text = Path.Combine(directory, $"{baseName}_concatenated.gif");
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate inputs
            if (lstGifFiles.Items.Count < 2)
            {
                MessageBox.Show("Please select at least 2 GIF files to concatenate.",
                               "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtOutputFile.Text))
            {
                MessageBox.Show("Please specify an output file.",
                               "Output Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Prepare settings
            Settings.GifFilePaths = lstGifFiles.Items.Cast<string>().ToList();
            Settings.OutputFilePath = txtOutputFile.Text;

            // FPS settings
            if (rbFpsAutoHighest.Checked)
                Settings.FpsMode = FpsUnificationMode.AutoHighest;
            else if (rbFpsUseReference.Checked)
            {
                Settings.FpsMode = FpsUnificationMode.UseReference;
                Settings.ReferenceFpsGifIndex = cmbFpsReference.SelectedIndex;
            }
            else if (rbFpsCustom.Checked)
            {
                Settings.FpsMode = FpsUnificationMode.Custom;
                Settings.CustomFps = (int)nudCustomFps.Value;
            }

            // Palette settings
            if (rbPaletteAutoMerge.Checked)
                Settings.PaletteMode = PaletteUnificationMode.AutoMerge;
            else if (rbPaletteUseReference.Checked)
            {
                Settings.PaletteMode = PaletteUnificationMode.UseReference;
                Settings.ReferencePaletteGifIndex = cmbPaletteReference.SelectedIndex;
            }

            Settings.UseStrongPaletteWeighting = chkStrongPaletteWeighting.Checked;
            Settings.UnifyDimensions = chkUnifyDimensions.Checked;
            Settings.UseFasterPalette = chkUseFasterPalette.Checked;
            Settings.UseGifsicleOptimization = chkUseGifsicleOptimization.Checked;

            // Prepare output properties
            SelectedFilePaths = Settings.GifFilePaths;
            OutputFilePath = Settings.OutputFilePath;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}