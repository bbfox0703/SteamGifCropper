using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
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
        private Button btnCancelPreview;
        private ProgressBar prgPreview;
        private Label lblPreviewStatus;
        
        private CancellationTokenSource _previewCancellationTokenSource;

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
            cmbFpsReference = new ComboBox();
            rbFpsCustom = new RadioButton();
            nudCustomFps = new NumericUpDown();
            lblCustomFps = new Label();
            grpPaletteSettings = new GroupBox();
            cmbPaletteReference = new ComboBox();
            rbPaletteAutoMerge = new RadioButton();
            rbPaletteUseReference = new RadioButton();
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
            lblTransitionDuration = new Label();
            nudTransitionDuration = new NumericUpDown();
            lblSeconds = new Label();
            btnPreviewTransition = new Button();
            btnCancelPreview = new Button();
            prgPreview = new ProgressBar();
            lblPreviewStatus = new Label();
            chkUnifyDimensions = new CheckBox();
            chkUseFasterPalette = new CheckBox();
            chkUseGifsicleOptimization = new CheckBox();
            btnOK = new Button();
            btnCancel = new Button();
            grpFpsSettings.SuspendLayout();
            ((ISupportInitialize)nudCustomFps).BeginInit();
            grpPaletteSettings.SuspendLayout();
            grpTransitionSettings.SuspendLayout();
            ((ISupportInitialize)nudTransitionDuration).BeginInit();
            SuspendLayout();
            // 
            // lblInstructions
            // 
            lblInstructions.AutoSize = true;
            lblInstructions.Location = new Point(12, 9);
            lblInstructions.Name = "lblInstructions";
            lblInstructions.Size = new Size(233, 15);
            lblInstructions.TabIndex = 0;
            lblInstructions.Text = "Select GIF files to concatenate (in order):";
            // 
            // lblGifFiles
            // 
            lblGifFiles.AutoSize = true;
            lblGifFiles.Location = new Point(12, 35);
            lblGifFiles.Name = "lblGifFiles";
            lblGifFiles.Size = new Size(121, 15);
            lblGifFiles.TabIndex = 1;
            lblGifFiles.Text = "GIF Files (2 or more):";
            // 
            // lstGifFiles
            // 
            lstGifFiles.ItemHeight = 15;
            lstGifFiles.Location = new Point(12, 55);
            lstGifFiles.Name = "lstGifFiles";
            lstGifFiles.Size = new Size(400, 109);
            lstGifFiles.TabIndex = 2;
            lstGifFiles.SelectedIndexChanged += LstGifFiles_SelectedIndexChanged;
            // 
            // btnAddFiles
            // 
            btnAddFiles.Location = new Point(420, 55);
            btnAddFiles.Name = "btnAddFiles";
            btnAddFiles.Size = new Size(80, 25);
            btnAddFiles.TabIndex = 3;
            btnAddFiles.Text = "Add Files...";
            btnAddFiles.UseVisualStyleBackColor = true;
            btnAddFiles.Click += BtnAddFiles_Click;
            // 
            // btnRemoveSelected
            // 
            btnRemoveSelected.Location = new Point(420, 85);
            btnRemoveSelected.Name = "btnRemoveSelected";
            btnRemoveSelected.Size = new Size(80, 25);
            btnRemoveSelected.TabIndex = 4;
            btnRemoveSelected.Text = "Remove";
            btnRemoveSelected.UseVisualStyleBackColor = true;
            btnRemoveSelected.Click += BtnRemoveSelected_Click;
            // 
            // btnMoveUp
            // 
            btnMoveUp.Location = new Point(420, 115);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Size = new Size(80, 25);
            btnMoveUp.TabIndex = 5;
            btnMoveUp.Text = "Move Up";
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += BtnMoveUp_Click;
            // 
            // btnMoveDown
            // 
            btnMoveDown.Location = new Point(420, 145);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Size = new Size(80, 25);
            btnMoveDown.TabIndex = 6;
            btnMoveDown.Text = "Move Down";
            btnMoveDown.UseVisualStyleBackColor = true;
            btnMoveDown.Click += BtnMoveDown_Click;
            // 
            // grpFpsSettings
            // 
            grpFpsSettings.Controls.Add(cmbFpsReference);
            grpFpsSettings.Controls.Add(rbFpsAutoHighest);
            grpFpsSettings.Controls.Add(rbFpsUseReference);
            grpFpsSettings.Controls.Add(rbFpsCustom);
            grpFpsSettings.Controls.Add(nudCustomFps);
            grpFpsSettings.Controls.Add(lblCustomFps);
            grpFpsSettings.Location = new Point(12, 185);
            grpFpsSettings.Name = "grpFpsSettings";
            grpFpsSettings.Size = new Size(240, 120);
            grpFpsSettings.TabIndex = 7;
            grpFpsSettings.TabStop = false;
            grpFpsSettings.Text = "FPS Settings";
            // 
            // rbFpsAutoHighest
            // 
            rbFpsAutoHighest.AutoSize = true;
            rbFpsAutoHighest.Checked = true;
            rbFpsAutoHighest.Location = new Point(6, 22);
            rbFpsAutoHighest.Name = "rbFpsAutoHighest";
            rbFpsAutoHighest.Size = new Size(130, 19);
            rbFpsAutoHighest.TabIndex = 0;
            rbFpsAutoHighest.TabStop = true;
            rbFpsAutoHighest.Text = "Auto (Use Highest)";
            rbFpsAutoHighest.UseVisualStyleBackColor = true;
            rbFpsAutoHighest.CheckedChanged += RbFps_CheckedChanged;
            // 
            // rbFpsUseReference
            // 
            rbFpsUseReference.AutoSize = true;
            rbFpsUseReference.Location = new Point(6, 47);
            rbFpsUseReference.Name = "rbFpsUseReference";
            rbFpsUseReference.Size = new Size(130, 19);
            rbFpsUseReference.TabIndex = 1;
            rbFpsUseReference.Text = "Use Reference GIF:";
            rbFpsUseReference.UseVisualStyleBackColor = true;
            rbFpsUseReference.CheckedChanged += RbFps_CheckedChanged;
            // 
            // cmbFpsReference
            // 
            cmbFpsReference.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFpsReference.Enabled = false;
            cmbFpsReference.Location = new Point(134, 47);
            cmbFpsReference.Name = "cmbFpsReference";
            cmbFpsReference.Size = new Size(100, 23);
            cmbFpsReference.TabIndex = 2;
            // 
            // rbFpsCustom
            // 
            rbFpsCustom.AutoSize = true;
            rbFpsCustom.Location = new Point(6, 76);
            rbFpsCustom.Name = "rbFpsCustom";
            rbFpsCustom.Size = new Size(94, 19);
            rbFpsCustom.TabIndex = 3;
            rbFpsCustom.Text = "Custom FPS:";
            rbFpsCustom.UseVisualStyleBackColor = true;
            rbFpsCustom.CheckedChanged += RbFps_CheckedChanged;
            // 
            // nudCustomFps
            // 
            nudCustomFps.Enabled = false;
            nudCustomFps.Location = new Point(134, 76);
            nudCustomFps.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            nudCustomFps.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudCustomFps.Name = "nudCustomFps";
            nudCustomFps.Size = new Size(60, 23);
            nudCustomFps.TabIndex = 4;
            nudCustomFps.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // lblCustomFps
            // 
            lblCustomFps.AutoSize = true;
            lblCustomFps.Location = new Point(197, 78);
            lblCustomFps.Name = "lblCustomFps";
            lblCustomFps.Size = new Size(24, 15);
            lblCustomFps.TabIndex = 5;
            lblCustomFps.Text = "fps";
            // 
            // grpPaletteSettings
            // 
            grpPaletteSettings.Controls.Add(cmbPaletteReference);
            grpPaletteSettings.Controls.Add(rbPaletteAutoMerge);
            grpPaletteSettings.Controls.Add(rbPaletteUseReference);
            grpPaletteSettings.Controls.Add(chkStrongPaletteWeighting);
            grpPaletteSettings.Location = new Point(260, 185);
            grpPaletteSettings.Name = "grpPaletteSettings";
            grpPaletteSettings.Size = new Size(248, 120);
            grpPaletteSettings.TabIndex = 8;
            grpPaletteSettings.TabStop = false;
            grpPaletteSettings.Text = "Palette Settings";
            // 
            // cmbPaletteReference
            // 
            cmbPaletteReference.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaletteReference.Enabled = false;
            cmbPaletteReference.Location = new Point(142, 45);
            cmbPaletteReference.Name = "cmbPaletteReference";
            cmbPaletteReference.Size = new Size(100, 23);
            cmbPaletteReference.TabIndex = 2;
            // 
            // rbPaletteAutoMerge
            // 
            rbPaletteAutoMerge.AutoSize = true;
            rbPaletteAutoMerge.Checked = true;
            rbPaletteAutoMerge.Location = new Point(6, 22);
            rbPaletteAutoMerge.Name = "rbPaletteAutoMerge";
            rbPaletteAutoMerge.Size = new Size(93, 19);
            rbPaletteAutoMerge.TabIndex = 0;
            rbPaletteAutoMerge.TabStop = true;
            rbPaletteAutoMerge.Text = "Auto Merge";
            rbPaletteAutoMerge.UseVisualStyleBackColor = true;
            rbPaletteAutoMerge.CheckedChanged += RbPalette_CheckedChanged;
            // 
            // rbPaletteUseReference
            // 
            rbPaletteUseReference.AutoSize = true;
            rbPaletteUseReference.Location = new Point(6, 47);
            rbPaletteUseReference.Name = "rbPaletteUseReference";
            rbPaletteUseReference.Size = new Size(130, 19);
            rbPaletteUseReference.TabIndex = 1;
            rbPaletteUseReference.Text = "Use Reference GIF:";
            rbPaletteUseReference.UseVisualStyleBackColor = true;
            rbPaletteUseReference.CheckedChanged += RbPalette_CheckedChanged;
            // 
            // chkStrongPaletteWeighting
            // 
            chkStrongPaletteWeighting.AutoSize = true;
            chkStrongPaletteWeighting.Checked = true;
            chkStrongPaletteWeighting.CheckState = CheckState.Checked;
            chkStrongPaletteWeighting.Location = new Point(6, 72);
            chkStrongPaletteWeighting.Name = "chkStrongPaletteWeighting";
            chkStrongPaletteWeighting.Size = new Size(198, 19);
            chkStrongPaletteWeighting.TabIndex = 3;
            chkStrongPaletteWeighting.Text = "8x weight for reference palette";
            chkStrongPaletteWeighting.UseVisualStyleBackColor = true;
            // 
            // lblOutputFile
            // 
            lblOutputFile.AutoSize = true;
            lblOutputFile.Location = new Point(12, 481);
            lblOutputFile.Name = "lblOutputFile";
            lblOutputFile.Size = new Size(91, 15);
            lblOutputFile.TabIndex = 18;
            lblOutputFile.Text = "Output GIF file:";
            // 
            // txtOutputFile
            // 
            txtOutputFile.Location = new Point(12, 501);
            txtOutputFile.Name = "txtOutputFile";
            txtOutputFile.Size = new Size(400, 23);
            txtOutputFile.TabIndex = 19;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new Point(420, 501);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new Size(80, 23);
            btnBrowseOutput.TabIndex = 20;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            // 
            // grpTransitionSettings
            // 
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
            grpTransitionSettings.Controls.Add(btnCancelPreview);
            grpTransitionSettings.Controls.Add(prgPreview);
            grpTransitionSettings.Controls.Add(lblPreviewStatus);
            grpTransitionSettings.Location = new Point(12, 315);
            grpTransitionSettings.Name = "grpTransitionSettings";
            grpTransitionSettings.Size = new Size(496, 146);
            grpTransitionSettings.TabIndex = 17;
            grpTransitionSettings.TabStop = false;
            grpTransitionSettings.Text = "Transition Settings";
            // 
            // rbTransitionNone
            // 
            rbTransitionNone.AutoSize = true;
            rbTransitionNone.Checked = true;
            rbTransitionNone.Location = new Point(6, 22);
            rbTransitionNone.Name = "rbTransitionNone";
            rbTransitionNone.Size = new Size(101, 19);
            rbTransitionNone.TabIndex = 0;
            rbTransitionNone.TabStop = true;
            rbTransitionNone.Text = "No Transition";
            rbTransitionNone.UseVisualStyleBackColor = true;
            rbTransitionNone.CheckedChanged += RbTransition_CheckedChanged;
            // 
            // rbTransitionFade
            // 
            rbTransitionFade.AutoSize = true;
            rbTransitionFade.Location = new Point(113, 22);
            rbTransitionFade.Name = "rbTransitionFade";
            rbTransitionFade.Size = new Size(53, 19);
            rbTransitionFade.TabIndex = 1;
            rbTransitionFade.Text = "Fade";
            rbTransitionFade.UseVisualStyleBackColor = true;
            rbTransitionFade.CheckedChanged += RbTransition_CheckedChanged;
            // 
            // rbTransitionSlide
            // 
            rbTransitionSlide.AutoSize = true;
            rbTransitionSlide.Location = new Point(172, 22);
            rbTransitionSlide.Name = "rbTransitionSlide";
            rbTransitionSlide.Size = new Size(53, 19);
            rbTransitionSlide.TabIndex = 2;
            rbTransitionSlide.Text = "Slide";
            rbTransitionSlide.UseVisualStyleBackColor = true;
            rbTransitionSlide.CheckedChanged += RbTransition_CheckedChanged;
            // 
            // rbTransitionZoom
            // 
            rbTransitionZoom.AutoSize = true;
            rbTransitionZoom.Location = new Point(232, 22);
            rbTransitionZoom.Name = "rbTransitionZoom";
            rbTransitionZoom.Size = new Size(59, 19);
            rbTransitionZoom.TabIndex = 3;
            rbTransitionZoom.Text = "Zoom";
            rbTransitionZoom.UseVisualStyleBackColor = true;
            rbTransitionZoom.CheckedChanged += RbTransition_CheckedChanged;
            // 
            // rbTransitionDissolve
            // 
            rbTransitionDissolve.AutoSize = true;
            rbTransitionDissolve.Location = new Point(297, 22);
            rbTransitionDissolve.Name = "rbTransitionDissolve";
            rbTransitionDissolve.Size = new Size(71, 19);
            rbTransitionDissolve.TabIndex = 4;
            rbTransitionDissolve.Text = "Dissolve";
            rbTransitionDissolve.UseVisualStyleBackColor = true;
            rbTransitionDissolve.CheckedChanged += RbTransition_CheckedChanged;
            // 
            // cmbSlideDirection
            // 
            cmbSlideDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSlideDirection.Enabled = false;
            cmbSlideDirection.Items.AddRange(new object[] { "Left", "Right", "Up", "Down" });
            cmbSlideDirection.Location = new Point(113, 46);
            cmbSlideDirection.Name = "cmbSlideDirection";
            cmbSlideDirection.Size = new Size(80, 23);
            cmbSlideDirection.TabIndex = 5;
            // 
            // cmbZoomType
            // 
            cmbZoomType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbZoomType.Enabled = false;
            cmbZoomType.Items.AddRange(new object[] { "Zoom In", "Zoom Out" });
            cmbZoomType.Location = new Point(232, 46);
            cmbZoomType.Name = "cmbZoomType";
            cmbZoomType.Size = new Size(80, 23);
            cmbZoomType.TabIndex = 6;
            // 
            // lblTransitionDuration
            // 
            lblTransitionDuration.AutoSize = true;
            lblTransitionDuration.Location = new Point(6, 77);
            lblTransitionDuration.Name = "lblTransitionDuration";
            lblTransitionDuration.Size = new Size(117, 15);
            lblTransitionDuration.TabIndex = 7;
            lblTransitionDuration.Text = "Transition Duration:";
            // 
            // nudTransitionDuration
            // 
            nudTransitionDuration.DecimalPlaces = 1;
            nudTransitionDuration.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudTransitionDuration.Location = new Point(130, 75);
            nudTransitionDuration.Maximum = new decimal(new int[] { 30, 0, 0, 65536 });
            nudTransitionDuration.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            nudTransitionDuration.Name = "nudTransitionDuration";
            nudTransitionDuration.Size = new Size(60, 23);
            nudTransitionDuration.TabIndex = 8;
            nudTransitionDuration.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            // 
            // lblSeconds
            // 
            lblSeconds.AutoSize = true;
            lblSeconds.Location = new Point(195, 77);
            lblSeconds.Name = "lblSeconds";
            lblSeconds.Size = new Size(53, 15);
            lblSeconds.TabIndex = 9;
            lblSeconds.Text = "seconds";
            // 
            // btnPreviewTransition
            // 
            btnPreviewTransition.Enabled = false;
            btnPreviewTransition.Location = new Point(3, 104);
            btnPreviewTransition.Name = "btnPreviewTransition";
            btnPreviewTransition.Size = new Size(100, 25);
            btnPreviewTransition.TabIndex = 10;
            btnPreviewTransition.Text = "Preview Transition";
            btnPreviewTransition.UseVisualStyleBackColor = true;
            btnPreviewTransition.Click += BtnPreviewTransition_Click;
            // 
            // btnCancelPreview
            // 
            btnCancelPreview.Enabled = false;
            btnCancelPreview.Location = new Point(308, 104);
            btnCancelPreview.Name = "btnCancelPreview";
            btnCancelPreview.Size = new Size(60, 25);
            btnCancelPreview.TabIndex = 11;
            btnCancelPreview.Text = "Cancel";
            btnCancelPreview.UseVisualStyleBackColor = true;
            btnCancelPreview.Click += BtnCancelPreview_Click;
            // 
            // prgPreview
            // 
            prgPreview.Location = new Point(378, 104);
            prgPreview.Name = "prgPreview";
            prgPreview.Size = new Size(100, 25);
            prgPreview.TabIndex = 12;
            prgPreview.Visible = false;
            // 
            // lblPreviewStatus
            // 
            lblPreviewStatus.AutoSize = true;
            lblPreviewStatus.Location = new Point(200, 100);
            lblPreviewStatus.Name = "lblPreviewStatus";
            lblPreviewStatus.Size = new Size(0, 15);
            lblPreviewStatus.TabIndex = 13;
            lblPreviewStatus.Visible = false;
            // 
            // chkUnifyDimensions
            // 
            chkUnifyDimensions.AutoSize = true;
            chkUnifyDimensions.Checked = true;
            chkUnifyDimensions.CheckState = CheckState.Checked;
            chkUnifyDimensions.Location = new Point(12, 530);
            chkUnifyDimensions.Name = "chkUnifyDimensions";
            chkUnifyDimensions.Size = new Size(221, 19);
            chkUnifyDimensions.TabIndex = 21;
            chkUnifyDimensions.Text = "Unify dimensions (resize to largest)";
            chkUnifyDimensions.UseVisualStyleBackColor = true;
            // 
            // chkUseFasterPalette
            // 
            chkUseFasterPalette.AutoSize = true;
            chkUseFasterPalette.Location = new Point(244, 530);
            chkUseFasterPalette.Name = "chkUseFasterPalette";
            chkUseFasterPalette.Size = new Size(185, 19);
            chkUseFasterPalette.TabIndex = 22;
            chkUseFasterPalette.Text = "Faster palette (lower quality)";
            chkUseFasterPalette.UseVisualStyleBackColor = true;
            // 
            // chkUseGifsicleOptimization
            // 
            chkUseGifsicleOptimization.AutoSize = true;
            chkUseGifsicleOptimization.Location = new Point(12, 555);
            chkUseGifsicleOptimization.Name = "chkUseGifsicleOptimization";
            chkUseGifsicleOptimization.Size = new Size(164, 19);
            chkUseGifsicleOptimization.TabIndex = 23;
            chkUseGifsicleOptimization.Text = "Use gifsicle optimization";
            chkUseGifsicleOptimization.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(320, 555);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(105, 25);
            btnOK.TabIndex = 24;
            btnOK.Text = "Concatenate";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(430, 555);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 25);
            btnCancel.TabIndex = 25;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // ConcatenateGifsDialog
            // 
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
            grpFpsSettings.ResumeLayout(false);
            grpFpsSettings.PerformLayout();
            ((ISupportInitialize)nudCustomFps).EndInit();
            grpPaletteSettings.ResumeLayout(false);
            grpPaletteSettings.PerformLayout();
            grpTransitionSettings.ResumeLayout(false);
            grpTransitionSettings.PerformLayout();
            ((ISupportInitialize)nudTransitionDuration).EndInit();
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

            // Create new cancellation token for this operation
            _previewCancellationTokenSource?.Cancel();
            _previewCancellationTokenSource = new CancellationTokenSource();
            
            btnPreviewTransition.Enabled = false;
            btnCancelPreview.Enabled = true;
            btnPreviewTransition.Text = "Generating...";
            prgPreview.Visible = true;
            lblPreviewStatus.Visible = true;
            prgPreview.Value = 0;
            lblPreviewStatus.Text = "Initializing preview...";

            var progress = new Progress<(int current, int total, string status)>(report =>
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke((Action)(() =>
                    {
                        prgPreview.Maximum = report.total;
                        prgPreview.Value = Math.Min(report.current, report.total);
                        lblPreviewStatus.Text = report.status;
                    }));
                }
                else
                {
                    prgPreview.Maximum = report.total;
                    prgPreview.Value = Math.Min(report.current, report.total);
                    lblPreviewStatus.Text = report.status;
                }
            });

            try
            {
                // Get first two GIFs for preview
                string firstGif = lstGifFiles.Items[0].ToString();
                string secondGif = lstGifFiles.Items[1].ToString();

                await Task.Run(() => GenerateTransitionPreview(firstGif, secondGif, progress, _previewCancellationTokenSource.Token), _previewCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                lblPreviewStatus.Text = "Preview cancelled by user";
                await Task.Delay(1000); // Show message briefly
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating transition preview: {ex.Message}",
                               "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnPreviewTransition.Enabled = true;
                btnCancelPreview.Enabled = false;
                btnPreviewTransition.Text = "Preview Transition";
                prgPreview.Visible = false;
                lblPreviewStatus.Visible = false;
                lblPreviewStatus.Text = "";
                
                _previewCancellationTokenSource?.Dispose();
                _previewCancellationTokenSource = null;
            }
        }

        private void GenerateTransitionPreview(string firstGifPath, string secondGifPath, IProgress<(int current, int total, string status)> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                using var firstCollection = new ImageMagick.MagickImageCollection(firstGifPath);
                using var secondCollection = new ImageMagick.MagickImageCollection(secondGifPath);
                
                // Optimize for preview: resize large images to reduce memory usage
                const int maxPreviewSize = 400;
                bool needsResize = firstCollection.Count > 0 && 
                                 (firstCollection[0].Width > maxPreviewSize || firstCollection[0].Height > maxPreviewSize);
                
                if (needsResize)
                {
                    progress?.Report((0, 1, "Resizing images for preview..."));
                    
                    foreach (var frame in firstCollection)
                    {
                        var geometry = new MagickGeometry(maxPreviewSize, maxPreviewSize) { IgnoreAspectRatio = false };
                        frame.Resize(geometry);
                    }
                    
                    foreach (var frame in secondCollection)
                    {
                        var geometry = new MagickGeometry(maxPreviewSize, maxPreviewSize) { IgnoreAspectRatio = false };
                        frame.Resize(geometry);
                    }
                }

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
                    fps,
                    progress,
                    cancellationToken);

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
                    
                    // Immediately dispose preview frames to free memory
                    previewFrames.Dispose();
                }
                
                // Force garbage collection after preview generation
                GC.Collect();
                GC.WaitForPendingFinalizers();
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
        
        private void BtnCancelPreview_Click(object sender, EventArgs e)
        {
            _previewCancellationTokenSource?.Cancel();
            btnCancelPreview.Enabled = false;
            lblPreviewStatus.Text = "Cancelling...";
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewCancellationTokenSource?.Cancel();
                _previewCancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}