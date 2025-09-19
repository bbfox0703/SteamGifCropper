#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ImageMagick;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    public partial class OverlayGifDialog : Form
    {
        public string BaseGifPath => txtBaseGif.Text;
        public string OverlayGifPath => txtOverlayGif.Text;
        public string OutputGifPath => txtOutputGif.Text;

        // Movement settings
        public ScrollDirection MovementDirection { get; private set; } = ScrollDirection.Right;
        public int StepPixels { get; private set; } = 1;
        public int MoveCount { get; private set; } = 1;
        public bool InfiniteMovement { get; private set; } = false;

        // Static overlay settings (when no movement)
        public int StaticOverlayX => (int)numStaticX.Value;
        public int StaticOverlayY => (int)numStaticY.Value;
        public bool UseStaticOverlay => !chkEnableMovement.Checked;

        public bool ResampleBaseFrames => chkResampleBase.Checked;

        private readonly ComponentResourceManager _resources = new(typeof(OverlayGifDialog));

        private TextBox txtBaseGif = null!;
        private Button btnBrowseBase = null!;
        private TextBox txtOverlayGif = null!;
        private Button btnBrowseOverlay = null!;
        private TextBox txtOutputGif = null!;
        private Button btnBrowseOutput = null!;
        private Label lblBase = null!;
        private Label lblOverlay = null!;
        private Label lblOutput = null!;
        private Label lblBaseInfo = null!;
        private Label lblOverlayInfo = null!;
        private CheckBox chkResampleBase = null!;

        // Static overlay controls
        private GroupBox grpStaticOverlay = null!;
        private Label lblStaticX = null!;
        private Label lblStaticY = null!;
        private NumericUpDown numStaticX = null!;
        private NumericUpDown numStaticY = null!;

        // Movement controls
        private CheckBox chkEnableMovement = null!;
        private GroupBox grpMovement = null!;
        private Label lblDirection = null!;
        private ComboBox cmbDirection = null!;
        private Label lblStepPixels = null!;
        private NumericUpDown numStepPixels = null!;
        private Label lblMoveCount = null!;
        private NumericUpDown numMoveCount = null!;
        private CheckBox chkInfiniteMovement = null!;

        private Button btnOK = null!;
        private Button btnCancel = null!;

        public OverlayGifDialog()
        {
            InitializeComponent();

            if (!DesignMode)
            {
                UpdateUIText();
                SetupEventHandlers();
                WindowsThemeManager.ApplyThemeToControl(this, WindowsThemeManager.IsDarkModeEnabled());
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Ensure UI text is updated when form loads (in case DesignMode blocked it in constructor)
            if (!DesignMode)
            {
                UpdateUIText();
            }
        }

        private void SetupEventHandlers()
        {
            chkEnableMovement.CheckedChanged += ChkEnableMovement_CheckedChanged;
            chkInfiniteMovement.CheckedChanged += ChkInfiniteMovement_CheckedChanged;
            btnOK.Click += BtnOK_Click;

            // Initially disable movement controls
            ChkEnableMovement_CheckedChanged(null, EventArgs.Empty);
            ChkInfiniteMovement_CheckedChanged(null, EventArgs.Empty);
        }

        private void ChkEnableMovement_CheckedChanged(object? sender, EventArgs e)
        {
            grpMovement.Enabled = chkEnableMovement.Checked;
            grpStaticOverlay.Enabled = !chkEnableMovement.Checked;
        }

        private void ChkInfiniteMovement_CheckedChanged(object? sender, EventArgs e)
        {
            numMoveCount.Enabled = !chkInfiniteMovement.Checked;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(txtBaseGif.Text))
            {
                MessageBox.Show("Please select a base GIF file.", Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOverlayGif.Text))
            {
                MessageBox.Show("Please select an overlay GIF file.", Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputGif.Text))
            {
                MessageBox.Show("Please specify an output GIF file.", Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Capture movement settings
            if (chkEnableMovement.Checked)
            {
                MovementDirection = ((DirectionItem)cmbDirection.SelectedItem!).Value;
                StepPixels = (int)numStepPixels.Value;
                MoveCount = (int)numMoveCount.Value;
                InfiniteMovement = chkInfiniteMovement.Checked;
            }

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Refreshes user interface text based on the current culture.
        /// </summary>
        public void UpdateUIText()
        {
            if (DesignMode) return;

            // Debug: Check if resources are loading
            System.Diagnostics.Debug.WriteLine($"UpdateUIText called. Culture: {System.Globalization.CultureInfo.CurrentUICulture.Name}");
            // File selection
            lblBase.Text = _resources.GetString("lblBase.Text") ?? "Base GIF:";
            btnBrowseBase.Text = _resources.GetString("btnBrowseBase.Text") ?? "Browse...";
            lblOverlay.Text = _resources.GetString("lblOverlay.Text") ?? "Overlay GIF:";
            btnBrowseOverlay.Text = _resources.GetString("btnBrowseOverlay.Text") ?? "Browse...";
            lblOutput.Text = _resources.GetString("lblOutput.Text") ?? "Output GIF:";
            btnBrowseOutput.Text = _resources.GetString("btnBrowseOutput.Text") ?? "Browse...";

            // Options
            chkResampleBase.Text = _resources.GetString("chkResampleBase.Text") ?? "Resample base GIF to overlay FPS";

            // Static overlay
            grpStaticOverlay.Text = _resources.GetString("grpStaticOverlay.Text") ?? "Overlay starting position";
            lblStaticX.Text = _resources.GetString("lblStaticX.Text") ?? "X:";
            lblStaticY.Text = _resources.GetString("lblStaticY.Text") ?? "Y:";

            // Movement
            chkEnableMovement.Text = _resources.GetString("chkEnableMovement.Text") ?? "Enable overlay movement";
            grpMovement.Text = _resources.GetString("grpMovement.Text") ?? "Movement Settings";
            lblDirection.Text = _resources.GetString("lblDirection.Text") ?? "Direction:";
            lblStepPixels.Text = _resources.GetString("lblStepPixels.Text") ?? "Pixels per step:";
            lblMoveCount.Text = _resources.GetString("lblMoveCount.Text") ?? "Move count:";
            chkInfiniteMovement.Text = _resources.GetString("chkInfiniteMovement.Text") ?? "Infinite movement (match base GIF duration)";

            // Buttons
            btnOK.Text = _resources.GetString("btnOK.Text") ?? "OK";
            btnCancel.Text = _resources.GetString("btnCancel.Text") ?? "Cancel";
            Text = _resources.GetString("$this.Text") ?? "Overlay GIF with Movement";

            // Populate direction combo box with localized text
            int selectedIndex = cmbDirection.SelectedIndex;
            cmbDirection.Items.Clear();
            cmbDirection.Items.Add(new DirectionItem(ScrollDirection.Right, _resources.GetString("Direction_Right") ?? "Right"));
            cmbDirection.Items.Add(new DirectionItem(ScrollDirection.Left, _resources.GetString("Direction_Left") ?? "Left"));
            cmbDirection.Items.Add(new DirectionItem(ScrollDirection.Down, _resources.GetString("Direction_Down") ?? "Down"));
            cmbDirection.Items.Add(new DirectionItem(ScrollDirection.Up, _resources.GetString("Direction_Up") ?? "Up"));
            cmbDirection.Items.Add(new DirectionItem(ScrollDirection.LeftUp, _resources.GetString("Direction_LeftUp") ?? "Left Up"));
            cmbDirection.Items.Add(new DirectionItem(ScrollDirection.LeftDown, _resources.GetString("Direction_LeftDown") ?? "Left Down"));
            cmbDirection.Items.Add(new DirectionItem(ScrollDirection.RightUp, _resources.GetString("Direction_RightUp") ?? "Right Up"));
            cmbDirection.Items.Add(new DirectionItem(ScrollDirection.RightDown, _resources.GetString("Direction_RightDown") ?? "Right Down"));
            cmbDirection.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0; // Restore or default to Right
        }

        private void BtnBrowseBase_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = "Select base GIF file"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtBaseGif.Text = dialog.FileName;
                try
                {
                    using var collection = new MagickImageCollection(dialog.FileName);
                    int width = (int)collection[0].Width;
                    int height = (int)collection[0].Height;

                    // Update static overlay position limits
                    numStaticX.Maximum = width > 0 ? width - 1 : 0;
                    numStaticY.Maximum = height > 0 ? height - 1 : 0;

                    double avgDelay = collection.Average(img => (double)img.AnimationDelay);
                    double fps = avgDelay > 0 ? 100.0 / avgDelay : 0;
                    lblBaseInfo.Text = string.Format(
                        _resources.GetString("GifInfoFormat") ?? "{0}×{1}, {2} fps, {3} frames",
                        width,
                        height,
                        fps.ToString("0.##"),
                        collection.Count);
                }
                catch
                {
                    // Ignore errors determining dimensions
                }
            }
        }

        private void BtnBrowseOverlay_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = "Select overlay GIF file"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtOverlayGif.Text = dialog.FileName;
                try
                {
                    using var collection = new MagickImageCollection(dialog.FileName);
                    int width = (int)collection[0].Width;
                    int height = (int)collection[0].Height;

                    double avgDelay = collection.Average(img => (double)img.AnimationDelay);
                    double fps = avgDelay > 0 ? 100.0 / avgDelay : 0;
                    lblOverlayInfo.Text = string.Format(
                        _resources.GetString("GifInfoFormat") ?? "{0}×{1}, {2} fps, {3} frames",
                        width,
                        height,
                        fps.ToString("0.##"),
                        collection.Count);
                }
                catch
                {
                    // Ignore errors determining dimensions
                }
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = "Save overlay GIF as...",
                DefaultExt = "gif"
            };

            if (!string.IsNullOrEmpty(txtBaseGif.Text))
            {
                string baseName = Path.GetFileNameWithoutExtension(txtBaseGif.Text);
                dialog.FileName = $"{baseName}_overlay.gif";
            }

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtOutputGif.Text = dialog.FileName;
            }
        }

        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(OverlayGifDialog));

            // Initialize controls
            lblBase = new Label();
            txtBaseGif = new TextBox();
            btnBrowseBase = new Button();
            lblBaseInfo = new Label();

            lblOverlay = new Label();
            txtOverlayGif = new TextBox();
            btnBrowseOverlay = new Button();
            lblOverlayInfo = new Label();

            lblOutput = new Label();
            txtOutputGif = new TextBox();
            btnBrowseOutput = new Button();

            chkResampleBase = new CheckBox();

            // Static overlay group
            grpStaticOverlay = new GroupBox();
            lblStaticX = new Label();
            numStaticX = new NumericUpDown();
            lblStaticY = new Label();
            numStaticY = new NumericUpDown();

            // Movement group
            chkEnableMovement = new CheckBox();
            grpMovement = new GroupBox();
            lblDirection = new Label();
            cmbDirection = new ComboBox();
            lblStepPixels = new Label();
            numStepPixels = new NumericUpDown();
            lblMoveCount = new Label();
            numMoveCount = new NumericUpDown();
            chkInfiniteMovement = new CheckBox();

            btnOK = new Button();
            btnCancel = new Button();

            ((ISupportInitialize)numStaticX).BeginInit();
            ((ISupportInitialize)numStaticY).BeginInit();
            ((ISupportInitialize)numStepPixels).BeginInit();
            ((ISupportInitialize)numMoveCount).BeginInit();
            grpStaticOverlay.SuspendLayout();
            grpMovement.SuspendLayout();
            SuspendLayout();

            // lblBase
            lblBase.AutoSize = true;
            lblBase.Location = new System.Drawing.Point(12, 15);
            lblBase.Name = "lblBase";
            lblBase.Size = new System.Drawing.Size(57, 15);
            lblBase.TabIndex = 0;
            lblBase.Text = "Base GIF:";

            // txtBaseGif
            txtBaseGif.Location = new System.Drawing.Point(96, 12);
            txtBaseGif.Name = "txtBaseGif";
            txtBaseGif.Size = new System.Drawing.Size(260, 23);
            txtBaseGif.TabIndex = 1;

            // btnBrowseBase
            btnBrowseBase.Location = new System.Drawing.Point(362, 12);
            btnBrowseBase.Name = "btnBrowseBase";
            btnBrowseBase.Size = new System.Drawing.Size(75, 26);
            btnBrowseBase.TabIndex = 2;
            btnBrowseBase.Text = "Browse...";
            btnBrowseBase.UseVisualStyleBackColor = true;
            btnBrowseBase.Click += BtnBrowseBase_Click;

            // lblBaseInfo
            lblBaseInfo.AutoSize = true;
            lblBaseInfo.Location = new System.Drawing.Point(96, 37);
            lblBaseInfo.Name = "lblBaseInfo";
            lblBaseInfo.Size = new System.Drawing.Size(0, 15);
            lblBaseInfo.TabIndex = 3;

            // lblOverlay
            lblOverlay.AutoSize = true;
            lblOverlay.Location = new System.Drawing.Point(12, 65);
            lblOverlay.Name = "lblOverlay";
            lblOverlay.Size = new System.Drawing.Size(74, 15);
            lblOverlay.TabIndex = 4;
            lblOverlay.Text = "Overlay GIF:";

            // txtOverlayGif
            txtOverlayGif.Location = new System.Drawing.Point(96, 62);
            txtOverlayGif.Name = "txtOverlayGif";
            txtOverlayGif.Size = new System.Drawing.Size(260, 23);
            txtOverlayGif.TabIndex = 5;

            // btnBrowseOverlay
            btnBrowseOverlay.Location = new System.Drawing.Point(362, 62);
            btnBrowseOverlay.Name = "btnBrowseOverlay";
            btnBrowseOverlay.Size = new System.Drawing.Size(75, 26);
            btnBrowseOverlay.TabIndex = 6;
            btnBrowseOverlay.Text = "Browse...";
            btnBrowseOverlay.UseVisualStyleBackColor = true;
            btnBrowseOverlay.Click += BtnBrowseOverlay_Click;

            // lblOverlayInfo
            lblOverlayInfo.AutoSize = true;
            lblOverlayInfo.Location = new System.Drawing.Point(96, 87);
            lblOverlayInfo.Name = "lblOverlayInfo";
            lblOverlayInfo.Size = new System.Drawing.Size(0, 15);
            lblOverlayInfo.TabIndex = 7;

            // lblOutput
            lblOutput.AutoSize = true;
            lblOutput.Location = new System.Drawing.Point(12, 115);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(67, 15);
            lblOutput.TabIndex = 8;
            lblOutput.Text = "Output GIF:";

            // txtOutputGif
            txtOutputGif.Location = new System.Drawing.Point(96, 112);
            txtOutputGif.Name = "txtOutputGif";
            txtOutputGif.Size = new System.Drawing.Size(260, 23);
            txtOutputGif.TabIndex = 9;

            // btnBrowseOutput
            btnBrowseOutput.Location = new System.Drawing.Point(362, 112);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new System.Drawing.Size(75, 26);
            btnBrowseOutput.TabIndex = 10;
            btnBrowseOutput.Text = "Browse...";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;

            // chkResampleBase
            chkResampleBase.Checked = true;
            chkResampleBase.CheckState = CheckState.Checked;
            chkResampleBase.Location = new System.Drawing.Point(96, 145);
            chkResampleBase.Name = "chkResampleBase";
            chkResampleBase.Size = new System.Drawing.Size(260, 19);
            chkResampleBase.TabIndex = 11;
            chkResampleBase.Text = "Resample base GIF to overlay FPS";
            chkResampleBase.UseVisualStyleBackColor = true;

            // grpStaticOverlay
            grpStaticOverlay.Controls.Add(lblStaticX);
            grpStaticOverlay.Controls.Add(numStaticX);
            grpStaticOverlay.Controls.Add(lblStaticY);
            grpStaticOverlay.Controls.Add(numStaticY);
            grpStaticOverlay.Location = new System.Drawing.Point(12, 175);
            grpStaticOverlay.Name = "grpStaticOverlay";
            grpStaticOverlay.Size = new System.Drawing.Size(425, 55);
            grpStaticOverlay.TabIndex = 12;
            grpStaticOverlay.TabStop = false;
            grpStaticOverlay.Text = "Overlay starting position";

            // lblStaticX
            lblStaticX.AutoSize = true;
            lblStaticX.Location = new System.Drawing.Point(15, 25);
            lblStaticX.Name = "lblStaticX";
            lblStaticX.Size = new System.Drawing.Size(18, 15);
            lblStaticX.TabIndex = 0;
            lblStaticX.Text = "X:";

            // numStaticX
            numStaticX.Location = new System.Drawing.Point(45, 22);
            numStaticX.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numStaticX.Name = "numStaticX";
            numStaticX.Size = new System.Drawing.Size(120, 23);
            numStaticX.TabIndex = 1;

            // lblStaticY
            lblStaticY.AutoSize = true;
            lblStaticY.Location = new System.Drawing.Point(180, 25);
            lblStaticY.Name = "lblStaticY";
            lblStaticY.Size = new System.Drawing.Size(17, 15);
            lblStaticY.TabIndex = 2;
            lblStaticY.Text = "Y:";

            // numStaticY
            numStaticY.Location = new System.Drawing.Point(210, 22);
            numStaticY.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numStaticY.Name = "numStaticY";
            numStaticY.Size = new System.Drawing.Size(120, 23);
            numStaticY.TabIndex = 3;

            // chkEnableMovement
            chkEnableMovement.AutoSize = true;
            chkEnableMovement.Location = new System.Drawing.Point(12, 245);
            chkEnableMovement.Name = "chkEnableMovement";
            chkEnableMovement.Size = new System.Drawing.Size(154, 19);
            chkEnableMovement.TabIndex = 13;
            chkEnableMovement.Text = "Enable overlay movement";
            chkEnableMovement.UseVisualStyleBackColor = true;

            // grpMovement
            grpMovement.Controls.Add(lblDirection);
            grpMovement.Controls.Add(cmbDirection);
            grpMovement.Controls.Add(lblStepPixels);
            grpMovement.Controls.Add(numStepPixels);
            grpMovement.Controls.Add(lblMoveCount);
            grpMovement.Controls.Add(numMoveCount);
            grpMovement.Controls.Add(chkInfiniteMovement);
            grpMovement.Location = new System.Drawing.Point(12, 270);
            grpMovement.Name = "grpMovement";
            grpMovement.Size = new System.Drawing.Size(425, 110);
            grpMovement.TabIndex = 14;
            grpMovement.TabStop = false;
            grpMovement.Text = "Movement Settings";

            // lblDirection
            lblDirection.AutoSize = true;
            lblDirection.Location = new System.Drawing.Point(15, 25);
            lblDirection.Name = "lblDirection";
            lblDirection.Size = new System.Drawing.Size(58, 15);
            lblDirection.TabIndex = 0;
            lblDirection.Text = "Direction:";

            // cmbDirection
            cmbDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDirection.Location = new System.Drawing.Point(85, 22);
            cmbDirection.Name = "cmbDirection";
            cmbDirection.Size = new System.Drawing.Size(120, 23);
            cmbDirection.TabIndex = 1;

            // lblStepPixels
            lblStepPixels.AutoSize = true;
            lblStepPixels.Location = new System.Drawing.Point(220, 25);
            lblStepPixels.Name = "lblStepPixels";
            lblStepPixels.Size = new System.Drawing.Size(87, 15);
            lblStepPixels.TabIndex = 2;
            lblStepPixels.Text = "Pixels per step:";

            // numStepPixels
            numStepPixels.Location = new System.Drawing.Point(315, 22);
            numStepPixels.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStepPixels.Name = "numStepPixels";
            numStepPixels.Size = new System.Drawing.Size(80, 23);
            numStepPixels.TabIndex = 3;
            numStepPixels.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // lblMoveCount
            lblMoveCount.AutoSize = true;
            lblMoveCount.Location = new System.Drawing.Point(15, 55);
            lblMoveCount.Name = "lblMoveCount";
            lblMoveCount.Size = new System.Drawing.Size(73, 15);
            lblMoveCount.TabIndex = 4;
            lblMoveCount.Text = "Move count:";

            // numMoveCount
            numMoveCount.Location = new System.Drawing.Point(95, 52);
            numMoveCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMoveCount.Name = "numMoveCount";
            numMoveCount.Size = new System.Drawing.Size(120, 23);
            numMoveCount.TabIndex = 5;
            numMoveCount.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // chkInfiniteMovement
            chkInfiniteMovement.AutoSize = true;
            chkInfiniteMovement.Location = new System.Drawing.Point(15, 80);
            chkInfiniteMovement.Name = "chkInfiniteMovement";
            chkInfiniteMovement.Size = new System.Drawing.Size(253, 19);
            chkInfiniteMovement.TabIndex = 6;
            chkInfiniteMovement.Text = "Infinite movement (match base GIF duration)";
            chkInfiniteMovement.UseVisualStyleBackColor = true;

            // btnOK
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new System.Drawing.Point(281, 395);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 26);
            btnOK.TabIndex = 15;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;

            // btnCancel
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(362, 395);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 26);
            btnCancel.TabIndex = 16;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;

            // OverlayGifDialog
            AcceptButton = btnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(450, 435);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(grpMovement);
            Controls.Add(chkEnableMovement);
            Controls.Add(grpStaticOverlay);
            Controls.Add(chkResampleBase);
            Controls.Add(btnBrowseOutput);
            Controls.Add(txtOutputGif);
            Controls.Add(lblOutput);
            Controls.Add(lblOverlayInfo);
            Controls.Add(btnBrowseOverlay);
            Controls.Add(txtOverlayGif);
            Controls.Add(lblOverlay);
            Controls.Add(lblBaseInfo);
            Controls.Add(btnBrowseBase);
            Controls.Add(txtBaseGif);
            Controls.Add(lblBase);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon?)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "OverlayGifDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Overlay GIF with Movement";

            ((ISupportInitialize)numStaticX).EndInit();
            ((ISupportInitialize)numStaticY).EndInit();
            ((ISupportInitialize)numStepPixels).EndInit();
            ((ISupportInitialize)numMoveCount).EndInit();
            grpStaticOverlay.ResumeLayout(false);
            grpStaticOverlay.PerformLayout();
            grpMovement.ResumeLayout(false);
            grpMovement.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
    }

    public class DirectionItem
    {
        public ScrollDirection Value { get; }
        public string DisplayText { get; }

        public DirectionItem(ScrollDirection value, string displayText)
        {
            Value = value;
            DisplayText = displayText;
        }

        public override string ToString() => DisplayText;
    }
}