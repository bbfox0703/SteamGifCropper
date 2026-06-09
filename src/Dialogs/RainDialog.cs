#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    // Rain-overlay (下雨) parameters dialog. Mirrors the other effect dialogs' inline layout + theme
    // handling. A translucent rain layer (amount / wind direction / wind strength / streak length, with an
    // optional "rain stops" fade-out) is drawn over the chosen [start, duration] window. BuildSettings()
    // does the control->settings mapping in one place.
    public class RainDialog : Form
    {
        private readonly bool _isGif;

        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseOutput = null!;
        private Label lblInput = null!;
        private Label lblOutput = null!;

        private Label lblRainAmount = null!;
        private NumericUpDown numRainAmount = null!;
        private Label lblWindDir = null!;
        private ComboBox cmbWindDir = null!;
        private Label lblWindStrength = null!;
        private NumericUpDown numWindStrength = null!;

        private Label lblDuration = null!;
        private NumericUpDown numDuration = null!;
        private Label lblFps = null!;
        private NumericUpDown numFps = null!;
        private Label lblStart = null!;
        private NumericUpDown numStart = null!;

        private Label lblDropLength = null!;
        private NumericUpDown numDropLength = null!;
        private CheckBox chkFadeOut = null!;
        private Label lblFadeSeconds = null!;
        private NumericUpDown numFadeSeconds = null!;

        private ComboBox cmbGifPlayMode = null!;
        private CheckBox chkKeepSize = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;

        public RainDialog(bool gifMode)
        {
            _isGif = gifMode;
            InitializeComponent();
            cmbGifPlayMode.Visible = gifMode;
            chkFadeOut.CheckedChanged += (s, e) => RefreshFadeEnabled();
            cmbGifPlayMode.SelectedIndexChanged += (s, e) => RefreshStartEnabled();
            UpdateUIText();
            RefreshFadeEnabled();
            RefreshStartEnabled();
            ApplyTheme();
        }

        // Single place that turns the controls into a settings object (avoids the "forgot to copy a
        // field" bug class).
        public RainSettings BuildSettings(bool isGif)
        {
            return new RainSettings
            {
                InputFilePath = txtInputPath.Text,
                OutputFilePath = txtOutputPath.Text,
                IsGif = isGif,
                Fps = (int)numFps.Value,
                DurationSeconds = (double)numDuration.Value,
                EffectStartSeconds = (double)numStart.Value,
                PlayGifDuringRain = cmbGifPlayMode.SelectedIndex == 0,
                KeepOriginalSize = chkKeepSize.Checked,
                RainAmount = (double)numRainAmount.Value,
                WindDirection = (RainWindDirection)cmbWindDir.SelectedIndex,
                WindStrength = (double)numWindStrength.Value,
                DropLength = (double)numDropLength.Value,
                FadeOut = chkFadeOut.Checked,
                FadeOutSeconds = (double)numFadeSeconds.Value,
            };
        }

        private void RefreshFadeEnabled()
        {
            numFadeSeconds.Enabled = chkFadeOut.Checked;
        }

        private void RefreshStartEnabled()
        {
            // The effect start window only applies to GIF "rain over playback".
            numStart.Enabled = _isGif && cmbGifPlayMode.SelectedIndex == 0;
        }

        private void UpdateUIText()
        {
            Text = Resources.RainDialog_Title;
            lblInput.Text = Resources.QuickDialog_InputLabel;
            lblOutput.Text = Resources.QuickDialog_OutputLabel;
            lblRainAmount.Text = Resources.RainDialog_RainAmount;
            lblWindDir.Text = Resources.RainDialog_WindDir;
            lblWindStrength.Text = Resources.RainDialog_WindStrength;
            lblDuration.Text = Resources.QuickDialog_Duration;
            lblFps.Text = Resources.SlotDialog_Fps;
            lblStart.Text = Resources.Dialog_StartSeconds;
            lblDropLength.Text = Resources.RainDialog_DropLength;
            chkFadeOut.Text = Resources.RainDialog_FadeOut;
            lblFadeSeconds.Text = Resources.RainDialog_FadeSeconds;
            chkKeepSize.Text = Resources.Dialog_KeepOriginalSize;
            btnBrowseInput.Text = Resources.Button_Browse;
            btnBrowseOutput.Text = Resources.Button_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;

            int dirSel = cmbWindDir.SelectedIndex < 0 ? 0 : cmbWindDir.SelectedIndex;
            cmbWindDir.Items.Clear();
            cmbWindDir.Items.Add(Resources.RainDir_None);
            cmbWindDir.Items.Add(Resources.RainDir_Left);
            cmbWindDir.Items.Add(Resources.RainDir_Right);
            cmbWindDir.SelectedIndex = dirSel;

            int playSel = cmbGifPlayMode.SelectedIndex < 0 ? 0 : cmbGifPlayMode.SelectedIndex;
            cmbGifPlayMode.Items.Clear();
            cmbGifPlayMode.Items.Add(Resources.RainDialog_GifPlayDuring);
            cmbGifPlayMode.Items.Add(Resources.RainDialog_GifFreeze);
            cmbGifPlayMode.SelectedIndex = playSel;
        }

        private void ApplyTheme()
        {
            bool isDark = WindowsThemeManager.IsDarkModeEnabled();
            if (isDark)
            {
                BackColor = Color.FromArgb(32, 32, 32);
                ForeColor = Color.White;
                ApplyDarkThemeToControls(Controls);
            }
            else
            {
                BackColor = SystemColors.Control;
                ForeColor = SystemColors.ControlText;
                ApplyLightThemeToControls(Controls);
            }

            if (IsHandleCreated)
            {
                WindowsThemeManager.SetDarkModeForWindow(Handle, isDark);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (value && IsHandleCreated)
            {
                WindowsThemeManager.SetDarkModeForWindow(Handle, WindowsThemeManager.IsDarkModeEnabled());
            }
        }

        private void ApplyDarkThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Label || control is CheckBox)
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
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.BackColor = Color.FromArgb(64, 64, 64);
                    numericUpDown.ForeColor = Color.White;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = Color.FromArgb(64, 64, 64);
                    comboBox.ForeColor = Color.White;
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
                if (control is Label || control is CheckBox)
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
                else if (control is NumericUpDown numericUpDown)
                {
                    numericUpDown.BackColor = SystemColors.Window;
                    numericUpDown.ForeColor = SystemColors.WindowText;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.BackColor = SystemColors.Window;
                    comboBox.ForeColor = SystemColors.WindowText;
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
                Filter = _isGif ? Resources.FileDialog_GifAndWebpFilter : Resources.FileDialog_ImageAndGifFilter,
                Title = Resources.QuickDialog_InputLabel
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInputPath.Text = ofd.FileName;
                if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
                {
                    string dir = Path.GetDirectoryName(ofd.FileName) ?? string.Empty;
                    string name = Path.GetFileNameWithoutExtension(ofd.FileName) + "_rain.gif";
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
                    ? "output_rain.gif"
                    : Path.GetFileNameWithoutExtension(txtInputPath.Text) + "_rain.gif"
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
            DialogResult = DialogResult.OK;
            Close();
        }

        private static NumericUpDown MakeNum(int x, int y, int width, int decimals, decimal min, decimal max, decimal inc, decimal val)
        {
            var n = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(width, 23),
                DecimalPlaces = decimals,
            };
            n.Minimum = min;   // set range before value so the value isn't clamped to the default 0..100
            n.Maximum = max;
            n.Increment = inc;
            n.Value = val;
            return n;
        }

        private void InitializeComponent()
        {
            lblInput = new Label { Location = new Point(14, 9), Size = new Size(360, 20), Text = "Input" };
            txtInputPath = new TextBox { Location = new Point(14, 29), Size = new Size(418, 23), ReadOnly = true };
            btnBrowseInput = new Button { Location = new Point(440, 27), Size = new Size(86, 25), Text = "Browse", UseVisualStyleBackColor = true };
            btnBrowseInput.Click += BtnBrowseInput_Click;
            lblOutput = new Label { Location = new Point(14, 58), Size = new Size(360, 20), Text = "Output" };
            txtOutputPath = new TextBox { Location = new Point(14, 78), Size = new Size(418, 23), ReadOnly = true };
            btnBrowseOutput = new Button { Location = new Point(440, 76), Size = new Size(86, 25), Text = "Browse", UseVisualStyleBackColor = true };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            // Rain row
            lblRainAmount = new Label { Location = new Point(14, 116), Size = new Size(90, 20), Text = "Rain amount" };
            numRainAmount = MakeNum(106, 114, 56, 0, 0m, 100m, 1m, 40m);
            lblWindDir = new Label { Location = new Point(178, 116), Size = new Size(40, 20), Text = "Wind" };
            cmbWindDir = new ComboBox { Location = new Point(220, 114), Size = new Size(110, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            lblWindStrength = new Label { Location = new Point(344, 116), Size = new Size(64, 20), Text = "Strength" };
            numWindStrength = MakeNum(410, 114, 60, 0, 0m, 1200m, 10m, 150m);

            // Scene row
            lblDuration = new Label { Location = new Point(14, 149), Size = new Size(100, 20), Text = "Duration" };
            numDuration = MakeNum(116, 147, 56, 2, 0.10m, 120.00m, 0.25m, 6.00m);
            lblFps = new Label { Location = new Point(178, 149), Size = new Size(34, 20), Text = "FPS" };
            numFps = MakeNum(214, 147, 46, 0, 5m, 60m, 1m, 20m);
            lblStart = new Label { Location = new Point(280, 149), Size = new Size(80, 20), Text = "Start" };
            numStart = MakeNum(362, 147, 56, 2, 0.00m, 120.00m, 0.25m, 0.00m);

            // Look + fade row
            lblDropLength = new Label { Location = new Point(14, 182), Size = new Size(90, 20), Text = "Streak len" };
            numDropLength = MakeNum(106, 180, 56, 0, 4m, 80m, 1m, 16m);
            chkFadeOut = new CheckBox { Location = new Point(178, 181), Size = new Size(210, 22), Text = "Rain stops (fade out)" };
            lblFadeSeconds = new Label { Location = new Point(392, 182), Size = new Size(70, 20), Text = "Fade (s)" };
            numFadeSeconds = MakeNum(466, 180, 56, 2, 0.10m, 30.00m, 0.25m, 1.00m);

            // Playback mode (GIF only) + keep size
            cmbGifPlayMode = new ComboBox { Location = new Point(14, 214), Size = new Size(200, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            chkKeepSize = new CheckBox { Location = new Point(230, 215), Size = new Size(280, 22), Text = "Keep original size", UseVisualStyleBackColor = true };

            btnOK = new Button { Location = new Point(363, 252), Size = new Size(75, 25), Text = "OK", UseVisualStyleBackColor = true };
            btnOK.Click += BtnOK_Click;
            btnCancel = new Button { Location = new Point(444, 252), Size = new Size(82, 25), Text = "Cancel", DialogResult = DialogResult.Cancel, UseVisualStyleBackColor = true };

            SuspendLayout();
            Controls.Add(lblInput);
            Controls.Add(txtInputPath);
            Controls.Add(btnBrowseInput);
            Controls.Add(lblOutput);
            Controls.Add(txtOutputPath);
            Controls.Add(btnBrowseOutput);
            Controls.Add(lblRainAmount);
            Controls.Add(numRainAmount);
            Controls.Add(lblWindDir);
            Controls.Add(cmbWindDir);
            Controls.Add(lblWindStrength);
            Controls.Add(numWindStrength);
            Controls.Add(lblDuration);
            Controls.Add(numDuration);
            Controls.Add(lblFps);
            Controls.Add(numFps);
            Controls.Add(lblStart);
            Controls.Add(numStart);
            Controls.Add(lblDropLength);
            Controls.Add(numDropLength);
            Controls.Add(chkFadeOut);
            Controls.Add(lblFadeSeconds);
            Controls.Add(numFadeSeconds);
            Controls.Add(cmbGifPlayMode);
            Controls.Add(chkKeepSize);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            AcceptButton = btnOK;
            ClientSize = new Size(540, 292);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RainDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Rain";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
