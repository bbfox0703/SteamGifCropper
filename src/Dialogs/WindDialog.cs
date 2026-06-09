#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    // Wind-sway (微/強風吹襲) parameters dialog. Mirrors the other dialogs' inline layout + theme
    // handling. The medium (direction / wavelength / speed / strength / bend / flutter) is shared; in
    // Normal mode up to 3 gusts each carry start / duration / power, while Nuclear mode collapses to a
    // single scripted event (forward blast -> still gap -> reverse wind). BuildSettings() does the
    // control->settings mapping in one place (so a new field can't be silently dropped on the way out).
    public class WindDialog : Form
    {
        private const int GustCount = 3;

        private readonly bool _isGif;

        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseOutput = null!;
        private Label lblInput = null!;
        private Label lblOutput = null!;

        private Label lblDirection = null!;
        private ComboBox cmbDirection = null!;
        private Label lblMode = null!;
        private ComboBox cmbMode = null!;

        private Label lblDuration = null!;
        private NumericUpDown numDuration = null!;
        private Label lblFps = null!;
        private NumericUpDown numFps = null!;
        private Label lblStart = null!;
        private NumericUpDown numStart = null!;
        private ComboBox cmbGifPlayMode = null!;

        private Label lblWavelength = null!;
        private NumericUpDown numWavelength = null!;
        private Label lblWaveSpeed = null!;
        private NumericUpDown numWaveSpeed = null!;
        private Label lblStrength = null!;
        private NumericUpDown numStrength = null!;
        private Label lblBend = null!;
        private NumericUpDown numBend = null!;
        private Label lblFlutter = null!;
        private NumericUpDown numFlutter = null!;

        // Normal mode (gusts).
        private Label lblGustsTitle = null!;
        private Label lblHdrStart = null!;
        private Label lblHdrDuration = null!;
        private Label lblHdrPower = null!;
        private readonly CheckBox[] chkGusts = new CheckBox[GustCount];
        private readonly NumericUpDown[] numGustStart = new NumericUpDown[GustCount];
        private readonly NumericUpDown[] numGustDuration = new NumericUpDown[GustCount];
        private readonly NumericUpDown[] numGustPower = new NumericUpDown[GustCount];

        // Nuclear mode (single event).
        private Label lblNukeTitle = null!;
        private Label lblNukeBlastStrength = null!;
        private NumericUpDown numNukeBlastStrength = null!;
        private Label lblNukeBlastDuration = null!;
        private NumericUpDown numNukeBlastDuration = null!;
        private Label lblNukeGap = null!;
        private NumericUpDown numNukeGap = null!;
        private Label lblNukeReverseStrength = null!;
        private NumericUpDown numNukeReverseStrength = null!;
        private Label lblNukeReverseDuration = null!;
        private NumericUpDown numNukeReverseDuration = null!;

        private readonly List<Control> _normalControls = new List<Control>();
        private readonly List<Control> _nuclearControls = new List<Control>();

        private Button btnOK = null!;
        private Button btnCancel = null!;
        private CheckBox chkKeepSize = null!;

        public WindDialog(bool gifMode)
        {
            _isGif = gifMode;
            InitializeComponent();
            cmbGifPlayMode.Visible = gifMode;
            // Editing any of a gust's fields auto-enables it (so "I just set the power" actually takes
            // effect). Wired AFTER InitializeComponent so setting the initial values doesn't trigger it.
            for (int i = 0; i < GustCount; i++)
            {
                int idx = i;
                EventHandler enable = (s, e) => chkGusts[idx].Checked = true;
                numGustStart[i].ValueChanged += enable;
                numGustDuration[i].ValueChanged += enable;
                numGustPower[i].ValueChanged += enable;
            }
            cmbMode.SelectedIndexChanged += (s, e) => RefreshModeVisibility();
            cmbGifPlayMode.SelectedIndexChanged += (s, e) => RefreshStartEnabled();
            UpdateUIText();
            RefreshModeVisibility();
            RefreshStartEnabled();
            ApplyTheme();
        }

        // Single place that turns the controls into a settings object (avoids the "forgot to copy a
        // field" bug class). Normal mode includes only the ticked gusts; Nuclear ignores the gust rows.
        public WindSettings BuildSettings(bool isGif)
        {
            var s = new WindSettings
            {
                InputFilePath = txtInputPath.Text,
                OutputFilePath = txtOutputPath.Text,
                IsGif = isGif,
                Fps = (int)numFps.Value,
                DurationSeconds = (double)numDuration.Value,
                EffectStartSeconds = (double)numStart.Value,
                PlayGifDuringWind = cmbGifPlayMode.SelectedIndex == 0,
                Direction = (WindFromDirection)cmbDirection.SelectedIndex,
                Wavelength = (double)numWavelength.Value,
                WaveSpeed = (double)numWaveSpeed.Value,
                Strength = (double)numStrength.Value,
                BendRatio = (double)numBend.Value,
                FlutterRatio = (double)numFlutter.Value,
                KeepOriginalSize = chkKeepSize.Checked,
                Mode = cmbMode.SelectedIndex == 1 ? WindMode.Nuclear : WindMode.Normal,
                NukeBlastStrength = (double)numNukeBlastStrength.Value,
                NukeBlastDuration = (double)numNukeBlastDuration.Value,
                NukeGap = (double)numNukeGap.Value,
                NukeReverseStrength = (double)numNukeReverseStrength.Value,
                NukeReverseDuration = (double)numNukeReverseDuration.Value,
            };
            if (s.Mode == WindMode.Normal)
            {
                for (int i = 0; i < GustCount; i++)
                {
                    if (!chkGusts[i].Checked) continue;
                    s.Gusts.Add(new WindGust(
                        (double)numGustStart[i].Value,
                        (double)numGustDuration[i].Value,
                        (double)numGustPower[i].Value));
                }
            }
            return s;
        }

        private void RefreshModeVisibility()
        {
            bool nuclear = cmbMode.SelectedIndex == 1;
            foreach (var c in _normalControls) c.Visible = !nuclear;
            foreach (var c in _nuclearControls) c.Visible = nuclear;
        }

        private void RefreshStartEnabled()
        {
            // The effect start window only applies to GIF "wind over playback".
            numStart.Enabled = _isGif && cmbGifPlayMode.SelectedIndex == 0;
        }

        private void UpdateUIText()
        {
            Text = Resources.WindDialog_Title;
            lblInput.Text = Resources.QuickDialog_InputLabel;
            lblOutput.Text = Resources.QuickDialog_OutputLabel;
            lblDirection.Text = Resources.WindDialog_Direction;
            lblMode.Text = Resources.WindDialog_Mode;
            lblDuration.Text = Resources.QuickDialog_Duration;
            lblFps.Text = Resources.SlotDialog_Fps;
            lblStart.Text = Resources.Dialog_StartSeconds;
            lblWavelength.Text = Resources.WindDialog_Wavelength;
            lblWaveSpeed.Text = Resources.WindDialog_WaveSpeed;
            lblStrength.Text = Resources.WindDialog_Strength;
            lblBend.Text = Resources.WindDialog_BendRatio;
            lblFlutter.Text = Resources.WindDialog_FlutterRatio;
            lblGustsTitle.Text = Resources.WindDialog_GustsTitle;
            lblHdrStart.Text = Resources.WindDialog_ColStart;
            lblHdrDuration.Text = Resources.WindDialog_ColDuration;
            lblHdrPower.Text = Resources.WindDialog_ColPower;
            lblNukeTitle.Text = Resources.WindDialog_NukeTitle;
            lblNukeBlastStrength.Text = Resources.WindDialog_NukeBlastStrength;
            lblNukeBlastDuration.Text = Resources.WindDialog_NukeBlastDuration;
            lblNukeGap.Text = Resources.WindDialog_NukeGap;
            lblNukeReverseStrength.Text = Resources.WindDialog_NukeReverseStrength;
            lblNukeReverseDuration.Text = Resources.WindDialog_NukeReverseDuration;
            btnBrowseInput.Text = Resources.Button_Browse;
            btnBrowseOutput.Text = Resources.Button_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;
            chkKeepSize.Text = Resources.Dialog_KeepOriginalSize;
            for (int i = 0; i < GustCount; i++)
            {
                chkGusts[i].Text = string.Format(Resources.WindDialog_GustN, i + 1);
            }

            int dirSel = cmbDirection.SelectedIndex < 0 ? 0 : cmbDirection.SelectedIndex;
            cmbDirection.Items.Clear();
            cmbDirection.Items.Add(Resources.WindDir_FromLeft);
            cmbDirection.Items.Add(Resources.WindDir_FromRight);
            cmbDirection.Items.Add(Resources.WindDir_FromTop);
            cmbDirection.Items.Add(Resources.WindDir_FromBottom);
            cmbDirection.Items.Add(Resources.WindDir_FromTopLeft);
            cmbDirection.Items.Add(Resources.WindDir_FromTopRight);
            cmbDirection.Items.Add(Resources.WindDir_FromBottomLeft);
            cmbDirection.Items.Add(Resources.WindDir_FromBottomRight);
            cmbDirection.SelectedIndex = dirSel;

            int modeSel = cmbMode.SelectedIndex < 0 ? 0 : cmbMode.SelectedIndex;
            cmbMode.Items.Clear();
            cmbMode.Items.Add(Resources.WindDialog_ModeNormal);
            cmbMode.Items.Add(Resources.WindDialog_ModeNuclear);
            cmbMode.SelectedIndex = modeSel;

            int playSel = cmbGifPlayMode.SelectedIndex < 0 ? 0 : cmbGifPlayMode.SelectedIndex;
            cmbGifPlayMode.Items.Clear();
            cmbGifPlayMode.Items.Add(Resources.WindDialog_GifPlayDuring);
            cmbGifPlayMode.Items.Add(Resources.WindDialog_GifFreeze);
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
                    string name = Path.GetFileNameWithoutExtension(ofd.FileName) + "_wind.gif";
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
                    ? "output_wind.gif"
                    : Path.GetFileNameWithoutExtension(txtInputPath.Text) + "_wind.gif"
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

            // Direction + mode row
            lblDirection = new Label { Location = new Point(14, 114), Size = new Size(60, 20), Text = "From" };
            cmbDirection = new ComboBox { Location = new Point(76, 112), Size = new Size(184, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            lblMode = new Label { Location = new Point(270, 114), Size = new Size(48, 20), Text = "Mode" };
            cmbMode = new ComboBox { Location = new Point(320, 112), Size = new Size(150, 23), DropDownStyle = ComboBoxStyle.DropDownList };

            // Scene row
            lblDuration = new Label { Location = new Point(14, 147), Size = new Size(100, 20), Text = "Duration" };
            numDuration = MakeNum(116, 145, 56, 2, 0.10m, 60.00m, 0.25m, 6.00m);
            lblFps = new Label { Location = new Point(178, 147), Size = new Size(30, 20), Text = "FPS" };
            numFps = MakeNum(210, 145, 46, 0, 5m, 60m, 1m, 20m);
            lblStart = new Label { Location = new Point(262, 147), Size = new Size(84, 20), Text = "Start" };
            numStart = MakeNum(348, 145, 56, 2, 0.00m, 60.00m, 0.25m, 0.00m);

            // Medium row 1
            lblWavelength = new Label { Location = new Point(14, 180), Size = new Size(64, 20), Text = "Wavelen" };
            numWavelength = MakeNum(80, 178, 56, 0, 4m, 400m, 1m, 120m);
            lblWaveSpeed = new Label { Location = new Point(148, 180), Size = new Size(60, 20), Text = "Speed" };
            numWaveSpeed = MakeNum(208, 178, 60, 0, 10m, 3000m, 10m, 300m);
            lblStrength = new Label { Location = new Point(282, 180), Size = new Size(60, 20), Text = "Sway" };
            numStrength = MakeNum(342, 178, 56, 1, 0.0m, 60.0m, 0.5m, 10.0m);

            // Medium row 2 (+ GIF playback-mode picker on the right; visible only for GIF input)
            lblBend = new Label { Location = new Point(14, 213), Size = new Size(64, 20), Text = "Bend" };
            numBend = MakeNum(80, 211, 56, 2, 0.00m, 3.00m, 0.10m, 0.40m);
            lblFlutter = new Label { Location = new Point(148, 213), Size = new Size(60, 20), Text = "Flutter" };
            numFlutter = MakeNum(208, 211, 56, 2, 0.00m, 2.00m, 0.05m, 0.20m);
            cmbGifPlayMode = new ComboBox { Location = new Point(300, 211), Size = new Size(180, 23), DropDownStyle = ComboBoxStyle.DropDownList };

            // --- Normal mode: gusts ---
            lblGustsTitle = new Label { Location = new Point(14, 248), Size = new Size(100, 20), Text = "Gusts" };
            lblHdrStart = new Label { Location = new Point(118, 248), Size = new Size(56, 20), Text = "Start" };
            lblHdrDuration = new Label { Location = new Point(180, 248), Size = new Size(56, 20), Text = "Length" };
            lblHdrPower = new Label { Location = new Point(244, 248), Size = new Size(56, 20), Text = "Power" };

            decimal[] defStart = { 0.00m, 1.50m, 3.50m };
            decimal[] defDuration = { 2.50m, 3.00m, 2.50m };
            decimal[] defPower = { 1.00m, 0.80m, 1.00m };
            for (int i = 0; i < GustCount; i++)
            {
                int y = 270 + i * 29;
                chkGusts[i] = new CheckBox { Location = new Point(14, y + 2), Size = new Size(100, 20), Text = "Gust " + (i + 1), Checked = i == 0 };
                numGustStart[i] = MakeNum(118, y, 56, 2, 0.00m, 60.00m, 0.25m, defStart[i]);
                numGustDuration[i] = MakeNum(180, y, 56, 2, 0.10m, 60.00m, 0.25m, defDuration[i]);
                numGustPower[i] = MakeNum(244, y, 56, 2, 0.00m, 5.00m, 0.10m, defPower[i]);
            }

            // --- Nuclear mode: single event (occupies the same vertical band as the gust rows) ---
            lblNukeTitle = new Label { Location = new Point(14, 248), Size = new Size(360, 20), Text = "Nuclear" };
            lblNukeBlastStrength = new Label { Location = new Point(14, 272), Size = new Size(92, 20), Text = "Blast power" };
            numNukeBlastStrength = MakeNum(110, 270, 56, 2, 0.00m, 5.00m, 0.10m, 1.20m);
            lblNukeBlastDuration = new Label { Location = new Point(178, 272), Size = new Size(84, 20), Text = "Blast len" };
            numNukeBlastDuration = MakeNum(266, 270, 56, 2, 0.05m, 10.00m, 0.05m, 0.40m);
            lblNukeGap = new Label { Location = new Point(14, 301), Size = new Size(92, 20), Text = "Still gap" };
            numNukeGap = MakeNum(110, 299, 56, 2, 0.00m, 10.00m, 0.10m, 0.60m);
            lblNukeReverseStrength = new Label { Location = new Point(14, 330), Size = new Size(92, 20), Text = "Back power" };
            numNukeReverseStrength = MakeNum(110, 328, 56, 2, 0.00m, 8.00m, 0.10m, 2.00m);
            lblNukeReverseDuration = new Label { Location = new Point(178, 330), Size = new Size(84, 20), Text = "Back len" };
            numNukeReverseDuration = MakeNum(266, 328, 56, 2, 0.05m, 30.00m, 0.25m, 4.00m);

            btnOK = new Button { Location = new Point(363, 372), Size = new Size(75, 25), Text = "OK", UseVisualStyleBackColor = true };
            btnOK.Click += BtnOK_Click;
            btnCancel = new Button { Location = new Point(444, 372), Size = new Size(82, 25), Text = "Cancel", DialogResult = DialogResult.Cancel, UseVisualStyleBackColor = true };
            chkKeepSize = new CheckBox { Location = new Point(14, 376), Size = new Size(340, 22), Text = "Keep original size", UseVisualStyleBackColor = true };

            _normalControls.AddRange(new Control[] { lblGustsTitle, lblHdrStart, lblHdrDuration, lblHdrPower });
            for (int i = 0; i < GustCount; i++)
            {
                _normalControls.Add(chkGusts[i]);
                _normalControls.Add(numGustStart[i]);
                _normalControls.Add(numGustDuration[i]);
                _normalControls.Add(numGustPower[i]);
            }
            _nuclearControls.AddRange(new Control[]
            {
                lblNukeTitle,
                lblNukeBlastStrength, numNukeBlastStrength, lblNukeBlastDuration, numNukeBlastDuration,
                lblNukeGap, numNukeGap,
                lblNukeReverseStrength, numNukeReverseStrength, lblNukeReverseDuration, numNukeReverseDuration,
            });

            SuspendLayout();
            Controls.Add(lblInput);
            Controls.Add(txtInputPath);
            Controls.Add(btnBrowseInput);
            Controls.Add(lblOutput);
            Controls.Add(txtOutputPath);
            Controls.Add(btnBrowseOutput);
            Controls.Add(lblDirection);
            Controls.Add(cmbDirection);
            Controls.Add(lblMode);
            Controls.Add(cmbMode);
            Controls.Add(lblDuration);
            Controls.Add(numDuration);
            Controls.Add(lblFps);
            Controls.Add(numFps);
            Controls.Add(lblStart);
            Controls.Add(numStart);
            Controls.Add(cmbGifPlayMode);
            Controls.Add(lblWavelength);
            Controls.Add(numWavelength);
            Controls.Add(lblWaveSpeed);
            Controls.Add(numWaveSpeed);
            Controls.Add(lblStrength);
            Controls.Add(numStrength);
            Controls.Add(lblBend);
            Controls.Add(numBend);
            Controls.Add(lblFlutter);
            Controls.Add(numFlutter);
            foreach (var c in _normalControls) Controls.Add(c);
            foreach (var c in _nuclearControls) Controls.Add(c);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            Controls.Add(chkKeepSize);

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            AcceptButton = btnOK;
            ClientSize = new Size(540, 412);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "WindDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Wind Sway";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
