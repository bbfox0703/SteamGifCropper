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
            // Resource manager removed - icon loaded directly from file
            this.btnSplitGif = new System.Windows.Forms.Button();
            this.btnResizeGif766 = new System.Windows.Forms.Button();
            this.btnWriteTailByte = new System.Windows.Forms.Button();
            this.pBarTaskStatus = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnSplitGIFWithReducedPalette = new System.Windows.Forms.Button();
            this.lblPaletteDesc = new System.Windows.Forms.Label();
            this.numUpDownPalette = new System.Windows.Forms.NumericUpDown();
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupDither = new System.Windows.Forms.GroupBox();
            this.radioBtnDDefault = new System.Windows.Forms.RadioButton();
            this.radioBtnDo8 = new System.Windows.Forms.RadioButton();
            this.radioBtnDro64 = new System.Windows.Forms.RadioButton();
            this.radioBtnDNone = new System.Windows.Forms.RadioButton();
            this.numUpDownOptimize = new System.Windows.Forms.NumericUpDown();
            this.numUpDownPaletteSicle = new System.Windows.Forms.NumericUpDown();
            this.numUpDownLossy = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.chkGifsicle = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownPalette)).BeginInit();
            this.panel1.SuspendLayout();
            this.groupDither.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownOptimize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownPaletteSicle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownLossy)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSplitGif
            // 
            this.btnSplitGif.Location = new System.Drawing.Point(12, 12);
            this.btnSplitGif.Name = "btnSplitGif";
            this.btnSplitGif.Size = new System.Drawing.Size(372, 45);
            this.btnSplitGif.TabIndex = 0;
            this.btnSplitGif.Text = "Split Gif file into 5 parts with gifsicle";
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
            this.pBarTaskStatus.Location = new System.Drawing.Point(0, 581);
            this.pBarTaskStatus.Name = "pBarTaskStatus";
            this.pBarTaskStatus.Size = new System.Drawing.Size(396, 36);
            this.pBarTaskStatus.TabIndex = 3;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Location = new System.Drawing.Point(0, 560);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(46, 21);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "Idle.";
            // 
            // btnSplitGIFWithReducedPalette
            // 
            this.btnSplitGIFWithReducedPalette.Location = new System.Drawing.Point(12, 165);
            this.btnSplitGIFWithReducedPalette.Name = "btnSplitGIFWithReducedPalette";
            this.btnSplitGIFWithReducedPalette.Size = new System.Drawing.Size(372, 45);
            this.btnSplitGIFWithReducedPalette.TabIndex = 5;
            this.btnSplitGIFWithReducedPalette.Text = "Split Gif file into 5 parts w/ # of palette";
            this.btnSplitGIFWithReducedPalette.UseVisualStyleBackColor = true;
            this.btnSplitGIFWithReducedPalette.Visible = false;
            this.btnSplitGIFWithReducedPalette.Click += new System.EventHandler(this.btnSplitGIFWithReducedPalette_Click);
            // 
            // lblPaletteDesc
            // 
            this.lblPaletteDesc.AutoSize = true;
            this.lblPaletteDesc.Location = new System.Drawing.Point(139, 218);
            this.lblPaletteDesc.Name = "lblPaletteDesc";
            this.lblPaletteDesc.Size = new System.Drawing.Size(159, 21);
            this.lblPaletteDesc.TabIndex = 6;
            this.lblPaletteDesc.Text = "Number of palette:";
            this.lblPaletteDesc.Visible = false;
            // 
            // numUpDownPalette
            // 
            this.numUpDownPalette.Location = new System.Drawing.Point(304, 216);
            this.numUpDownPalette.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numUpDownPalette.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.numUpDownPalette.Name = "numUpDownPalette";
            this.numUpDownPalette.Size = new System.Drawing.Size(80, 33);
            this.numUpDownPalette.TabIndex = 8;
            this.numUpDownPalette.Value = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numUpDownPalette.Visible = false;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupDither);
            this.panel1.Controls.Add(this.numUpDownOptimize);
            this.panel1.Controls.Add(this.numUpDownPaletteSicle);
            this.panel1.Controls.Add(this.numUpDownLossy);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.chkGifsicle);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 296);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(396, 264);
            this.panel1.TabIndex = 11;
            // 
            // groupDither
            // 
            this.groupDither.Controls.Add(this.radioBtnDDefault);
            this.groupDither.Controls.Add(this.radioBtnDo8);
            this.groupDither.Controls.Add(this.radioBtnDro64);
            this.groupDither.Controls.Add(this.radioBtnDNone);
            this.groupDither.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupDither.Location = new System.Drawing.Point(0, 191);
            this.groupDither.Name = "groupDither";
            this.groupDither.Size = new System.Drawing.Size(396, 73);
            this.groupDither.TabIndex = 19;
            this.groupDither.TabStop = false;
            this.groupDither.Text = "Dither";
            // 
            // radioBtnDDefault
            // 
            this.radioBtnDDefault.AutoSize = true;
            this.radioBtnDDefault.Location = new System.Drawing.Point(234, 32);
            this.radioBtnDDefault.Name = "radioBtnDDefault";
            this.radioBtnDDefault.Size = new System.Drawing.Size(94, 25);
            this.radioBtnDDefault.TabIndex = 3;
            this.radioBtnDDefault.Text = "Default";
            this.radioBtnDDefault.UseVisualStyleBackColor = true;
            this.radioBtnDDefault.Click += new System.EventHandler(this.radioBtnDDefault_Click);
            // 
            // radioBtnDo8
            // 
            this.radioBtnDo8.AutoSize = true;
            this.radioBtnDo8.Location = new System.Drawing.Point(173, 32);
            this.radioBtnDo8.Name = "radioBtnDo8";
            this.radioBtnDo8.Size = new System.Drawing.Size(55, 25);
            this.radioBtnDo8.TabIndex = 2;
            this.radioBtnDo8.Text = "o8";
            this.radioBtnDo8.UseVisualStyleBackColor = true;
            this.radioBtnDo8.Click += new System.EventHandler(this.radioBtnDo8_Click);
            // 
            // radioBtnDro64
            // 
            this.radioBtnDro64.AutoSize = true;
            this.radioBtnDro64.Location = new System.Drawing.Point(95, 32);
            this.radioBtnDro64.Name = "radioBtnDro64";
            this.radioBtnDro64.Size = new System.Drawing.Size(72, 25);
            this.radioBtnDro64.TabIndex = 1;
            this.radioBtnDro64.Text = "ro64";
            this.radioBtnDro64.UseVisualStyleBackColor = true;
            this.radioBtnDro64.Click += new System.EventHandler(this.radioBtnDro64_Click);
            // 
            // radioBtnDNone
            // 
            this.radioBtnDNone.AutoSize = true;
            this.radioBtnDNone.Checked = true;
            this.radioBtnDNone.Location = new System.Drawing.Point(11, 32);
            this.radioBtnDNone.Name = "radioBtnDNone";
            this.radioBtnDNone.Size = new System.Drawing.Size(78, 25);
            this.radioBtnDNone.TabIndex = 0;
            this.radioBtnDNone.TabStop = true;
            this.radioBtnDNone.Text = "None";
            this.radioBtnDNone.UseVisualStyleBackColor = true;
            this.radioBtnDNone.Click += new System.EventHandler(this.radioBtnDNone_Click);
            // 
            // numUpDownOptimize
            // 
            this.numUpDownOptimize.Location = new System.Drawing.Point(101, 130);
            this.numUpDownOptimize.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numUpDownOptimize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numUpDownOptimize.Name = "numUpDownOptimize";
            this.numUpDownOptimize.Size = new System.Drawing.Size(61, 33);
            this.numUpDownOptimize.TabIndex = 18;
            this.numUpDownOptimize.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // numUpDownPaletteSicle
            // 
            this.numUpDownPaletteSicle.Location = new System.Drawing.Point(82, 96);
            this.numUpDownPaletteSicle.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.numUpDownPaletteSicle.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
            this.numUpDownPaletteSicle.Name = "numUpDownPaletteSicle";
            this.numUpDownPaletteSicle.Size = new System.Drawing.Size(80, 33);
            this.numUpDownPaletteSicle.TabIndex = 17;
            this.numUpDownPaletteSicle.Value = new decimal(new int[] {
            256,
            0,
            0,
            0});
            // 
            // numUpDownLossy
            // 
            this.numUpDownLossy.Location = new System.Drawing.Point(77, 61);
            this.numUpDownLossy.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numUpDownLossy.Name = "numUpDownLossy";
            this.numUpDownLossy.Size = new System.Drawing.Size(80, 33);
            this.numUpDownLossy.TabIndex = 16;
            this.numUpDownLossy.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 132);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(87, 21);
            this.label4.TabIndex = 15;
            this.label4.Text = "Optimize:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 98);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 21);
            this.label3.TabIndex = 14;
            this.label3.Text = "Palette:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 21);
            this.label2.TabIndex = 13;
            this.label2.Text = "Lossy:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(369, 21);
            this.label1.TabIndex = 12;
            this.label1.Text = "Notice: gifsicle.exe must be in \"System Path\"";
            // 
            // chkGifsicle
            // 
            this.chkGifsicle.AutoSize = true;
            this.chkGifsicle.Location = new System.Drawing.Point(12, 3);
            this.chkGifsicle.Name = "chkGifsicle";
            this.chkGifsicle.Size = new System.Drawing.Size(257, 25);
            this.chkGifsicle.TabIndex = 11;
            this.chkGifsicle.Text = "Enable gifsicle optimization";
            this.chkGifsicle.UseVisualStyleBackColor = true;
            // 
            // GifToolMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(168F, 168F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(396, 617);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.numUpDownPalette);
            this.Controls.Add(this.lblPaletteDesc);
            this.Controls.Add(this.btnSplitGIFWithReducedPalette);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pBarTaskStatus);
            this.Controls.Add(this.btnWriteTailByte);
            this.Controls.Add(this.btnResizeGif766);
            this.Controls.Add(this.btnSplitGif);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            // Load icon from file instead of embedded resource to avoid resource issues
            try
            {
                this.Icon = new System.Drawing.Icon("icon.ico");
            }
            catch
            {
                // If icon file is not found, continue without icon
                this.Icon = null;
            }
            this.MaximizeBox = false;
            this.Name = "GifToolMainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Steam Gif tool";
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownPalette)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupDither.ResumeLayout(false);
            this.groupDither.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownOptimize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownPaletteSicle)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownLossy)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSplitGif;
        private System.Windows.Forms.Button btnResizeGif766;
        public System.Windows.Forms.ProgressBar pBarTaskStatus;
        private System.Windows.Forms.Button btnWriteTailByte;
        public System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnSplitGIFWithReducedPalette;
        private System.Windows.Forms.Label lblPaletteDesc;
        public System.Windows.Forms.NumericUpDown numUpDownPalette;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        public System.Windows.Forms.CheckBox chkGifsicle;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.NumericUpDown numUpDownOptimize;
        public System.Windows.Forms.NumericUpDown numUpDownPaletteSicle;
        public System.Windows.Forms.NumericUpDown numUpDownLossy;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.GroupBox groupDither;
        private System.Windows.Forms.RadioButton radioBtnDDefault;
        private System.Windows.Forms.RadioButton radioBtnDo8;
        private System.Windows.Forms.RadioButton radioBtnDro64;
        private System.Windows.Forms.RadioButton radioBtnDNone;
    }
}