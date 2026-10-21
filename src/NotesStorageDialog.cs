using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace LocalDictation
{
    static class NotesStorageDialog
    {
        sealed class Deleted
        {
            public string Folder, Title;
            public override string ToString() { return Title + "  -  " + NotesData.Size(NotesData.Bytes(Folder)); }
        }
        public static void Show(IWin32Window owner, NoteStore store, Config config)
        {
            using (var form = new Form { Text = "Storage & recently deleted", Size = new Size(720, 540), StartPosition = FormStartPosition.CenterParent, Font = new Font("Segoe UI", 10), Icon = AppLogo.Icon })
            {
                var sizes = new Label { Dock = DockStyle.Top, Height = 112, Padding = new Padding(12) };
                var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
                var actions = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 148, Padding = new Padding(8), AutoScroll = true };
                string deletedRoot = Path.Combine(store.Root, ".deleted"), cache = Path.Combine(Config.DirectoryPath, "SetupCache");
                Action refresh = () =>
                {
                    long deleted = NotesData.Bytes(deletedRoot), total = NotesData.Bytes(store.Root);
                    sizes.Text = "App & speech files: " + NotesData.Size(NotesData.Bytes(AppDomain.CurrentDomain.BaseDirectory)) + "\nActive notes & recordings: " + NotesData.Size(Math.Max(0, total - deleted)) + "   |   Recently deleted: " + NotesData.Size(deleted) + "\nDownload cache: " + NotesData.Size(NotesData.Bytes(cache)) + "\nRecently deleted notes stay until you restore or permanently delete them.";
                    list.Items.Clear();
                    if (Directory.Exists(deletedRoot)) foreach (string folder in Directory.GetDirectories(deletedRoot))
                    {
                        try { var note = new JavaScriptSerializer().Deserialize<NoteDocument>(File.ReadAllText(Path.Combine(folder, "note.json"))); store.Folder(note); list.Items.Add(new Deleted { Folder = folder, Title = note.Title }); } catch (Exception) { }
                    }
                };
                Action<string, Action> button = (label, click) => { var b = new Button { Text = label, AutoSize = true, Height = 34 }; b.Click += (s, e) => { try { click(); refresh(); } catch (Exception ex) { MessageBox.Show(form, ex.Message, label); } }; actions.Controls.Add(b); };
                button("Restore selected", () =>
                {
                    var item = list.SelectedItem as Deleted; if (item == null) return;
                    var note = new JavaScriptSerializer().Deserialize<NoteDocument>(File.ReadAllText(Path.Combine(item.Folder, "note.json")));
                    string target = store.Folder(note); if (Directory.Exists(target)) throw new IOException("An active note already has this identifier. It will not be overwritten.");
                    Directory.Move(item.Folder, target); note.DeletedUtc = null; store.Save(note);
                });
                button("Delete permanently", () =>
                {
                    var item = list.SelectedItem as Deleted; if (item == null) return;
                    if (MessageBox.Show(form, "Permanently erase this note and its recordings? This cannot be undone.", "Delete permanently", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) NotesData.DeleteTree(item.Folder, deletedRoot);
                });
                button("Back up active notes", () => { using (var file = new SaveFileDialog { Filter = "Notes backup|*.zip", FileName = "LocalDictation-notes-" + DateTime.Now.ToString("yyyy-MM-dd") + ".zip", AddExtension = true, DefaultExt = "zip" }) if (file.ShowDialog(form) == DialogResult.OK) { NotesData.Backup(store, file.FileName); MessageBox.Show(form, "Backup saved. Keep a copy on another drive."); } });
                button("Restore backup", () => { using (var file = new OpenFileDialog { Filter = "Notes backup|*.zip" }) if (file.ShowDialog(form) == DialogResult.OK) MessageBox.Show(form, NotesData.Restore(store, file.FileName) + " notes restored. Existing identifiers were kept."); });
                button("Clear download cache", () => { if (Directory.Exists(cache) && MessageBox.Show(form, "Remove completed setup downloads? The installed speech model and your notes stay in place.", "Clear download cache", MessageBoxButtons.YesNo) == DialogResult.Yes) NotesData.DeleteTree(cache, Config.DirectoryPath); });
                button("Open notes folder", () => { Directory.CreateDirectory(store.Root); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = store.Root, UseShellExecute = true }); });
                var retention = new ComboBox { Width = 170, DropDownStyle = ComboBoxStyle.DropDownList }; retention.Items.AddRange(new object[] { "Keep deleted forever", "Delete after 7 days", "Delete after 30 days", "Delete after 90 days" }); retention.SelectedIndex = Math.Max(0, Array.IndexOf(new[] { 0, 7, 30, 90 }, config.DeletedRetentionDays)); actions.Controls.Add(retention);
                button("Apply retention", () => { int days = new[] { 0, 7, 30, 90 }[retention.SelectedIndex]; if (days != 0 && MessageBox.Show(form, "Permanently erase deleted notes older than " + days + " days, now and on future starts?", "Retention", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; config.DeletedRetentionDays = days; config.Save(); store.CleanupDeleted(days); });
                button("Close", () => form.Close());
                form.Controls.Add(list); form.Controls.Add(sizes); form.Controls.Add(actions); refresh(); ThemeColors.Apply(form, config.Theme); form.ShowDialog(owner);
            }
        }
    }
}
