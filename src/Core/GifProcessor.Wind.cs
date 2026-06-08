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
        #region Wind Sway (微/強風吹襲) Methods

        // Sway an image / GIF with travelling wind waves (1-3 gusts, or a single Nuclear blast that
        // reverses), like wind rolling over a wheat field. Output is a single full-width 766px GIF
        // (no auto-split), chainable; split with the main "Split GIF" button.
        public static async Task WindStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new WindDialog(false);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunWind(mainForm, dialog.BuildSettings(false));
        }

        public static async Task WindGif(GifToolMainForm mainForm)
        {
            using var dialog = new WindDialog(true);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunWind(mainForm, dialog.BuildSettings(true));
        }

        private static async Task RunWind(GifToolMainForm mainForm, WindSettings settings)
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
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_WindBuilding);
                    using var source = new MagickImageCollection(settings.InputFilePath);
                    source.Coalesce();

                    // Auto-resize to 766px wide when not already a supported width (the wave geometry is
                    // expressed in this canvas) — unless the user opted to keep the original size.
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

                    // Warn before running at a large native size (output frame count: wind-over-playback
                    // keeps the GIF length; frozen / static add the wind frames).
                    int outFrames = (settings.IsGif && settings.PlayGifDuringWind)
                        ? source.Count
                        : (int)Math.Round(Math.Max(0.1, settings.DurationSeconds) * Math.Max(1, settings.Fps)) + (settings.IsGif ? source.Count : 0);
                    if (!ConfirmLargeCanvas(mainForm, Math.Max(source.Count, outFrames), width, source[0].Height))
                    {
                        canceled = true;
                        return;
                    }

                    using var animation = BuildWindAnimation(mainForm, source, settings);
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

        // Dispatcher. GIF + "wind over playback": the GIF keeps playing its full length while the wind
        // is mixed onto the chosen [start, duration] window. Otherwise (static image, or GIF + "freeze
        // on frame 0"): wind over a single frozen frame, optionally followed by the full GIF.
        private static MagickImageCollection BuildWindAnimation(GifToolMainForm mainForm,
            MagickImageCollection source, WindSettings settings)
        {
            WindGust[] gusts = settings.ResolveGusts();
            WindMedium medium = settings.ToMedium();
            int srcTicks = (int)source[0].AnimationTicksPerSecond;
            if (srcTicks <= 0) srcTicks = 100;

            if (settings.IsGif && settings.PlayGifDuringWind)
            {
                return BuildWindPlayAlong(mainForm, source, settings, gusts, medium, srcTicks);
            }

            return BuildWindFrozenThenPlay(mainForm, source, settings, gusts, medium, srcTicks);
        }

        // Output length == source GIF length, native frame timing preserved. The wind is mixed onto each
        // live frame whose native time falls in the [EffectStart, +Duration) window where a gust is
        // blowing; everything else (outside the window, or still frames inside it) passes through as the
        // plain GIF. e.g. a 15s clip with a 6s window at start 0 -> 6s of wind+video, then 9s plain.
        private static MagickImageCollection BuildWindPlayAlong(GifToolMainForm mainForm,
            MagickImageCollection source, WindSettings settings, WindGust[] gusts, WindMedium medium, int srcTicks)
        {
            int n = source.Count;
            double duration = settings.DurationSeconds;
            if (duration < 0.1) duration = 0.1;

            // Cumulative native start time (seconds) of each source frame + total clip length.
            double[] startSec = new double[n];
            double acc = 0.0;
            for (int i = 0; i < n; i++)
            {
                startSec[i] = acc;
                acc += (double)source[i].AnimationDelay / srcTicks;
            }
            double gifSeconds = acc;

            // Clamp the desired [start, duration] effect window to fit inside the clip (shared helper).
            var (winStart, winDur) = GifEffectWindow.Clamp(settings.EffectStartSeconds, duration, gifSeconds);

            var result = new MagickImageCollection();
            int built = 0;
            for (int i = 0; i < n; i++)
            {
                double te = startSec[i] - winStart; // time within the effect window
                MagickImage frame;
                if (te >= 0.0 && te < winDur && WindField.AnyGustActive(te, gusts))
                {
                    frame = WindRenderer.RenderFrame(source[i], te, gusts, medium);
                }
                else
                {
                    frame = (MagickImage)source[i].Clone();
                }
                frame.ResetPage(); // uniform full-canvas page (rendered + cloned frames) so Optimize works
                frame.AnimationDelay = source[i].AnimationDelay;
                frame.AnimationTicksPerSecond = srcTicks;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);

                if (++built % 5 == 0 || built == n)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / n, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_WindBuilding);
                }
            }

            return result;
        }

        // Wind over a frozen frame 0 (or the static image) for `Duration` seconds at the chosen FPS. For
        // a GIF this is followed by the full source GIF played at its native timing (output length ==
        // Duration + GIF length); for a static image there is no play phase (output length == Duration).
        private static MagickImageCollection BuildWindFrozenThenPlay(GifToolMainForm mainForm,
            MagickImageCollection source, WindSettings settings, WindGust[] gusts, WindMedium medium, int srcTicks)
        {
            int fps = Math.Max(1, settings.Fps);
            int delay = Math.Max(1, (int)Math.Round(100.0 / fps));

            double duration = settings.DurationSeconds;
            if (duration <= 0.0)
            {
                duration = WindField.TotalSeconds(gusts);
            }
            if (duration < 0.1) duration = 0.1;
            int windFrames = Math.Max(1, (int)Math.Round(duration * fps));
            int playFrames = settings.IsGif ? source.Count : 0;
            int totalFrames = windFrames + playFrames;

            var result = new MagickImageCollection();
            int built = 0;

            // Phase 1: wind over the frozen first frame.
            for (int f = 0; f < windFrames; f++)
            {
                double t = (double)f / fps;
                var frame = WindRenderer.RenderFrame(source[0], t, gusts, medium);
                frame.AnimationDelay = (uint)delay;
                frame.AnimationTicksPerSecond = 100;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);

                if (++built % 5 == 0 || built == totalFrames)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_WindBuilding);
                }
            }

            // Phase 2 (GIF only): play the full source GIF at native timing after the wind settles.
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
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_WindBuilding);
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
