using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NH5AiSkillAdjustment
{
    internal sealed class SkillSlider : Control
    {
        private int _value = 105;
        private bool _dragging;

        public int Minimum { get; set; } = 60;
        public int Maximum { get; set; } = 200;

        public int Value
        {
            get => _value;
            set
            {
                var next = Math.Max(Minimum, Math.Min(Maximum, value));
                if (next == _value)
                {
                    return;
                }

                _value = next;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? ValueChanged;

        public SkillSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 36;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            var track = new Rectangle(10, Height / 2 - 5, Width - 20, 10);
            using (var trackBrush = new SolidBrush(Theme.Track))
            {
                g.FillRectangle(trackBrush, track);
            }

            var t = Maximum == Minimum ? 0f : (float)(Value - Minimum) / (Maximum - Minimum);
            var fillWidth = (int)(track.Width * t);
            using (var yellow = new SolidBrush(Theme.Yellow))
            {
                g.FillRectangle(yellow, track.X, track.Y, fillWidth, track.Height);
            }

            var thumbX = track.X + fillWidth;
            var thumb = new Rectangle(thumbX - 9, Height / 2 - 14, 18, 28);
            Theme.FillCheckered(g, thumb, 6);
            using (var pen = new Pen(Theme.Yellow, 2))
            {
                g.DrawRectangle(pen, thumb);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                Capture = true;
                SetFromX(e.X);
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                SetFromX(e.X);
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _dragging = false;
            Capture = false;
            base.OnMouseUp(e);
        }

        private void SetFromX(int x)
        {
            var track = new Rectangle(10, 0, Math.Max(1, Width - 20), Height);
            var t = (x - track.X) / (float)track.Width;
            t = Math.Max(0f, Math.Min(1f, t));
            Value = Minimum + (int)Math.Round(t * (Maximum - Minimum));
        }
    }
}
