using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHEdit;

/// <summary>Shows an <see cref="ImportPlan"/> grouped by severity and asks whether to apply it.</summary>
internal static class ImportReportDialog
{
    /// <summary>Shows the report; true = apply. <paramref name="compareLeft"/>/<paramref name="showCompare"/>
    /// power the Compare button (opens PKM Compare; both null = button disabled). When
    /// <paramref name="offerDiscardItem"/> the dialog also offers applying without returning the held
    /// item, reported via <paramref name="discardItem"/>.</summary>
    public static bool Confirm(IWin32Window owner, ImportPlan plan, string title, Font font, PKM? compareLeft, Action<PKM>? showCompare, bool offerDiscardItem, out bool discardItem)
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
        // One-shot raise: when PKHEditor isn't the foreground window (e.g. the file was dropped from
        // another app), a normal modal can open BEHIND the active window and the user never sees it.
        // Toggling TopMost once while showing brings the dialog to the front, then it is released
        // immediately so it behaves like a normal window afterwards (nothing stays topmost).
        form.Shown += (_, _) =>
        {
            form.BringToFront();
            // form.TopMost = true;
            // form.TopMost = false;
            form.Activate();
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
        bottom.Controls.Add(cancel);
        bottom.Controls.Add(ok);
        if (offerDiscardItem)
        {
            // Leftmost of the RightToLeft row (still bottom-right of the dialog): apply without returning the held item.
            bottom.Controls.Add(new Button { Text = "Apply and Discard item", Size = new Size(160, 32), DialogResult = DialogResult.Yes, Enabled = !plan.IsBlocked });
        }
        // Added last in the RightToLeft flow → leftmost of the bottom row (bottom-left of the dialog):
        // opens PKM Compare with the current PKH's data for the incoming format on the left side.
        var compare = new Button { Text = "Compare", Size = new Size(90, 32), Enabled = compareLeft != null && showCompare != null };
        compare.Click += (_, _) => { if (compareLeft != null) showCompare?.Invoke(compareLeft); };
        bottom.Controls.Add(compare);

        form.AcceptButton = ok;
        form.CancelButton = cancel;

        form.Controls.Add(list);
        form.Controls.Add(bottom);
        var result = form.ShowDialog(owner);
        discardItem = result == DialogResult.Yes;
        return result is DialogResult.OK or DialogResult.Yes;
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
