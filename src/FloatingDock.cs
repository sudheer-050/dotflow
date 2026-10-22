using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LocalDictation
{
    sealed class DockButton : Button
    {
        public bool Active;
        public string Glyph;
        public DockButton() { TabStop = false; SetStyle(ControlStyles.Selectable, false); FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand; Font = new Font("Segoe UI", 9); SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(25, 27, 30)); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = Active ? Color.FromArgb(191, 238, 216) : ClientRectangle.Contains(PointToClient(Cursor.Position)) ? Color.FromArgb(64, 67, 72) : Color.FromArgb(42, 45, 49);
            using (var path = Surface.Shape(new RectangleF(0, 0, Width - 1, Height - 1), 8)) using (var brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
            Color ink = !Enabled ? Color.FromArgb(111, 115, 121) : Active ? Color.FromArgb(25, 47, 37) : Color.White;
            using (var iconFont = new Font("Segoe MDL2 Assets", 13))
                TextRenderer.DrawText(e.Graphics, Active ? "\uE71A" : Glyph ?? Text, iconFont, ClientRectangle, ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        }
    }
    // A non-activating controller: clicking Write keeps the user's original text field focused.
    sealed class FloatingDock : Form
    {
        readonly DockButton write = new DockButton { Text = "Dictate", AccessibleName = "Toggle dictation" }, note = new DockButton { Text = "Note", AccessibleName = "Toggle personal note" }, meeting = new DockButton { Text = "Meeting", AccessibleName = "Toggle meeting note" }, more = new DockButton { Text = "Open app", AccessibleName = "Open Dotflow" };
        readonly Panel grip = new Panel { Cursor = Cursors.SizeAll };
        readonly Label status = new Label { Text = "Ready · hold the dots to move", ForeColor = Color.FromArgb(170, 177, 185), AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        readonly ToolTip hints = new ToolTip { InitialDelay = 200, ReshowDelay = 100, ShowAlways = true };
        readonly Timer hover = new Timer { Interval = 100 };
        bool dragging, expanded, vertical;
        bool dragHintShown;
        int outsideTicks;
        float dpi = 1;
        Point restingPosition;
        Point dragStart, locationStart;
        public Action ToggleWrite, ToggleNote, ToggleMeeting, OpenApp, CancelWrite;
        bool cancelAvailable, reducedMotion, enabled = true;
        public string VisibilityDescription { get { return !enabled ? "Hidden in preferences" : snoozedUntil != DateTime.MinValue ? "Hidden until " + snoozedUntil.ToString("t") : "Ready on the screen edge"; } }
        DateTime snoozedUntil;
        readonly Timer fade = new Timer { Interval = 20 };
        public Action<Point> PositionSaved;
        public FloatingDock()
        {
            Text = "Dotflow toolbar"; Icon = AppLogo.Icon; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; AutoScaleMode = AutoScaleMode.None; BackColor = Color.FromArgb(25, 27, 30); ClientSize = new Size(326, 72); StartPosition = FormStartPosition.Manual;
            grip.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(write.Active || note.Active || meeting.Active ? Color.FromArgb(191, 238, 216) : Color.FromArgb(156, 163, 170), Px(2)))
                    if (vertical) e.Graphics.DrawLine(pen, grip.Width / 2, expanded ? Px(5) : Px(15), grip.Width / 2, grip.Height - (expanded ? Px(5) : Px(15)));
                    else e.Graphics.DrawLine(pen, expanded ? Px(5) : Px(15), grip.Height / 2, grip.Width - (expanded ? Px(5) : Px(15)), grip.Height / 2);
            };
            write.SetBounds(36, 9, 73, 34); note.SetBounds(115, 9, 73, 34); meeting.SetBounds(194, 9, 86, 34); more.SetBounds(286, 9, 30, 34); status.SetBounds(38, 47, 278, 19); status.Font = new Font("Segoe UI", 8);
            Controls.AddRange(new Control[] { grip, write, note, meeting, more, status });
            write.Click += (s, e) => { if (ToggleWrite != null) ToggleWrite(); }; note.Click += (s, e) => { if (ToggleNote != null) ToggleNote(); }; meeting.Click += (s, e) => { if (ToggleMeeting != null) ToggleMeeting(); }; more.Click += (s, e) => { if (cancelAvailable && CancelWrite != null) CancelWrite(); else if (OpenApp != null) OpenApp(); };
            hints.SetToolTip(grip, "Drag to move. Release to dock in the nearest screen edge."); hints.SetToolTip(write, "Click to write in the current app. Click again to refine and stop."); hints.SetToolTip(note, "Click to start a personal note. Click again to stop and save."); hints.SetToolTip(meeting, "Click to record a meeting. Click again to stop and save.");
            grip.MouseDown += DragStart; status.MouseDown += DragStart; MouseDown += DragStart;
            grip.MouseMove += DragMove; status.MouseMove += DragMove; MouseMove += DragMove;
            grip.MouseUp += DragEnd; status.MouseUp += DragEnd; MouseUp += DragEnd;
            grip.MouseCaptureChanged += CaptureChanged; status.MouseCaptureChanged += CaptureChanged; MouseCaptureChanged += CaptureChanged;
            using (var graphics = CreateGraphics()) dpi = graphics.DpiX / 96f;
            write.Glyph = "\uE720"; note.Glyph = "\uE70B"; meeting.Glyph = "\uE716"; more.Glyph = "\uE712";
            foreach (var button in new[] { write, note, meeting, more })
            {
                button.Font = new Font("Segoe UI", 8);

            }
            hints.SetToolTip(write, "Dictation"); hints.SetToolTip(note, "Personal notes"); hints.SetToolTip(meeting, "Meeting notes"); hints.SetToolTip(more, "Open Dotflow");
            hover.Tick += (s, e) =>
            {
                if (snoozedUntil != DateTime.MinValue) { if (DateTime.Now >= snoozedUntil) { snoozedUntil = DateTime.MinValue; if (enabled) Show(); } return; }
                if (!Visible || dragging) return;
                if (Bounds.Contains(Cursor.Position)) { outsideTicks = 0; SetExpanded(true); }
                else if (++outsideTicks >= 5) SetExpanded(false);
            };
            fade.Tick += (s, e) => { Opacity = Math.Min(1, Opacity + 0.12); if (Opacity >= 1) fade.Stop(); };
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += DisplaysChanged;
            SetExpanded(false, true); hover.Start();
        }
        public void Reveal() { enabled = true; snoozedUntil = DateTime.MinValue; Show(); }
        public void SetVisibility(bool value) { enabled = value; snoozedUntil = DateTime.MinValue; if (enabled) Show(); else Hide(); }
        public void Configure(bool reduced) { reducedMotion = reduced; if (reduced) { fade.Stop(); Opacity = 1; } }
        public void Snooze() { if (!enabled) return; snoozedUntil = DateTime.Now.AddHours(1); Hide(); }
        void DisplaysChanged(object sender, EventArgs e) { if (!IsDisposed && IsHandleCreated) BeginInvoke((Action)(() => RestorePosition(restingPosition.X, restingPosition.Y))); }
        int Px(int value) { return (int)Math.Round(value * dpi); }
        internal void SetExpanded(bool value, bool force = false)
        {
            if (!force && expanded == value) return;
            bool opening = value && !expanded; expanded = value;
            if (opening && !reducedMotion) { Opacity = 0.7; fade.Start(); }
            var area = Screen.FromPoint(new Point(restingPosition.X + Px(28), restingPosition.Y + Px(7))).WorkingArea;
            var idle = new Size(Px(56), Px(14));
            int left = Math.Abs(restingPosition.X - area.Left), right = Math.Abs(area.Right - restingPosition.X - idle.Width);
            int top = Math.Abs(restingPosition.Y - area.Top), bottom = Math.Abs(area.Bottom - restingPosition.Y - idle.Height);
            int edge = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
            vertical = edge == left || edge == right;
            ClientSize = vertical ? (value ? new Size(Px(44), Px(188)) : new Size(Px(14), Px(56))) : (value ? new Size(Px(188), Px(44)) : idle);
            grip.SetBounds(0, 0, value && !vertical ? Px(16) : Width, value && vertical ? Px(16) : Height);
            var buttons = new[] { write, note, meeting, more };
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].Visible = value;
                if (vertical) buttons[i].SetBounds(Px(5), Px(18 + i * 43), Px(34), Px(36));
                else buttons[i].SetBounds(Px(18 + i * 43), Px(5), Px(36), Px(34));
            }
            status.Visible = false;
            var position = restingPosition;
            if (edge == right) position.X += idle.Width - Width;
            if (edge == bottom) position.Y += idle.Height - Height;
            Location = Clamp(position, Rectangle.Inflate(area, -Px(3), -Px(3)), Size);
            var oldRegion = Region;
            using (var shape = Surface.Shape(new RectangleF(0, 0, Width, Height), Px(value ? 12 : 6))) Region = new Region(shape);
            if (oldRegion != null) oldRegion.Dispose();
            grip.Invalidate();
            if (opening && !dragHintShown && Visible) { dragHintShown = true; hints.Show("Drag this grip to move along any screen edge.", grip, vertical ? Width : 0, vertical ? 0 : Height, 2800); }
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams { get { var value = base.CreateParams; value.ExStyle |= 0x08000000 | 0x80; return value; } }
        protected override void WndProc(ref Message message) { if (message.Msg == 0x02E0) { dpi = (message.WParam.ToInt64() & 0xffff) / 96f; SetExpanded(expanded, true); message.Result = IntPtr.Zero; return; } if (message.Msg == 0x21) { message.Result = (IntPtr)3; return; } base.WndProc(ref message); }
        static Point Clamp(Point position, Rectangle area, Size size) { return new Point(Math.Max(area.Left, Math.Min(area.Right - size.Width, position.X)), Math.Max(area.Top, Math.Min(area.Bottom - size.Height, position.Y))); }
        internal static Point EdgePosition(Point point, Rectangle area, Size size, int margin)
        {
            var inset = Rectangle.Inflate(area, -margin, -margin);
            var result = Clamp(point, inset, size);
            int left = Math.Abs(point.X - inset.Left), right = Math.Abs(inset.Right - point.X - size.Width);
            int top = Math.Abs(point.Y - inset.Top), bottom = Math.Abs(inset.Bottom - point.Y - size.Height);
            int distance = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
            if (distance == left) result.X = inset.Left;
            else if (distance == right) result.X = inset.Right - size.Width;
            else if (distance == top) result.Y = inset.Top;
            else result.Y = inset.Bottom - size.Height;
            return result;
        }
        public void RestorePosition(int x, int y)
        {
            var point = x == Int32.MinValue || y == Int32.MinValue ? new Point(Screen.PrimaryScreen.WorkingArea.Right - 1, Screen.PrimaryScreen.WorkingArea.Bottom - 1) : new Point(x, y);
            var area = Screen.FromPoint(point).WorkingArea;
            restingPosition = EdgePosition(point, area, new Size(Px(56), Px(14)), Px(3));
            SetExpanded(expanded, true);
        }
        void DockToEdge()
        {
            var center = new Point(Left + Width / 2 - Px(28), Top + Height / 2 - Px(7));
            restingPosition = EdgePosition(center, Screen.FromPoint(center).WorkingArea, new Size(Px(56), Px(14)), Px(3));
            SetExpanded(false, true);
            if (PositionSaved != null) PositionSaved(restingPosition);
        }
        void DragStart(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return; dragging = true; dragStart = Cursor.Position; locationStart = Location; ((Control)sender).Capture = true;
        }
        void CaptureChanged(object sender, EventArgs e)
        {
            if (dragging && !((Control)sender).Capture) { dragging = false; DockToEdge(); }
        }
        void DragMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return; var point = Cursor.Position;
            Location = Clamp(new Point(locationStart.X + point.X - dragStart.X, locationStart.Y + point.Y - dragStart.Y), Screen.FromPoint(point).WorkingArea, Size);
        }
        void DragEnd(object sender, MouseEventArgs e)
        {
            if (!dragging) return; dragging = false; ((Control)sender).Capture = false;
            DockToEdge();
        }
        public void UpdateState(bool writing, bool processing, bool notesRecording, bool noteBusy, bool isMeeting, string message)
        {
            write.Text = processing ? "Refining" : "Dictate"; note.Text = "Note"; meeting.Text = "Meeting"; more.Text = "Open app";
            cancelAvailable = writing || processing; more.Glyph = cancelAvailable ? "\uE711" : "\uE8A7"; more.AccessibleName = cancelAvailable ? "Cancel dictation" : "Open Dotflow"; hints.SetToolTip(more, cancelAvailable ? "Cancel dictation" : "Open Dotflow");
            write.Active = writing; note.Active = notesRecording && !isMeeting; meeting.Active = notesRecording && isMeeting;
            write.Enabled = !noteBusy && !processing; note.Enabled = !writing && !processing && (!noteBusy || notesRecording && !isMeeting); meeting.Enabled = !writing && !processing && (!noteBusy || notesRecording && isMeeting);
            hints.SetToolTip(write, writing ? "Stop dictation" : processing ? "Refining text" : "Dictation");
            hints.SetToolTip(note, note.Active ? "Stop personal note" : "Personal notes");
            hints.SetToolTip(meeting, meeting.Active ? "Stop meeting" : "Meeting notes");
            grip.Invalidate(); write.Invalidate(); note.Invalidate(); meeting.Invalidate();
        }
        protected override void Dispose(bool disposing) { if (disposing) { hover.Dispose(); hints.Dispose(); fade.Dispose(); Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= DisplaysChanged; } base.Dispose(disposing); }
    }
}
