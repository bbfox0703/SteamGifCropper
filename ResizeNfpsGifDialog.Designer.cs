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
            this.lblGif = new System.Windows.Forms.Label();
            this.txtGifPath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblOriginalLabel = new System.Windows.Forms.Label();
            this.lblOriginal = new System.Windows.Forms.Label();
            this.lblWidth = new System.Windows.Forms.Label();
            this.numWidth = new System.Windows.Forms.NumericUpDown();
            this.lblHeight = new System.Windows.Forms.Label();
            this.numHeight = new System.Windows.Forms.NumericUpDown();
            this.lblFps = new System.Windows.Forms.Label();
            this.numFps = new System.Windows.Forms.NumericUpDown();
            this.chkLockRatio = new System.Windows.Forms.CheckBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFps)).BeginInit();
            this.SuspendLayout();
            // 
            // lblGif
            // 
            this.lblGif.AutoSize = true;
            this.lblGif.Location = new System.Drawing.Point(12, 15);
            this.lblGif.Name = "lblGif";
            this.lblGif.Size = new System.Drawing.Size(27, 15);
            this.lblGif.TabIndex = 0;
            this.lblGif.Text = "GIF:";
            // 
            // txtGifPath
            // 
            this.txtGifPath.Location = new System.Drawing.Point(60, 12);
            this.txtGifPath.Name = "txtGifPath";
            this.txtGifPath.Size = new System.Drawing.Size(260, 23);
            this.txtGifPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(326, 11);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 25);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.BtnBrowse_Click);
            // 
            // lblOriginalLabel
            // 
            this.lblOriginalLabel.AutoSize = true;
            this.lblOriginalLabel.Location = new System.Drawing.Point(12, 45);
            this.lblOriginalLabel.Name = "lblOriginalLabel";
            this.lblOriginalLabel.Size = new System.Drawing.Size(53, 15);
            this.lblOriginalLabel.TabIndex = 3;
            this.lblOriginalLabel.Text = "Original:";
            // 
            // lblOriginal
            // 
            this.lblOriginal.AutoSize = true;
            this.lblOriginal.Location = new System.Drawing.Point(71, 45);
            this.lblOriginal.Name = "lblOriginal";
            this.lblOriginal.Size = new System.Drawing.Size(0, 15);
            this.lblOriginal.TabIndex = 4;
            // 
            // lblWidth
            // 
            this.lblWidth.AutoSize = true;
            this.lblWidth.Location = new System.Drawing.Point(12, 80);
            this.lblWidth.Name = "lblWidth";
            this.lblWidth.Size = new System.Drawing.Size(42, 15);
            this.lblWidth.TabIndex = 5;
            this.lblWidth.Text = "Width:";
            // 
            // numWidth
            // 
            this.numWidth.Location = new System.Drawing.Point(71, 78);
            this.numWidth.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numWidth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numWidth.Name = "numWidth";
            this.numWidth.Size = new System.Drawing.Size(120, 23);
            this.numWidth.TabIndex = 6;
            this.numWidth.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numWidth.ValueChanged += new System.EventHandler(this.NumWidth_ValueChanged);
            // 
            // lblHeight
            // 
            this.lblHeight.AutoSize = true;
            this.lblHeight.Location = new System.Drawing.Point(12, 109);
            this.lblHeight.Name = "lblHeight";
            this.lblHeight.Size = new System.Drawing.Size(46, 15);
            this.lblHeight.TabIndex = 7;
            this.lblHeight.Text = "Height:";
            // 
            // numHeight
            // 
            this.numHeight.Location = new System.Drawing.Point(71, 107);
            this.numHeight.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numHeight.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numHeight.Name = "numHeight";
            this.numHeight.Size = new System.Drawing.Size(120, 23);
            this.numHeight.TabIndex = 8;
            this.numHeight.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numHeight.ValueChanged += new System.EventHandler(this.NumHeight_ValueChanged);
            // 
            // lblFps
            // 
            this.lblFps.AutoSize = true;
            this.lblFps.Location = new System.Drawing.Point(12, 138);
            this.lblFps.Name = "lblFps";
            this.lblFps.Size = new System.Drawing.Size(28, 15);
            this.lblFps.TabIndex = 9;
            this.lblFps.Text = "FPS:";
            // 
            // numFps
            // 
            this.numFps.Location = new System.Drawing.Point(71, 136);
            this.numFps.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numFps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numFps.Name = "numFps";
            this.numFps.Size = new System.Drawing.Size(120, 23);
            this.numFps.TabIndex = 10;
            this.numFps.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // chkLockRatio
            // 
            this.chkLockRatio.AutoSize = true;
            this.chkLockRatio.Checked = true;
            this.chkLockRatio.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkLockRatio.Location = new System.Drawing.Point(210, 92);
            this.chkLockRatio.Name = "chkLockRatio";
            this.chkLockRatio.Size = new System.Drawing.Size(128, 19);
            this.chkLockRatio.TabIndex = 11;
            this.chkLockRatio.Text = "Lock aspect ratio";
            this.chkLockRatio.UseVisualStyleBackColor = true;
            this.chkLockRatio.CheckedChanged += new System.EventHandler(this.ChkLockRatio_CheckedChanged);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(245, 175);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 25);
            this.btnOk.TabIndex = 12;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(326, 175);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 25);
            this.btnCancel.TabIndex = 13;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // ResizeNfpsGifDialog
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(413, 212);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.chkLockRatio);
            this.Controls.Add(this.numFps);
            this.Controls.Add(this.lblFps);
            this.Controls.Add(this.numHeight);
            this.Controls.Add(this.lblHeight);
            this.Controls.Add(this.numWidth);
            this.Controls.Add(this.lblWidth);
            this.Controls.Add(this.lblOriginal);
            this.Controls.Add(this.lblOriginalLabel);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtGifPath);
            this.Controls.Add(this.lblGif);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ResizeNfpsGifDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Resize GIF";
            ((System.ComponentModel.ISupportInitialize)(this.numWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numFps)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
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
