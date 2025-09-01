using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using ImageMagick;

namespace GifProcessorApp
{
    public partial class GifToolMainForm : Form
    {
        public int DitherMethod { get; private set; } = 0;
        private bool _isDarkMode;
        private readonly bool _useFfmpegForResize;

        public GifToolMainForm()
        {
            try
            {
                InitializeComponent();
                _useFfmpegForResize = CheckFfmpegAvailable();
                UpdateUIText();

                // Initialize theme
                _isDarkMode = WindowsThemeManager.IsDarkModeEnabled();
                ApplyCurrentTheme();

                // Set initial state
                lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready;
                pBarTaskStatus.Visible = false;
                label1.Text = SteamGifCropper.Properties.Resources.Label_GifsicleNotice;

                // Ensure proper form state
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterScreen;

                // Register for theme changes
                SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

                UpdateResourceLimitLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(SteamGifCropper.Properties.Resources.Error_InitializationFailed, ex.Message),
                                SteamGifCropper.Properties.Resources.Title_InitializationError,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                throw;
            }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.VisualStyle)
            {
                // Check if theme changed
                bool newDarkMode = WindowsThemeManager.IsDarkModeEnabled();
                if (newDarkMode != _isDarkMode)
                {
                    _isDarkMode = newDarkMode;
                    this.BeginInvoke(new Action(ApplyCurrentTheme));
                }
            }
        }

        private void ApplyCurrentTheme()
        {
            try
            {
                WindowsThemeManager.ApplyThemeToControl(this, _isDarkMode);
                WindowsThemeManager.ApplyThemeToControl(conMenuLangSwitch, _isDarkMode);
                WindowsThemeManager.ApplyThemeToControl(btnResizeNfpsGIF, _isDarkMode);
                this.Refresh();
            }
            catch (Exception ex)
            {
                // Silently handle theme application errors
                System.Diagnostics.Debug.WriteLine($"Theme application error: {ex.Message}");
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            base.OnHandleDestroyed(e);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            Bounds = e.SuggestedRectangle;
            AutoScaleDimensions = new SizeF(e.DeviceDpiNew, e.DeviceDpiNew);
            PerformLayout();
        }

        private async Task ExecuteWithErrorHandling(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"An error occurred during {operationName}: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool CheckFfmpegAvailable()
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(1000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async void btnSplitGif_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(() =>
            {
                GifProcessor.StartProcessing(this);
                return Task.CompletedTask;
            }, "GIF splitting");
        }

        private async void btnResizeGif766_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(() =>
            {
                GifProcessor.ResizeGifTo766(this);
                return Task.CompletedTask;
            }, "GIF resizing");
        }

        private async void btnWriteTailByte_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(() =>
            {
                GifProcessor.WriteTailByteForMultipleGifs(this);
                return Task.CompletedTask;
            }, "tail byte modification");
        }

        private async void btnRestoreTailByte_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(() =>
            {
                GifProcessor.RestoreTailByteForMultipleGifs(this);
                return Task.CompletedTask;
            }, "tail byte restoration");
        }

        private async void btnSplitGIFWithReducedPalette_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(() =>
            {
                GifProcessor.SplitGifWithReducedPalette(this);
                return Task.CompletedTask;
            }, "palette reduction and splitting");
        }

        private async void btnMp4ToGif_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(async () => await GifProcessor.ConvertMp4ToGif(this), "MP4 to GIF conversion");
        }

        private async void btnMerge2to5GifToOne_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(async () =>
            {
                using (var dialog = new MergeGifsDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        int targetFramerate = (int)numUpDownFramerate.Value;
                        bool useFastPalette = dialog.chkGIFMergeFasterPaletteProcess.Checked;
                        await GifProcessor.MergeMultipleGifs(dialog.SelectedFilePaths, dialog.OutputFilePath, this, targetFramerate, useFastPalette);
                    }
                }
            }, "GIF merge");
        }

        private async void btnReverseGIF_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(async () => await GifProcessor.ReverseGif(this), "GIF reversal");
        }

        private async void btnScrollStaticImage_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(async () => await GifProcessor.ScrollStaticImage(this), "static image scroll");
        }

        private async void btnOverlayGIF_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(() =>
            {
                GifProcessor.OverlayGif(this);
                return Task.CompletedTask;
            }, "GIF overlay");
        }

        private async void btnResizeNfpsGIF_Click(object sender, EventArgs e)
        {
            await ExecuteWithErrorHandling(() =>
            {
                using var dialog = new ResizeNfpsGifDialog();
                dialog.ShowDialog(this);
                return Task.CompletedTask;
            }, "GIF resize / re-FPS");
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void radioBtnDNone_Click(object sender, EventArgs e) => DitherMethod = 0;
        private void radioBtnDro64_Click(object sender, EventArgs e) => DitherMethod = 1;
        private void radioBtnDo8_Click(object sender, EventArgs e) => DitherMethod = 2;
        private void radioBtnDDefault_Click(object sender, EventArgs e) => DitherMethod = 3;

        public void ForceThemeRefresh()
        {
            _isDarkMode = WindowsThemeManager.IsDarkModeEnabled();
            ApplyCurrentTheme();
        }

        private void ToggleThemeForTesting()
        {
            // This is just for testing - normally theme comes from Windows settings
            _isDarkMode = !_isDarkMode;
            ApplyCurrentTheme();
        }

        /// <summary>
        /// Switch language for testing purposes
        /// </summary>
        /// <param name="culture">Culture code: "en", "zh-TW", or "ja"</param>
        public void SwitchLanguage(string culture)
        {
            try
            {
                Program.InitializeLocalization(culture);

                // Update the form text and controls
                UpdateUIText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to switch language: {ex.Message}", "Language Switch Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Update all UI text elements with current culture
        /// </summary>
        private void UpdateUIText()
        {
            try
            {
                var resources = new ComponentResourceManager(typeof(GifToolMainForm));

                ApplyResourcesRecursively(this, resources);

                // Update context menu items if resource entries exist
                resources.ApplyResources(conMenuLangSwitch, conMenuLangSwitch.Name);
                ApplyToolStripItemsRecursively(conMenuLangSwitch.Items, resources);

                // Reassign control captions from resources so they update with the culture
                btnSplitGif.Text = SteamGifCropper.Properties.Resources.Button_SplitGif;
                btnResizeGif766.Text = SteamGifCropper.Properties.Resources.Button_ResizeGif;
                btnWriteTailByte.Text = SteamGifCropper.Properties.Resources.Button_WriteTailByte;
                btnRestoreTailByte.Text = SteamGifCropper.Properties.Resources.Button_RestoreTailByte;
                btnMergeAndSplit.Text = SteamGifCropper.Properties.Resources.Button_MergeAndSplit;
                btnMp4ToGif.Text = SteamGifCropper.Properties.Resources.Button_Mp4ToGif;
                radioBtnDDefault.Text = SteamGifCropper.Properties.Resources.Radio_Default;
                radioBtnDo8.Text = SteamGifCropper.Properties.Resources.Radio_o8;
                radioBtnDro64.Text = SteamGifCropper.Properties.Resources.Radio_ro64;
                radioBtnDNone.Text = SteamGifCropper.Properties.Resources.Radio_None;
                chkGifsicle.Text = SteamGifCropper.Properties.Resources.CheckBox_GifsicleOptimization;
                btnMerge2to5GifToOne.Text = SteamGifCropper.Properties.Resources.Button_MergeGifs;
                chk5GIFMergeFasterPaletteProcess.Text = SteamGifCropper.Properties.Resources.CheckBox_FasterPalette;
                btnReverseGIF.Text = SteamGifCropper.Properties.Resources.Button_ReverseGif;
                btnScrollStaticImage.Text = SteamGifCropper.Properties.Resources.Button_ScrollStaticImage;
                btnOverlayGIF.Text = SteamGifCropper.Properties.Resources.Button_OverlayGif;
                btnResizeNfpsGIF.Text = SteamGifCropper.Properties.Resources.Button_ResizeNfpsGif;
                if (_useFfmpegForResize)
                {
                    btnResizeNfpsGIF.Text = "FFMPEG: " + btnResizeNfpsGIF.Text;
                }
                label1.Text = SteamGifCropper.Properties.Resources.Label_GifsicleNotice;


                this.Text = "Steam GIF Cropper"; // Keep main title in English

                if (lblStatus.Text == "Ready" || lblStatus.Text == "就緒" || lblStatus.Text == "準備完了")
                {
                    lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready;
                }

                UpdateResourceLimitLabel();

                this.Invalidate(true);
                this.Update();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update UI text: {ex.Message}");
            }
        }

        private void UpdateResourceLimitLabel()
        {
            try
            {
                ulong memMb = ResourceLimits.Memory / (1024UL * 1024UL);
                ulong diskMb = ResourceLimits.Disk / (1024UL * 1024UL);
                lblResourceLimitDesc.Text = string.Format(SteamGifCropper.Properties.Resources.Label_ResourceLimitDesc, memMb, diskMb);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update resource limit label: {ex.Message}");
            }
        }

        private void ApplyResourcesRecursively(Control control, ComponentResourceManager resources)
        {
            resources.ApplyResources(control, control.Name);
            foreach (Control child in control.Controls)
            {
                ApplyResourcesRecursively(child, resources);
            }
        }

        private void ApplyToolStripItemsRecursively(ToolStripItemCollection items, ComponentResourceManager resources)
        {
            foreach (ToolStripItem item in items)
            {
                resources.ApplyResources(item, item.Name);
                if (item is ToolStripDropDownItem dropDownItem && dropDownItem.HasDropDownItems)
                {
                    ApplyToolStripItemsRecursively(dropDownItem.DropDownItems, resources);
                }
            }
        }

        private void btnLanguageChange_Click(object sender, EventArgs e)
        {
            conMenuLangSwitch.Show(btnLanguageChange, new Point(0, btnLanguageChange.Height));
        }

        private void toolStripLangEnglish_Click(object sender, EventArgs e)
        {
            SwitchLanguage("en");
            conMenuLangSwitch.Close();
        }

        private void toolStripLangTradChinese_Click(object sender, EventArgs e)
        {
            SwitchLanguage("zh-TW");
            conMenuLangSwitch.Close();
        }

        private void toolStripLangJapanese_Click(object sender, EventArgs e)
        {
            SwitchLanguage("ja");
            conMenuLangSwitch.Close();
        }
    }
}
