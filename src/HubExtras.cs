using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LocalDictation
{
    sealed partial class SettingsForm
    {
        readonly ListBox recentNotes = new ListBox { BorderStyle = BorderStyle.None, DisplayMember = "Title", Font = new Font("Segoe UI", 9), IntegralHeight = false };
        TabControl settingsTabs;
        Label readinessStatus;
        readonly ComboBox themeChoice = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly CheckBox toolbarChoice = new CheckBox { Text = "Show the edge toolbar", AutoSize = true };
        readonly CheckBox indicatorChoice = new CheckBox { Text = "Show the separate voice animation while recording", AutoSize = true };
        readonly CheckBox motionChoice = new CheckBox { Text = "Reduce motion", AutoSize = true };
        readonly CheckBox noiseChoice = new CheckBox { Text = "Suppress quiet background noise in notes", AutoSize = true };
        readonly TextBox updateUrl = new TextBox(), updatePublisher = new TextBox();
        readonly CheckBox updateOnStart = new CheckBox { Text = "Check the configured signed-update feed on startup", AutoSize = true };
        bool dirty;
        System.Collections.Generic.List<PersonalRule> pendingRules;
        public Action BeginDictation, OpenRecovery;
        public Action DismissIssues;
        string[] issues = new string[0];
        HubButton issuesButton;
        public void SetIssues(System.Collections.Generic.IEnumerable<string> entries) { issues = entries.ToArray(); if (issuesButton != null) issuesButton.Text = issues.Length == 0 ? "Issues & recovery" : "Issues (" + issues.Length + ")"; }
        public void ShowIssues()
        {
            using (var dialog = new Form { Text = "Issues & recovery", Size = new Size(680, 400), StartPosition = FormStartPosition.CenterParent, Font = Font, Icon = AppLogo.Icon })
            {
                var text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text = issues.Length == 0 ? "No unacknowledged issues this session." : String.Join("\r\n\r\n", issues) };
                var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48 };
                var recover = new Button { Text = "Recover last dictation", AutoSize = true }; recover.Click += (s, e) => { dialog.Close(); if (OpenRecovery != null) OpenRecovery(); };
                var acknowledge = new Button { Text = "Acknowledge issues", AutoSize = true }; acknowledge.Click += (s, e) => { if (DismissIssues != null) DismissIssues(); dialog.Close(); };
                actions.Controls.AddRange(new Control[] { recover, acknowledge }); dialog.Controls.Add(text); dialog.Controls.Add(actions); ThemeColors.Apply(dialog, source.Theme); dialog.ShowDialog(this);
            }
        }
        public void SetReadiness(string value) { if (readinessStatus != null) readinessStatus.Text = value; }
        void MarkDirty() { dirty = true; savePinned.Text = "Save changes *"; }
        void WireDirtyState()
        {
            updateUrl.TextChanged += (s, e) => MarkDirty(); updatePublisher.TextChanged += (s, e) => MarkDirty();
            updateOnStart.CheckedChanged += (s, e) => MarkDirty();
            foreach (var box in new[] { cleanup, autoPaste, liveWriting, startup, toolbarChoice, indicatorChoice, motionChoice, noiseChoice }) box.CheckedChanged += (s, e) => MarkDirty();
            foreach (var box in new[] { mic, noteShortcut, noteSource, meetingSource, pasteMethod, themeChoice }) box.SelectedIndexChanged += (s, e) => MarkDirty();
        }
        bool ConfirmUnsaved()
        {
            if (!dirty || !CloseToTray) return true;
            var result = MessageBox.Show(this, "Save your preference changes before closing?", "Unsaved preferences", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.Yes) { SavePreferences(); return !dirty; }
            selectedShortcut = source.Hotkey; hotkey.Text = selectedShortcut;
            cleanup.Checked = source.CleanupEnabled; autoPaste.Checked = source.AutoPaste; liveWriting.Checked = source.LiveWriting; startup.Checked = source.StartWithWindows;
            toolbarChoice.Checked = source.ShowToolbar; indicatorChoice.Checked = source.ShowVoiceIndicator; motionChoice.Checked = source.ReducedMotion; noiseChoice.Checked = source.ReduceNoise; themeChoice.SelectedItem = source.Theme;
            noteShortcut.SelectedItem = source.NotesHotkey; noteSource.SelectedIndex = Array.IndexOf(new[] { "Microphone", "Computer", "Both" }, source.NotesSource); meetingSource.SelectedIndex = Array.IndexOf(new[] { "Microphone", "Computer", "Both" }, source.MeetingSource);
            mic.SelectedIndex = Math.Max(0, mic.Items.IndexOf(source.Microphone)); pasteMethod.SelectedIndex = Array.IndexOf(new[] { "Auto", "CtrlV", "CtrlShiftV", "ShiftInsert" }, source.PasteMethod);
            updateUrl.Text = source.UpdateManifestUrl; updatePublisher.Text = source.UpdatePublisherThumbprint;
            updateOnStart.Checked = source.CheckUpdatesOnStart;
            pendingRules = source.Copy().Personalization; dirty = false; savePinned.Text = "Save preferences"; return true;
        }
        Panel SettingsTab(string name)
        {
            var tab = new TabPage(name) { Padding = new Padding(12), BackColor = Paper };
            settingsTabs.TabPages.Add(tab); var panel = new Panel { Size = new Size(700, 520), Dock = DockStyle.Fill, AutoScroll = true, BackColor = Paper }; tab.Controls.Add(panel);
            panel.Resize += (s, e) => { float scale; using (var g = panel.CreateGraphics()) scale = g.DpiX / 96f; foreach (Control child in panel.Controls) if (child is Label || (child.Anchor & AnchorStyles.Right) != 0) child.Width = Math.Max(1, panel.ClientSize.Width - child.Left - (int)(20 * scale)); };
            return panel;
        }
        void Setting(Panel parent, Control control, int top, int height = 32)
        {
            control.SetBounds(20, top, 630, height); control.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; parent.Controls.Add(control);
        }
        void BuildOrganizedPreferences()
        {
            var p = pages[2]; Heading(p, "Preferences", "Recording, writing and storage - organized in one place.");
            settingsTabs = new TabControl { Left = 40, Top = 112, Width = 760, Height = 580, Font = Font }; p.Controls.Add(settingsTabs);
            var recording = SettingsTab("Recording");
            Label(recording, "Hold-to-write shortcut", 20, 16, 620, 28, 12, true, Ink); Setting(recording, hotkey, 54, 40);
            Label(recording, "Press a key or combination. Modifier keys can delay live writing until release.", 20, 104, 620, 42, 9, false, Muted);
            Label(recording, "Microphone", 20, 154, 620, 25, 11, true, Ink); Setting(recording, mic, 186);
            testMic.SetBounds(20, 238, 160, 36); recording.Controls.Add(testMic); meter.SetBounds(200, 250, 270, 12); recording.Controls.Add(meter);
            micStatus.SetBounds(20, 286, 620, 44); recording.Controls.Add(micStatus);
            Button(recording, "Windows microphone permissions", 20, 342, 294, (s, e) => OpenPermissionSettings());
            testStt.Text = "Check speech engine"; testStt.SetBounds(332, 342, 200, 38); recording.Controls.Add(testStt);
            Button(recording, "Repair speech installation", 20, 394, 260, (s, e) => { var setup = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uninstall.exe"); if (File.Exists(setup)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = setup, UseShellExecute = true }); else MessageBox.Show(this, "Run the latest Setup executable to repair the speech files."); });
            pendingRules = source.Copy().Personalization;
            var writing = SettingsTab("Writing");
            Setting(writing, cleanup, 22); Setting(writing, autoPaste, 66); Setting(writing, liveWriting, 110);
            Label(writing, "Paste method", 20, 170, 600, 26, 11, true, Ink); Setting(writing, pasteMethod, 206);
            Button(writing, "Dictionary & snippets", 20, 270, 248, (s, e) => EditRules());
            Label(writing, "Clean dictation removes fillers. Turn it off for verbatim writing.\nDictionary and snippets apply only to dictation, never to recorded conversations.", 20, 334, 620, 70, 10, false, Muted);
            var noteSettings = SettingsTab("Notes");
            Label(noteSettings, "Recording shortcut - select one", 20, 20, 620, 28, 11, true, Ink); Setting(noteSettings, noteShortcut, 58);
            Label(noteSettings, "Personal-note audio", 20, 120, 620, 28, 11, true, Ink); Setting(noteSettings, noteSource, 158);
            Label(noteSettings, "Meeting audio", 20, 220, 620, 28, 11, true, Ink); Setting(noteSettings, meetingSource, 258); Setting(noteSettings, noiseChoice, 324);
            Label(noteSettings, "The shortcut toggles personal notes. Start meetings from Home or the hover toolbar.\nNoise suppression reduces quiet sound; it does not guarantee removal of music or every background voice.", 20, 376, 620, 74, 9, false, Muted);
            var appearance = SettingsTab("App");
            themeChoice.Items.AddRange(new object[] { "System", "Light", "Dark" }); themeChoice.SelectedItem = source.Theme;
            Label(appearance, "Appearance", 20, 20, 620, 28, 11, true, Ink); Setting(appearance, themeChoice, 58);
            toolbarChoice.Checked = source.ShowToolbar; indicatorChoice.Checked = source.ShowVoiceIndicator; motionChoice.Checked = source.ReducedMotion; noiseChoice.Checked = source.ReduceNoise;
            Setting(appearance, toolbarChoice, 124); Setting(appearance, indicatorChoice, 170); Setting(appearance, motionChoice, 216); Setting(appearance, startup, 262);
            Label(appearance, "The toolbar docks along any edge. Hover reveals icons; hover over an icon for its name.\nKeyboard users can open the Windows tray action menu without changing the target text field.", 20, 324, 620, 76, 10, false, Muted);
            var storage = SettingsTab("Storage");
            Label(storage, "Keep control of your local data", 20, 20, 620, 32, 14, true, Ink);
            Label(storage, "Recordings, deleted notes and download cache use disk space.\nBack up notes before moving them. Conversation exports can include private annotations.\nSpeech and notes stay on this PC; no account or cloud subscription is required.", 20, 78, 620, 110, 10, false, Muted);
            Button(storage, "Storage & recently deleted", 20, 220, 284, (s, e) => { if (notes != null && !notes.Busy) { NotesStorageDialog.Show(this, notes.Store, source); notes.ReloadLibrary(); } else MessageBox.Show(this, "Finish the current recording/transcription before managing storage."); });
            var about = SettingsTab("About");
            Label(about, "Local Dictation 0.6.1", 20, 20, 620, 38, 18, true, Ink);
            Label(about, "Windows x64 | English | Vosk small-en-us 0.15\nOffline speech after one-time installation.\n\nThis release adds speech recovery, organized settings, richer notes,\nplayback, scoped exports, storage recovery, dictionary and snippets.\n\nConversation text is read-only in the app. External document protections\nare convenience restrictions, not proof that a copy was never changed.", 20, 82, 630, 220, 10, false, Muted);
            Button(about, "Open release notes", 20, 336, 220, (s, e) => ShowReleaseNotes());
            Button(about, "Check for updates", 260, 336, 220, async (s, e) => await ReleaseUpdates.Check(this, source));
            Label(about, "Advanced: HTTPS update manifest URL", 20, 398, 620, 26, 9, false, Muted); Setting(about, updateUrl, 432); updateUrl.Text = source.UpdateManifestUrl;
            Label(about, "Trusted publisher certificate thumbprint", 20, 482, 620, 26, 9, false, Muted); Setting(about, updatePublisher, 516); updatePublisher.Text = source.UpdatePublisherThumbprint;
            Setting(about, updateOnStart, 566); updateOnStart.Checked = source.CheckUpdatesOnStart;
        }
        void EditRules()
        {
            var draft = source.Copy(); draft.Personalization = pendingRules;
            PersonalRule.Edit(this, draft, () => { pendingRules = draft.Personalization; MarkDirty(); });
        }
        async void TestAudioFile()
        {
            using (var file = new OpenFileDialog { Filter = "PCM WAV audio|*.wav", Title = "Choose a 16 kHz mono PCM16 speech sample" })
            {
                if (file.ShowDialog(this) != DialogResult.OK) return;
                if (new FileInfo(file.FileName).Length > 20 * 1024 * 1024) { MessageBox.Show(this, "Use a test clip smaller than 20 MB."); return; }
                try
                {
                    byte[] wav = File.ReadAllBytes(file.FileName); sttStatus.Text = "Transcribing the selected local test clip...";
                    string result = await System.Threading.Tasks.Task.Run(() => SpeechEngine.Transcribe(source, wav));
                    if (!IsDisposed) { UpdateTranscript(result, result); sttStatus.Text = "Local test complete. Compare the recognized words against your reference transcript."; }
                }
                catch (Exception ex) { if (!IsDisposed) sttStatus.Text = ex.Message; }
            }
        }
        internal void SelectSettingsCategory(string name) { foreach (TabPage tab in settingsTabs.TabPages) if (tab.Text == name) { settingsTabs.SelectedTab = tab; break; } }
        static void OpenPermissionSettings() { try { System.Diagnostics.Process.Start("ms-settings:privacy-microphone"); } catch (Exception ex) { MessageBox.Show(ex.Message, "Open microphone settings"); } }
        void ShowReleaseNotes() { MessageBox.Show(this, "0.6.1\n\nRecover unfinished dictation in memory.\nSafer shutdown and clipboard handling.\nDirect Home actions and grouped settings.\nMeeting pause/resume, playback and search.\nRecently deleted, backup and restore.\nThemes, local dictionary and snippets.\nScoped protected exports and separate summaries.", "Release notes"); }
        public void ShowFirstRunGuide() { FirstRunSetup(); source.SetupGuideSeen = true; source.Save(); }
        void FirstRunSetup()
        {
            using (var dialog = new Form { Text = "Check your setup", Size = new Size(570, 380), Font = Font, StartPosition = FormStartPosition.CenterParent, Icon = AppLogo.Icon })
            {
                string detail; bool available = SpeechEngine.Healthy(source, out detail);
                var label = new Label { Dock = DockStyle.Top, Height = 150, Padding = new Padding(16), Text = "1. Allow desktop microphone access in Windows.\n2. Choose a microphone and run its input test.\n3. Try a sentence in the Playground, then in another app.\n\nSpeech files: " + (available ? "Installed - English, offline" : detail) };
                var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), FlowDirection = FlowDirection.TopDown };
                var permission = new Button { Text = "Open microphone permissions", AutoSize = true }; permission.Click += (s, e) => OpenPermissionSettings();
                var audio = new Button { Text = "Choose and test microphone", AutoSize = true }; audio.Click += (s, e) => { dialog.Close(); ShowPage("Preferences"); settingsTabs.SelectedIndex = 0; };
                var practice = new Button { Text = "Practice a sentence", AutoSize = true }; practice.Click += (s, e) => { dialog.Close(); ShowPage("Playground"); };
                actions.Controls.AddRange(new Control[] { permission, audio, practice }); dialog.Controls.Add(actions); dialog.Controls.Add(label); ThemeColors.Apply(dialog, source.Theme); dialog.ShowDialog(this);
            }
        }
    }
}
