using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LocalDictation
{
    static class ThemeColors
    {
        public static string Mode = "System";
        public static bool Dark
        {
            get
            {
                if (Mode != "System") return Mode == "Dark";
                try { using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")) return key != null && Convert.ToInt32(key.GetValue("AppsUseLightTheme", 1)) == 0; } catch { return false; }
            }
        }
        public static Color Paper { get { return SystemInformation.HighContrast ? SystemColors.Window : Dark ? Color.FromArgb(23, 25, 29) : Color.FromArgb(250, 249, 246); } }
        public static Color Card { get { return SystemInformation.HighContrast ? SystemColors.Window : Dark ? Color.FromArgb(34, 37, 42) : Color.White; } }
        public static Color Ink { get { return SystemInformation.HighContrast ? SystemColors.WindowText : Dark ? Color.FromArgb(237, 239, 243) : Color.FromArgb(39, 42, 47); } }
        public static Color Muted { get { return SystemInformation.HighContrast ? SystemColors.WindowText : Dark ? Color.FromArgb(174, 182, 192) : Color.FromArgb(115, 116, 118); } }
        public static Color Button { get { return SystemInformation.HighContrast ? SystemColors.Control : Dark ? Color.FromArgb(49, 53, 60) : Color.FromArgb(246, 244, 240); } }
        public static Color Selected { get { return SystemInformation.HighContrast ? SystemColors.Highlight : Dark ? Color.FromArgb(62, 78, 72) : Color.FromArgb(228, 224, 216); } }
        public static void Apply(Control root, string mode)
        {
            Mode = mode;
            var tabPage = root as TabPage; if (tabPage != null) tabPage.UseVisualStyleBackColor = false;
            var buttonBase = root as ButtonBase; if (buttonBase != null) buttonBase.UseVisualStyleBackColor = false;
            var surface = root as Surface;
            if (surface != null) surface.Fill = Card;
            root.BackColor = root is TabPage || root is TabControl ? Paper : surface != null ? Color.Transparent : root is TextBoxBase || root is ListBox || root is DataGridView ? Card : root.Parent == null ? Paper : Surface.CanvasColor(root.Parent);
            root.ForeColor = root is Label && !root.Font.Bold && root.Font.Size < 11 ? Muted : Ink;
            var label = root as Label; if (label != null) label.UseMnemonic = false;
            foreach (Control child in root.Controls) Apply(child, mode);
            var tabs = root as TabControl;
            if (tabs != null && tabs.DrawMode != TabDrawMode.OwnerDrawFixed)
            {
                tabs.Padding = new Point(10, 5); tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
                tabs.DrawItem += (s, e) => { using (var brush = new SolidBrush(tabs.SelectedIndex == e.Index ? Card : Paper)) e.Graphics.FillRectangle(brush, e.Bounds); TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds, Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix); };
            }
            root.Invalidate(true);
        }
    }
}
