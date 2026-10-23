using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace LocalDictation
{
    static class UxVerification
    {
        static BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static object Field(object target, string name) { return target.GetType().GetField(name, Flags).GetValue(target); }
        static void Invoke(object target, string name) { target.GetType().GetMethod(name, Flags).Invoke(target, null); }
        static System.Collections.Generic.IEnumerable<Control> Controls(Control parent) { foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Controls(child)) yield return nested; } }
        static void Snapshot(Form form, string path)
        {
            Application.DoEvents(); foreach (var control in Controls(form)) if (control.Visible) control.Refresh(); form.Refresh();
            using (var image = new Bitmap(form.Width, form.Height)) using (var graphics = Graphics.FromImage(image)) { IntPtr dc = graphics.GetHdc(); try { Native.PrintWindow(form.Handle, dc, 0); } finally { graphics.ReleaseHdc(dc); } image.Save(path); }
        }
        static void Dialog(Form owner, string name, string root, Action open, Action<Form> inspect)
        {
            Exception failure = null; bool found = false;
            using (var timer = new Timer { Interval = 200 })
            {
                timer.Tick += (s, e) => { var dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(form => form != owner && form.Visible); if (dialog == null) return; timer.Stop(); found = true; try { inspect(dialog); Snapshot(dialog, Path.Combine(root, name + ".png")); } catch (Exception ex) { failure = ex; } finally { dialog.Close(); } };
                timer.Start(); open(); timer.Stop();
            }
            if (failure != null) throw failure; if (!found) throw new Exception("Dialog did not open: " + name);
        }
        public static void Run(string root)
        {
            Directory.CreateDirectory(root); Config.DiagnosticDataRoot(Path.Combine(root, "settings"));
            var config = new Config { NoteMicrophoneId = "previous-microphone", NoteOutputId = "previous-output" }; var store = new NoteStore(Path.Combine(root, "notes")); var note = store.Create("Release review", "Computer", "Alex"); note.Status = "Saved"; note.WrittenNotes = "Keep personal notes separate."; note.Segments.Add(new NoteSegment { Source = "Computer", Complete = true, Frames = 16000, RawText = "The original words stay unchanged.", Text = "The original words stay unchanged." }); store.Save(note);
            using (var track = new WaveTrack(store.AudioPath(note, "Computer"))) track.WriteAt(0, new byte[32000]); string original = NoteStore.Transcript(note);
            Application.EnableVisualStyles();
            using (var hub = new SettingsForm(config, "", "", store))
            {
                hub.Show(); hub.UpdateTranscript("sue dear has a meeting", "Sudheer has a meeting.");
                if (((TextBox)Field(hub, "cleaned")).Text != "Sudheer has a meeting.") throw new Exception("Preview discarded the corrected final transcript.");
                hub.ShowPage("Notes"); hub.Notes.SelectNote(note.Id); Application.DoEvents();
                var playerSources = (ComboBox)Field(hub.Notes, "audioSource"); if (playerSources.Items.Count != 1 || Convert.ToString(playerSources.SelectedItem) != "Computer") throw new Exception("Playback offers a nonexistent source.");
                var tabs = (TabControl)Field(hub.Notes, "tabs"); tabs.SelectedIndex = 1; Application.DoEvents(); var transcript = (RichTextBox)Field(hub.Notes, "transcript"); if (!transcript.ReadOnly || !transcript.Text.Contains("original words")) throw new Exception("Conversation is missing or editable."); Snapshot(hub, Path.Combine(root, "conversation.png"));
                Dialog(hub, "export", root, () => Invoke(hub.Notes, "ShowExportDialog"), dialog => {
                    var all = Controls(dialog).ToArray(); var choices = all.OfType<ComboBox>().ToArray(); if (choices.Length != 1 || all.OfType<Button>().Any(button => button.Text.Contains("CSV") || button.Text.Contains("Word") || button.Text.Contains("Excel"))) throw new Exception("Editable document export remains available.");
                    var preview = all.OfType<RichTextBox>().Single(); if (!preview.ReadOnly || preview.Text.StartsWith("#") || preview.Text.Contains("Personal notes")) throw new Exception("Conversation preview has markup or leaked annotations.");
                    choices[0].SelectedIndex = 1; if (!preview.Text.Contains("Keep personal notes separate.") || preview.Text.Contains("original words")) throw new Exception("Personal-note export scope leaks conversation."); choices[0].SelectedIndex = 0;
                    var save = all.OfType<Button>().Single(button => button.Text == "Save PDF..."); if (save.Bottom > save.Parent.ClientSize.Height || preview.Height < 70) throw new Exception("Export controls are clipped.");
                });
                string ids = config.NoteMicrophoneId + "|" + config.NoteOutputId;
                Dialog(hub, "devices-cancel", root, () => Invoke(hub.Notes, "ShowDevices"), dialog => { });
                if (ids != config.NoteMicrophoneId + "|" + config.NoteOutputId || File.Exists(Config.FilePath)) throw new Exception("Closing devices committed choices.");
                Dialog(hub, "dictionary", root, () => Invoke(hub, "EditRules"), dialog => { var all = Controls(dialog).ToArray(); if (all.OfType<DataGridView>().Any() || !all.OfType<ListView>().Any() || !all.OfType<Button>().Any(button => button.Text == "Add entry")) throw new Exception("Dictionary still requires grid editing."); });
                Dialog(hub, "speaker-labels", root, () => Invoke(hub.Notes, "EditSpeakerLabels"), dialog => { if (!Controls(dialog).OfType<ListView>().Any(list => list.MultiSelect)) throw new Exception("Speaker editing cannot select statements."); });
                Dialog(hub, "storage", root, () => NotesStorageDialog.Show(hub, store, config), dialog => { var pages = Controls(dialog).OfType<TabControl>().Single().TabPages.Cast<TabPage>().Select(page => page.Text).ToArray(); if (!pages.SequenceEqual(new[] { "Storage", "Backups", "Recently deleted", "Retention" })) throw new Exception("Storage operations are not separated."); });
                hub.ShowPage("Preferences"); hub.SelectSettingsCategory("About"); if (Controls(hub).OfType<Label>().Any(label => label.Visible && (label.Text.Contains("thumbprint") || label.Text.Contains("manifest URL")))) throw new Exception("Publisher setup is exposed to ordinary users."); Snapshot(hub, Path.Combine(root, "about.png"));
                if (NoteStore.Transcript(note) != original || note.Segments[0].SpeakerLabel != null) throw new Exception("Review dialogs changed original data."); hub.CloseToTray = false;
            }
            var file = new FileInfo(Config.FilePath); if (file.Exists) throw new Exception("Read-only dialog checks wrote preferences.");
            Console.Write("ux-ok|pdf-only|readable-preview|scope|devices-cancel|dictionary|speaker-labels|storage|consumer-updates|originals-preserved");
        }
    }
}
