using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using ImageMagick;

namespace GifProcessorApp
{
    public partial class OverlayGifDialog : Form
    {
        public string BaseGifPath => txtBaseGif.Text;
        public string OverlayGifPath => txtOverlayGif.Text;
        public int OverlayX => (int)numX.Value;
        public int OverlayY => (int)numY.Value;
        public bool ResampleBaseFrames => chkResampleBase.Checked;

        private readonly ComponentResourceManager _resources = new(typeof(OverlayGifDialog));

        private TextBox txtBaseGif;
        private Button btnBrowseBase;
        private TextBox txtOverlayGif;
        private Button btnBrowseOverlay;
        private Label lblBase;
        private Label lblOverlay;
        private Label lblBaseInfo;
        private Label lblOverlayInfo;
        private Label lblX;
        private Label lblY;
        private CheckBox chkResampleBase;
        private NumericUpDown numX;
        private NumericUpDown numY;
        private Button btnOK;
        private Button btnCancel;

        public OverlayGifDialog()
        {
            InitializeComponent();
            UpdateUIText();
            WindowsThemeManager.ApplyThemeToControl(this, WindowsThemeManager.IsDarkModeEnabled());
        }

        /// <summary>
        /// Refreshes user interface text based on the current culture.
        /// </summary>
        public void UpdateUIText()
        {
            lblBase.Text = _resources.GetString("lblBase.Text") ?? lblBase.Text;
            btnBrowseBase.Text = _resources.GetString("btnBrowseBase.Text") ?? btnBrowseBase.Text;
            lblOverlay.Text = _resources.GetString("lblOverlay.Text") ?? lblOverlay.Text;
            btnBrowseOverlay.Text = _resources.GetString("btnBrowseOverlay.Text") ?? btnBrowseOverlay.Text;
            chkResampleBase.Text = _resources.GetString("chkResampleBase.Text") ?? chkResampleBase.Text;
            lblX.Text = _resources.GetString("lblX.Text") ?? lblX.Text;
            lblY.Text = _resources.GetString("lblY.Text") ?? lblY.Text;
            btnOK.Text = _resources.GetString("btnOK.Text") ?? btnOK.Text;
            btnCancel.Text = _resources.GetString("btnCancel.Text") ?? btnCancel.Text;
            Text = _resources.GetString("$this.Text") ?? Text;
        }

        private void BtnBrowseBase_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtBaseGif.Text = dialog.FileName;
                try
                {
                    using var collection = new MagickImageCollection(dialog.FileName);
                    int width = (int)collection[0].Width;
                    int height = (int)collection[0].Height;
                    numX.Maximum = width > 0 ? width - 1 : 0;
                    numY.Maximum = height > 0 ? height - 1 : 0;

                    double avgDelay = collection.Average(img => (double)img.AnimationDelay);
                    double fps = avgDelay > 0 ? 100.0 / avgDelay : 0;
                    lblBaseInfo.Text = string.Format(
                        _resources.GetString("GifInfoFormat") ?? "{0}×{1}, {2} fps",
                        width,
                        height,
                        fps.ToString("0.##"));
                }
                catch
                {
                    // Ignore errors determining dimensions
                }
            }
        }

        private void BtnBrowseOverlay_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter
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
                        _resources.GetString("GifInfoFormat") ?? "{0}×{1}, {2} fps",
                        width,
                        height,
                        fps.ToString("0.##"));
                }
                catch
                {
                    // Ignore errors determining dimensions
                }
            }
        }

        private void InitializeComponent()
        {
            lblBase = new Label();
            txtBaseGif = new TextBox();
            btnBrowseBase = new Button();
            lblOverlay = new Label();
            txtOverlayGif = new TextBox();
            btnBrowseOverlay = new Button();
            lblBaseInfo = new Label();
            lblOverlayInfo = new Label();
            chkResampleBase = new CheckBox();
            lblX = new Label();
            numX = new NumericUpDown();
            lblY = new Label();
            numY = new NumericUpDown();
            btnOK = new Button();
            btnCancel = new Button();
            ((ISupportInitialize)numX).BeginInit();
            ((ISupportInitialize)numY).BeginInit();
            SuspendLayout();
            // 
            // lblBase
            // 
            lblBase.AutoSize = true;
            lblBase.Location = new System.Drawing.Point(12, 15);
            lblBase.Name = "lblBase";
            lblBase.Size = new System.Drawing.Size(57, 15);
            lblBase.TabIndex = 14;
            lblBase.Text = "Base GIF:";
            // 
            // txtBaseGif
            // 
            txtBaseGif.Location = new System.Drawing.Point(96, 12);
            txtBaseGif.Name = "txtBaseGif";
            txtBaseGif.Size = new System.Drawing.Size(260, 23);
            txtBaseGif.TabIndex = 0;
            // 
            // btnBrowseBase
            // 
            btnBrowseBase.Location = new System.Drawing.Point(362, 12);
            btnBrowseBase.Name = "btnBrowseBase";
            btnBrowseBase.Size = new System.Drawing.Size(75, 26);
            btnBrowseBase.TabIndex = 1;
            btnBrowseBase.Text = "Browse...";
            btnBrowseBase.UseVisualStyleBackColor = true;
            btnBrowseBase.Click += BtnBrowseBase_Click;
            // 
            // lblOverlay
            // 
            lblOverlay.AutoSize = true;
            lblOverlay.Location = new System.Drawing.Point(12, 65);
            lblOverlay.Name = "lblOverlay";
            lblOverlay.Size = new System.Drawing.Size(74, 15);
            lblOverlay.TabIndex = 12;
            lblOverlay.Text = "Overlay GIF:";
            // 
            // txtOverlayGif
            // 
            txtOverlayGif.Location = new System.Drawing.Point(96, 62);
            txtOverlayGif.Name = "txtOverlayGif";
            txtOverlayGif.Size = new System.Drawing.Size(260, 23);
            txtOverlayGif.TabIndex = 2;
            // 
            // btnBrowseOverlay
            // 
            btnBrowseOverlay.Location = new System.Drawing.Point(362, 62);
            btnBrowseOverlay.Name = "btnBrowseOverlay";
            btnBrowseOverlay.Size = new System.Drawing.Size(75, 26);
            btnBrowseOverlay.TabIndex = 3;
            btnBrowseOverlay.Text = "Browse...";
            btnBrowseOverlay.UseVisualStyleBackColor = true;
            btnBrowseOverlay.Click += BtnBrowseOverlay_Click;
            // 
            // lblBaseInfo
            // 
            lblBaseInfo.AutoSize = true;
            lblBaseInfo.Location = new System.Drawing.Point(96, 37);
            lblBaseInfo.Name = "lblBaseInfo";
            lblBaseInfo.Size = new System.Drawing.Size(0, 15);
            lblBaseInfo.TabIndex = 13;
            // 
            // lblOverlayInfo
            // 
            lblOverlayInfo.AutoSize = true;
            lblOverlayInfo.Location = new System.Drawing.Point(96, 92);
            lblOverlayInfo.Name = "lblOverlayInfo";
            lblOverlayInfo.Size = new System.Drawing.Size(0, 15);
            lblOverlayInfo.TabIndex = 11;
            // 
            // chkResampleBase
            // 
            chkResampleBase.Checked = true;
            chkResampleBase.CheckState = CheckState.Checked;
            chkResampleBase.Location = new System.Drawing.Point(94, 116);
            chkResampleBase.Name = "chkResampleBase";
            chkResampleBase.Size = new System.Drawing.Size(260, 19);
            chkResampleBase.TabIndex = 4;
            chkResampleBase.Text = "Resample base GIF to overlay FPS";
            chkResampleBase.UseVisualStyleBackColor = true;
            // 
            // lblX
            // 
            lblX.AutoSize = true;
            lblX.Location = new System.Drawing.Point(51, 144);
            lblX.Name = "lblX";
            lblX.Size = new System.Drawing.Size(18, 15);
            lblX.TabIndex = 10;
            lblX.Text = "X:";
            // 
            // numX
            // 
            numX.Location = new System.Drawing.Point(94, 142);
            numX.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numX.Name = "numX";
            numX.Size = new System.Drawing.Size(120, 23);
            numX.TabIndex = 5;
            // 
            // lblY
            // 
            lblY.AutoSize = true;
            lblY.Location = new System.Drawing.Point(228, 144);
            lblY.Name = "lblY";
            lblY.Size = new System.Drawing.Size(17, 15);
            lblY.TabIndex = 9;
            lblY.Text = "Y:";
            // 
            // numY
            // 
            numY.Location = new System.Drawing.Point(262, 142);
            numY.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numY.Name = "numY";
            numY.Size = new System.Drawing.Size(120, 23);
            numY.TabIndex = 6;
            // 
            // btnOK
            // 
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new System.Drawing.Point(281, 172);
            btnOK.Name = "btnOK";
            btnOK.Size = new System.Drawing.Size(75, 26);
            btnOK.TabIndex = 7;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(362, 172);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 26);
            btnCancel.TabIndex = 8;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // OverlayGifDialog
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(450, 210);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(numY);
            Controls.Add(lblY);
            Controls.Add(numX);
            Controls.Add(lblX);
            Controls.Add(chkResampleBase);
            Controls.Add(lblOverlayInfo);
            Controls.Add(btnBrowseOverlay);
            Controls.Add(txtOverlayGif);
            Controls.Add(lblOverlay);
            Controls.Add(lblBaseInfo);
            Controls.Add(btnBrowseBase);
            Controls.Add(txtBaseGif);
            Controls.Add(lblBase);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "OverlayGifDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Overlay GIF";
            ((ISupportInitialize)numX).EndInit();
            ((ISupportInitialize)numY).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
