namespace PKHEdit
{
    partial class PKHEditor
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
            GB_Main = new System.Windows.Forms.GroupBox();
            PB_Sprite = new System.Windows.Forms.PictureBox();
            TB_Name = new System.Windows.Forms.TextBox();
            L_Tracker = new System.Windows.Forms.Label();
            TB_Tracker = new System.Windows.Forms.TextBox();
            B_NewTracker = new System.Windows.Forms.Button();
            L_Ids = new System.Windows.Forms.Label();
            TC_Info = new System.Windows.Forms.TabControl();
            Tab_Edit = new System.Windows.Forms.TabPage();
            TLP_Edit = new System.Windows.Forms.TableLayoutPanel();
            L_Nickname = new System.Windows.Forms.Label();
            TB_Nickname = new System.Windows.Forms.TextBox();
            L_Gender = new System.Windows.Forms.Label();
            CB_Gender = new System.Windows.Forms.ComboBox();
            L_EXP = new System.Windows.Forms.Label();
            NUD_EXP = new System.Windows.Forms.NumericUpDown();
            L_Level = new System.Windows.Forms.Label();
            NUD_Level = new System.Windows.Forms.NumericUpDown();
            L_Nature = new System.Windows.Forms.Label();
            CB_Nature = new System.Windows.Forms.ComboBox();
            L_StatAlignment = new System.Windows.Forms.Label();
            CB_StatAlignment = new System.Windows.Forms.ComboBox();
            L_Friendship = new System.Windows.Forms.Label();
            NUD_Friendship = new System.Windows.Forms.NumericUpDown();
            Tab_Main = new System.Windows.Forms.TabPage();
            TB_Main = new System.Windows.Forms.TextBox();
            Tab_Met = new System.Windows.Forms.TabPage();
            TB_Met = new System.Windows.Forms.TextBox();
            Tab_Stat = new System.Windows.Forms.TabPage();
            TB_Stat = new System.Windows.Forms.TextBox();
            Tab_Cosmetic = new System.Windows.Forms.TabPage();
            TB_Cosmetic = new System.Windows.Forms.TextBox();
            Tab_Other = new System.Windows.Forms.TabPage();
            TB_Other = new System.Windows.Forms.TextBox();
            GB_Versions = new System.Windows.Forms.GroupBox();
            FLP_Versions = new System.Windows.Forms.FlowLayoutPanel();
            FLP_Create = new System.Windows.Forms.FlowLayoutPanel();
            CB_CreateFormat = new System.Windows.Forms.ComboBox();
            B_CreateVersion = new System.Windows.Forms.Button();
            GB_VersionInfo = new System.Windows.Forms.GroupBox();
            TB_VersionInfo = new System.Windows.Forms.TextBox();
            L_Status = new System.Windows.Forms.Label();
            B_Home = new System.Windows.Forms.Button();
            B_Save = new System.Windows.Forms.Button();
            B_Close = new System.Windows.Forms.Button();
            CHK_UseCustomTracker = new System.Windows.Forms.CheckBox();
            B_ClearTracker = new System.Windows.Forms.Button();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            GB_Main.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PB_Sprite).BeginInit();
            TC_Info.SuspendLayout();
            Tab_Edit.SuspendLayout();
            TLP_Edit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_EXP).BeginInit();
            ((System.ComponentModel.ISupportInitialize)NUD_Level).BeginInit();
            ((System.ComponentModel.ISupportInitialize)NUD_Friendship).BeginInit();
            Tab_Main.SuspendLayout();
            Tab_Met.SuspendLayout();
            Tab_Stat.SuspendLayout();
            Tab_Cosmetic.SuspendLayout();
            Tab_Other.SuspendLayout();
            GB_Versions.SuspendLayout();
            GB_VersionInfo.SuspendLayout();
            SuspendLayout();
            // 
            // GB_Main
            // 
            GB_Main.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            GB_Main.Controls.Add(PB_Sprite);
            GB_Main.Controls.Add(TB_Name);
            GB_Main.Controls.Add(L_Tracker);
            GB_Main.Controls.Add(TB_Tracker);
            GB_Main.Controls.Add(B_NewTracker);
            GB_Main.Controls.Add(B_ClearTracker);
            GB_Main.Controls.Add(L_Ids);
            GB_Main.Controls.Add(TC_Info);
            GB_Main.Location = new System.Drawing.Point(8, 8);
            GB_Main.Name = "GB_Main";
            GB_Main.Size = new System.Drawing.Size(500, 444);
            GB_Main.TabIndex = 0;
            GB_Main.TabStop = false;
            GB_Main.Text = "PKH";
            // 
            // PB_Sprite
            // 
            PB_Sprite.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            PB_Sprite.Location = new System.Drawing.Point(12, 24);
            PB_Sprite.Name = "PB_Sprite";
            PB_Sprite.Size = new System.Drawing.Size(80, 80);
            PB_Sprite.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            PB_Sprite.TabIndex = 0;
            PB_Sprite.TabStop = false;
            // 
            // TB_Name
            // 
            TB_Name.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            TB_Name.Location = new System.Drawing.Point(104, 24);
            TB_Name.Name = "TB_Name";
            TB_Name.ReadOnly = true;
            TB_Name.Size = new System.Drawing.Size(344, 23);
            TB_Name.TabIndex = 1;
            TB_Name.TabStop = false;
            // 
            // L_Tracker
            // 
            L_Tracker.AutoSize = true;
            L_Tracker.Location = new System.Drawing.Point(104, 56);
            L_Tracker.Name = "L_Tracker";
            L_Tracker.Size = new System.Drawing.Size(97, 17);
            L_Tracker.TabIndex = 2;
            L_Tracker.Text = "HOME Tracker:";
            // 
            // TB_Tracker
            // 
            TB_Tracker.Font = new System.Drawing.Font("Courier New", 9F);
            TB_Tracker.Location = new System.Drawing.Point(207, 53);
            TB_Tracker.MaxLength = 16;
            TB_Tracker.Name = "TB_Tracker";
            TB_Tracker.Size = new System.Drawing.Size(128, 21);
            TB_Tracker.TabIndex = 3;
            TB_Tracker.Validated += TB_Tracker_Validated;
            // 
            // B_NewTracker
            // 
            B_NewTracker.Location = new System.Drawing.Point(341, 51);
            B_NewTracker.Name = "B_NewTracker";
            B_NewTracker.Size = new System.Drawing.Size(50, 26);
            B_NewTracker.TabIndex = 4;
            B_NewTracker.Text = "Gen";
            B_NewTracker.UseVisualStyleBackColor = true;
            B_NewTracker.Click += B_NewTracker_Click;
            // 
            // B_ClearTracker
            // 
            B_ClearTracker.Location = new System.Drawing.Point(397, 51);
            B_ClearTracker.Name = "B_ClearTracker";
            B_ClearTracker.Size = new System.Drawing.Size(50, 26);
            B_ClearTracker.TabIndex = 7;
            B_ClearTracker.Text = "Clear";
            B_ClearTracker.UseVisualStyleBackColor = true;
            B_ClearTracker.Click += B_ClearTracker_Click;
            // 
            // L_Ids
            // 
            L_Ids.AutoSize = true;
            L_Ids.Font = new System.Drawing.Font("Courier New", 9F);
            L_Ids.Location = new System.Drawing.Point(104, 88);
            L_Ids.Name = "L_Ids";
            L_Ids.Size = new System.Drawing.Size(0, 15);
            L_Ids.TabIndex = 5;
            // 
            // TC_Info
            // 
            TC_Info.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            TC_Info.Controls.Add(Tab_Edit);
            TC_Info.Controls.Add(Tab_Main);
            TC_Info.Controls.Add(Tab_Met);
            TC_Info.Controls.Add(Tab_Stat);
            TC_Info.Controls.Add(Tab_Cosmetic);
            TC_Info.Controls.Add(Tab_Other);
            TC_Info.Location = new System.Drawing.Point(12, 116);
            TC_Info.Name = "TC_Info";
            TC_Info.SelectedIndex = 0;
            TC_Info.Size = new System.Drawing.Size(476, 296);
            TC_Info.TabIndex = 6;
            // 
            // Tab_Edit
            // 
            Tab_Edit.Controls.Add(TLP_Edit);
            Tab_Edit.Location = new System.Drawing.Point(4, 26);
            Tab_Edit.Name = "Tab_Edit";
            Tab_Edit.Padding = new System.Windows.Forms.Padding(6);
            Tab_Edit.Size = new System.Drawing.Size(468, 266);
            Tab_Edit.TabIndex = 0;
            Tab_Edit.Text = "Edit";
            Tab_Edit.UseVisualStyleBackColor = true;
            // 
            // TLP_Edit
            // 
            TLP_Edit.ColumnCount = 4;
            TLP_Edit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
            TLP_Edit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            TLP_Edit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            TLP_Edit.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            TLP_Edit.Controls.Add(L_Nickname, 0, 0);
            TLP_Edit.Controls.Add(TB_Nickname, 1, 0);
            TLP_Edit.Controls.Add(L_Gender, 2, 0);
            TLP_Edit.Controls.Add(CB_Gender, 3, 0);
            TLP_Edit.Controls.Add(L_EXP, 0, 1);
            TLP_Edit.Controls.Add(NUD_EXP, 1, 1);
            TLP_Edit.Controls.Add(L_Level, 2, 1);
            TLP_Edit.Controls.Add(NUD_Level, 3, 1);
            TLP_Edit.Controls.Add(L_Nature, 0, 2);
            TLP_Edit.Controls.Add(CB_Nature, 1, 2);
            TLP_Edit.Controls.Add(L_StatAlignment, 2, 2);
            TLP_Edit.Controls.Add(CB_StatAlignment, 3, 2);
            TLP_Edit.Controls.Add(L_Friendship, 0, 3);
            TLP_Edit.Controls.Add(NUD_Friendship, 1, 3);
            TLP_Edit.Dock = System.Windows.Forms.DockStyle.Fill;
            TLP_Edit.Location = new System.Drawing.Point(6, 6);
            TLP_Edit.Name = "TLP_Edit";
            TLP_Edit.RowCount = 5;
            TLP_Edit.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            TLP_Edit.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            TLP_Edit.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            TLP_Edit.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 36F));
            TLP_Edit.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            TLP_Edit.Size = new System.Drawing.Size(456, 254);
            TLP_Edit.TabIndex = 0;
            // 
            // L_Nickname
            // 
            L_Nickname.Anchor = System.Windows.Forms.AnchorStyles.Left;
            L_Nickname.AutoSize = true;
            L_Nickname.Name = "L_Nickname";
            L_Nickname.TabIndex = 0;
            L_Nickname.Text = "Nickname:";
            // 
            // TB_Nickname
            // 
            TB_Nickname.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            TB_Nickname.MaxLength = 12;
            TB_Nickname.Name = "TB_Nickname";
            TB_Nickname.TabIndex = 1;
            TB_Nickname.Validated += TB_Nickname_Validated;
            // 
            // L_EXP
            // 
            L_EXP.Anchor = System.Windows.Forms.AnchorStyles.Left;
            L_EXP.AutoSize = true;
            L_EXP.Name = "L_EXP";
            L_EXP.TabIndex = 2;
            L_EXP.Text = "EXP:";
            // 
            // NUD_EXP
            // 
            NUD_EXP.Anchor = System.Windows.Forms.AnchorStyles.Left;
            NUD_EXP.Maximum = new decimal(new int[] { 2000000, 0, 0, 0 });
            NUD_EXP.Name = "NUD_EXP";
            NUD_EXP.Size = new System.Drawing.Size(120, 23);
            NUD_EXP.TabIndex = 3;
            NUD_EXP.ValueChanged += NUD_EXP_ValueChanged;
            // 
            // L_Gender
            // 
            L_Gender.Anchor = System.Windows.Forms.AnchorStyles.Left;
            L_Gender.AutoSize = true;
            L_Gender.Name = "L_Gender";
            L_Gender.TabIndex = 10;
            L_Gender.Text = "Gender:";
            // 
            // CB_Gender
            // 
            CB_Gender.Anchor = System.Windows.Forms.AnchorStyles.Left;
            CB_Gender.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_Gender.Name = "CB_Gender";
            CB_Gender.Size = new System.Drawing.Size(90, 25);
            CB_Gender.TabIndex = 11;
            CB_Gender.SelectionChangeCommitted += CB_Gender_SelectionChangeCommitted;
            // 
            // L_Level
            // 
            L_Level.Anchor = System.Windows.Forms.AnchorStyles.Left;
            L_Level.AutoSize = true;
            L_Level.Name = "L_Level";
            L_Level.TabIndex = 4;
            L_Level.Text = "Level:";
            // 
            // NUD_Level
            // 
            NUD_Level.Anchor = System.Windows.Forms.AnchorStyles.Left;
            NUD_Level.Location = new System.Drawing.Point(0, 0);
            NUD_Level.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            NUD_Level.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            NUD_Level.Name = "NUD_Level";
            NUD_Level.Size = new System.Drawing.Size(70, 23);
            NUD_Level.TabIndex = 12;
            NUD_Level.Value = new decimal(new int[] { 1, 0, 0, 0 });
            NUD_Level.ValueChanged += NUD_Level_ValueChanged;
            // 
            // L_Nature
            // 
            L_Nature.Anchor = System.Windows.Forms.AnchorStyles.Left;
            L_Nature.AutoSize = true;
            L_Nature.Name = "L_Nature";
            L_Nature.TabIndex = 6;
            L_Nature.Text = "Nature:";
            // 
            // CB_Nature
            // 
            CB_Nature.Anchor = System.Windows.Forms.AnchorStyles.Left;
            CB_Nature.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_Nature.Name = "CB_Nature";
            CB_Nature.Size = new System.Drawing.Size(125, 25);
            CB_Nature.TabIndex = 7;
            CB_Nature.SelectionChangeCommitted += CB_Nature_SelectionChangeCommitted;
            // 
            // L_StatAlignment
            // 
            L_StatAlignment.Anchor = System.Windows.Forms.AnchorStyles.Left;
            L_StatAlignment.AutoSize = true;
            L_StatAlignment.Name = "L_StatAlignment";
            L_StatAlignment.TabIndex = 13;
            L_StatAlignment.Text = "Stat alignment:";
            // 
            // CB_StatAlignment
            // 
            CB_StatAlignment.Anchor = System.Windows.Forms.AnchorStyles.Left;
            CB_StatAlignment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_StatAlignment.Name = "CB_StatAlignment";
            CB_StatAlignment.Size = new System.Drawing.Size(130, 25);
            CB_StatAlignment.TabIndex = 14;
            CB_StatAlignment.SelectionChangeCommitted += CB_StatAlignment_SelectionChangeCommitted;
            // 
            // L_Friendship
            // 
            L_Friendship.Anchor = System.Windows.Forms.AnchorStyles.Left;
            L_Friendship.AutoSize = true;
            L_Friendship.Name = "L_Friendship";
            L_Friendship.TabIndex = 8;
            L_Friendship.Text = "Friendship:";
            // 
            // NUD_Friendship
            // 
            NUD_Friendship.Anchor = System.Windows.Forms.AnchorStyles.Left;
            NUD_Friendship.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            NUD_Friendship.Name = "NUD_Friendship";
            NUD_Friendship.Size = new System.Drawing.Size(80, 23);
            NUD_Friendship.TabIndex = 9;
            NUD_Friendship.ValueChanged += NUD_Friendship_ValueChanged;
            // 
            // Tab_Main
            // 
            Tab_Main.Controls.Add(TB_Main);
            Tab_Main.Location = new System.Drawing.Point(4, 26);
            Tab_Main.Name = "Tab_Main";
            Tab_Main.Padding = new System.Windows.Forms.Padding(6);
            Tab_Main.Size = new System.Drawing.Size(468, 266);
            Tab_Main.TabIndex = 1;
            Tab_Main.Text = "Main";
            Tab_Main.UseVisualStyleBackColor = true;
            // 
            // TB_Main
            // 
            TB_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            TB_Main.Multiline = true;
            TB_Main.Name = "TB_Main";
            TB_Main.ReadOnly = true;
            TB_Main.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            TB_Main.TabIndex = 0;
            TB_Main.TabStop = false;
            // 
            // Tab_Met
            // 
            Tab_Met.Controls.Add(TB_Met);
            Tab_Met.Location = new System.Drawing.Point(4, 26);
            Tab_Met.Name = "Tab_Met";
            Tab_Met.Padding = new System.Windows.Forms.Padding(6);
            Tab_Met.Size = new System.Drawing.Size(468, 266);
            Tab_Met.TabIndex = 2;
            Tab_Met.Text = "Met";
            Tab_Met.UseVisualStyleBackColor = true;
            // 
            // TB_Met
            // 
            TB_Met.Dock = System.Windows.Forms.DockStyle.Fill;
            TB_Met.Multiline = true;
            TB_Met.Name = "TB_Met";
            TB_Met.ReadOnly = true;
            TB_Met.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            TB_Met.TabIndex = 0;
            TB_Met.TabStop = false;
            // 
            // Tab_Stat
            // 
            Tab_Stat.Controls.Add(TB_Stat);
            Tab_Stat.Location = new System.Drawing.Point(4, 26);
            Tab_Stat.Name = "Tab_Stat";
            Tab_Stat.Padding = new System.Windows.Forms.Padding(6);
            Tab_Stat.Size = new System.Drawing.Size(468, 266);
            Tab_Stat.TabIndex = 3;
            Tab_Stat.Text = "Stat";
            Tab_Stat.UseVisualStyleBackColor = true;
            // 
            // TB_Stat
            // 
            TB_Stat.Dock = System.Windows.Forms.DockStyle.Fill;
            TB_Stat.Multiline = true;
            TB_Stat.Name = "TB_Stat";
            TB_Stat.ReadOnly = true;
            TB_Stat.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            TB_Stat.TabIndex = 0;
            TB_Stat.TabStop = false;
            // 
            // Tab_Cosmetic
            // 
            Tab_Cosmetic.Controls.Add(TB_Cosmetic);
            Tab_Cosmetic.Location = new System.Drawing.Point(4, 26);
            Tab_Cosmetic.Name = "Tab_Cosmetic";
            Tab_Cosmetic.Padding = new System.Windows.Forms.Padding(6);
            Tab_Cosmetic.Size = new System.Drawing.Size(468, 266);
            Tab_Cosmetic.TabIndex = 4;
            Tab_Cosmetic.Text = "Cosmetic";
            Tab_Cosmetic.UseVisualStyleBackColor = true;
            // 
            // TB_Cosmetic
            // 
            TB_Cosmetic.Dock = System.Windows.Forms.DockStyle.Fill;
            TB_Cosmetic.Multiline = true;
            TB_Cosmetic.Name = "TB_Cosmetic";
            TB_Cosmetic.ReadOnly = true;
            TB_Cosmetic.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            TB_Cosmetic.TabIndex = 0;
            TB_Cosmetic.TabStop = false;
            // 
            // Tab_Other
            // 
            Tab_Other.Controls.Add(TB_Other);
            Tab_Other.Location = new System.Drawing.Point(4, 26);
            Tab_Other.Name = "Tab_Other";
            Tab_Other.Padding = new System.Windows.Forms.Padding(6);
            Tab_Other.Size = new System.Drawing.Size(468, 266);
            Tab_Other.TabIndex = 5;
            Tab_Other.Text = "Other";
            Tab_Other.UseVisualStyleBackColor = true;
            // 
            // TB_Other
            // 
            TB_Other.Dock = System.Windows.Forms.DockStyle.Fill;
            TB_Other.Multiline = true;
            TB_Other.Name = "TB_Other";
            TB_Other.ReadOnly = true;
            TB_Other.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            TB_Other.TabIndex = 0;
            TB_Other.TabStop = false;
            // 
            // GB_Versions
            // 
            GB_Versions.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            GB_Versions.Controls.Add(FLP_Versions);
            GB_Versions.Controls.Add(FLP_Create);
            GB_Versions.Location = new System.Drawing.Point(516, 8);
            GB_Versions.Name = "GB_Versions";
            GB_Versions.Padding = new System.Windows.Forms.Padding(3, 18, 3, 3);
            GB_Versions.Size = new System.Drawing.Size(136, 444);
            GB_Versions.TabIndex = 1;
            GB_Versions.TabStop = false;
            GB_Versions.Text = "Versions";
            // 
            // FLP_Versions
            // 
            FLP_Versions.AutoScroll = true;
            FLP_Versions.Dock = System.Windows.Forms.DockStyle.Fill;
            FLP_Versions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            FLP_Versions.Name = "FLP_Versions";
            FLP_Versions.TabIndex = 0;
            FLP_Versions.WrapContents = false;
            // 
            // FLP_Create
            // 
            FLP_Create.Controls.Add(CB_CreateFormat);
            FLP_Create.Controls.Add(B_CreateVersion);
            FLP_Create.Dock = System.Windows.Forms.DockStyle.Bottom;
            FLP_Create.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            FLP_Create.Name = "FLP_Create";
            FLP_Create.Padding = new System.Windows.Forms.Padding(0, 4, 0, 0);
            FLP_Create.Size = new System.Drawing.Size(130, 70);
            FLP_Create.TabIndex = 1;
            FLP_Create.WrapContents = false;
            // 
            // CB_CreateFormat
            // 
            CB_CreateFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            CB_CreateFormat.Name = "CB_CreateFormat";
            CB_CreateFormat.Size = new System.Drawing.Size(120, 25);
            CB_CreateFormat.TabIndex = 0;
            // 
            // B_CreateVersion
            // 
            B_CreateVersion.Name = "B_CreateVersion";
            B_CreateVersion.Size = new System.Drawing.Size(120, 28);
            B_CreateVersion.TabIndex = 1;
            B_CreateVersion.Text = "Create version";
            B_CreateVersion.UseVisualStyleBackColor = true;
            B_CreateVersion.Click += B_CreateVersion_Click;
            // 
            // GB_VersionInfo
            // 
            GB_VersionInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            GB_VersionInfo.Controls.Add(TB_VersionInfo);
            GB_VersionInfo.Location = new System.Drawing.Point(660, 8);
            GB_VersionInfo.Name = "GB_VersionInfo";
            GB_VersionInfo.Size = new System.Drawing.Size(232, 444);
            GB_VersionInfo.TabIndex = 2;
            GB_VersionInfo.TabStop = false;
            GB_VersionInfo.Text = "Version data";
            // 
            // TB_VersionInfo
            // 
            TB_VersionInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            TB_VersionInfo.Multiline = true;
            TB_VersionInfo.Name = "TB_VersionInfo";
            TB_VersionInfo.ReadOnly = true;
            TB_VersionInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            TB_VersionInfo.TabIndex = 0;
            TB_VersionInfo.TabStop = false;
            // 
            // L_Status
            // 
            L_Status.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            L_Status.AutoEllipsis = true;
            L_Status.Location = new System.Drawing.Point(8, 456);
            L_Status.Name = "L_Status";
            L_Status.Size = new System.Drawing.Size(534, 36);
            L_Status.TabIndex = 3;
            L_Status.Text = "Drop a .pkh file to open it, or a PKM file to create or update a PKH. Drag the sprite out to export.";
            L_Status.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // B_Home
            // 
            B_Home.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            B_Home.Location = new System.Drawing.Point(864, 460);
            B_Home.Name = "B_Home";
            B_Home.Size = new System.Drawing.Size(28, 28);
            B_Home.TabIndex = 4;
            B_Home.Text = "H";
            B_Home.UseVisualStyleBackColor = true;
            B_Home.Click += B_Home_Click;
            // 
            // B_Save
            // 
            B_Save.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            B_Save.Location = new System.Drawing.Point(688, 460);
            B_Save.Name = "B_Save";
            B_Save.Size = new System.Drawing.Size(70, 28);
            B_Save.TabIndex = 5;
            B_Save.Text = "Save As";
            B_Save.UseVisualStyleBackColor = true;
            B_Save.Click += B_Save_Click;
            // 
            // B_Close
            // 
            B_Close.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            B_Close.Location = new System.Drawing.Point(766, 460);
            B_Close.Name = "B_Close";
            B_Close.Size = new System.Drawing.Size(90, 28);
            B_Close.TabIndex = 6;
            B_Close.Text = "Close File";
            B_Close.UseVisualStyleBackColor = true;
            B_Close.Click += B_CloseFile_Click;
            // 
            // CHK_UseCustomTracker
            // 
            CHK_UseCustomTracker.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            CHK_UseCustomTracker.Location = new System.Drawing.Point(550, 463);
            CHK_UseCustomTracker.Name = "CHK_UseCustomTracker";
            CHK_UseCustomTracker.Size = new System.Drawing.Size(130, 22);
            CHK_UseCustomTracker.TabIndex = 7;
            CHK_UseCustomTracker.Text = "Custom Tracker";
            CHK_UseCustomTracker.UseVisualStyleBackColor = true;
            CHK_UseCustomTracker.CheckedChanged += CHK_UseCustomTracker_CheckedChanged;
            // 
            // PKHEditor
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 500);
            Controls.Add(GB_Main);
            Controls.Add(GB_Versions);
            Controls.Add(GB_VersionInfo);
            Controls.Add(L_Status);
            Controls.Add(B_Home);
            Controls.Add(B_Save);
            Controls.Add(B_Close);
            Controls.Add(CHK_UseCustomTracker);
            MinimumSize = new System.Drawing.Size(916, 440);
            Name = "PKHEditor";
            Text = "PKH Editor";
            FormClosing += PKHEditor_FormClosing;
            GB_Main.ResumeLayout(false);
            GB_Main.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)PB_Sprite).EndInit();
            TC_Info.ResumeLayout(false);
            Tab_Edit.ResumeLayout(false);
            TLP_Edit.ResumeLayout(false);
            TLP_Edit.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NUD_EXP).EndInit();
            ((System.ComponentModel.ISupportInitialize)NUD_Level).EndInit();
            ((System.ComponentModel.ISupportInitialize)NUD_Friendship).EndInit();
            Tab_Main.ResumeLayout(false);
            Tab_Main.PerformLayout();
            Tab_Met.ResumeLayout(false);
            Tab_Met.PerformLayout();
            Tab_Stat.ResumeLayout(false);
            Tab_Stat.PerformLayout();
            Tab_Cosmetic.ResumeLayout(false);
            Tab_Cosmetic.PerformLayout();
            Tab_Other.ResumeLayout(false);
            Tab_Other.PerformLayout();
            GB_Versions.ResumeLayout(false);
            GB_VersionInfo.ResumeLayout(false);
            GB_VersionInfo.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.GroupBox GB_Main;
        private System.Windows.Forms.PictureBox PB_Sprite;
        private System.Windows.Forms.TextBox TB_Name;
        private System.Windows.Forms.Label L_Tracker;
        private System.Windows.Forms.TextBox TB_Tracker;
        private System.Windows.Forms.Button B_NewTracker;
        private System.Windows.Forms.Button B_ClearTracker;
        private System.Windows.Forms.Label L_Ids;
        private System.Windows.Forms.TabControl TC_Info;
        private System.Windows.Forms.TabPage Tab_Edit;
        private System.Windows.Forms.TableLayoutPanel TLP_Edit;
        private System.Windows.Forms.Label L_Nickname;
        private System.Windows.Forms.TextBox TB_Nickname;
        private System.Windows.Forms.Label L_Gender;
        private System.Windows.Forms.ComboBox CB_Gender;
        private System.Windows.Forms.Label L_EXP;
        private System.Windows.Forms.NumericUpDown NUD_EXP;
        private System.Windows.Forms.Label L_Level;
        private System.Windows.Forms.NumericUpDown NUD_Level;
        private System.Windows.Forms.Label L_Nature;
        private System.Windows.Forms.ComboBox CB_Nature;
        private System.Windows.Forms.Label L_StatAlignment;
        private System.Windows.Forms.ComboBox CB_StatAlignment;
        private System.Windows.Forms.Label L_Friendship;
        private System.Windows.Forms.NumericUpDown NUD_Friendship;
        private System.Windows.Forms.TabPage Tab_Main;
        private System.Windows.Forms.TextBox TB_Main;
        private System.Windows.Forms.TabPage Tab_Met;
        private System.Windows.Forms.TextBox TB_Met;
        private System.Windows.Forms.TabPage Tab_Stat;
        private System.Windows.Forms.TextBox TB_Stat;
        private System.Windows.Forms.TabPage Tab_Cosmetic;
        private System.Windows.Forms.TextBox TB_Cosmetic;
        private System.Windows.Forms.TabPage Tab_Other;
        private System.Windows.Forms.TextBox TB_Other;
        private System.Windows.Forms.GroupBox GB_Versions;
        private System.Windows.Forms.FlowLayoutPanel FLP_Versions;
        private System.Windows.Forms.FlowLayoutPanel FLP_Create;
        private System.Windows.Forms.ComboBox CB_CreateFormat;
        private System.Windows.Forms.Button B_CreateVersion;
        private System.Windows.Forms.GroupBox GB_VersionInfo;
        private System.Windows.Forms.Button B_Home;
        private System.Windows.Forms.TextBox TB_VersionInfo;
        private System.Windows.Forms.Label L_Status;
        private System.Windows.Forms.Button B_Save;
        private System.Windows.Forms.Button B_Close;
        private System.Windows.Forms.CheckBox CHK_UseCustomTracker;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
