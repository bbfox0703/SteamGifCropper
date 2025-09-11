using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageMagick;

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

        // Transition Settings
        private GroupBox grpTransitionSettings;
        private RadioButton rbTransitionNone;
        private RadioButton rbTransitionFade;
        private RadioButton rbTransitionSlide;
        private RadioButton rbTransitionZoom;
        private RadioButton rbTransitionDissolve;
        private ComboBox cmbSlideDirection;
        private ComboBox cmbZoomType;
        private NumericUpDown nudTransitionDuration;
        private Label lblTransitionDuration;
        private Label lblSeconds;
        private Button btnPreviewTransition;

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

            grpTransitionSettings = new GroupBox();
            rbTransitionNone = new RadioButton();
            rbTransitionFade = new RadioButton();
            rbTransitionSlide = new RadioButton();
            rbTransitionZoom = new RadioButton();
            rbTransitionDissolve = new RadioButton();
            cmbSlideDirection = new ComboBox();
            cmbZoomType = new ComboBox();
            nudTransitionDuration = new NumericUpDown();
            lblTransitionDuration = new Label();
            lblSeconds = new Label();
            btnPreviewTransition = new Button();

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

            // grpTransitionSettings
            grpTransitionSettings.Location = new Point(12, 315);
            grpTransitionSettings.Size = new Size(488, 120);
            grpTransitionSettings.TabIndex = 17;
            grpTransitionSettings.TabStop = false;
            grpTransitionSettings.Text = "Transition Settings";

            // rbTransitionNone
            rbTransitionNone.AutoSize = true;
            rbTransitionNone.Checked = true;
            rbTransitionNone.Location = new Point(6, 22);
            rbTransitionNone.Size = new Size(80, 19);
            rbTransitionNone.TabIndex = 0;
            rbTransitionNone.TabStop = true;
            rbTransitionNone.Text = "No Transition";
            rbTransitionNone.UseVisualStyleBackColor = true;
            rbTransitionNone.CheckedChanged += RbTransition_CheckedChanged;

            // rbTransitionFade
            rbTransitionFade.AutoSize = true;
            rbTransitionFade.Location = new Point(100, 22);
            rbTransitionFade.Size = new Size(50, 19);
            rbTransitionFade.TabIndex = 1;
            rbTransitionFade.Text = "Fade";
            rbTransitionFade.UseVisualStyleBackColor = true;
            rbTransitionFade.CheckedChanged += RbTransition_CheckedChanged;

            // rbTransitionSlide
            rbTransitionSlide.AutoSize = true;
            rbTransitionSlide.Location = new Point(160, 22);
            rbTransitionSlide.Size = new Size(50, 19);
            rbTransitionSlide.TabIndex = 2;
            rbTransitionSlide.Text = "Slide";
            rbTransitionSlide.UseVisualStyleBackColor = true;
            rbTransitionSlide.CheckedChanged += RbTransition_CheckedChanged;

            // rbTransitionZoom
            rbTransitionZoom.AutoSize = true;
            rbTransitionZoom.Location = new Point(220, 22);
            rbTransitionZoom.Size = new Size(55, 19);
            rbTransitionZoom.TabIndex = 3;
            rbTransitionZoom.Text = "Zoom";
            rbTransitionZoom.UseVisualStyleBackColor = true;
            rbTransitionZoom.CheckedChanged += RbTransition_CheckedChanged;

            // rbTransitionDissolve
            rbTransitionDissolve.AutoSize = true;
            rbTransitionDissolve.Location = new Point(285, 22);
            rbTransitionDissolve.Size = new Size(70, 19);
            rbTransitionDissolve.TabIndex = 4;
            rbTransitionDissolve.Text = "Dissolve";
            rbTransitionDissolve.UseVisualStyleBackColor = true;
            rbTransitionDissolve.CheckedChanged += RbTransition_CheckedChanged;

            // cmbSlideDirection
            cmbSlideDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSlideDirection.Enabled = false;
            cmbSlideDirection.Location = new Point(160, 47);
            cmbSlideDirection.Size = new Size(80, 23);
            cmbSlideDirection.TabIndex = 5;
            cmbSlideDirection.Items.AddRange(new string[] { "Left", "Right", "Up", "Down" });
            cmbSlideDirection.SelectedIndex = 0;

            // cmbZoomType
            cmbZoomType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbZoomType.Enabled = false;
            cmbZoomType.Location = new Point(220, 47);
            cmbZoomType.Size = new Size(80, 23);
            cmbZoomType.TabIndex = 6;
            cmbZoomType.Items.AddRange(new string[] { "Zoom In", "Zoom Out" });
            cmbZoomType.SelectedIndex = 0;

            // lblTransitionDuration
            lblTransitionDuration.AutoSize = true;
            lblTransitionDuration.Location = new Point(6, 77);
            lblTransitionDuration.Size = new Size(120, 15);
            lblTransitionDuration.TabIndex = 7;
            lblTransitionDuration.Text = "Transition Duration:";

            // nudTransitionDuration
            nudTransitionDuration.Location = new Point(130, 75);
            nudTransitionDuration.Minimum = new decimal(new int[] { 1, 0, 0, 65536 }); // 0.1
            nudTransitionDuration.Maximum = new decimal(new int[] { 30, 0, 0, 65536 }); // 3.0
            nudTransitionDuration.DecimalPlaces = 1;
            nudTransitionDuration.Increment = new decimal(new int[] { 1, 0, 0, 65536 }); // 0.1
            nudTransitionDuration.Size = new Size(60, 23);
            nudTransitionDuration.TabIndex = 8;
            nudTransitionDuration.Value = new decimal(new int[] { 5, 0, 0, 65536 }); // 0.5

            // lblSeconds
            lblSeconds.AutoSize = true;
            lblSeconds.Location = new Point(195, 77);
            lblSeconds.Size = new Size(50, 15);
            lblSeconds.TabIndex = 9;
            lblSeconds.Text = "seconds";

            // btnPreviewTransition
            btnPreviewTransition.Location = new Point(380, 75);
            btnPreviewTransition.Size = new Size(100, 25);
            btnPreviewTransition.TabIndex = 10;
            btnPreviewTransition.Text = "Preview Transition";
            btnPreviewTransition.UseVisualStyleBackColor = true;
            btnPreviewTransition.Enabled = false;
            btnPreviewTransition.Click += BtnPreviewTransition_Click;

            grpTransitionSettings.Controls.Add(rbTransitionNone);
            grpTransitionSettings.Controls.Add(rbTransitionFade);
            grpTransitionSettings.Controls.Add(rbTransitionSlide);
            grpTransitionSettings.Controls.Add(rbTransitionZoom);
            grpTransitionSettings.Controls.Add(rbTransitionDissolve);
            grpTransitionSettings.Controls.Add(cmbSlideDirection);
            grpTransitionSettings.Controls.Add(cmbZoomType);
            grpTransitionSettings.Controls.Add(lblTransitionDuration);
            grpTransitionSettings.Controls.Add(nudTransitionDuration);
            grpTransitionSettings.Controls.Add(lblSeconds);
            grpTransitionSettings.Controls.Add(btnPreviewTransition);

            // lblOutputFile
            lblOutputFile.AutoSize = true;
            lblOutputFile.Location = new Point(12, 445);
            lblOutputFile.Size = new Size(100, 15);
            lblOutputFile.TabIndex = 18;
            lblOutputFile.Text = "Output GIF file:";

            // txtOutputFile
            txtOutputFile.Location = new Point(12, 465);
            txtOutputFile.Size = new Size(400, 23);
            txtOutputFile.TabIndex = 19;

            // btnBrowseOutput
            btnBrowseOutput.Location = new Point(420, 465);
            btnBrowseOutput.Size = new Size(80, 23);
            btnBrowseOutput.TabIndex = 20;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            // chkUnifyDimensions
            chkUnifyDimensions.AutoSize = true;
            chkUnifyDimensions.Checked = true;
            chkUnifyDimensions.Location = new Point(12, 500);
            chkUnifyDimensions.Size = new Size(200, 19);
            chkUnifyDimensions.TabIndex = 21;
            chkUnifyDimensions.Text = "Unify dimensions (resize to largest)";
            chkUnifyDimensions.UseVisualStyleBackColor = true;

            // chkUseFasterPalette
            chkUseFasterPalette.AutoSize = true;
            chkUseFasterPalette.Location = new Point(220, 500);
            chkUseFasterPalette.Size = new Size(180, 19);
            chkUseFasterPalette.TabIndex = 22;
            chkUseFasterPalette.Text = "Faster palette (lower quality)";
            chkUseFasterPalette.UseVisualStyleBackColor = true;

            // chkUseGifsicleOptimization
            chkUseGifsicleOptimization.AutoSize = true;
            chkUseGifsicleOptimization.Location = new Point(12, 525);
            chkUseGifsicleOptimization.Size = new Size(160, 19);
            chkUseGifsicleOptimization.TabIndex = 23;
            chkUseGifsicleOptimization.Text = "Use gifsicle optimization";
            chkUseGifsicleOptimization.UseVisualStyleBackColor = true;

            // btnOK
            btnOK.Location = new Point(350, 555);
            btnOK.Size = new Size(75, 25);
            btnOK.TabIndex = 24;
            btnOK.Text = "Concatenate";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;

            // btnCancel
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(430, 555);
            btnCancel.Size = new Size(75, 25);
            btnCancel.TabIndex = 25;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;

            // ConcatenateGifsDialog
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(520, 595);
            Controls.Add(lblInstructions);
            Controls.Add(lblGifFiles);
            Controls.Add(lstGifFiles);
            Controls.Add(btnAddFiles);
            Controls.Add(btnRemoveSelected);
            Controls.Add(btnMoveUp);
            Controls.Add(btnMoveDown);
            Controls.Add(grpFpsSettings);
            Controls.Add(grpPaletteSettings);
            Controls.Add(grpTransitionSettings);
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

        private void RbTransition_CheckedChanged(object sender, EventArgs e)
        {
            cmbSlideDirection.Enabled = rbTransitionSlide.Checked;
            cmbZoomType.Enabled = rbTransitionZoom.Checked;
            
            // Enable/disable transition duration for all transition types except None
            bool hasTransition = !rbTransitionNone.Checked;
            nudTransitionDuration.Enabled = hasTransition;
            lblTransitionDuration.Enabled = hasTransition;
            lblSeconds.Enabled = hasTransition;
            
            // Enable preview button only if there are GIFs and transition is selected
            btnPreviewTransition.Enabled = hasTransition && lstGifFiles.Items.Count >= 2;
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
            
            // Update preview button availability
            btnPreviewTransition.Enabled = !rbTransitionNone.Checked && lstGifFiles.Items.Count >= 2;
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

            // Transition settings
            if (rbTransitionNone.Checked)
                Settings.Transition = TransitionType.None;
            else if (rbTransitionFade.Checked)
                Settings.Transition = TransitionType.Fade;
            else if (rbTransitionSlide.Checked)
            {
                switch (cmbSlideDirection.SelectedIndex)
                {
                    case 0: Settings.Transition = TransitionType.SlideLeft; break;
                    case 1: Settings.Transition = TransitionType.SlideRight; break;
                    case 2: Settings.Transition = TransitionType.SlideUp; break;
                    case 3: Settings.Transition = TransitionType.SlideDown; break;
                    default: Settings.Transition = TransitionType.SlideLeft; break;
                }
            }
            else if (rbTransitionZoom.Checked)
            {
                Settings.Transition = cmbZoomType.SelectedIndex == 0 ? TransitionType.ZoomIn : TransitionType.ZoomOut;
            }
            else if (rbTransitionDissolve.Checked)
                Settings.Transition = TransitionType.Dissolve;

            Settings.TransitionDuration = (float)nudTransitionDuration.Value;

            // Prepare output properties
            SelectedFilePaths = Settings.GifFilePaths;
            OutputFilePath = Settings.OutputFilePath;

            DialogResult = DialogResult.OK;
            Close();
        }

        private async void BtnPreviewTransition_Click(object sender, EventArgs e)
        {
            if (lstGifFiles.Items.Count < 2)
            {
                MessageBox.Show("Please select at least 2 GIF files to preview transitions.",
                               "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (rbTransitionNone.Checked)
            {
                MessageBox.Show("Please select a transition type to preview.",
                               "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnPreviewTransition.Enabled = false;
            btnPreviewTransition.Text = "Generating...";

            try
            {
                // Get first two GIFs for preview
                string firstGif = lstGifFiles.Items[0].ToString();
                string secondGif = lstGifFiles.Items[1].ToString();

                await Task.Run(() => GenerateTransitionPreview(firstGif, secondGif));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating transition preview: {ex.Message}",
                               "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnPreviewTransition.Enabled = true;
                btnPreviewTransition.Text = "Preview Transition";
            }
        }

        private void GenerateTransitionPreview(string firstGifPath, string secondGifPath)
        {
            try
            {
                using var firstCollection = new ImageMagick.MagickImageCollection(firstGifPath);
                using var secondCollection = new ImageMagick.MagickImageCollection(secondGifPath);

                // Get transition type
                var transitionType = GetSelectedTransitionType();
                float duration = (float)nudTransitionDuration.Value;

                // Generate a short preview (0.3 seconds max)
                float previewDuration = Math.Min(duration, 0.3f);
                int fps = 10; // Lower FPS for faster preview generation

                var previewFrames = TransitionGenerator.GenerateTransition(
                    firstCollection,
                    secondCollection,
                    transitionType,
                    previewDuration,
                    fps);

                if (previewFrames != null && previewFrames.Count > 0)
                {
                    // Save preview to temp file
                    string tempPath = Path.Combine(Path.GetTempPath(), $"transition_preview_{DateTime.Now.Ticks}.gif");
                    previewFrames.Write(tempPath);

                    // Show preview in default application
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempPath,
                        UseShellExecute = true
                    });

                    // Clean up temp file after a delay
                    Task.Delay(10000).ContinueWith(_ =>
                    {
                        try
                        {
                            if (File.Exists(tempPath))
                                File.Delete(tempPath);
                        }
                        catch { }
                    });
                }
            }
            catch (Exception ex)
            {
                this.Invoke((Action)(() =>
                {
                    MessageBox.Show($"Error generating preview: {ex.Message}",
                                   "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private TransitionType GetSelectedTransitionType()
        {
            if (rbTransitionFade.Checked) return TransitionType.Fade;
            if (rbTransitionSlide.Checked)
            {
                switch (cmbSlideDirection.SelectedIndex)
                {
                    case 0: return TransitionType.SlideLeft;
                    case 1: return TransitionType.SlideRight;
                    case 2: return TransitionType.SlideUp;
                    case 3: return TransitionType.SlideDown;
                    default: return TransitionType.SlideLeft;
                }
            }
            if (rbTransitionZoom.Checked)
                return cmbZoomType.SelectedIndex == 0 ? TransitionType.ZoomIn : TransitionType.ZoomOut;
            if (rbTransitionDissolve.Checked) return TransitionType.Dissolve;
            return TransitionType.None;
        }
    }
}