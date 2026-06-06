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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GifToolMainForm));
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
            numUpDownPalette = new System.Windows.Forms.NumericUpDown();
            lblPaletteDesc = new System.Windows.Forms.Label();
            lblFramerate = new System.Windows.Forms.Label();
            lblFPS = new System.Windows.Forms.Label();
            btnMerge2to5GifToOne = new System.Windows.Forms.Button();
            chk5GIFMergeFasterPaletteProcess = new System.Windows.Forms.CheckBox();
            btnReverseGIF = new System.Windows.Forms.Button();
            btnLanguageChange = new System.Windows.Forms.Button();
            conMenuLangSwitch = new System.Windows.Forms.ContextMenuStrip(components);
            toolStripLangEnglish = new System.Windows.Forms.ToolStripMenuItem();
            toolStripLangTradChinese = new System.Windows.Forms.ToolStripMenuItem();
            toolStripLangJapanese = new System.Windows.Forms.ToolStripMenuItem();
            btnScrollStaticImage = new System.Windows.Forms.Button();
            btnScrollAnimatedGif = new System.Windows.Forms.Button();
            btnOverlayGIF = new System.Windows.Forms.Button();
            btnResizeNfpsGIF = new System.Windows.Forms.Button();
            lblResourceLimitDesc = new System.Windows.Forms.Label();
            numUpDownFramerate = new System.Windows.Forms.NumericUpDown();
            btnConcatenateGifs = new System.Windows.Forms.Button();
            panelGifsicle.SuspendLayout();
            groupDither.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numUpDownOptimize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownPaletteSicle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownLossy).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpDownPalette).BeginInit();
            conMenuLangSwitch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numUpDownFramerate).BeginInit();
            SuspendLayout();
            // 
            // btnSplitGif
            // 
            btnSplitGif.Location = new System.Drawing.Point(7, 7);
            btnSplitGif.Margin = new System.Windows.Forms.Padding(2);
            btnSplitGif.Name = "btnSplitGif";
            btnSplitGif.Size = new System.Drawing.Size(300, 26);
            btnSplitGif.TabIndex = 0;
            btnSplitGif.Text = SteamGifCropper.Properties.Resources.Button_SplitGif;
            btnSplitGif.UseVisualStyleBackColor = true;
            btnSplitGif.Click += btnSplitGif_Click;
            // 
            // btnResizeGif766
            // 
            btnResizeGif766.Location = new System.Drawing.Point(7, 95);
            btnResizeGif766.Margin = new System.Windows.Forms.Padding(2);
            btnResizeGif766.Name = "btnResizeGif766";
            btnResizeGif766.Size = new System.Drawing.Size(300, 26);
            btnResizeGif766.TabIndex = 4;
            btnResizeGif766.Text = SteamGifCropper.Properties.Resources.Button_ResizeGif;
            btnResizeGif766.UseVisualStyleBackColor = true;
            btnResizeGif766.Click += btnResizeGif766_Click;
            // 
            // btnWriteTailByte
            // 
            btnWriteTailByte.Location = new System.Drawing.Point(7, 157);
            btnWriteTailByte.Margin = new System.Windows.Forms.Padding(2);
            btnWriteTailByte.Name = "btnWriteTailByte";
            btnWriteTailByte.Size = new System.Drawing.Size(300, 26);
            btnWriteTailByte.TabIndex = 8;
            btnWriteTailByte.Text = SteamGifCropper.Properties.Resources.Button_WriteTailByte;
            btnWriteTailByte.UseVisualStyleBackColor = true;
            btnWriteTailByte.Click += btnWriteTailByte_Click;
            // 
            // btnRestoreTailByte
            // 
            btnRestoreTailByte.Location = new System.Drawing.Point(312, 157);
            btnRestoreTailByte.Margin = new System.Windows.Forms.Padding(2);
            btnRestoreTailByte.Name = "btnRestoreTailByte";
            btnRestoreTailByte.Size = new System.Drawing.Size(300, 26);
            btnRestoreTailByte.TabIndex = 9;
            btnRestoreTailByte.Text = SteamGifCropper.Properties.Resources.Button_RestoreTailByte;
            btnRestoreTailByte.UseVisualStyleBackColor = true;
            btnRestoreTailByte.Click += btnRestoreTailByte_Click;
            // 
            // pBarTaskStatus
            // 
            pBarTaskStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            pBarTaskStatus.Location = new System.Drawing.Point(0, 472);
            pBarTaskStatus.Margin = new System.Windows.Forms.Padding(2);
            pBarTaskStatus.Name = "pBarTaskStatus";
            pBarTaskStatus.Size = new System.Drawing.Size(619, 20);
            pBarTaskStatus.TabIndex = 3;
            // 
            // lblStatus
            // 
            lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            lblStatus.Location = new System.Drawing.Point(0, 457);
            lblStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(619, 15);
            lblStatus.TabIndex = 18;
            lblStatus.Text = "Idle.";
            // 
            // btnMergeAndSplit
            // 
            btnMergeAndSplit.Location = new System.Drawing.Point(7, 38);
            btnMergeAndSplit.Margin = new System.Windows.Forms.Padding(2);
            btnMergeAndSplit.Name = "btnMergeAndSplit";
            btnMergeAndSplit.Size = new System.Drawing.Size(300, 52);
            btnMergeAndSplit.TabIndex = 2;
            btnMergeAndSplit.Text = SteamGifCropper.Properties.Resources.Button_MergeAndSplit;
            btnMergeAndSplit.UseVisualStyleBackColor = true;
            btnMergeAndSplit.Click += btnSplitGIFWithReducedPalette_Click;
            // 
            // btnMp4ToGif
            // 
            btnMp4ToGif.Location = new System.Drawing.Point(7, 126);
            btnMp4ToGif.Margin = new System.Windows.Forms.Padding(2);
            btnMp4ToGif.Name = "btnMp4ToGif";
            btnMp4ToGif.Size = new System.Drawing.Size(300, 26);
            btnMp4ToGif.TabIndex = 6;
            btnMp4ToGif.Text = "FFMPEG: Convert MP4 to GIF (with time control)";
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
            panelGifsicle.Controls.Add(numUpDownPalette);
            panelGifsicle.Controls.Add(lblPaletteDesc);
            panelGifsicle.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelGifsicle.Location = new System.Drawing.Point(0, 338);
            panelGifsicle.Margin = new System.Windows.Forms.Padding(2);
            panelGifsicle.Name = "panelGifsicle";
            panelGifsicle.Size = new System.Drawing.Size(619, 119);
            panelGifsicle.TabIndex = 17;
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
            groupDither.Size = new System.Drawing.Size(619, 53);
            groupDither.TabIndex = 10;
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
            numUpDownOptimize.Location = new System.Drawing.Point(305, 26);
            numUpDownOptimize.Margin = new System.Windows.Forms.Padding(2);
            numUpDownOptimize.Maximum = new decimal(new int[] { 3, 0, 0, 0 });
            numUpDownOptimize.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numUpDownOptimize.Name = "numUpDownOptimize";
            numUpDownOptimize.Size = new System.Drawing.Size(35, 23);
            numUpDownOptimize.TabIndex = 7;
            numUpDownOptimize.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // numUpDownPaletteSicle
            // 
            numUpDownPaletteSicle.Location = new System.Drawing.Point(175, 26);
            numUpDownPaletteSicle.Margin = new System.Windows.Forms.Padding(2);
            numUpDownPaletteSicle.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPaletteSicle.Minimum = new decimal(new int[] { 32, 0, 0, 0 });
            numUpDownPaletteSicle.Name = "numUpDownPaletteSicle";
            numUpDownPaletteSicle.Size = new System.Drawing.Size(59, 23);
            numUpDownPaletteSicle.TabIndex = 5;
            numUpDownPaletteSicle.Value = new decimal(new int[] { 256, 0, 0, 0 });
            // 
            // numUpDownLossy
            // 
            numUpDownLossy.Location = new System.Drawing.Point(56, 26);
            numUpDownLossy.Margin = new System.Windows.Forms.Padding(2);
            numUpDownLossy.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            numUpDownLossy.Name = "numUpDownLossy";
            numUpDownLossy.Size = new System.Drawing.Size(46, 23);
            numUpDownLossy.TabIndex = 3;
            numUpDownLossy.Value = new decimal(new int[] { 20, 0, 0, 0 });
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(239, 28);
            label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(62, 15);
            label4.TabIndex = 6;
            label4.Text = "Optimize:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(122, 28);
            label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(49, 15);
            label3.TabIndex = 4;
            label3.Text = "Palette:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(7, 28);
            label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(40, 15);
            label2.TabIndex = 2;
            label2.Text = "Lossy:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(203, 4);
            label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(282, 15);
            label1.TabIndex = 1;
            label1.Text = "Notice: gifsicle.exe must be in the \"System PATH\"";
            // 
            // chkGifsicle
            // 
            chkGifsicle.AutoSize = true;
            chkGifsicle.Location = new System.Drawing.Point(7, 2);
            chkGifsicle.Margin = new System.Windows.Forms.Padding(2);
            chkGifsicle.Name = "chkGifsicle";
            chkGifsicle.Size = new System.Drawing.Size(182, 19);
            chkGifsicle.TabIndex = 0;
            chkGifsicle.Text = SteamGifCropper.Properties.Resources.CheckBox_GifsicleOptimization;
            chkGifsicle.UseVisualStyleBackColor = true;
            // 
            // numUpDownPalette
            // 
            numUpDownPalette.Location = new System.Drawing.Point(562, 39);
            numUpDownPalette.Margin = new System.Windows.Forms.Padding(2);
            numUpDownPalette.Maximum = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPalette.Minimum = new decimal(new int[] { 32, 0, 0, 0 });
            numUpDownPalette.Name = "numUpDownPalette";
            numUpDownPalette.Size = new System.Drawing.Size(46, 23);
            numUpDownPalette.TabIndex = 9;
            numUpDownPalette.Value = new decimal(new int[] { 256, 0, 0, 0 });
            numUpDownPalette.Visible = false;
            // 
            // lblPaletteDesc
            // 
            lblPaletteDesc.AutoSize = true;
            lblPaletteDesc.Location = new System.Drawing.Point(443, 41);
            lblPaletteDesc.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblPaletteDesc.Name = "lblPaletteDesc";
            lblPaletteDesc.Size = new System.Drawing.Size(115, 15);
            lblPaletteDesc.TabIndex = 8;
            lblPaletteDesc.Text = "Number of palette:";
            lblPaletteDesc.Visible = false;
            // 
            // lblFramerate
            // 
            lblFramerate.AutoSize = true;
            lblFramerate.Location = new System.Drawing.Point(11, 295);
            lblFramerate.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblFramerate.Name = "lblFramerate";
            lblFramerate.Size = new System.Drawing.Size(0, 15);
            lblFramerate.TabIndex = 12;
            // 
            // lblFPS
            // 
            lblFPS.AutoSize = true;
            lblFPS.Location = new System.Drawing.Point(310, 295);
            lblFPS.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            lblFPS.Name = "lblFPS";
            lblFPS.Size = new System.Drawing.Size(0, 15);
            lblFPS.TabIndex = 14;
            // 
            // btnMerge2to5GifToOne
            // 
            btnMerge2to5GifToOne.Location = new System.Drawing.Point(313, 7);
            btnMerge2to5GifToOne.Name = "btnMerge2to5GifToOne";
            btnMerge2to5GifToOne.Size = new System.Drawing.Size(300, 26);
            btnMerge2to5GifToOne.TabIndex = 1;
            btnMerge2to5GifToOne.Text = SteamGifCropper.Properties.Resources.Button_MergeGifs;
            btnMerge2to5GifToOne.UseVisualStyleBackColor = true;
            btnMerge2to5GifToOne.Click += btnMerge2to5GifToOne_Click;
            // 
            // chk5GIFMergeFasterPaletteProcess
            // 
            chk5GIFMergeFasterPaletteProcess.AutoSize = true;
            chk5GIFMergeFasterPaletteProcess.Location = new System.Drawing.Point(315, 56);
            chk5GIFMergeFasterPaletteProcess.Name = "chk5GIFMergeFasterPaletteProcess";
            chk5GIFMergeFasterPaletteProcess.Size = new System.Drawing.Size(243, 19);
            chk5GIFMergeFasterPaletteProcess.TabIndex = 3;
            chk5GIFMergeFasterPaletteProcess.Text = SteamGifCropper.Properties.Resources.CheckBox_FasterPalette;
            chk5GIFMergeFasterPaletteProcess.UseVisualStyleBackColor = true;
            // 
            // btnReverseGIF
            // 
            btnReverseGIF.Location = new System.Drawing.Point(313, 95);
            btnReverseGIF.Name = "btnReverseGIF";
            btnReverseGIF.Size = new System.Drawing.Size(300, 26);
            btnReverseGIF.TabIndex = 7;
            btnReverseGIF.Text = SteamGifCropper.Properties.Resources.Button_ReverseGif;
            btnReverseGIF.UseVisualStyleBackColor = true;
            btnReverseGIF.Click += btnReverseGIF_Click;
            // 
            // btnLanguageChange
            // 
            btnLanguageChange.Location = new System.Drawing.Point(548, 276);
            btnLanguageChange.Name = "btnLanguageChange";
            btnLanguageChange.Size = new System.Drawing.Size(64, 26);
            btnLanguageChange.TabIndex = 16;
            btnLanguageChange.Text = "A⇆文";
            btnLanguageChange.UseVisualStyleBackColor = true;
            btnLanguageChange.Click += btnLanguageChange_Click;
            // 
            // conMenuLangSwitch
            // 
            conMenuLangSwitch.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripLangEnglish, toolStripLangTradChinese, toolStripLangJapanese });
            conMenuLangSwitch.Name = "conMenuLangSwitch";
            conMenuLangSwitch.Size = new System.Drawing.Size(123, 70);
            // 
            // toolStripLangEnglish
            // 
            toolStripLangEnglish.Name = "toolStripLangEnglish";
            toolStripLangEnglish.Size = new System.Drawing.Size(122, 22);
            toolStripLangEnglish.Text = "English";
            toolStripLangEnglish.Click += toolStripLangEnglish_Click;
            // 
            // toolStripLangTradChinese
            // 
            toolStripLangTradChinese.Name = "toolStripLangTradChinese";
            toolStripLangTradChinese.Size = new System.Drawing.Size(122, 22);
            toolStripLangTradChinese.Text = "繁體中文";
            toolStripLangTradChinese.Click += toolStripLangTradChinese_Click;
            // 
            // toolStripLangJapanese
            // 
            toolStripLangJapanese.Name = "toolStripLangJapanese";
            toolStripLangJapanese.Size = new System.Drawing.Size(122, 22);
            toolStripLangJapanese.Text = "日本語";
            toolStripLangJapanese.Click += toolStripLangJapanese_Click;
            // 
            // btnScrollStaticImage
            // 
            btnScrollStaticImage.Location = new System.Drawing.Point(312, 126);
            btnScrollStaticImage.Name = "btnScrollStaticImage";
            btnScrollStaticImage.Size = new System.Drawing.Size(300, 26);
            btnScrollStaticImage.TabIndex = 5;
            btnScrollStaticImage.Text = SteamGifCropper.Properties.Resources.Button_ScrollStaticImage;
            btnScrollStaticImage.UseVisualStyleBackColor = true;
            btnScrollStaticImage.Click += btnScrollStaticImage_Click;
            // 
            // btnScrollAnimatedGif
            // 
            btnScrollAnimatedGif.Location = new System.Drawing.Point(7, 125);
            btnScrollAnimatedGif.Name = "btnScrollAnimatedGif";
            btnScrollAnimatedGif.Size = new System.Drawing.Size(300, 26);
            btnScrollAnimatedGif.TabIndex = 6;
            btnScrollAnimatedGif.Text = SteamGifCropper.Properties.Resources.Button_ScrollAnimatedGif;
            btnScrollAnimatedGif.UseVisualStyleBackColor = true;
            btnScrollAnimatedGif.Click += btnScrollAnimatedGif_Click;
            // 
            // btnOverlayGIF
            // 
            btnOverlayGIF.Location = new System.Drawing.Point(7, 187);
            btnOverlayGIF.Name = "btnOverlayGIF";
            btnOverlayGIF.Size = new System.Drawing.Size(300, 26);
            btnOverlayGIF.TabIndex = 10;
            btnOverlayGIF.Text = SteamGifCropper.Properties.Resources.Button_OverlayGif;
            btnOverlayGIF.UseVisualStyleBackColor = true;
            btnOverlayGIF.Click += btnOverlayGIF_Click;
            // 
            // btnResizeNfpsGIF
            // 
            btnResizeNfpsGIF.Location = new System.Drawing.Point(313, 187);
            btnResizeNfpsGIF.Name = "btnResizeNfpsGIF";
            btnResizeNfpsGIF.Size = new System.Drawing.Size(300, 26);
            btnResizeNfpsGIF.TabIndex = 11;
            btnResizeNfpsGIF.Text = SteamGifCropper.Properties.Resources.Button_ResizeNfpsGif;
            btnResizeNfpsGIF.UseVisualStyleBackColor = true;
            btnResizeNfpsGIF.Click += btnResizeNfpsGIF_Click;
            // 
            // lblResourceLimitDesc
            // 
            lblResourceLimitDesc.AutoSize = true;
            lblResourceLimitDesc.Location = new System.Drawing.Point(7, 269);
            lblResourceLimitDesc.Name = "lblResourceLimitDesc";
            lblResourceLimitDesc.Size = new System.Drawing.Size(234, 15);
            lblResourceLimitDesc.TabIndex = 15;
            lblResourceLimitDesc.Text = "Resource limit: mem: ?? MB / disk: ?? MB";
            // 
            // numUpDownFramerate
            // 
            numUpDownFramerate.Location = new System.Drawing.Point(255, 293);
            numUpDownFramerate.Margin = new System.Windows.Forms.Padding(2);
            numUpDownFramerate.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            numUpDownFramerate.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            numUpDownFramerate.Name = "numUpDownFramerate";
            numUpDownFramerate.Size = new System.Drawing.Size(46, 23);
            numUpDownFramerate.TabIndex = 13;
            numUpDownFramerate.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // btnConcatenateGifs
            // 
            btnConcatenateGifs.Location = new System.Drawing.Point(7, 219);
            btnConcatenateGifs.Name = "btnConcatenateGifs";
            btnConcatenateGifs.Size = new System.Drawing.Size(300, 26);
            btnConcatenateGifs.TabIndex = 17;
            btnConcatenateGifs.Text = SteamGifCropper.Properties.Resources.GTMainForm_ConcatenateGifs;
            btnConcatenateGifs.UseVisualStyleBackColor = true;
            btnConcatenateGifs.Click += btnConcatenateGifs_Click;
            // 
            // GifToolMainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            ClientSize = new System.Drawing.Size(619, 492);
            Controls.Add(lblResourceLimitDesc);
            Controls.Add(btnResizeNfpsGIF);
            Controls.Add(btnOverlayGIF);
            Controls.Add(btnScrollStaticImage);
            Controls.Add(btnScrollAnimatedGif);
            Controls.Add(btnLanguageChange);
            Controls.Add(btnReverseGIF);
            Controls.Add(chk5GIFMergeFasterPaletteProcess);
            Controls.Add(panelGifsicle);
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
            Controls.Add(btnMerge2to5GifToOne);
            Controls.Add(btnConcatenateGifs);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(2);
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
            ((System.ComponentModel.ISupportInitialize)numUpDownPalette).EndInit();
            conMenuLangSwitch.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numUpDownFramerate).EndInit();
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
        private System.Windows.Forms.Label lblFramerate;
        private System.Windows.Forms.Label lblFPS;
        private System.Windows.Forms.Button btnMerge2to5GifToOne;
        public System.Windows.Forms.CheckBox chk5GIFMergeFasterPaletteProcess;
        private System.Windows.Forms.Button btnReverseGIF;
        private System.Windows.Forms.Button btnLanguageChange;
        private System.Windows.Forms.ContextMenuStrip conMenuLangSwitch;
        private System.Windows.Forms.ToolStripMenuItem toolStripLangEnglish;
        private System.Windows.Forms.ToolStripMenuItem toolStripLangTradChinese;
        private System.Windows.Forms.ToolStripMenuItem toolStripLangJapanese;
        private System.Windows.Forms.Button btnScrollStaticImage;
        private System.Windows.Forms.Button btnScrollAnimatedGif;
        private System.Windows.Forms.Button btnOverlayGIF;
        private System.Windows.Forms.Button btnResizeNfpsGIF;
        private System.Windows.Forms.Label lblResourceLimitDesc;
        public System.Windows.Forms.NumericUpDown numUpDownFramerate;
        private System.Windows.Forms.Button btnConcatenateGifs;
    }
}