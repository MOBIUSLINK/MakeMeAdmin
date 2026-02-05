namespace SinclairCC.MakeMeAdmin
{
    partial class AdminApprovalForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ListView requestsListView;
        private System.Windows.Forms.Button approveButton;
        private System.Windows.Forms.Button rejectButton;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.Button settingsButton;
        private System.Windows.Forms.Button selectAllButton;
        private System.Windows.Forms.Button deselectAllButton;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label emptyStateLabel;
        private System.Windows.Forms.Panel actionPanel;
        private System.Windows.Forms.Panel selectionPanel;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.NumericUpDown timeoutNumericUpDown;
        private System.Windows.Forms.Label timeoutLabel;
        private System.Windows.Forms.ColumnHeader userNameColumn;
        private System.Windows.Forms.ColumnHeader requestTimeColumn;
        private System.Windows.Forms.ColumnHeader timeoutColumn;
        private System.Windows.Forms.ColumnHeader remoteAddressColumn;

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
            this.components = new System.ComponentModel.Container();
            this.requestsListView = new System.Windows.Forms.ListView();
            this.userNameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.requestTimeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.timeoutColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.remoteAddressColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.approveButton = new System.Windows.Forms.Button();
            this.rejectButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.settingsButton = new System.Windows.Forms.Button();
            this.selectAllButton = new System.Windows.Forms.Button();
            this.deselectAllButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.emptyStateLabel = new System.Windows.Forms.Label();
            this.actionPanel = new System.Windows.Forms.Panel();
            this.timeoutLabel = new System.Windows.Forms.Label();
            this.timeoutNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.selectionPanel = new System.Windows.Forms.Panel();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.actionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // requestsListView
            // 
            this.requestsListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.requestsListView.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
            this.requestsListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.requestsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.userNameColumn,
            this.requestTimeColumn,
            this.timeoutColumn,
            this.remoteAddressColumn});
            this.requestsListView.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.requestsListView.FullRowSelect = true;
            this.requestsListView.GridLines = true;
            this.requestsListView.HideSelection = false;
            this.requestsListView.Location = new System.Drawing.Point(12, 12);
            this.requestsListView.Name = "requestsListView";
            this.requestsListView.Size = new System.Drawing.Size(893, 320);
            this.requestsListView.TabIndex = 0;
            this.requestsListView.UseCompatibleStateImageBehavior = false;
            this.requestsListView.View = System.Windows.Forms.View.Details;
            this.requestsListView.SelectedIndexChanged += new System.EventHandler(this.requestsListView_SelectedIndexChanged);
            this.requestsListView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.requestsListView_KeyDown);
            // 
            // userNameColumn
            // 
            this.userNameColumn.Text = "用户名";
            this.userNameColumn.Width = 200;
            // 
            // requestTimeColumn
            // 
            this.requestTimeColumn.Text = "请求时间";
            this.requestTimeColumn.Width = 180;
            // 
            // timeoutColumn
            // 
            this.timeoutColumn.Text = "超时时间(分钟)";
            this.timeoutColumn.Width = 120;
            // 
            // remoteAddressColumn
            // 
            this.remoteAddressColumn.Text = "远程地址";
            this.remoteAddressColumn.Width = 200;
            // 
            // approveButton
            // 
            this.approveButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(56)))), ((int)(((byte)(142)))), ((int)(((byte)(60)))));
            this.approveButton.FlatAppearance.BorderSize = 0;
            this.approveButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this.approveButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(66)))), ((int)(((byte)(160)))), ((int)(((byte)(70)))));
            this.approveButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.approveButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.approveButton.ForeColor = System.Drawing.Color.White;
            this.approveButton.Location = new System.Drawing.Point(198, 12);
            this.approveButton.Name = "approveButton";
            this.approveButton.Size = new System.Drawing.Size(110, 40);
            this.approveButton.TabIndex = 1;
            this.approveButton.Text = "✓ 批准";
            this.toolTip.SetToolTip(this.approveButton, "批准选中的请求（Enter键）");
            this.approveButton.UseMnemonic = false;
            this.approveButton.UseVisualStyleBackColor = false;
            this.approveButton.Click += new System.EventHandler(this.approveButton_Click);
            // 
            // rejectButton
            // 
            this.rejectButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(211)))), ((int)(((byte)(47)))), ((int)(((byte)(47)))));
            this.rejectButton.FlatAppearance.BorderSize = 0;
            this.rejectButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(198)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.rejectButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(57)))), ((int)(((byte)(53)))));
            this.rejectButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.rejectButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.rejectButton.ForeColor = System.Drawing.Color.White;
            this.rejectButton.Location = new System.Drawing.Point(316, 12);
            this.rejectButton.Name = "rejectButton";
            this.rejectButton.Size = new System.Drawing.Size(110, 40);
            this.rejectButton.TabIndex = 2;
            this.rejectButton.Text = "✗ 拒绝";
            this.toolTip.SetToolTip(this.rejectButton, "拒绝选中的请求（Delete键）");
            this.rejectButton.UseMnemonic = false;
            this.rejectButton.UseVisualStyleBackColor = false;
            this.rejectButton.Click += new System.EventHandler(this.rejectButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.refreshButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this.refreshButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(55)))), ((int)(((byte)(58)))));
            this.refreshButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(55)))), ((int)(((byte)(58)))));
            this.refreshButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.refreshButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.refreshButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.refreshButton.Location = new System.Drawing.Point(730, 12);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(75, 40);
            this.refreshButton.TabIndex = 3;
            this.refreshButton.Text = "🔄 刷新";
            this.toolTip.SetToolTip(this.refreshButton, "刷新请求列表（F5键）");
            this.refreshButton.UseMnemonic = false;
            this.refreshButton.UseVisualStyleBackColor = false;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // settingsButton
            // 
            this.settingsButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.settingsButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this.settingsButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(55)))), ((int)(((byte)(58)))));
            this.settingsButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(55)))), ((int)(((byte)(58)))));
            this.settingsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.settingsButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.settingsButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.settingsButton.Location = new System.Drawing.Point(813, 12);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(75, 40);
            this.settingsButton.TabIndex = 5;
            this.settingsButton.Text = "⚙ 设置";
            this.toolTip.SetToolTip(this.settingsButton, "打开服务器设置");
            this.settingsButton.UseMnemonic = false;
            this.settingsButton.UseVisualStyleBackColor = false;
            this.settingsButton.Click += new System.EventHandler(this.settingsButton_Click);
            // 
            // selectAllButton
            // 
            this.selectAllButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.selectAllButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this.selectAllButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(55)))), ((int)(((byte)(58)))));
            this.selectAllButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(55)))), ((int)(((byte)(58)))));
            this.selectAllButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.selectAllButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.selectAllButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.selectAllButton.Location = new System.Drawing.Point(12, 12);
            this.selectAllButton.Name = "selectAllButton";
            this.selectAllButton.Size = new System.Drawing.Size(85, 40);
            this.selectAllButton.TabIndex = 6;
            this.selectAllButton.Text = "全选";
            this.toolTip.SetToolTip(this.selectAllButton, "选择所有请求（Ctrl+A）");
            this.selectAllButton.UseMnemonic = false;
            this.selectAllButton.UseVisualStyleBackColor = false;
            this.selectAllButton.Click += new System.EventHandler(this.selectAllButton_Click);
            // 
            // deselectAllButton
            // 
            this.deselectAllButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(48)))));
            this.deselectAllButton.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(62)))), ((int)(((byte)(62)))), ((int)(((byte)(66)))));
            this.deselectAllButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(55)))), ((int)(((byte)(58)))));
            this.deselectAllButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(55)))), ((int)(((byte)(55)))), ((int)(((byte)(58)))));
            this.deselectAllButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.deselectAllButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.deselectAllButton.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.deselectAllButton.Location = new System.Drawing.Point(105, 12);
            this.deselectAllButton.Name = "deselectAllButton";
            this.deselectAllButton.Size = new System.Drawing.Size(85, 40);
            this.deselectAllButton.TabIndex = 7;
            this.deselectAllButton.Text = "取消全选";
            this.toolTip.SetToolTip(this.deselectAllButton, "取消所有选择");
            this.deselectAllButton.UseMnemonic = false;
            this.deselectAllButton.UseVisualStyleBackColor = false;
            this.deselectAllButton.Click += new System.EventHandler(this.deselectAllButton_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.statusLabel.Location = new System.Drawing.Point(600, 23);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(111, 19);
            this.statusLabel.TabIndex = 4;
            this.statusLabel.Text = "📊 待审批请求: 0";
            // 
            // emptyStateLabel
            // 
            this.emptyStateLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.emptyStateLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
            this.emptyStateLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.emptyStateLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(170)))), ((int)(((byte)(170)))), ((int)(((byte)(170)))));
            this.emptyStateLabel.Location = new System.Drawing.Point(12, 12);
            this.emptyStateLabel.Name = "emptyStateLabel";
            this.emptyStateLabel.Size = new System.Drawing.Size(893, 320);
            this.emptyStateLabel.TabIndex = 8;
            this.emptyStateLabel.Text = "📋 暂无待审批请求\r\n\r\n所有请求已处理完毕";
            this.emptyStateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.emptyStateLabel.Visible = false;
            // 
            // actionPanel
            // 
            this.actionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.actionPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.actionPanel.Controls.Add(this.selectAllButton);
            this.actionPanel.Controls.Add(this.deselectAllButton);
            this.actionPanel.Controls.Add(this.approveButton);
            this.actionPanel.Controls.Add(this.rejectButton);
            this.actionPanel.Controls.Add(this.timeoutLabel);
            this.actionPanel.Controls.Add(this.timeoutNumericUpDown);
            this.actionPanel.Controls.Add(this.statusLabel);
            this.actionPanel.Controls.Add(this.refreshButton);
            this.actionPanel.Controls.Add(this.settingsButton);
            this.actionPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.actionPanel.Location = new System.Drawing.Point(0, 380);
            this.actionPanel.Name = "actionPanel";
            this.actionPanel.Size = new System.Drawing.Size(917, 64);
            this.actionPanel.TabIndex = 9;
            // 
            // timeoutLabel
            // 
            this.timeoutLabel.AutoSize = true;
            this.timeoutLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.timeoutLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.timeoutLabel.Location = new System.Drawing.Point(434, 24);
            this.timeoutLabel.Name = "timeoutLabel";
            this.timeoutLabel.Size = new System.Drawing.Size(91, 17);
            this.timeoutLabel.TabIndex = 8;
            this.timeoutLabel.Text = "提权时长(分钟):";
            // 
            // timeoutNumericUpDown
            // 
            this.timeoutNumericUpDown.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(37)))), ((int)(((byte)(38)))));
            this.timeoutNumericUpDown.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.timeoutNumericUpDown.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.timeoutNumericUpDown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(241)))), ((int)(((byte)(241)))));
            this.timeoutNumericUpDown.Location = new System.Drawing.Point(532, 21);
            this.timeoutNumericUpDown.Maximum = new decimal(new int[] {
            1440,
            0,
            0,
            0});
            this.timeoutNumericUpDown.Name = "timeoutNumericUpDown";
            this.timeoutNumericUpDown.Size = new System.Drawing.Size(60, 23);
            this.timeoutNumericUpDown.TabIndex = 9;
            this.toolTip.SetToolTip(this.timeoutNumericUpDown, "设置批准请求时的管理员权限持续时间（分钟）。设置为0则使用请求中的默认值。最大1440分钟（24小时）。");
            // 
            // selectionPanel
            // 
            this.selectionPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.selectionPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.selectionPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.selectionPanel.Location = new System.Drawing.Point(0, 380);
            this.selectionPanel.Name = "selectionPanel";
            this.selectionPanel.Size = new System.Drawing.Size(917, 0);
            this.selectionPanel.TabIndex = 10;
            this.selectionPanel.Visible = false;
            // 
            // AdminApprovalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.ClientSize = new System.Drawing.Size(917, 444);
            this.Controls.Add(this.requestsListView);
            this.Controls.Add(this.selectionPanel);
            this.Controls.Add(this.actionPanel);
            this.Controls.Add(this.emptyStateLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AdminApprovalForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Make Me Admin Managed - 审批管理";
            this.actionPanel.ResumeLayout(false);
            this.actionPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeoutNumericUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
    }
}
