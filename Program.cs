using System;
using System.IO;
using System.Windows.Forms;
using ImageMagick;

namespace GifProcessor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "GIF Files (*.gif)|*.gif",
                Title = "Select a GIF file to process"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string inputFilePath = openFileDialog.FileName;

                try
                {
                    using (var collection = new MagickImageCollection(inputFilePath))
                    {
                        // 獲取 GIF 整體畫布尺寸
                        uint canvasWidth = collection[0].Page.Width;
                        uint canvasHeight = collection[0].Page.Height;
                        // 判斷畫布寬度是否為 766
                        if (canvasWidth != 766)
                        {
                            string message = $"The GIF canvas width is not 766 pixels. Detected canvas dimensions: Width={canvasWidth}, Height={canvasHeight}. Please provide a valid GIF.";
                            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // 初始化進度條
                        ProgressBar progressBar = new ProgressBar
                        {
                            Minimum = 0,
                            Maximum = 100,
                            Value = 0,
                            Dock = DockStyle.None,
                            Width = 300,
                            Height = 30
                        };

                        Form progressForm = new Form
                        {
                            Text = "Processing...",
                            Width = 350,
                            Height = 80,
                            StartPosition = FormStartPosition.CenterScreen,
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            MaximizeBox = false,
                            MinimizeBox = false
                        };

                        progressBar.Left = (progressForm.ClientSize.Width - progressBar.Width) / 2;
                        progressBar.Top = (progressForm.ClientSize.Height - progressBar.Height) / 2;
                        progressForm.Controls.Add(progressBar);
                        progressForm.Shown += (s, e) => progressForm.Activate();
                        progressForm.Show();

                        // 分割 GIF
                        SplitGifIntoParts(inputFilePath, progressBar);

                        progressForm.Close();
                        MessageBox.Show("GIF processing and splitting completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        static void SplitGifIntoParts(string inputFilePath, ProgressBar progressBar)
        {
            using (var collection = new MagickImageCollection(inputFilePath))
            {
                int totalWidth = (int)collection[0].Page.Width; // 假設所有幀的寬度一致

                // 定義分割範圍
                var ranges = new (int Start, int End)[]
                {
                    (0, 149),    // 第一部分
                    (154, 303),  // 第二部分
                    (308, 457),  // 第三部分
                    (462, 611),  // 第四部分
                    (616, totalWidth - 1) // 第五部分，動態計算最後部分
                };

                int totalSteps = collection.Count * ranges.Length;
                int currentStep = 0;

                for (int i = 0; i < ranges.Length; i++)
                {
                    using (var partCollection = new MagickImageCollection())
                    {
                        foreach (var frame in collection)
                        {
                            uint originalDelay = frame.AnimationDelay; // 保留原始幀延遲
                            int copyWidth = ranges[i].End - ranges[i].Start + 1;

                            // 建立新的圖像，寬度與範圍一致，高度與原幀一致
                            MagickImage newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, frame.Page.Height);

                            // 複製原內容到新圖像
                            newImage.Composite(frame, -ranges[i].Start, 0, CompositeOperator.Copy);

                            // 保留延遲
                            newImage.AnimationDelay = originalDelay;
                            partCollection.Add(newImage);

                            // 更新進度條
                            currentStep++;
                            progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                            Application.DoEvents();
                        }

                        // 儲存分割出的 GIF 
                        string outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFileName);
                        partCollection.Write(outputPath);
                        // Modify the saved GIF file
                        ModifyGifFile(outputPath, (int)collection[0].Height - 100);
                    }
                }
            }
        }

        static void ModifyGifFile(string filePath, int adjustedHeight)
        {
            byte[] fileData = File.ReadAllBytes(filePath);

            // Set the last byte to 0x21
            fileData[fileData.Length - 1] = 0x21;

            // Write the adjusted height at byte 8 (2 bytes, little-endian)
            ushort heightValue = (ushort)adjustedHeight;
            fileData[8] = (byte)(heightValue & 0xFF);        // Lower byte
            fileData[9] = (byte)((heightValue >> 8) & 0xFF); // Higher byte

            File.WriteAllBytes(filePath, fileData);
        }
    }
}
