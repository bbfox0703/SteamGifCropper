namespace GifProcessorApp
{
    partial class GifToolMainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GifToolMainForm));
            this.btnSplitGif = new System.Windows.Forms.Button();
            this.btnResizeGif766 = new System.Windows.Forms.Button();
            this.btnWriteTailByte = new System.Windows.Forms.Button();
            this.pBarTaskStatus = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSplitGif
            // 
            this.btnSplitGif.Location = new System.Drawing.Point(12, 12);
            this.btnSplitGif.Name = "btnSplitGif";
            this.btnSplitGif.Size = new System.Drawing.Size(372, 45);
            this.btnSplitGif.TabIndex = 0;
            this.btnSplitGif.Text = "Split Gif file into 5 parts";
            this.btnSplitGif.UseVisualStyleBackColor = true;
            this.btnSplitGif.Click += new System.EventHandler(this.btnSplitGif_Click);
            // 
            // btnResizeGif766
            // 
            this.btnResizeGif766.Location = new System.Drawing.Point(12, 63);
            this.btnResizeGif766.Name = "btnResizeGif766";
            this.btnResizeGif766.Size = new System.Drawing.Size(372, 45);
            this.btnResizeGif766.TabIndex = 1;
            this.btnResizeGif766.Text = "Resize GIF file to 766px width";
            this.btnResizeGif766.UseVisualStyleBackColor = true;
            this.btnResizeGif766.Click += new System.EventHandler(this.btnResizeGif766_Click);
            // 
            // btnWriteTailByte
            // 
            this.btnWriteTailByte.Location = new System.Drawing.Point(12, 114);
            this.btnWriteTailByte.Name = "btnWriteTailByte";
            this.btnWriteTailByte.Size = new System.Drawing.Size(372, 45);
            this.btnWriteTailByte.TabIndex = 2;
            this.btnWriteTailByte.Text = "Write Gif files last byte as 0x21";
            this.btnWriteTailByte.UseVisualStyleBackColor = true;
            this.btnWriteTailByte.Click += new System.EventHandler(this.btnWriteTailByte_Click);
            // 
            // pBarTaskStatus
            // 
            this.pBarTaskStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pBarTaskStatus.Location = new System.Drawing.Point(0, 279);
            this.pBarTaskStatus.Name = "pBarTaskStatus";
            this.pBarTaskStatus.Size = new System.Drawing.Size(396, 36);
            this.pBarTaskStatus.TabIndex = 3;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Location = new System.Drawing.Point(0, 258);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(46, 21);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Idle.";
            // 
            // GifToolMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(168F, 168F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(396, 315);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pBarTaskStatus);
            this.Controls.Add(this.btnWriteTailByte);
            this.Controls.Add(this.btnResizeGif766);
            this.Controls.Add(this.btnSplitGif);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "GifToolMainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Steam Gif tool";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSplitGif;
        private System.Windows.Forms.Button btnResizeGif766;
        public System.Windows.Forms.ProgressBar pBarTaskStatus;
        private System.Windows.Forms.Button btnWriteTailByte;
        public System.Windows.Forms.Label lblStatus;
    }
}