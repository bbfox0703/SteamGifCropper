using System;

namespace GifProcessorApp
{
    // The direction the planks fall FROM -> the edge they stack at. Down = fall downward, stack from the
    // bottom up (the classic case). Enum order matches the dialog's direction dropdown index.
    public enum BrickDirection
    {
        Down = 0,  // fall down,  stack from the bottom
        Up = 1,    // fall up,    stack from the top
        Left = 2,  // fall left,  stack from the left
        Right = 3, // fall right, stack from the right
    }

    public struct BrickParams
    {
        public int Pieces;          // number of planks along the fall axis
        public BrickDirection Direction;
        public double TotalHeightM; // physical height of the canvas (m) — scales the impact velocity
        public double Gravity;      // g (m/s^2)
        public double Weight;       // heavier -> the bounce settles faster
        public double Hardness;     // 0..1 -> bigger restitution / bounce
    }

    // One plank for a single frame: it carries B's DESTINATION slice (SliceStart..+SliceLen along the fall
    // axis) painted on a rigid board, currently drawn at CurrentPos (off-screen while waiting, accelerating
    // while falling, at its slot +/- a damped bounce once landed).
    public struct BrickPlank
    {
        public bool Started;    // false until this plank's drop begins (slice still shows clip A)
        public int SliceStart;  // top-left coord of the destination slice along the fall axis (px in B)
        public int SliceLen;    // px along the fall axis
        public int CurrentPos;  // current top-left coord along the axis (may be off-canvas while falling)
    }

    // Pure, dependency-free physics for the brick/plank-drop morph (linked into the test project;
    // BrickRenderer does the Magick crop/composite). Planks drop one at a time in a stagger derived from
    // free-fall timing (far planks fall farther -> take longer), each landing with a damped bounce whose
    // size comes from the impact velocity (g + drop height) and hardness, and whose settle rate comes from
    // weight. The last 20% of planks (the final ones to drop) skip the bounce. The whole sequence is
    // normalised to the morph window [0,1] (MorphSeconds sets the overall pace; the physics shapes the
    // fall acceleration + bounce), so every plank is at rest -> full B by t = 1.
    public static class BrickField
    {
        private const double LandEnd = 0.9;     // last plank lands at t=0.9, leaving room to settle by t=1
        private const double SettleFrac = 0.08; // bounce/settle window (fraction of the morph) per plank
        private const double BounceHops = 2.0;  // number of decaying hops in the bounce

        public static bool IsVertical(BrickDirection d) => d == BrickDirection.Down || d == BrickDirection.Up;
        private static bool Forward(BrickDirection d) => d == BrickDirection.Down || d == BrickDirection.Right;
        private static int Edge(int i, int axisLen, int n) => (int)Math.Round((double)i * axisLen / n);

        // All planks' states at normalised morph progress t in [0,1]. axisLen = canvas size along the fall
        // axis (height for Down/Up, width for Left/Right).
        public static BrickPlank[] Planks(double t, BrickParams p, int axisLen)
        {
            int n = p.Pieces < 1 ? 1 : p.Pieces;
            int L = Math.Max(1, axisLen);
            bool forward = Forward(p.Direction);

            int[] edge = new int[n + 1];
            for (int i = 0; i <= n; i++) edge[i] = Edge(i, L, n);

            // Per drop-order d: which slice it fills, where it starts off-screen, how far it falls.
            int[] sliceStart = new int[n];
            int[] sliceLen = new int[n];
            double[] start = new double[n];
            double[] dest = new double[n];
            double[] dist = new double[n];
            for (int d = 0; d < n; d++)
            {
                int si = forward ? (n - 1 - d) : d; // forward stacks from the far/high end
                sliceStart[d] = edge[si];
                sliceLen[d] = Math.Max(1, edge[si + 1] - edge[si]);
                dest[d] = sliceStart[d];
                start[d] = forward ? -sliceLen[d] : L; // just off the leading edge
                dist[d] = Math.Abs(dest[d] - start[d]);
            }

            // Normalised stagger: fall duration ~ sqrt(distance) (free fall); all land within [0, LandEnd].
            double[] raw = new double[n];
            double sumRaw = 0.0;
            for (int d = 0; d < n; d++) { raw[d] = Math.Sqrt(dist[d]); sumRaw += raw[d]; }
            double scale = sumRaw > 1e-9 ? LandEnd / sumRaw : 0.0;
            double[] tStart = new double[n];
            double[] tLand = new double[n];
            double acc = 0.0;
            for (int d = 0; d < n; d++)
            {
                tStart[d] = scale * acc;
                acc += raw[d];
                tLand[d] = scale * acc;
            }

            int noBounce = (int)Math.Ceiling(0.2 * n); // last 20% (final to drop) don't bounce
            double hardness = p.Hardness < 0.0 ? 0.0 : p.Hardness > 1.0 ? 1.0 : p.Hardness;
            double decay = 3.0 + Math.Max(0.0, p.Weight); // heavier -> faster settle

            var planks = new BrickPlank[n];
            for (int d = 0; d < n; d++)
            {
                var pl = new BrickPlank { SliceStart = sliceStart[d], SliceLen = sliceLen[d] };
                if (t < tStart[d])
                {
                    pl.Started = false;
                    pl.CurrentPos = (int)Math.Round(start[d]);
                }
                else
                {
                    pl.Started = true;
                    if (t < tLand[d])
                    {
                        double frac = (t - tStart[d]) / Math.Max(1e-6, tLand[d] - tStart[d]);
                        double pos = start[d] + (dest[d] - start[d]) * frac * frac; // accelerating fall
                        pl.CurrentPos = (int)Math.Round(pos);
                    }
                    else
                    {
                        double pos = dest[d];
                        double tau = t - tLand[d];
                        bool bounces = d < n - noBounce;
                        if (bounces && tau < SettleFrac)
                        {
                            double distM = dist[d] * p.TotalHeightM / L;
                            double v = Math.Sqrt(2.0 * Math.Max(0.0, p.Gravity) * Math.Max(0.0, distM)); // impact speed
                            double amp = hardness * sliceLen[d] * 0.6 * Math.Tanh(v / 5.0);
                            double mag = amp * Math.Exp(-decay * tau / SettleFrac)
                                             * Math.Abs(Math.Sin(Math.PI * BounceHops * tau / SettleFrac));
                            double dir = forward ? 1.0 : -1.0;
                            pos = dest[d] - dir * mag; // bounce away from the surface it hit
                        }
                        pl.CurrentPos = (int)Math.Round(pos);
                    }
                }
                planks[d] = pl;
            }
            return planks;
        }
    }
}
