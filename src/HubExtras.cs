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
        readonly ComboBox writingStyle = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        HubButton recoverButton;
        Label toolbarStatus;
        public void SetToolbarStatus(string value) { if (toolbarStatus != null) toolbarStatus.Text = "Toolbar: " + value; }
        public void SetRecoveryAvailable(bool available) { if (recoverButton != null) recoverButton.Visible = available; }
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
        public void SetIssues(System.Collections.Generic.IEnumerable<string> entries) { issues = entries.ToArray(); if (issuesButton != null) { issuesButton.Text = "Issues (" + issues.Length + ")"; issuesButton.Visible = issues.Length > 0; } }
        public void ShowIssues()
        {
            using (var dialog = DialogUi.Create("Issues & recovery", new Size(680, 460)))
            {
                var text = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Text = issues.Length == 0 ? "No unacknowledged issues this session." : String.Join("\r\n\r\n", issues) };
                var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 110, WrapContents = true, AutoScroll = true };
                var recover = new Button { Text = "Recover last dictation", AutoSize = true }; recover.Click += (s, e) => { dialog.Close(); if (OpenRecovery != null) OpenRecovery(); };
                var acknowledge = new Button { Text = "Acknowledge issues", AutoSize = true }; acknowledge.Click += (s, e) => { if (DismissIssues != null) DismissIssues(); dialog.Close(); };
                recover.Visible = recoverButton != null && recoverButton.Visible;
                actions.Controls.Add(DialogUi.Button("Microphone permissions", (s, e) => OpenPermissionSettings()));
                actions.Controls.Add(DialogUi.Button("Meeting audio help", (s, e) => { dialog.Close(); ShowPage("Notes"); notes.ShowRecordingHelp(); }));
                actions.Controls.Add(DialogUi.Button("Typing help", (s, e) => { dialog.Close(); ShowPage("Help"); }));
                actions.Controls.AddRange(new Control[] { recover, acknowledge }); dialog.Controls.Add(text); dialog.Controls.Add(actions); ThemeColors.Apply(dialog, source.Theme); dialog.ShowDialog(this);
            }
        }
        public void SetReadiness(string value) { if (readinessStatus != null) readinessStatus.Text = value; }
        void MarkDirty() { dirty = true; savePinned.Text = "Save changes *"; }
        void WireDirtyState()
        {
            updateUrl.TextChanged += (s, e) => MarkDirty(); updatePublisher.TextChanged += (s, e) => MarkDirty();
            updateOnStart.CheckedChanged += (s, e) => MarkDirty(); writingStyle.SelectedIndexChanged += (s, e) => { MarkDirty(); RefreshCleaned(); };
            foreach (var box in new[] { quality, cleanup, autoPaste, liveWriting, startup, toolbarChoice, indicatorChoice, motionChoice, noiseChoice }) box.CheckedChanged += (s, e) => MarkDirty();
            foreach (var box in new[] { mic, noteShortcut, noteSource, meetingSource, pasteMethod, themeChoice }) box.SelectedIndexChanged += (s, e) => MarkDirty();
        }
        bool ConfirmUnsaved()
        {
            if (!dirty || !CloseToTray) return true;
            var result = MessageBox.Show(this, "Save your preference changes before closing?", "Unsaved preferences", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.Yes) { SavePreferences(); return !dirty; }
            selectedShortcut = source.Hotkey; hotkey.Text = selectedShortcut;
            quality.Checked = source.QualityEnabled; cleanup.Checked = source.CleanupEnabled; autoPaste.Checked = source.AutoPaste; liveWriting.Checked = source.LiveWriting; startup.Checked = source.StartWithWindows;
            toolbarChoice.Checked = source.ShowToolbar; indicatorChoice.Checked = source.ShowVoiceIndicator; motionChoice.Checked = source.ReducedMotion; noiseChoice.Checked = source.ReduceNoise; themeChoice.SelectedItem = source.Theme; writingStyle.SelectedItem = source.WritingStyle;
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
            settingsTabs = new AppTabs { Left = 40, Top = 112, Width = 760, Height = 580, Font = Font }; p.Controls.Add(settingsTabs);
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
            Setting(writing, cleanup, 22); Setting(writing, autoPaste, 66); Setting(writing, liveWriting, 110); Setting(writing, quality, 144);
            Label(writing, "Paste method", 20, 170, 600, 26, 11, true, Ink); Setting(writing, pasteMethod, 206);
            Label(writing, "Writing style", 20, 250, 600, 26, 11, true, Ink); writingStyle.Items.AddRange(new object[] { "Standard", "Chat", "Email", "Code" }); writingStyle.SelectedItem = source.WritingStyle; Setting(writing, writingStyle, 284); Button(writing, "Words & snippets", 20, 340, 248, (s, e) => EditRules());
            Label(writing, "Enhanced mode needs the local speech setup and grammar service.\nCleanup removes contextual fillers; turn it off for verbatim writing.", 20, 400, 620, 70, 10, false, Muted);
            var noteSettings = SettingsTab("Notes");
            Label(noteSettings, "Recording shortcut - select one", 20, 20, 620, 28, 11, true, Ink); Setting(noteSettings, noteShortcut, 58);
            Label(noteSettings, "Personal-note audio", 20, 120, 620, 28, 11, true, Ink); Setting(noteSettings, noteSource, 158);
            Label(noteSettings, "Meeting audio", 20, 220, 620, 28, 11, true, Ink); Setting(noteSettings, meetingSource, 258); Setting(noteSettings, noiseChoice, 324);
            Label(noteSettings, "The shortcut toggles personal notes. Start meetings from Home or the hover toolbar.\nNoise suppression reduces quiet sound; it does not guarantee removal of music or every background voice.", 20, 376, 620, 74, 9, false, Muted);
            var appearance = SettingsTab("App");
            themeChoice.Items.AddRange(new object[] { "System", "Light", "Dark" }); themeChoice.SelectedItem = source.Theme; writingStyle.SelectedItem = source.WritingStyle;
            Label(appearance, "Appearance", 20, 20, 620, 28, 11, true, Ink); Setting(appearance, themeChoice, 58);
            toolbarChoice.Checked = source.ShowToolbar; indicatorChoice.Checked = source.ShowVoiceIndicator; motionChoice.Checked = source.ReducedMotion; noiseChoice.Checked = source.ReduceNoise;
            toolbarStatus = Label(appearance, "Toolbar: " + (source.ShowToolbar ? "Ready on the screen edge" : "Hidden in preferences"), 20, 408, 620, 44, 9, false, Muted); Setting(appearance, toolbarChoice, 124); Setting(appearance, indicatorChoice, 170); Setting(appearance, motionChoice, 216); Setting(appearance, startup, 262);
            Label(appearance, "The toolbar docks along any edge. Hover reveals icons; hover over an icon for its name.\nKeyboard users can open the Windows tray action menu without changing the target text field.", 20, 324, 620, 76, 10, false, Muted);
            var storage = SettingsTab("Storage");
            Label(storage, "Keep control of your local data", 20, 20, 620, 32, 14, true, Ink);
            Label(storage, "Recordings, deleted notes and download cache use disk space.\nBack up notes before moving them. Conversation exports can include private annotations.\nSpeech and notes stay on this PC; no account or cloud subscription is required.", 20, 78, 620, 110, 10, false, Muted);
            Button(storage, "Storage & recently deleted", 20, 220, 284, (s, e) => { if (notes != null && !notes.Busy) { NotesStorageDialog.Show(this, notes.Store, source); notes.ReloadLibrary(); } else MessageBox.Show(this, "Finish the current recording/transcription before managing storage."); });
            var about = SettingsTab("About");
            Label(about, "Notewll 0.8.0", 20, 20, 620, 38, 18, true, Ink);
            Label(about, "Windows x64 | English | Vosk small-en-us 0.15\nOffline speech after one-time installation.\n\nThis release adds speech recovery, organized settings, richer notes,\nplayback, scoped exports, storage recovery, dictionary and snippets.\n\nConversation text is read-only in the app. External document protections\nare convenience restrictions, not proof that a copy was never changed.", 20, 82, 630, 220, 10, false, Muted);
            Button(about, "Open release notes", 20, 336, 220, (s, e) => ShowReleaseNotes());
            Button(about, "Check for updates", 260, 336, 220, async (s, e) => await ReleaseUpdates.Check(this, source));
            var updateInfo = String.IsNullOrWhiteSpace(source.UpdateManifestUrl) || String.IsNullOrWhiteSpace(source.UpdatePublisherThumbprint) ? "This preview has no signed update feed. Use a trusted full installer to update." : "Signed updates are available from this publisher. Downloads are verified before installation.";
            Label(about, updateInfo, 20, 398, 620, 70, 10, false, Muted);
            Setting(about, updateOnStart, 482); updateOnStart.Checked = source.CheckUpdatesOnStart; updateOnStart.Enabled = !String.IsNullOrWhiteSpace(source.UpdateManifestUrl) && !String.IsNullOrWhiteSpace(source.UpdatePublisherThumbprint);
            updateUrl.Text = source.UpdateManifestUrl; updatePublisher.Text = source.UpdatePublisherThumbprint;

        }
        void EditRules()
        {
            var draft = source.Copy(); draft.Personalization = pendingRules;
            PersonalRule.Edit(this, draft, () => { pendingRules = draft.Personalization; MarkDirty(); });
        }
        void RememberCorrection()
        {
            var entry = PersonalRule.Prompt(this, new PersonalRule { Kind = "Word", Trigger = raw.SelectedText, Replacement = "" }, source.Theme);
            if (entry == null) return;
            try { var proposed = pendingRules.Where(rule => !rule.Trigger.Equals(entry.Trigger, StringComparison.OrdinalIgnoreCase)).Concat(new[] { entry }).ToList(); PersonalRule.Validate(proposed); pendingRules = proposed; ShowPage("Preferences"); SelectSettingsCategory("Writing"); MarkDirty(); saved.Text = "Correction added. Save preferences to use it."; } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not remember correction"); }
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
                    var config = source.Copy();
                    string result = await System.Threading.Tasks.Task.Run(() => SpeechQuality.Available(config) ? SpeechQuality.Recognize(config, SpeechEngine.PcmFromWav(wav), false).text : SpeechEngine.Transcribe(config, wav));
                    if (!IsDisposed) { UpdateTranscript(result, result); sttStatus.Text = "Local test complete. Compare the recognized words against your reference transcript."; }
                }
                catch (Exception ex) { if (!IsDisposed) sttStatus.Text = ex.Message; }
            }
        }
        internal void SelectSettingsCategory(string name) { foreach (TabPage tab in settingsTabs.TabPages) if (tab.Text == name) { settingsTabs.SelectedTab = tab; break; } }
        static void OpenPermissionSettings() { try { System.Diagnostics.Process.Start("ms-settings:privacy-microphone"); } catch (Exception ex) { MessageBox.Show(ex.Message, "Open microphone settings"); } }
        void ShowReleaseNotes() { MessageBox.Show(this, "0.8.0\n\nContinuous local recognition while you speak.\nBackground grammar correction with enhanced speech enabled.\nFast release using the live transcript and available corrections.\nSilence and short shortcut taps finish without warnings.\nSlow grammar falls back to local cleanup.\nNotes, recordings and shortcut preferences are preserved.\n\nThree six-second audio replays finished in 148-509 ms after simulated release; desktop timing and accuracy still need acceptance testing.", "Release notes"); }
        public void ShowFirstRunGuide() { FirstRunSetup(); source.SetupGuideSeen = true; source.Save(); }
        void FirstRunSetup()
        {
            using (var dialog = DialogUi.Create("Check your setup", new Size(640, 510)))
            {
                string detail; bool available = SpeechEngine.Healthy(source, out detail);
                var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
                var status = DialogUi.Text("1. Voice files: " + (available ? "Ready - English, offline" : detail) + "\r\n\r\n2. Microphone: " + (source.SetupMicrophoneVerified ? "Input activity verified" : "Not tested yet") + "\r\n\r\n3. Practice sentence: " + (source.SetupSpeechVerified ? "Recognized successfully" : "Not verified yet") + "\r\n\r\n4. Other-app typing: " + (source.SetupInsertionConfirmed ? "Confirmed by you" : "Not confirmed yet")); flow.Controls.Add(status);
                flow.Controls.Add(DialogUi.Button("Microphone permissions", (s, e) => OpenPermissionSettings()));
                flow.Controls.Add(DialogUi.Button("Choose microphone", (s, e) => { dialog.Close(); ShowPage("Preferences"); SelectSettingsCategory("Recording"); }));
                flow.Controls.Add(DialogUi.Button("Practice a sentence", (s, e) => { dialog.Close(); ShowPage("Playground"); }));
                flow.Controls.Add(DialogUi.Text("For the last step, click a text field in Notepad or your browser, hold your dictation key, speak and release. Check that the text appears correctly."));
                var confirm = DialogUi.Button("I tested typing in another app", (s, e) => { source.SetupInsertionConfirmed = true; if (CloseToTray) source.Save(); dialog.Close(); }, true); confirm.Enabled = source.SetupSpeechVerified; flow.Controls.Add(confirm);
                flow.Controls.Add(DialogUi.Button("Do this later", (s, e) => dialog.Close()));
                flow.Resize += (s, e) => { foreach (Label label in flow.Controls.OfType<Label>()) label.MaximumSize = new Size(Math.Max(240, flow.ClientSize.Width - 32), 0); };
                dialog.Controls.Add(flow); ThemeColors.Apply(dialog, source.Theme); dialog.ShowDialog(this);
            }
        }
    }
}
