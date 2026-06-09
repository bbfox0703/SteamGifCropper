using System;
using System.Threading.Tasks;
using ImageMagick;

namespace GifProcessorApp
{
    // Renders one "water fill + bubbles" frame onto a raw RGBA byte[] (Q8). Above the wavy surface the
    // output is `aboveSrc` unchanged; below it the output is `belowSrc` refracted by a gentle shimmer and,
    // where bubbles sit, magnified through them with a bright rim + highlight. The water level / surface /
    // bubble schedule come from the pure WaterField. The above/below sources are separate so the same
    // renderer serves both the single-GIF effect (above == below == the frame) and the A->B morph
    // (above == A, below == B).
    public static class WaterRenderer
    {
        // effectAlpha (0..1) fades the whole water effect toward the plain `aboveSrc` (1 = full water,
        // 0 = no effect) — used by the main-UI fade-out after the tank is full. The morph passes 1.
        public static MagickImage RenderFrame(IMagickImage<byte> aboveSrc, IMagickImage<byte> belowSrc,
            double fillFrac, double te, WaterParams p, double effectAlpha = 1.0)
        {
            uint w = aboveSrc.Width;
            uint h = aboveSrc.Height;
            int iw = (int)w;
            int ih = (int)h;

            byte[] above, below;
            using (var sp = aboveSrc.GetPixels()) above = sp.ToByteArray("RGBA")!;
            using (var sp = belowSrc.GetPixels()) below = sp.ToByteArray("RGBA")!;
            byte[] dst = new byte[above.Length];

            bool hasWater = fillFrac > 0.0 && effectAlpha > 0.0;

            // Pass 1: above the surface = aboveSrc as-is; below = belowSrc with the refraction shimmer.
            Parallel.For(0, ih, y =>
            {
                int rowBase = y * iw * 4;
                for (int x = 0; x < iw; x++)
                {
                    int idx = rowBase + x * 4;
                    if (hasWater && WaterField.IsUnderwater(x, y, te, fillFrac, iw, ih, p))
                    {
                        var (dx, dy) = WaterField.Shimmer(x, y, te, p);
                        SampleBilinearRgba(below, iw, ih, x + dx, y + dy, dst, idx);
                    }
                    else
                    {
                        dst[idx] = above[idx];
                        dst[idx + 1] = above[idx + 1];
                        dst[idx + 2] = above[idx + 2];
                        dst[idx + 3] = above[idx + 3];
                    }
                }
            });

            // Pass 2: bubbles (sequential; far/small first so near/big draw on top).
            if (hasWater)
            {
                WaterBubble[] bubbles = WaterField.Bubbles(te, fillFrac, iw, ih, p);
                Array.Sort(bubbles, (a, b) => a.R.CompareTo(b.R));
                foreach (var b in bubbles)
                {
                    DrawBubble(dst, below, iw, ih, te, fillFrac, p, b);
                }
            }

            // Fade-out: blend the whole water result toward the plain (above) source.
            if (effectAlpha < 1.0)
            {
                double alpha = effectAlpha < 0.0 ? 0.0 : effectAlpha;
                double inv = 1.0 - alpha;
                Parallel.For(0, ih, y =>
                {
                    int rowBase = y * iw * 4;
                    for (int x = 0; x < iw; x++)
                    {
                        int idx = rowBase + x * 4;
                        for (int c = 0; c < 4; c++)
                            dst[idx + c] = (byte)(above[idx + c] * inv + dst[idx + c] * alpha + 0.5);
                    }
                });
            }

            var settings = new PixelReadSettings(w, h, StorageType.Char, "RGBA");
            var outImg = new MagickImage();
            outImg.ReadPixels(dst, settings);
            outImg.ResetPage();
            return outImg;
        }

        private static void DrawBubble(byte[] dst, byte[] below, int w, int h, double te,
            double fillFrac, WaterParams p, WaterBubble b)
        {
            int x0 = Math.Max(0, (int)Math.Floor(b.X - b.R - 1));
            int x1 = Math.Min(w - 1, (int)Math.Ceiling(b.X + b.R + 1));
            int y0 = Math.Max(0, (int)Math.Floor(b.Y - b.R - 1));
            int y1 = Math.Min(h - 1, (int)Math.Ceiling(b.Y + b.R + 1));
            double hx = b.X - 0.32 * b.R, hy = b.Y - 0.32 * b.R; // specular highlight centre
            double bubbleAlpha = Math.Min(1.0, b.Opacity * 1.8);  // overall visibility (fades near the surface)
            double binv = 1.0 - bubbleAlpha;
            byte[] tmp = new byte[4];

            for (int py = y0; py <= y1; py++)
            {
                for (int px = x0; px <= x1; px++)
                {
                    double ddx = px - b.X, ddy = py - b.Y;
                    double dist = Math.Sqrt(ddx * ddx + ddy * ddy);
                    if (dist > b.R) continue;
                    if (!WaterField.IsUnderwater(px, py, te, fillFrac, w, h, p)) continue;

                    int idx = (py * w + px) * 4;

                    // Lens: sample the underwater source magnified toward the bubble centre (+ shimmer),
                    // blended toward the existing underwater pixel by the bubble's visibility so a fading
                    // bubble dissolves rather than distorting at full strength.
                    double m = WaterField.LensFactor(dist, b.R, b.Lens);
                    double sx = b.X + ddx * m;
                    double sy = b.Y + ddy * m;
                    var (shx, shy) = WaterField.Shimmer(sx, sy, te, p);
                    SampleBilinearRgba(below, w, h, sx + shx, sy + shy, tmp, 0);
                    dst[idx] = (byte)(dst[idx] * binv + tmp[0] * bubbleAlpha + 0.5);
                    dst[idx + 1] = (byte)(dst[idx + 1] * binv + tmp[1] * bubbleAlpha + 0.5);
                    dst[idx + 2] = (byte)(dst[idx + 2] * binv + tmp[2] * bubbleAlpha + 0.5);
                    dst[idx + 3] = (byte)(dst[idx + 3] * binv + tmp[3] * bubbleAlpha + 0.5);

                    // Bright rim near the edge.
                    double rim = dist / b.R;
                    if (rim > 0.82)
                    {
                        double k = (rim - 0.82) / 0.18 * b.Opacity;
                        BlendWhite(dst, idx, k);
                    }

                    // Specular highlight spot.
                    double hdx = px - hx, hdy = py - hy;
                    double hd = Math.Sqrt(hdx * hdx + hdy * hdy);
                    if (hd < b.R * 0.22)
                    {
                        double k = (1.0 - hd / (b.R * 0.22)) * 0.6 * b.Opacity;
                        BlendWhite(dst, idx, k);
                    }
                }
            }
        }

        private static void BlendWhite(byte[] buf, int idx, double a)
        {
            if (a <= 0.0) return;
            if (a > 1.0) a = 1.0;
            double inv = 1.0 - a;
            buf[idx] = (byte)(buf[idx] * inv + 255 * a + 0.5);
            buf[idx + 1] = (byte)(buf[idx + 1] * inv + 255 * a + 0.5);
            buf[idx + 2] = (byte)(buf[idx + 2] * inv + 255 * a + 0.5);
            int na = (int)(buf[idx + 3] + a * 255.0 + 0.5);
            buf[idx + 3] = na > 255 ? (byte)255 : (byte)na;
        }

        // Bilinear RGBA sample, edge-clamped (copied from the ripple/wind resamplers).
        private static void SampleBilinearRgba(byte[] src, int w, int h, double sx, double sy, byte[] dst, int di)
        {
            if (sx < 0.0) sx = 0.0; else if (sx > w - 1) sx = w - 1;
            if (sy < 0.0) sy = 0.0; else if (sy > h - 1) sy = h - 1;

            int x0 = (int)Math.Floor(sx);
            int y0 = (int)Math.Floor(sy);
            int x1 = x0 + 1; if (x1 > w - 1) x1 = w - 1;
            int y1 = y0 + 1; if (y1 > h - 1) y1 = h - 1;

            double fx = sx - x0;
            double fy = sy - y0;
            double w00 = (1.0 - fx) * (1.0 - fy);
            double w10 = fx * (1.0 - fy);
            double w01 = (1.0 - fx) * fy;
            double w11 = fx * fy;

            int i00 = (y0 * w + x0) * 4;
            int i10 = (y0 * w + x1) * 4;
            int i01 = (y1 * w + x0) * 4;
            int i11 = (y1 * w + x1) * 4;

            for (int c = 0; c < 4; c++)
            {
                double v = src[i00 + c] * w00 + src[i10 + c] * w10 + src[i01 + c] * w01 + src[i11 + c] * w11;
                int iv = (int)(v + 0.5);
                dst[di + c] = iv < 0 ? (byte)0 : iv > 255 ? (byte)255 : (byte)iv;
            }
        }
    }
}
