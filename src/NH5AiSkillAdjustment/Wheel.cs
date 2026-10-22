using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NH5AiSkillAdjustment
{
    internal sealed class WheelTextBox : TextBox
    {
        public WheelTextBox()
        {
            BorderStyle = BorderStyle.FixedSingle;
            BackColor = Color.White;
            ForeColor = Color.Black;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetWindowTheme(Handle, "", "");
        }

        protected override void WndProc(ref Message m)
        {
            if (Wheel.Forward(this, ref m))
            {
                return;
            }

            base.WndProc(ref m);
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string app, string id);
    }

    internal sealed class WheelCombo : ComboBox
    {
        public WheelCombo()
        {
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.White;
            ForeColor = Color.Black;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetWindowTheme(Handle, "", "");
        }

        protected override void WndProc(ref Message m)
        {
            if (Wheel.Forward(this, ref m))
            {
                return;
            }

            base.WndProc(ref m);
            if (m.Msg == 0x000F)
            {
                PaintFace();
            }
        }

        private void PaintFace()
        {
            using (var g = Graphics.FromHwnd(Handle))
            using (var fill = new SolidBrush(BackColor))
            using (var text = new SolidBrush(ForeColor))
            using (var arrow = new SolidBrush(ForeColor.GetBrightness() > 0.6f ? Theme.Yellow : Color.Black))
            using (var edge = new Pen(Color.FromArgb(160, 160, 160)))
            {
                g.FillRectangle(fill, ClientRectangle);
                if (BackColor.GetBrightness() > 0.5f)
                {
                    g.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);
                }
                var label = Text;
                if (SelectedIndex >= 0 && SelectedIndex < Items.Count && Items[SelectedIndex] != null)
                {
                    label = Items[SelectedIndex].ToString();
                }

                g.DrawString(label ?? "", Font, text, new RectangleF(4, 0, Width - 22, Height), new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
                var mid = Height / 2;
                g.FillPolygon(arrow, new[] { new Point(Width - 16, mid - 2), new Point(Width - 8, mid - 2), new Point(Width - 12, mid + 3) });
            }
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string app, string id);
    }

    internal static class Wheel
    {
        public static bool Forward(Control from, ref Message m)
        {
            if (m.Msg != 0x020A)
            {
                return false;
            }

            ScrollableControl? parent = null;
            for (Control? cursor = from.Parent; cursor != null; cursor = cursor.Parent)
            {
                if (cursor is ScrollableControl scroll && scroll.AutoScroll)
                {
                    parent = scroll;
                    break;
                }
            }

            if (parent != null)
            {
                var delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                var pos = parent.AutoScrollPosition;
                var lines = SystemInformation.MouseWheelScrollLines;
                if (lines < 1)
                {
                    lines = 3;
                }

                var y = -pos.Y - Math.Sign((int)delta) * lines * 8;
                if (y < 0)
                {
                    y = 0;
                }

                parent.AutoScrollPosition = new Point(-pos.X, y);
            }

            return true;
        }
    }
}
