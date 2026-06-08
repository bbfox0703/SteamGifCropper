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
        private Label lblTransitionType;
        private ComboBox cmbTransition;
        private NumericUpDown nudTransitionDuration;
        private Label lblTransitionDuration;
        private Label lblSeconds;

        // ComboBox order -> TransitionType, each with a localized family-name resource key and a
        // (non-translated) direction/variant suffix. The selected index maps 1:1 to this array.
        private static readonly (TransitionType Type, string Key, string Suffix)[] _transitionItems =
        {
            (TransitionType.None,         "TransitionDialog_NoTransition",            ""),
            (TransitionType.Fade,         "TransitionDialog_Fade",                    ""),
            (TransitionType.CrossFade,    "ConcatenateDialog_TransitionCrossFade",    ""),
            (TransitionType.SlideLeft,    "ConcatenateDialog_TransitionSlide",        " ←"),
            (TransitionType.SlideRight,   "ConcatenateDialog_TransitionSlide",        " →"),
            (TransitionType.SlideUp,      "ConcatenateDialog_TransitionSlide",        " ↑"),
            (TransitionType.SlideDown,    "ConcatenateDialog_TransitionSlide",        " ↓"),
            (TransitionType.ZoomIn,       "ConcatenateDialog_TransitionZoom",         " +"),
            (TransitionType.ZoomOut,      "ConcatenateDialog_TransitionZoom",         " −"),
            (TransitionType.IrisOpen,     "ConcatenateDialog_TransitionIris",         " ◯+"),
            (TransitionType.IrisClose,    "ConcatenateDialog_TransitionIris",         " ◯−"),
            (TransitionType.WipeLeft,     "ConcatenateDialog_TransitionWipe",         " →"),
            (TransitionType.WipeRight,    "ConcatenateDialog_TransitionWipe",         " ←"),
            (TransitionType.WipeDiagonal, "ConcatenateDialog_TransitionWipe",         " ↘"),
            (TransitionType.Dissolve,     "ConcatenateDialog_TransitionDissolve",     ""),
            (TransitionType.BlurDissolve, "ConcatenateDialog_TransitionBlurDissolve", ""),
            (TransitionType.DipToBlack,   "ConcatenateDialog_TransitionDipToBlack",   ""),
            (TransitionType.Ripple,       "ConcatenateDialog_TransitionRipple",       ""),
        };

        // Preview functionality removed

        // General Settings
        private CheckBox chkUnifyDimensions;
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

            // Build the transition dropdown and react to selection (after InitializeComponent).
            PopulateTransitions();
            cmbTransition.SelectedIndexChanged += CmbTransition_SelectedIndexChanged;
            CmbTransition_SelectedIndexChanged(this, EventArgs.Empty); // set initial enabled state (None)
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
            lblTransitionType = new Label();
            cmbTransition = new ComboBox();
            lblTransitionDuration = new Label();
            nudTransitionDuration = new NumericUpDown();
            lblSeconds = new Label();
            chkUnifyDimensions = new CheckBox();
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
            lblInstructions.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_Instructions;
            // 
            // lblGifFiles
            // 
            lblGifFiles.AutoSize = true;
            lblGifFiles.Location = new Point(12, 35);
            lblGifFiles.Name = "lblGifFiles";
            lblGifFiles.Size = new Size(121, 15);
            lblGifFiles.TabIndex = 1;
            lblGifFiles.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_GifFiles;
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
            grpFpsSettings.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_FpsSettings;
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
            lblCustomFps.Text = SteamGifCropper.Properties.Resources.Label_FPS;
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
            grpPaletteSettings.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_PaletteSettings;
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
            lblOutputFile.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_OutputFile;
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
            grpTransitionSettings.Controls.Add(lblTransitionType);
            grpTransitionSettings.Controls.Add(cmbTransition);
            grpTransitionSettings.Controls.Add(lblTransitionDuration);
            grpTransitionSettings.Controls.Add(nudTransitionDuration);
            grpTransitionSettings.Controls.Add(lblSeconds);
            grpTransitionSettings.Location = new Point(12, 315);
            grpTransitionSettings.Name = "grpTransitionSettings";
            grpTransitionSettings.Size = new Size(496, 146);
            grpTransitionSettings.TabIndex = 17;
            grpTransitionSettings.TabStop = false;
            grpTransitionSettings.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_TransitionSettings;
            //
            // lblTransitionType
            //
            lblTransitionType.AutoSize = true;
            lblTransitionType.Location = new Point(6, 25);
            lblTransitionType.Name = "lblTransitionType";
            lblTransitionType.TabIndex = 0;
            lblTransitionType.Text = SteamGifCropper.Properties.Resources.ResourceManager.GetString("ConcatenateDialog_TransitionTypeLabel", SteamGifCropper.Properties.Resources.Culture) ?? "Transition:";
            //
            // cmbTransition
            //
            cmbTransition.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTransition.Location = new Point(120, 22);
            cmbTransition.Name = "cmbTransition";
            cmbTransition.Size = new Size(300, 23);
            cmbTransition.TabIndex = 1;
            // 
            // lblTransitionDuration
            // 
            lblTransitionDuration.AutoSize = true;
            lblTransitionDuration.Location = new Point(6, 89);
            lblTransitionDuration.Name = "lblTransitionDuration";
            lblTransitionDuration.Size = new Size(117, 15);
            lblTransitionDuration.TabIndex = 7;
            lblTransitionDuration.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_TransitionDurationLabel;
            // 
            // nudTransitionDuration
            // 
            nudTransitionDuration.DecimalPlaces = 1;
            nudTransitionDuration.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            nudTransitionDuration.Location = new Point(129, 87);
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
            lblSeconds.Location = new Point(195, 89);
            lblSeconds.Name = "lblSeconds";
            lblSeconds.Size = new Size(53, 15);
            lblSeconds.TabIndex = 9;
            lblSeconds.Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_SecondsLabel;
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
            Controls.Add(chkUseGifsicleOptimization);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConcatenateGifsDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = SteamGifCropper.Properties.Resources.ConcatenateDialog_Title;
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

        private void CmbTransition_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Enable the duration controls for every transition except None (index 0).
            bool hasTransition = cmbTransition.SelectedIndex > 0;
            nudTransitionDuration.Enabled = hasTransition;
            lblTransitionDuration.Enabled = hasTransition;
            lblSeconds.Enabled = hasTransition;
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
                MessageBox.Show(SteamGifCropper.Properties.Resources.ConcatenateDialog_RequireMinGifs,
                               SteamGifCropper.Properties.Resources.Mp4Dialog_InputRequired, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtOutputFile.Text))
            {
                MessageBox.Show(SteamGifCropper.Properties.Resources.ConcatenateDialog_RequireOutput,
                               SteamGifCropper.Properties.Resources.Mp4Dialog_OutputRequired, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            Settings.UseGifsicleOptimization = chkUseGifsicleOptimization.Checked;

            // Transition settings
            int transIndex = cmbTransition.SelectedIndex;
            if (transIndex < 0 || transIndex >= _transitionItems.Length) transIndex = 0;
            Settings.Transition = _transitionItems[transIndex].Type;
            Settings.TransitionDuration = (float)nudTransitionDuration.Value;

            // Prepare output properties
            SelectedFilePaths = Settings.GifFilePaths;
            OutputFilePath = Settings.OutputFilePath;

            DialogResult = DialogResult.OK;
            Close();
        }

        // Populate the transition dropdown from _transitionItems (localized family name + variant suffix).
        private void PopulateTransitions()
        {
            cmbTransition.Items.Clear();
            foreach (var item in _transitionItems)
            {
                string name = SteamGifCropper.Properties.Resources.ResourceManager
                    .GetString(item.Key, SteamGifCropper.Properties.Resources.Culture) ?? item.Key;
                cmbTransition.Items.Add(name + item.Suffix);
            }
            cmbTransition.SelectedIndex = 0; // None
        }

        // Preview functionality completely removed

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Component cleanup handled by Windows Forms designer
            }
            base.Dispose(disposing);
        }

    }
}