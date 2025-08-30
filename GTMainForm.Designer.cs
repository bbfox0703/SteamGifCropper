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
            btnRestoreTailByte = new System.Windows.Forms.Button();
            pBarTaskStatus = new System.Windows.Forms.ProgressBar();
            lblStatus = new System.Windows.Forms.Label();
            btnMergeAndSplit = new System.Windows.Forms.Button();
            btnMp4ToGif = new System.Windows.Forms.Button();
            panelGifsicle = new System.Windows.Forms.Panel();
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
            numUpDownFramerate = new System.Windows.Forms.NumericUpDown();
            lblFramerate = new System.Windows.Forms.Label();
            lblFPS = new System.Windows.Forms.Label();
            numUpDownPalette = new System.Windows.Forms.NumericUpDown();
            lblPaletteDesc = new System.Windows.Forms.Label();
            btnMerge2to5GifToOne = new System.Windows.Forms.Button();
            panelGifsicle.SuspendLayout();
            groupDither.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numUpDownOptimize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownPaletteSicle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownLossy).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownFramerate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownPalette).BeginInit();
            SuspendLayout();
            // 
            // btnSplitGif
            // 
            btnSplitGif.Location = new System.Drawing.Point(7, 7);
            btnSplitGif.Margin = new System.Windows.Forms.Padding(2);
            btnSplitGif.Name = "btnSplitGif";
            btnSplitGif.Size = new System.Drawing.Size(284, 26);
            btnSplitGif.TabIndex = 0;
            btnSplitGif.Text = "Split GIF file into 5 parts with gifsicle";
            btnSplitGif.UseVisualStyleBackColor = true;
            btnSplitGif.Click += btnSplitGif_Click;
            // 
            // btnResizeGif766
            // 
            btnResizeGif766.Location = new System.Drawing.Point(7, 65);
            btnResizeGif766.Margin = new System.Windows.Forms.Padding(2);
            btnResizeGif766.Name = "btnResizeGif766";
            btnResizeGif766.Size = new System.Drawing.Size(284, 26);
            btnResizeGif766.TabIndex = 1;
            btnResizeGif766.Text = SteamGifCropper.Properties.Resources.Button_ResizeGif;
            btnResizeGif766.UseVisualStyleBackColor = true;
            btnResizeGif766.Click += btnResizeGif766_Click;
            // 
            // btnWriteTailByte
            // 
            btnWriteTailByte.Location = new System.Drawing.Point(7, 125);
            btnWriteTailByte.Margin = new System.Windows.Forms.Padding(2);
            btnWriteTailByte.Name = "btnWriteTailByte";
            btnWriteTailByte.Size = new System.Drawing.Size(284, 26);
            btnWriteTailByte.TabIndex = 2;
            btnWriteTailByte.Text = "Write GIF files last byte as 0x21";
            btnWriteTailByte.UseVisualStyleBackColor = true;
            btnWriteTailByte.Click += btnWriteTailByte_Click;
            // 
            // btnRestoreTailByte
            // 
            btnRestoreTailByte.Location = new System.Drawing.Point(295, 125);
            btnRestoreTailByte.Margin = new System.Windows.Forms.Padding(2);
            btnRestoreTailByte.Name = "btnRestoreTailByte";
            btnRestoreTailByte.Size = new System.Drawing.Size(284, 26);
            btnRestoreTailByte.TabIndex = 6;
            btnRestoreTailByte.Text = "Restore GIF files last byte from 0x21 to 0x3B";
            btnRestoreTailByte.UseVisualStyleBackColor = true;
            btnRestoreTailByte.Click += btnRestoreTailByte_Click;
            // 
            // pBarTaskStatus
            // 
            pBarTaskStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            pBarTaskStatus.Location = new System.Drawing.Point(0, 424);
            pBarTaskStatus.Margin = new System.Windows.Forms.Padding(2);
            pBarTaskStatus.Name = "pBarTaskStatus";
            pBarTaskStatus.Size = new System.Drawing.Size(586, 20);
            pBarTaskStatus.TabIndex = 3;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            lblStatus.Location = new System.Drawing.Point(0, 409);
            lblStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(31, 15);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "Idle.";
            // 
            // btnMergeAndSplit
            // 
            btnMergeAndSplit.Location = new System.Drawing.Point(7, 35);
            btnMergeAndSplit.Margin = new System.Windows.Forms.Padding(2);
            btnMergeAndSplit.Name = "btnMergeAndSplit";
            btnMergeAndSplit.Size = new System.Drawing.Size(284, 26);
            btnMergeAndSplit.TabIndex = 5;
            btnMergeAndSplit.Text = "Merge 5 GIF files into 1 part (766px)";
            btnMergeAndSplit.UseVisualStyleBackColor = true;
            btnMergeAndSplit.Click += btnSplitGIFWithReducedPalette_Click;
            // 
            // btnMp4ToGif
            // 
            btnMp4ToGif.Location = new System.Drawing.Point(7, 95);
            btnMp4ToGif.Margin = new System.Windows.Forms.Padding(2);
            btnMp4ToGif.Name = "btnMp4ToGif";
            btnMp4ToGif.Size = new System.Drawing.Size(284, 26);
            btnMp4ToGif.TabIndex = 7;
            btnMp4ToGif.Text = SteamGifCropper.Properties.Resources.Button_Mp4ToGif;
            btnMp4ToGif.UseVisualStyleBackColor = true;
            btnMp4ToGif.Click += btnMp4ToGif_Click;
            // 
            // panelGifsicle
            // 
            panelGifsicle.Controls.Add(groupDither);
            panelGifsicle.Controls.Add(numUpDownOptimize);
            panelGifsicle.Controls.Add(numUpDownPaletteSicle);
            panelGifsicle.Controls.Add(numUpDownLossy);
            panelGifsicle.Controls.Add(label4);
            panelGifsicle.Controls.Add(label3);
            panelGifsicle.Controls.Add(label2);
            panelGifsicle.Controls.Add(label1);
            panelGifsicle.Controls.Add(chkGifsicle);
            panelGifsicle.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelGifsicle.Location = new System.Drawing.Point(0, 290);
            panelGifsicle.Margin = new System.Windows.Forms.Padding(2);
            panelGifsicle.Name = "panelGifsicle";
            panelGifsicle.Size = new System.Drawing.Size(586, 119);
            panelGifsicle.TabIndex = 11;
            // 
            // groupDither
            // 
            groupDither.Controls.Add(radioBtnDDefault);
            groupDither.Controls.Add(radioBtnDo8);
            groupDither.Controls.Add(radioBtnDro64);
            groupDither.Controls.Add(radioBtnDNone);
            groupDither.Dock = System.Windows.Forms.DockStyle.Bottom;
            groupDither.Location = new System.Drawing.Point(0, 66);
            groupDither.Margin = new System.Windows.Forms.Padding(2);
            groupDither.Name = "groupDither";
            groupDither.Padding = new System.Windows.Forms.Padding(2);
            groupDither.Size = new System.Drawing.Size(586, 53);
            groupDither.TabIndex = 19;
            groupDither.TabStop = false;
            groupDither.Text = "Dither";
            // 
            // radioBtnDDefault
            // 
            radioBtnDDefault.AutoSize = true;
            radioBtnDDefault.Location = new System.Drawing.Point(168, 18);
            radioBtnDDefault.Margin = new System.Windows.Forms.Padding(2);
            radioBtnDDefault.Name = "radioBtnDDefault";
            radioBtnDDefault.Size = new System.Drawing.Size(66, 19);
            radioBtnDDefault.TabIndex = 3;
            radioBtnDDefault.Text = SteamGifCropper.Properties.Resources.Radio_Default;
            radioBtnDDefault.UseVisualStyleBackColor = true;
            radioBtnDDefault.Click += radioBtnDDefault_Click;
            // 
            // radioBtnDo8
            // 
            radioBtnDo8.AutoSize = true;
            radioBtnDo8.Location = new System.Drawing.Point(124, 18);
            radioBtnDo8.Margin = new System.Windows.Forms.Padding(2);
            radioBtnDo8.Name = "radioBtnDo8";
            radioBtnDo8.Size = new System.Drawing.Size(40, 19);
            radioBtnDo8.TabIndex = 2;
            radioBtnDo8.Text = SteamGifCropper.Properties.Resources.Radio_o8;
            radioBtnDo8.UseVisualStyleBackColor = true;
            radioBtnDo8.Click += radioBtnDo8_Click;
            // 
            // radioBtnDro64
            // 
            radioBtnDro64.AutoSize = true;
            radioBtnDro64.Location = new System.Drawing.Point(68, 18);
            radioBtnDro64.Margin = new System.Windows.Forms.Padding(2);
            radioBtnDro64.Name = "radioBtnDro64";
            radioBtnDro64.Size = new System.Drawing.Size(51, 19);
            radioBtnDro64.TabIndex = 1;
            radioBtnDro64.Text = SteamGifCropper.Properties.Resources.Radio_ro64;
            radioBtnDro64.UseVisualStyleBackColor = true;
            radioBtnDro64.Click += radioBtnDro64_Click;
            // 
            // radioBtnDNone
            // 
            radioBtnDNone.AutoSize = true;
            radioBtnDNone.Checked = true;
            radioBtnDNone.Location = new System.Drawing.Point(7, 18);
            radioBtnDNone.Margin = new System.Windows.Forms.Padding(2);
            radioBtnDNone.Name = "radioBtnDNone";
            radioBtnDNone.Size = new System.Drawing.Size(57, 19);
            radioBtnDNone.TabIndex = 0;
            radioBtnDNone.TabStop = true;
            radioBtnDNone.Text = SteamGifCropper.Properties.Resources.Radio_None;
            radioBtnDNone.UseVisualStyleBackColor = true;
            radioBtnDNone.Click += radioBtnDNone_Click;
            // 
            // numUpDownOptimize
            // 
            numUpDownOptimize.Location = new System.Drawing.Point(305, 21);
            numUpDownOptimize.Margin = new System.Windows.Forms.Padding(2);
            numUpDownOptimize.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
            numUpDownOptimize.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numUpDownOptimize.Name = "numUpDownOptimize";
            numUpDownOptimize.Size = new System.Drawing.Size(35, 23);
            numUpDownOptimize.TabIndex = 18;
            numUpDownOptimize.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // numUpDownPaletteSicle
            // 
            numUpDownPaletteSicle.Location = new System.Drawing.Point(175, 21);
            numUpDownPaletteSicle.Margin = new System.Windows.Forms.Padding(2);
            numUpDownPaletteSicle.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPaletteSicle.Minimum = new decimal(new int[] { 32, 0, 0, 0 });
            numUpDownPaletteSicle.Name = "numUpDownPaletteSicle";
            numUpDownPaletteSicle.Size = new System.Drawing.Size(46, 23);
            numUpDownPaletteSicle.TabIndex = 17;
            numUpDownPaletteSicle.Value = new decimal(new int[] { 256, 0, 0, 0 });
            // 
            // numUpDownLossy
            // 
            numUpDownLossy.Location = new System.Drawing.Point(56, 21);
            numUpDownLossy.Margin = new System.Windows.Forms.Padding(2);
            numUpDownLossy.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            numUpDownLossy.Name = "numUpDownLossy";
            numUpDownLossy.Size = new System.Drawing.Size(46, 23);
            numUpDownLossy.TabIndex = 16;
            numUpDownLossy.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(239, 23);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(62, 15);
            label4.TabIndex = 15;
            label4.Text = "Optimize:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(122, 23);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(49, 15);
            label3.TabIndex = 14;
            label3.Text = "Palette:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(7, 23);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(40, 15);
            label2.TabIndex = 13;
            label2.Text = "Lossy:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(222, 3);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(255, 15);
            label1.TabIndex = 12;
            label1.Text = "Notice: gifsicle.exe must be in \"System Path\"";
            // 
            // chkGifsicle
            // 
            chkGifsicle.AutoSize = true;
            chkGifsicle.Location = new System.Drawing.Point(7, 2);
            chkGifsicle.Margin = new System.Windows.Forms.Padding(2);
            chkGifsicle.Name = "chkGifsicle";
            chkGifsicle.Size = new System.Drawing.Size(182, 19);
            chkGifsicle.TabIndex = 11;
            chkGifsicle.Text = "Enable gifsicle optimization";
            chkGifsicle.UseVisualStyleBackColor = true;
            // 
            // numUpDownFramerate
            // 
            numUpDownFramerate.Location = new System.Drawing.Point(78, 224);
            numUpDownFramerate.Margin = new System.Windows.Forms.Padding(2);
            numUpDownFramerate.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            numUpDownFramerate.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numUpDownFramerate.Name = "numUpDownFramerate";
            numUpDownFramerate.Size = new System.Drawing.Size(46, 23);
            numUpDownFramerate.TabIndex = 20;
            numUpDownFramerate.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // lblFramerate
            // 
            lblFramerate.AutoSize = true;
            lblFramerate.Location = new System.Drawing.Point(7, 228);
            lblFramerate.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblFramerate.Name = "lblFramerate";
            lblFramerate.Size = new System.Drawing.Size(67, 15);
            lblFramerate.TabIndex = 21;
            lblFramerate.Text = "Framerate:";
            // 
            // lblFPS
            // 
            lblFPS.AutoSize = true;
            lblFPS.Location = new System.Drawing.Point(128, 228);
            lblFPS.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblFPS.Name = "lblFPS";
            lblFPS.Size = new System.Drawing.Size(24, 15);
            lblFPS.TabIndex = 22;
            lblFPS.Text = "fps";
            // 
            // numUpDownPalette
            // 
            numUpDownPalette.Location = new System.Drawing.Point(126, 255);
            numUpDownPalette.Margin = new System.Windows.Forms.Padding(2);
            numUpDownPalette.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPalette.Minimum = new decimal(new int[] { 32, 0, 0, 0 });
            numUpDownPalette.Name = "numUpDownPalette";
            numUpDownPalette.Size = new System.Drawing.Size(46, 23);
            numUpDownPalette.TabIndex = 8;
            numUpDownPalette.Value = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPalette.Visible = false;
            // 
            // lblPaletteDesc
            // 
            lblPaletteDesc.AutoSize = true;
            lblPaletteDesc.Location = new System.Drawing.Point(7, 257);
            lblPaletteDesc.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblPaletteDesc.Name = "lblPaletteDesc";
            lblPaletteDesc.Size = new System.Drawing.Size(115, 15);
            lblPaletteDesc.TabIndex = 6;
            lblPaletteDesc.Text = "Number of palette:";
            lblPaletteDesc.Visible = false;
            // 
            // btnMerge2to5GifToOne
            // 
            btnMerge2to5GifToOne.Location = new System.Drawing.Point(7, 125);
            btnMerge2to5GifToOne.Name = "btnMerge2to5GifToOne";
            btnMerge2to5GifToOne.Size = new System.Drawing.Size(284, 26);
            btnMerge2to5GifToOne.TabIndex = 23;
            btnMerge2to5GifToOne.Text = SteamGifCropper.Properties.Resources.Button_MergeGifs;
            btnMerge2to5GifToOne.UseVisualStyleBackColor = true;
            btnMerge2to5GifToOne.Click += btnMerge2to5GifToOne_Click;
            // 
            // GifToolMainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            AutoSize = true;
            ClientSize = new System.Drawing.Size(586, 444);
            Controls.Add(panelGifsicle);
            Controls.Add(numUpDownPalette);
            Controls.Add(lblPaletteDesc);
            Controls.Add(btnMp4ToGif);
            Controls.Add(lblFPS);
            Controls.Add(lblFramerate);
            Controls.Add(numUpDownFramerate);
            Controls.Add(btnMergeAndSplit);
            Controls.Add(lblStatus);
            Controls.Add(pBarTaskStatus);
            Controls.Add(btnWriteTailByte);
            Controls.Add(btnRestoreTailByte);
            Controls.Add(btnResizeGif766);
            Controls.Add(btnSplitGif);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            Margin = new System.Windows.Forms.Padding(2);
            MaximizeBox = false;
            Name = "GifToolMainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Steam Gif tool";
            panelGifsicle.ResumeLayout(false);
            panelGifsicle.PerformLayout();
            groupDither.ResumeLayout(false);
            groupDither.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numUpDownOptimize).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownPaletteSicle).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownLossy).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownFramerate).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownPalette).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSplitGif;
        private System.Windows.Forms.Button btnResizeGif766;
        public System.Windows.Forms.ProgressBar pBarTaskStatus;
        private System.Windows.Forms.Button btnWriteTailByte;
        private System.Windows.Forms.Button btnRestoreTailByte;
        public System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnMergeAndSplit;
        private System.Windows.Forms.Button btnMp4ToGif;
        private System.Windows.Forms.Panel panelGifsicle;
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
        public System.Windows.Forms.NumericUpDown numUpDownPalette;
        private System.Windows.Forms.Label lblPaletteDesc;
        public System.Windows.Forms.NumericUpDown numUpDownFramerate;
        private System.Windows.Forms.Label lblFramerate;
        private System.Windows.Forms.Label lblFPS;
        private System.Windows.Forms.Button btnMerge2to5GifToOne;
    }
}