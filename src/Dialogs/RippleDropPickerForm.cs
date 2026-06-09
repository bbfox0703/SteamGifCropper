#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using SteamGifCropper.Properties;

namespace GifProcessorApp
{
    // Click-to-pick the drop position on a pre-rendered frame-0 canvas supplied by RippleDialog (which
    // builds + caches it at the SAME size the engine renders — native when "keep original size" is on,
    // otherwise 766px wide — so the picked coordinates line up with the output). Static frame, no
    // playback. The bitmap is borrowed (owned + cached by the dialog); this form never disposes it.
    public class RippleDropPickerForm : Form
    {
        public int PickedX { get; private set; }
        public int PickedY { get; private set; }

        private readonly Bitmap _bitmap;
        private readonly int _imgW;
        private readonly int _imgH;
        private readonly double _scale;
        private readonly int _initialX;
        private readonly int _initialY;

        private PictureBox _pic = null!;
        private Label _lblCoord = null!;
        private Button _btnCancel = null!;
        private Point _cursorDisp = new Point(-1, -1);

        public RippleDropPickerForm(Bitmap canvasFrame0, int imgW, int imgH, int initialX, int initialY)
        {
            _bitmap = canvasFrame0; // borrowed: owned + cached by RippleDialog, not disposed here
            _imgW = imgW;
            _imgH = imgH;
            _initialX = initialX;
            _initialY = initialY;
            PickedX = initialX;
            PickedY = initialY;

            // Design-pixel fit budget. AutoScaleMode.Font multiplies these by the DPI ratio, so keep them
            // modest enough that the scaled form still fits a 1366x768 screen at 150%.
            const int maxW = 1000, maxH = 410;
            _scale = Math.Min(1.0, Math.Min((double)maxW / _imgW, (double)maxH / _imgH));

            InitializeComponent();
            ApplyTheme();
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);

        // Map between displayed (PictureBox client) coords and image coords using the PictureBox's ACTUAL
        // size, so it stays correct whether or not the form was DPI-scaled.
        private int DispToImageX(int dispX) => Clamp((int)Math.Round(dispX * (double)_imgW / Math.Max(1, _pic.Width)), 0, _imgW - 1);
        private int DispToImageY(int dispY) => Clamp((int)Math.Round(dispY * (double)_imgH / Math.Max(1, _pic.Height)), 0, _imgH - 1);

        private void ApplyTheme()
        {
            bool isDark = WindowsThemeManager.IsDarkModeEnabled();
            BackColor = isDark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
            ForeColor = isDark ? Color.White : SystemColors.ControlText;
            _lblCoord.ForeColor = ForeColor;
            if (isDark)
            {
                _btnCancel.BackColor = Color.FromArgb(64, 64, 64);
                _btnCancel.ForeColor = Color.White;
                _btnCancel.FlatStyle = FlatStyle.Flat;
            }
            if (IsHandleCreated)
            {
                WindowsThemeManager.SetDarkModeForWindow(Handle, isDark);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (value && IsHandleCreated)
            {
                WindowsThemeManager.SetDarkModeForWindow(Handle, WindowsThemeManager.IsDarkModeEnabled());
            }
        }

        private void InitializeComponent()
        {
            int dispW = (int)Math.Round(_imgW * _scale);
            int dispH = (int)Math.Round(_imgH * _scale);
            int clientW = Math.Max(dispW, 260);

            _pic = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(dispW, dispH),
                Image = _bitmap,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(96, 96, 96),
                Cursor = Cursors.Cross,
            };
            _pic.MouseMove += Pic_MouseMove;
            _pic.MouseClick += Pic_MouseClick;
            _pic.Paint += Pic_Paint;

            _lblCoord = new Label
            {
                Location = new Point(10, dispH + 15),
                AutoSize = true,
                Text = "X: -   Y: -",
            };

            _btnCancel = new Button
            {
                Location = new Point(clientW - 98, dispH + 9),
                Size = new Size(88, 30),
                Text = Resources.ScrollDialog_Cancel,
                DialogResult = DialogResult.Cancel,
                UseVisualStyleBackColor = true,
            };

            SuspendLayout();
            Controls.Add(_pic);
            Controls.Add(_lblCoord);
            Controls.Add(_btnCancel);

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _btnCancel;
            ClientSize = new Size(clientW, dispH + 48);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RippleDropPickerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = Resources.RippleDialog_PickTitle;
            ResumeLayout(false);
            PerformLayout();
        }

        private void Pic_MouseMove(object? sender, MouseEventArgs e)
        {
            _cursorDisp = e.Location;
            int ix = DispToImageX(e.X);
            int iy = DispToImageY(e.Y);
            _lblCoord.Text = $"X: {ix}   Y: {iy}";
            _pic.Invalidate();
        }

        private void Pic_MouseClick(object? sender, MouseEventArgs e)
        {
            PickedX = DispToImageX(e.X);
            PickedY = DispToImageY(e.Y);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Pic_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Marker at the current value (if it falls inside the canvas).
            if (_initialX >= 0 && _initialX < _imgW && _initialY >= 0 && _initialY < _imgH)
            {
                int mx = (int)Math.Round(_initialX * (double)_pic.Width / _imgW);
                int my = (int)Math.Round(_initialY * (double)_pic.Height / _imgH);
                using var marker = new Pen(Color.FromArgb(220, Color.Yellow), 1.5f);
                g.DrawEllipse(marker, mx - 5, my - 5, 10, 10);
            }

            // Crosshair following the cursor.
            if (_cursorDisp.X >= 0)
            {
                using var pen = new Pen(Color.FromArgb(200, Color.Red), 1f);
                g.DrawLine(pen, _cursorDisp.X, 0, _cursorDisp.X, _pic.Height);
                g.DrawLine(pen, 0, _cursorDisp.Y, _pic.Width, _cursorDisp.Y);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _pic != null)
            {
                _pic.Image = null; // the bitmap is owned + cached by RippleDialog, not by this form
            }
            base.Dispose(disposing);
        }
    }
}
