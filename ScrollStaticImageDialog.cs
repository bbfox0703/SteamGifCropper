#nullable enable
using System;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    public class ScrollStaticImageDialog : Form
    {
        public string InputFilePath { get; private set; } = string.Empty;
        public string OutputFilePath { get; private set; } = string.Empty;
        public ScrollDirection Direction { get; private set; } = ScrollDirection.Right;
        public int StepPixels { get; private set; } = 1;
        public int DurationSeconds { get; private set; } = 0;
        public int MoveCount { get; private set; } = 0;
        public bool FullCycle { get; private set; }

        private TextBox txtInputPath = null!;
        private Button btnBrowseInput = null!;
        private TextBox txtOutputPath = null!;
        private Button btnBrowseOutput = null!;
        private ComboBox cmbDirection = null!;
        private NumericUpDown numStep = null!;
        private NumericUpDown numDuration = null!;
        private NumericUpDown numMoveCount = null!;
        private CheckBox chkFullCycle = null!;
        private Button btnOK = null!;
        private Button btnCancel = null!;
        private Label lblInput = null!;
        private Label lblOutput = null!;
        private Label lblDirection = null!;
        private Label lblStep = null!;
        private Label lblDuration = null!;
        private Label lblMoveCount = null!;

        public ScrollStaticImageDialog()
        {
            InitializeComponent();
            chkFullCycle.CheckedChanged += ChkFullCycle_CheckedChanged;
            numDuration.ValueChanged += NumDuration_ValueChanged;
            ChkFullCycle_CheckedChanged(null, EventArgs.Empty);
            UpdateUIText();
            ApplyTheme();
        }

        private void UpdateUIText()
        {
            lblInput.Text = Resources.ScrollDialog_InputLabel;
            lblOutput.Text = Resources.ScrollDialog_OutputLabel;
            lblDirection.Text = Resources.ScrollDialog_Direction;
            lblStep.Text = Resources.ScrollDialog_Step;
            lblDuration.Text = Resources.ScrollDialog_Duration;
            lblMoveCount.Text = Resources.ScrollDialog_MoveCount;
            chkFullCycle.Text = Resources.ScrollDialog_FullCycle;
            btnBrowseInput.Text = Resources.ScrollDialog_Browse;
            btnBrowseOutput.Text = Resources.ScrollDialog_Browse;
            btnOK.Text = Resources.ScrollDialog_OK;
            btnCancel.Text = Resources.ScrollDialog_Cancel;
            Text = Resources.ScrollDialog_Title;

            cmbDirection.Items.Clear();
            cmbDirection.Items.AddRange(new object[]
            {
                Resources.ScrollDialog_DirRight,
                Resources.ScrollDialog_DirLeft,
                Resources.ScrollDialog_DirDown,
                Resources.ScrollDialog_DirUp,
                Resources.ScrollDialog_DirLeftUp,
                Resources.ScrollDialog_DirLeftDown,
                Resources.ScrollDialog_DirRightUp,
                Resources.ScrollDialog_DirRightDown
            });
            cmbDirection.SelectedIndex = 0;
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
                Filter = Resources.FileDialog_ImageFilter,
                Title = Resources.ScrollDialog_SelectInput
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtInputPath.Text = ofd.FileName;
            }
        }

        private void BtnBrowseOutput_Click(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = Resources.ScrollDialog_SelectOutput,
                FileName = Path.GetFileNameWithoutExtension(txtInputPath.Text) + "_scroll.gif"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                txtOutputPath.Text = sfd.FileName;
            }
        }

        private void ChkFullCycle_CheckedChanged(object? sender, EventArgs e)
        {
            numDuration.Enabled = chkFullCycle.Checked;
            NumDuration_ValueChanged(sender, e);
            UpdateMoveCountVisibility();
        }

        private void NumDuration_ValueChanged(object? sender, EventArgs e)
        {
            bool useDuration = numDuration.Enabled && numDuration.Value > 0;
            numStep.Visible = !useDuration;
            numStep.Enabled = !useDuration;
            lblStep.Visible = !useDuration;
            UpdateMoveCountVisibility();
        }

        private void UpdateMoveCountVisibility()
        {
            bool showMoveCount = !chkFullCycle.Checked && numDuration.Value == 0;
            numMoveCount.Visible = showMoveCount;
            numMoveCount.Enabled = showMoveCount;
            lblMoveCount.Visible = showMoveCount;
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
            Direction = (ScrollDirection)cmbDirection.SelectedIndex;
            StepPixels = numStep.Visible ? (int)numStep.Value : 0;
            DurationSeconds = numDuration.Enabled ? (int)numDuration.Value : 0;
            MoveCount = numMoveCount.Visible ? (int)numMoveCount.Value : 0;
            FullCycle = chkFullCycle.Checked;
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
            lblDirection = new Label();
            cmbDirection = new ComboBox();
            lblStep = new Label();
            numStep = new NumericUpDown();
            lblMoveCount = new Label();
            numMoveCount = new NumericUpDown();
            lblDuration = new Label();
            numDuration = new NumericUpDown();
            chkFullCycle = new CheckBox();
            btnOK = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMoveCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDuration).BeginInit();
            SuspendLayout();
            // 
            // lblInput
            // 
            lblInput.Location = new System.Drawing.Point(14, 9);
            lblInput.Name = "lblInput";
            lblInput.Size = new System.Drawing.Size(120, 20);
            lblInput.TabIndex = 0;
            lblInput.Text = "輸入檔案";
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
            lblOutput.Location = new System.Drawing.Point(14, 71);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(120, 20);
            lblOutput.TabIndex = 3;
            lblOutput.Text = "輸出GIF";
            // 
            // txtOutputPath
            // 
            txtOutputPath.Location = new System.Drawing.Point(14, 92);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.ReadOnly = true;
            txtOutputPath.Size = new System.Drawing.Size(333, 23);
            txtOutputPath.TabIndex = 4;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new System.Drawing.Point(353, 90);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new System.Drawing.Size(88, 25);
            btnBrowseOutput.TabIndex = 5;
            btnBrowseOutput.Text = "Browse";
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            // 
            // lblDirection
            // 
            lblDirection.Location = new System.Drawing.Point(14, 134);
            lblDirection.Name = "lblDirection";
            lblDirection.Size = new System.Drawing.Size(120, 20);
            lblDirection.TabIndex = 6;
            lblDirection.Text = "Direction";
            // 
            // cmbDirection
            // 
            cmbDirection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDirection.Location = new System.Drawing.Point(140, 132);
            cmbDirection.Name = "cmbDirection";
            cmbDirection.Size = new System.Drawing.Size(141, 23);
            cmbDirection.TabIndex = 7;
            // 
            // lblStep
            // 
            lblStep.Location = new System.Drawing.Point(14, 167);
            lblStep.Name = "lblStep";
            lblStep.Size = new System.Drawing.Size(120, 20);
            lblStep.TabIndex = 8;
            lblStep.Text = "每次移動像素";
            // 
            // numStep
            // 
            numStep.Location = new System.Drawing.Point(140, 165);
            numStep.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numStep.Name = "numStep";
            numStep.Size = new System.Drawing.Size(60, 23);
            numStep.TabIndex = 9;
            numStep.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // lblMoveCount
            //
            lblMoveCount.Location = new System.Drawing.Point(220, 167);
            lblMoveCount.Name = "lblMoveCount";
            lblMoveCount.Size = new System.Drawing.Size(120, 20);
            lblMoveCount.TabIndex = 10;
            lblMoveCount.Text = "Moves";
            lblMoveCount.Visible = false;
            //
            // numMoveCount
            //
            numMoveCount.Location = new System.Drawing.Point(346, 165);
            numMoveCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numMoveCount.Name = "numMoveCount";
            numMoveCount.Size = new System.Drawing.Size(60, 23);
            numMoveCount.TabIndex = 11;
            numMoveCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numMoveCount.Visible = false;
            //
            // lblDuration
            //
            lblDuration.Location = new System.Drawing.Point(14, 200);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new System.Drawing.Size(120, 20);
            lblDuration.TabIndex = 12;
            lblDuration.Text = "秒數";
            //
            // numDuration
            //
            numDuration.Location = new System.Drawing.Point(140, 198);
            numDuration.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            numDuration.Name = "numDuration";
            numDuration.Size = new System.Drawing.Size(60, 23);
            numDuration.TabIndex = 13;
            //
            // chkFullCycle
            //
            chkFullCycle.Location = new System.Drawing.Point(220, 198);
            chkFullCycle.Name = "chkFullCycle";
            chkFullCycle.Size = new System.Drawing.Size(130, 24);
            chkFullCycle.TabIndex = 14;
            chkFullCycle.Text = "整個捲動一次";
            //
            // btnOK
            //
            btnOK.Location = new System.Drawing.Point(272, 238);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 25);
            btnOK.TabIndex = 15;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            //
            // btnCancel
            //
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(353, 238);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(88, 25);
            btnCancel.TabIndex = 16;
            btnCancel.Text = Resources.ScrollDialog_Cancel;
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // ScrollStaticImageDialog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(455, 275);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(chkFullCycle);
            Controls.Add(numDuration);
            Controls.Add(lblDuration);
            Controls.Add(numMoveCount);
            Controls.Add(lblMoveCount);
            Controls.Add(numStep);
            Controls.Add(lblStep);
            Controls.Add(cmbDirection);
            Controls.Add(lblDirection);
            Controls.Add(btnBrowseOutput);
            Controls.Add(txtOutputPath);
            Controls.Add(lblOutput);
            Controls.Add(btnBrowseInput);
            Controls.Add(txtInputPath);
            Controls.Add(lblInput);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ScrollStaticImageDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "靜態轉動態捲動";
            ((System.ComponentModel.ISupportInitialize)numStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMoveCount).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDuration).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
