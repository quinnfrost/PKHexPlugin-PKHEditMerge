namespace PKMMerge
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
            op1 = new System.Windows.Forms.DataGridViewButtonColumn();
            op2 = new System.Windows.Forms.DataGridViewButtonColumn();
            value2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CB_HideEmpty = new System.Windows.Forms.CheckBox();
            CB_HideSame = new System.Windows.Forms.CheckBox();
            B_Import1 = new System.Windows.Forms.Button();
            B_Import2 = new System.Windows.Forms.Button();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            pictureBox2 = new System.Windows.Forms.PictureBox();
            TB_PKM1_Name = new System.Windows.Forms.TextBox();
            TB_PKM2_Name = new System.Windows.Forms.TextBox();
            B_Export1 = new System.Windows.Forms.Button();
            B_Export2 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { key, value1, op1, op2, value2 });
            dataGridView1.Dock = System.Windows.Forms.DockStyle.Bottom;
            dataGridView1.Location = new System.Drawing.Point(0, 195);
            dataGridView1.Margin = new System.Windows.Forms.Padding(2);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new System.Drawing.Size(711, 308);
            dataGridView1.TabIndex = 0;
            // 
            // key
            // 
            key.HeaderText = "Key";
            key.MinimumWidth = 8;
            key.Name = "key";
            key.ReadOnly = true;
            // 
            // value1
            // 
            value1.HeaderText = "Value1";
            value1.MinimumWidth = 8;
            value1.Name = "value1";
            value1.ReadOnly = true;
            value1.Width = 200;
            // 
            // op1
            // 
            op1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            op1.HeaderText = "Op1";
            op1.MinimumWidth = 8;
            op1.Name = "op1";
            op1.ReadOnly = true;
            op1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            op1.Width = 50;
            // 
            // op2
            // 
            op2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            op2.HeaderText = "Op2";
            op2.Name = "op2";
            op2.ReadOnly = true;
            op2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            op2.Width = 50;
            // 
            // value2
            // 
            value2.HeaderText = "Value2";
            value2.Name = "value2";
            value2.ReadOnly = true;
            value2.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            value2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            value2.Width = 200;
            // 
            // CB_HideEmpty
            // 
            CB_HideEmpty.AutoSize = true;
            CB_HideEmpty.Location = new System.Drawing.Point(12, 12);
            CB_HideEmpty.Name = "CB_HideEmpty";
            CB_HideEmpty.Size = new System.Drawing.Size(90, 21);
            CB_HideEmpty.TabIndex = 1;
            CB_HideEmpty.Text = "HideEmpty";
            CB_HideEmpty.UseVisualStyleBackColor = true;
            CB_HideEmpty.CheckedChanged += CB_HideEmpty_CheckedChanged;
            // 
            // CB_HideSame
            // 
            CB_HideSame.AutoSize = true;
            CB_HideSame.Location = new System.Drawing.Point(12, 39);
            CB_HideSame.Name = "CB_HideSame";
            CB_HideSame.Size = new System.Drawing.Size(86, 21);
            CB_HideSame.TabIndex = 2;
            CB_HideSame.Text = "HideSame";
            CB_HideSame.UseVisualStyleBackColor = true;
            CB_HideSame.CheckedChanged += CB_HideSame_CheckedChanged;
            // 
            // B_Import1
            // 
            B_Import1.Location = new System.Drawing.Point(135, 64);
            B_Import1.Name = "B_Import1";
            B_Import1.Size = new System.Drawing.Size(75, 23);
            B_Import1.TabIndex = 3;
            B_Import1.Text = "Import";
            B_Import1.UseVisualStyleBackColor = true;
            B_Import1.Click += B_Import1_Click;
            // 
            // B_Import2
            // 
            B_Import2.Location = new System.Drawing.Point(495, 66);
            B_Import2.Name = "B_Import2";
            B_Import2.Size = new System.Drawing.Size(75, 23);
            B_Import2.TabIndex = 4;
            B_Import2.Text = "Import";
            B_Import2.UseVisualStyleBackColor = true;
            B_Import2.Click += B_Import2_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new System.Drawing.Point(230, 37);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(82, 75);
            pictureBox1.TabIndex = 5;
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox1.TabStop = false;
            // 
            // pictureBox2
            // 
            pictureBox2.Location = new System.Drawing.Point(366, 39);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new System.Drawing.Size(86, 73);
            pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 6;
            pictureBox2.TabStop = false;
            // 
            // TB_PKM1_Name
            // 
            TB_PKM1_Name.Location = new System.Drawing.Point(230, 118);
            TB_PKM1_Name.Name = "TB_PKM1_Name";
            TB_PKM1_Name.Size = new System.Drawing.Size(100, 23);
            TB_PKM1_Name.TabIndex = 7;
            // 
            // TB_PKM2_Name
            // 
            TB_PKM2_Name.Location = new System.Drawing.Point(366, 118);
            TB_PKM2_Name.Name = "TB_PKM2_Name";
            TB_PKM2_Name.Size = new System.Drawing.Size(100, 23);
            TB_PKM2_Name.TabIndex = 8;
            // 
            // B_Export1
            // 
            B_Export1.Location = new System.Drawing.Point(135, 93);
            B_Export1.Name = "B_Export1";
            B_Export1.Size = new System.Drawing.Size(75, 23);
            B_Export1.TabIndex = 9;
            B_Export1.Text = "Export";
            B_Export1.UseVisualStyleBackColor = true;
            B_Export1.Click += B_Export1_Click;
            // 
            // B_Export2
            // 
            B_Export2.Location = new System.Drawing.Point(495, 93);
            B_Export2.Name = "B_Export2";
            B_Export2.Size = new System.Drawing.Size(75, 23);
            B_Export2.TabIndex = 10;
            B_Export2.Text = "Export";
            B_Export2.UseVisualStyleBackColor = true;
            B_Export2.Click += B_Export2_Click;
            // 
            // FormDiff
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(711, 503);
            Controls.Add(B_Export2);
            Controls.Add(B_Export1);
            Controls.Add(TB_PKM2_Name);
            Controls.Add(TB_PKM1_Name);
            Controls.Add(pictureBox2);
            Controls.Add(pictureBox1);
            Controls.Add(B_Import2);
            Controls.Add(B_Import1);
            Controls.Add(CB_HideSame);
            Controls.Add(CB_HideEmpty);
            Controls.Add(dataGridView1);
            Margin = new System.Windows.Forms.Padding(2);
            Name = "FormDiff";
            Text = "FormDiff";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.CheckBox CB_HideEmpty;
        private System.Windows.Forms.CheckBox CB_HideSame;
        private System.Windows.Forms.Button B_Import1;
        private System.Windows.Forms.Button B_Import2;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.DataGridViewTextBoxColumn key;
        private System.Windows.Forms.DataGridViewTextBoxColumn value1;
        private System.Windows.Forms.DataGridViewButtonColumn op1;
        private System.Windows.Forms.DataGridViewButtonColumn op2;
        private System.Windows.Forms.DataGridViewTextBoxColumn value2;
        private System.Windows.Forms.TextBox TB_PKM1_Name;
        private System.Windows.Forms.TextBox TB_PKM2_Name;
        private System.Windows.Forms.Button B_Export1;
        private System.Windows.Forms.Button B_Export2;
    }
}
