using FFMpegCore;
using FFMpegCore.Exceptions;
using FFMpegCore.Pipes;
using ImageMagick;
using ImageMagick.Drawing;
using SteamGifCropper;
using SteamGifCropper.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GifProcessorApp
{
    public static partial class GifProcessor
    {
        #region Water Ripple (水波紋) Methods

        // Drop up to 3 expanding damped ripples onto an image / GIF and refract the pixels through the
        // summed wave field (interference emerges from the sum; see RippleField). Output is a single
        // full-width 766px GIF (no auto-split), chainable; split with the main "Split GIF" button.
        public static async Task RippleStaticImage(GifToolMainForm mainForm)
        {
            using var dialog = new RippleDialog(false);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunRipple(mainForm, dialog.BuildSettings(false));
        }

        public static async Task RippleGif(GifToolMainForm mainForm)
        {
            using var dialog = new RippleDialog(true);
            if (dialog.ShowDialog(mainForm) != DialogResult.OK)
            {
                return;
            }
            await RunRipple(mainForm, dialog.BuildSettings(true));
        }

        private static async Task RunRipple(GifToolMainForm mainForm, RippleSettings settings)
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
                    using var source = LoadCoalesceWithProgress(mainForm, settings.InputFilePath);

                    // Auto-resize to 766px wide when not already a supported width (drop coordinates are
                    // expressed in this canvas) — unless the user opted to keep the original size.
                    uint width = source[0].Width;
                    if (!settings.KeepOriginalSize && !IsValidCanvasWidth(width))
                    {
                        ResizeAllToWidthWithProgress(mainForm, source, SupportedWidth1);
                        width = source[0].Width;
                    }

                    // Warn before running at a large native size (output frame count: ripple-over-playback
                    // keeps the GIF length; frozen / static add the ripple frames — sized by the effective
                    // duration, which auto-extends to cover drops landing after the set duration).
                    double sceneSeconds = RippleField.EffectiveDuration(
                        Math.Max(0.1, settings.DurationSeconds), settings.Drops.ToArray(), settings.ToMedium());
                    int outFrames = (settings.IsGif && settings.PlayGifDuringRipple)
                        ? source.Count
                        : (int)Math.Round(sceneSeconds * Math.Max(1, settings.Fps)) + (settings.IsGif ? source.Count : 0);
                    if (!ConfirmLargeCanvas(mainForm, Math.Max(source.Count, outFrames), width, source[0].Height))
                    {
                        canceled = true;
                        return;
                    }

                    using var animation = BuildRippleAnimation(mainForm, source, settings);
                    OptimizeAndWriteWithProgress(mainForm, animation, settings.OutputFilePath);
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

        // Renders the ripple scene (duration x fps frames). Frozen: every frame refracts frame 0.
        // Play-along: each frame refracts the live GIF frame sampled at that scene time (looping). Runs on
        // a background thread; all UI updates go through the marshaled helpers.
        private static MagickImageCollection BuildRippleAnimation(GifToolMainForm mainForm,
            MagickImageCollection source, RippleSettings settings)
        {
            RippleDrop[] drops = settings.Drops.ToArray();
            RippleMedium medium = settings.ToMedium();
            int srcTicks = (int)source[0].AnimationTicksPerSecond;
            if (srcTicks <= 0) srcTicks = 100;

            // GIF + "ripple over playback": keep the GIF playing its FULL length at its native timing; the
            // ripple is mixed onto the first `Duration` seconds, then the rest plays plain.
            if (settings.IsGif && settings.PlayGifDuringRipple)
            {
                return BuildRipplePlayAlong(mainForm, source, settings, drops, medium, srcTicks);
            }

            // Static image: ripple on the still frame for `Duration`. GIF + "freeze on frame 0": ripple on
            // a frozen frame 0 for `Duration`, then the full GIF plays (native timing).
            return BuildRippleFrozenThenPlay(mainForm, source, settings, drops, medium, srcTicks);
        }

        // Output length == source GIF length, native frame timing preserved (so playback duration is
        // unchanged). Each frame within [0, Duration) where a drop is active is refracted; everything else
        // (after the ripple window, or dead frames inside it) passes through as the plain GIF.
        private static MagickImageCollection BuildRipplePlayAlong(GifToolMainForm mainForm,
            MagickImageCollection source, RippleSettings settings, RippleDrop[] drops, RippleMedium medium, int srcTicks)
        {
            int n = source.Count;
            // The mix window must reach every drop (start + lifetime), not just the dialog's Duration —
            // otherwise drops landing after the default 4s would silently never render.
            double duration = RippleField.EffectiveDuration(settings.DurationSeconds, drops, medium);
            if (duration < 0.1) duration = 0.1;

            // Cumulative native start time (seconds) of each source frame.
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
                double t = startSec[i];
                MagickImage frame;
                if (t < duration && RippleField.AnyDropActive(t, drops, medium))
                {
                    frame = RippleRenderer.RenderFrame(source[i], t, drops, medium);
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
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RippleBuilding);
                }
            }

            return result;
        }

        // Ripple over a frozen frame 0 (or the static image) for `Duration` seconds at the chosen FPS. For
        // a GIF this is followed by the full source GIF played at its native timing (output length ==
        // Duration + GIF length); for a static image there is no play phase (output length == Duration).
        private static MagickImageCollection BuildRippleFrozenThenPlay(GifToolMainForm mainForm,
            MagickImageCollection source, RippleSettings settings, RippleDrop[] drops, RippleMedium medium, int srcTicks)
        {
            int fps = Math.Max(1, settings.Fps);
            int delay = Math.Max(1, (int)Math.Round(100.0 / fps));

            // The frozen scene must reach every drop (start + lifetime), not just the dialog's Duration —
            // a drop landing after the set duration would otherwise be silently cut off.
            double duration = RippleField.EffectiveDuration(Math.Max(0.0, settings.DurationSeconds), drops, medium);
            if (duration < 0.1) duration = 0.1;
            int rippleFrames = Math.Max(1, (int)Math.Round(duration * fps));
            int playFrames = settings.IsGif ? source.Count : 0;
            int totalFrames = rippleFrames + playFrames;

            var result = new MagickImageCollection();
            int built = 0;

            // Phase 1: ripple over the frozen first frame.
            for (int f = 0; f < rippleFrames; f++)
            {
                double t = (double)f / fps;
                var frame = RippleRenderer.RenderFrame(source[0], t, drops, medium);
                frame.AnimationDelay = (uint)delay;
                frame.AnimationTicksPerSecond = 100;
                frame.GifDisposeMethod = GifDisposeMethod.Background;
                result.Add(frame);

                if (++built % 5 == 0 || built == totalFrames)
                {
                    SetProgressBar(mainForm.pBarTaskStatus, built * 100 / totalFrames, 100);
                    SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RippleBuilding);
                }
            }

            // Phase 2 (GIF only): play the full source GIF at native timing after the ripple settles.
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
                        SetStatusText(mainForm, SteamGifCropper.Properties.Resources.Status_RippleBuilding);
                    }
                }
            }

            return result;
        }

        #endregion

    }
}
