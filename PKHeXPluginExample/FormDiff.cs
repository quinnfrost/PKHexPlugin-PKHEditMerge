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

namespace PKHeXPluginExample
{
    public partial class FormDiff : Form
    {
        public FormDiff(IPKMView edit, SaveFile sav)
        {
            InitializeComponent();

            BuildList(edit.PreparePKM(), edit.PreparePKM());
        }

        public void BuildList(PKM pk1, PKM pk2)
        {
            dataGridView1.Rows.Clear();

            foreach (var item in EntityBatchEditor.Instance.Properties[pk1.Generation])
            {
                if (EntityBatchEditor.Instance.TryGetHasProperty(pk1, item, out var pi))
                {
                    GetPropertyDisplayText(pi, pk1, out var display);
                    dataGridView1.Rows.Add(item, display, "==", display);
                }
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
    }
}
