using ImageMagick;
using SteamGifCropper;
using SteamGifCropper.Properties;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public static partial class GifProcessor
    {
        #region Rain Overlay (下雨) Methods

        // Overlay a translucent rain layer (slanted streaks, optional wind + a "rain stops" fade-out) on
        // an image / GIF. Output is a single full-width 766px GIF (no auto-split), chainable; split with
        // the main "Split GIF" button.
        public static async Task RainStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new RainDialog(false);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunRain(mainForm, dialog.BuildSettings(false));
        }

        public static async Task RainGif(GifToolMainForm mainForm)
        {
            using var dialog = new RainDialog(true);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunRain(mainForm, dialog.BuildSettings(true));
        }

        private static async Task RunRain(GifToolMainForm mainForm, RainSettings settings)
        {
            mainForm.Enabled = false;
            SetProgressRange(mainForm, 0, 100);
            SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
            SetProgressVisible(mainForm, true);

            bool canceled = false;
            try
            {
                await Task.Run(() =>
                {
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RainBuilding);
                    using var source = new MagickImageCollection(settings.InputFilePath);
                    source.Coalesce();

                    // Auto-resize to 766px wide when not already a supported width (rain density scales to
                    // this canvas) — unless the user opted to keep the original size.
                    uint width = source[0].Width;
                    if (!settings.KeepOriginalSize && !IsValidCanvasWidth(width))
                    {
                        foreach (var frame in source)
                        {
                            frame.ResetPage();
                            frame.Resize(SupportedWidth1, 0);
                        }
                        width = source[0].Width;
                    }

                    // Warn before running at a large native size (rain-over-playback keeps the GIF length;
                    // frozen / static add the rain frames).
                    int outFrames = (settings.IsGif && settings.PlayGifDuringRain)
                        ? source.Count
                        : (int)Math.Round(Math.Max(0.1, settings.DurationSeconds) * Math.Max(1, settings.Fps)) + (settings.IsGif ? source.Count : 0);
                    if (!ConfirmLargeCanvas(mainForm, Math.Max(source.Count, outFrames), width, source[0].Height))
                    {
                        canceled = true;
                        return;
                    }

                    using var animation = BuildRainAnimation(mainForm, source, settings);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Saving);
                    animation.Optimize();
                    animation.Write(settings.OutputFilePath);
                });

                if (!canceled)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, 100, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Done);
                    WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                        SteamGifCropper.Properties.Resources.Message_ProcessingComplete,
                        SteamGifCropper.Properties.Resources.Title_Success,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Error);
                WindowsThemeManager.ShowThemeAwareMessageBox(mainForm,
                    string.Format(SteamGifCropper.Properties.Resources.Error_Occurred, ex.Message),
                    SteamGifCropper.Properties.Resources.Title_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                mainForm.Enabled = true;
                SetProgressBar(mainForm.pBarTaskStatus, 0, 100);
                SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_Idle);
            }
        }

        // Dispatcher. GIF + "rain over playback": the GIF keeps playing its full length while the rain is
        // overlaid on the chosen [start, duration] window. Otherwise (static image, or GIF + "freeze on
        // frame 0"): rain over a single frozen frame, optionally followed by the full GIF.
        private static MagickImageCollection BuildRainAnimation(GifToolMainForm mainForm,
            MagickImageCollection source, RainSettings settings)
        {
            int srcTicks = (int)source[0].AnimationTicksPerSecond;
            if (srcTicks <= 0) srcTicks = 100;
            int w = (int)source[0].Width;
            int h = (int)source[0].Height;
            RainParams rain = settings.ToParams(w, h);

            if (settings.IsGif && settings.PlayGifDuringRain)
            {
                return BuildRainPlayAlong(mainForm, source, settings, rain, srcTicks);
            }

            return BuildRainFrozenThenPlay(mainForm, source, settings, rain, srcTicks);
        }

        // Output length == source GIF length, native frame timing preserved. Rain is drawn onto each live
        // frame whose native time falls in the [EffectStart, +Duration) window; frames outside the window
        // pass through as the plain GIF. The streaks move with absolute time, so the rain is continuous;
        // the optional fade-out empties the rain over the last FadeOutSeconds of the window.
        private static MagickImageCollection BuildRainPlayAlong(GifToolMainForm mainForm,
            MagickImageCollection source, RainSettings settings, RainParams rain, int srcTicks)
        {
            int n = source.Count;
            double duration = settings.DurationSeconds;
            if (duration < 0.1) duration = 0.1;
            int w = (int)source[0].Width;
            int h = (int)source[0].Height;

            double[] startSec = new double[n];
            double acc = 0.0;
            for (int i = 0; i < n; i++)
            {
                startSec[i] = acc;
                acc += (double)source[i].AnimationDelay / srcTicks;
            }
            double gifSeconds = acc;

            var (winStart, winDur) = GifEffectWindow.Clamp(settings.EffectStartSeconds, duration, gifSeconds);

            var result = new MagickImageCollection();
            int built = 0;
            for (int i = 0; i < n; i++)
            {
                double te = startSec[i] - winStart; // time within the effect window
                MagickImage frame;
                if (te >= 0.0 && te < winDur && RainField.AnyRainActive(te, winDur, settings.FadeOut, settings.FadeOutSeconds))
                {
                    double alpha = RainField.FadeAlpha(te, winDur, settings.FadeOut, settings.FadeOutSeconds);
                    RainStreak[] streaks = RainField.Streaks(startSec[i], w, h, rain);
                    frame = RainRenderer.RenderFrame(source[i], streaks, alpha);
                }
                else
                {
                    frame = (MagickImage)source[i].Clone();
                }
                frame.ResetPage();
                frame.AnimationDelay = source[i].AnimationDelay;
                frame.AnimationTicksPerSecond = srcTicks;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);

                if (++built % 5 == 0 || built == n)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / n, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RainBuilding);
                }
            }

            return result;
        }

        // Rain over a frozen frame 0 (or the static image) for `Duration` seconds at the chosen FPS. For a
        // GIF this is followed by the full source GIF played at its native timing (output length ==
        // Duration + GIF length); for a static image there is no play phase (output length == Duration).
        private static MagickImageCollection BuildRainFrozenThenPlay(GifToolMainForm mainForm,
            MagickImageCollection source, RainSettings settings, RainParams rain, int srcTicks)
        {
            int fps = Math.Max(1, settings.Fps);
            int delay = Math.Max(1, (int)Math.Round(100.0 / fps));

            double duration = settings.DurationSeconds;
            if (duration < 0.1) duration = 0.1;
            int rainFrames = Math.Max(1, (int)Math.Round(duration * fps));
            int playFrames = settings.IsGif ? source.Count : 0;
            int totalFrames = rainFrames + playFrames;
            int w = (int)source[0].Width;
            int h = (int)source[0].Height;

            var result = new MagickImageCollection();
            int built = 0;

            // Phase 1: rain over the frozen first frame.
            for (int f = 0; f < rainFrames; f++)
            {
                double t = (double)f / fps;
                double alpha = RainField.FadeAlpha(t, duration, settings.FadeOut, settings.FadeOutSeconds);
                RainStreak[] streaks = RainField.Streaks(t, w, h, rain);
                var frame = RainRenderer.RenderFrame(source[0], streaks, alpha);
                frame.AnimationDelay = (uint)delay;
                frame.AnimationTicksPerSecond = 100;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);

                if (++built % 5 == 0 || built == totalFrames)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RainBuilding);
                }
            }

            // Phase 2 (GIF only): play the full source GIF at native timing after the rain frames.
            if (settings.IsGif)
            {
                foreach (var srcFrame in source)
                {
                    var play = (MagickImage)srcFrame.Clone();
                    play.ResetPage();
                    play.AnimationTicksPerSecond = srcTicks;
                    play.GifDisposeMethod = GifDisposeMethod.Background;
                    result.Add(play);

                    if (++built % 5 == 0 || built == totalFrames)
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RainBuilding);
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
