namespace PKMMerge
{
    partial class HexCompareForm
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
            System.Windows.Forms.DataGridViewCellStyle headerStyle = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle cellStyle = new System.Windows.Forms.DataGridViewCellStyle();
            DGV_Hex = new System.Windows.Forms.DataGridView();
            PAN_SideHeader = new System.Windows.Forms.Panel();
            L_Side1 = new System.Windows.Forms.Label();
            L_Side2 = new System.Windows.Forms.Label();
            PAN_Bottom = new System.Windows.Forms.Panel();
            L_Summary = new System.Windows.Forms.Label();
            FLP_Buttons = new System.Windows.Forms.FlowLayoutPanel();
            B_Save = new System.Windows.Forms.Button();
            B_Cancel = new System.Windows.Forms.Button();
            B_NextDiff = new System.Windows.Forms.Button();
            B_PrevDiff = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)DGV_Hex).BeginInit();
            PAN_SideHeader.SuspendLayout();
            PAN_Bottom.SuspendLayout();
            FLP_Buttons.SuspendLayout();
            SuspendLayout();
            // 
            // DGV_Hex
            // 
            DGV_Hex.AllowUserToAddRows = false;
            DGV_Hex.AllowUserToDeleteRows = false;
            DGV_Hex.AllowUserToResizeColumns = false;
            DGV_Hex.AllowUserToResizeRows = false;
            DGV_Hex.BackgroundColor = System.Drawing.SystemColors.Window;
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.Font = new System.Drawing.Font("Courier New", 9F);
            DGV_Hex.ColumnHeadersDefaultCellStyle = headerStyle;
            DGV_Hex.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            cellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            cellStyle.Font = new System.Drawing.Font("Courier New", 9F);
            DGV_Hex.DefaultCellStyle = cellStyle;
            DGV_Hex.Dock = System.Windows.Forms.DockStyle.Fill;
            DGV_Hex.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystrokeOrF2;
            DGV_Hex.Location = new System.Drawing.Point(0, 25);
            DGV_Hex.Name = "DGV_Hex";
            DGV_Hex.RowHeadersVisible = false;
            DGV_Hex.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            DGV_Hex.Size = new System.Drawing.Size(1000, 423);
            DGV_Hex.TabIndex = 0;
            DGV_Hex.CellEndEdit += DGV_Hex_CellEndEdit;
            DGV_Hex.CellValidating += DGV_Hex_CellValidating;
            DGV_Hex.ColumnWidthChanged += DGV_Hex_ColumnWidthChanged;
            DGV_Hex.Scroll += DGV_Hex_Scroll;
            // 
            // PAN_SideHeader
            // 
            PAN_SideHeader.Controls.Add(L_Side1);
            PAN_SideHeader.Controls.Add(L_Side2);
            PAN_SideHeader.Dock = System.Windows.Forms.DockStyle.Top;
            PAN_SideHeader.Location = new System.Drawing.Point(0, 0);
            PAN_SideHeader.Name = "PAN_SideHeader";
            PAN_SideHeader.Size = new System.Drawing.Size(1000, 25);
            PAN_SideHeader.TabIndex = 1;
            // 
            // L_Side1
            // 
            L_Side1.Location = new System.Drawing.Point(80, 0);
            L_Side1.Name = "L_Side1";
            L_Side1.Size = new System.Drawing.Size(400, 25);
            L_Side1.TabIndex = 0;
            L_Side1.Text = "PKM 1";
            L_Side1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // L_Side2
            // 
            L_Side2.Location = new System.Drawing.Point(500, 0);
            L_Side2.Name = "L_Side2";
            L_Side2.Size = new System.Drawing.Size(400, 25);
            L_Side2.TabIndex = 1;
            L_Side2.Text = "PKM 2";
            L_Side2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // PAN_Bottom
            // 
            PAN_Bottom.Controls.Add(L_Summary);
            PAN_Bottom.Controls.Add(FLP_Buttons);
            PAN_Bottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            PAN_Bottom.Location = new System.Drawing.Point(0, 448);
            PAN_Bottom.Name = "PAN_Bottom";
            PAN_Bottom.Padding = new System.Windows.Forms.Padding(8);
            PAN_Bottom.Size = new System.Drawing.Size(1000, 52);
            PAN_Bottom.TabIndex = 2;
            // 
            // L_Summary
            // 
            L_Summary.AutoEllipsis = true;
            L_Summary.Dock = System.Windows.Forms.DockStyle.Fill;
            L_Summary.Location = new System.Drawing.Point(8, 8);
            L_Summary.Name = "L_Summary";
            L_Summary.Size = new System.Drawing.Size(560, 36);
            L_Summary.TabIndex = 0;
            L_Summary.Text = "0 of 0 bytes differ";
            L_Summary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // FLP_Buttons
            // 
            FLP_Buttons.AutoSize = true;
            FLP_Buttons.Controls.Add(B_Save);
            FLP_Buttons.Controls.Add(B_Cancel);
            FLP_Buttons.Controls.Add(B_NextDiff);
            FLP_Buttons.Controls.Add(B_PrevDiff);
            FLP_Buttons.Dock = System.Windows.Forms.DockStyle.Right;
            FLP_Buttons.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            FLP_Buttons.Location = new System.Drawing.Point(568, 8);
            FLP_Buttons.Name = "FLP_Buttons";
            FLP_Buttons.Size = new System.Drawing.Size(424, 36);
            FLP_Buttons.TabIndex = 1;
            FLP_Buttons.WrapContents = false;
            // 
            // B_Save
            // 
            B_Save.Location = new System.Drawing.Point(325, 3);
            B_Save.Name = "B_Save";
            B_Save.Size = new System.Drawing.Size(96, 32);
            B_Save.TabIndex = 0;
            B_Save.Text = "Save";
            B_Save.UseVisualStyleBackColor = true;
            B_Save.Click += B_Save_Click;
            // 
            // B_Cancel
            // 
            B_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            B_Cancel.Location = new System.Drawing.Point(223, 3);
            B_Cancel.Name = "B_Cancel";
            B_Cancel.Size = new System.Drawing.Size(96, 32);
            B_Cancel.TabIndex = 1;
            B_Cancel.Text = "Cancel";
            B_Cancel.UseVisualStyleBackColor = true;
            // 
            // B_NextDiff
            // 
            B_NextDiff.Location = new System.Drawing.Point(121, 3);
            B_NextDiff.Name = "B_NextDiff";
            B_NextDiff.Size = new System.Drawing.Size(96, 32);
            B_NextDiff.TabIndex = 2;
            B_NextDiff.Text = "Next Diff";
            B_NextDiff.UseVisualStyleBackColor = true;
            B_NextDiff.Click += B_NextDiff_Click;
            // 
            // B_PrevDiff
            // 
            B_PrevDiff.Location = new System.Drawing.Point(19, 3);
            B_PrevDiff.Name = "B_PrevDiff";
            B_PrevDiff.Size = new System.Drawing.Size(96, 32);
            B_PrevDiff.TabIndex = 3;
            B_PrevDiff.Text = "Prev Diff";
            B_PrevDiff.UseVisualStyleBackColor = true;
            B_PrevDiff.Click += B_PrevDiff_Click;
            // 
            // HexCompareForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = B_Cancel;
            ClientSize = new System.Drawing.Size(1000, 500);
            Controls.Add(DGV_Hex);
            Controls.Add(PAN_SideHeader);
            Controls.Add(PAN_Bottom);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "HexCompareForm";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Hex Compare";
            FormClosing += HexCompareForm_FormClosing;
            Shown += HexCompareForm_Shown;
            ((System.ComponentModel.ISupportInitialize)DGV_Hex).EndInit();
            PAN_SideHeader.ResumeLayout(false);
            PAN_Bottom.ResumeLayout(false);
            PAN_Bottom.PerformLayout();
            FLP_Buttons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.DataGridView DGV_Hex;
        private System.Windows.Forms.Panel PAN_SideHeader;
        private System.Windows.Forms.Label L_Side1;
        private System.Windows.Forms.Label L_Side2;
        private System.Windows.Forms.Panel PAN_Bottom;
        private System.Windows.Forms.Label L_Summary;
        private System.Windows.Forms.FlowLayoutPanel FLP_Buttons;
        private System.Windows.Forms.Button B_Save;
        private System.Windows.Forms.Button B_Cancel;
        private System.Windows.Forms.Button B_NextDiff;
        private System.Windows.Forms.Button B_PrevDiff;
    }
}
