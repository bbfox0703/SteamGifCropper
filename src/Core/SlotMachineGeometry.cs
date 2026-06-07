using System;

namespace GifProcessorApp
{
    // Pure, dependency-free slot-machine reel math so it can be linked into the test project
    // (which uses a GifProcessor stub instead of the Magick-heavy real class).
    //
    // Model: each of `reelCount` reels (= Steam columns) scrolls its own column content
    // vertically (wrap-around) and decelerates to land on the aligned image. Reels stop
    // left-to-right (staggered). The vertical offset is what the renderer feeds to
    // MagickImage.Roll(0, offset).
    public static class SlotMachineGeometry
    {
        // Standard ease-out cubic: fast start, slow finish. Clamped to [0, 1].
        public static double EaseOutCubic(double t)
        {
            if (t <= 0.0) return 0.0;
            if (t >= 1.0) return 1.0;
            double inv = 1.0 - t;
            return 1.0 - inv * inv * inv;
        }

        // Frame at which reel `reelIndex` snaps to its aligned (locked) position.
        // The rightmost reel stops last (== totalSpinFrames); each earlier reel stops
        // `staggerFrames` sooner. Always >= 1 so callers can divide by it safely.
        public static int ReelStopFrame(int reelIndex, int reelCount, int totalSpinFrames, int staggerFrames)
        {
            if (reelCount < 1) reelCount = 1;
            if (staggerFrames < 0) staggerFrames = 0;
            int baseStop = totalSpinFrames - (reelCount - 1) * staggerFrames;
            int stop = baseStop + reelIndex * staggerFrames;
            return Math.Max(1, stop);
        }

        // Vertical wrap offset (in [0, canvasHeight)) for a reel at a given frame.
        // Returns 0 once the reel has reached its stop frame (locked on the image).
        public static int ReelOffsetY(int frameIndex, int reelIndex, int reelCount,
                                      int totalSpinFrames, int staggerFrames, int canvasHeight, int spins)
        {
            if (canvasHeight <= 0) return 0;
            if (spins < 1) spins = 1;

            int stop = ReelStopFrame(reelIndex, reelCount, totalSpinFrames, staggerFrames);
            if (frameIndex >= stop) return 0; // locked

            double localT = (double)frameIndex / stop;        // [0, 1)
            double eased = EaseOutCubic(localT);              // [0, 1)
            double distance = spins * (double)canvasHeight * (1.0 - eased); // travel remaining

            int offset = (int)Math.Round(distance) % canvasHeight;
            if (offset < 0) offset += canvasHeight;
            return offset;
        }
    }
}
