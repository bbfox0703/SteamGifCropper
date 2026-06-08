#nullable enable
using System;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    // Quicksand-flow (流沙) parameters dialog. Mirrors SlotMachineDialog's inline layout + theme
    // handling. gifMode toggles the input file filter (animated GIF vs any image) and shows the GIF
    // playback-mode picker (play during flow vs freeze on frame 0).
    //
    // The image is sliced into N horizontal bands; each band wrap-scrolls horizontally a whole number
    // of turns over the duration (eased in/out, returning to the original), with the turn count graded
    // across the bands so one edge (or the centre) flows fastest — a viscous shear / quicksand look.
    public class QuicksandDialog : Form
    {
        public string InputFilePath { get; private set; } = string.Empty;
        public string OutputFilePath { get; private set; } = string.Empty;
        public bool IsGif { get; private set; }
        public int Layers { get; private set; } = 16;
        public double DurationSeconds { get; private set; } = 6.0;
        public double StartSeconds { get; private set; } = 0.0;
        public int Fps { get; private set; } = 15;
        public int MaxRevolutions { get; private set; } = 12;
        public int MinRevolutions { get; private set; } = 2;
        public QuicksandFastBand FastBand { get; private set; } = QuicksandFastBand.Bottom;
        public double Viscosity { get; private set; } = 1.0;
        public bool FlowRight { get; private set; } = true;
        public QuicksandFlowAxis Axis { get; private set; } = QuicksandFlowAxis.Horizontal;
        public bool PlayGifDuringFlow { get; private set; } = true;

        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseOutput = null!;
        private NumericUpDown numLayers = null!;
        private NumericUpDown numDuration = null!;
        private NumericUpDown numFps = null!;
        private NumericUpDown numMaxRevs = null!;
        private NumericUpDown numMinRevs = null!;
        private ComboBox cmbDirection = null!;
        private ComboBox cmbFastBand = null!;
        private NumericUpDown numViscosity = null!;
        private ComboBox cmbGifPlayMode = null!;
        private NumericUpDown numStart = null!;
        private Label lblStart = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Label lblInput = null!;
        private Label lblOutput = null!;
        private Label lblLayers = null!;
        private Label lblDuration = null!;
        private Label lblFps = null!;
        private Label lblMaxRevs = null!;
        private Label lblMinRevs = null!;
        private Label lblDirection = null!;
        private Label lblFastBand = null!;
        private Label lblViscosity = null!;

        public QuicksandDialog(bool gifMode)
        {
            IsGif = gifMode;
            InitializeComponent();
            // The GIF playback-mode picker + start-offset only apply to animated input.
            cmbGifPlayMode.Visible = gifMode;
            lblStart.Visible = gifMode;
            numStart.Visible = gifMode;
            cmbGifPlayMode.SelectedIndexChanged += (s, e) => UpdateStartEnabled();
            UpdateUIText();
            UpdateStartEnabled();
            ApplyTheme();
        }

        // The start offset only has meaning for "play during flow" (a window into the GIF timeline);
        // "flow, then play" runs over a frozen frame, so it is greyed out there.
        private void UpdateStartEnabled()
        {
            bool enabled = IsGif && cmbGifPlayMode.SelectedIndex == 0;
            numStart.Enabled = enabled;
            lblStart.Enabled = enabled;
        }

        private void UpdateUIText()
        {
            Text = Resources.QuickDialog_Title;
            lblInput.Text = Resources.QuickDialog_InputLabel;
            lblOutput.Text = Resources.QuickDialog_OutputLabel;
            lblLayers.Text = Resources.QuickDialog_Layers;
            lblDuration.Text = Resources.QuickDialog_Duration;
            lblFps.Text = Resources.SlotDialog_Fps;
            lblMaxRevs.Text = Resources.QuickDialog_MaxRevs;
            lblMinRevs.Text = Resources.QuickDialog_MinRevs;
            lblDirection.Text = Resources.SlotDialog_Direction;
            lblFastBand.Text = Resources.QuickDialog_FastBand;
            lblViscosity.Text = Resources.QuickDialog_Viscosity;
            lblStart.Text = Resources.Dialog_StartSeconds;
            btnBrowseInput.Text = Resources.Button_Browse;
            btnBrowseOutput.Text = Resources.Button_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;

            // 4-way picker encodes both the flow axis and direction: 0 right, 1 left (horizontal),
            // 2 down, 3 up (vertical).
            int dirSel = cmbDirection.SelectedIndex < 0 ? 0 : cmbDirection.SelectedIndex;
            cmbDirection.Items.Clear();
            cmbDirection.Items.Add(Resources.QuickDialog_DirRight);
            cmbDirection.Items.Add(Resources.QuickDialog_DirLeft);
            cmbDirection.Items.Add(Resources.QuickDialog_DirDown);
            cmbDirection.Items.Add(Resources.QuickDialog_DirUp);
            cmbDirection.SelectedIndex = dirSel;

            RefreshFastBandLabels();

            int playSel = cmbGifPlayMode.SelectedIndex < 0 ? 0 : cmbGifPlayMode.SelectedIndex;
            cmbGifPlayMode.Items.Clear();
            cmbGifPlayMode.Items.Add(Resources.QuickDialog_GifPlayDuringFlow);
            cmbGifPlayMode.Items.Add(Resources.QuickDialog_GifFreezeFrame0);
            cmbGifPlayMode.SelectedIndex = playSel;
        }

        // The fast-band picker's labels follow the flow axis: horizontal rows read bottom/top/middle,
        // vertical columns read right/left/middle. The index -> enum mapping is identical either way
        // (0 = last band, 1 = first band, 2 = middle), so the selection survives an axis switch.
        private void RefreshFastBandLabels()
        {
            bool vertical = cmbDirection.SelectedIndex >= 2;
            int sel = cmbFastBand.SelectedIndex < 0 ? 0 : cmbFastBand.SelectedIndex;
            cmbFastBand.Items.Clear();
            cmbFastBand.Items.Add(vertical ? Resources.QuickDialog_BandRight : Resources.QuickDialog_BandBottom);
            cmbFastBand.Items.Add(vertical ? Resources.QuickDialog_BandLeft : Resources.QuickDialog_BandTop);
            cmbFastBand.Items.Add(Resources.QuickDialog_BandMiddle);
            cmbFastBand.SelectedIndex = sel;
        }

        private void CmbDirection_SelectedIndexChanged(object? sender, EventArgs e)
        {
            RefreshFastBandLabels();
        }

        private void ApplyTheme()
        {
            bool isDark = WindowsThemeManager.IsDarkModeEnabled();
            if (isDark)
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
                WindowsThemeManager.SetDarkModeForWindow(this.Handle, isDark);
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
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
                    numericUpDown.ForeColor = System.Drawing.Color.White;
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
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.BackColor = System.Drawing.SystemColors.Window;
                    numericUpDown.ForeColor = System.Drawing.SystemColors.WindowText;
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

        private void BtnBrowseInput_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = IsGif ? Resources.FileDialog_GifAndWebpFilter : Resources.FileDialog_ImageAndGifFilter,
                Title = Resources.QuickDialog_InputLabel
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInputPath.Text = ofd.FileName;
                if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
                {
                    string dir = Path.GetDirectoryName(ofd.FileName) ?? string.Empty;
                    string name = Path.GetFileNameWithoutExtension(ofd.FileName) + "_quicksand.gif";
                    txtOutputPath.Text = Path.Combine(dir, name);
                }
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = Resources.QuickDialog_OutputLabel,
                FileName = string.IsNullOrEmpty(txtInputPath.Text)
                    ? "output_quicksand.gif"
                    : Path.GetFileNameWithoutExtension(txtInputPath.Text) + "_quicksand.gif"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                txtOutputPath.Text = sfd.FileName;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInputPath.Text) || !File.Exists(txtInputPath.Text))
            {
                MessageBox.Show(this, Resources.ScrollDialog_InputRequired, Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
            {
                MessageBox.Show(this, Resources.ScrollDialog_OutputRequired, Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            InputFilePath = txtInputPath.Text;
            OutputFilePath = txtOutputPath.Text;
            Layers = (int)numLayers.Value;
            DurationSeconds = (double)numDuration.Value;
            StartSeconds = (double)numStart.Value;
            Fps = (int)numFps.Value;
            MaxRevolutions = (int)numMaxRevs.Value;
            MinRevolutions = (int)numMinRevs.Value;
            FastBand = (QuicksandFastBand)cmbFastBand.SelectedIndex;
            Viscosity = (double)numViscosity.Value;
            int dir = cmbDirection.SelectedIndex;
            Axis = dir >= 2 ? QuicksandFlowAxis.Vertical : QuicksandFlowAxis.Horizontal;
            FlowRight = dir == 0 || dir == 2; // right (horizontal) / down (vertical) = positive roll
            PlayGifDuringFlow = cmbGifPlayMode.SelectedIndex == 0;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void InitializeComponent()
        {
            lblInput = new Label();
            txtInputPath = new TextBox();
            btnBrowseInput = new Button();
            lblOutput = new Label();
            txtOutputPath = new TextBox();
            btnBrowseOutput = new Button();
            lblLayers = new Label();
            numLayers = new NumericUpDown();
            lblDuration = new Label();
            numDuration = new NumericUpDown();
            lblFps = new Label();
            numFps = new NumericUpDown();
            lblMaxRevs = new Label();
            numMaxRevs = new NumericUpDown();
            lblMinRevs = new Label();
            numMinRevs = new NumericUpDown();
            lblDirection = new Label();
            cmbDirection = new ComboBox();
            lblFastBand = new Label();
            cmbFastBand = new ComboBox();
            lblViscosity = new Label();
            numViscosity = new NumericUpDown();
            cmbGifPlayMode = new ComboBox();
            lblStart = new Label();
            numStart = new NumericUpDown();
            btnOK = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numLayers).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDuration).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFps).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRevs).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinRevs).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numViscosity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStart).BeginInit();
            SuspendLayout();
            //
            // lblInput
            //
            lblInput.Location = new System.Drawing.Point(14, 9);
            lblInput.Name = "lblInput";
            lblInput.Size = new System.Drawing.Size(360, 20);
            lblInput.TabIndex = 0;
            lblInput.Text = "Input";
            //
            // txtInputPath
            //
            txtInputPath.Location = new System.Drawing.Point(14, 29);
            txtInputPath.Name = "txtInputPath";
            txtInputPath.ReadOnly = true;
            txtInputPath.Size = new System.Drawing.Size(358, 23);
            txtInputPath.TabIndex = 1;
            //
            // btnBrowseInput
            //
            btnBrowseInput.Location = new System.Drawing.Point(378, 27);
            btnBrowseInput.Name = "btnBrowseInput";
            btnBrowseInput.Size = new System.Drawing.Size(88, 25);
            btnBrowseInput.TabIndex = 2;
            btnBrowseInput.Text = "Browse";
            btnBrowseInput.UseVisualStyleBackColor = true;
            btnBrowseInput.Click += BtnBrowseInput_Click;
            //
            // lblOutput
            //
            lblOutput.Location = new System.Drawing.Point(14, 64);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(360, 20);
            lblOutput.TabIndex = 3;
            lblOutput.Text = "Output";
            //
            // txtOutputPath
            //
            txtOutputPath.Location = new System.Drawing.Point(14, 84);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.ReadOnly = true;
            txtOutputPath.Size = new System.Drawing.Size(358, 23);
            txtOutputPath.TabIndex = 4;
            //
            // btnBrowseOutput
            //
            btnBrowseOutput.Location = new System.Drawing.Point(378, 82);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new System.Drawing.Size(88, 25);
            btnBrowseOutput.TabIndex = 5;
            btnBrowseOutput.Text = "Browse";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            //
            // lblLayers
            //
            lblLayers.Location = new System.Drawing.Point(14, 126);
            lblLayers.Name = "lblLayers";
            lblLayers.Size = new System.Drawing.Size(98, 20);
            lblLayers.TabIndex = 6;
            lblLayers.Text = "Layers";
            //
            // numLayers
            //
            numLayers.Location = new System.Drawing.Point(114, 124);
            numLayers.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            numLayers.Maximum = new decimal(new int[] { 64, 0, 0, 0 });
            numLayers.Name = "numLayers";
            numLayers.Size = new System.Drawing.Size(46, 23);
            numLayers.TabIndex = 7;
            numLayers.Value = new decimal(new int[] { 16, 0, 0, 0 });
            //
            // lblDuration
            //
            lblDuration.Location = new System.Drawing.Point(168, 126);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new System.Drawing.Size(96, 20);
            lblDuration.TabIndex = 8;
            lblDuration.Text = "Duration";
            //
            // numDuration
            //
            numDuration.DecimalPlaces = 2;
            numDuration.Increment = new decimal(new int[] { 25, 0, 0, 131072 }); // 0.25
            numDuration.Location = new System.Drawing.Point(268, 124);
            numDuration.Minimum = new decimal(new int[] { 10, 0, 0, 131072 }); // 0.10
            numDuration.Maximum = new decimal(new int[] { 3000, 0, 0, 131072 }); // 30.00
            numDuration.Name = "numDuration";
            numDuration.Size = new System.Drawing.Size(52, 23);
            numDuration.TabIndex = 9;
            numDuration.Value = new decimal(new int[] { 600, 0, 0, 131072 }); // 6.00
            //
            // lblFps
            //
            lblFps.Location = new System.Drawing.Point(322, 126);
            lblFps.Name = "lblFps";
            lblFps.Size = new System.Drawing.Size(34, 20);
            lblFps.TabIndex = 10;
            lblFps.Text = "FPS";
            //
            // numFps
            //
            numFps.Location = new System.Drawing.Point(360, 124);
            numFps.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            numFps.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numFps.Name = "numFps";
            numFps.Size = new System.Drawing.Size(46, 23);
            numFps.TabIndex = 11;
            numFps.Value = new decimal(new int[] { 15, 0, 0, 0 });
            //
            // lblMaxRevs
            //
            lblMaxRevs.Location = new System.Drawing.Point(14, 159);
            lblMaxRevs.Name = "lblMaxRevs";
            lblMaxRevs.Size = new System.Drawing.Size(98, 20);
            lblMaxRevs.TabIndex = 12;
            lblMaxRevs.Text = "Max turns";
            //
            // numMaxRevs
            //
            numMaxRevs.Location = new System.Drawing.Point(114, 157);
            numMaxRevs.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMaxRevs.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numMaxRevs.Name = "numMaxRevs";
            numMaxRevs.Size = new System.Drawing.Size(46, 23);
            numMaxRevs.TabIndex = 13;
            numMaxRevs.Value = new decimal(new int[] { 12, 0, 0, 0 });
            //
            // lblMinRevs
            //
            lblMinRevs.Location = new System.Drawing.Point(168, 159);
            lblMinRevs.Name = "lblMinRevs";
            lblMinRevs.Size = new System.Drawing.Size(96, 20);
            lblMinRevs.TabIndex = 14;
            lblMinRevs.Text = "Min turns";
            //
            // numMinRevs
            //
            numMinRevs.Location = new System.Drawing.Point(268, 157);
            numMinRevs.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numMinRevs.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numMinRevs.Name = "numMinRevs";
            numMinRevs.Size = new System.Drawing.Size(46, 23);
            numMinRevs.TabIndex = 15;
            numMinRevs.Value = new decimal(new int[] { 2, 0, 0, 0 });
            //
            // lblDirection
            //
            lblDirection.Location = new System.Drawing.Point(322, 159);
            lblDirection.Name = "lblDirection";
            lblDirection.Size = new System.Drawing.Size(36, 20);
            lblDirection.TabIndex = 16;
            lblDirection.Text = "Dir";
            //
            // cmbDirection
            //
            cmbDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDirection.Location = new System.Drawing.Point(360, 157);
            cmbDirection.Name = "cmbDirection";
            cmbDirection.Size = new System.Drawing.Size(104, 23);
            cmbDirection.TabIndex = 17;
            cmbDirection.SelectedIndexChanged += CmbDirection_SelectedIndexChanged;
            //
            // lblFastBand
            //
            lblFastBand.Location = new System.Drawing.Point(14, 192);
            lblFastBand.Name = "lblFastBand";
            lblFastBand.Size = new System.Drawing.Size(68, 20);
            lblFastBand.TabIndex = 18;
            lblFastBand.Text = "Fast band";
            //
            // cmbFastBand
            //
            cmbFastBand.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFastBand.Location = new System.Drawing.Point(84, 190);
            cmbFastBand.Name = "cmbFastBand";
            cmbFastBand.Size = new System.Drawing.Size(82, 23);
            cmbFastBand.TabIndex = 19;
            //
            // lblViscosity
            //
            lblViscosity.Location = new System.Drawing.Point(168, 192);
            lblViscosity.Name = "lblViscosity";
            lblViscosity.Size = new System.Drawing.Size(96, 20);
            lblViscosity.TabIndex = 20;
            lblViscosity.Text = "Viscosity";
            //
            // numViscosity
            //
            numViscosity.DecimalPlaces = 1;
            numViscosity.Increment = new decimal(new int[] { 1, 0, 0, 65536 }); // 0.1
            numViscosity.Location = new System.Drawing.Point(268, 190);
            numViscosity.Minimum = new decimal(new int[] { 2, 0, 0, 65536 }); // 0.2
            numViscosity.Maximum = new decimal(new int[] { 30, 0, 0, 65536 }); // 3.0
            numViscosity.Name = "numViscosity";
            numViscosity.Size = new System.Drawing.Size(46, 23);
            numViscosity.TabIndex = 21;
            numViscosity.Value = new decimal(new int[] { 10, 0, 0, 65536 }); // 1.0
            //
            // cmbGifPlayMode (row D, gif mode only; self-describing items, no label)
            //
            cmbGifPlayMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGifPlayMode.Location = new System.Drawing.Point(14, 223);
            cmbGifPlayMode.Name = "cmbGifPlayMode";
            cmbGifPlayMode.Size = new System.Drawing.Size(190, 23);
            cmbGifPlayMode.TabIndex = 22;
            //
            // lblStart (row D, gif + play-during only)
            //
            lblStart.Location = new System.Drawing.Point(214, 225);
            lblStart.Name = "lblStart";
            lblStart.Size = new System.Drawing.Size(80, 20);
            lblStart.TabIndex = 23;
            lblStart.Text = "Start (s)";
            //
            // numStart
            //
            numStart.DecimalPlaces = 2;
            numStart.Increment = new decimal(new int[] { 25, 0, 0, 131072 }); // 0.25
            numStart.Location = new System.Drawing.Point(300, 223);
            numStart.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numStart.Maximum = new decimal(new int[] { 60000, 0, 0, 131072 }); // 600.00
            numStart.Name = "numStart";
            numStart.Size = new System.Drawing.Size(60, 23);
            numStart.TabIndex = 24;
            //
            // btnOK
            //
            btnOK.Location = new System.Drawing.Point(303, 260);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 25);
            btnOK.TabIndex = 25;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            //
            // btnCancel
            //
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(384, 260);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(82, 25);
            btnCancel.TabIndex = 26;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            //
            // QuicksandDialog
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(480, 300);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(numStart);
            Controls.Add(lblStart);
            Controls.Add(cmbGifPlayMode);
            Controls.Add(numViscosity);
            Controls.Add(lblViscosity);
            Controls.Add(cmbFastBand);
            Controls.Add(lblFastBand);
            Controls.Add(cmbDirection);
            Controls.Add(lblDirection);
            Controls.Add(numMinRevs);
            Controls.Add(lblMinRevs);
            Controls.Add(numMaxRevs);
            Controls.Add(lblMaxRevs);
            Controls.Add(numFps);
            Controls.Add(lblFps);
            Controls.Add(numDuration);
            Controls.Add(lblDuration);
            Controls.Add(numLayers);
            Controls.Add(lblLayers);
            Controls.Add(btnBrowseOutput);
            Controls.Add(txtOutputPath);
            Controls.Add(lblOutput);
            Controls.Add(btnBrowseInput);
            Controls.Add(txtInputPath);
            Controls.Add(lblInput);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "QuicksandDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Quicksand Flow";
            ((System.ComponentModel.ISupportInitialize)numLayers).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDuration).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFps).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRevs).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinRevs).EndInit();
            ((System.ComponentModel.ISupportInitialize)numViscosity).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStart).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
