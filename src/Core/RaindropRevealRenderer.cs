using System;
using System.Threading.Tasks;
using ImageMagick;

namespace GifProcessorApp
{
    // Cross-dissolves clip A into clip B per the raindrop coverage mask, one morph frame at a time. Like
    // the other renderers it works on raw RGBA byte[] (Q8) + Parallel.For: out = A*(1-cov) + B*cov, where
    // cov is the soft puddle coverage from RaindropRevealField. The soft puddle edges give the "暈開"
    // spreading look without a separate blur pass.
    public static class RaindropRevealRenderer
    {
        // A and B must already be the same size. Returns a new image the size of A; the caller owns it.
        public static MagickImage RenderFrame(IMagickImage<byte> frameA, IMagickImage<byte> frameB,
            double t, RaindropSeed[] drops, double soft)
        {
            uint w = frameA.Width;
            uint h = frameA.Height;
            int iw = (int)w;
            int ih = (int)h;

            byte[] a, b;
            using (var sp = frameA.GetPixels()) a = sp.ToByteArray("RGBA")!;
            using (var sp = frameB.GetPixels()) b = sp.ToByteArray("RGBA")!;
            byte[] dst = new byte[a.Length];

            Parallel.For(0, ih, y =>
            {
                int rowBase = y * iw * 4;
                for (int x = 0; x < iw; x++)
                {
                    double cov = RaindropRevealField.Coverage(x, y, t, drops, soft);
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
