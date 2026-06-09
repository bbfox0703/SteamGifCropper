#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    // A->B "morph" transition dialog: A plays for a pre-roll, morphs into B over the morph window, then
    // B's remaining footage plays to the end. Mirrors the other dialogs' inline layout + theme handling.
    // A style combo (raindrop reveal / tile flip) shows/hides the matching parameter group, like the wind
    // dialog's Normal/Nuclear groups. BuildSettings() does the control->settings mapping in one place.
    public class MorphTransitionDialog : Form
    {
        private TextBox txtInputA = null!;
        private Button btnBrowseA = null!;
        private TextBox txtInputB = null!;
        private Button btnBrowseB = null!;
        private TextBox txtOutput = null!;
        private Button btnBrowseOutput = null!;
        private Label lblInputA = null!;
        private Label lblInputB = null!;
        private Label lblOutput = null!;

        private Label lblStyle = null!;
        private ComboBox cmbStyle = null!;

        private Label lblPreRoll = null!;
        private NumericUpDown numPreRoll = null!;
        private Label lblMorph = null!;
        private NumericUpDown numMorph = null!;
        private Label lblFps = null!;
        private NumericUpDown numFps = null!;
        private CheckBox chkKeepSize = null!;

        // Raindrop-reveal group.
        private Label lblRainIntensity = null!;
        private NumericUpDown numRainIntensity = null!;
        private Label lblDropSizeVar = null!;
        private NumericUpDown numDropSizeVar = null!;
        private Label lblSpreadRadius = null!;
        private NumericUpDown numSpreadRadius = null!;
        private Label lblSoftEdge = null!;
        private NumericUpDown numSoftEdge = null!;

        // Tile-flip group.
        private Label lblDivisions = null!;
        private NumericUpDown numDivisions = null!;
        private Label lblFlipDir = null!;
        private ComboBox cmbFlipDir = null!;

        // Spotlight group.
        private Label lblSpotRadius = null!;
        private NumericUpDown numSpotRadius = null!;
        private Label lblSpotSpeed = null!;
        private NumericUpDown numSpotSpeed = null!;
        private Label lblSpotExpand = null!;
        private NumericUpDown numSpotExpand = null!;

        // Jigsaw group.
        private Label lblJigsawPieces = null!;
        private NumericUpDown numJigsawPieces = null!;
        private CheckBox chkJigsawLines = null!;
        private Label lblJigsawColor = null!;
        private Panel pnlJigsawColor = null!;
        private Color _jigsawLineColor = Color.White;

        private readonly List<Control> _raindropControls = new List<Control>();
        private readonly List<Control> _tileControls = new List<Control>();
        private readonly List<Control> _spotlightControls = new List<Control>();
        private readonly List<Control> _jigsawControls = new List<Control>();

        private Button btnOK = null!;
        private Button btnCancel = null!;

        public MorphTransitionDialog()
        {
            InitializeComponent();
            cmbStyle.SelectedIndexChanged += (s, e) => RefreshStyleVisibility();
            chkJigsawLines.CheckedChanged += (s, e) => RefreshJigsawColorEnabled();
            UpdateUIText();
            RefreshStyleVisibility();
            RefreshJigsawColorEnabled();
            ApplyTheme();
        }

        public MorphSettings BuildSettings()
        {
            int styleIdx = cmbStyle.SelectedIndex < 0 ? 0 : cmbStyle.SelectedIndex;
            var style = (MorphStyle)styleIdx;
            return new MorphSettings
            {
                InputAPath = txtInputA.Text,
                InputBPath = txtInputB.Text,
                OutputPath = txtOutput.Text,
                Style = style,
                PreRollSeconds = (double)numPreRoll.Value,
                MorphSeconds = (double)numMorph.Value,
                Fps = (int)numFps.Value,
                KeepOriginalSize = chkKeepSize.Checked,
                RainIntensity = (double)numRainIntensity.Value,
                DropSizeVariationPct = (double)numDropSizeVar.Value,
                SpreadRadius = (double)numSpreadRadius.Value,
                SoftEdge = (double)numSoftEdge.Value,
                // Divisions is shared by tile flip and jigsaw; read whichever style's piece box is active.
                Divisions = style == MorphStyle.Jigsaw ? (int)numJigsawPieces.Value : (int)numDivisions.Value,
                FlipDirection = (TileFlipDirection)cmbFlipDir.SelectedIndex,
                SpotlightRadius = (double)numSpotRadius.Value,
                SpotlightSpeed = (double)numSpotSpeed.Value,
                SpotlightExpandSeconds = (double)numSpotExpand.Value,
                JigsawShowLines = chkJigsawLines.Checked,
                JigsawLineR = _jigsawLineColor.R,
                JigsawLineG = _jigsawLineColor.G,
                JigsawLineB = _jigsawLineColor.B,
            };
        }

        private void RefreshStyleVisibility()
        {
            var style = (MorphStyle)(cmbStyle.SelectedIndex < 0 ? 0 : cmbStyle.SelectedIndex);
            foreach (var c in _raindropControls) c.Visible = style == MorphStyle.RaindropReveal;
            foreach (var c in _tileControls) c.Visible = style == MorphStyle.TileFlip;
            foreach (var c in _spotlightControls) c.Visible = style == MorphStyle.Spotlight;
            foreach (var c in _jigsawControls) c.Visible = style == MorphStyle.Jigsaw;
        }

        private void RefreshJigsawColorEnabled()
        {
            lblJigsawColor.Enabled = chkJigsawLines.Checked;
            pnlJigsawColor.Enabled = chkJigsawLines.Checked;
        }

        private void PnlJigsawColor_Click(object? sender, EventArgs e)
        {
            if (!chkJigsawLines.Checked) return;
            using var cd = new ColorDialog { Color = _jigsawLineColor, FullOpen = true };
            if (cd.ShowDialog(this) == DialogResult.OK)
            {
                _jigsawLineColor = cd.Color;
                pnlJigsawColor.BackColor = cd.Color;
            }
        }

        private void UpdateUIText()
        {
            Text = Resources.MorphDialog_Title;
            lblInputA.Text = Resources.MorphDialog_InputA;
            lblInputB.Text = Resources.MorphDialog_InputB;
            lblOutput.Text = Resources.QuickDialog_OutputLabel;
            lblStyle.Text = Resources.MorphDialog_Style;
            lblPreRoll.Text = Resources.MorphDialog_PreRoll;
            lblMorph.Text = Resources.MorphDialog_MorphSeconds;
            lblFps.Text = Resources.SlotDialog_Fps;
            chkKeepSize.Text = Resources.Dialog_KeepOriginalSize;
            lblRainIntensity.Text = Resources.MorphDialog_RainIntensity;
            lblDropSizeVar.Text = Resources.MorphDialog_DropSizeVar;
            lblSpreadRadius.Text = Resources.MorphDialog_SpreadRadius;
            lblSoftEdge.Text = Resources.MorphDialog_SoftEdge;
            lblDivisions.Text = Resources.MorphDialog_Divisions;
            lblFlipDir.Text = Resources.MorphDialog_FlipDir;
            lblSpotRadius.Text = Resources.MorphDialog_SpotRadius;
            lblSpotSpeed.Text = Resources.MorphDialog_SpotSpeed;
            lblSpotExpand.Text = Resources.MorphDialog_SpotExpand;
            lblJigsawPieces.Text = Resources.MorphDialog_JigsawPieces;
            chkJigsawLines.Text = Resources.MorphDialog_JigsawShowLines;
            lblJigsawColor.Text = Resources.MorphDialog_JigsawLineColor;
            btnBrowseA.Text = Resources.Button_Browse;
            btnBrowseB.Text = Resources.Button_Browse;
            btnBrowseOutput.Text = Resources.Button_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;

            int styleSel = cmbStyle.SelectedIndex < 0 ? 0 : cmbStyle.SelectedIndex;
            cmbStyle.Items.Clear();
            cmbStyle.Items.Add(Resources.MorphStyle_Raindrop);
            cmbStyle.Items.Add(Resources.MorphStyle_TileFlip);
            cmbStyle.Items.Add(Resources.MorphStyle_Spotlight);
            cmbStyle.Items.Add(Resources.MorphStyle_Jigsaw);
            cmbStyle.SelectedIndex = styleSel;

            int dirSel = cmbFlipDir.SelectedIndex < 0 ? 0 : cmbFlipDir.SelectedIndex;
            cmbFlipDir.Items.Clear();
            cmbFlipDir.Items.Add(Resources.FlipDir_Random);
            cmbFlipDir.Items.Add(Resources.FlipDir_Up);
            cmbFlipDir.Items.Add(Resources.FlipDir_Down);
            cmbFlipDir.Items.Add(Resources.FlipDir_Left);
            cmbFlipDir.Items.Add(Resources.FlipDir_Right);
            cmbFlipDir.SelectedIndex = dirSel;
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

        private void BrowseInput(TextBox target, string suffix)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = Resources.FileDialog_ImageAndGifFilter,
                Title = Resources.QuickDialog_InputLabel
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                target.Text = ofd.FileName;
                if (string.IsNullOrWhiteSpace(txtOutput.Text) && !string.IsNullOrWhiteSpace(txtInputA.Text))
                {
                    string dir = Path.GetDirectoryName(txtInputA.Text) ?? string.Empty;
                    string name = Path.GetFileNameWithoutExtension(txtInputA.Text) + "_morph.gif";
                    txtOutput.Text = Path.Combine(dir, name);
                }
            }
        }

        private void BtnBrowseA_Click(object? sender, EventArgs e) => BrowseInput(txtInputA, "_morph");
        private void BtnBrowseB_Click(object? sender, EventArgs e) => BrowseInput(txtInputB, "_morph");

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = Resources.QuickDialog_OutputLabel,
                FileName = string.IsNullOrEmpty(txtInputA.Text)
                    ? "output_morph.gif"
                    : Path.GetFileNameWithoutExtension(txtInputA.Text) + "_morph.gif"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                txtOutput.Text = sfd.FileName;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInputA.Text) || !File.Exists(txtInputA.Text) ||
                string.IsNullOrWhiteSpace(txtInputB.Text) || !File.Exists(txtInputB.Text))
            {
                MessageBox.Show(this, Resources.ScrollDialog_InputRequired, Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtOutput.Text))
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
            lblInputA = new Label { Location = new Point(14, 9), Size = new Size(360, 20), Text = "Clip A" };
            txtInputA = new TextBox { Location = new Point(14, 29), Size = new Size(418, 23), ReadOnly = true };
            btnBrowseA = new Button { Location = new Point(440, 27), Size = new Size(86, 25), Text = "Browse", UseVisualStyleBackColor = true };
            btnBrowseA.Click += BtnBrowseA_Click;

            lblInputB = new Label { Location = new Point(14, 58), Size = new Size(360, 20), Text = "Clip B" };
            txtInputB = new TextBox { Location = new Point(14, 78), Size = new Size(418, 23), ReadOnly = true };
            btnBrowseB = new Button { Location = new Point(440, 76), Size = new Size(86, 25), Text = "Browse", UseVisualStyleBackColor = true };
            btnBrowseB.Click += BtnBrowseB_Click;

            lblOutput = new Label { Location = new Point(14, 107), Size = new Size(360, 20), Text = "Output" };
            txtOutput = new TextBox { Location = new Point(14, 127), Size = new Size(418, 23), ReadOnly = true };
            btnBrowseOutput = new Button { Location = new Point(440, 125), Size = new Size(86, 25), Text = "Browse", UseVisualStyleBackColor = true };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            // Style + shared timeline
            lblStyle = new Label { Location = new Point(14, 165), Size = new Size(56, 20), Text = "Style" };
            cmbStyle = new ComboBox { Location = new Point(72, 163), Size = new Size(180, 23), DropDownStyle = ComboBoxStyle.DropDownList };

            lblPreRoll = new Label { Location = new Point(14, 198), Size = new Size(116, 20), Text = "Pre-roll A" };
            numPreRoll = MakeNum(132, 196, 56, 2, 0.00m, 120.00m, 0.25m, 2.00m);
            lblMorph = new Label { Location = new Point(196, 198), Size = new Size(76, 20), Text = "Morph" };
            numMorph = MakeNum(274, 196, 56, 2, 0.10m, 120.00m, 0.25m, 3.00m);
            lblFps = new Label { Location = new Point(338, 198), Size = new Size(34, 20), Text = "FPS" };
            numFps = MakeNum(374, 196, 46, 0, 5m, 60m, 1m, 20m);
            // Keep-size goes on the style row (its own space) so the long localized label isn't clipped.
            chkKeepSize = new CheckBox { Location = new Point(270, 166), Size = new Size(252, 22), Text = "Keep original size", UseVisualStyleBackColor = true };

            // --- Raindrop-reveal group ---
            lblRainIntensity = new Label { Location = new Point(14, 234), Size = new Size(74, 20), Text = "Drops" };
            numRainIntensity = MakeNum(96, 232, 56, 0, 1m, 400m, 1m, 30m);
            lblDropSizeVar = new Label { Location = new Point(162, 234), Size = new Size(88, 20), Text = "Size var %" };
            numDropSizeVar = MakeNum(256, 232, 56, 0, 0m, 100m, 1m, 40m);
            lblSpreadRadius = new Label { Location = new Point(14, 267), Size = new Size(74, 20), Text = "Spread r" };
            numSpreadRadius = MakeNum(96, 265, 56, 0, 8m, 600m, 2m, 90m);
            lblSoftEdge = new Label { Location = new Point(162, 267), Size = new Size(88, 20), Text = "Soft edge" };
            numSoftEdge = MakeNum(256, 265, 56, 0, 1m, 80m, 1m, 8m);

            // --- Tile-flip group (same vertical band) ---
            lblDivisions = new Label { Location = new Point(14, 234), Size = new Size(74, 20), Text = "Divisions" };
            numDivisions = MakeNum(96, 232, 56, 0, 2m, 40m, 1m, 8m);
            lblFlipDir = new Label { Location = new Point(162, 234), Size = new Size(60, 20), Text = "Flip dir" };
            cmbFlipDir = new ComboBox { Location = new Point(228, 232), Size = new Size(140, 23), DropDownStyle = ComboBoxStyle.DropDownList };

            // --- Spotlight group (same vertical band) ---
            lblSpotRadius = new Label { Location = new Point(14, 234), Size = new Size(82, 20), Text = "Light size" };
            numSpotRadius = MakeNum(98, 232, 60, 0, 20m, 600m, 5m, 120m);
            lblSpotSpeed = new Label { Location = new Point(170, 234), Size = new Size(60, 20), Text = "Speed" };
            numSpotSpeed = MakeNum(236, 232, 64, 0, 50m, 2000m, 10m, 400m);
            lblSpotExpand = new Label { Location = new Point(14, 267), Size = new Size(82, 20), Text = "Expand (s)" };
            numSpotExpand = MakeNum(98, 265, 56, 2, 0.10m, 60.00m, 0.25m, 1.00m);

            // --- Jigsaw group (same vertical band) ---
            lblJigsawPieces = new Label { Location = new Point(14, 234), Size = new Size(82, 20), Text = "Pieces (X)" };
            numJigsawPieces = MakeNum(98, 232, 56, 0, 2m, 40m, 1m, 8m);
            chkJigsawLines = new CheckBox { Location = new Point(14, 266), Size = new Size(150, 22), Text = "Show piece edges", Checked = true, UseVisualStyleBackColor = true };
            lblJigsawColor = new Label { Location = new Point(176, 268), Size = new Size(90, 20), Text = "Edge colour" };
            pnlJigsawColor = new Panel { Location = new Point(272, 265), Size = new Size(44, 22), BorderStyle = BorderStyle.FixedSingle, BackColor = _jigsawLineColor, Cursor = Cursors.Hand };
            pnlJigsawColor.Click += PnlJigsawColor_Click;

            btnOK = new Button { Location = new Point(363, 312), Size = new Size(75, 25), Text = "OK", UseVisualStyleBackColor = true };
            btnOK.Click += BtnOK_Click;
            btnCancel = new Button { Location = new Point(444, 312), Size = new Size(82, 25), Text = "Cancel", DialogResult = DialogResult.Cancel, UseVisualStyleBackColor = true };

            _raindropControls.AddRange(new Control[]
            {
                lblRainIntensity, numRainIntensity, lblDropSizeVar, numDropSizeVar,
                lblSpreadRadius, numSpreadRadius, lblSoftEdge, numSoftEdge,
            });
            _tileControls.AddRange(new Control[] { lblDivisions, numDivisions, lblFlipDir, cmbFlipDir });
            _spotlightControls.AddRange(new Control[]
            {
                lblSpotRadius, numSpotRadius, lblSpotSpeed, numSpotSpeed, lblSpotExpand, numSpotExpand,
            });
            _jigsawControls.AddRange(new Control[]
            {
                lblJigsawPieces, numJigsawPieces, chkJigsawLines, lblJigsawColor, pnlJigsawColor,
            });

            SuspendLayout();
            Controls.Add(lblInputA);
            Controls.Add(txtInputA);
            Controls.Add(btnBrowseA);
            Controls.Add(lblInputB);
            Controls.Add(txtInputB);
            Controls.Add(btnBrowseB);
            Controls.Add(lblOutput);
            Controls.Add(txtOutput);
            Controls.Add(btnBrowseOutput);
            Controls.Add(lblStyle);
            Controls.Add(cmbStyle);
            Controls.Add(lblPreRoll);
            Controls.Add(numPreRoll);
            Controls.Add(lblMorph);
            Controls.Add(numMorph);
            Controls.Add(lblFps);
            Controls.Add(numFps);
            Controls.Add(chkKeepSize);
            foreach (var c in _raindropControls) Controls.Add(c);
            foreach (var c in _tileControls) Controls.Add(c);
            foreach (var c in _spotlightControls) Controls.Add(c);
            foreach (var c in _jigsawControls) Controls.Add(c);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            AcceptButton = btnOK;
            ClientSize = new Size(540, 352);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MorphTransitionDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Morph A to B";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
