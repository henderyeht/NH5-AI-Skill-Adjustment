using System.Drawing;

namespace NH5AiSkillAdjustment
{
    internal static class Theme
    {
        public static readonly Color Asphalt = Color.FromArgb(16, 16, 18);
        public static readonly Color Panel = Color.FromArgb(28, 28, 32);
        public static readonly Color Yellow = Color.FromArgb(242, 203, 34);
        public static readonly Color Red = Color.FromArgb(196, 18, 28);
        public static readonly Color White = Color.FromArgb(245, 245, 245);
        public static readonly Color Mute = Color.FromArgb(168, 168, 176);
        public static readonly Color Track = Color.FromArgb(48, 48, 52);

        public static void FillCheckered(Graphics g, Rectangle bounds, int square)
        {
            using (var black = new SolidBrush(Color.Black))
            using (var white = new SolidBrush(Color.White))
            {
                for (var y = 0; y < bounds.Height; y += square)
                {
                    for (var x = 0; x < bounds.Width; x += square)
                    {
                        var col = ((x / square) + (y / square)) % 2 == 0 ? black : white;
                        g.FillRectangle(col, bounds.X + x, bounds.Y + y, square, square);
                    }
                }
            }
        }
    }
}
