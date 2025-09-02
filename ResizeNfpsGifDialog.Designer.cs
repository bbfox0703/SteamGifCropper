namespace GifProcessorApp
{
    partial class ResizeNfpsGifDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ResizeNfpsGifDialog));
            lblGif = new System.Windows.Forms.Label();
            txtGifPath = new System.Windows.Forms.TextBox();
            btnBrowse = new System.Windows.Forms.Button();
            lblOriginalLabel = new System.Windows.Forms.Label();
            lblOriginal = new System.Windows.Forms.Label();
            lblWidth = new System.Windows.Forms.Label();
            numWidth = new System.Windows.Forms.NumericUpDown();
            lblHeight = new System.Windows.Forms.Label();
            numHeight = new System.Windows.Forms.NumericUpDown();
            lblFps = new System.Windows.Forms.Label();
            numFps = new System.Windows.Forms.NumericUpDown();
            chkLockRatio = new System.Windows.Forms.CheckBox();
            btnOk = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)numWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numHeight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFps).BeginInit();
            SuspendLayout();
            // 
            // lblGif
            // 
            lblGif.AutoSize = true;
            lblGif.Location = new System.Drawing.Point(12, 15);
            lblGif.Name = "lblGif";
            lblGif.Size = new System.Drawing.Size(28, 15);
            lblGif.TabIndex = 0;
            lblGif.Text = "GIF:";
            // 
            // txtGifPath
            // 
            txtGifPath.Location = new System.Drawing.Point(60, 12);
            txtGifPath.Name = "txtGifPath";
            txtGifPath.Size = new System.Drawing.Size(260, 23);
            txtGifPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new System.Drawing.Point(326, 11);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new System.Drawing.Size(75, 25);
            btnBrowse.TabIndex = 2;
            btnBrowse.Text = "Browse";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // lblOriginalLabel
            // 
            lblOriginalLabel.AutoSize = true;
            lblOriginalLabel.Location = new System.Drawing.Point(12, 45);
            lblOriginalLabel.Name = "lblOriginalLabel";
            lblOriginalLabel.Size = new System.Drawing.Size(55, 15);
            lblOriginalLabel.TabIndex = 3;
            lblOriginalLabel.Text = "Original:";
            // 
            // lblOriginal
            // 
            lblOriginal.AutoSize = true;
            lblOriginal.Location = new System.Drawing.Point(71, 45);
            lblOriginal.Name = "lblOriginal";
            lblOriginal.Size = new System.Drawing.Size(0, 15);
            lblOriginal.TabIndex = 4;
            // 
            // lblWidth
            // 
            lblWidth.AutoSize = true;
            lblWidth.Location = new System.Drawing.Point(12, 80);
            lblWidth.Name = "lblWidth";
            lblWidth.Size = new System.Drawing.Size(44, 15);
            lblWidth.TabIndex = 5;
            lblWidth.Text = "Width:";
            // 
            // numWidth
            // 
            numWidth.Location = new System.Drawing.Point(71, 78);
            numWidth.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numWidth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numWidth.Name = "numWidth";
            numWidth.Size = new System.Drawing.Size(120, 23);
            numWidth.TabIndex = 6;
            numWidth.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numWidth.ValueChanged += NumWidth_ValueChanged;
            // 
            // lblHeight
            // 
            lblHeight.AutoSize = true;
            lblHeight.Location = new System.Drawing.Point(12, 109);
            lblHeight.Name = "lblHeight";
            lblHeight.Size = new System.Drawing.Size(48, 15);
            lblHeight.TabIndex = 7;
            lblHeight.Text = "Height:";
            // 
            // numHeight
            // 
            numHeight.Location = new System.Drawing.Point(71, 107);
            numHeight.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            numHeight.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numHeight.Name = "numHeight";
            numHeight.Size = new System.Drawing.Size(120, 23);
            numHeight.TabIndex = 8;
            numHeight.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numHeight.ValueChanged += NumHeight_ValueChanged;
            // 
            // lblFps
            // 
            lblFps.AutoSize = true;
            lblFps.Location = new System.Drawing.Point(12, 138);
            lblFps.Name = "lblFps";
            lblFps.Size = new System.Drawing.Size(30, 15);
            lblFps.TabIndex = 9;
            lblFps.Text = "FPS:";
            // 
            // numFps
            // 
            numFps.Location = new System.Drawing.Point(71, 136);
            numFps.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            numFps.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numFps.Name = "numFps";
            numFps.Size = new System.Drawing.Size(120, 23);
            numFps.TabIndex = 10;
            numFps.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // chkLockRatio
            // 
            chkLockRatio.AutoSize = true;
            chkLockRatio.Checked = true;
            chkLockRatio.CheckState = System.Windows.Forms.CheckState.Checked;
            chkLockRatio.Location = new System.Drawing.Point(210, 92);
            chkLockRatio.Name = "chkLockRatio";
            chkLockRatio.Size = new System.Drawing.Size(121, 19);
            chkLockRatio.TabIndex = 11;
            chkLockRatio.Text = "Lock aspect ratio";
            chkLockRatio.UseVisualStyleBackColor = true;
            chkLockRatio.CheckedChanged += ChkLockRatio_CheckedChanged;
            // 
            // btnOk
            // 
            btnOk.Location = new System.Drawing.Point(245, 175);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(75, 25);
            btnOk.TabIndex = 12;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += BtnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(326, 175);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 25);
            btnCancel.TabIndex = 13;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // ResizeNfpsGifDialog
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(413, 212);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(chkLockRatio);
            Controls.Add(numFps);
            Controls.Add(lblFps);
            Controls.Add(numHeight);
            Controls.Add(lblHeight);
            Controls.Add(numWidth);
            Controls.Add(lblWidth);
            Controls.Add(lblOriginal);
            Controls.Add(lblOriginalLabel);
            Controls.Add(btnBrowse);
            Controls.Add(txtGifPath);
            Controls.Add(lblGif);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ResizeNfpsGifDialog";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Resize GIF";
            ((System.ComponentModel.ISupportInitialize)numWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)numHeight).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFps).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblGif;
        private System.Windows.Forms.TextBox txtGifPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblOriginalLabel;
        private System.Windows.Forms.Label lblOriginal;
        private System.Windows.Forms.Label lblWidth;
        private System.Windows.Forms.NumericUpDown numWidth;
        private System.Windows.Forms.Label lblHeight;
        private System.Windows.Forms.NumericUpDown numHeight;
        private System.Windows.Forms.Label lblFps;
        private System.Windows.Forms.NumericUpDown numFps;
        private System.Windows.Forms.CheckBox chkLockRatio;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}
