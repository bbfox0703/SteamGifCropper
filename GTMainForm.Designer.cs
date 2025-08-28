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
            btnSplitGif = new System.Windows.Forms.Button();
            btnResizeGif766 = new System.Windows.Forms.Button();
            btnWriteTailByte = new System.Windows.Forms.Button();
            pBarTaskStatus = new System.Windows.Forms.ProgressBar();
            lblStatus = new System.Windows.Forms.Label();
            btnMergeAndSplit = new System.Windows.Forms.Button();
            lblPaletteDesc = new System.Windows.Forms.Label();
            numUpDownPalette = new System.Windows.Forms.NumericUpDown();
            panel1 = new System.Windows.Forms.Panel();
            groupDither = new System.Windows.Forms.GroupBox();
            radioBtnDDefault = new System.Windows.Forms.RadioButton();
            radioBtnDo8 = new System.Windows.Forms.RadioButton();
            radioBtnDro64 = new System.Windows.Forms.RadioButton();
            radioBtnDNone = new System.Windows.Forms.RadioButton();
            numUpDownOptimize = new System.Windows.Forms.NumericUpDown();
            numUpDownPaletteSicle = new System.Windows.Forms.NumericUpDown();
            numUpDownLossy = new System.Windows.Forms.NumericUpDown();
            label4 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            chkGifsicle = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)numUpDownPalette).BeginInit();
            panel1.SuspendLayout();
            groupDither.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numUpDownOptimize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownPaletteSicle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownLossy).BeginInit();
            SuspendLayout();
            // 
            // btnSplitGif
            // 
            btnSplitGif.Location = new System.Drawing.Point(12, 12);
            btnSplitGif.Name = "btnSplitGif";
            btnSplitGif.Size = new System.Drawing.Size(372, 45);
            btnSplitGif.TabIndex = 0;
            btnSplitGif.Text = "Split Gif file into 5 parts with gifsicle";
            btnSplitGif.UseVisualStyleBackColor = true;
            btnSplitGif.Click += btnSplitGif_Click;
            // 
            // btnResizeGif766
            // 
            btnResizeGif766.Location = new System.Drawing.Point(12, 63);
            btnResizeGif766.Name = "btnResizeGif766";
            btnResizeGif766.Size = new System.Drawing.Size(372, 45);
            btnResizeGif766.TabIndex = 1;
            btnResizeGif766.Text = "Resize GIF file to 766px width";
            btnResizeGif766.UseVisualStyleBackColor = true;
            btnResizeGif766.Click += btnResizeGif766_Click;
            // 
            // btnWriteTailByte
            // 
            btnWriteTailByte.Location = new System.Drawing.Point(12, 114);
            btnWriteTailByte.Name = "btnWriteTailByte";
            btnWriteTailByte.Size = new System.Drawing.Size(372, 45);
            btnWriteTailByte.TabIndex = 2;
            btnWriteTailByte.Text = "Write Gif files last byte as 0x21";
            btnWriteTailByte.UseVisualStyleBackColor = true;
            btnWriteTailByte.Click += btnWriteTailByte_Click;
            // 
            // pBarTaskStatus
            // 
            pBarTaskStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            pBarTaskStatus.Location = new System.Drawing.Point(0, 581);
            pBarTaskStatus.Name = "pBarTaskStatus";
            pBarTaskStatus.Size = new System.Drawing.Size(465, 36);
            pBarTaskStatus.TabIndex = 3;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            lblStatus.Location = new System.Drawing.Point(0, 555);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(53, 26);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Idle.";
            // 
            // btnMergeAndSplit
            // 
            btnMergeAndSplit.Location = new System.Drawing.Point(12, 165);
            btnMergeAndSplit.Name = "btnMergeAndSplit";
            btnMergeAndSplit.Size = new System.Drawing.Size(372, 45);
            btnMergeAndSplit.TabIndex = 5;
            btnMergeAndSplit.Text = "Merge & split 5 gif files into 5 parts";
            btnMergeAndSplit.UseVisualStyleBackColor = true;
            btnMergeAndSplit.Visible = false;
            btnMergeAndSplit.Click += btnSplitGIFWithReducedPalette_Click;
            // 
            // lblPaletteDesc
            // 
            lblPaletteDesc.AutoSize = true;
            lblPaletteDesc.Location = new System.Drawing.Point(98, 218);
            lblPaletteDesc.Name = "lblPaletteDesc";
            lblPaletteDesc.Size = new System.Drawing.Size(199, 26);
            lblPaletteDesc.TabIndex = 6;
            lblPaletteDesc.Text = "Number of palette:";
            lblPaletteDesc.Visible = false;
            // 
            // numUpDownPalette
            // 
            numUpDownPalette.Location = new System.Drawing.Point(304, 216);
            numUpDownPalette.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPalette.Minimum = new decimal(new int[] { 32, 0, 0, 0 });
            numUpDownPalette.Name = "numUpDownPalette";
            numUpDownPalette.Size = new System.Drawing.Size(80, 34);
            numUpDownPalette.TabIndex = 8;
            numUpDownPalette.Value = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPalette.Visible = false;
            // 
            // panel1
            // 
            panel1.Controls.Add(groupDither);
            panel1.Controls.Add(numUpDownOptimize);
            panel1.Controls.Add(numUpDownPaletteSicle);
            panel1.Controls.Add(numUpDownLossy);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(chkGifsicle);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 291);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(465, 264);
            panel1.TabIndex = 11;
            // 
            // groupDither
            // 
            groupDither.Controls.Add(radioBtnDDefault);
            groupDither.Controls.Add(radioBtnDo8);
            groupDither.Controls.Add(radioBtnDro64);
            groupDither.Controls.Add(radioBtnDNone);
            groupDither.Dock = System.Windows.Forms.DockStyle.Bottom;
            groupDither.Location = new System.Drawing.Point(0, 191);
            groupDither.Name = "groupDither";
            groupDither.Size = new System.Drawing.Size(465, 73);
            groupDither.TabIndex = 19;
            groupDither.TabStop = false;
            groupDither.Text = "Dither";
            // 
            // radioBtnDDefault
            // 
            radioBtnDDefault.AutoSize = true;
            radioBtnDDefault.Location = new System.Drawing.Point(234, 32);
            radioBtnDDefault.Name = "radioBtnDDefault";
            radioBtnDDefault.Size = new System.Drawing.Size(110, 30);
            radioBtnDDefault.TabIndex = 3;
            radioBtnDDefault.Text = "Default";
            radioBtnDDefault.UseVisualStyleBackColor = true;
            radioBtnDDefault.Click += radioBtnDDefault_Click;
            // 
            // radioBtnDo8
            // 
            radioBtnDo8.AutoSize = true;
            radioBtnDo8.Location = new System.Drawing.Point(173, 32);
            radioBtnDo8.Name = "radioBtnDo8";
            radioBtnDo8.Size = new System.Drawing.Size(62, 30);
            radioBtnDo8.TabIndex = 2;
            radioBtnDo8.Text = "o8";
            radioBtnDo8.UseVisualStyleBackColor = true;
            radioBtnDo8.Click += radioBtnDo8_Click;
            // 
            // radioBtnDro64
            // 
            radioBtnDro64.AutoSize = true;
            radioBtnDro64.Location = new System.Drawing.Point(95, 32);
            radioBtnDro64.Name = "radioBtnDro64";
            radioBtnDro64.Size = new System.Drawing.Size(82, 30);
            radioBtnDro64.TabIndex = 1;
            radioBtnDro64.Text = "ro64";
            radioBtnDro64.UseVisualStyleBackColor = true;
            radioBtnDro64.Click += radioBtnDro64_Click;
            // 
            // radioBtnDNone
            // 
            radioBtnDNone.AutoSize = true;
            radioBtnDNone.Checked = true;
            radioBtnDNone.Location = new System.Drawing.Point(11, 32);
            radioBtnDNone.Name = "radioBtnDNone";
            radioBtnDNone.Size = new System.Drawing.Size(92, 30);
            radioBtnDNone.TabIndex = 0;
            radioBtnDNone.TabStop = true;
            radioBtnDNone.Text = "None";
            radioBtnDNone.UseVisualStyleBackColor = true;
            radioBtnDNone.Click += radioBtnDNone_Click;
            // 
            // numUpDownOptimize
            // 
            numUpDownOptimize.Location = new System.Drawing.Point(101, 130);
            numUpDownOptimize.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
            numUpDownOptimize.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numUpDownOptimize.Name = "numUpDownOptimize";
            numUpDownOptimize.Size = new System.Drawing.Size(61, 34);
            numUpDownOptimize.TabIndex = 18;
            numUpDownOptimize.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // numUpDownPaletteSicle
            // 
            numUpDownPaletteSicle.Location = new System.Drawing.Point(82, 96);
            numUpDownPaletteSicle.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPaletteSicle.Minimum = new decimal(new int[] { 32, 0, 0, 0 });
            numUpDownPaletteSicle.Name = "numUpDownPaletteSicle";
            numUpDownPaletteSicle.Size = new System.Drawing.Size(80, 34);
            numUpDownPaletteSicle.TabIndex = 17;
            numUpDownPaletteSicle.Value = new decimal(new int[] { 256, 0, 0, 0 });
            // 
            // numUpDownLossy
            // 
            numUpDownLossy.Location = new System.Drawing.Point(77, 61);
            numUpDownLossy.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            numUpDownLossy.Name = "numUpDownLossy";
            numUpDownLossy.Size = new System.Drawing.Size(80, 34);
            numUpDownLossy.TabIndex = 16;
            numUpDownLossy.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(8, 132);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(106, 26);
            label4.TabIndex = 15;
            label4.Text = "Optimize:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(8, 98);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(87, 26);
            label3.TabIndex = 14;
            label3.Text = "Palette:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(8, 63);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(72, 26);
            label2.TabIndex = 13;
            label2.Text = "Lossy:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(8, 31);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(447, 26);
            label1.TabIndex = 12;
            label1.Text = "Notice: gifsicle.exe must be in \"System Path\"";
            // 
            // chkGifsicle
            // 
            chkGifsicle.AutoSize = true;
            chkGifsicle.Location = new System.Drawing.Point(12, 3);
            chkGifsicle.Name = "chkGifsicle";
            chkGifsicle.Size = new System.Drawing.Size(305, 30);
            chkGifsicle.TabIndex = 11;
            chkGifsicle.Text = "Enable gifsicle optimization";
            chkGifsicle.UseVisualStyleBackColor = true;
            // 
            // GifToolMainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(168F, 168F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            AutoSize = true;
            ClientSize = new System.Drawing.Size(465, 617);
            Controls.Add(panel1);
            Controls.Add(numUpDownPalette);
            Controls.Add(lblPaletteDesc);
            Controls.Add(btnMergeAndSplit);
            Controls.Add(lblStatus);
            Controls.Add(pBarTaskStatus);
            Controls.Add(btnWriteTailByte);
            Controls.Add(btnResizeGif766);
            Controls.Add(btnSplitGif);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            Name = "GifToolMainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Steam Gif tool";
            ((System.ComponentModel.ISupportInitialize)numUpDownPalette).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            groupDither.ResumeLayout(false);
            groupDither.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numUpDownOptimize).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownPaletteSicle).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownLossy).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSplitGif;
        private System.Windows.Forms.Button btnResizeGif766;
        public System.Windows.Forms.ProgressBar pBarTaskStatus;
        private System.Windows.Forms.Button btnWriteTailByte;
        public System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnMergeAndSplit;
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