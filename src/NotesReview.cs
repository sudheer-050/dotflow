using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LocalDictation
{
    static class NotesSummary
    {
        public static string Generate(NoteDocument note)
        {
            var speech = note.Segments.Where(s => s.Complete && String.IsNullOrEmpty(s.Error) && !String.IsNullOrWhiteSpace(s.RawText ?? s.Text)).OrderBy(s => s.StartFrame).ToList();
            if (speech.Count == 0) return "No recognized speech to excerpt yet.";
            Func<NoteSegment, string> quote = s => Quote(note, s);
            var decisions = speech.Where(s => Regex.IsMatch(s.RawText ?? s.Text, @"(?i)\b(?:agree\w*|decid\w*|decision|approved?|chosen|choose)\b")).Take(8).ToList();
            var actions = speech.Where(s => Regex.IsMatch(s.RawText ?? s.Text, @"(?i)\b(?:will|should|need|tomorrow|deadline|action|next|follow up)\b")).Take(8).ToList();
            return "Local review - original source excerpts, not an AI summary. Check the recording; categories use keywords.\r\n\r\nOverview\r\n" + String.Join("\r\n\r\n", speech.Take(6).Select(quote)) +
                "\r\n\r\nPossible decisions\r\n" + (decisions.Count == 0 ? "No matching statement found." : String.Join("\r\n\r\n", decisions.Select(quote))) +
                "\r\n\r\nPossible follow-ups\r\n" + (actions.Count == 0 ? "No matching statement found." : String.Join("\r\n\r\n", actions.Select(quote)));
        }
        static string Quote(NoteDocument note, NoteSegment s) { return "[" + TimeSpan.FromSeconds((s.TranscriptStartFrame ?? s.StartFrame) / 16000.0).ToString(@"hh\:mm\:ss") + "] " + NoteStore.Speaker(note, s) + "\r\n" + (s.RawText ?? s.Text); }
        public static string Answer(NoteDocument note, string query)
        {
            // ponytail: lexical retrieval quotes evidence; no semantic inference or invented owners/deadlines.
            var stop = new[] { "what", "when", "where", "who", "why", "how", "the", "did", "does", "was", "were", "are", "and", "for", "about", "with", "this", "that", "have", "will" };
            var terms = Regex.Matches(query ?? "", @"[\p{L}\p{N}]+").Cast<Match>().Select(m => m.Value.ToLowerInvariant()).Where(w => w.Length > 2 && !stop.Contains(w)).Distinct().ToArray();
            if (terms.Length == 0) return "Use a specific topic, name or phrase to find original statements.";
            var ranked = note.Segments.Where(s => s.Complete && String.IsNullOrEmpty(s.Error) && !String.IsNullOrWhiteSpace(s.RawText ?? s.Text)).Select(s => new { Segment = s, Score = terms.Count(term => Regex.IsMatch(s.RawText ?? s.Text, @"\b" + Regex.Escape(term) + @"\b", RegexOptions.IgnoreCase)) }).Where(x => x.Score > 0).OrderByDescending(x => x.Score).ThenBy(x => x.Segment.StartFrame).Take(5).ToList();
            return ranked.Count == 0 ? "No matching recognized speech found. Try different keywords or review the audio." : "Matching source statements - search results, not inferred answers.\r\n\r\n" + String.Join("\r\n\r\n", ranked.Select(x => Quote(note, x.Segment)));
        }
    }
    sealed partial class NotesPane
    {
        readonly HubButton pauseNote = new HubButton { Text = "Pause", Dock = DockStyle.Fill };
        readonly NotesPlayback player = new NotesPlayback();
        readonly TextBox findText = new TextBox { Width = 160, AccessibleName = "Find in conversation" };
        readonly TextBox excerptText = new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Vertical };
        readonly TrackBar playPosition = new TrackBar { Width = 120, Minimum = 0, Maximum = 1, TickStyle = TickStyle.None, Height = 28 };
        readonly ComboBox audioSource = Choice(), speed = Choice(), sort = Choice(), filter = Choice();
        readonly Label playTime = new Label { AutoSize = true, Padding = new Padding(0, 5, 0, 0) };
        string libraryQuery = "";
        bool playerOpen;
        TextBox librarySearch;
        readonly FlowLayoutPanel emptyState = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12) };
        bool writingFresh;
        internal System.Collections.Generic.IEnumerable<NoteDocument> RecentEntries { get { return documents.OrderByDescending(n => n.CreatedUtc).ToArray(); } }
        internal int VisibleNotes { get { return history.Items.Count; } }
        internal void SearchFor(string query) { librarySearch.Text = query; }
        internal void SelectNote(string id) { Select(documents.FirstOrDefault(n => n.Id == id)); }
        internal void VerifyReviewLayout()
        {
            if (selected == null || written.Text != selected.WrittenNotes || transcript.Text != NoteStore.Transcript(selected) || !transcript.ReadOnly) throw new InvalidOperationException("Review fields lost content or read-only state.");
            if (!written.Visible || record.Bottom > record.Parent.ClientSize.Height || mode.Bottom > mode.Parent.ClientSize.Height || record.Height < 20 || mode.Height < 18) throw new InvalidOperationException("Review controls are clipped or hidden.");
        }
        public void NewWrittenNote() { writingFresh = true; Select(null); tabs.SelectedIndex = 0; written.Focus(); }
        static void SelectEndpoint(ComboBox box, string id)
        {
            for (int i = 0; i < box.Items.Count; i++) if (((AudioEndpoint)box.Items[i]).Id == id) { box.SelectedIndex = i; return; }
        }
        public void ReloadLibrary()
        {
            if (Busy) return; SaveEdits(); documents.Clear(); documents.AddRange(store.List()); selected = null; Refilter(); Select(documents.FirstOrDefault());
        }
        void Refilter()
        {
            SaveEdits(); populating = true; history.Items.Clear();
            foreach (var note in Sorted(documents, config.NotesSort))
            {
                if (filter.SelectedIndex == 1 && note.Kind == "Meeting" || filter.SelectedIndex == 2 && note.Kind != "Meeting") continue;
                string searchable; lock (note) searchable = note.Title + " " + note.UserName + " " + note.Kind + " " + note.WrittenNotes + " " + note.Tags + " " + String.Join(" ", note.Participants) + " " + NoteStore.Transcript(note);
                if (searchable.IndexOf(libraryQuery, StringComparison.OrdinalIgnoreCase) >= 0) history.Items.Add(note);
            }
            history.SelectedItem = selected; populating = false;
        }
        internal static System.Collections.Generic.IEnumerable<NoteDocument> Sorted(System.Collections.Generic.IEnumerable<NoteDocument> notes, string order)
        {
            if (order == "Title") return notes.OrderBy(n => n.Title, StringComparer.CurrentCultureIgnoreCase).ThenByDescending(n => n.CreatedUtc);
            if (order == "Oldest") return notes.OrderBy(n => n.CreatedUtc);
            if (order == "Longest") return notes.OrderByDescending(n => n.DurationSeconds).ThenByDescending(n => n.CreatedUtc);
            return notes.OrderByDescending(n => n.CreatedUtc);
        }
        void BuildReviewTools(TableLayoutPanel library, TableLayoutPanel workspace, TabPage noteTab, TabPage transcriptTab, TextBox search, HubButton copy, HubButton export)
        {
            librarySearch = search;
            emptyState.Controls.Add(new Label { Text = "Capture what matters. Choose a starting point.", AutoSize = true, MaximumSize = new Size(260, 0), Margin = new Padding(0, 0, 0, 18) });
            Action<string, Action> start = (label, action) => { var button = new HubButton { Text = label, Width = 220, Height = 38 }; button.Click += (s, e) => action(); emptyState.Controls.Add(button); };
            start("Write a note", NewWrittenNote); start("Record a personal note", () => Toggle(preferredSource, "Note")); start("Record a meeting", () => Toggle(config.MeetingSource, "Meeting"));
            noteTab.Controls.Add(emptyState); emptyState.BringToFront();
            library.RowCount = 6; library.RowStyles.Clear(); foreach (int height in new[] { 28, 34, 34, 34, -1, 42 }) library.RowStyles.Add(new RowStyle(height < 0 ? SizeType.Percent : SizeType.Absolute, height < 0 ? 100 : height));
            var fresh = library.GetControlFromPosition(0, 3); library.SetRow(fresh, 5); library.SetRow(history, 4);
            filter.Items.AddRange(new object[] { "All notes & meetings", "Personal notes", "Meetings" }); filter.SelectedIndex = 0; library.Controls.Add(filter, 0, 2); filter.SelectedIndexChanged += (s, e) => Refilter();
            sort.Items.AddRange(new object[] { "Newest", "Oldest", "Title", "Longest" }); sort.SelectedItem = config.NotesSort; sort.AccessibleName = "Sort notes"; library.Controls.Add(sort, 0, 3);
            sort.SelectedIndexChanged += (s, e) => { config.NotesSort = sort.Text; try { config.Save(); } catch (Exception ex) { uiError = "Sort applied for this session. Could not save preference: " + ex.Message; } Refilter(); };
            history.DrawMode = DrawMode.OwnerDrawFixed; history.ItemHeight = 58;
            history.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return; var note = (NoteDocument)history.Items[e.Index];
                using (var fill = new SolidBrush((e.State & DrawItemState.Selected) != 0 ? ThemeColors.Selected : ThemeColors.Paper)) e.Graphics.FillRectangle(fill, e.Bounds);
                Color ink = SystemInformation.HighContrast && (e.State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : ThemeColors.Ink;
                TextRenderer.DrawText(e.Graphics, note.Title, Font, new Rectangle(e.Bounds.Left + 7, e.Bounds.Top + 5, e.Bounds.Width - 14, 25), ink, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                DateTime created; DateTime.TryParse(note.CreatedUtc, out created);
                string duration = note.DurationSeconds > 0 ? " | " + TimeSpan.FromSeconds(note.DurationSeconds).ToString(@"hh\:mm\:ss") : "";
                string detail = (note.Kind ?? "Note") + " | " + created.ToLocalTime().ToString("MMM d") + duration + " | " + note.Status;
                using (var small = new Font(Font.FontFamily, 8)) TextRenderer.DrawText(e.Graphics, detail, small, new Rectangle(e.Bounds.Left + 7, e.Bounds.Top + 31, e.Bounds.Width - 14, 22), SystemInformation.HighContrast ? ink : ThemeColors.Muted, TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                e.DrawFocusRectangle();
            };
            pauseNote.Click += (s, e) => { if (session != null) session.TogglePause(); RefreshState(); };
            var find = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, WrapContents = false };
            var next = new Button { Text = "Find next", AutoSize = true }; find.Controls.Add(findText); find.Controls.Add(next);
            next.Click += (s, e) => FindNext(); findText.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; FindNext(); } };
            transcriptTab.Controls.Add(find); find.BringToFront();
            transcript.DoubleClick += (s, e) =>
            {
                int line = transcript.GetLineFromCharIndex(transcript.SelectionStart);
                for (int i = Math.Min(line, transcript.Lines.Length - 1); i >= 0; i--)
                { var match = Regex.Match(transcript.Lines[i], @"^\[(\d+:\d+:\d+)\]"); TimeSpan time; if (match.Success && TimeSpan.TryParse(match.Groups[1].Value, out time)) { var segment = selected.Segments.Where(x => x.Complete && String.IsNullOrEmpty(x.Error) && !String.IsNullOrWhiteSpace(x.RawText ?? x.Text)).OrderBy(x => x.StartFrame).ThenBy(x => x.Source).FirstOrDefault(x => (int)((x.TranscriptStartFrame ?? x.StartFrame) / 16000) == (int)time.TotalSeconds && transcript.Lines[i].Contains(NoteStore.Speaker(selected, x))); if (segment != null) audioSource.SelectedIndex = segment.Source == "Computer" ? 1 : 0; StartPlayback((int)time.TotalMilliseconds); break; } }
            };
            var excerptTab = new TabPage("Local review"); var generate = new Button { Text = "Create overview, decisions & follow-ups (source quotes)", Dock = DockStyle.Top, Height = 34 };
            generate.Click += (s, e) => { if (selected == null) return; lock (selected) { selected.Summary = NotesSummary.Generate(selected); store.Save(selected); excerptText.Text = selected.Summary; } };
            excerptTab.Controls.Add(excerptText); excerptTab.Controls.Add(generate); tabs.TabPages.Add(excerptTab);
            var questions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, WrapContents = false }; var question = new TextBox { Width = 230, AccessibleName = "Search meeting statements" }; var ask = new Button { Text = "Find an answer in source text", AutoSize = true };
            Action answer = () => { if (selected != null) lock (selected) excerptText.Text = NotesSummary.Answer(selected, question.Text); };
            ask.Click += (s, e) => answer(); question.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; answer(); } }; questions.Controls.AddRange(new Control[] { question, ask }); excerptTab.Controls.Add(questions); questions.BringToFront(); generate.BringToFront();
            var playback = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, WrapContents = false, AutoScroll = true };
            audioSource.Width = 105; audioSource.Height = 28; audioSource.Items.AddRange(new object[] { "Microphone", "Computer" }); audioSource.SelectedIndex = 0;
            var play = new Button { Text = "Play", AutoSize = true }; var pause = new Button { Text = "Pause", AutoSize = true };
            speed.Width = 65; speed.Items.AddRange(new object[] { "0.75x", "1x", "1.25x", "1.5x", "2x" }); speed.SelectedIndex = 1;
            playback.Controls.AddRange(new Control[] { audioSource, play, pause, playPosition, speed, playTime });
            transcriptTab.Controls.Add(playback); playback.BringToFront();
            play.Click += (s, e) => StartPlayback(); pause.Click += (s, e) => { try { player.Pause(); } catch (Exception ex) { uiError = ex.Message; } };
            audioSource.SelectedIndexChanged += (s, e) => { player.Dispose(); playerOpen = false; };
            speed.SelectedIndexChanged += (s, e) => { try { if (playerOpen) player.Speed(new[] { 0.75, 1.0, 1.25, 1.5, 2.0 }[speed.SelectedIndex]); } catch (Exception ex) { uiError = "Playback speed unavailable: " + ex.Message; } };
            playPosition.MouseUp += (s, e) => StartPlayback(playPosition.Value);
            var actions = (TableLayoutPanel)export.Parent; actions.ColumnCount = 4; actions.ColumnStyles.Clear();
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
            var more = new HubButton { Text = "More", Dock = DockStyle.Fill }; more.Click += (s, e) => ShowFilesMenu(more); actions.Controls.Add(more, 3, 0);
            var meters = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 22, ColumnCount = 4 };
            meters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34)); meters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); meters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70)); meters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            meters.Controls.Add(new Label { Text = "Mic", Dock = DockStyle.Fill }, 0, 0); meters.Controls.Add(micMeter, 1, 0); meters.Controls.Add(new Label { Text = "Output", Dock = DockStyle.Fill }, 2, 0); meters.Controls.Add(outputMeter, 3, 0);
            var captureState = new Panel { Dock = DockStyle.Fill }; workspace.Controls.Remove(status); status.Dock = DockStyle.Top; status.Height = 26; captureState.Controls.Add(status); captureState.Controls.Add(meters); workspace.RowStyles[3].Height = 52; workspace.Controls.Add(captureState, 0, 3); meters.BringToFront();
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            if (keys == (Keys.Control | Keys.F)) { tabs.SelectedIndex = 1; findText.Focus(); return true; }
            if (keys == Keys.F3) { FindNext(); return true; }
            if (keys == (Keys.Control | Keys.Shift | Keys.F)) { librarySearch.Focus(); return true; }
            return base.ProcessCmdKey(ref message, keys);
        }
        void FindNext()
        {
            string query = findText.Text; if (query.Length == 0) return;
            int start = Math.Min(transcript.TextLength, transcript.SelectionStart + Math.Max(1, transcript.SelectionLength));
            int index = transcript.Text.IndexOf(query, start, StringComparison.OrdinalIgnoreCase); if (index < 0) index = transcript.Text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (index < 0) { uiError = "No matches in this conversation."; return; }
            transcript.Select(index, query.Length); transcript.ScrollToCaret(); transcript.Focus();
        }
        void StartPlayback(int position = -1)
        {
            if (selected == null) return;
            try
            {
                if (Busy && ReferenceEquals(selected, session.Document)) { uiError = "Playback is available after this recording is saved."; return; }
                if (!playerOpen) { player.Open(store.AudioPath(selected, audioSource.SelectedIndex == 0 ? "Microphone" : "Computer")); playerOpen = true; }
                player.Play(position); uiError = "";
            }
            catch (Exception ex) { playerOpen = false; uiError = ex.Message; }
        }
        void UpdateReviewState()
        {
            emptyState.Visible = selected == null && !writingFresh;
            if (selected != null) { playPosition.Maximum = Math.Max(1, (int)Math.Min(Int32.MaxValue - 1, selected.DurationSeconds * 1000)); if (playerOpen) playPosition.Value = Math.Min(playPosition.Maximum, player.Position); playTime.Text = TimeSpan.FromMilliseconds(playerOpen ? player.Position : 0).ToString(@"mm\:ss"); }
            if (Busy && session.Document != null && !ReferenceEquals(selected, session.Document)) status.Text = "Active: " + session.Document.Title + " - " + session.Document.Status;
            if (selected != null)
            {
                int failures; lock (selected) failures = selected.Segments.Count(s => !s.Complete || !String.IsNullOrEmpty(s.Error));
                if (!Recording && failures > 0) status.Text += " | " + failures + " missing sections at " + String.Join(", ", selected.Segments.Where(s => !s.Complete || !String.IsNullOrEmpty(s.Error)).Take(3).Select(s => TimeSpan.FromSeconds(s.StartFrame / 16000.0).ToString(@"mm\:ss"))) + " - audio kept; Retry.";
            }
        }
        void ResumeSelected()
        {
            if (selected == null || Busy || CanStart != null && !CanStart()) return;
            try
            {
                foreach (string source in selected.Mode == "Both" ? new[] { "Microphone", "Computer" } : new[] { selected.Mode }) if (!File.Exists(store.AudioPath(selected, source))) throw new IOException("The original audio is missing. Start a new note instead.");
                SaveEdits(); if (session != null) session.Dispose(); session = new NotesSession(store, config); session.Changed = SessionChanged;
                session.Resume(selected, microphone.SelectedItem == null ? "" : ((AudioEndpoint)microphone.SelectedItem).Id, output.SelectedItem == null ? "" : ((AudioEndpoint)output.SelectedItem).Id); RefreshState();
            }
            catch (Exception ex) { uiError = ex.Message; RefreshState(); }
        }
        internal static void SetSpeakerLabels(NoteDocument note, string[] labels)
        {
            if (labels.Length != note.Segments.Count) throw new InvalidDataException("The recording changed. Reopen speaker labels.");
            if (labels.Any(label => label != null && (label.Length > 80 || label.Any(Char.IsControl)))) throw new InvalidDataException("Use speaker names up to 80 characters, without line breaks.");
            for (int i = 0; i < labels.Length; i++) note.Segments[i].SpeakerLabel = (labels[i] ?? "").Trim();
        }
        void EditSpeakerLabels()
        {
            if (selected == null || Busy && ReferenceEquals(selected, session.Document)) return;
            var note = selected;
            using (var dialog = new Form { Text = "Speaker labels - original words stay read-only", Size = new Size(820, 540), StartPosition = FormStartPosition.CenterParent, Font = Font, Icon = AppLogo.Icon })
            {
                var hint = new Label { Text = "Correct names for each recognized section. Leave blank to keep its audio-source label. This does not identify voices automatically.", Dock = DockStyle.Top, Height = 54, Padding = new Padding(10) };
                var grid = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AccessibleName = "Speaker labels" };
                grid.Columns.Add("Time", "Time"); grid.Columns.Add("Source", "Audio source"); grid.Columns.Add("Words", "Original words (read-only)"); grid.Columns.Add("Speaker", "Speaker label");
                for (int c = 0; c < 3; c++) grid.Columns[c].ReadOnly = true;
                grid.Columns[0].FillWeight = 15; grid.Columns[1].FillWeight = 20; grid.Columns[2].FillWeight = 50; grid.Columns[3].FillWeight = 25;
                lock (note) foreach (var segment in note.Segments) grid.Rows.Add(TimeSpan.FromSeconds((segment.TranscriptStartFrame ?? segment.StartFrame) / 16000.0).ToString(@"hh\:mm\:ss"), segment.Source, segment.RawText ?? segment.Text, segment.SpeakerLabel);
                var save = new Button { Text = "Save speaker labels", Dock = DockStyle.Bottom, Height = 42 };
                save.Click += (s, e) =>
                {
                    grid.EndEdit(); string[] previous = note.Segments.Select(x => x.SpeakerLabel).ToArray();
                    try { lock (note) { SetSpeakerLabels(note, grid.Rows.Cast<DataGridViewRow>().Select(row => Convert.ToString(row.Cells[3].Value)).ToArray()); store.Save(note); } transcript.Text = NoteStore.Transcript(note); dialog.Close(); }
                    catch (Exception ex) { lock (note) SetSpeakerLabels(note, previous); MessageBox.Show(dialog, ex.Message, "Could not save speaker labels"); }
                };
                dialog.Controls.Add(grid); dialog.Controls.Add(hint); dialog.Controls.Add(save); ThemeColors.Apply(dialog, config.Theme); dialog.ShowDialog(FindForm());
            }
        }
    }
}
