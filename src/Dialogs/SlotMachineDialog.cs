#nullable enable
using System;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    // Slot-machine (拉霸) parameters dialog. Mirrors ScrollStaticImageDialog's inline layout +
    // theme handling. gifMode toggles the input file filter (animated GIF vs any image) and hides
    // the "hold result" field (a GIF plays after the reels lock instead of holding a still frame).
    //
    // Per-reel stop time and revolution count are randomized (Duration/Spins +/- their variance %),
    // so which reel stops first and which spins longest is non-deterministic — that replaces the old
    // fixed "stagger" setting.
    public class SlotMachineDialog : Form
    {
        public string InputFilePath { get; private set; } = string.Empty;
        public string OutputFilePath { get; private set; } = string.Empty;
        public bool IsGif { get; private set; }
        public int DurationSeconds { get; private set; } = 3;
        public int DurationVariancePercent { get; private set; } = 20;
        public int Fps { get; private set; } = 15;
        public int Spins { get; private set; } = 4;
        public int SpinsVariancePercent { get; private set; } = 25;
        public double BounceSeconds { get; private set; } = 0.5;
        public bool TopToBottom { get; private set; } = true;
        public int HoldSeconds { get; private set; } = 1;

        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseOutput = null!;
        private NumericUpDown numDuration = null!;
        private NumericUpDown numDurVar = null!;
        private NumericUpDown numFps = null!;
        private NumericUpDown numSpins = null!;
        private NumericUpDown numSpinVar = null!;
        private ComboBox cmbDirection = null!;
        private NumericUpDown numBounce = null!;
        private NumericUpDown numHold = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Label lblInput = null!;
        private Label lblOutput = null!;
        private Label lblDuration = null!;
        private Label lblDurVar = null!;
        private Label lblFps = null!;
        private Label lblSpins = null!;
        private Label lblSpinVar = null!;
        private Label lblDirection = null!;
        private Label lblBounce = null!;
        private Label lblHold = null!;

        public SlotMachineDialog(bool gifMode)
        {
            IsGif = gifMode;
            InitializeComponent();
            // The GIF variant plays the animation after the reels lock, so a static "hold" makes no sense.
            lblHold.Visible = !gifMode;
            numHold.Visible = !gifMode;
            UpdateUIText();
            ApplyTheme();
        }

        private void UpdateUIText()
        {
            Text = Resources.SlotDialog_Title;
            lblInput.Text = Resources.SlotDialog_InputLabel;
            lblOutput.Text = Resources.SlotDialog_OutputLabel;
            lblDuration.Text = Resources.SlotDialog_Duration;
            lblDurVar.Text = Resources.SlotDialog_Variance;
            lblFps.Text = Resources.SlotDialog_Fps;
            lblSpins.Text = Resources.SlotDialog_Spins;
            lblSpinVar.Text = Resources.SlotDialog_Variance;
            lblDirection.Text = Resources.SlotDialog_Direction;
            lblBounce.Text = Resources.SlotDialog_Bounce;
            lblHold.Text = Resources.SlotDialog_Hold;
            btnBrowseInput.Text = Resources.Button_Browse;
            btnBrowseOutput.Text = Resources.Button_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;

            int selected = cmbDirection.SelectedIndex < 0 ? 0 : cmbDirection.SelectedIndex;
            cmbDirection.Items.Clear();
            cmbDirection.Items.Add(Resources.SlotDialog_DirTopDown);
            cmbDirection.Items.Add(Resources.SlotDialog_DirBottomUp);
            cmbDirection.SelectedIndex = selected;
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
                Filter = IsGif ? Resources.FileDialog_GifFilter : Resources.FileDialog_ImageAndGifFilter,
                Title = Resources.SlotDialog_InputLabel
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInputPath.Text = ofd.FileName;
                if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
                {
                    string dir = Path.GetDirectoryName(ofd.FileName) ?? string.Empty;
                    string name = Path.GetFileNameWithoutExtension(ofd.FileName) + "_slot.gif";
                    txtOutputPath.Text = Path.Combine(dir, name);
                }
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = Resources.SlotDialog_OutputLabel,
                FileName = string.IsNullOrEmpty(txtInputPath.Text)
                    ? "output_slot.gif"
                    : Path.GetFileNameWithoutExtension(txtInputPath.Text) + "_slot.gif"
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
            DurationSeconds = (int)numDuration.Value;
            DurationVariancePercent = (int)numDurVar.Value;
            Fps = (int)numFps.Value;
            Spins = (int)numSpins.Value;
            SpinsVariancePercent = (int)numSpinVar.Value;
            BounceSeconds = (double)numBounce.Value;
            TopToBottom = cmbDirection.SelectedIndex == 0;
            HoldSeconds = (int)numHold.Value;
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
            lblDuration = new Label();
            numDuration = new NumericUpDown();
            lblDurVar = new Label();
            numDurVar = new NumericUpDown();
            lblFps = new Label();
            numFps = new NumericUpDown();
            lblSpins = new Label();
            numSpins = new NumericUpDown();
            lblSpinVar = new Label();
            numSpinVar = new NumericUpDown();
            lblDirection = new Label();
            cmbDirection = new ComboBox();
            lblBounce = new Label();
            numBounce = new NumericUpDown();
            lblHold = new Label();
            numHold = new NumericUpDown();
            btnOK = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numDuration).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDurVar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFps).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSpins).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSpinVar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numBounce).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numHold).BeginInit();
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
            // lblDuration
            //
            lblDuration.Location = new System.Drawing.Point(14, 126);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new System.Drawing.Size(98, 20);
            lblDuration.TabIndex = 6;
            lblDuration.Text = "Duration";
            //
            // numDuration
            //
            numDuration.Location = new System.Drawing.Point(114, 124);
            numDuration.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDuration.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numDuration.Name = "numDuration";
            numDuration.Size = new System.Drawing.Size(46, 23);
            numDuration.TabIndex = 7;
            numDuration.Value = new decimal(new int[] { 3, 0, 0, 0 });
            //
            // lblDurVar
            //
            lblDurVar.Location = new System.Drawing.Point(168, 126);
            lblDurVar.Name = "lblDurVar";
            lblDurVar.Size = new System.Drawing.Size(66, 20);
            lblDurVar.TabIndex = 8;
            lblDurVar.Text = "Variance";
            //
            // numDurVar
            //
            numDurVar.Location = new System.Drawing.Point(236, 124);
            numDurVar.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numDurVar.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            numDurVar.Name = "numDurVar";
            numDurVar.Size = new System.Drawing.Size(46, 23);
            numDurVar.TabIndex = 9;
            numDurVar.Value = new decimal(new int[] { 20, 0, 0, 0 });
            //
            // lblFps
            //
            lblFps.Location = new System.Drawing.Point(292, 126);
            lblFps.Name = "lblFps";
            lblFps.Size = new System.Drawing.Size(34, 20);
            lblFps.TabIndex = 10;
            lblFps.Text = "FPS";
            //
            // numFps
            //
            numFps.Location = new System.Drawing.Point(330, 124);
            numFps.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            numFps.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numFps.Name = "numFps";
            numFps.Size = new System.Drawing.Size(46, 23);
            numFps.TabIndex = 11;
            numFps.Value = new decimal(new int[] { 15, 0, 0, 0 });
            //
            // lblSpins
            //
            lblSpins.Location = new System.Drawing.Point(14, 159);
            lblSpins.Name = "lblSpins";
            lblSpins.Size = new System.Drawing.Size(98, 20);
            lblSpins.TabIndex = 12;
            lblSpins.Text = "Spins";
            //
            // numSpins
            //
            numSpins.Location = new System.Drawing.Point(114, 157);
            numSpins.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numSpins.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numSpins.Name = "numSpins";
            numSpins.Size = new System.Drawing.Size(46, 23);
            numSpins.TabIndex = 13;
            numSpins.Value = new decimal(new int[] { 4, 0, 0, 0 });
            //
            // lblSpinVar
            //
            lblSpinVar.Location = new System.Drawing.Point(168, 159);
            lblSpinVar.Name = "lblSpinVar";
            lblSpinVar.Size = new System.Drawing.Size(66, 20);
            lblSpinVar.TabIndex = 14;
            lblSpinVar.Text = "Variance";
            //
            // numSpinVar
            //
            numSpinVar.Location = new System.Drawing.Point(236, 157);
            numSpinVar.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numSpinVar.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numSpinVar.Name = "numSpinVar";
            numSpinVar.Size = new System.Drawing.Size(46, 23);
            numSpinVar.TabIndex = 15;
            numSpinVar.Value = new decimal(new int[] { 25, 0, 0, 0 });
            //
            // lblDirection
            //
            lblDirection.Location = new System.Drawing.Point(292, 159);
            lblDirection.Name = "lblDirection";
            lblDirection.Size = new System.Drawing.Size(40, 20);
            lblDirection.TabIndex = 16;
            lblDirection.Text = "Dir";
            //
            // cmbDirection
            //
            cmbDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDirection.Location = new System.Drawing.Point(332, 157);
            cmbDirection.Name = "cmbDirection";
            cmbDirection.Size = new System.Drawing.Size(130, 23);
            cmbDirection.TabIndex = 17;
            //
            // lblBounce
            //
            lblBounce.Location = new System.Drawing.Point(14, 192);
            lblBounce.Name = "lblBounce";
            lblBounce.Size = new System.Drawing.Size(98, 20);
            lblBounce.TabIndex = 18;
            lblBounce.Text = "Bounce";
            //
            // numBounce
            //
            numBounce.DecimalPlaces = 1;
            numBounce.Increment = new decimal(new int[] { 1, 0, 0, 65536 }); // 0.1
            numBounce.Location = new System.Drawing.Point(114, 190);
            numBounce.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numBounce.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
            numBounce.Name = "numBounce";
            numBounce.Size = new System.Drawing.Size(46, 23);
            numBounce.TabIndex = 19;
            numBounce.Value = new decimal(new int[] { 5, 0, 0, 65536 }); // 0.5
            //
            // lblHold
            //
            lblHold.Location = new System.Drawing.Point(168, 192);
            lblHold.Name = "lblHold";
            lblHold.Size = new System.Drawing.Size(110, 20);
            lblHold.TabIndex = 20;
            lblHold.Text = "Hold";
            //
            // numHold
            //
            numHold.Location = new System.Drawing.Point(284, 190);
            numHold.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numHold.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numHold.Name = "numHold";
            numHold.Size = new System.Drawing.Size(46, 23);
            numHold.TabIndex = 21;
            numHold.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // btnOK
            //
            btnOK.Location = new System.Drawing.Point(303, 228);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 25);
            btnOK.TabIndex = 22;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            //
            // btnCancel
            //
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(384, 228);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(82, 25);
            btnCancel.TabIndex = 23;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            //
            // SlotMachineDialog
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(480, 268);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(numHold);
            Controls.Add(lblHold);
            Controls.Add(numBounce);
            Controls.Add(lblBounce);
            Controls.Add(cmbDirection);
            Controls.Add(lblDirection);
            Controls.Add(numSpinVar);
            Controls.Add(lblSpinVar);
            Controls.Add(numSpins);
            Controls.Add(lblSpins);
            Controls.Add(numFps);
            Controls.Add(lblFps);
            Controls.Add(numDurVar);
            Controls.Add(lblDurVar);
            Controls.Add(numDuration);
            Controls.Add(lblDuration);
            Controls.Add(btnBrowseOutput);
            Controls.Add(txtOutputPath);
            Controls.Add(lblOutput);
            Controls.Add(btnBrowseInput);
            Controls.Add(txtInputPath);
            Controls.Add(lblInput);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SlotMachineDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Slot Machine";
            ((System.ComponentModel.ISupportInitialize)numDuration).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDurVar).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFps).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSpins).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSpinVar).EndInit();
            ((System.ComponentModel.ISupportInitialize)numBounce).EndInit();
            ((System.ComponentModel.ISupportInitialize)numHold).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
