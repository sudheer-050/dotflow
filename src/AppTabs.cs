using System.Drawing;
using System.Windows.Forms;
namespace LocalDictation
{
    sealed class AppTabs : TabControl
    {
        protected override void WndProc(ref Message message)
        {
            base.WndProc(ref message);
            if ((message.Msg != 0xF && message.Msg != 0x318) || TabCount == 0 || SystemInformation.HighContrast) return;
            using (var graphics = message.Msg == 0x318 && message.WParam != System.IntPtr.Zero ? Graphics.FromHdc(message.WParam) : Graphics.FromHwnd(Handle))
            using (var fill = new SolidBrush(ThemeColors.Paper))
            using (var border = new Pen(ThemeColors.Paper, 2))
            {
                var last = GetTabRect(TabCount - 1); graphics.FillRectangle(fill, last.Right + 2, 0, System.Math.Max(0, ClientSize.Width - last.Right - 2), last.Bottom + 2);
                var page = DisplayRectangle; graphics.DrawRectangle(border, page.Left - 2, page.Top - 2, page.Width + 3, page.Height + 3);
            }
        }
    }
}
