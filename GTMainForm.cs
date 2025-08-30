using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GifProcessorApp
{
    public partial class GifToolMainForm : Form
    {
        public int DitherMethod { get; private set; } = 0;
        private bool _isDarkMode;
        
        public GifToolMainForm()
        {
            try
            {
                InitializeComponent();
                
                // Initialize theme
                _isDarkMode = WindowsThemeManager.IsDarkModeEnabled();
                ApplyCurrentTheme();
                
                // Set initial state
                lblStatus.Text = SteamGifCropper.Properties.Resources.Status_Ready;
                pBarTaskStatus.Visible = false;
                
                // Ensure proper form state
                this.WindowState = FormWindowState.Normal;
                this.StartPosition = FormStartPosition.CenterScreen;
                
                // Register for theme changes
                SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
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
            await ExecuteWithErrorHandling(() =>
            {
                using (var dialog = new MergeGifsDialog())
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        int targetFramerate = (int)numUpDownFramerate.Value;
                        GifProcessor.MergeMultipleGifs(dialog.SelectedFilePaths, dialog.OutputFilePath, this, targetFramerate);
                    }
                }
                return Task.CompletedTask;
            }, "GIF merge");
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
    }
}
