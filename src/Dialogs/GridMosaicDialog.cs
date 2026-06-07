#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    public class GridMosaicDialog : Form
    {
        public string InputFilePath { get; private set; } = string.Empty;
        public int ColumnsPerSlot { get; private set; } = 4;
        public int Rows { get; private set; } = 5;
        public int LineWidth { get; private set; } = 4;
        public GridLineStyle Style { get; private set; } = GridLineStyle.Transparent;
        public Color LineColor { get; private set; } = Color.Black;

        // The narrowest Steam slot is 150px wide for both 766 and 774 layouts; validating
        // against it guarantees the grid lines fit every part.
        private const int MinSlotWidth = 150;

        private uint _canvasWidth;
        private int _canvasHeight;

        private Label lblInput = null!;
        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private Label lblDetected = null!;
        private Label lblColumns = null!;
        private NumericUpDown numColumns = null!;
        private Label lblRows = null!;
        private NumericUpDown numRows = null!;
        private Label lblLineWidth = null!;
        private NumericUpDown numLineWidth = null!;
        private Label lblStyle = null!;
        private RadioButton rdoTransparent = null!;
        private RadioButton rdoSolid = null!;
        private Button btnPickColor = null!;
        private Panel pnlColorPreview = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;

        public GridMosaicDialog()
        {
            InitializeComponent();
            btnBrowseInput.Click += BtnBrowseInput_Click;
            rdoTransparent.CheckedChanged += OnLineStyleChanged;
            rdoSolid.CheckedChanged += OnLineStyleChanged;
            btnPickColor.Click += BtnPickColor_Click;
            btnOK.Click += BtnOK_Click;
            pnlColorPreview.BackColor = LineColor;
            OnLineStyleChanged(null, EventArgs.Empty);
            UpdateUIText();
            ApplyTheme();
        }

        private void UpdateUIText()
        {
            Text = Resources.GridDialog_Title;
            lblInput.Text = Resources.GridDialog_InputLabel;
            btnBrowseInput.Text = Resources.GridDialog_Browse;
            lblColumns.Text = Resources.GridDialog_ColumnsPerSlot;
            lblRows.Text = Resources.GridDialog_Rows;
            lblLineWidth.Text = Resources.GridDialog_LineWidth;
            lblStyle.Text = Resources.GridDialog_Style;
            rdoTransparent.Text = Resources.GridDialog_StyleTransparent;
            rdoSolid.Text = Resources.GridDialog_StyleSolid;
            btnPickColor.Text = Resources.GridDialog_PickColor;
            btnOK.Text = Resources.GridDialog_OK;
            btnCancel.Text = Resources.GridDialog_Cancel;
        }

        private void BtnBrowseInput_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = Resources.FileDialog_SelectGif
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInputPath.Text = ofd.FileName;
                DetectDimensions(ofd.FileName);
            }
        }

        private void DetectDimensions(string path)
        {
            _canvasWidth = 0;
            _canvasHeight = 0;
            try
            {
                using var img = Image.FromFile(path);
                _canvasWidth = (uint)img.Width;
                _canvasHeight = img.Height;
            }
            catch
            {
                // Leave dimensions unknown; GridMosaic() validates again before processing.
            }

            if (_canvasWidth == 766 || _canvasWidth == 774)
            {
                numLineWidth.Value = _canvasWidth == 766 ? 4 : 5;
                lblDetected.Text = string.Format(Resources.GridDialog_Detected, _canvasWidth, _canvasHeight);
            }
            else if (_canvasWidth > 0)
            {
                lblDetected.Text = string.Format(Resources.GridDialog_DetectedInvalid, _canvasWidth);
            }
            else
            {
                lblDetected.Text = string.Empty;
            }
        }

        private void OnLineStyleChanged(object? sender, EventArgs e)
        {
            bool solid = rdoSolid.Checked;
            btnPickColor.Enabled = solid;
            pnlColorPreview.Visible = solid;
        }

        private void BtnPickColor_Click(object? sender, EventArgs e)
        {
            using var dlg = new ColorDialog { Color = LineColor, FullOpen = true };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                LineColor = dlg.Color;
                pnlColorPreview.BackColor = LineColor;
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInputPath.Text) || !File.Exists(txtInputPath.Text))
            {
                MessageBox.Show(this, Resources.GridDialog_InputRequired, Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_canvasWidth != 0 && _canvasWidth != 766 && _canvasWidth != 774)
            {
                MessageBox.Show(this, string.Format(Resources.GridDialog_DetectedInvalid, _canvasWidth), Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int columns = (int)numColumns.Value;
            int rows = (int)numRows.Value;
            int lineWidth = (int)numLineWidth.Value;

            if ((columns - 1) * lineWidth >= MinSlotWidth ||
                (_canvasHeight > 0 && (rows - 1) * lineWidth >= _canvasHeight))
            {
                MessageBox.Show(this, Resources.GridDialog_LineTooWide, Resources.Title_Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            InputFilePath = txtInputPath.Text;
            ColumnsPerSlot = columns;
            Rows = rows;
            LineWidth = lineWidth;
            Style = rdoSolid.Checked ? GridLineStyle.Solid : GridLineStyle.Transparent;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ApplyTheme()
        {
            bool isDark = WindowsThemeManager.IsDarkModeEnabled();
            if (isDark)
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
                    label.BackColor = Color.Transparent;
                    label.ForeColor = Color.White;
                }
                else if (control is RadioButton radio)
                {
                    radio.BackColor = Color.Transparent;
                    radio.ForeColor = Color.White;
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
                    label.BackColor = Color.Transparent;
                    label.ForeColor = SystemColors.ControlText;
                }
                else if (control is RadioButton radio)
                {
                    radio.BackColor = Color.Transparent;
                    radio.ForeColor = SystemColors.ControlText;
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

                if (control.HasChildren)
                {
                    ApplyLightThemeToControls(control.Controls);
                }
            }
        }

        private void InitializeComponent()
        {
            lblInput = new Label();
            txtInputPath = new TextBox();
            btnBrowseInput = new Button();
            lblDetected = new Label();
            lblColumns = new Label();
            numColumns = new NumericUpDown();
            lblRows = new Label();
            numRows = new NumericUpDown();
            lblLineWidth = new Label();
            numLineWidth = new NumericUpDown();
            lblStyle = new Label();
            rdoTransparent = new RadioButton();
            rdoSolid = new RadioButton();
            btnPickColor = new Button();
            pnlColorPreview = new Panel();
            btnOK = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numColumns).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRows).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numLineWidth).BeginInit();
            SuspendLayout();
            //
            // lblInput
            //
            lblInput.Location = new System.Drawing.Point(14, 9);
            lblInput.Name = "lblInput";
            lblInput.Size = new System.Drawing.Size(200, 20);
            lblInput.TabIndex = 0;
            lblInput.Text = "Input GIF (766/774)";
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
            //
            // lblDetected
            //
            lblDetected.Location = new System.Drawing.Point(14, 56);
            lblDetected.Name = "lblDetected";
            lblDetected.Size = new System.Drawing.Size(427, 20);
            lblDetected.TabIndex = 3;
            //
            // lblColumns
            //
            lblColumns.Location = new System.Drawing.Point(14, 88);
            lblColumns.Name = "lblColumns";
            lblColumns.Size = new System.Drawing.Size(160, 20);
            lblColumns.TabIndex = 4;
            lblColumns.Text = "Columns per slot";
            //
            // numColumns
            //
            numColumns.Location = new System.Drawing.Point(180, 86);
            numColumns.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numColumns.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numColumns.Name = "numColumns";
            numColumns.Size = new System.Drawing.Size(60, 23);
            numColumns.TabIndex = 5;
            numColumns.Value = new decimal(new int[] { 4, 0, 0, 0 });
            //
            // lblRows
            //
            lblRows.Location = new System.Drawing.Point(14, 118);
            lblRows.Name = "lblRows";
            lblRows.Size = new System.Drawing.Size(160, 20);
            lblRows.TabIndex = 6;
            lblRows.Text = "Rows";
            //
            // numRows
            //
            numRows.Location = new System.Drawing.Point(180, 116);
            numRows.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numRows.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numRows.Name = "numRows";
            numRows.Size = new System.Drawing.Size(60, 23);
            numRows.TabIndex = 7;
            numRows.Value = new decimal(new int[] { 5, 0, 0, 0 });
            //
            // lblLineWidth
            //
            lblLineWidth.Location = new System.Drawing.Point(14, 148);
            lblLineWidth.Name = "lblLineWidth";
            lblLineWidth.Size = new System.Drawing.Size(160, 20);
            lblLineWidth.TabIndex = 8;
            lblLineWidth.Text = "Line width (px)";
            //
            // numLineWidth
            //
            numLineWidth.Location = new System.Drawing.Point(180, 146);
            numLineWidth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numLineWidth.Maximum = new decimal(new int[] { 40, 0, 0, 0 });
            numLineWidth.Name = "numLineWidth";
            numLineWidth.Size = new System.Drawing.Size(60, 23);
            numLineWidth.TabIndex = 9;
            numLineWidth.Value = new decimal(new int[] { 4, 0, 0, 0 });
            //
            // lblStyle
            //
            lblStyle.Location = new System.Drawing.Point(14, 178);
            lblStyle.Name = "lblStyle";
            lblStyle.Size = new System.Drawing.Size(160, 20);
            lblStyle.TabIndex = 10;
            lblStyle.Text = "Line style";
            //
            // rdoTransparent
            //
            rdoTransparent.Checked = true;
            rdoTransparent.Location = new System.Drawing.Point(180, 176);
            rdoTransparent.Name = "rdoTransparent";
            rdoTransparent.Size = new System.Drawing.Size(120, 24);
            rdoTransparent.TabIndex = 11;
            rdoTransparent.TabStop = true;
            rdoTransparent.Text = "Transparent";
            //
            // rdoSolid
            //
            rdoSolid.Location = new System.Drawing.Point(305, 176);
            rdoSolid.Name = "rdoSolid";
            rdoSolid.Size = new System.Drawing.Size(80, 24);
            rdoSolid.TabIndex = 12;
            rdoSolid.Text = "Solid";
            //
            // btnPickColor
            //
            btnPickColor.Location = new System.Drawing.Point(180, 204);
            btnPickColor.Name = "btnPickColor";
            btnPickColor.Size = new System.Drawing.Size(100, 25);
            btnPickColor.TabIndex = 13;
            btnPickColor.Text = "Pick colour...";
            btnPickColor.UseVisualStyleBackColor = true;
            //
            // pnlColorPreview
            //
            pnlColorPreview.BorderStyle = BorderStyle.FixedSingle;
            pnlColorPreview.Location = new System.Drawing.Point(290, 204);
            pnlColorPreview.Name = "pnlColorPreview";
            pnlColorPreview.Size = new System.Drawing.Size(40, 25);
            pnlColorPreview.TabIndex = 14;
            //
            // btnOK
            //
            btnOK.Location = new System.Drawing.Point(272, 244);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 25);
            btnOK.TabIndex = 15;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            //
            // btnCancel
            //
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(353, 244);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(88, 25);
            btnCancel.TabIndex = 16;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            //
            // GridMosaicDialog
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(455, 281);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(pnlColorPreview);
            Controls.Add(btnPickColor);
            Controls.Add(rdoSolid);
            Controls.Add(rdoTransparent);
            Controls.Add(lblStyle);
            Controls.Add(numLineWidth);
            Controls.Add(lblLineWidth);
            Controls.Add(numRows);
            Controls.Add(lblRows);
            Controls.Add(numColumns);
            Controls.Add(lblColumns);
            Controls.Add(lblDetected);
            Controls.Add(btnBrowseInput);
            Controls.Add(txtInputPath);
            Controls.Add(lblInput);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "GridMosaicDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Grid Mosaic";
            ((System.ComponentModel.ISupportInitialize)numColumns).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRows).EndInit();
            ((System.ComponentModel.ISupportInitialize)numLineWidth).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
