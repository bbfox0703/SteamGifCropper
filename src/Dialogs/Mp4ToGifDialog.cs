using System;
using System.IO;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    public partial class Mp4ToGifDialog : Form
    {
        public string InputFilePath { get; private set; }
        public string OutputFilePath { get; private set; }
        public TimeSpan StartTime { get; private set; }
        public TimeSpan Duration { get; private set; }

        private TextBox txtInputPath;
        private Button btnBrowseInput;
        private TextBox txtOutputPath;
        private Button btnBrowseOutput;
        private NumericUpDown numStartMinutes;
        private NumericUpDown numStartSeconds;
        private NumericUpDown numStartMilliseconds;
        private NumericUpDown numDurationSeconds;
        private Button btnOK;
        private Label lblInput;
        private Label lblOutput;
        private Label lblStartTime;
        private Label lblMinutes;
        private Label lblSeconds;
        private Label lblMs;
        private Label lblDuration;
        private Label lblDurationUnit;
        private Button btnCancel;
        private LinkLabel linkFFmpegHelp;

        public Mp4ToGifDialog()
        {
            InitializeComponent();
            UpdateUIText();
            ApplyTheme();
        }

        /// <summary>
        /// Refreshes all user-facing text based on the current culture.
        /// </summary>
        public void UpdateUIText()
        {
            lblInput.Text = Resources.Mp4Dialog_InputLabel;
            lblOutput.Text = Resources.Mp4Dialog_OutputLabel;
            lblStartTime.Text = Resources.Mp4Dialog_StartTimeLabel;
            lblMinutes.Text = Resources.Mp4Dialog_Min;
            lblSeconds.Text = Resources.Mp4Dialog_Sec;
            lblMs.Text = Resources.Mp4Dialog_Ms;
            lblDuration.Text = Resources.Mp4Dialog_DurationLabel;
            lblDurationUnit.Text = Resources.Mp4Dialog_Seconds;
            btnBrowseInput.Text = Resources.Mp4Dialog_Browse;
            btnBrowseOutput.Text = Resources.Mp4Dialog_Browse;
            btnOK.Text = Resources.Mp4Dialog_Convert;
            btnCancel.Text = Resources.Mp4Dialog_Cancel;
            linkFFmpegHelp.Text = Resources.Mp4Dialog_FFmpegHelp;
            Text = Resources.Mp4Dialog_Title;
        }

        private void ApplyTheme()
        {
            bool isDarkMode = WindowsThemeManager.IsDarkModeEnabled();
            
            if (isDarkMode)
            {
                // Dark theme
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32);
                ForeColor = System.Drawing.Color.White;
                
                // Apply dark theme to all controls
                ApplyDarkThemeToControls(this.Controls);
            }
            else
            {
                // Light theme (default)
                BackColor = System.Drawing.SystemColors.Control;
                ForeColor = System.Drawing.SystemColors.ControlText;
                
                // Apply light theme to all controls
                ApplyLightThemeToControls(this.Controls);
            }
            
            // Apply title bar theme when form handle is created
            if (IsHandleCreated)
            {
                WindowsThemeManager.SetDarkModeForWindow(this.Handle, isDarkMode);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            
            // Apply title bar theme when form becomes visible
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
                else if (control is CheckBox checkBox)
                {
                    checkBox.BackColor = System.Drawing.Color.Transparent;
                    checkBox.ForeColor = System.Drawing.Color.White;
                }
                else if (control is LinkLabel linkLabel)
                {
                    linkLabel.BackColor = System.Drawing.Color.Transparent;
                    linkLabel.LinkColor = System.Drawing.Color.LightBlue;
                    linkLabel.VisitedLinkColor = System.Drawing.Color.LightPink;
                }
                
                // Recursively apply to child controls
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
                else if (control is CheckBox checkBox)
                {
                    checkBox.BackColor = System.Drawing.Color.Transparent;
                    checkBox.ForeColor = System.Drawing.SystemColors.ControlText;
                }
                else if (control is LinkLabel linkLabel)
                {
                    linkLabel.BackColor = System.Drawing.Color.Transparent;
                    linkLabel.LinkColor = System.Drawing.SystemColors.HotTrack;
                    linkLabel.VisitedLinkColor = System.Drawing.SystemColors.HotTrack;
                }
                
                // Recursively apply to child controls
                if (control.HasChildren)
                {
                    ApplyLightThemeToControls(control.Controls);
                }
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Mp4ToGifDialog));
            lblInput = new Label();
            txtInputPath = new TextBox();
            btnBrowseInput = new Button();
            lblOutput = new Label();
            txtOutputPath = new TextBox();
            btnBrowseOutput = new Button();
            lblStartTime = new Label();
            lblMinutes = new Label();
            numStartMinutes = new NumericUpDown();
            lblSeconds = new Label();
            numStartSeconds = new NumericUpDown();
            lblMs = new Label();
            numStartMilliseconds = new NumericUpDown();
            lblDuration = new Label();
            numDurationSeconds = new NumericUpDown();
            lblDurationUnit = new Label();
            btnOK = new Button();
            btnCancel = new Button();
            linkFFmpegHelp = new LinkLabel();
            ((System.ComponentModel.ISupportInitialize)numStartMinutes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStartSeconds).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numStartMilliseconds).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numDurationSeconds).BeginInit();
            SuspendLayout();
            // 
            // lblInput
            // 
            lblInput.Location = new System.Drawing.Point(14, 9);
            lblInput.Margin = new Padding(41, 0, 41, 0);
            lblInput.Name = "lblInput";
            lblInput.Size = new System.Drawing.Size(132, 20);
            lblInput.TabIndex = 0;
            lblInput.Text = "Input MP4 file:";
            // 
            // txtInputPath
            // 
            txtInputPath.Location = new System.Drawing.Point(14, 29);
            txtInputPath.Margin = new Padding(41, 19, 41, 19);
            txtInputPath.Name = "txtInputPath";
            txtInputPath.ReadOnly = true;
            txtInputPath.Size = new System.Drawing.Size(333, 23);
            txtInputPath.TabIndex = 1;
            // 
            // btnBrowseInput
            // 
            btnBrowseInput.Location = new System.Drawing.Point(363, 27);
            btnBrowseInput.Margin = new Padding(41, 19, 41, 19);
            btnBrowseInput.Name = "btnBrowseInput";
            btnBrowseInput.Size = new System.Drawing.Size(88, 25);
            btnBrowseInput.TabIndex = 2;
            btnBrowseInput.Text = Resources.Mp4Dialog_Browse;
            btnBrowseInput.UseVisualStyleBackColor = true;
            btnBrowseInput.Click += BtnBrowseInput_Click;
            // 
            // lblOutput
            // 
            lblOutput.Location = new System.Drawing.Point(14, 71);
            lblOutput.Margin = new Padding(41, 0, 41, 0);
            lblOutput.Name = "lblOutput";
            lblOutput.Size = new System.Drawing.Size(110, 22);
            lblOutput.TabIndex = 3;
            lblOutput.Text = "Output GIF file:";
            // 
            // txtOutputPath
            // 
            txtOutputPath.Location = new System.Drawing.Point(14, 92);
            txtOutputPath.Margin = new Padding(41, 19, 41, 19);
            txtOutputPath.Name = "txtOutputPath";
            txtOutputPath.ReadOnly = true;
            txtOutputPath.Size = new System.Drawing.Size(333, 23);
            txtOutputPath.TabIndex = 4;
            // 
            // btnBrowseOutput
            // 
            btnBrowseOutput.Location = new System.Drawing.Point(363, 90);
            btnBrowseOutput.Margin = new Padding(41, 19, 41, 19);
            btnBrowseOutput.Name = "btnBrowseOutput";
            btnBrowseOutput.Size = new System.Drawing.Size(88, 25);
            btnBrowseOutput.TabIndex = 5;
            btnBrowseOutput.Text = Resources.Mp4Dialog_Browse;
            btnBrowseOutput.UseVisualStyleBackColor = true;
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            // 
            // lblStartTime
            // 
            lblStartTime.Location = new System.Drawing.Point(14, 134);
            lblStartTime.Margin = new Padding(41, 0, 41, 0);
            lblStartTime.Name = "lblStartTime";
            lblStartTime.Size = new System.Drawing.Size(68, 23);
            lblStartTime.TabIndex = 6;
            lblStartTime.Text = "Start time:";
            // 
            // lblMinutes
            // 
            lblMinutes.Location = new System.Drawing.Point(86, 134);
            lblMinutes.Margin = new Padding(41, 0, 41, 0);
            lblMinutes.Name = "lblMinutes";
            lblMinutes.Size = new System.Drawing.Size(39, 23);
            lblMinutes.TabIndex = 7;
            lblMinutes.Text = "min:";
            // 
            // numStartMinutes
            // 
            numStartMinutes.Location = new System.Drawing.Point(120, 132);
            numStartMinutes.Margin = new Padding(41, 19, 41, 19);
            numStartMinutes.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            numStartMinutes.Name = "numStartMinutes";
            numStartMinutes.Size = new System.Drawing.Size(50, 23);
            numStartMinutes.TabIndex = 8;
            // 
            // lblSeconds
            // 
            lblSeconds.Location = new System.Drawing.Point(182, 134);
            lblSeconds.Margin = new Padding(41, 0, 41, 0);
            lblSeconds.Name = "lblSeconds";
            lblSeconds.Size = new System.Drawing.Size(39, 23);
            lblSeconds.TabIndex = 9;
            lblSeconds.Text = "sec:";
            // 
            // numStartSeconds
            // 
            numStartSeconds.Location = new System.Drawing.Point(217, 132);
            numStartSeconds.Margin = new Padding(41, 19, 41, 19);
            numStartSeconds.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            numStartSeconds.Name = "numStartSeconds";
            numStartSeconds.Size = new System.Drawing.Size(50, 23);
            numStartSeconds.TabIndex = 10;
            // 
            // lblMs
            // 
            lblMs.Location = new System.Drawing.Point(282, 134);
            lblMs.Margin = new Padding(41, 0, 41, 0);
            lblMs.Name = "lblMs";
            lblMs.Size = new System.Drawing.Size(40, 23);
            lblMs.TabIndex = 11;
            lblMs.Text = "ms:";
            // 
            // numStartMilliseconds
            // 
            numStartMilliseconds.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            numStartMilliseconds.Location = new System.Drawing.Point(316, 132);
            numStartMilliseconds.Margin = new Padding(41, 19, 41, 19);
            numStartMilliseconds.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
            numStartMilliseconds.Name = "numStartMilliseconds";
            numStartMilliseconds.Size = new System.Drawing.Size(55, 23);
            numStartMilliseconds.TabIndex = 12;
            // 
            // lblDuration
            // 
            lblDuration.Location = new System.Drawing.Point(14, 189);
            lblDuration.Margin = new Padding(41, 0, 41, 0);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new System.Drawing.Size(132, 23);
            lblDuration.TabIndex = 13;
            lblDuration.Text = "Duration (max 30s):";
            // 
            // numDurationSeconds
            // 
            numDurationSeconds.DecimalPlaces = 2;
            numDurationSeconds.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numDurationSeconds.Location = new System.Drawing.Point(151, 187);
            numDurationSeconds.Margin = new Padding(41, 19, 41, 19);
            numDurationSeconds.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            numDurationSeconds.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDurationSeconds.Name = "numDurationSeconds";
            numDurationSeconds.Size = new System.Drawing.Size(70, 23);
            numDurationSeconds.TabIndex = 14;
            numDurationSeconds.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // lblDurationUnit
            // 
            lblDurationUnit.Location = new System.Drawing.Point(222, 189);
            lblDurationUnit.Margin = new Padding(41, 0, 41, 0);
            lblDurationUnit.Name = "lblDurationUnit";
            lblDurationUnit.Size = new System.Drawing.Size(71, 24);
            lblDurationUnit.TabIndex = 15;
            lblDurationUnit.Text = "seconds";
            // 
            // btnOK
            // 
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new System.Drawing.Point(288, 305);
            btnOK.Margin = new Padding(41, 19, 41, 19);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(83, 25);
            btnOK.TabIndex = 16;
            btnOK.Text = Resources.Mp4Dialog_Convert;
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += BtnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(378, 305);
            btnCancel.Margin = new Padding(41, 19, 41, 19);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(73, 25);
            btnCancel.TabIndex = 17;
            btnCancel.Text = Resources.Mp4Dialog_Cancel;
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // linkFFmpegHelp
            // 
            linkFFmpegHelp.Location = new System.Drawing.Point(12, 290);
            linkFFmpegHelp.Margin = new Padding(41, 0, 41, 0);
            linkFFmpegHelp.Name = "linkFFmpegHelp";
            linkFFmpegHelp.Size = new System.Drawing.Size(266, 42);
            linkFFmpegHelp.TabIndex = 18;
            linkFFmpegHelp.TabStop = true;
            linkFFmpegHelp.Text = "How to install FFmpeg? (Required for MP4 conversion)";
            linkFFmpegHelp.LinkClicked += LinkFFmpegHelp_LinkClicked;
            // 
            // 
            // Mp4ToGifDialog
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(479, 341);
            Controls.Add(txtInputPath);
            Controls.Add(txtOutputPath);
            Controls.Add(numStartSeconds);
            Controls.Add(numStartMinutes);
            Controls.Add(numStartMilliseconds);
            Controls.Add(numDurationSeconds);
            Controls.Add(lblInput);
            Controls.Add(btnBrowseInput);
            Controls.Add(lblOutput);
            Controls.Add(btnBrowseOutput);
            Controls.Add(lblStartTime);
            Controls.Add(lblMinutes);
            Controls.Add(lblSeconds);
            Controls.Add(lblMs);
            Controls.Add(lblDuration);
            Controls.Add(lblDurationUnit);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            Controls.Add(linkFFmpegHelp);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            ImeMode = ImeMode.Off;
            Margin = new Padding(41, 19, 41, 19);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Mp4ToGifDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "MP4 to GIF Converter";
            ((System.ComponentModel.ISupportInitialize)numStartMinutes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStartSeconds).EndInit();
            ((System.ComponentModel.ISupportInitialize)numStartMilliseconds).EndInit();
            ((System.ComponentModel.ISupportInitialize)numDurationSeconds).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private void BtnBrowseInput_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = Resources.FileDialog_Mp4Filter,
                Title = Resources.FileDialog_SelectMp4
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtInputPath.Text = openFileDialog.FileName;
                    
                    // Auto-generate output path
                    var inputDir = Path.GetDirectoryName(openFileDialog.FileName);
                    var inputName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    txtOutputPath.Text = Path.Combine(inputDir, $"{inputName}.gif");
                }
            }
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog
            {
                Filter = Resources.FileDialog_GifFilter,
                Title = Resources.FileDialog_SaveGif,
                FileName = txtOutputPath.Text
            })
            {
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputPath.Text = saveFileDialog.FileName;
                }
            }
        }

        private void LinkFFmpegHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(Resources.Mp4Dialog_FFmpegMessage,
                          Resources.Mp4Dialog_FFmpegGuideTitle,
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(txtInputPath.Text))
            {
                MessageBox.Show(Resources.Mp4Dialog_SelectInputFile,
                              Resources.Mp4Dialog_InputRequired,
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(txtInputPath.Text))
            {
                MessageBox.Show(Resources.Mp4Dialog_FileNotFound,
                              Resources.Mp4Dialog_FileNotFoundTitle,
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(txtOutputPath.Text))
            {
                MessageBox.Show(Resources.Mp4Dialog_SpecifyOutput,
                              Resources.Mp4Dialog_OutputRequired,
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(txtOutputPath.Text);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                try
                {
                    Directory.CreateDirectory(outputDir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{Resources.Mp4Dialog_CannotCreateDir}\n{ex.Message}",
                                  Resources.Mp4Dialog_DirectoryError,
                                  MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Set properties - use full paths to avoid path issues
            InputFilePath = Path.GetFullPath(txtInputPath.Text);
            OutputFilePath = Path.GetFullPath(txtOutputPath.Text);
            
            StartTime = new TimeSpan(0, 0, (int)numStartMinutes.Value, (int)numStartSeconds.Value, (int)numStartMilliseconds.Value);
            Duration = TimeSpan.FromSeconds((double)numDurationSeconds.Value);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}