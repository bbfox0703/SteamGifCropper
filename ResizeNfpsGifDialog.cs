using System;
using System.IO;
using System.Windows.Forms;
using FFMpegCore;
using FFMpegCore.Pipes;
using ImageMagick;

namespace GifProcessorApp
{
    public partial class ResizeNfpsGifDialog : Form
    {
        public string InputGifPath => txtGifPath.Text;
        public int NewWidth => (int)numWidth.Value;
        public int NewHeight => (int)numHeight.Value;
        public int NewFps => (int)numFps.Value;

        private double _aspectRatio = 1.0;
        private bool _suppressEvents;

        public ResizeNfpsGifDialog()
        {
            InitializeComponent();
            WindowsThemeManager.ApplyThemeToControl(this, WindowsThemeManager.IsDarkModeEnabled());
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                Title = SteamGifCropper.Properties.Resources.FileDialog_SelectGifResize
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtGifPath.Text = dialog.FileName;
                try
                {
                    using var collection = new MagickImageCollection(dialog.FileName);
                    int width = (int)collection[0].Width;
                    int height = (int)collection[0].Height;
                    double avgDelay = 0;
                    if (collection.Count > 0)
                        foreach (var img in collection)
                            avgDelay += img.AnimationDelay;
                    avgDelay = collection.Count > 0 ? avgDelay / collection.Count : 0;
                    double fps = avgDelay > 0 ? 100.0 / avgDelay : 0;

                    lblOriginal.Text = $"{width}×{height}, {fps:0.##} fps";

                    _aspectRatio = height != 0 ? (double)width / height : 1.0;

                    _suppressEvents = true;
                    numWidth.Value = width;
                    numHeight.Value = height;
                    numFps.Value = fps > 0 ? (decimal)Math.Round(fps) : 15;
                    _suppressEvents = false;
                }
                catch
                {
                    // ignore errors
                }
            }
        }

        private void NumWidth_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            if (chkLockRatio.Checked)
            {
                _suppressEvents = true;
                numHeight.Value = Math.Max(1, (int)Math.Round(numWidth.Value / (decimal)_aspectRatio));
                _suppressEvents = false;
            }
        }

        private void NumHeight_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            if (chkLockRatio.Checked)
            {
                _suppressEvents = true;
                numWidth.Value = Math.Max(1, (int)Math.Round(numHeight.Value * (decimal)_aspectRatio));
                _suppressEvents = false;
            }
        }

        private void ChkLockRatio_CheckedChanged(object sender, EventArgs e)
        {
            if (chkLockRatio.Checked)
            {
                _aspectRatio = numHeight.Value > 0 ? (double)numWidth.Value / (double)numHeight.Value : 1.0;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (!File.Exists(InputGifPath))
            {
                WindowsThemeManager.ShowThemeAwareMessageBox(this, "Please select a GIF file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = SteamGifCropper.Properties.Resources.FileDialog_GifFilter,
                FileName = Path.GetFileNameWithoutExtension(InputGifPath) + "_resized.gif"
            };
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    ConvertWithFfmpeg(InputGifPath, saveDialog.FileName, NewWidth, NewHeight, NewFps);
                    WindowsThemeManager.ShowThemeAwareMessageBox(this, SteamGifCropper.Properties.Resources.Message_ResizeComplete, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    WindowsThemeManager.ShowThemeAwareMessageBox(this, string.Format(SteamGifCropper.Properties.Resources.Error_ResizeFailed, ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void ConvertWithFfmpeg(string input, string output, int width, int height, int fps)
        {
            using var inStream = File.OpenRead(input);
            FFMpegArguments
                .FromPipeInput(new StreamPipeSource(inStream))
                .OutputToFile(output, true, opt => opt
                    .ForceFormat("gif")
                    .WithVideoFilters(f => f.Scale(width, height))
                    .WithFramerate(fps)
                    .WithCustomArgument("-loop 0"))
                .ProcessSynchronously();
        }

        private static void ConvertWithMagick(string input, string output, int width, int height, int fps)
        {
            using var collection = new MagickImageCollection(input);
            foreach (var frame in collection)
            {
                frame.Resize((uint)width, (uint)height);
                frame.AnimationDelay = fps > 0 ? (uint)Math.Round(100.0 / fps) : frame.AnimationDelay;
            }
            collection.Write(output);
        }
    }
}
