using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace PKHEdit;

/// <summary>Side-by-side hex view of one binary property that highlights differing bytes and allows editing.</summary>
public partial class HexCompareForm : Form
{
    private const int BytesPerRow = 16;
    private const int Col1Start = 1;
    private const int Col2Start = Col1Start + BytesPerRow + 1;
    private static readonly Color DiffColor = Color.LightCoral;
    private static readonly Color MissingColor = Color.LightGray;

    private readonly byte[] data1 = [];
    private readonly byte[] data2 = [];
    private readonly byte[] original1 = [];
    private readonly byte[] original2 = [];
    private readonly bool has1;
    private readonly bool has2;
    private readonly Font monoBold;

    /// <summary>Edited bytes for the left side, or null if unchanged.</summary>
    public byte[]? Result1 { get; private set; }

    /// <summary>Edited bytes for the right side, or null if unchanged.</summary>
    public byte[]? Result2 { get; private set; }

    public HexCompareForm(string key, byte[]? left, byte[]? right, bool canEdit1, bool canEdit2, Font baseFont, Icon? icon)
    {
        InitializeComponent();

        has1 = left != null;
        has2 = right != null;
        data1 = left?.ToArray() ?? [];
        data2 = right?.ToArray() ?? [];
        original1 = data1.ToArray();
        original2 = data2.ToArray();

        var mono = DGV_Hex.DefaultCellStyle.Font!;
        monoBold = new Font(mono, FontStyle.Bold);
        Disposed += (_, _) => monoBold.Dispose();

        Text = $"{key} - Hex Compare";
        Font = baseFont;
        if (icon != null)
            Icon = icon;
        PAN_SideHeader.Height = Font.Height + 8;
        DGV_Hex.RowTemplate.Height = mono.Height + 6;

        AddColumns(mono, canEdit1 && has1, canEdit2 && has2);
        FillRows();
        UpdateSummary();
        FitToContent();
    }

    // The 34 columns depend on the byte count and editability, so they are built here rather than in the designer.
    private void AddColumns(Font mono, bool canEdit1, bool canEdit2)
    {
        int hexWidth = TextRenderer.MeasureText("WW", mono).Width + 8;
        var offset = MakeColumn("Offset", TextRenderer.MeasureText("0x0000", mono).Width + 12, readOnly: true);
        offset.DefaultCellStyle.ForeColor = SystemColors.GrayText;
        DGV_Hex.Columns.Add(offset);
        AddHexColumns(hexWidth, !canEdit1);
        var separator = MakeColumn("", 10, readOnly: true);
        separator.DefaultCellStyle.BackColor = SystemColors.Control;
        DGV_Hex.Columns.Add(separator);
        AddHexColumns(hexWidth, !canEdit2);
    }

    private void AddHexColumns(int width, bool readOnly)
    {
        for (int i = 0; i < BytesPerRow; i++)
            DGV_Hex.Columns.Add(MakeColumn($"{i:X2}", width, readOnly, maxInput: 2));
    }

    private static DataGridViewTextBoxColumn MakeColumn(string header, int width, bool readOnly, int maxInput = 0)
    {
        var column = new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Width = width,
            ReadOnly = readOnly,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.False,
        };
        if (maxInput > 0)
            column.MaxInputLength = maxInput;
        return column;
    }

    private void FitToContent()
    {
        int width = DGV_Hex.Columns.Cast<DataGridViewColumn>().Sum(c => c.Width) + SystemInformation.VerticalScrollBarWidth + 4;
        int rowsShown = Math.Min(DGV_Hex.Rows.Count, 24);
        int height = DGV_Hex.ColumnHeadersHeight + (rowsShown * DGV_Hex.RowTemplate.Height) + 4;
        ClientSize = new Size(Math.Max(width, FLP_Buttons.PreferredSize.Width + 200), height + PAN_SideHeader.Height + PAN_Bottom.Height);
        MinimumSize = new Size(Width, Math.Min(Height, 300));
    }

    private int ByteCount => Math.Max(data1.Length, data2.Length);

    private void FillRows()
    {
        int rows = (ByteCount + BytesPerRow - 1) / BytesPerRow;
        for (int r = 0; r < rows; r++)
        {
            var row = DGV_Hex.Rows[DGV_Hex.Rows.Add()];
            row.Cells[0].Value = $"0x{r * BytesPerRow:X4}";
            for (int c = 0; c < BytesPerRow; c++)
            {
                int index = (r * BytesPerRow) + c;
                SetByteCell(row.Cells[Col1Start + c], data1, has1, index);
                SetByteCell(row.Cells[Col2Start + c], data2, has2, index);
                UpdateByteStyle(index);
            }
        }
    }

    private static void SetByteCell(DataGridViewCell cell, byte[] data, bool has, int index)
    {
        bool exists = has && index < data.Length;
        cell.Value = exists ? $"{data[index]:X2}" : "";
        if (!exists)
            cell.ReadOnly = true;
    }

    private bool IsDifferent(int index)
    {
        if (!has1 || !has2)
            return false;
        bool in1 = index < data1.Length, in2 = index < data2.Length;
        if (in1 != in2)
            return true;
        return in1 && data1[index] != data2[index];
    }

    private static bool IsModified(byte[] data, byte[] original, int index)
        => index < data.Length && data[index] != original[index];

    private void UpdateByteStyle(int index)
    {
        int r = index / BytesPerRow, c = index % BytesPerRow;
        if (r >= DGV_Hex.Rows.Count)
            return;
        var row = DGV_Hex.Rows[r];
        bool diff = IsDifferent(index);
        StyleByteCell(row.Cells[Col1Start + c], has1 && index < data1.Length, diff, IsModified(data1, original1, index));
        StyleByteCell(row.Cells[Col2Start + c], has2 && index < data2.Length, diff, IsModified(data2, original2, index));
    }

    private void StyleByteCell(DataGridViewCell cell, bool exists, bool diff, bool modified)
    {
        var style = cell.Style;
        Color? back = !exists ? MissingColor : diff ? DiffColor : null;
        style.BackColor = back ?? Color.Empty;
        style.ForeColor = back.HasValue ? Color.Black : Color.Empty;
        style.SelectionBackColor = back is { } b ? Color.FromArgb(b.R * 4 / 5, b.G * 4 / 5, b.B * 4 / 5) : Color.Empty;
        style.SelectionForeColor = back.HasValue ? Color.Black : Color.Empty;
        // Bold marks bytes edited in this window.
        style.Font = modified ? monoBold : null;
    }

    private void UpdateSummary()
    {
        if (!has1 || !has2)
        {
            L_Summary.Text = $"{ByteCount} bytes";
            return;
        }

        int diffs = Enumerable.Range(0, ByteCount).Count(IsDifferent);
        L_Summary.Text = data1.Length == data2.Length
            ? $"{diffs} of {ByteCount} bytes differ"
            : $"{diffs} bytes differ (sizes 0x{data1.Length:X} vs 0x{data2.Length:X})";
    }

    private bool TryGetByte(int rowIndex, int columnIndex, out int side, out int index)
    {
        side = 0;
        index = -1;
        if (rowIndex < 0)
            return false;
        if (columnIndex is >= Col1Start and < Col1Start + BytesPerRow)
            (side, index) = (1, (rowIndex * BytesPerRow) + (columnIndex - Col1Start));
        else if (columnIndex is >= Col2Start and < Col2Start + BytesPerRow)
            (side, index) = (2, (rowIndex * BytesPerRow) + (columnIndex - Col2Start));
        else
            return false;
        return index < (side == 1 ? data1 : data2).Length;
    }

    private void DGV_Hex_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (!DGV_Hex.IsCurrentCellInEditMode || !TryGetByte(e.RowIndex, e.ColumnIndex, out _, out _))
            return;

        var text = e.FormattedValue?.ToString()?.Trim() ?? "";
        var cell = DGV_Hex.Rows[e.RowIndex].Cells[e.ColumnIndex];
        if (text.Length is 1 or 2 && byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            cell.ErrorText = "";
            return;
        }
        cell.ErrorText = "Enter a hex byte (00-FF). Esc to cancel.";
        e.Cancel = true;
    }

    private void DGV_Hex_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (!TryGetByte(e.RowIndex, e.ColumnIndex, out int side, out int index))
            return;

        var cell = DGV_Hex.Rows[e.RowIndex].Cells[e.ColumnIndex];
        var data = side == 1 ? data1 : data2;
        if (byte.TryParse(cell.Value?.ToString()?.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            data[index] = value;
        cell.Value = $"{data[index]:X2}";
        cell.ErrorText = "";
        UpdateByteStyle(index);
        UpdateSummary();
    }

    private void JumpToDiff(bool forward)
    {
        int count = ByteCount;
        if (count == 0)
            return;

        int current = DGV_Hex.CurrentCell is { } cell && TryGetByte(cell.RowIndex, cell.ColumnIndex, out _, out int i) ? i : -1;
        for (int step = 1; step <= count; step++)
        {
            int index = forward ? (current + step) % count : (((current - step) % count) + count) % count;
            if (!IsDifferent(index))
                continue;
            int column = (index < data1.Length ? Col1Start : Col2Start) + (index % BytesPerRow);
            DGV_Hex.CurrentCell = DGV_Hex.Rows[index / BytesPerRow].Cells[column];
            return;
        }
        System.Media.SystemSounds.Beep.Play();
    }

    private void B_NextDiff_Click(object? sender, EventArgs e) => JumpToDiff(forward: true);

    private void B_PrevDiff_Click(object? sender, EventArgs e) => JumpToDiff(forward: false);

    private void B_Save_Click(object? sender, EventArgs e)
    {
        if (DGV_Hex.IsCurrentCellInEditMode && !DGV_Hex.EndEdit())
            return;
        Result1 = has1 && !data1.AsSpan().SequenceEqual(original1) ? data1.ToArray() : null;
        Result2 = has2 && !data2.AsSpan().SequenceEqual(original2) ? data2.ToArray() : null;
        DialogResult = DialogResult.OK;
    }

    private void DGV_Hex_ColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e) => PositionSideLabels();

    private void DGV_Hex_Scroll(object? sender, ScrollEventArgs e) => PositionSideLabels();

    private void HexCompareForm_Shown(object? sender, EventArgs e) => PositionSideLabels();

    private void PositionSideLabels()
    {
        if (DGV_Hex.Columns.Count <= Col2Start + BytesPerRow - 1)
            return;
        PlaceOver(L_Side1, Col1Start);
        PlaceOver(L_Side2, Col2Start);
    }

    private void PlaceOver(Label label, int firstColumn)
    {
        var first = DGV_Hex.GetColumnDisplayRectangle(firstColumn, cutOverflow: false);
        var last = DGV_Hex.GetColumnDisplayRectangle(firstColumn + BytesPerRow - 1, cutOverflow: false);
        label.Visible = first.Width > 0 && last.Width > 0;
        label.Bounds = new Rectangle(DGV_Hex.Left + first.Left, 0, last.Right - first.Left, PAN_SideHeader.Height);
    }

    private void HexCompareForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
            return;
        bool modified = !data1.AsSpan().SequenceEqual(original1) || !data2.AsSpan().SequenceEqual(original2);
        if (modified && MessageBox.Show(this, "Discard the edited bytes?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            e.Cancel = true;
    }
}
