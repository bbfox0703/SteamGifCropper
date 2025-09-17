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
        
        // Embedded Preview Panel
        private GroupBox grpPreview;
        private PictureBox picPreview;
        private Button btnPlay;
        private Button btnPause;
        private Button btnReplay;
        private ProgressBar prgAnimation;
        private Label lblFrameInfo;
        private CheckBox chkAutoPreview;
        private CheckBox chkFastPreview;
        
        private CancellationTokenSource _previewCancellationTokenSource;
        private System.Windows.Forms.Timer _animationTimer;
        private List<System.Drawing.Image> _previewFrames;
        private int _currentFrame;

        // General Settings
        private CheckBox chkUnifyDimensions;
        private CheckBox chkUseFasterPalette;
        private CheckBox chkUseGifsicleOptimization;

        // Action Buttons
        private Button btnOK;
        private IContainer components;
        private Button btnCancel;

        public ConcatenateGifsDialog()
        {
            InitializeComponent();
            InitializeSettings();
            ApplyTheme();
            UpdateReferenceComboBoxes();
            
            // Add event handlers for preview updates (after InitializeComponent)
            cmbSlideDirection.SelectedIndexChanged += CmbSlideDirection_SelectedIndexChanged;
            cmbZoomType.SelectedIndexChanged += CmbZoomType_SelectedIndexChanged;
            nudTransitionDuration.ValueChanged += NudTransitionDuration_ValueChanged;
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
            components = new Container();
            lblInstructions = new Label();
            lblGifFiles = new Label();
            lstGifFiles = new ListBox();
            btnAddFiles = new Button();
            btnRemoveSelected = new Button();
            btnMoveUp = new Button();
            btnMoveDown = new Button();
            grpFpsSettings = new GroupBox();
            cmbFpsReference = new ComboBox();
            rbFpsAutoHighest = new RadioButton();
            rbFpsUseReference = new RadioButton();
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
            grpPreview = new GroupBox();
            picPreview = new PictureBox();
            btnPlay = new Button();
            btnPause = new Button();
            btnReplay = new Button();
            prgAnimation = new ProgressBar();
            lblFrameInfo = new Label();
            chkAutoPreview = new CheckBox();
            chkFastPreview = new CheckBox();
            _animationTimer = new System.Windows.Forms.Timer(components);
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
            grpPreview.SuspendLayout();
            ((ISupportInitialize)picPreview).BeginInit();
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
            btnAddFiles.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_AddFiles;
            btnAddFiles.UseVisualStyleBackColor = true;
            btnAddFiles.Click += BtnAddFiles_Click;
            // 
            // btnRemoveSelected
            // 
            btnRemoveSelected.Location = new Point(420, 85);
            btnRemoveSelected.Name = "btnRemoveSelected";
            btnRemoveSelected.Size = new Size(80, 25);
            btnRemoveSelected.TabIndex = 4;
            btnRemoveSelected.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_Remove;
            btnRemoveSelected.UseVisualStyleBackColor = true;
            btnRemoveSelected.Click += BtnRemoveSelected_Click;
            // 
            // btnMoveUp
            // 
            btnMoveUp.Location = new Point(420, 115);
            btnMoveUp.Name = "btnMoveUp";
            btnMoveUp.Size = new Size(80, 25);
            btnMoveUp.TabIndex = 5;
            btnMoveUp.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_MoveUp;
            btnMoveUp.UseVisualStyleBackColor = true;
            btnMoveUp.Click += BtnMoveUp_Click;
            // 
            // btnMoveDown
            // 
            btnMoveDown.Location = new Point(420, 145);
            btnMoveDown.Name = "btnMoveDown";
            btnMoveDown.Size = new Size(80, 25);
            btnMoveDown.TabIndex = 6;
            btnMoveDown.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_MoveDown;
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
            // cmbFpsReference
            // 
            cmbFpsReference.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFpsReference.Enabled = false;
            cmbFpsReference.Location = new Point(134, 47);
            cmbFpsReference.Name = "cmbFpsReference";
            cmbFpsReference.Size = new Size(100, 23);
            cmbFpsReference.TabIndex = 2;
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
            rbFpsAutoHighest.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_FpsAutoHighest;
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
            rbFpsUseReference.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_FpsUseReference;
            rbFpsUseReference.UseVisualStyleBackColor = true;
            rbFpsUseReference.CheckedChanged += RbFps_CheckedChanged;
            // 
            // rbFpsCustom
            // 
            rbFpsCustom.AutoSize = true;
            rbFpsCustom.Location = new Point(6, 76);
            rbFpsCustom.Name = "rbFpsCustom";
            rbFpsCustom.Size = new Size(94, 19);
            rbFpsCustom.TabIndex = 3;
            rbFpsCustom.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_FpsCustom;
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
            rbPaletteAutoMerge.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_PaletteAutoMerge;
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
            rbPaletteUseReference.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_PaletteUseReference;
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
            chkStrongPaletteWeighting.Size = new Size(168, 19);
            chkStrongPaletteWeighting.TabIndex = 3;
            chkStrongPaletteWeighting.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_StrongPaletteWeightingShort;
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
            btnBrowseOutput.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_Browse;
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
            rbTransitionNone.Text = SteamGifCropper.Properties.Resources.TransitionDialog_NoTransition;
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
            rbTransitionFade.Text = SteamGifCropper.Properties.Resources.TransitionDialog_Fade;
            rbTransitionFade.UseVisualStyleBackColor = true;
            rbTransitionFade.CheckedChanged += RbTransition_CheckedChanged;
            // 
            // rbTransitionSlide
            // 
            rbTransitionSlide.AutoSize = true;
            rbTransitionSlide.Location = new Point(189, 22);
            rbTransitionSlide.Name = "rbTransitionSlide";
            rbTransitionSlide.Size = new Size(53, 19);
            rbTransitionSlide.TabIndex = 2;
            rbTransitionSlide.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_TransitionSlide;
            rbTransitionSlide.UseVisualStyleBackColor = true;
            rbTransitionSlide.CheckedChanged += RbTransition_CheckedChanged;
            // 
            // rbTransitionZoom
            // 
            rbTransitionZoom.AutoSize = true;
            rbTransitionZoom.Location = new Point(266, 22);
            rbTransitionZoom.Name = "rbTransitionZoom";
            rbTransitionZoom.Size = new Size(59, 19);
            rbTransitionZoom.TabIndex = 3;
            rbTransitionZoom.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_TransitionZoom;
            rbTransitionZoom.UseVisualStyleBackColor = true;
            rbTransitionZoom.CheckedChanged += RbTransition_CheckedChanged;
            // 
            // rbTransitionDissolve
            // 
            rbTransitionDissolve.AutoSize = true;
            rbTransitionDissolve.Location = new Point(351, 22);
            rbTransitionDissolve.Name = "rbTransitionDissolve";
            rbTransitionDissolve.Size = new Size(71, 19);
            rbTransitionDissolve.TabIndex = 4;
            rbTransitionDissolve.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_TransitionDissolve;
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
            btnPreviewTransition.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_PreviewTransition;
            btnPreviewTransition.UseVisualStyleBackColor = true;
            btnPreviewTransition.Click += BtnPreviewTransition_Click;
            // 
            // btnCancelPreview
            // 
            btnCancelPreview.Enabled = false;
            btnCancelPreview.Location = new Point(287, 104);
            btnCancelPreview.Name = "btnCancelPreview";
            btnCancelPreview.Size = new Size(85, 25);
            btnCancelPreview.TabIndex = 11;
            btnCancelPreview.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_CancelPreview;
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
            // grpPreview
            // 
            grpPreview.Controls.Add(picPreview);
            grpPreview.Controls.Add(btnPlay);
            grpPreview.Controls.Add(btnPause);
            grpPreview.Controls.Add(btnReplay);
            grpPreview.Controls.Add(prgAnimation);
            grpPreview.Controls.Add(lblFrameInfo);
            grpPreview.Controls.Add(chkAutoPreview);
            grpPreview.Controls.Add(chkFastPreview);
            grpPreview.Location = new Point(530, 12);
            grpPreview.Name = "grpPreview";
            grpPreview.Size = new Size(260, 350);
            grpPreview.TabIndex = 26;
            grpPreview.TabStop = false;
            grpPreview.Text = "Preview";
            // 
            // picPreview
            // 
            picPreview.BackColor = Color.Black;
            picPreview.BorderStyle = BorderStyle.FixedSingle;
            picPreview.Location = new Point(10, 25);
            picPreview.Name = "picPreview";
            picPreview.Size = new Size(240, 180);
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;
            picPreview.TabIndex = 0;
            picPreview.TabStop = false;
            // 
            // btnPlay
            // 
            btnPlay.Location = new Point(10, 215);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(50, 25);
            btnPlay.TabIndex = 1;
            btnPlay.Text = "▶";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += BtnPlay_Click;
            // 
            // btnPause
            // 
            btnPause.Location = new Point(70, 215);
            btnPause.Name = "btnPause";
            btnPause.Size = new Size(50, 25);
            btnPause.TabIndex = 2;
            btnPause.Text = "⏸";
            btnPause.UseVisualStyleBackColor = true;
            btnPause.Click += BtnPause_Click;
            // 
            // btnReplay
            // 
            btnReplay.Location = new Point(130, 215);
            btnReplay.Name = "btnReplay";
            btnReplay.Size = new Size(50, 25);
            btnReplay.TabIndex = 3;
            btnReplay.Text = "⏮";
            btnReplay.UseVisualStyleBackColor = true;
            btnReplay.Click += BtnReplay_Click;
            // 
            // prgAnimation
            // 
            prgAnimation.Location = new Point(10, 250);
            prgAnimation.Name = "prgAnimation";
            prgAnimation.Size = new Size(240, 15);
            prgAnimation.TabIndex = 4;
            // 
            // lblFrameInfo
            // 
            lblFrameInfo.AutoSize = true;
            lblFrameInfo.Location = new Point(10, 275);
            lblFrameInfo.Name = "lblFrameInfo";
            lblFrameInfo.Size = new Size(126, 15);
            lblFrameInfo.TabIndex = 5;
            lblFrameInfo.Text = "No preview available";
            // 
            // chkAutoPreview
            // 
            chkAutoPreview.AutoSize = true;
            chkAutoPreview.Checked = true;
            chkAutoPreview.CheckState = CheckState.Checked;
            chkAutoPreview.Location = new Point(10, 300);
            chkAutoPreview.Name = "chkAutoPreview";
            chkAutoPreview.Size = new Size(99, 19);
            chkAutoPreview.TabIndex = 6;
            chkAutoPreview.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_AutoPreview;
            chkAutoPreview.UseVisualStyleBackColor = true;
            chkAutoPreview.CheckedChanged += ChkAutoPreview_CheckedChanged;
            // 
            // chkFastPreview
            // 
            chkFastPreview.AutoSize = true;
            chkFastPreview.Checked = true;
            chkFastPreview.CheckState = CheckState.Checked;
            chkFastPreview.Location = new Point(10, 325);
            chkFastPreview.Name = "chkFastPreview";
            chkFastPreview.Size = new Size(132, 19);
            chkFastPreview.TabIndex = 7;
            chkFastPreview.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_FastPreview;
            chkFastPreview.UseVisualStyleBackColor = true;
            // 
            // _animationTimer
            // 
            _animationTimer.Tick += AnimationTimer_Tick;
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
            chkUnifyDimensions.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_UnifyDimensions;
            chkUnifyDimensions.UseVisualStyleBackColor = true;
            // 
            // chkUseFasterPalette
            // 
            chkUseFasterPalette.AutoSize = true;
            chkUseFasterPalette.Location = new Point(244, 530);
            chkUseFasterPalette.Name = "chkUseFasterPalette";
            chkUseFasterPalette.Size = new Size(185, 19);
            chkUseFasterPalette.TabIndex = 22;
            chkUseFasterPalette.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_UseFasterPalette;
            chkUseFasterPalette.UseVisualStyleBackColor = true;
            // 
            // chkUseGifsicleOptimization
            // 
            chkUseGifsicleOptimization.AutoSize = true;
            chkUseGifsicleOptimization.Location = new Point(12, 555);
            chkUseGifsicleOptimization.Name = "chkUseGifsicleOptimization";
            chkUseGifsicleOptimization.Size = new Size(164, 19);
            chkUseGifsicleOptimization.TabIndex = 23;
            chkUseGifsicleOptimization.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_UseGifsicleOptimization;
            chkUseGifsicleOptimization.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(320, 555);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(105, 25);
            btnOK.TabIndex = 24;
            btnOK.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_Concatenate;
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
            btnCancel.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_Cancel;
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // ConcatenateGifsDialog
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(800, 595);
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
            Controls.Add(grpPreview);
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
            grpPreview.ResumeLayout(false);
            grpPreview.PerformLayout();
            ((ISupportInitialize)picPreview).EndInit();
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
            
            // Update embedded preview when transition changes
            if (chkAutoPreview.Checked)
            {
                UpdatePreview();
            }
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
            
            // Update embedded preview when GIF files change
            if (chkAutoPreview.Checked)
            {
                UpdatePreview();
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
                btnPreviewTransition.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_PreviewTransition;
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
        
        // Event handlers for preview updates
        private void CmbSlideDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkAutoPreview.Checked)
            {
                UpdatePreview();
            }
        }

        private void CmbZoomType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkAutoPreview.Checked)
            {
                UpdatePreview();
            }
        }

        private void NudTransitionDuration_ValueChanged(object sender, EventArgs e)
        {
            if (chkAutoPreview.Checked)
            {
                UpdatePreview();
            }
        }
        
        // Embedded Preview Implementation
        private void BtnPlay_Click(object sender, EventArgs e)
        {
            if (_previewFrames != null && _previewFrames.Count > 0)
            {
                _animationTimer.Start();
                btnPlay.Enabled = false;
                btnPause.Enabled = true;
            }
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            _animationTimer.Stop();
            btnPlay.Enabled = true;
            btnPause.Enabled = false;
        }

        private void BtnReplay_Click(object sender, EventArgs e)
        {
            if (_previewFrames != null && _previewFrames.Count > 0)
            {
                _currentFrame = 0;
                DisplayCurrentFrame();
                _animationTimer.Start();
                btnPlay.Enabled = false;
                btnPause.Enabled = true;
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_previewFrames == null || _previewFrames.Count == 0) return;

            DisplayCurrentFrame();
            
            _currentFrame = (_currentFrame + 1) % _previewFrames.Count;
            prgAnimation.Value = (int)((double)_currentFrame / _previewFrames.Count * 100);
            
            if (_currentFrame == 0) // Loop completed
            {
                _animationTimer.Stop();
                btnPlay.Enabled = true;
                btnPause.Enabled = false;
            }
        }

        private void DisplayCurrentFrame()
        {
            if (_previewFrames != null && _currentFrame >= 0 && _currentFrame < _previewFrames.Count)
            {
                picPreview.Image = _previewFrames[_currentFrame];
                lblFrameInfo.Text = $"Frame {_currentFrame + 1} of {_previewFrames.Count}";
            }
        }

        private void ChkAutoPreview_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoPreview.Checked)
            {
                UpdatePreview();
            }
        }

        private async void UpdatePreview()
        {
            if (!chkAutoPreview.Checked || lstGifFiles.Items.Count < 2 || rbTransitionNone.Checked)
            {
                ClearPreview();
                return;
            }

            try
            {
                await GenerateEmbeddedPreview();
            }
            catch (Exception ex)
            {
                lblFrameInfo.Text = $"Preview error: {ex.Message}";
                ClearPreview();
            }
        }

        private async Task GenerateEmbeddedPreview()
        {
            btnPlay.Enabled = false;
            btnPause.Enabled = false;
            btnReplay.Enabled = false;
            lblFrameInfo.Text = "Generating preview...";

            await Task.Run(() =>
            {
                try
                {
                    // Dispose existing preview frames
                    DisposePreviewFrames();

                    string firstGif = lstGifFiles.Items[0].ToString();
                    string secondGif = lstGifFiles.Items[1].ToString();

                    using var firstCollection = new ImageMagick.MagickImageCollection(firstGif);
                    using var secondCollection = new ImageMagick.MagickImageCollection(secondGif);

                    // Dynamic optimization based on fast preview setting
                    int previewSize = chkFastPreview.Checked ? 100 : 180; // Smaller size for fast mode
                    int maxFrames = chkFastPreview.Checked ? 2 : 5; // Fewer frames for fast mode

                    // Sample fewer frames for preview
                    var firstFrames = SampleFrames(firstCollection, maxFrames);
                    var secondFrames = SampleFrames(secondCollection, maxFrames);

                    // Resize sampled frames
                    foreach (var frame in firstFrames)
                    {
                        var geometry = new MagickGeometry((uint)previewSize, (uint)previewSize) { IgnoreAspectRatio = false };
                        frame.Resize(geometry);
                        frame.Strip(); // Remove metadata
                        frame.Quality = chkFastPreview.Checked ? (uint)60 : (uint)75; // Lower quality for fast mode
                    }

                    foreach (var frame in secondFrames)
                    {
                        var geometry = new MagickGeometry((uint)previewSize, (uint)previewSize) { IgnoreAspectRatio = false };
                        frame.Resize(geometry);
                        frame.Strip(); // Remove metadata
                        frame.Quality = chkFastPreview.Checked ? (uint)60 : (uint)75; // Lower quality for fast mode
                    }

                    var transitionType = GetSelectedTransitionType();
                    float duration = chkFastPreview.Checked ? 0.3f : 0.8f; // Shorter duration for fast mode
                    int fps = chkFastPreview.Checked ? 4 : 8; // Lower FPS for fast mode

                    // Create collections from sampled frames
                    using var sampledFirst = new ImageMagick.MagickImageCollection();
                    using var sampledSecond = new ImageMagick.MagickImageCollection();

                    foreach (var frame in firstFrames) sampledFirst.Add(frame.Clone());
                    foreach (var frame in secondFrames) sampledSecond.Add(frame.Clone());

                    var previewCollection = TransitionGenerator.GenerateTransition(
                        sampledFirst,
                        sampledSecond,
                        transitionType,
                        duration,
                        fps);

                    if (previewCollection != null && previewCollection.Count > 0)
                    {
                        _previewFrames = new List<System.Drawing.Image>();
                        foreach (var frame in previewCollection)
                        {
                            var magickImage = (MagickImage)frame;
                            // Use faster JPEG format for preview
                            byte[] imageBytes = magickImage.ToByteArray(MagickFormat.Jpeg);
                            using (var ms = new System.IO.MemoryStream(imageBytes))
                            {
                                _previewFrames.Add(System.Drawing.Image.FromStream(ms));
                            }
                        }
                        previewCollection.Dispose();
                    }

                    // Cleanup sampled frames
                    foreach (var frame in firstFrames) frame.Dispose();
                    foreach (var frame in secondFrames) frame.Dispose();
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() =>
                    {
                        lblFrameInfo.Text = $"Preview error: {ex.Message}";
                    }));
                }
            });

            if (_previewFrames != null && _previewFrames.Count > 0)
            {
                _currentFrame = 0;
                DisplayCurrentFrame();
                btnPlay.Enabled = true;
                btnReplay.Enabled = true;
                prgAnimation.Maximum = 100;
                prgAnimation.Value = 0;
            }
            else
            {
                lblFrameInfo.Text = "Failed to generate preview";
            }
        }

        private List<MagickImage> SampleFrames(MagickImageCollection collection, int maxFrames)
        {
            var sampledFrames = new List<MagickImage>();

            if (collection.Count <= maxFrames)
            {
                // If we have few frames, use all of them
                foreach (var frame in collection)
                {
                    sampledFrames.Add((MagickImage)frame.Clone());
                }
            }
            else
            {
                // Sample frames evenly across the collection
                for (int i = 0; i < maxFrames; i++)
                {
                    int index = (int)((float)i / (maxFrames - 1) * (collection.Count - 1));
                    sampledFrames.Add((MagickImage)collection[index].Clone());
                }
            }

            return sampledFrames;
        }

        private void ClearPreview()
        {
            _animationTimer.Stop();
            DisposePreviewFrames();
            picPreview.Image = null;
            lblFrameInfo.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_NoPreviewAvailable;
            prgAnimation.Value = 0;
            btnPlay.Enabled = false;
            btnPause.Enabled = false;
            btnReplay.Enabled = false;
        }

        private void DisposePreviewFrames()
        {
            if (_previewFrames != null)
            {
                foreach (var frame in _previewFrames)
                {
                    frame?.Dispose();
                }
                _previewFrames.Clear();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewCancellationTokenSource?.Cancel();
                _previewCancellationTokenSource?.Dispose();
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
                DisposePreviewFrames();
            }
            base.Dispose(disposing);
        }
    }
}