using System;
using System.Drawing;
using System.Windows.Forms;

namespace GifProcessorApp
{
    /// <summary>
    /// A flat, owner-drawn progress bar. The native comctl32 ProgressBar renders its fill as
    /// themed "chunks" with a sliding animation; under the dark theme the leading chunk shows as
    /// two black vertical lines at the fill edge that travel right as progress advances. Painting
    /// the bar ourselves (a single solid fill rectangle) bypasses that rendering entirely.
    /// It subclasses ProgressBar, so Minimum/Maximum/Value/Visible keep working unchanged.
    /// </summary>
    public class FlatProgressBar : ProgressBar
    {
        private const int PBM_SETPOS = 0x0402;
        private const int PBM_DELTAPOS = 0x0403;
        private const int PBM_SETRANGE = 0x0401;
        private const int PBM_SETRANGE32 = 0x0406;

        private Color _fillColor = Color.FromArgb(0, 120, 215); // Windows accent blue, reads on light & dark

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color FillColor
        {
            get { return _fillColor; }
            set { _fillColor = value; Invalidate(); }
        }

        public FlatProgressBar()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // Background is drawn in OnPaint to avoid a flicker between the two passes.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle rect = ClientRectangle;
            Graphics g = e.Graphics;

            using (var background = new SolidBrush(BackColor))
            {
                g.FillRectangle(background, rect);
            }

            int range = Maximum - Minimum;
            if (range <= 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                return;
            }

            double ratio = (double)(Value - Minimum) / range;
            int fillWidth = (int)Math.Round(rect.Width * ratio);
            if (fillWidth > 0)
            {
                using (var fill = new SolidBrush(_fillColor))
                {
                    g.FillRectangle(fill, rect.X, rect.Y, fillWidth, rect.Height);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // The base ProgressBar updates its position via these messages; repaint our flat fill.
            if (m.Msg == PBM_SETPOS || m.Msg == PBM_DELTAPOS || m.Msg == PBM_SETRANGE || m.Msg == PBM_SETRANGE32)
            {
                Invalidate();
            }
        }
    }
}
