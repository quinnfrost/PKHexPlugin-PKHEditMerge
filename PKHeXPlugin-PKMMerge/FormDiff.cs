using PKHeX.Core;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace PKMMerge
{
    public partial class FormDiff : Form
    {
        private readonly IPKMView edit;
        public PKM? pk1;
        public PKM? pk2;

        public FormDiff(IPKMView edit)
        {
            InitializeComponent();
            dataGridView1.CellContentClick += OnDataGridViewCellClick;

            this.edit = edit;
            pk1 = edit.PreparePKM();
            TB_PKM1_Name.Text = $"\"{pk1.Nickname}\"";
            UpdateList();
        }

        public void UpdateList()
        {
            dataGridView1.Rows.Clear();

            var pk1 = this.pk1 ??= edit.PreparePKM();
            var batch = EntityBatchEditor.Instance;

            // Properties[0] is the union of all entity types; per-type lists are offset by type index, not Generation.
            foreach (var item in batch.Properties[0])
            {
                bool has1 = batch.TryGetHasProperty(pk1, item, out var pi1);
                PropertyInfo? pi2 = null;
                bool has2 = pk2 != null && batch.TryGetHasProperty(pk2, item, out pi2);
                if (!has1 && !has2)
                    continue;

                string display1 = has1 ? GetPropertyDisplayText(pi1!, pk1) : "";
                if (pk2 == null)
                {
                    if (CB_HideEmpty.Checked && string.IsNullOrEmpty(display1))
                        continue;
                    AddRow(item, display1, "", canCopy1: false, canCopy2: false);
                    continue;
                }

                string display2 = has2 ? GetPropertyDisplayText(pi2!, pk2) : "";
                bool isEqual = has1 && has2 && display1 == display2;
                if (CB_HideSame.Checked && isEqual)
                    continue;
                if (CB_HideEmpty.Checked && string.IsNullOrEmpty(display1) && string.IsNullOrEmpty(display2))
                    continue;

                bool canCopy1 = !isEqual && has1 && has2 && pi1!.CanRead && pi2!.CanWrite;
                bool canCopy2 = !isEqual && has1 && has2 && pi2!.CanRead && pi1!.CanWrite;
                AddRow(item, display1, display2, canCopy1, canCopy2);
            }
        }

        private void AddRow(string key, string display1, string display2, bool canCopy1, bool canCopy2)
        {
            int idx = dataGridView1.Rows.Add(key, display1, canCopy1 ? ">>" : "", canCopy2 ? "<<" : "", display2);
            var row = dataGridView1.Rows[idx];
            if (!canCopy1)
                row.Cells[op1.Index] = new DataGridViewTextBoxCell();
            if (!canCopy2)
                row.Cells[op2.Index] = new DataGridViewTextBoxCell();
            if (canCopy1 || canCopy2)
            {
                row.Cells[value1.Index].Style.BackColor = Color.MistyRose;
                row.Cells[value2.Index].Style.BackColor = Color.MistyRose;
            }
        }

        private void OnDataGridViewCellClick(object? sender, DataGridViewCellEventArgs e)
        {
            int col = e.ColumnIndex;
            int row = e.RowIndex;

            if (row < 0 || col < 0)
                return;
            if (pk1 == null || pk2 == null)
            {
                return;
            }
            if (col != op1.Index && col != op2.Index)
                return;
            if (dataGridView1.Rows[row].Cells[col] is not DataGridViewButtonCell)
                return;

            string prop = dataGridView1.Rows[row].Cells[key.Index].Value?.ToString() ?? "";
            if (!EntityBatchEditor.Instance.TryGetHasProperty(pk1, prop, out var pi1) ||
                !EntityBatchEditor.Instance.TryGetHasProperty(pk2, prop, out var pi2))
            {
                return;
            }

            try
            {
                if (col == op1.Index)
                    pi2.SetValue(pk2, CloneValue(pi1.GetValue(pk1)));
                else
                    pi1.SetValue(pk1, CloneValue(pi2.GetValue(pk2)));
                UpdateList();
            }
            catch (Exception ex)
            {
                var msg = (ex as TargetInvocationException)?.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Failed to copy \"{prop}\": {msg}", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void B_Import1_Click(object sender, EventArgs e)
        {
            pk1 = edit.PreparePKM();
            TB_PKM1_Name.Text = $"\"{pk1.Nickname}\"";
            UpdateList();
        }

        private void B_Import2_Click(object sender, EventArgs e)
        {
            pk2 = edit.PreparePKM();
            TB_PKM2_Name.Text = $"\"{pk2.Nickname}\"";
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
