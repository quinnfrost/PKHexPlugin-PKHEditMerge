namespace PKHeXPluginExample
{
    partial class FormDiff
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dataGridView1 = new System.Windows.Forms.DataGridView();
            key = new System.Windows.Forms.DataGridViewTextBoxColumn();
            value1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            op = new System.Windows.Forms.DataGridViewButtonColumn();
            value2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            checkBox1 = new System.Windows.Forms.CheckBox();
            checkBox2 = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { key, value1, op, value2 });
            dataGridView1.Dock = System.Windows.Forms.DockStyle.Bottom;
            dataGridView1.Location = new System.Drawing.Point(0, 195);
            dataGridView1.Margin = new System.Windows.Forms.Padding(2);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new System.Drawing.Size(711, 308);
            dataGridView1.TabIndex = 0;
            // 
            // key
            // 
            key.HeaderText = "Key";
            key.MinimumWidth = 8;
            key.Name = "key";
            // 
            // value1
            // 
            value1.HeaderText = "Value1";
            value1.MinimumWidth = 8;
            value1.Name = "value1";
            value1.Width = 200;
            // 
            // op
            // 
            op.HeaderText = "Op";
            op.MinimumWidth = 8;
            op.Name = "op";
            op.Width = 50;
            // 
            // value2
            // 
            value2.HeaderText = "Value2";
            value2.MinimumWidth = 8;
            value2.Name = "value2";
            value2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            value2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            value2.Width = 200;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new System.Drawing.Point(12, 12);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new System.Drawing.Size(89, 21);
            checkBox1.TabIndex = 1;
            checkBox1.Text = "checkBox1";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new System.Drawing.Point(12, 39);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new System.Drawing.Size(89, 21);
            checkBox2.TabIndex = 2;
            checkBox2.Text = "checkBox2";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // FormDiff
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(711, 503);
            Controls.Add(checkBox2);
            Controls.Add(checkBox1);
            Controls.Add(dataGridView1);
            Margin = new System.Windows.Forms.Padding(2);
            Name = "FormDiff";
            Text = "FormDiff";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn key;
        private System.Windows.Forms.DataGridViewTextBoxColumn value1;
        private System.Windows.Forms.DataGridViewButtonColumn op;
        private System.Windows.Forms.DataGridViewTextBoxColumn value2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
    }
}
