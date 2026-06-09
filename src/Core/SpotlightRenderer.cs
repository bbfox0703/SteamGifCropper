using System;
using System.Threading.Tasks;
using ImageMagick;

namespace GifProcessorApp
{
    // Renders one spotlight morph frame: clip A everywhere, with clip B showing through the soft circular
    // spotlight (SpotlightField gives the centre + radius + soft coverage). Like the other morph renderers
    // it blends raw RGBA byte[] with Parallel.For: out = A*(1-cov) + B*cov.
    public static class SpotlightRenderer
    {
        // A and B must already be the same size. tNorm is the morph progress in [0,1]; morphSeconds is the
        // window length (used to convert the px/sec speed into travel). Returns a new image; caller owns it.
        public static MagickImage RenderFrame(IMagickImage<byte> frameA, IMagickImage<byte> frameB,
            double tNorm, double morphSeconds, SpotlightParams p)
        {
            uint w = frameA.Width;
            uint h = frameA.Height;
            int iw = (int)w;
            int ih = (int)h;

            byte[] a, b;
            using (var sp = frameA.GetPixels()) a = sp.ToByteArray("RGBA")!;
            using (var sp = frameB.GetPixels()) b = sp.ToByteArray("RGBA")!;
            byte[] dst = new byte[a.Length];

            var (cx, cy) = SpotlightField.Center(tNorm, morphSeconds, iw, ih, p);
            double radius = SpotlightField.RadiusAt(tNorm, morphSeconds, iw, ih, p);
            double soft = p.Soft;

            Parallel.For(0, ih, y =>
            {
                int rowBase = y * iw * 4;
                for (int x = 0; x < iw; x++)
                {
                    double cov = SpotlightField.Coverage(x, y, cx, cy, radius, soft);
                    double inv = 1.0 - cov;
                    int idx = rowBase + x * 4;
                    for (int c = 0; c < 4; c++)
                    {
                        double v = a[idx + c] * inv + b[idx + c] * cov;
                        int iv = (int)(v + 0.5);
                        dst[idx + c] = iv < 0 ? (byte)0 : iv > 255 ? (byte)255 : (byte)iv;
                    }
                }
            });

            var settings = new PixelReadSettings(w, h, StorageType.Char, "RGBA");
            var outImg = new MagickImage();
            outImg.ReadPixels(dst, settings);
            outImg.ResetPage();
            return outImg;
        }
    }
}
