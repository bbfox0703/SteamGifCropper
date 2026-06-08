#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    // Water-ripple (水波紋) parameters dialog. Mirrors the other dialogs' inline layout + theme handling.
    // The medium (wave speed / wavelength / damping / strength / threshold) is shared; up to 3 drops each
    // carry position / start time / intensity. BuildSettings() does the control->settings mapping in one
    // place (so a new field can't be silently dropped on the way to the engine).
    public class RippleDialog : Form
    {
        private const int DropCount = 3;

        private readonly bool _isGif;

        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseOutput = null!;
        private Label lblInput = null!;
        private Label lblOutput = null!;

        private Label lblDuration = null!;
        private NumericUpDown numDuration = null!;
        private Label lblFps = null!;
        private NumericUpDown numFps = null!;
        private ComboBox cmbGifPlayMode = null!;

        private Label lblWaveSpeed = null!;
        private NumericUpDown numWaveSpeed = null!;
        private Label lblWavelength = null!;
        private NumericUpDown numWavelength = null!;
        private Label lblStrength = null!;
        private NumericUpDown numStrength = null!;
        private Label lblSpatialDamping = null!;
        private NumericUpDown numSpatialDamping = null!;
        private Label lblTimeDamping = null!;
        private NumericUpDown numTimeDamping = null!;
        private Label lblThreshold = null!;
        private NumericUpDown numThreshold = null!;

        private Label lblDropsTitle = null!;
        private Label lblHdrX = null!;
        private Label lblHdrY = null!;
        private Label lblHdrTime = null!;
        private Label lblHdrIntensity = null!;
        private readonly CheckBox[] chkDrops = new CheckBox[DropCount];
        private readonly NumericUpDown[] numDropX = new NumericUpDown[DropCount];
        private readonly NumericUpDown[] numDropY = new NumericUpDown[DropCount];
        private readonly NumericUpDown[] numDropTime = new NumericUpDown[DropCount];
        private readonly NumericUpDown[] numDropIntensity = new NumericUpDown[DropCount];
        private readonly Button[] btnPickDrop = new Button[DropCount];

        private Button btnOK = null!;
        private Button btnCancel = null!;

        public RippleDialog(bool gifMode)
        {
            _isGif = gifMode;
            InitializeComponent();
            cmbGifPlayMode.Visible = gifMode;
            // Editing any of a drop's fields auto-enables it (so "I just set the intensity" actually takes
            // effect). Wired AFTER InitializeComponent so setting the initial values doesn't trigger it.
            for (int i = 0; i < DropCount; i++)
            {
                int idx = i;
                EventHandler enable = (s, e) => chkDrops[idx].Checked = true;
                numDropX[i].ValueChanged += enable;
                numDropY[i].ValueChanged += enable;
                numDropTime[i].ValueChanged += enable;
                numDropIntensity[i].ValueChanged += enable;
            }
            UpdateUIText();
            ApplyTheme();
        }

        // Single place that turns the controls into a settings object (avoids the "forgot to copy a field"
        // bug class). Only ticked drops are included.
        public RippleSettings BuildSettings(bool isGif)
        {
            var s = new RippleSettings
            {
                InputFilePath = txtInputPath.Text,
                OutputFilePath = txtOutputPath.Text,
                IsGif = isGif,
                Fps = (int)numFps.Value,
                DurationSeconds = (double)numDuration.Value,
                PlayGifDuringRipple = cmbGifPlayMode.SelectedIndex == 0,
                WaveSpeed = (double)numWaveSpeed.Value,
                Wavelength = (double)numWavelength.Value,
                SpatialDamping = (double)numSpatialDamping.Value,
                TimeDamping = (double)numTimeDamping.Value,
                Strength = (double)numStrength.Value,
                Threshold = (double)numThreshold.Value,
            };
            for (int i = 0; i < DropCount; i++)
            {
                if (!chkDrops[i].Checked) continue;
                s.Drops.Add(new RippleDrop(
                    (double)numDropX[i].Value,
                    (double)numDropY[i].Value,
                    (double)numDropTime[i].Value,
                    (double)numDropIntensity[i].Value));
            }
            return s;
        }

        private void UpdateUIText()
        {
            Text = Resources.RippleDialog_Title;
            lblInput.Text = Resources.QuickDialog_InputLabel;
            lblOutput.Text = Resources.QuickDialog_OutputLabel;
            lblDuration.Text = Resources.QuickDialog_Duration;
            lblFps.Text = Resources.SlotDialog_Fps;
            lblWaveSpeed.Text = Resources.RippleDialog_WaveSpeed;
            lblWavelength.Text = Resources.RippleDialog_Wavelength;
            lblStrength.Text = Resources.RippleDialog_Strength;
            lblSpatialDamping.Text = Resources.RippleDialog_SpatialDamping;
            lblTimeDamping.Text = Resources.RippleDialog_TimeDamping;
            lblThreshold.Text = Resources.RippleDialog_Threshold;
            lblDropsTitle.Text = Resources.RippleDialog_DropsTitle;
            lblHdrX.Text = Resources.RippleDialog_ColX;
            lblHdrY.Text = Resources.RippleDialog_ColY;
            lblHdrTime.Text = Resources.RippleDialog_ColTime;
            lblHdrIntensity.Text = Resources.RippleDialog_ColIntensity;
            btnBrowseInput.Text = Resources.Button_Browse;
            btnBrowseOutput.Text = Resources.Button_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;
            for (int i = 0; i < DropCount; i++)
            {
                chkDrops[i].Text = string.Format(Resources.RippleDialog_DropN, i + 1);
                btnPickDrop[i].Text = Resources.RippleDialog_Pick;
            }

            int playSel = cmbGifPlayMode.SelectedIndex < 0 ? 0 : cmbGifPlayMode.SelectedIndex;
            cmbGifPlayMode.Items.Clear();
            cmbGifPlayMode.Items.Add(Resources.RippleDialog_GifPlayDuring);
            cmbGifPlayMode.Items.Add(Resources.RippleDialog_GifFreeze);
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
                Filter = _isGif ? Resources.FileDialog_GifFilter : Resources.FileDialog_ImageAndGifFilter,
                Title = Resources.QuickDialog_InputLabel
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInputPath.Text = ofd.FileName;
                if (string.IsNullOrWhiteSpace(txtOutputPath.Text))
                {
                    string dir = Path.GetDirectoryName(ofd.FileName) ?? string.Empty;
                    string name = Path.GetFileNameWithoutExtension(ofd.FileName) + "_ripple.gif";
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
                    ? "output_ripple.gif"
                    : Path.GetFileNameWithoutExtension(txtInputPath.Text) + "_ripple.gif"
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

        // Opens the click-to-pick overlay on frame 0 of the current input and fills the drop's X/Y. The
        // user can still type the numbers afterwards (e.g. to place a drop off-screen).
        private void PickDrop(int i)
        {
            if (string.IsNullOrWhiteSpace(txtInputPath.Text) || !File.Exists(txtInputPath.Text))
            {
                MessageBox.Show(this, Resources.ScrollDialog_InputRequired, Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using var picker = new RippleDropPickerForm(txtInputPath.Text, (int)numDropX[i].Value, (int)numDropY[i].Value);
                if (picker.ShowDialog(this) == DialogResult.OK)
                {
                    numDropX[i].Value = ClampToRange(numDropX[i], picker.PickedX);
                    numDropY[i].Value = ClampToRange(numDropY[i], picker.PickedY);
                    chkDrops[i].Checked = true; // picking a position implies enabling the drop
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(Resources.Error_Occurred, ex.Message), Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static decimal ClampToRange(NumericUpDown control, int value)
        {
            decimal d = value;
            if (d < control.Minimum) return control.Minimum;
            if (d > control.Maximum) return control.Maximum;
            return d;
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

            // Scene row
            lblDuration = new Label { Location = new Point(14, 114), Size = new Size(70, 20), Text = "Duration" };
            numDuration = MakeNum(88, 112, 56, 2, 0.10m, 30.00m, 0.25m, 4.00m);
            lblFps = new Label { Location = new Point(152, 114), Size = new Size(34, 20), Text = "FPS" };
            numFps = MakeNum(188, 112, 46, 0, 5m, 60m, 1m, 20m);
            cmbGifPlayMode = new ComboBox { Location = new Point(244, 112), Size = new Size(200, 23), DropDownStyle = ComboBoxStyle.DropDownList };

            // Medium A
            lblWaveSpeed = new Label { Location = new Point(14, 147), Size = new Size(66, 20), Text = "Speed" };
            numWaveSpeed = MakeNum(82, 145, 60, 0, 10m, 2000m, 10m, 220m);
            lblWavelength = new Label { Location = new Point(150, 147), Size = new Size(66, 20), Text = "Wavelen" };
            numWavelength = MakeNum(218, 145, 56, 0, 4m, 400m, 1m, 36m);
            lblStrength = new Label { Location = new Point(282, 147), Size = new Size(70, 20), Text = "Displace" };
            numStrength = MakeNum(356, 145, 56, 1, 0.0m, 50.0m, 0.5m, 8.0m);

            // Medium B
            lblSpatialDamping = new Label { Location = new Point(14, 180), Size = new Size(66, 20), Text = "Dist fade" };
            numSpatialDamping = MakeNum(82, 178, 60, 3, 0.000m, 0.100m, 0.001m, 0.004m);
            lblTimeDamping = new Label { Location = new Point(150, 180), Size = new Size(66, 20), Text = "Time fade" };
            numTimeDamping = MakeNum(218, 178, 56, 2, 0.00m, 10.00m, 0.10m, 0.80m);
            lblThreshold = new Label { Location = new Point(282, 180), Size = new Size(70, 20), Text = "Vanish" };
            numThreshold = MakeNum(356, 178, 56, 2, 0.01m, 1.00m, 0.01m, 0.03m);

            // Drops header
            lblDropsTitle = new Label { Location = new Point(14, 214), Size = new Size(90, 20), Text = "Drops" };
            lblHdrX = new Label { Location = new Point(110, 214), Size = new Size(60, 20), Text = "X" };
            lblHdrY = new Label { Location = new Point(174, 214), Size = new Size(60, 20), Text = "Y" };
            lblHdrTime = new Label { Location = new Point(238, 214), Size = new Size(56, 20), Text = "Start" };
            lblHdrIntensity = new Label { Location = new Point(298, 214), Size = new Size(56, 20), Text = "Power" };

            // Randomize the 3 default drop positions on each open (X across the 766 canvas, Y in the upper
            // band that fits any GIF height). The user can still pick or type — including negatives /
            // beyond the canvas for off-screen drops.
            var rng = new Random();
            decimal[] defX = { rng.Next(0, 766), rng.Next(0, 766), rng.Next(0, 766) };
            decimal[] defY = { rng.Next(0, 300), rng.Next(0, 300), rng.Next(0, 300) };
            decimal[] defTime = { 0.00m, 0.50m, 1.00m };
            decimal[] defIntensity = { 1.00m, 1.00m, 0.80m };
            for (int i = 0; i < DropCount; i++)
            {
                int y = 236 + i * 29;
                chkDrops[i] = new CheckBox { Location = new Point(14, y + 2), Size = new Size(94, 20), Text = "Drop " + (i + 1), Checked = i == 0 };
                numDropX[i] = MakeNum(110, y, 60, 0, -2000m, 3000m, 1m, defX[i]);
                numDropY[i] = MakeNum(174, y, 60, 0, -2000m, 3000m, 1m, defY[i]);
                numDropTime[i] = MakeNum(238, y, 56, 2, 0.00m, 30.00m, 0.25m, defTime[i]);
                numDropIntensity[i] = MakeNum(298, y, 56, 2, 0.10m, 5.00m, 0.10m, defIntensity[i]);
                btnPickDrop[i] = new Button { Location = new Point(360, y - 1), Size = new Size(56, 23), Text = "Pick", UseVisualStyleBackColor = true };
                int idx = i;
                btnPickDrop[i].Click += (s, e) => PickDrop(idx);
            }

            btnOK = new Button { Location = new Point(363, 330), Size = new Size(75, 25), Text = "OK", UseVisualStyleBackColor = true };
            btnOK.Click += BtnOK_Click;
            btnCancel = new Button { Location = new Point(444, 330), Size = new Size(82, 25), Text = "Cancel", DialogResult = DialogResult.Cancel, UseVisualStyleBackColor = true };

            SuspendLayout();
            Controls.Add(lblInput);
            Controls.Add(txtInputPath);
            Controls.Add(btnBrowseInput);
            Controls.Add(lblOutput);
            Controls.Add(txtOutputPath);
            Controls.Add(btnBrowseOutput);
            Controls.Add(lblDuration);
            Controls.Add(numDuration);
            Controls.Add(lblFps);
            Controls.Add(numFps);
            Controls.Add(cmbGifPlayMode);
            Controls.Add(lblWaveSpeed);
            Controls.Add(numWaveSpeed);
            Controls.Add(lblWavelength);
            Controls.Add(numWavelength);
            Controls.Add(lblStrength);
            Controls.Add(numStrength);
            Controls.Add(lblSpatialDamping);
            Controls.Add(numSpatialDamping);
            Controls.Add(lblTimeDamping);
            Controls.Add(numTimeDamping);
            Controls.Add(lblThreshold);
            Controls.Add(numThreshold);
            Controls.Add(lblDropsTitle);
            Controls.Add(lblHdrX);
            Controls.Add(lblHdrY);
            Controls.Add(lblHdrTime);
            Controls.Add(lblHdrIntensity);
            for (int i = 0; i < DropCount; i++)
            {
                Controls.Add(chkDrops[i]);
                Controls.Add(numDropX[i]);
                Controls.Add(numDropY[i]);
                Controls.Add(numDropTime[i]);
                Controls.Add(numDropIntensity[i]);
                Controls.Add(btnPickDrop[i]);
            }
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            AcceptButton = btnOK;
            ClientSize = new Size(540, 372);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RippleDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Water Ripple";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
