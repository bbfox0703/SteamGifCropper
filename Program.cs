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
                        // 確認所有幀的寬度是否一致且為 766
                        foreach (var frame in collection)
                        {
                            if (frame.Width != 766)
                            {
                                MessageBox.Show("The GIF width is not 766 pixels. Please provide a valid GIF.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
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
            var ranges = new (int Start, int End)[]
            {
        (0, 149),   // First part
        (153, 303), // Second part
        (307, 457), // Third part
        (461, 611), // Fourth part
        (615, 765)  // Fifth part
            };

            using (var collection = new MagickImageCollection(inputFilePath))
            {
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

                            // 創建新的圖像，寬度與範圍一致，高度為原高度 + 100px
                            MagickImage newImage = new MagickImage(MagickColors.Transparent, (uint)copyWidth, frame.Height + 100);

                            // 複製原內容到新圖像
                            newImage.Composite(frame, -ranges[i].Start, 0, CompositeOperator.Copy);

                            // 設置透明色為新增區域的 (0, 新高度 - 1) 像素值
                            newImage.Extent(new MagickGeometry((uint)copyWidth, frame.Height + 100), Gravity.North);
                            using (var pixels = newImage.GetPixels())
                            {
                                var transparentPixel = pixels.GetPixel(0, (int)newImage.Height - 1); // 新增高度的最後一行
                                var transparentColor = transparentPixel.ToColor();
                                newImage.Transparent(transparentColor); // 設定透明色
                            }

                            newImage.AnimationDelay = originalDelay; // 恢復幀延遲
                            partCollection.Add(newImage);

                            // 更新進度條
                            currentStep++;
                            progressBar.Value = (int)((double)currentStep / totalSteps * 100);
                            Application.DoEvents();
                        }

                        string outputFileName = $"{Path.GetFileNameWithoutExtension(inputFilePath)}_Part{i + 1}.gif";
                        string outputDir = Path.GetDirectoryName(inputFilePath);
                        string outputPath = Path.Combine(outputDir, outputFileName);
                        partCollection.Write(outputPath);

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
