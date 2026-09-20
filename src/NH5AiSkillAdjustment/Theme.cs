using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NH5AiSkillAdjustment
{
    internal static class Theme
    {
        public static readonly Color Asphalt = Color.FromArgb(16, 16, 18);
        public static readonly Color Panel = Color.FromArgb(28, 28, 32);
        public static readonly Color Yellow = Color.FromArgb(242, 203, 34);
        public static readonly Color Red = Color.FromArgb(196, 18, 28);
        public static readonly Color Blue = Color.FromArgb(0, 84, 182);
        public static readonly Color White = Color.FromArgb(245, 245, 245);
        public static readonly Color Mute = Color.FromArgb(168, 168, 176);
        public static readonly Color Track = Color.FromArgb(48, 48, 52);

        public static void FillCheckered(Graphics g, Rectangle bounds, int square)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0 || square <= 0)
            {
                return;
            }

            var state = g.Save();
            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.SetClip(bounds);
            using (var black = new SolidBrush(Color.Black))
            using (var white = new SolidBrush(Color.White))
            {
                for (var y = 0; y < bounds.Height; y += square)
                {
                    for (var x = 0; x < bounds.Width; x += square)
                    {
                        var w = Math.Min(square, bounds.Width - x);
                        var h = Math.Min(square, bounds.Height - y);
                        var col = ((x / square) + (y / square)) % 2 == 0 ? black : white;
                        g.FillRectangle(col, bounds.X + x, bounds.Y + y, w, h);
                    }
                }
            }

            g.Restore(state);
        }

        public static void DrawCheckeredThumb(Graphics g, Rectangle thumb, Color border)
        {
            var inner = Rectangle.Inflate(thumb, -2, -2);
            FillCheckered(g, inner, 6);
            using (var pen = new Pen(border, 2) { Alignment = PenAlignment.Inset })
            {
                g.DrawRectangle(pen, thumb);
            }
        }

        public static void DrawOutlinedText(Graphics g, string text, Font font, RectangleF bounds, StringFormat format)
        {
            var emSize = font.SizeInPoints * g.DpiY / 72f;
            using (var path = new GraphicsPath())
            {
                path.AddString(text, font.FontFamily, (int)font.Style, emSize, bounds, format);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var outline = new Pen(Color.Black, 2.6f) { LineJoin = LineJoin.Round })
                {
                    g.DrawPath(outline, path);
                }

                using (var fill = new SolidBrush(White))
                {
                    g.FillPath(fill, path);
                }
            }
        }

        public static void DrawHeaderStripes(Graphics g, int y, int width)
        {
            using (var blue = new SolidBrush(Blue))
            using (var yellow = new SolidBrush(Yellow))
            {
                g.FillRectangle(blue, 0, y, width, 3);
                g.FillRectangle(yellow, 0, y + 3, width, 5);
            }
        }

        public static void PaintLeftAccent(Graphics g, int height)
        {
            using (var blue = new SolidBrush(Blue))
            {
                g.FillRectangle(blue, 0, 0, 4, height);
            }
        }
    }
}
