using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;

namespace PKMMerge
{
    public partial class FormDiff : Form
    {
        IPKMView edit;
        public PKM? pk1;
        public PKM? pk2;
        public FormDiff(IPKMView edit, SaveFile sav)
        {
            InitializeComponent();
            dataGridView1.CellContentClick += OnDataGridViewCellClick;

            this.edit = edit;
            pk1 = edit.PreparePKM();
            pk2 = null;
            UpdateList();
        }

        public void UpdateList()
        {
            dataGridView1.Rows.Clear();

            PKM pk1 = this.pk1 ?? edit.PreparePKM();

            int rowIdx = 0;
            foreach (var item in EntityBatchEditor.Instance.Properties[pk1.Generation])
            {
                if (EntityBatchEditor.Instance.TryGetHasProperty(pk1, item, out var pi))
                {
                    GetPropertyDisplayText(pi, pk1, out var display1);
                    if (pk2 != null)
                    {
                        GetPropertyDisplayText(pi, pk2, out var display2);

                        bool isEqual = display1.Equals(display2);
                        bool isEmpty = string.IsNullOrEmpty(display1) && string.IsNullOrEmpty(display2);
                        if (CB_HideSame.Checked && isEqual)
                            continue;

                        if (CB_HideEmpty.Checked && isEmpty)
                            continue;

                        if (isEqual)
                        {
                            dataGridView1.Rows.Add(item, display1, "", "", display2);
                            dataGridView1.Rows[rowIdx].Cells[op1.Index] = new DataGridViewTextBoxCell();
                            dataGridView1.Rows[rowIdx].Cells[op2.Index] = new DataGridViewTextBoxCell();

                            rowIdx++;
                        }
                        else
                        {
                            dataGridView1.Rows.Add(item, display1, ">>", "<<", display2);
                            dataGridView1.Rows[rowIdx].Cells[op1.Index].Style.ForeColor = Color.LightCoral;
                            dataGridView1.Rows[rowIdx].Cells[op2.Index].Style.BackColor = Color.LightCoral;

                            rowIdx++;
                        }
                    }
                    else
                    {
                        dataGridView1.Rows.Add(item, display1, "", "", "");
                        rowIdx++;
                    }
                }
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
            String prop = dataGridView1.Rows[row].Cells[key.Index].Value?.ToString() ?? "";
            EntityBatchEditor.Instance.TryGetHasProperty(pk1, prop, out var pi1);
            EntityBatchEditor.Instance.TryGetHasProperty(pk2, prop, out var pi2);

            if (pi1 == null || pi2 == null)
            {
                return;
            }
            try
            {
                if (col == op1.Index)
                {
                    var value = pi1.GetValue(pk1);
                    pi2.SetValue(pk2, value);
                    UpdateList();
                }
                if (col == op2.Index)
                {
                    var value = pi2.GetValue(pk2);
                    pi1.SetValue(pk1, value);
                    UpdateList();
                }
            }
            catch
            {

            }
        }

        private static bool GetPropertyDisplayText(PropertyInfo pi, PKM pk, out string display)
        {
            var type = pi.PropertyType;
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition().IsByRefLike) // Span, ReadOnlySpan
                {
                    display = pi.PropertyType.ToString();
                    return false;
                }
            }

            var value = pi.GetValue(pk);
            if (value?.ToString() is not { } x)
            {
                display = "null";
                return false;
            }

            display = x;
            return true;
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
            edit.PopulateFields(pk1 ?? edit.PreparePKM());
        }

        private void B_Export2_Click(object sender, EventArgs e)
        {
            edit.PopulateFields(pk2 ?? edit.PreparePKM());
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
