using System;
using ImageMagick;

namespace GifProcessorApp
{
    // Draws a translucent rain layer (slanted streaks) over a frame. Like the other effect renderers this
    // works on a raw RGBA byte[] (Q8) rather than ImageMagick Drawables, so it composites the rain with
    // explicit alpha blending and dodges Q8 quantization. The streak geometry comes from the pure
    // RainField; this class only rasterizes it.
    public static class RainRenderer
    {
        // Light, slightly bluish rain colour.
        private const int RainR = 235, RainG = 240, RainB = 250;

        // Maximum per-pixel opacity of a streak (kept well below 1 so rain stays translucent); the layer
        // fade scales it further.
        private const double StrokeAlpha = 0.55;

        // Renders `source` with `streaks` composited on top at whole-layer opacity `alpha` (0..1). Returns
        // a new image the same size as the source; the caller owns it.
        public static MagickImage RenderFrame(IMagickImage<byte> source, RainStreak[] streaks, double alpha)
        {
            uint w = source.Width;
            uint h = source.Height;
            int iw = (int)w;
            int ih = (int)h;

            byte[] buf;
            using (var sp = source.GetPixels())
            {
                buf = sp.ToByteArray("RGBA")!; // iw*ih*4 bytes, Q8 = 1 byte/channel
            }

            if (streaks != null && alpha > 0.0)
            {
                double a = StrokeAlpha * (alpha < 0.0 ? 0.0 : alpha > 1.0 ? 1.0 : alpha);
                for (int s = 0; s < streaks.Length; s++)
                {
                    DrawLine(buf, iw, ih, streaks[s].X0, streaks[s].Y0, streaks[s].X1, streaks[s].Y1, a);
                }
            }

            var settings = new PixelReadSettings(w, h, StorageType.Char, "RGBA");
            var outImg = new MagickImage();
            outImg.ReadPixels(buf, settings);
            outImg.ResetPage(); // uniform full-canvas page so the collection stays Optimize-able
            return outImg;
        }

        // Alpha-blends a 1px line (DDA) of rain colour into the RGBA buffer; pixels outside the canvas are
        // skipped. a is the per-pixel blend factor (0..1).
        private static void DrawLine(byte[] buf, int w, int h, double x0, double y0, double x1, double y1, double a)
        {
            double dx = x1 - x0;
            double dy = y1 - y0;
            int steps = (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)));
            if (steps < 1) steps = 1;
            double sx = dx / steps;
            double sy = dy / steps;
            double x = x0;
            double y = y0;
            for (int i = 0; i <= steps; i++)
            {
                Plot(buf, w, h, (int)Math.Round(x), (int)Math.Round(y), a);
                x += sx;
                y += sy;
            }
        }

        private static void Plot(byte[] buf, int w, int h, int x, int y, double a)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
            int idx = (y * w + x) * 4;
            double inv = 1.0 - a;
            buf[idx]     = (byte)(buf[idx]     * inv + RainR * a + 0.5);
            buf[idx + 1] = (byte)(buf[idx + 1] * inv + RainG * a + 0.5);
            buf[idx + 2] = (byte)(buf[idx + 2] * inv + RainB * a + 0.5);
            int na = (int)(buf[idx + 3] + a * 255.0 + 0.5); // rain makes transparent areas a bit opaque
            buf[idx + 3] = na > 255 ? (byte)255 : (byte)na;
        }
    }
}
