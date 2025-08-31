using System;
using System.ComponentModel;
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

        private TextBox txtBaseGif;
        private Button btnBrowseBase;
        private TextBox txtOverlayGif;
        private Button btnBrowseOverlay;
        private Label lblBase;
        private Label lblOverlay;
        private Label lblX;
        private Label lblY;
        private NumericUpDown numX;
        private NumericUpDown numY;
        private Button btnOK;
        private Button btnCancel;

        public OverlayGifDialog()
        {
            InitializeComponent();
            WindowsThemeManager.ApplyThemeToControl(this, WindowsThemeManager.IsDarkModeEnabled());
        }

        private void BtnBrowseBase_Click(object? sender, EventArgs e)
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
                    numX.Maximum = collection[0].Width > 0 ? collection[0].Width - 1 : 0;
                    numY.Maximum = collection[0].Height > 0 ? collection[0].Height - 1 : 0;
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
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtOverlayGif.Text = dialog.FileName;
            }
        }

        private void InitializeComponent()
        {
            ComponentResourceManager resources = new ComponentResourceManager(typeof(OverlayGifDialog));
            lblBase = new Label();
            txtBaseGif = new TextBox();
            btnBrowseBase = new Button();
            lblOverlay = new Label();
            txtOverlayGif = new TextBox();
            btnBrowseOverlay = new Button();
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
            lblBase.Name = "lblBase";
            lblBase.Location = new System.Drawing.Point(12, 15);
            resources.ApplyResources(lblBase, "lblBase");
            //
            // txtBaseGif
            //
            txtBaseGif.Name = "txtBaseGif";
            txtBaseGif.Location = new System.Drawing.Point(96, 12);
            txtBaseGif.Size = new System.Drawing.Size(260, 23);
            txtBaseGif.TabIndex = 0;
            resources.ApplyResources(txtBaseGif, "txtBaseGif");
            //
            // btnBrowseBase
            //
            btnBrowseBase.Name = "btnBrowseBase";
            btnBrowseBase.Location = new System.Drawing.Point(362, 11);
            btnBrowseBase.Size = new System.Drawing.Size(75, 25);
            btnBrowseBase.TabIndex = 1;
            resources.ApplyResources(btnBrowseBase, "btnBrowseBase");
            btnBrowseBase.UseVisualStyleBackColor = true;
            btnBrowseBase.Click += BtnBrowseBase_Click;
            //
            // lblOverlay
            //
            lblOverlay.AutoSize = true;
            lblOverlay.Name = "lblOverlay";
            lblOverlay.Location = new System.Drawing.Point(12, 53);
            resources.ApplyResources(lblOverlay, "lblOverlay");
            //
            // txtOverlayGif
            //
            txtOverlayGif.Name = "txtOverlayGif";
            txtOverlayGif.Location = new System.Drawing.Point(96, 50);
            txtOverlayGif.Size = new System.Drawing.Size(260, 23);
            txtOverlayGif.TabIndex = 2;
            resources.ApplyResources(txtOverlayGif, "txtOverlayGif");
            //
            // btnBrowseOverlay
            //
            btnBrowseOverlay.Name = "btnBrowseOverlay";
            btnBrowseOverlay.Location = new System.Drawing.Point(362, 49);
            btnBrowseOverlay.Size = new System.Drawing.Size(75, 25);
            btnBrowseOverlay.TabIndex = 3;
            resources.ApplyResources(btnBrowseOverlay, "btnBrowseOverlay");
            btnBrowseOverlay.UseVisualStyleBackColor = true;
            btnBrowseOverlay.Click += BtnBrowseOverlay_Click;
            //
            // lblX
            //
            lblX.AutoSize = true;
            lblX.Name = "lblX";
            lblX.Location = new System.Drawing.Point(12, 91);
            resources.ApplyResources(lblX, "lblX");
            //
            // numX
            //
            numX.Name = "numX";
            numX.Location = new System.Drawing.Point(96, 89);
            numX.Maximum = int.MaxValue;
            numX.Size = new System.Drawing.Size(120, 23);
            numX.TabIndex = 4;
            resources.ApplyResources(numX, "numX");
            //
            // lblY
            //
            lblY.AutoSize = true;
            lblY.Name = "lblY";
            lblY.Location = new System.Drawing.Point(230, 91);
            resources.ApplyResources(lblY, "lblY");
            //
            // numY
            //
            numY.Name = "numY";
            numY.Location = new System.Drawing.Point(264, 89);
            numY.Maximum = int.MaxValue;
            numY.Size = new System.Drawing.Size(120, 23);
            numY.TabIndex = 5;
            resources.ApplyResources(numY, "numY");
            //
            // btnOK
            //
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Name = "btnOK";
            btnOK.Location = new System.Drawing.Point(96, 128);
            btnOK.Size = new System.Drawing.Size(100, 27);
            btnOK.TabIndex = 6;
            resources.ApplyResources(btnOK, "btnOK");
            btnOK.UseVisualStyleBackColor = true;
            //
            // btnCancel
            //
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Name = "btnCancel";
            btnCancel.Location = new System.Drawing.Point(206, 128);
            btnCancel.Size = new System.Drawing.Size(100, 27);
            btnCancel.TabIndex = 7;
            resources.ApplyResources(btnCancel, "btnCancel");
            btnCancel.UseVisualStyleBackColor = true;
            //
            // OverlayGifDialog
            //
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(450, 170);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(numY);
            Controls.Add(lblY);
            Controls.Add(numX);
            Controls.Add(lblX);
            Controls.Add(btnBrowseOverlay);
            Controls.Add(txtOverlayGif);
            Controls.Add(lblOverlay);
            Controls.Add(btnBrowseBase);
            Controls.Add(txtBaseGif);
            Controls.Add(lblBase);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "OverlayGifDialog";
            StartPosition = FormStartPosition.CenterParent;
            resources.ApplyResources(this, "$this");
            ((ISupportInitialize)numX).EndInit();
            ((ISupportInitialize)numY).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
