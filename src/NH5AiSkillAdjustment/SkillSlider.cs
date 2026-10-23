using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NH5AiSkillAdjustment
{
    internal sealed class SkillSlider : Control
    {
        private int _value = 100;
        private bool _dragging;

        public int Minimum { get; set; } = 60;
        public int Maximum { get; set; } = 200;
        public bool FromCenter { get; set; }
        public bool ColorFromNeutral { get; set; }
        public int Neutral { get; set; }
        public int ThumbWidth { get; set; }
        public string CenterLabel { get; set; } = "";

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
            Height = 52;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            var basic = ThumbWidth <= 0;
            var track = basic
                ? new Rectangle(22, Height / 2 - 6, Math.Max(1, Width - 44), 12)
                : new Rectangle(8, string.IsNullOrEmpty(CenterLabel) ? Height / 2 - 4 : Height - 18, Math.Max(1, Width - 16), 8);
            using (var trackBrush = new SolidBrush(Theme.Track))
            {
                g.FillRectangle(trackBrush, track);
            }

            var t = Maximum == Minimum ? 0f : (float)(Value - Minimum) / (Maximum - Minimum);
            var thumbX = track.X + (int)(track.Width * t);
            var anchor = track.X;
            var fillColor = Theme.Blue;
            var paintFill = true;
            if (!basic && ColorFromNeutral && Maximum != Minimum)
            {
                var nt = (float)(Neutral - Minimum) / (Maximum - Minimum);
                anchor = track.X + (int)(track.Width * nt);
                if (Value > Neutral)
                {
                    fillColor = Theme.Blue;
                }
                else if (Value < Neutral)
                {
                    fillColor = Theme.Red;
                }
                else
                {
                    paintFill = false;
                }
            }
            else if (FromCenter)
            {
                anchor = track.X + (track.Width / 2);
            }

            if (paintFill)
            {
                using (var fill = new SolidBrush(fillColor))
                {
                    var left = Math.Min(anchor, thumbX);
                    g.FillRectangle(fill, left, track.Y, Math.Abs(thumbX - anchor), track.Height);
                }
            }
            if (!string.IsNullOrEmpty(CenterLabel))
            {
                var mid = track.X + (track.Width / 2);
                using (var tick = new Pen(Theme.White))
                {
                    g.DrawLine(tick, mid, track.Y - 3, mid, track.Bottom + 3);
                }

                using (var font = new Font("Segoe UI", 7.5f, FontStyle.Bold))
                using (var brush = new SolidBrush(Theme.White))
                {
                    var size = g.MeasureString(CenterLabel, font);
                    g.DrawString(CenterLabel, font, brush, mid - (size.Width / 2f), 0);
                }
            }

            int thumbW;
            int thumbH;
            int thumbTop;
            if (basic)
            {
                thumbH = Math.Min(46, Math.Max(16, Height - 6));
                thumbW = Math.Min(40, thumbH);
                thumbTop = (Height - thumbH) / 2;
            }
            else
            {
                thumbW = ThumbWidth;
                thumbH = 22;
                var roomTop = string.IsNullOrEmpty(CenterLabel) ? 2 : 14;
                var roomBottom = Height - 2;
                if (thumbH > roomBottom - roomTop)
                {
                    thumbH = Math.Max(12, roomBottom - roomTop);
                }

                thumbTop = track.Y + (track.Height / 2) - (thumbH / 2);
                if (thumbTop < roomTop)
                {
                    thumbTop = roomTop;
                }

                if (thumbTop + thumbH > roomBottom)
                {
                    thumbTop = Math.Max(roomTop, roomBottom - thumbH);
                }
            }

            var thumb = new Rectangle(thumbX - (thumbW / 2), thumbTop, thumbW, thumbH);
            Theme.DrawCheckeredThumb(g, thumb, Theme.Yellow);
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
            var inset = ThumbWidth <= 0 ? 22 : 8;
            var span = ThumbWidth <= 0 ? 44 : 16;
            var track = new Rectangle(inset, 0, Math.Max(1, Width - span), Height);
            var t = (x - track.X) / (float)track.Width;
            t = Math.Max(0f, Math.Min(1f, t));
            Value = Minimum + (int)Math.Round(t * (Maximum - Minimum));
        }
    }
}
