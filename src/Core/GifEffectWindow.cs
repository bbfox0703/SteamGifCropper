using System;

namespace GifProcessorApp
{
    // Pure, dependency-free math for a GIF-animation effect window: a [start, duration] span (seconds)
    // within a source GIF, clamped to fit the clip and snapped to whole frames. Shared by the slot
    // machine and quicksand "play during" models so the effect runs over a chosen segment of the live
    // GIF instead of always starting at frame 0. Linked into the test project (GifProcessor uses a stub).
    public static class GifEffectWindow
    {
        // Clamp a desired [start, duration] window (seconds) to fit inside [0, gifSeconds]:
        //   - duration <= 0 or >= gifSeconds -> the whole clip (0, gifSeconds)
        //   - start + duration > gifSeconds  -> slide start back so the window ends at gifSeconds
        //   - start < 0                      -> 0
        // Worst case is therefore start = 0, duration = gifSeconds (the entire animation).
        public static (double Start, double Duration) Clamp(double desiredStart, double desiredDuration, double gifSeconds)
        {
            if (gifSeconds <= 0.0) return (0.0, 0.0);

            double dur = desiredDuration;
            if (dur <= 0.0 || dur >= gifSeconds)
            {
                return (0.0, gifSeconds);
            }

            double start = desiredStart < 0.0 ? 0.0 : desiredStart;
            if (start + dur > gifSeconds)
            {
                start = gifSeconds - dur;
            }
            if (start < 0.0) start = 0.0;
            return (start, dur);
        }

        // Index of the frame whose start time is nearest to targetSec (ties resolve to the lower index).
        public static int NearestFrameIndex(double[] frameStartSec, double targetSec)
        {
            if (frameStartSec == null || frameStartSec.Length == 0) return 0;

            int best = 0;
            double bestDist = Math.Abs(frameStartSec[0] - targetSec);
            for (int i = 1; i < frameStartSec.Length; i++)
            {
                double d = Math.Abs(frameStartSec[i] - targetSec);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }

        // Resolve a desired [start, duration] (seconds) into a frame window [StartFrame, EndFrame) for a
        // GIF described by per-frame start times + total length. Clamps, then snaps the start to the
        // nearest frame and the end to the nearest frame boundary OR the clip end (frameCount). Always
        // returns EndFrame > StartFrame so the window is at least one frame.
        public static (int StartFrame, int EndFrame) ResolveFrames(double[] frameStartSec, double gifSeconds,
            double desiredStart, double desiredDuration)
        {
            if (frameStartSec == null || frameStartSec.Length == 0) return (0, 0);
            int n = frameStartSec.Length;

            var (start, dur) = Clamp(desiredStart, desiredDuration, gifSeconds);

            int startFrame = NearestFrameIndex(frameStartSec, start);

            double endTarget = start + dur;
            int endFrame = NearestFrameIndex(frameStartSec, endTarget);
            // The end can also snap to the clip's end boundary (index n), which is closer than any frame
            // start when the window reaches the tail of the GIF.
            if (Math.Abs(gifSeconds - endTarget) <= Math.Abs(frameStartSec[endFrame] - endTarget))
            {
                endFrame = n;
            }
            if (endFrame <= startFrame)
            {
                endFrame = Math.Min(n, startFrame + 1);
            }
            return (startFrame, endFrame);
        }

        // Effect phase t in [0, 1] for frame index i within a resolved [startFrame, endFrame) window:
        //   i <= startFrame -> 0 (effect not started)
        //   i >= endFrame   -> 1 (effect finished)
        //   otherwise linearly across the window's frames.
        // For effects that return to the original image at t = 0 and t = 1 (quicksand flow, slot reels),
        // this means the frames outside the window pass through untouched.
        public static double FramePhase(int i, int startFrame, int endFrame)
        {
            if (endFrame <= startFrame) return i >= startFrame ? 1.0 : 0.0;
            if (i <= startFrame) return 0.0;
            if (i >= endFrame) return 1.0;
            return (double)(i - startFrame) / (endFrame - startFrame);
        }
    }
}
