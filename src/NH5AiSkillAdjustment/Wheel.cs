using System;
using System.Drawing;
using System.Windows.Forms;

namespace NH5AiSkillAdjustment
{
    internal sealed class WheelTextBox : TextBox
    {
        protected override void WndProc(ref Message m)
        {
            if (Wheel.Forward(this, ref m))
            {
                return;
            }

            base.WndProc(ref m);
        }
    }

    internal sealed class WheelCombo : ComboBox
    {
        protected override void WndProc(ref Message m)
        {
            if (Wheel.Forward(this, ref m))
            {
                return;
            }

            base.WndProc(ref m);
        }
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
