using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PKMMerge;

/// <summary>Shows an <see cref="ImportPlan"/> grouped by severity and asks whether to apply it.</summary>
internal static class ImportReportDialog
{
    public static bool Confirm(IWin32Window owner, ImportPlan plan, string title, Font font)
    {
        using var form = new Form
        {
            Text = title,
            Font = font,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(760, 460),
            MinimumSize = new Size(480, 300),
        };

        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            ShowItemToolTips = true,
        };
        list.Columns.Add("Level", 80);
        list.Columns.Add("Area", 120);
        list.Columns.Add("Change", 540);

        // Most important first; each severity gets its own list group.
        foreach (var severity in new[] { ImportSeverity.Block, ImportSeverity.Confirm, ImportSeverity.Info })
        {
            var rows = plan.Items.Where(i => i.Severity == severity).ToList();
            if (rows.Count == 0)
                continue;
            var group = new ListViewGroup(severity switch
            {
                ImportSeverity.Block => $"Blocked ({rows.Count}) — the import cannot be applied",
                ImportSeverity.Confirm => $"Needs confirmation ({rows.Count})",
                _ => $"Expected PKHeX conversions ({rows.Count})",
            });
            list.Groups.Add(group);
            foreach (var row in rows)
            {
                var item = new ListViewItem([severity.ToString(), row.Group, row.Text], group) { ToolTipText = row.Text };
                if (severity == ImportSeverity.Block)
                    item.ForeColor = Color.DarkRed;
                else if (severity == ImportSeverity.Confirm)
                    item.ForeColor = Color.DarkGoldenrod;
                list.Items.Add(item);
            }
        }

        list.MouseDoubleClick += (_, _) =>
        {
            if (list.SelectedItems.Count != 0)
                ShowDetail(form, list.SelectedItems[0].SubItems[2].Text, font);
        };

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(8) };
        var ok = new Button { Text = plan.NeedsConfirm ? "Apply anyway" : "Apply", Size = new Size(110, 32), DialogResult = DialogResult.OK, Enabled = !plan.IsBlocked };
        var cancel = new Button { Text = "Cancel", Size = new Size(96, 32), DialogResult = DialogResult.Cancel };
        bottom.Controls.AddRange([cancel, ok]);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        form.Controls.Add(list);
        form.Controls.Add(bottom);
        return form.ShowDialog(owner) == DialogResult.OK;
    }

    // A plain read-only textbox is enough for a single message; mirrors this dialog's own style rather than a new form type.
    private static void ShowDetail(IWin32Window owner, string text, Font font)
    {
        using var detail = new Form
        {
            Text = "Change detail",
            Font = font,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(480, 220),
            MinimumSize = new Size(320, 180),
        };
        var box = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = text,
        };
        var bottom2 = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(8) };
        var close = new Button { Text = "Close", Size = new Size(96, 32), DialogResult = DialogResult.OK };
        bottom2.Controls.Add(close);
        detail.AcceptButton = close;
        detail.CancelButton = close;
        detail.Controls.Add(box);
        detail.Controls.Add(bottom2);
        detail.ShowDialog(owner);
    }
}
