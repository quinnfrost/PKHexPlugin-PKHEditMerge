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
            components = new System.ComponentModel.Container();
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
            GB_PKM1 = new System.Windows.Forms.GroupBox();
            GB_PKM2 = new System.Windows.Forms.GroupBox();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            GB_PKM1.SuspendLayout();
            GB_PKM2.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dataGridView1.BackgroundColor = System.Drawing.SystemColors.Window;
            dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { key, value1, op1, op2, value2 });
            dataGridView1.Location = new System.Drawing.Point(8, 128);
            dataGridView1.Margin = new System.Windows.Forms.Padding(2);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new System.Drawing.Size(704, 424);
            dataGridView1.TabIndex = 2;
            // 
            // key
            // 
            key.HeaderText = "Key";
            key.MinimumWidth = 80;
            key.Name = "key";
            key.ReadOnly = true;
            key.Width = 170;
            // 
            // value1
            // 
            value1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            value1.HeaderText = "Value1";
            value1.MinimumWidth = 100;
            value1.Name = "value1";
            value1.ReadOnly = true;
            // 
            // op1
            // 
            op1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            op1.HeaderText = "Op1";
            op1.MinimumWidth = 50;
            op1.Name = "op1";
            op1.ReadOnly = true;
            op1.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            op1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            op1.Width = 55;
            // 
            // op2
            // 
            op2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            op2.HeaderText = "Op2";
            op2.MinimumWidth = 50;
            op2.Name = "op2";
            op2.ReadOnly = true;
            op2.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            op2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
            op2.Width = 55;
            // 
            // value2
            // 
            value2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            value2.HeaderText = "Value2";
            value2.MinimumWidth = 100;
            value2.Name = "value2";
            value2.ReadOnly = true;
            // 
            // CB_HideSame
            // 
            CB_HideSame.Anchor = System.Windows.Forms.AnchorStyles.Top;
            CB_HideSame.AutoSize = true;
            CB_HideSame.Location = new System.Drawing.Point(316, 44);
            CB_HideSame.Name = "CB_HideSame";
            CB_HideSame.Size = new System.Drawing.Size(86, 21);
            CB_HideSame.TabIndex = 1;
            CB_HideSame.Text = "Hide same";
            CB_HideSame.UseVisualStyleBackColor = true;
            CB_HideSame.CheckedChanged += CB_HideSame_CheckedChanged;
            // 
            // CB_HideEmpty
            // 
            CB_HideEmpty.Anchor = System.Windows.Forms.AnchorStyles.Top;
            CB_HideEmpty.AutoSize = true;
            CB_HideEmpty.Location = new System.Drawing.Point(316, 71);
            CB_HideEmpty.Name = "CB_HideEmpty";
            CB_HideEmpty.Size = new System.Drawing.Size(90, 21);
            CB_HideEmpty.TabIndex = 4;
            CB_HideEmpty.Text = "Hide empty";
            CB_HideEmpty.UseVisualStyleBackColor = true;
            CB_HideEmpty.CheckedChanged += CB_HideEmpty_CheckedChanged;
            // 
            // GB_PKM1
            // 
            GB_PKM1.Controls.Add(pictureBox1);
            GB_PKM1.Controls.Add(TB_PKM1_Name);
            GB_PKM1.Controls.Add(B_Import1);
            GB_PKM1.Controls.Add(B_Export1);
            GB_PKM1.Location = new System.Drawing.Point(8, 8);
            GB_PKM1.Name = "GB_PKM1";
            GB_PKM1.Size = new System.Drawing.Size(290, 112);
            GB_PKM1.TabIndex = 0;
            GB_PKM1.TabStop = false;
            GB_PKM1.Text = "PKM 1";
            // 
            // pictureBox1
            // 
            pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pictureBox1.Location = new System.Drawing.Point(12, 24);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(80, 76);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // TB_PKM1_Name
            // 
            TB_PKM1_Name.Location = new System.Drawing.Point(104, 24);
            TB_PKM1_Name.Name = "TB_PKM1_Name";
            TB_PKM1_Name.ReadOnly = true;
            TB_PKM1_Name.Size = new System.Drawing.Size(174, 23);
            TB_PKM1_Name.TabIndex = 1;
            TB_PKM1_Name.TabStop = false;
            // 
            // B_Import1
            // 
            B_Import1.Location = new System.Drawing.Point(104, 72);
            B_Import1.Name = "B_Import1";
            B_Import1.Size = new System.Drawing.Size(84, 28);
            B_Import1.TabIndex = 2;
            B_Import1.Text = "Import";
            B_Import1.UseVisualStyleBackColor = true;
            B_Import1.Click += B_Import1_Click;
            // 
            // B_Export1
            // 
            B_Export1.Location = new System.Drawing.Point(194, 72);
            B_Export1.Name = "B_Export1";
            B_Export1.Size = new System.Drawing.Size(84, 28);
            B_Export1.TabIndex = 3;
            B_Export1.Text = "Export";
            B_Export1.UseVisualStyleBackColor = true;
            B_Export1.Click += B_Export1_Click;
            // 
            // GB_PKM2
            // 
            GB_PKM2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            GB_PKM2.Controls.Add(TB_PKM2_Name);
            GB_PKM2.Controls.Add(B_Import2);
            GB_PKM2.Controls.Add(B_Export2);
            GB_PKM2.Controls.Add(pictureBox2);
            GB_PKM2.Location = new System.Drawing.Point(422, 8);
            GB_PKM2.Name = "GB_PKM2";
            GB_PKM2.Size = new System.Drawing.Size(290, 112);
            GB_PKM2.TabIndex = 3;
            GB_PKM2.TabStop = false;
            GB_PKM2.Text = "PKM 2";
            // 
            // TB_PKM2_Name
            // 
            TB_PKM2_Name.Location = new System.Drawing.Point(12, 24);
            TB_PKM2_Name.Name = "TB_PKM2_Name";
            TB_PKM2_Name.ReadOnly = true;
            TB_PKM2_Name.Size = new System.Drawing.Size(174, 23);
            TB_PKM2_Name.TabIndex = 0;
            TB_PKM2_Name.TabStop = false;
            // 
            // B_Import2
            // 
            B_Import2.Location = new System.Drawing.Point(12, 72);
            B_Import2.Name = "B_Import2";
            B_Import2.Size = new System.Drawing.Size(84, 28);
            B_Import2.TabIndex = 1;
            B_Import2.Text = "Import";
            B_Import2.UseVisualStyleBackColor = true;
            B_Import2.Click += B_Import2_Click;
            // 
            // B_Export2
            // 
            B_Export2.Location = new System.Drawing.Point(102, 72);
            B_Export2.Name = "B_Export2";
            B_Export2.Size = new System.Drawing.Size(84, 28);
            B_Export2.TabIndex = 2;
            B_Export2.Text = "Export";
            B_Export2.UseVisualStyleBackColor = true;
            B_Export2.Click += B_Export2_Click;
            // 
            // pictureBox2
            // 
            pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pictureBox2.Location = new System.Drawing.Point(198, 24);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new System.Drawing.Size(80, 76);
            pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 3;
            pictureBox2.TabStop = false;
            // 
            // toolTip1
            // 
            toolTip1.SetToolTip(GB_PKM1, "Drop a PKM file or box slot here. Drag the sprite out to export.");
            toolTip1.SetToolTip(pictureBox1, "Drop a PKM file or box slot here. Drag the sprite out to export.");
            toolTip1.SetToolTip(GB_PKM2, "Drop a PKM file or box slot here. Drag the sprite out to export.");
            toolTip1.SetToolTip(pictureBox2, "Drop a PKM file or box slot here. Drag the sprite out to export.");
            // 
            // FormDiff
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(720, 560);
            Controls.Add(GB_PKM1);
            Controls.Add(CB_HideSame);
            Controls.Add(dataGridView1);
            Controls.Add(GB_PKM2);
            Controls.Add(CB_HideEmpty);
            Margin = new System.Windows.Forms.Padding(2);
            MinimumSize = new System.Drawing.Size(736, 400);
            Name = "FormDiff";
            Text = "PKMMerge";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            GB_PKM1.ResumeLayout(false);
            GB_PKM1.PerformLayout();
            GB_PKM2.ResumeLayout(false);
            GB_PKM2.PerformLayout();
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
        private System.Windows.Forms.GroupBox GB_PKM1;
        private System.Windows.Forms.GroupBox GB_PKM2;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
