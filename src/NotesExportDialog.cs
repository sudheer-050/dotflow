using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace LocalDictation
{
    sealed partial class NotesPane
    {
        void ShowExportDialog()
        {
            if (selected == null) { uiError = "Choose a saved note to export."; return; }
            if (Busy && ReferenceEquals(selected, session.Document)) { uiError = "Stop and save this recording before exporting it."; return; }
            SaveEdits(); var note = selected;
            using (var dialog = new Form { Text = "Export - choose what to include", Size = new Size(670, 540), StartPosition = FormStartPosition.CenterParent, Font = Font, Icon = AppLogo.Icon })
            {
                var controls = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 95, Padding = new Padding(10) };
                var scope = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList }; scope.Items.AddRange(new object[] { "Conversation only", "Personal notes only", "Full record" }); scope.SelectedIndex = 0;
                var format = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList }; format.Items.AddRange(new object[] { "PDF", "Word", "Excel", "CSV" }); format.SelectedItem = config.ExportFormat;
                var names = new CheckBox { Text = "Include participant names", AutoSize = true, Checked = false };
                var notice = new Label { AutoSize = true, Text = "Preview below. CSV is editable; document protections are not tamper-proof." };
                controls.Controls.AddRange(new Control[] { scope, format, names, notice });
                var preview = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
                var save = new Button { Text = "Save export...", Dock = DockStyle.Bottom, Height = 42 };
                Func<NoteDocument> snapshot = () => NoteExport.Scope(note, scope.SelectedIndex, names.Checked);
                Action refresh = () => preview.Text = NoteStore.Markdown(snapshot()); scope.SelectedIndexChanged += (s, e) => refresh(); names.CheckedChanged += (s, e) => refresh();
                bool writingFile = false; dialog.FormClosing += (s, e) => { if (writingFile) e.Cancel = true; };
                save.Click += async (s, e) =>
                {
                    string extension = new[] { "pdf", "docx", "xlsx", "csv" }[format.SelectedIndex];
                    using (var file = new SaveFileDialog { Filter = format.Text + "|*." + extension, DefaultExt = extension, AddExtension = true, FileName = String.Concat(note.Title.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)) })
                    {
                        if (file.ShowDialog(dialog) != DialogResult.OK) return;
                        var copy = snapshot(); writingFile = true; save.Enabled = false; save.Text = "Exporting...";
                        try { await Task.Run(() => NoteExport.Save(copy, file.FileName)); config.ExportFormat = format.Text; config.Save(); save.Text = "Saved - export again"; if (MessageBox.Show(dialog, "Export saved. Open it now?", "Export complete", MessageBoxButtons.YesNo) == DialogResult.Yes) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = file.FileName, UseShellExecute = true }); }
                        catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Export could not finish"); save.Text = "Retry export"; }
                        finally { writingFile = false; save.Enabled = true; }
                    }
                };
                dialog.Controls.Add(preview); dialog.Controls.Add(controls); dialog.Controls.Add(save); refresh(); ThemeColors.Apply(dialog, config.Theme); dialog.ShowDialog(FindForm());
            }
        }
    }
}
