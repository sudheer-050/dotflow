using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LocalDictation
{
    static class Native
    {
        public const int WH_MOUSE_LL = 14;
        public const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101, WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105;
        public const int VK_ESCAPE = 0x1B, INPUT_KEYBOARD = 1, KEYEVENTF_KEYUP = 0x0002;
        public delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)] public struct Kbd { public uint vkCode, scanCode, flags, time; public IntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] public struct Input { public uint type; public InputUnion union; }
        [StructLayout(LayoutKind.Explicit)] public struct InputUnion { [FieldOffset(0)] public KeyInput key; [FieldOffset(0)] public MouseInput mouse; }
        [StructLayout(LayoutKind.Sequential)] public struct KeyInput { public ushort vk, scan; public uint flags, time; public IntPtr extra; }
        [StructLayout(LayoutKind.Sequential)] public struct MouseInput { public int dx, dy; public uint data, flags, time; public IntPtr extra; }

        [DllImport("user32.dll")] public static extern IntPtr SetWindowsHookEx(int id, HookProc fn, IntPtr module, uint threadId);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr window, System.Text.StringBuilder name, int size);
        [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr dc, uint flags);
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern uint SendInput(uint count, Input[] inputs, int size);
        [DllImport("kernel32.dll")] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc fn, IntPtr param);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder text, int max);
        delegate bool EnumProc(IntPtr hwnd, IntPtr param);

        // Test-harness helper: first visible top-level window whose title contains the text.
        public static IntPtr FindWindowByTitle(string part)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((h, p) =>
            {
                var t = new System.Text.StringBuilder(256); GetWindowText(h, t, 256);
                if (IsWindowVisible(h) && t.ToString().IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) { found = h; return false; }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        public static bool Paste(IntPtr target, string method = "Auto")
        {
            if (target == IntPtr.Zero || !IsWindow(target)) return false;
            if (GetForegroundWindow() != target && !SetForegroundWindow(target)) return false;
            Thread.Sleep(80);
            if (GetForegroundWindow() != target) return false;
            if (new[] { 16, 17, 18, 91, 92 }.Any(k => (GetAsyncKeyState(k) & 0x8000) != 0)) return false;
            var windowClass = new System.Text.StringBuilder(256); GetClassName(target, windowClass, windowClass.Capacity);
            string mode = PastePolicy.Resolve(method, windowClass.ToString());
            var inputs = mode == "ShiftInsert" ? new[] { Key(0x10, 0), Key(0x2D, 0), Key(0x2D, KEYEVENTF_KEYUP), Key(0x10, KEYEVENTF_KEYUP) } :
                mode == "CtrlShiftV" ? new[] { Key(0x11, 0), Key(0x10, 0), Key(0x56, 0), Key(0x56, KEYEVENTF_KEYUP), Key(0x10, KEYEVENTF_KEYUP), Key(0x11, KEYEVENTF_KEYUP) } :
                new[] { Key(0x11, 0), Key(0x56, 0), Key(0x56, KEYEVENTF_KEYUP), Key(0x11, KEYEVENTF_KEYUP) };
            return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input))) == inputs.Length;
        }

        public static bool ModifiersDown() { return new[] { 16, 17, 18, 91, 92 }.Any(k => (GetAsyncKeyState(k) & 0x8000) != 0); }
        public static bool WriteLive(IntPtr target, LiveEdit edit)
        {
            if (GetForegroundWindow() != target || ModifiersDown()) return false;
            var inputs = new List<Input>();
            for (int i = 0; i < edit.Remove; i++) { inputs.Add(Key(8, 0)); inputs.Add(Key(8, KEYEVENTF_KEYUP)); }
            foreach (char c in edit.Append)
            {
                inputs.Add(new Input { type = INPUT_KEYBOARD, union = new InputUnion { key = new KeyInput { scan = c, flags = 4 } } });
                inputs.Add(new Input { type = INPUT_KEYBOARD, union = new InputUnion { key = new KeyInput { scan = c, flags = 4 | KEYEVENTF_KEYUP } } });
            }
            return inputs.Count == 0 || SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(Input))) == inputs.Count;
        }
        public static void Tap(ushort vk, bool down)
        {
            var inputs = new[] { Key(vk, down ? 0u : (uint)KEYEVENTF_KEYUP) };
            SendInput(1, inputs, Marshal.SizeOf(typeof(Input)));
        }

        static Input Key(ushort key, uint flags) { return new Input { type = INPUT_KEYBOARD, union = new InputUnion { key = new KeyInput { vk = key, flags = flags } } }; }
    }

    static class Startup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run", ValueName = "LocalDictation";
        public static bool Enabled { get { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue(ValueName) != null; } }
        public static void Set(bool on)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) k.SetValue(ValueName, "\"" + Application.ExecutablePath + "\" --background");
                else k.DeleteValue(ValueName, false);
            }
        }
    }

    sealed class DictationApp : ApplicationContext
    {
        readonly Overlay overlay = new Overlay();
        readonly FloatingDock dock = new FloatingDock();
        readonly NotifyIcon tray;
        readonly System.Windows.Forms.Timer noteStatus = new System.Windows.Forms.Timer { Interval = 1000 };
        readonly StateMachine state = new StateMachine();
        readonly DictationRecovery recovery = new DictationRecovery();
        RecoveryForm recoveryWindow;
        volatile bool exiting, recovering;
        bool setupShown;
        string engineReadiness = "Loading the offline speech engine...";
        readonly System.Collections.Generic.List<string> issues = new System.Collections.Generic.List<string>();
        void ReportIssue(string message) { string entry = DateTime.Now.ToString("HH:mm:ss") + "  " + message; issues.Insert(0, entry); if (issues.Count > 20) issues.RemoveAt(20); if (hub != null && !hub.IsDisposed) hub.SetIssues(issues); }
        readonly Native.HookProc hookProc, mouseProc;
        IntPtr mouseHook;
        readonly System.Windows.Forms.Timer liveTimer = new System.Windows.Forms.Timer { Interval = 250 };
        readonly ManualResetEvent liveIdle = new ManualResetEvent(true);
        int liveBusy;
        LiveRecognition liveRecognition;
        string liveInserted = "";
        bool liveInterrupted, noteIndicator, sessionLiveWriting, sessionAccurate;
        volatile bool prewarming;
        int engineGeneration, lastAccurateBytes;
        IntPtr hook, target;
        IntPtr previousApp;
        Config config;
        MicRecorder recorder;
        readonly ShortcutTracker shortcutTracker = new ShortcutTracker();
        HashSet<int> shortcutKeys;
        readonly ShortcutTracker noteTracker = new ShortcutTracker();
        HashSet<int> noteBinding;
        void SetShortcutBindings()
        {
            shortcutKeys = Shortcut.Parse(config.Hotkey); shortcutTracker.Reset();
            noteBinding = Shortcut.Parse(config.NotesHotkey); noteTracker.Reset();
        }
        void ToggleNotes(string mode, string kind = "Note")
        {
            if (state.Current != AppState.Idle) return;
            EnsureHub(); hub.ToggleNote(mode, kind); UpdateDock();
        }
        void UpdateDock()
        {
            if (hub != null && !hub.IsDisposed) { hub.SetRecoveryAvailable(recovery.HasWork); hub.SetToolbarStatus(dock.VisibilityDescription); }
            bool recording = hub != null && hub.IsNoteRecording; bool busy = hub != null && hub.NoteBusy;
            string label = state.Current == AppState.Recording ? "Writing live - click Stop to refine" : state.Current == AppState.Processing ? "Refining your words..." : recording ? (hub.Notes.Document != null && hub.Notes.Document.Kind == "Meeting" ? "Recording meeting" : "Recording note") : busy ? "Saving your note..." : "Ready - drag the grip to move";
            if (recording && hub.Notes.Document != null) label += " " + TimeSpan.FromSeconds(hub.Notes.RecordingSeconds).ToString(@"mm\:ss") + " - click Stop to save";
            if (!recording && !busy && hub != null && !String.IsNullOrWhiteSpace(hub.Notes.Feedback)) label = hub.Notes.Feedback;
            if (!recording && !busy && state.Current == AppState.Idle && config.AccurateDictationEnabled && !AccurateSpeech.Ready) label = engineReadiness;
            dock.UpdateState(state.Current == AppState.Recording, state.Current == AppState.Processing, recording, busy, recording && hub.Notes.Document != null && hub.Notes.Document.Kind == "Meeting", label);
        }
        int generation;
        SettingsForm hub;
        EventWaitHandle showHubEvent, exitEvent;
        RegisteredWaitHandle exitWait;
        RegisteredWaitHandle showHubWait;
        // Last dictation, memory only, for the raw-vs-cleaned preview. Never written to disk.
        string lastRaw = "", lastCleaned = "";

        public DictationApp(bool showHub = true)
        {
            overlay.InputLevel = () => recorder != null ? recorder.SoundLevel : hub != null ? hub.NoteSoundLevel : 0;
            try { config = Config.Load(); }
            catch (InvalidDataException ex) { config = new Config(); MessageBox.Show(ex.Message + " Defaults will be used.", "Notewll"); }
            overlay.ReducedMotion = config.ReducedMotion;
            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Notewll", null, (s, e) => ShowHub("Home"));
            menu.Items.Add("Show floating toolbar", null, (s, e) => { config.ShowToolbar = true; dock.SetVisibility(true); try { config.Save(); } catch (Exception ex) { ReportIssue("Toolbar shown, but its preference could not be saved: " + ex.Message); } });
            menu.Items.Add("Recover last dictation", null, (s, e) => ShowRecovery());
            menu.Items.Add("Issues & recovery", null, (s, e) => { ShowHub("Home"); if (hub != null) hub.ShowIssues(); });
            menu.Items.Add("Hide toolbar for 1 hour", null, (s, e) => dock.Snooze());
            menu.Items.Add("Keyboard action menu", null, (s, e) => ShowActionMenu());
            menu.Items.Add("Preferences", null, (s, e) => ShowHub("Preferences"));
            menu.Items.Add("Stop and save note", null, (s, e) => { if (hub != null && hub.IsNoteRecording) hub.ToggleNote(hub.NoteMode); });
            menu.Items.Add("Exit", null, (s, e) => ExitThread());
            tray = new NotifyIcon { Icon = AppLogo.Icon, Text = "Notewll 0.8.1 - hold " + config.Hotkey, Visible = true, ContextMenuStrip = menu };
            dock.RestorePosition(config.ToolbarX, config.ToolbarY);
            dock.Configure(config.ReducedMotion); dock.CancelWrite = Cancel;
            dock.ToggleWrite = () => { if (state.Current == AppState.Recording) StopRecording(); else StartRecording(); UpdateDock(); };
            dock.ToggleNote = () => { EnsureHub(); ToggleNotes(hub.PreferredNoteSource, "Note"); }; dock.ToggleMeeting = () => ToggleNotes(config.MeetingSource, "Meeting"); dock.OpenApp = () => ShowHub("Notes");
            dock.PositionSaved = position => { config.ToolbarX = position.X; config.ToolbarY = position.Y; try { config.Save(); } catch { } };
            dock.SetVisibility(config.ShowToolbar);
            noteStatus.Tick += (s, e) =>
            {
                UpdateDock();
                bool recordingNote = hub != null && hub.IsNoteRecording;
                if (config.ShowVoiceIndicator && recordingNote && !noteIndicator && state.Current == AppState.Idle) { overlay.Display("Recording from " + hub.NoteMode + " notes", Color.FromArgb(45, 120, 75)); noteIndicator = true; }
                if (!recordingNote && noteIndicator) { if (state.Current == AppState.Idle) overlay.Hide(); noteIndicator = false; }
                tray.Text = hub != null && hub.IsNoteRecording ? "Notewll - recording " + hub.NoteMode + " notes" : "Notewll - hold " + (config.Hotkey.Length > 30 ? config.Hotkey.Substring(0, 30) : config.Hotkey); }; noteStatus.Start();
            tray.DoubleClick += (s, e) => ShowHub("Home");
            SetShortcutBindings();
            liveTimer.Tick += (s, e) => LiveTick();
            mouseProc = (code, message, data) =>
            {
                var mousePoint = code >= 0 ? Marshal.PtrToStructure<Point>(data) : Point.Empty;
                if (code >= 0 && !dock.Bounds.Contains(mousePoint) && (message == (IntPtr)0x201 || message == (IntPtr)0x204 || message == (IntPtr)0x207 || message == (IntPtr)0x20A) && (state.Current == AppState.Recording || state.Current == AppState.Processing)) liveInterrupted = true;
                return Native.CallNextHookEx(mouseHook, code, message, data);
            };
            mouseHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, mouseProc, Native.GetModuleHandle(null), 0);
            hookProc = KeyboardHook;
            hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, hookProc, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new InvalidOperationException("Could not register the global keyboard hook.");
            // Create the message target without showing a startup/status box.
            var overlayHandle = overlay.Handle;
            exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "LocalDictation.Exit");
            exitWait = ThreadPool.RegisterWaitForSingleObject(exitEvent, (s, timedOut) => { if (!overlay.IsDisposed) PostUi(() => ExitThread()); }, null, Timeout.Infinite, false);
            showHubEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "LocalDictation.ShowHub");
            showHubWait = ThreadPool.RegisterWaitForSingleObject(showHubEvent, (s, timedOut) =>
            {
                if (!overlay.IsDisposed) PostUi(() => ShowHub("Home"));
            }, null, Timeout.Infinite, false);
            if (showHub) PostUi(() => ShowHub("Home"));
            Prewarm();
            if (config.CheckUpdatesOnStart) CheckStartupUpdate();
        }
        async void CheckStartupUpdate()
        {
            try { string version = await ReleaseUpdates.AvailableVersion(config.Copy()); if (version != null && !exiting) PostUi(() => { ReportIssue("Version " + version + " is available. Open Preferences > About > Check for updates to verify and install it."); tray.ShowBalloonTip(5000, "Update available", "Open Preferences > About to download and verify version " + version + ".", ToolTipIcon.Info); }); }
            catch (Exception ex) { PostUi(() => ReportIssue("Startup update check failed; installed speech still works offline. " + ex.Message)); }
        }

        // Make sure the bundled speech engine is up before the first dictation; only speaks up if it cannot be reached.
        void PostUi(Action action)
        {
            if (exiting || overlay.IsDisposed || !overlay.IsHandleCreated) return;
            try { overlay.BeginInvoke((Action)(() => { if (!exiting && !overlay.IsDisposed) action(); })); } catch (InvalidOperationException) { }
        }
        void Prewarm()
        {
            var cfg = config.Copy(); int revision = ++engineGeneration; prewarming = true;
            engineReadiness = cfg.AccurateDictationEnabled ? "Preparing accurate local speech..." : "Preparing basic offline speech...";
            if (hub != null && !hub.IsDisposed) hub.SetReadiness(engineReadiness);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                string problem = null;
                try
                {
                    if (cfg.AccurateDictationEnabled) AccurateSpeech.Warmup(cfg);
                    else { AccurateSpeech.Shutdown(); problem = SpeechEngine.EnsureRunning(cfg, 30); }
                }
                catch (Exception ex) { problem = ex.Message; }
                string detail = problem;
                PostUi(() =>
                {
                    if (revision != engineGeneration) return;
                    prewarming = false;
                    engineReadiness = detail ?? (cfg.AccurateDictationEnabled ? "Ready - accurate local speech with speech detection." : "Ready - basic offline speech.");
                    if (hub != null && !hub.IsDisposed) hub.SetReadiness(engineReadiness);
                    if (detail != null) ReportIssue(detail);
                    UpdateDock();
                });
            });
        }

        void StartRecording()
        {
            if (state.Current != AppState.Idle || recovering) return;
            if (hub != null && hub.IsTesting) return;
            if (config.AccurateDictationEnabled && (prewarming || !AccurateSpeech.Ready)) { if (!prewarming) Prewarm(); UpdateDock(); return; }
            target = Native.GetForegroundWindow();
            sessionLiveWriting = config.LiveWriting;
            sessionAccurate = config.AccurateDictationEnabled; lastAccurateBytes = 0;
            try
            {
                var rec = new MicRecorder(config.Microphone);
                rec.Start();
                recorder = rec;
            }
            catch (Exception ex) { Fail("Microphone problem: " + ex.Message); return; }
            state.Move(AppState.Recording); generation++; if (liveRecognition != null) liveRecognition.Dispose(); liveRecognition = new LiveRecognition(sessionAccurate ? config.Copy() : null); liveInserted = ""; liveInterrupted = mouseHook == IntPtr.Zero; liveTimer.Start();
            if (config.ShowVoiceIndicator) overlay.Display("● Recording from " + recorder.DeviceName + (recorder.FellBack ? " (saved mic not found — using default)" : "") + " — release " + config.Hotkey, Color.FromArgb(190, 45, 45));
        }

        void LiveTick()
        {
            if (state.Current != AppState.Recording || recorder == null || Interlocked.CompareExchange(ref liveBusy, 1, 0) != 0) return;
            byte[] pcm = recorder.Snapshot(); int peak = recorder.Peak; int gen = generation; var cfg = config; var recognition = liveRecognition;
            if (pcm.Length < 8000 || peak < 150) { Interlocked.Exchange(ref liveBusy, 0); return; }
            if (sessionAccurate && pcm.Length - lastAccurateBytes < 16000) { Interlocked.Exchange(ref liveBusy, 0); return; }
            lastAccurateBytes = pcm.Length;
            liveIdle.Reset();
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    string text = recognition.Snapshot(pcm, peak, false);
                    string formatted = WritingFormat.Compose(cfg, text.Trim());
                    if (!overlay.IsDisposed) PostUi(() => { if (gen == generation && state.Current == AppState.Recording && sessionLiveWriting) ApplyLiveText(formatted); });
                }
                catch { } // The complete recording still goes through normal recovery on release.
                finally { Interlocked.Exchange(ref liveBusy, 0); liveIdle.Set(); }
            });
        }
        bool ApplyLiveText(string text)
        {
            if (hub != null && !hub.IsDisposed && target == hub.Handle) { hub.UpdateLiveTranscript(text); return true; }
            if (!config.AutoPaste || liveInterrupted) return false;
            if (Native.GetForegroundWindow() != target) { liveInterrupted = true; return false; }
            if (Native.ModifiersDown()) return false;
            var edit = LiveEdit.Between(liveInserted, text);
            if (!Native.WriteLive(target, edit)) { liveInterrupted = true; return false; }
            liveInserted = text; return true;
        }
        void StopRecording()
        {
            if (state.Current != AppState.Recording) return;
            liveTimer.Stop();
            byte[] pcm = recorder.Stop();
            int peak = recorder.Peak;
            recorder.Dispose(); recorder = null;
            recovery.Audio = (byte[])pcm.Clone(); recovery.Text = liveInserted; recovery.Message = "Captured speech can be retried.";
            if (pcm.Length < 16000 * 2 / 3 || !sessionAccurate && peak < 150) { Cancel(); return; }
            state.Move(AppState.Processing);
            if (config.ShowVoiceIndicator) overlay.Display("Transcribing locally…", Color.FromArgb(50, 90, 170));
            int gen = ++generation;
            var cfg = config; var recognition = liveRecognition;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (!liveIdle.WaitOne(12000)) throw new SttException("Local speech did not finish. Open Recover last dictation to keep or retry the captured audio.");
                    string raw = recognition != null ? recognition.Snapshot(pcm, peak, true) : SpeechEngine.Transcribe(cfg, MicRecorder.ToWav(MicRecorder.Normalize(pcm, peak)));
                    if (recognition != null) recognition.Dispose();
                    PostUi(() => { if (!exiting && gen == generation) Finish(raw, raw); });
                }
                catch (Exception ex) { PostUi(() => { if (gen == generation) Fail(ex.Message); }); }
            });
        }

        void Finish(string raw, string refined)
        {
            if (state.Current != AppState.Processing) return;
            string text = WritingFormat.Compose(config, refined);
            recovery.Text = text;
            lastRaw = raw; lastCleaned = text;
            if (String.IsNullOrWhiteSpace(text)) { Cancel(); return; }
            if (hub != null && !hub.IsDisposed)
            {
                hub.UpdateTranscript(raw, text);
                if (target == hub.Handle) { state.Move(AppState.Idle); overlay.Hide(); return; }
            }
            if (config.AutoPaste)
            {
                if (ApplyLiveText(text)) { state.Move(AppState.Idle); overlay.Hide(); recovery.Discard(); return; }
                Fail("Text is ready but typing was interrupted. Open Recover last dictation to review and copy it."); return;
            }
            try { Clipboard.SetDataObject(text, true, 5, 100); }
            catch (Exception ex) { Fail("Could not copy transcript: " + ex.Message); return; }
            state.Move(AppState.Idle); recovery.Message = "Text copied. You can review it from the tray.";
            overlay.Display("Copied to clipboard", Color.FromArgb(45, 120, 75)); HideSoon();
        }
        void ShowRecovery()
        {
            if (!recovery.HasWork) { tray.ShowBalloonTip(3000, "Notewll", "There is no unfinished dictation to recover.", ToolTipIcon.Info); return; }
            if (recoveryWindow != null && !recoveryWindow.IsDisposed) { recoveryWindow.Activate(); return; }
            recoveryWindow = new RecoveryForm(recovery, done =>
            {
                if (state.Current == AppState.Recording || state.Current == AppState.Processing) { recovery.Message = "Finish the active dictation first."; done(); return; }
                var original = recovery.Audio; var audio = original == null ? null : (byte[])original.Clone(); if (audio == null || recovering) { done(); return; } int revision = recovery.Revision; recovering = true;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { string recognized = DictationSpeech.Transcribe(config, audio); string recovered = WritingFormat.Compose(config, recognized); if (revision == recovery.Revision) { recovery.Text = recovered; recovery.Message = "Recovered. Copy only what you need."; } }
                    catch (Exception ex) { if (revision == recovery.Revision) recovery.Message = ex.Message; }
                    finally { recovering = false; Array.Clear(audio, 0, audio.Length); done(); }
                });
            }); recoveryWindow.Show();
        }
        void ShowActionMenu()
        {
            var menu = new ContextMenuStrip(); menu.Items.Add("Dictate / stop", null, (s, e) => { if (state.Current == AppState.Recording) StopRecording(); else StartRecording(); });
            menu.Items.Add("Cancel dictation", null, (s, e) => Cancel()); menu.Items.Add("Personal note", null, (s, e) => { EnsureHub(); ToggleNotes(hub.PreferredNoteSource); });
            menu.Items.Add("Meeting", null, (s, e) => ToggleNotes(config.MeetingSource, "Meeting")); menu.Items.Add("Open app", null, (s, e) => ShowHub("Home")); menu.Closed += (s, e) => menu.Dispose(); menu.Show(Cursor.Position);
        }

        void Cancel()
        {
            liveTimer.Stop(); generation++; if (liveRecognition != null) liveRecognition.Dispose();
            if (liveInserted.Length > 0 && !liveInterrupted) ApplyLiveText("");
            if (state.Current == AppState.Recording)
            {
                if (recorder != null) recorder.Dispose(); recorder = null; state.Move(AppState.Idle);
            }
            else if (state.Current == AppState.Processing) { generation++; state.Move(AppState.Idle); }
            else return;
            recovery.Discard(); overlay.Hide();
        }

        void Fail(string message)
        {
            liveTimer.Stop();
            ReportIssue(message);
            if (state.Current != AppState.Error) state.Move(AppState.Error);
            recovery.Message = message + " Open Recover last dictation from the tray.";
            if (hub != null && !hub.IsDisposed) hub.SetReadiness(recovery.Message);
            overlay.Display(message, Color.FromArgb(180, 90, 20));
            tray.ShowBalloonTip(5000, "Notewll", message, ToolTipIcon.Warning);
            var timer = new System.Windows.Forms.Timer { Interval = 6000 };
            timer.Tick += (s, e) => { timer.Stop(); if (state.Current == AppState.Error) { state.Move(AppState.Idle); overlay.Hide(); } };
            timer.Start();
        }

        void HideSoon()
        {
            var timer = new System.Windows.Forms.Timer { Interval = 1200 };
            timer.Tick += (s, e) => { timer.Stop(); if (state.Current == AppState.Idle) overlay.Hide(); };
            timer.Start();
        }

        IntPtr KeyboardHook(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0)
            {
                int vk = (int)Marshal.PtrToStructure<Native.Kbd>(lParam).vkCode;
                var data = Marshal.PtrToStructure<Native.Kbd>(lParam);
                if ((data.flags & 0x10) != 0) return Native.CallNextHookEx(hook, code, wParam, lParam);
                bool down = wParam == (IntPtr)Native.WM_KEYDOWN || wParam == (IntPtr)Native.WM_SYSKEYDOWN;
                bool up = wParam == (IntPtr)Native.WM_KEYUP || wParam == (IntPtr)Native.WM_SYSKEYUP;
                if (hub != null && hub.IsCapturingShortcut)
                {
                    if (down || up) hub.CaptureShortcutKey(vk, down);
                    shortcutTracker.Reset(); noteTracker.Reset(); return (IntPtr)1;
                }
                if ((state.Current == AppState.Recording || state.Current == AppState.Processing) && down && !shortcutKeys.Contains(Shortcut.Normalize(vk)) && vk != Native.VK_ESCAPE) liveInterrupted = true;
                int transition;
                bool consume = shortcutTracker.Feed(shortcutKeys, vk, down, out transition);
                if (transition == 1) PostUi(StartRecording);
                if (transition == -1) PostUi(StopRecording);
                int noteTransition; consume |= noteTracker.Feed(noteBinding, vk, down, out noteTransition);
                if (noteTransition == 1) PostUi(() => { EnsureHub(); ToggleNotes(hub.PreferredNoteSource); });
                if (consume) return (IntPtr)1;
                if (vk == Native.VK_ESCAPE && down && (state.Current == AppState.Recording || state.Current == AppState.Processing)) { PostUi(Cancel); return (IntPtr)1; }
            }
            return Native.CallNextHookEx(hook, code, wParam, lParam);
        }

        void ShowSettings()
        {
            ShowHub("Preferences");
        }

        void EnsureHub()
        {
            if (hub == null || hub.IsDisposed)
            {
                hub = new SettingsForm(config, lastRaw, lastCleaned) { CloseToTray = true };
                hub.SetIssues(issues); hub.DismissIssues = () => { issues.Clear(); hub.SetIssues(issues); };
                hub.Notes.IssueReported = ReportIssue;
                hub.CanTest = () => state.Current == AppState.Idle;
                hub.OnSaved = next =>
                {
                    if (state.Current != AppState.Idle || hub.IsTesting) throw new InvalidOperationException("Finish dictating before saving preferences.");
                    next.Save();
                    if (next.StartWithWindows || next.StartWithWindows != Startup.Enabled) Startup.Set(next.StartWithWindows);
                    bool speechChanged = next.AccurateDictationEnabled != config.AccurateDictationEnabled || next.QualityPython != config.QualityPython;
                    config = next; overlay.ReducedMotion = config.ReducedMotion; dock.Configure(config.ReducedMotion); dock.SetVisibility(config.ShowToolbar); SetShortcutBindings();
                    if (speechChanged) Prewarm();
                    tray.Text = "Notewll - hold " + config.Hotkey;
                };
            }
        }
        void ShowHub(string page)
        {
            if (state.Current != AppState.Idle) return;
            var foreground = Native.GetForegroundWindow(); if (foreground != IntPtr.Zero && (hub == null || foreground != hub.Handle) && foreground != dock.Handle && foreground != overlay.Handle) previousApp = foreground;
            EnsureHub();
            hub.BeginDictation = () =>
            {
                if (previousApp == IntPtr.Zero || !Native.IsWindow(previousApp) || !Native.SetForegroundWindow(previousApp)) { ReportIssue("Choose a text field in another app, then use the dictation key or edge toolbar."); hub.ShowIssues(); return; }
                hub.Hide(); var focusDelay = new System.Windows.Forms.Timer { Interval = 120 };
                focusDelay.Tick += (s, e) => { focusDelay.Stop(); focusDelay.Dispose(); if (!exiting) { if (Native.GetForegroundWindow() == previousApp) StartRecording(); else { ReportIssue("The target app did not accept focus. Click its text field and use your dictation key."); ShowHub("Home"); } } }; focusDelay.Start();
            };
            hub.OpenRecovery = ShowRecovery;
            hub.SetReadiness(engineReadiness); hub.ShowPage(page); hub.Show(); if (hub.WindowState == FormWindowState.Minimized) hub.WindowState = FormWindowState.Normal; hub.Activate();
            if (!config.SetupGuideSeen && !setupShown) { setupShown = true; hub.ShowFirstRunGuide(); }
        }

        protected override void ExitThreadCore()
        {
            exiting = true; generation++; liveTimer.Stop();
            if (recorder != null) { var tail = recorder.Stop(); recovery.Audio = tail; }
            if (recoveryWindow != null) recoveryWindow.Dispose();
            dock.Close(); dock.Dispose();
            liveTimer.Stop(); liveTimer.Dispose();
            if (mouseHook != IntPtr.Zero) Native.UnhookWindowsHookEx(mouseHook);
            noteStatus.Stop(); noteStatus.Dispose();
            if (exitWait != null) exitWait.Unregister(null); if (exitEvent != null) exitEvent.Dispose();
            if (showHubWait != null) showHubWait.Unregister(null);
            if (showHubEvent != null) showHubEvent.Dispose();
            if (hub != null) { hub.CloseToTray = false; hub.Dispose(); }
            if (recorder != null) recorder.Dispose();
            if (liveRecognition != null) liveRecognition.Dispose(); SpeechEngine.Shutdown(); ThreadPool.QueueUserWorkItem(_ => SpeechQuality.Shutdown()); ThreadPool.QueueUserWorkItem(_ => AccurateSpeech.Shutdown());
            if (hook != IntPtr.Zero) Native.UnhookWindowsHookEx(hook);
            tray.Visible = false; tray.Dispose(); overlay.Close(); base.ExitThreadCore();
        }
    }

}
