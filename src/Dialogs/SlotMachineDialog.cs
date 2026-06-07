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
    public class SlotMachineDialog : Form
    {
        public string InputFilePath { get; private set; } = string.Empty;
        public string OutputFilePath { get; private set; } = string.Empty;
        public bool IsGif { get; private set; }
        public int DurationSeconds { get; private set; } = 3;
        public int Fps { get; private set; } = 20;
        public int Spins { get; private set; } = 4;
        public double StaggerSeconds { get; private set; } = 0.3;
        public int HoldSeconds { get; private set; } = 1;

        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseOutput = null!;
        private NumericUpDown numDuration = null!;
        private NumericUpDown numFps = null!;
        private NumericUpDown numSpins = null!;
        private NumericUpDown numStagger = null!;
        private NumericUpDown numHold = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Label lblInput = null!;
        private Label lblOutput = null!;
        private Label lblDuration = null!;
        private Label lblFps = null!;
        private Label lblSpins = null!;
        private Label lblStagger = null!;
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
            lblFps.Text = Resources.SlotDialog_Fps;
            lblSpins.Text = Resources.SlotDialog_Spins;
            lblStagger.Text = Resources.SlotDialog_Stagger;
            lblHold.Text = Resources.SlotDialog_Hold;
            btnBrowseInput.Text = Resources.Button_Browse;
            btnBrowseOutput.Text = Resources.Button_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;
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
            Fps = (int)numFps.Value;
            Spins = (int)numSpins.Value;
            StaggerSeconds = (double)numStagger.Value;
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
            lblFps = new Label();
            numFps = new NumericUpDown();
            lblSpins = new Label();
            numSpins = new NumericUpDown();
            lblStagger = new Label();
            numStagger = new NumericUpDown();
            lblHold = new Label();
            numHold = new NumericUpDown();
            btnOK = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numDuration).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFps).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSpins).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStagger).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numHold).BeginInit();
            SuspendLayout();
            //
            // lblInput
            //
            lblInput.Location = new System.Drawing.Point(14, 9);
            lblInput.Name = "lblInput";
            lblInput.Size = new System.Drawing.Size(330, 20);
            lblInput.TabIndex = 0;
            lblInput.Text = "Input";
            //
            // txtInputPath
            //
            txtInputPath.Location = new System.Drawing.Point(14, 29);
            txtInputPath.Name = "txtInputPath";
            txtInputPath.ReadOnly = true;
            txtInputPath.Size = new System.Drawing.Size(333, 23);
            txtInputPath.TabIndex = 1;
            //
            // btnBrowseInput
            //
            btnBrowseInput.Location = new System.Drawing.Point(353, 27);
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
            lblOutput.Size = new System.Drawing.Size(330, 20);
            lblOutput.TabIndex = 3;
            lblOutput.Text = "Output";
            //
            // txtOutputPath
            //
            txtOutputPath.Location = new System.Drawing.Point(14, 84);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.ReadOnly = true;
            txtOutputPath.Size = new System.Drawing.Size(333, 23);
            txtOutputPath.TabIndex = 4;
            //
            // btnBrowseOutput
            //
            btnBrowseOutput.Location = new System.Drawing.Point(353, 82);
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
            lblDuration.Size = new System.Drawing.Size(110, 20);
            lblDuration.TabIndex = 6;
            lblDuration.Text = "Duration";
            //
            // numDuration
            //
            numDuration.Location = new System.Drawing.Point(130, 124);
            numDuration.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDuration.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numDuration.Name = "numDuration";
            numDuration.Size = new System.Drawing.Size(60, 23);
            numDuration.TabIndex = 7;
            numDuration.Value = new decimal(new int[] { 3, 0, 0, 0 });
            //
            // lblFps
            //
            lblFps.Location = new System.Drawing.Point(240, 126);
            lblFps.Name = "lblFps";
            lblFps.Size = new System.Drawing.Size(70, 20);
            lblFps.TabIndex = 8;
            lblFps.Text = "FPS";
            //
            // numFps
            //
            numFps.Location = new System.Drawing.Point(316, 124);
            numFps.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            numFps.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            numFps.Name = "numFps";
            numFps.Size = new System.Drawing.Size(60, 23);
            numFps.TabIndex = 9;
            numFps.Value = new decimal(new int[] { 20, 0, 0, 0 });
            //
            // lblSpins
            //
            lblSpins.Location = new System.Drawing.Point(14, 159);
            lblSpins.Name = "lblSpins";
            lblSpins.Size = new System.Drawing.Size(110, 20);
            lblSpins.TabIndex = 10;
            lblSpins.Text = "Spins";
            //
            // numSpins
            //
            numSpins.Location = new System.Drawing.Point(130, 157);
            numSpins.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numSpins.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numSpins.Name = "numSpins";
            numSpins.Size = new System.Drawing.Size(60, 23);
            numSpins.TabIndex = 11;
            numSpins.Value = new decimal(new int[] { 4, 0, 0, 0 });
            //
            // lblStagger
            //
            lblStagger.Location = new System.Drawing.Point(240, 159);
            lblStagger.Name = "lblStagger";
            lblStagger.Size = new System.Drawing.Size(70, 20);
            lblStagger.TabIndex = 12;
            lblStagger.Text = "Stagger";
            //
            // numStagger
            //
            numStagger.DecimalPlaces = 1;
            numStagger.Increment = new decimal(new int[] { 1, 0, 0, 65536 }); // 0.1
            numStagger.Location = new System.Drawing.Point(316, 157);
            numStagger.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numStagger.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            numStagger.Name = "numStagger";
            numStagger.Size = new System.Drawing.Size(60, 23);
            numStagger.TabIndex = 13;
            numStagger.Value = new decimal(new int[] { 3, 0, 0, 65536 }); // 0.3
            //
            // lblHold
            //
            lblHold.Location = new System.Drawing.Point(14, 192);
            lblHold.Name = "lblHold";
            lblHold.Size = new System.Drawing.Size(110, 20);
            lblHold.TabIndex = 14;
            lblHold.Text = "Hold";
            //
            // numHold
            //
            numHold.Location = new System.Drawing.Point(130, 190);
            numHold.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            numHold.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numHold.Name = "numHold";
            numHold.Size = new System.Drawing.Size(60, 23);
            numHold.TabIndex = 15;
            numHold.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // btnOK
            //
            btnOK.Location = new System.Drawing.Point(272, 230);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 25);
            btnOK.TabIndex = 16;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            //
            // btnCancel
            //
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(353, 230);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(88, 25);
            btnCancel.TabIndex = 17;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            //
            // SlotMachineDialog
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(455, 270);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(numHold);
            Controls.Add(lblHold);
            Controls.Add(numStagger);
            Controls.Add(lblStagger);
            Controls.Add(numSpins);
            Controls.Add(lblSpins);
            Controls.Add(numFps);
            Controls.Add(lblFps);
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
            ((System.ComponentModel.ISupportInitialize)numFps).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSpins).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStagger).EndInit();
            ((System.ComponentModel.ISupportInitialize)numHold).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
