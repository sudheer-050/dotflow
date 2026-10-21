using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;

namespace LocalDictation
{
    sealed class PersonalRule
    {
        public string Kind { get; set; }
        public string Trigger { get; set; }
        public string Replacement { get; set; }
        public static void Validate(IEnumerable<PersonalRule> rules)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in rules)
            {
                if (rule == null || (rule.Kind != "Word" && rule.Kind != "Snippet") || String.IsNullOrWhiteSpace(rule.Trigger) || String.IsNullOrWhiteSpace(rule.Replacement)) throw new ArgumentException("Each entry needs a Word or Snippet type, a trigger and replacement.");
                rule.Trigger = rule.Trigger.Trim();
                if (rule.Trigger.Length > 120 || rule.Replacement.Length > 8000 || !seen.Add(rule.Trigger)) throw new ArgumentException("Triggers must be unique and at most 120 characters; replacement limit is 8,000 characters.");
            }
        }
        public static string Apply(Config config, string text)
        {
            foreach (var rule in config.Personalization.OrderByDescending(r => r.Trigger.Length))
                text = Regex.Replace(text, @"(?<![\p{L}\p{N}])" + Regex.Escape(rule.Trigger) + @"(?![\p{L}\p{N}])", m => rule.Replacement, RegexOptions.IgnoreCase);
            return text;
        }
        public static void Edit(IWin32Window owner, Config config, Action changed)
        {
            using (var dialog = new Form { Text = "Dictionary & snippets", Size = new Size(760, 460), StartPosition = FormStartPosition.CenterParent, Font = new Font("Segoe UI", 10), Icon = AppLogo.Icon })
            {
                var grid = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true, BackgroundColor = Color.White };
                var kind = new DataGridViewComboBoxColumn { Name = "Kind", HeaderText = "Type", FillWeight = 22 }; kind.Items.AddRange("Word", "Snippet"); grid.Columns.Add(kind);
                grid.Columns.Add("Trigger", "Spoken word or phrase"); grid.Columns.Add("Replacement", "Correct spelling / saved text");
                foreach (var rule in config.Personalization) grid.Rows.Add(rule.Kind, rule.Trigger, rule.Replacement);
                var tip = new Label { Dock = DockStyle.Top, Height = 52, Text = "Words correct recurring spellings. Snippets expand a phrase into saved text.\nApplies to dictation only; recorded conversations remain unchanged.", Padding = new Padding(10) };
                var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48 };
                var save = new Button { Text = "Apply entries", AutoSize = true }; var import = new Button { Text = "Import JSON", AutoSize = true }; var export = new Button { Text = "Export JSON", AutoSize = true };
                Func<List<PersonalRule>> read = () => grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Select(r => new PersonalRule { Kind = Convert.ToString(r.Cells[0].Value), Trigger = Convert.ToString(r.Cells[1].Value), Replacement = Convert.ToString(r.Cells[2].Value) }).ToList();
                save.Click += (s, e) => { try { grid.EndEdit(); var rules = read(); Validate(rules); config.Personalization = rules; if (changed != null) changed(); dialog.Close(); } catch (Exception ex) { MessageBox.Show(dialog, ex.Message); } };
                import.Click += (s, e) => { using (var file = new OpenFileDialog { Filter = "Dictionary JSON|*.json" }) if (file.ShowDialog(dialog) == DialogResult.OK) try { var rules = NotesData.ReadRules(file.FileName); foreach (var rule in rules) grid.Rows.Add(rule.Kind, rule.Trigger, rule.Replacement); } catch (Exception ex) { MessageBox.Show(dialog, ex.Message); } };
                export.Click += (s, e) => { using (var file = new SaveFileDialog { Filter = "Dictionary JSON|*.json", DefaultExt = "json", AddExtension = true }) if (file.ShowDialog(dialog) == DialogResult.OK) try { var rules = read(); Validate(rules); NotesData.WriteRules(file.FileName, rules); } catch (Exception ex) { MessageBox.Show(dialog, ex.Message); } };
                footer.Controls.AddRange(new Control[] { save, import, export }); dialog.Controls.Add(grid); dialog.Controls.Add(tip); dialog.Controls.Add(footer); ThemeColors.Apply(dialog, config.Theme); dialog.ShowDialog(owner);
            }
        }
    }
}
