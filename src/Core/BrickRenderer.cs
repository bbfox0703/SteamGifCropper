using System;
using ImageMagick;

namespace GifProcessorApp
{
    // Renders one brick/plank-drop morph frame: clip A is the base; each started plank crops its
    // destination slice from clip B and composites it at the plank's current (falling / landed+bounce)
    // position. Planks are drawn in drop order, so the one currently in the air (the latest to start)
    // lands on top of the already-stacked ones. The geometry/physics is the pure BrickField.
    public static class BrickRenderer
    {
        public static MagickImage RenderFrame(IMagickImage<byte> frameA, IMagickImage<byte> frameB, double t, BrickParams p)
        {
            int w = (int)frameA.Width;
            int h = (int)frameA.Height;
            bool vertical = BrickField.IsVertical(p.Direction);
            int axisLen = vertical ? h : w;

            var canvas = (MagickImage)frameA.Clone();
            canvas.ResetPage();

            BrickPlank[] planks = BrickField.Planks(t, p, axisLen);
            for (int d = 0; d < planks.Length; d++)
            {
                BrickPlank pl = planks[d];
                if (!pl.Started) continue;

                using var strip = (MagickImage)frameB.Clone();
                if (vertical)
                {
                    strip.Crop(new MagickGeometry(0, pl.SliceStart, (uint)w, (uint)pl.SliceLen));
                    strip.ResetPage();
                    canvas.Composite(strip, 0, pl.CurrentPos, CompositeOperator.Over);
                }
                else
                {
                    strip.Crop(new MagickGeometry(pl.SliceStart, 0, (uint)pl.SliceLen, (uint)h));
                    strip.ResetPage();
                    canvas.Composite(strip, pl.CurrentPos, 0, CompositeOperator.Over);
                }
            }

            canvas.ResetPage();
            return canvas;
        }
    }
}
