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
        #region Water Fill (灌水 + 氣泡) Methods

        // Fill an image / GIF with rising water (refraction shimmer + lensing bubbles below the wavy
        // surface, nothing above it). Output is a single full-width 766px GIF (no auto-split), chainable.
        public static async Task WaterStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new WaterDialog(false);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunWater(mainForm, dialog.BuildSettings(false));
        }

        public static async Task WaterGif(GifToolMainForm mainForm)
        {
            using var dialog = new WaterDialog(true);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunWater(mainForm, dialog.BuildSettings(true));
        }

        private static async Task RunWater(GifToolMainForm mainForm, WaterSettings settings)
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
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_WaterBuilding);
                    using var source = new MagickImageCollection(settings.InputFilePath);
                    source.Coalesce();

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

                    int outFrames = (settings.IsGif && settings.PlayGifDuringWater)
                        ? source.Count
                        : (int)Math.Round(Math.Max(0.1, settings.DurationSeconds) * Math.Max(1, settings.Fps)) + (settings.IsGif ? source.Count : 0);
                    if (!ConfirmLargeCanvas(mainForm, Math.Max(source.Count, outFrames), width, source[0].Height))
                    {
                        canceled = true;
                        return;
                    }

                    using var animation = BuildWaterAnimation(mainForm, source, settings);
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

        private static MagickImageCollection BuildWaterAnimation(GifToolMainForm mainForm,
            MagickImageCollection source, WaterSettings settings)
        {
            int srcTicks = (int)source[0].AnimationTicksPerSecond;
            if (srcTicks <= 0) srcTicks = 100;
            WaterParams water = settings.ToParams();

            if (settings.IsGif && settings.PlayGifDuringWater)
            {
                return BuildWaterPlayAlong(mainForm, source, settings, water, srcTicks);
            }
            return BuildWaterFrozenThenPlay(mainForm, source, settings, water, srcTicks);
        }

        // Output length == source GIF length, native timing. Water fills over the live frames between
        // StartSeconds and FullSeconds; before the start the frame passes through unchanged; once full it
        // stays full so the rest of the GIF plays fully submerged (the no-truncate invariant: the whole
        // clip still plays).
        private static MagickImageCollection BuildWaterPlayAlong(GifToolMainForm mainForm,
            MagickImageCollection source, WaterSettings settings, WaterParams water, int srcTicks)
        {
            int n = source.Count;
            double[] startSec = new double[n];
            double acc = 0.0;
            for (int i = 0; i < n; i++)
            {
                startSec[i] = acc;
                acc += (double)source[i].AnimationDelay / srcTicks;
            }

            var result = new MagickImageCollection();
            int built = 0;
            for (int i = 0; i < n; i++)
            {
                double te = startSec[i];
                double fill = WaterField.FillFrac(te, settings.StartSeconds, settings.FullSeconds);
                double alpha = WaterField.EffectAlpha(te, settings.FullSeconds, settings.FadeOutSeconds);
                MagickImage frame;
                if (fill > 0.0 && alpha > 0.0)
                {
                    frame = WaterRenderer.RenderFrame(source[i], source[i], fill, te, water, alpha);
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
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_WaterBuilding);
                }
            }
            return result;
        }

        // Water fills over a frozen frame 0 for `Duration` seconds at the chosen FPS, then (GIF) the full
        // source GIF plays fully submerged; a static image is just the fill animation.
        private static MagickImageCollection BuildWaterFrozenThenPlay(GifToolMainForm mainForm,
            MagickImageCollection source, WaterSettings settings, WaterParams water, int srcTicks)
        {
            int fps = Math.Max(1, settings.Fps);
            int delay = Math.Max(1, (int)Math.Round(100.0 / fps));
            double duration = settings.DurationSeconds;
            if (duration < 0.1) duration = 0.1;
            int fillFrames = Math.Max(1, (int)Math.Round(duration * fps));
            int playFrames = settings.IsGif ? source.Count : 0;
            int totalFrames = fillFrames + playFrames;

            var result = new MagickImageCollection();
            int built = 0;

            // Phase 1: water rises over the frozen first frame, then fades out.
            for (int f = 0; f < fillFrames; f++)
            {
                double te = (double)f / fps;
                double fill = WaterField.FillFrac(te, settings.StartSeconds, settings.FullSeconds);
                double alpha = WaterField.EffectAlpha(te, settings.FullSeconds, settings.FadeOutSeconds);
                MagickImage frame = (fill > 0.0 && alpha > 0.0)
                    ? WaterRenderer.RenderFrame(source[0], source[0], fill, te, water, alpha)
                    : (MagickImage)source[0].Clone();
                frame.ResetPage();
                frame.AnimationDelay = (uint)delay;
                frame.AnimationTicksPerSecond = 100;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);

                if (++built % 5 == 0 || built == totalFrames)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_WaterBuilding);
                }
            }

            // Phase 2 (GIF only): play the full source GIF. While the water is still up it is submerged;
            // once it has faded out (alpha == 0) the frames play plain.
            if (settings.IsGif)
            {
                double te = duration;
                for (int i = 0; i < source.Count; i++)
                {
                    double alpha = WaterField.EffectAlpha(te, settings.FullSeconds, settings.FadeOutSeconds);
                    MagickImage play = alpha > 0.0
                        ? WaterRenderer.RenderFrame(source[i], source[i], 1.0, te, water, alpha)
                        : (MagickImage)source[i].Clone();
                    play.ResetPage();
                    play.AnimationDelay = source[i].AnimationDelay;
                    play.AnimationTicksPerSecond = srcTicks;
                    play.GifDisposeMethod = GifDisposeMethod.Background;
                    result.Add(play);
                    te += (double)source[i].AnimationDelay / srcTicks;

                    if (++built % 5 == 0 || built == totalFrames)
                    {
                        SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_WaterBuilding);
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
