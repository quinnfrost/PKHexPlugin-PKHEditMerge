using PKHeX.Core;
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PKHEdit
{
    public partial class FormDiff : Form
    {
        // Declaration order is the Op column sort order.
        private enum DiffState { Different, Same, Empty, Incomparable }

        private sealed class RowInfo(string key, PropertyInfo? pi1, PropertyInfo? pi2)
        {
            public string Key { get; } = key;
            public PropertyInfo? Pi1 { get; } = pi1;
            public PropertyInfo? Pi2 { get; } = pi2;
            public DiffState State { get; set; }
            public bool IsEqual { get; set; }
            public bool CanCopy1 { get; set; }
            public bool CanCopy2 { get; set; }
            public bool CanEdit1 { get; set; }
            public bool CanEdit2 { get; set; }
        }

        // Different > Same > Empty, then by Key; descending flips only the group order.
        private sealed class StateComparer(ListSortDirection direction) : IComparer
        {
            public int Compare(object? x, object? y)
            {
                var a = (RowInfo)((DataGridViewRow)x!).Tag!;
                var b = (RowInfo)((DataGridViewRow)y!).Tag!;
                int cmp = a.State.CompareTo(b.State);
                if (direction == ListSortDirection.Descending)
                    cmp = -cmp;
                return cmp != 0 ? cmp : StringComparer.OrdinalIgnoreCase.Compare(a.Key, b.Key);
            }
        }

        private static readonly Color SameColor = Color.LightGreen;
        private static readonly Color DiffColor = Color.LightCoral;
        private static readonly Color IncomparableColor = Color.LightGray;

        private readonly IPKMView edit;
        private readonly ISaveFileProvider saveProvider;
        private PKM? pk1;
        private PKM? pk2;

        private DataGridViewColumn? sortColumn;
        private ListSortDirection sortDirection;

        private Rectangle dragBox = Rectangle.Empty;
        private int dragSourceSide;

        private sealed record HeaderIds(GroupBox Box, Label Chk, Label Pid, Label Ec);

        private readonly Font monoFont;
        private readonly HeaderIds ids1;
        private readonly HeaderIds ids2;

        public FormDiff(IPKMView edit, ISaveFileProvider saveProvider)
        {
            InitializeComponent();
            dataGridView1.CellContentClick += OnDataGridViewCellClick;
            dataGridView1.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
            dataGridView1.Sorted += OnSorted;
            dataGridView1.CellDoubleClick += OnCellDoubleClick;
            dataGridView1.CellValidating += OnCellValidating;
            dataGridView1.CellEndEdit += OnCellEndEdit;
            dataGridView1.KeyDown += OnGridKeyDown;
            FormClosed += (_, _) =>
            {
                pictureBox1.Image?.Dispose();
                pictureBox2.Image?.Dispose();
            };

            this.edit = edit;
            this.saveProvider = saveProvider;

            monoFont = new Font(FontFamily.GenericMonospace, Font.Size);
            Disposed += (_, _) => monoFont.Dispose();
            // Created before SetupDragDrop so the labels also accept drops.
            ids1 = CreateHeaderIds(GB_PKM1);
            ids2 = CreateHeaderIds(GB_PKM2);
            GB_PKM1.SizeChanged += (_, _) => LayoutHeaderIds(ids1);
            GB_PKM2.SizeChanged += (_, _) => LayoutHeaderIds(ids2);
            pictureBox1.Paint += OnSpritePaint;
            pictureBox2.Paint += OnSpritePaint;

            SetupDragDrop(GB_PKM1, pictureBox1, side: 1);
            SetupDragDrop(GB_PKM2, pictureBox2, side: 2);
            // Deferred: the combo box applies its dropdown choice after this event, which would undo a revert done inside it.
            CB_Format1.SelectionChangeCommitted += (_, _) => BeginInvoke(() => OnFormatChanged(CB_Format1, side: 1));
            CB_Format2.SelectionChangeCommitted += (_, _) => BeginInvoke(() => OnFormatChanged(CB_Format2, side: 2));
        }

        /// <summary>Rebuilds all rows, applying the Hide filters and the current sort.</summary>
        public void UpdateList()
        {
            var batch = EntityBatchEditor.Instance;
            string? topKey = GetTopKey();

            dataGridView1.SuspendLayout();
            try
            {
                dataGridView1.Rows.Clear();
                if (pk1 == null && pk2 == null)
                    return;

                // Properties[0] is the union of all entity types; per-type lists are offset by type index, not Generation.
                foreach (var key in batch.Properties[0])
                {
                    PropertyInfo? pi1 = null, pi2 = null;
                    if (pk1 != null)
                        batch.TryGetHasProperty(pk1, key, out pi1);
                    if (pk2 != null)
                        batch.TryGetHasProperty(pk2, key, out pi2);
                    if (pi1 == null && pi2 == null)
                        continue;

                    var info = new RowInfo(key, pi1, pi2);
                    Evaluate(info, out var display1, out var display2);
                    if (CB_HideSame.Checked && info.IsEqual)
                        continue;
                    if (CB_HideEmpty.Checked && info.State is DiffState.Empty or DiffState.Incomparable)
                        continue;

                    var row = dataGridView1.Rows[dataGridView1.Rows.Add(key)];
                    row.Tag = info;
                    ApplyRow(row, info, display1, display2);
                }

                ApplySort();
                RestoreTop(topKey);
            }
            finally
            {
                dataGridView1.ResumeLayout();
            }
        }

        /// <summary>Re-reads values for the existing rows without removing or reordering any of them.</summary>
        private void RefreshRows()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Tag is not RowInfo info)
                    continue;
                Evaluate(info, out var display1, out var display2);
                ApplyRow(row, info, display1, display2);
            }
        }

        private void Evaluate(RowInfo info, out string display1, out string display2)
        {
            var v1 = info.Pi1 != null && pk1 != null ? PropertyValue.Read(info.Pi1, pk1) : PropertyValue.None;
            var v2 = info.Pi2 != null && pk2 != null ? PropertyValue.Read(info.Pi2, pk2) : PropertyValue.None;
            display1 = v1.Display;
            display2 = v2.Display;
            info.IsEqual = false;
            info.CanCopy1 = info.CanCopy2 = false;
            info.CanEdit1 = pk1 != null && info.Pi1 != null && PropertyValue.CanEdit(info.Pi1);
            info.CanEdit2 = pk2 != null && info.Pi2 != null && PropertyValue.CanEdit(info.Pi2);

            if (!v1.IsComparable || !v2.IsComparable)
            {
                info.State = DiffState.Incomparable;
                return;
            }

            bool bothEmpty = string.IsNullOrEmpty(display1) && string.IsNullOrEmpty(display2);
            if (pk1 == null || pk2 == null)
            {
                info.State = bothEmpty ? DiffState.Empty : DiffState.Same;
                return;
            }

            info.IsEqual = info.Pi1 != null && info.Pi2 != null && display1 == display2;
            info.State = bothEmpty ? DiffState.Empty : info.IsEqual ? DiffState.Same : DiffState.Different;
            if (info.State != DiffState.Different || info.Pi1 is not { } pi1 || info.Pi2 is not { } pi2)
                return;

            info.CanCopy1 = PropertyValue.CanCopy(pi1, pk1, pi2, pk2);
            info.CanCopy2 = PropertyValue.CanCopy(pi2, pk2, pi1, pk1);
        }

        private static readonly Color ReadOnlyTextColor = SystemColors.GrayText;

        private void ApplyRow(DataGridViewRow row, RowInfo info, string display1, string display2)
        {
            SetValueCell(row.Cells[value1.Index], display1, info.CanEdit1);
            SetValueCell(row.Cells[value2.Index], display2, info.CanEdit2);

            bool binary = (info.Pi1 != null && pk1 != null && PropertyValue.ReadBytes(info.Pi1, pk1) != null)
                || (info.Pi2 != null && pk2 != null && PropertyValue.ReadBytes(info.Pi2, pk2) != null);
            var keyCell = row.Cells[key.Index];
            keyCell.ToolTipText = binary ? "Double-click to compare/edit as hex" : "";
            keyCell.Style.ForeColor = binary ? SystemColors.HotTrack : Color.Empty;

            Color? color = info.State switch
            {
                DiffState.Incomparable => IncomparableColor,
                _ when pk1 == null || pk2 == null => null,
                DiffState.Different => DiffColor,
                DiffState.Same => SameColor,
                _ => null,
            };
            SetOpCell(row, op1.Index, info.CanCopy1 ? ">>" : null, color);
            SetOpCell(row, op2.Index, info.CanCopy2 ? "<<" : null, color);
        }

        private static void SetValueCell(DataGridViewCell cell, string display, bool canEdit)
        {
            cell.Value = display;
            cell.ReadOnly = !canEdit;
            cell.Style.ForeColor = canEdit ? Color.Empty : ReadOnlyTextColor;
            cell.ErrorText = "";
        }

        private static void SetOpCell(DataGridViewRow row, int column, string? buttonText, Color? color)
        {
            var cell = row.Cells[column];
            if (buttonText != null && cell is not DataGridViewButtonCell)
                row.Cells[column] = cell = new DataGridViewButtonCell { FlatStyle = FlatStyle.Flat };
            else if (buttonText == null && cell is not DataGridViewTextBoxCell)
                row.Cells[column] = cell = new DataGridViewTextBoxCell();

            cell.Value = buttonText ?? "";
            cell.Style.BackColor = color ?? Color.Empty;
            cell.Style.SelectionBackColor = color is { } c ? Darken(c) : Color.Empty;
            cell.Style.SelectionForeColor = color.HasValue ? Color.Black : Color.Empty;
        }

        private static Color Darken(Color c) => Color.FromArgb(c.A, c.R * 4 / 5, c.G * 4 / 5, c.B * 4 / 5);

        private string? GetTopKey()
        {
            int top = dataGridView1.FirstDisplayedScrollingRowIndex;
            return top >= 0 && top < dataGridView1.Rows.Count ? (dataGridView1.Rows[top].Tag as RowInfo)?.Key : null;
        }

        private void RestoreTop(string? key)
        {
            if (key == null)
                return;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Tag is RowInfo info && info.Key == key)
                {
                    dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                    return;
                }
            }
        }

        private void OnDataGridViewCellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || pk1 == null || pk2 == null)
                return;
            bool toRight = e.ColumnIndex == op1.Index;
            if (!toRight && e.ColumnIndex != op2.Index)
                return;

            var row = dataGridView1.Rows[e.RowIndex];
            if (row.Cells[e.ColumnIndex] is not DataGridViewButtonCell)
                return;
            if (row.Tag is not RowInfo { Pi1: { } pi1, Pi2: { } pi2 } info)
                return;

            try
            {
                if (toRight)
                    PropertyValue.Copy(pi1, pk1, pi2, pk2);
                else
                    PropertyValue.Copy(pi2, pk2, pi1, pk1);
            }
            catch (Exception ex)
            {
                var msg = (ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Failed to copy \"{info.Key}\": {msg}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Deferred: refreshing replaces the clicked button cell, which must not happen inside its own click event.
            BeginInvoke(() =>
            {
                RefreshRows();
                if (toRight)
                    UpdateHeader(pictureBox2, TB_PKM2_Name, pk2, fromEditor: false);
                else
                    UpdateHeader(pictureBox1, TB_PKM1_Name, pk1, fromEditor: false);
                UpdateHeaderIds();
            });
        }

        private bool TryGetEditTarget(int rowIndex, int columnIndex, out RowInfo info, out PropertyInfo pi, out PKM pk)
        {
            info = null!;
            pi = null!;
            pk = null!;
            if (rowIndex < 0 || dataGridView1.Rows[rowIndex].Tag is not RowInfo row)
                return false;
            info = row;

            if (columnIndex == value1.Index && row is { CanEdit1: true, Pi1: { } p1 } && pk1 is { } k1)
                (pi, pk) = (p1, k1);
            else if (columnIndex == value2.Index && row is { CanEdit2: true, Pi2: { } p2 } && pk2 is { } k2)
                (pi, pk) = (p2, k2);
            else
                return false;
            return true;
        }

        private void OnCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == key.Index)
            {
                TryOpenHexCompare(e.RowIndex);
                return;
            }
            if (TryGetEditTarget(e.RowIndex, e.ColumnIndex, out _, out _, out _))
                dataGridView1.BeginEdit(selectAll: true);
        }

        /// <summary>Opens the side-by-side hex view for a binary (Span or byte[]) row.</summary>
        private void TryOpenHexCompare(int rowIndex)
        {
            if (rowIndex < 0 || dataGridView1.Rows[rowIndex].Tag is not RowInfo info)
                return;

            var left = info.Pi1 != null && pk1 != null ? PropertyValue.ReadBytes(info.Pi1, pk1) : null;
            var right = info.Pi2 != null && pk2 != null ? PropertyValue.ReadBytes(info.Pi2, pk2) : null;
            if (left == null && right == null)
                return;

            using var form = new HexCompareForm(info.Key, left, right, info.CanEdit1, info.CanEdit2, Font, Icon);
            if (form.ShowDialog(this) != DialogResult.OK)
                return;

            string? error = null;
            if (form.Result1 is { } r1 && !PropertyValue.TryWriteBytes(info.Pi1!, pk1!, r1, out var e1))
                error = $"PKM 1: {e1}";
            if (form.Result2 is { } r2 && !PropertyValue.TryWriteBytes(info.Pi2!, pk2!, r2, out var e2))
                error = error == null ? $"PKM 2: {e2}" : $"{error}{Environment.NewLine}PKM 2: {e2}";
            if (error != null)
                MessageBox.Show(error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);

            RefreshRows();
            if (form.Result1 != null && pk1 != null)
                UpdateHeader(pictureBox1, TB_PKM1_Name, pk1, fromEditor: false);
            if (form.Result2 != null && pk2 != null)
                UpdateHeader(pictureBox2, TB_PKM2_Name, pk2, fromEditor: false);
            UpdateHeaderIds();
        }

        private void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.F2 || dataGridView1.CurrentCell is not { } cell)
                return;
            if (TryGetEditTarget(cell.RowIndex, cell.ColumnIndex, out _, out _, out _))
            {
                dataGridView1.BeginEdit(selectAll: true);
                e.Handled = true;
            }
        }

        private void OnCellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (!dataGridView1.IsCurrentCellInEditMode)
                return;
            if (!TryGetEditTarget(e.RowIndex, e.ColumnIndex, out var info, out var pi, out var pk))
                return;

            var cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var text = e.FormattedValue?.ToString() ?? "";
            if (text == (cell.Value?.ToString() ?? ""))
                return;

            if (!PropertyValue.TryWrite(pi, pk, text, out var error))
            {
                // Keep the editor open so the value can be corrected; Esc reverts.
                cell.ErrorText = $"{info.Key}: {error}";
                e.Cancel = true;
                var rect = dataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, cutOverflow: false);
                toolTip1.Show($"{error} (Esc to cancel)", dataGridView1, rect.Left, rect.Bottom, 4000);
            }
            else
            {
                toolTip1.Hide(dataGridView1);
            }
        }

        private void OnCellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            bool left = e.ColumnIndex == value1.Index;
            // Deferred: a refresh rewrites cells, which must not happen while the grid is still ending the edit.
            BeginInvoke(() =>
            {
                RefreshRows();
                if (left && pk1 != null)
                    UpdateHeader(pictureBox1, TB_PKM1_Name, pk1, fromEditor: false);
                else if (!left && pk2 != null)
                    UpdateHeader(pictureBox2, TB_PKM2_Name, pk2, fromEditor: false);
                UpdateHeaderIds();
            });
        }

        private void OnColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            var column = dataGridView1.Columns[e.ColumnIndex];
            if (column != op1 && column != op2)
                return;

            sortDirection = sortColumn == column && sortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            sortColumn = column;
            ApplySort();
        }

        private void OnSorted(object? sender, EventArgs e)
        {
            // A built-in sort on Key/Value replaces the custom Op sort.
            if (dataGridView1.SortedColumn is not { } column)
                return;
            sortColumn = column;
            sortDirection = dataGridView1.SortOrder == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            op1.HeaderCell.SortGlyphDirection = SortOrder.None;
            op2.HeaderCell.SortGlyphDirection = SortOrder.None;
        }

        private void ApplySort()
        {
            if (sortColumn == null || dataGridView1.Rows.Count == 0)
                return;

            if (sortColumn != op1 && sortColumn != op2)
            {
                dataGridView1.Sort(sortColumn, sortDirection);
                return;
            }

            dataGridView1.Sort(new StateComparer(sortDirection));
            foreach (DataGridViewColumn column in dataGridView1.Columns)
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            sortColumn.HeaderCell.SortGlyphDirection = sortDirection == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;
        }

        private sealed record FormatItem(Type Type, string Text)
        {
            public override string ToString() => Text;
        }

        private static string GetDisplayName(PKM pk) => PkmUtil.GetDisplayName(pk);

        /// <summary>Re-applies names after PKHeX switches display language.</summary>
        public void RefreshNames()
        {
            if (pk1 != null)
                TB_PKM1_Name.Text = GetDisplayName(pk1);
            if (pk2 != null)
                TB_PKM2_Name.Text = GetDisplayName(pk2);
        }

        // Programmatic selection doesn't raise SelectionChangeCommitted, so rebuilding here never triggers a conversion.
        private static void PopulateFormats(ComboBox cb, PKM pk)
        {
            cb.BeginUpdate();
            try
            {
                cb.Items.Clear();
                foreach (var type in EntityBatchEditor.Instance.Types)
                {
                    var blank = EntityBlank.GetBlank(type);
                    if (type != pk.GetType() && !EntityConverter.IsConvertibleToFormat(pk, blank.Format))
                        continue;
                    var item = new FormatItem(type, $"{type.Name} ({blank.Context})");
                    cb.Items.Add(item);
                    if (type == pk.GetType())
                        cb.SelectedItem = item;
                }
                cb.Enabled = cb.Items.Count > 1;
            }
            finally
            {
                cb.EndUpdate();
            }
        }

        private void OnFormatChanged(ComboBox cb, int side)
        {
            if (cb.SelectedItem is not FormatItem { Type: var dest })
                return;
            var pk = side == 1 ? pk1 : pk2;
            if (pk == null || pk.GetType() == dest)
                return;

            PKM? converted;
            EntityConverterResult result;
            try
            {
                converted = EntityConverter.ConvertToType(pk, dest, out result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Conversion failed: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                PopulateFormats(cb, pk);
                return;
            }

            if (converted == null)
            {
                MessageBox.Show(result.GetDisplayString(pk, dest), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                PopulateFormats(cb, pk);
                return;
            }
            // Same as PKHeX's IsSilent, which is a C# 14 extension property this project (C# 12) can't call.
            if (result is not (EntityConverterResult.None or EntityConverterResult.Success))
                MessageBox.Show(result.GetDisplayString(pk, dest), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);

            SetSide(side, converted, fromEditor: false);
            UpdateList();
        }

        private void UpdateHeader(PictureBox pb, TextBox tb, PKM pk, bool fromEditor)
        {
            tb.Text = GetDisplayName(pk);
            PopulateFormats(pb == pictureBox1 ? CB_Format1 : CB_Format2, pk);

            // The editor preview only matches pk when pk was just taken from the editor.
            var img = PKMSprite.Render(pk) ?? (fromEditor ? PKMSprite.CopyEditorPreview() : null);
            if (img == null && !fromEditor)
                return;
            var old = pb.Image;
            pb.Image = img;
            old?.Dispose();
        }

        private void SetSide(int side, PKM pk, bool fromEditor)
        {
            if (side == 1)
            {
                pk1 = pk;
                UpdateHeader(pictureBox1, TB_PKM1_Name, pk, fromEditor);
            }
            else
            {
                pk2 = pk;
                UpdateHeader(pictureBox2, TB_PKM2_Name, pk, fromEditor);
            }
            UpdateHeaderIds();
        }

        #region Header IDs (checksum / PID / EC)

        private HeaderIds CreateHeaderIds(GroupBox box)
        {
            Label Make() => new()
            {
                AutoSize = true,
                Font = monoFont,
                Padding = new Padding(2, 0, 2, 0),
                Margin = Padding.Empty,
                Visible = false,
            };

            var ids = new HeaderIds(box, Make(), Make(), Make());
            foreach (var label in new[] { ids.Chk, ids.Pid, ids.Ec })
            {
                box.Controls.Add(label);
                label.BringToFront();
            }
            return ids;
        }

        private static ushort GetFileNameChecksum(PKM pk) => PkmUtil.GetFileNameChecksum(pk);

        // Same-Pokémon (EC) indicators. Each can be switched off independently.
        private const bool ShowEcLabelTint = true;
        private const bool ShowSpriteBorder = true;
        private const int SpriteBorderWidth = 3;

        /// <summary>Current same-Pokémon result, or null when either side is empty.</summary>
        private Color? ecMatchColor;

        /// <summary>Refreshes both sides, since the EC colour depends on the other side too.</summary>
        private void UpdateHeaderIds()
        {
            ApplyHeaderIds(ids1, pk1);
            ApplyHeaderIds(ids2, pk2);

            ecMatchColor = GetEcMatch(out var match);
            foreach (var (ids, pk, sprite) in new[] { (ids1, pk1, pictureBox1), (ids2, pk2, pictureBox2) })
            {
                var tip = pk == null ? "" : $"EC: {pk.EncryptionConstant:X8}";
                if (match.Length != 0)
                    tip = $"{tip}{Environment.NewLine}{match}";

                if (pk != null)
                {
                    if (ShowEcLabelTint)
                        ApplyEcLabelTint(ids);
                    toolTip1.SetToolTip(ids.Ec, tip);
                }

                if (ShowSpriteBorder)
                {
                    toolTip1.SetToolTip(sprite, match.Length != 0 ? $"{SpriteToolTip}{Environment.NewLine}{match}" : SpriteToolTip);
                    sprite.Invalidate();
                }
            }
        }

        private const string SpriteToolTip = "Drop a PKM file or box slot here. Drag the sprite out to export.";

        private Color? GetEcMatch(out string match)
        {
            match = "";
            if (pk1 == null || pk2 == null)
                return null;

            uint ec1 = pk1.EncryptionConstant, ec2 = pk2.EncryptionConstant;
            // EC 0 is a blank entity or Gen1/2 data, so it says nothing about identity.
            if (ec1 == 0 || ec2 == 0)
            {
                match = "EC is 0: cannot tell whether they are the same Pokémon.";
                return IncomparableColor;
            }
            if (ec1 == ec2)
            {
                match = "Same EC: likely the same Pokémon.";
                return SameColor;
            }
            match = "Different EC: different Pokémon.";
            return DiffColor;
        }

        private void ApplyEcLabelTint(HeaderIds ids)
        {
            ids.Ec.BackColor = ecMatchColor ?? ids.Box.BackColor;
            ids.Ec.ForeColor = ecMatchColor.HasValue ? Color.Black : ids.Box.ForeColor;
        }

        // Drawn inside the sprite box; sprites are transparent PNGs, so the frame stays visible.
        private void OnSpritePaint(object? sender, PaintEventArgs e)
        {
            if (!ShowSpriteBorder || ecMatchColor is not { } color || sender is not PictureBox pb)
                return;
            using var pen = new Pen(color, SpriteBorderWidth) { Alignment = PenAlignment.Inset };
            e.Graphics.DrawRectangle(pen, 0, 0, pb.ClientSize.Width - 1, pb.ClientSize.Height - 1);
        }

        private void ApplyHeaderIds(HeaderIds ids, PKM? pk)
        {
            bool loaded = pk != null;
            ids.Chk.Visible = ids.Pid.Visible = ids.Ec.Visible = loaded;
            if (pk == null)
                return;

            ids.Chk.Text = $"{GetFileNameChecksum(pk):X4}";
            ids.Pid.Text = $"{pk.PID:X8}";
            ids.Ec.Text = $"{pk.EncryptionConstant:X8}";
            toolTip1.SetToolTip(ids.Chk, $"chk: {ids.Chk.Text}");
            toolTip1.SetToolTip(ids.Pid, $"PID: {ids.Pid.Text}");
            foreach (var label in new[] { ids.Chk, ids.Pid, ids.Ec })
            {
                label.BackColor = ids.Box.BackColor;
                label.ForeColor = ids.Box.ForeColor;
            }
            LayoutHeaderIds(ids);
        }

        // Each label covers only its own stretch of the top border, so the line stays visible between them.
        private static void LayoutHeaderIds(HeaderIds ids)
        {
            if (!ids.Chk.Visible)
                return;

            const int gap = 10;
            var titleWidth = TextRenderer.MeasureText(ids.Box.Text, ids.Box.Font).Width;
            int x = 8 + titleWidth + gap;
            int lineY = ids.Box.Font.Height / 2;
            foreach (var label in new[] { ids.Chk, ids.Pid, ids.Ec })
            {
                label.Location = new Point(x, Math.Max(0, lineY - (label.Height / 2)));
                x += label.Width + gap;
            }
        }

        #endregion

        #region Drag & Drop

        private void SetupDragDrop(Control area, PictureBox sprite, int side)
        {
            foreach (var c in new[] { area }.Concat(area.Controls.Cast<Control>()))
            {
                c.AllowDrop = true;
                c.DragEnter += (_, e) => OnAreaDragEnter(e, side);
                c.DragDrop += (_, e) => OnAreaDragDrop(e, side);
            }

            sprite.MouseDown += (_, e) =>
            {
                var size = SystemInformation.DragSize;
                dragBox = e.Button == MouseButtons.Left
                    ? new Rectangle(e.X - (size.Width / 2), e.Y - (size.Height / 2), size.Width, size.Height)
                    : Rectangle.Empty;
            };
            sprite.MouseUp += (_, _) => dragBox = Rectangle.Empty;
            sprite.MouseMove += (_, e) =>
            {
                if (e.Button != MouseButtons.Left || dragBox == Rectangle.Empty || dragBox.Contains(e.Location))
                    return;
                dragBox = Rectangle.Empty;
                BeginDragOut(sprite, side);
            };
        }

        private void OnAreaDragEnter(DragEventArgs e, int side)
        {
            bool accept = side != dragSourceSide
                && e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                && e.AllowedEffect.HasFlag(DragDropEffects.Copy);
            // Must be Copy: PKHeX's box slots treat Link as "moved to another slot".
            e.Effect = accept ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void OnAreaDragDrop(DragEventArgs e, int side)
        {
            if (side == dragSourceSide || e.Data?.GetData(DataFormats.FileDrop) is not string[] { Length: not 0 } files)
                return;

            // Two files dropped at once fill both sides, starting with the drop target.
            int other = side == 1 ? 2 : 1;
            int[] sides = files.Length >= 2 ? [side, other] : [side];
            for (int i = 0; i < sides.Length; i++)
            {
                if (!TryLoadFile(files[i], out var pk, out var error))
                {
                    MessageBox.Show($"Unable to load \"{Path.GetFileName(files[i])}\": {error}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }
                SetSide(sides[i], pk, fromEditor: false);
            }
            UpdateList();
        }

        private bool TryLoadFile(string path, [NotNullWhen(true)] out PKM? pk, out string error)
            => PkmUtil.TryLoadPkm(path, saveProvider.SAV, out pk, out error);

        private void BeginDragOut(PictureBox sprite, int side)
        {
            if ((side == 1 ? pk1 : pk2) is not { } source)
                return;

            dragSourceSide = side;
            try { PkmUtil.DragOut(sprite, source); }
            finally { dragSourceSide = 0; }
        }

        #endregion

        private void B_Import1_Click(object sender, EventArgs e)
        {
            SetSide(1, edit.PreparePKM(), fromEditor: true);
            UpdateList();
        }

        private void B_Import2_Click(object sender, EventArgs e)
        {
            SetSide(2, edit.PreparePKM(), fromEditor: true);
            UpdateList();
        }

        private void B_Export1_Click(object sender, EventArgs e)
        {
            if (pk1 is { } p)
                edit.PopulateFields(p);
        }

        private void B_Export2_Click(object sender, EventArgs e)
        {
            if (pk2 is { } p)
                edit.PopulateFields(p);
        }

        private void CB_HideEmpty_CheckedChanged(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void CB_HideSame_CheckedChanged(object sender, EventArgs e)
        {
            UpdateList();
        }
    }
}
