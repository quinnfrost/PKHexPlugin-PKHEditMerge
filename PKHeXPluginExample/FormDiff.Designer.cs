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
            value2 = new System.Windows.Forms.DataGridViewButtonColumn();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { key, value1, op, value2 });
            dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            dataGridView1.Location = new System.Drawing.Point(0, 0);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new System.Drawing.Size(690, 650);
            dataGridView1.TabIndex = 0;
            // 
            // key
            // 
            key.HeaderText = "Key";
            key.MinimumWidth = 8;
            key.Name = "key";
            key.Width = 150;
            // 
            // value1
            // 
            value1.HeaderText = "Value1";
            value1.MinimumWidth = 8;
            value1.Name = "value1";
            value1.Width = 150;
            // 
            // op
            // 
            op.HeaderText = "Op";
            op.MinimumWidth = 8;
            op.Name = "op";
            op.Width = 150;
            // 
            // value2
            // 
            value2.HeaderText = "Value2";
            value2.MinimumWidth = 8;
            value2.Name = "value2";
            value2.Width = 150;
            // 
            // FormDiff
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(690, 650);
            Controls.Add(dataGridView1);
            Name = "FormDiff";
            Text = "FormDiff";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn key;
        private System.Windows.Forms.DataGridViewTextBoxColumn value1;
        private System.Windows.Forms.DataGridViewButtonColumn op;
        private System.Windows.Forms.DataGridViewButtonColumn value2;
    }
}
