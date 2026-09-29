using PKHeX.Core;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace PKMMerge
{
    public partial class FormDiff : Form
    {
        private enum DiffState { Different, Same, Empty }

        private sealed class RowInfo(string key, PropertyInfo? pi1, PropertyInfo? pi2)
        {
            public string Key { get; } = key;
            public PropertyInfo? Pi1 { get; } = pi1;
            public PropertyInfo? Pi2 { get; } = pi2;
            public DiffState State { get; set; }
            public bool IsEqual { get; set; }
            public bool CanCopy1 { get; set; }
            public bool CanCopy2 { get; set; }
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

        // PKHeX deletes its own drag-out temp files after a similar delay, giving the drop target time to read them.
        private static readonly TimeSpan TempFileLifetime = TimeSpan.FromSeconds(20);

        private readonly IPKMView edit;
        private readonly ISaveFileProvider saveProvider;
        public PKM? pk1;
        public PKM? pk2;

        private DataGridViewColumn? sortColumn;
        private ListSortDirection sortDirection;

        private Rectangle dragBox = Rectangle.Empty;
        private int dragSourceSide;

        public FormDiff(IPKMView edit, ISaveFileProvider saveProvider)
        {
            InitializeComponent();
            dataGridView1.CellContentClick += OnDataGridViewCellClick;
            dataGridView1.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
            dataGridView1.Sorted += OnSorted;
            FormClosed += (_, _) =>
            {
                pictureBox1.Image?.Dispose();
                pictureBox2.Image?.Dispose();
            };

            this.edit = edit;
            this.saveProvider = saveProvider;
            SetupDragDrop(GB_PKM1, pictureBox1, side: 1);
            SetupDragDrop(GB_PKM2, pictureBox2, side: 2);
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
                    if (CB_HideEmpty.Checked && info.State == DiffState.Empty)
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
            display1 = info.Pi1 != null && pk1 != null ? GetPropertyDisplayText(info.Pi1, pk1) : "";
            display2 = info.Pi2 != null && pk2 != null ? GetPropertyDisplayText(info.Pi2, pk2) : "";

            bool bothEmpty = string.IsNullOrEmpty(display1) && string.IsNullOrEmpty(display2);
            if (pk1 == null || pk2 == null)
            {
                info.IsEqual = false;
                info.State = bothEmpty ? DiffState.Empty : DiffState.Same;
                info.CanCopy1 = info.CanCopy2 = false;
                return;
            }

            info.IsEqual = info.Pi1 != null && info.Pi2 != null && display1 == display2;
            info.State = bothEmpty ? DiffState.Empty : info.IsEqual ? DiffState.Same : DiffState.Different;

            bool canCopy = info.State == DiffState.Different && info.Pi1 != null && info.Pi2 != null;
            info.CanCopy1 = canCopy && info.Pi1!.CanRead && info.Pi2!.CanWrite;
            info.CanCopy2 = canCopy && info.Pi2!.CanRead && info.Pi1!.CanWrite;
        }

        private void ApplyRow(DataGridViewRow row, RowInfo info, string display1, string display2)
        {
            row.Cells[value1.Index].Value = display1;
            row.Cells[value2.Index].Value = display2;

            Color? color = pk1 == null || pk2 == null ? null : info.State switch
            {
                DiffState.Different => DiffColor,
                DiffState.Same => SameColor,
                _ => null,
            };
            SetOpCell(row, op1.Index, info.CanCopy1 ? ">>" : null, color);
            SetOpCell(row, op2.Index, info.CanCopy2 ? "<<" : null, color);
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
                    pi2.SetValue(pk2, CloneValue(pi1.GetValue(pk1)));
                else
                    pi1.SetValue(pk1, CloneValue(pi2.GetValue(pk2)));
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

        private static void UpdateHeader(PictureBox pb, TextBox tb, PKM pk, bool fromEditor)
        {
            tb.Text = $"\"{pk.Nickname}\"";

            // The editor preview only matches pk when pk was just taken from the editor.
            var img = PKMSprite.Render(pk) ?? (fromEditor ? PKMSprite.CopyEditorPreview() : null);
            if (img == null && !fromEditor)
                return;
            var old = pb.Image;
            pb.Image = img;
            old?.Dispose();
        }

        // Avoid sharing array instances between the two entities.
        private static object? CloneValue(object? value) => value is Array a ? a.Clone() : value;

        private static string GetPropertyDisplayText(PropertyInfo pi, PKM pk)
        {
            var type = pi.PropertyType;
            if (type.IsByRefLike || !pi.CanRead || pi.GetIndexParameters().Length != 0)
                return type.ToString();

            object? value;
            try { value = pi.GetValue(pk); }
            catch (TargetInvocationException) { return "<error>"; }

            return value switch
            {
                null => "null",
                byte[] b => Convert.ToHexString(b),
                Array a => string.Join(", ", a.Cast<object?>()),
                _ => value.ToString() ?? "null",
            };
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
        }

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
                    MessageBox.Show($"Unable to load \"{System.IO.Path.GetFileName(files[i])}\": {error}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }
                SetSide(sides[i], pk, fromEditor: false);
            }
            UpdateList();
        }

        private bool TryLoadFile(string path, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out PKM? pk, out string error)
        {
            pk = null;
            try
            {
                var sav = saveProvider.SAV;
                pk = FileUtil.GetSupportedFile(path, sav) switch
                {
                    PKM p => p,
                    MysteryGift g => g.ConvertToPKM(sav),
                    IEncounterConvertible enc => enc.ConvertToPKM(sav),
                    _ => null,
                };
                error = pk == null ? "not a supported PKM file." : "";
                return pk != null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private void BeginDragOut(PictureBox sprite, int side)
        {
            var source = side == 1 ? pk1 : pk2;
            if (source == null)
                return;

            string? file = null;
            dragSourceSide = side;
            try
            {
                var pk = source.Clone();
                pk.ForcePartyData();
                var data = new byte[pk.SIZE_PARTY];
                pk.WriteDecryptedDataParty(data);

                file = FileUtil.GetPKMTempFileName(pk, encrypt: false);
                System.IO.File.WriteAllBytes(file, data);
                sprite.DoDragDrop(new DataObject(DataFormats.FileDrop, new[] { file }), DragDropEffects.Copy);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Drag && Drop failed: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                dragSourceSide = 0;
                if (file != null)
                    _ = DeleteLaterAsync(file);
            }
        }

        private static async System.Threading.Tasks.Task DeleteLaterAsync(string file)
        {
            await System.Threading.Tasks.Task.Delay(TempFileLifetime).ConfigureAwait(false);
            try { System.IO.File.Delete(file); }
            catch (System.IO.IOException) { }
            catch (UnauthorizedAccessException) { }
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
