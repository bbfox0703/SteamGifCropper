using System;
using System.Threading.Tasks;
using ImageMagick;

namespace GifProcessorApp
{
    // Applies a RippleField displacement field to an image, one frame at a time. Kept separate from the
    // Magick-heavy GifProcessor (and from the pure RippleField math) so the wave physics stays unit-tested.
    //
    // Per output pixel we look up a displaced source position (inverse mapping) and bilinearly sample it,
    // which produces the refraction/lensing look of water ripples. Done with direct RGBA byte buffers +
    // Parallel.For (no ImageMagick Drawables / Q8 Displace map), so it is fast and dodges Q8 quantization.
    public static class RippleRenderer
    {
        // Renders `source` displaced by the ripple field at absolute time t (seconds). Returns a new image
        // the same size as the source; the caller owns it.
        public static MagickImage RenderFrame(IMagickImage<byte> source, double t, RippleDrop[] drops, RippleMedium medium)
        {
            uint w = source.Width;
            uint h = source.Height;
            int iw = (int)w;
            int ih = (int)h;

            byte[] src;
            using (var sp = source.GetPixels())
            {
                src = sp.ToByteArray("RGBA")!; // iw*ih*4 bytes, Q8 = 1 byte/channel (never null for a valid frame)
            }
            byte[] dst = new byte[src.Length];

            Parallel.For(0, ih, y =>
            {
                int rowBase = y * iw * 4;
                for (int x = 0; x < iw; x++)
                {
                    var (dx, dy) = RippleField.Displacement(x, y, t, drops, medium);
                    SampleBilinearRgba(src, iw, ih, x + dx, y + dy, dst, rowBase + x * 4);
                }
            });

            var settings = new PixelReadSettings(w, h, StorageType.Char, "RGBA");
            var outImg = new MagickImage();
            outImg.ReadPixels(dst, settings);
            outImg.ResetPage(); // uniform full-canvas page so the collection stays Optimize-able
            return outImg;
        }

        // Bilinear RGBA sample of `src` at (sx, sy), edge-clamped (out-of-range looks up the nearest edge
        // pixel rather than punching a transparent hole), writing 4 bytes to dst[di..di+3].
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
                if (iv < 0) iv = 0; else if (iv > 255) iv = 255;
                dst[di + c] = (byte)iv;
            }
        }
    }
}
