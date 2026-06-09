#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    // Water-fill (灌水 + 氣泡) parameters dialog. Mirrors the other effect dialogs' inline layout + theme
    // handling. The canvas fills with water from a chosen direction between start and full seconds; below
    // the wavy surface the image is refracted and seen through rising bubbles. BuildSettings() does the
    // control->settings mapping in one place.
    public class WaterDialog : Form
    {
        private readonly bool _isGif;

        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseOutput = null!;
        private Label lblInput = null!;
        private Label lblOutput = null!;

        private Label lblDirection = null!;
        private ComboBox cmbDirection = null!;
        private ComboBox cmbGifPlayMode = null!;

        private Label lblStart = null!;
        private NumericUpDown numStart = null!;
        private Label lblFull = null!;
        private NumericUpDown numFull = null!;
        private Label lblFps = null!;
        private NumericUpDown numFps = null!;

        private Label lblDuration = null!;
        private NumericUpDown numDuration = null!;
        private Label lblRefraction = null!;
        private NumericUpDown numRefraction = null!;
        private Label lblWobble = null!;
        private NumericUpDown numWobble = null!;

        private Label lblFadeOut = null!;
        private NumericUpDown numFadeOut = null!;
        private Label lblBubbleSize = null!;
        private NumericUpDown numBubbleSize = null!;
        private Label lblLayers = null!;
        private NumericUpDown numLayers = null!;

        private CheckBox chkKeepSize = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;

        public WaterDialog(bool gifMode)
        {
            _isGif = gifMode;
            InitializeComponent();
            cmbGifPlayMode.Visible = gifMode;
            cmbGifPlayMode.SelectedIndexChanged += (s, e) => RefreshModeEnabled();
            UpdateUIText();
            RefreshModeEnabled();
            ApplyTheme();
        }

        public WaterSettings BuildSettings(bool isGif)
        {
            return new WaterSettings
            {
                InputFilePath = txtInputPath.Text,
                OutputFilePath = txtOutputPath.Text,
                IsGif = isGif,
                Fps = (int)numFps.Value,
                DurationSeconds = (double)numDuration.Value,
                PlayGifDuringWater = cmbGifPlayMode.SelectedIndex == 0,
                KeepOriginalSize = chkKeepSize.Checked,
                Direction = (WaterDirection)cmbDirection.SelectedIndex,
                StartSeconds = (double)numStart.Value,
                FullSeconds = (double)numFull.Value,
                FadeOutSeconds = (double)numFadeOut.Value,
                RefractionStrength = (double)numRefraction.Value,
                SurfaceWobble = (double)numWobble.Value,
                BubbleSize = (double)numBubbleSize.Value,
                Layers = (int)numLayers.Value,
            };
        }

        private void RefreshModeEnabled()
        {
            // Duration only drives the frozen / static fill; the play-along mode uses the live GIF length.
            numDuration.Enabled = !(_isGif && cmbGifPlayMode.SelectedIndex == 0);
        }

        private void UpdateUIText()
        {
            Text = Resources.WaterDialog_Title;
            lblInput.Text = Resources.QuickDialog_InputLabel;
            lblOutput.Text = Resources.QuickDialog_OutputLabel;
            lblDirection.Text = Resources.WaterDialog_Direction;
            lblStart.Text = Resources.Dialog_StartSeconds;
            lblFull.Text = Resources.WaterDialog_FullSeconds;
            lblFps.Text = Resources.SlotDialog_Fps;
            lblDuration.Text = Resources.QuickDialog_Duration;
            lblRefraction.Text = Resources.WaterDialog_Refraction;
            lblWobble.Text = Resources.WaterDialog_Wobble;
            lblFadeOut.Text = Resources.WaterDialog_FadeOut;
            lblBubbleSize.Text = Resources.WaterDialog_BubbleSize;
            lblLayers.Text = Resources.WaterDialog_Layers;
            chkKeepSize.Text = Resources.Dialog_KeepOriginalSize;
            btnBrowseInput.Text = Resources.Button_Browse;
            btnBrowseOutput.Text = Resources.Button_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;

            int dirSel = cmbDirection.SelectedIndex < 0 ? 0 : cmbDirection.SelectedIndex;
            cmbDirection.Items.Clear();
            cmbDirection.Items.Add(Resources.WaterDir_Up);
            cmbDirection.Items.Add(Resources.WaterDir_Down);
            cmbDirection.Items.Add(Resources.WaterDir_Left);
            cmbDirection.Items.Add(Resources.WaterDir_Right);
            cmbDirection.SelectedIndex = dirSel;

            int playSel = cmbGifPlayMode.SelectedIndex < 0 ? 0 : cmbGifPlayMode.SelectedIndex;
            cmbGifPlayMode.Items.Clear();
            cmbGifPlayMode.Items.Add(Resources.WaterDialog_GifPlayDuring);
            cmbGifPlayMode.Items.Add(Resources.WaterDialog_GifFreeze);
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
                    string name = Path.GetFileNameWithoutExtension(ofd.FileName) + "_water.gif";
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
                    ? "output_water.gif"
                    : Path.GetFileNameWithoutExtension(txtInputPath.Text) + "_water.gif"
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
            n.Minimum = min;
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

            // Direction + playback-mode row
            lblDirection = new Label { Location = new Point(14, 116), Size = new Size(40, 20), Text = "Dir" };
            cmbDirection = new ComboBox { Location = new Point(56, 114), Size = new Size(100, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbGifPlayMode = new ComboBox { Location = new Point(280, 114), Size = new Size(200, 23), DropDownStyle = ComboBoxStyle.DropDownList };

            // Timing row
            lblStart = new Label { Location = new Point(14, 149), Size = new Size(70, 20), Text = "Start" };
            numStart = MakeNum(116, 147, 56, 2, 0.00m, 120.00m, 0.25m, 0.50m);
            lblFull = new Label { Location = new Point(178, 149), Size = new Size(84, 20), Text = "Full level" };
            numFull = MakeNum(268, 147, 56, 2, 0.10m, 120.00m, 0.25m, 4.00m);
            lblFps = new Label { Location = new Point(338, 149), Size = new Size(34, 20), Text = "FPS" };
            numFps = MakeNum(374, 147, 46, 0, 5m, 60m, 1m, 20m);

            // Look row 1
            lblDuration = new Label { Location = new Point(14, 182), Size = new Size(100, 20), Text = "Duration" };
            numDuration = MakeNum(116, 180, 56, 2, 0.10m, 120.00m, 0.25m, 6.00m);
            lblRefraction = new Label { Location = new Point(178, 182), Size = new Size(46, 20), Text = "Refract" };
            numRefraction = MakeNum(228, 180, 56, 1, 0.0m, 30.0m, 0.5m, 4.0m);
            lblWobble = new Label { Location = new Point(290, 182), Size = new Size(70, 20), Text = "Wobble" };
            numWobble = MakeNum(364, 180, 56, 1, 0.0m, 40.0m, 0.5m, 6.0m);

            // Look row 2 (bubble size + layers + fade-out; bubble count is automatic)
            lblBubbleSize = new Label { Location = new Point(14, 215), Size = new Size(72, 20), Text = "Bubble size" };
            numBubbleSize = MakeNum(88, 213, 56, 1, 2.0m, 60.0m, 1.0m, 12.0m);
            lblLayers = new Label { Location = new Point(154, 215), Size = new Size(46, 20), Text = "Layers" };
            numLayers = MakeNum(202, 213, 46, 0, 1m, 8m, 1m, 3m);
            lblFadeOut = new Label { Location = new Point(260, 215), Size = new Size(88, 20), Text = "Fade out (s)" };
            numFadeOut = MakeNum(350, 213, 56, 2, 0.00m, 30.00m, 0.25m, 0.50m);

            chkKeepSize = new CheckBox { Location = new Point(14, 247), Size = new Size(340, 22), Text = "Keep original size", UseVisualStyleBackColor = true };

            btnOK = new Button { Location = new Point(363, 283), Size = new Size(75, 25), Text = "OK", UseVisualStyleBackColor = true };
            btnOK.Click += BtnOK_Click;
            btnCancel = new Button { Location = new Point(444, 283), Size = new Size(82, 25), Text = "Cancel", DialogResult = DialogResult.Cancel, UseVisualStyleBackColor = true };

            SuspendLayout();
            Controls.Add(lblInput);
            Controls.Add(txtInputPath);
            Controls.Add(btnBrowseInput);
            Controls.Add(lblOutput);
            Controls.Add(txtOutputPath);
            Controls.Add(btnBrowseOutput);
            Controls.Add(lblDirection);
            Controls.Add(cmbDirection);
            Controls.Add(cmbGifPlayMode);
            Controls.Add(lblStart);
            Controls.Add(numStart);
            Controls.Add(lblFull);
            Controls.Add(numFull);
            Controls.Add(lblFps);
            Controls.Add(numFps);
            Controls.Add(lblDuration);
            Controls.Add(numDuration);
            Controls.Add(lblRefraction);
            Controls.Add(numRefraction);
            Controls.Add(lblWobble);
            Controls.Add(numWobble);
            Controls.Add(lblFadeOut);
            Controls.Add(numFadeOut);
            Controls.Add(lblBubbleSize);
            Controls.Add(numBubbleSize);
            Controls.Add(lblLayers);
            Controls.Add(numLayers);
            Controls.Add(chkKeepSize);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            AcceptButton = btnOK;
            ClientSize = new Size(540, 322);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "WaterDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Water Fill";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
