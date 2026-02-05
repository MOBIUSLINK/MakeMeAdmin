// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//

namespace SinclairCC.MakeMeAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Windows.Forms;

    /// <summary>
    /// Form for viewing client-side logs.
    /// </summary>
    internal partial class LogViewerForm : Form
    {
        private System.Windows.Forms.ListView logListView;
        private System.Windows.Forms.TextBox detailsTextBox;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.ComboBox levelFilterComboBox;
        private System.Windows.Forms.Label levelFilterLabel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Timer refreshTimer;

        /// <summary>
        /// Constructor.
        /// </summary>
        public LogViewerForm()
        {
            InitializeComponent();
            SetupLogListView();
            LoadLogs();
            StartAutoRefresh();
        }

        /// <summary>
        /// Initializes the form components.
        /// </summary>
        private void InitializeComponent()
        {
            this.logListView = new ListView();
            this.detailsTextBox = new TextBox();
            this.clearButton = new Button();
            this.exportButton = new Button();
            this.refreshButton = new Button();
            this.levelFilterComboBox = new ComboBox();
            this.levelFilterLabel = new Label();
            this.splitContainer1 = new SplitContainer();
            this.refreshTimer = new Timer();

            this.SuspendLayout();

            // Form
            this.Text = "Make Me Admin - 日志查看器";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 400);

            // SplitContainer
            this.splitContainer1.Dock = DockStyle.Fill;
            this.splitContainer1.Orientation = Orientation.Horizontal;
            this.splitContainer1.SplitterDistance = 400;
            this.splitContainer1.Panel1.Controls.Add(this.logListView);
            this.splitContainer1.Panel2.Controls.Add(this.detailsTextBox);

            // LogListView
            this.logListView.Dock = DockStyle.Fill;
            this.logListView.View = View.Details;
            this.logListView.FullRowSelect = true;
            this.logListView.GridLines = true;
            this.logListView.MultiSelect = false;
            this.logListView.SelectedIndexChanged += LogListView_SelectedIndexChanged;

            // DetailsTextBox
            this.detailsTextBox.Dock = DockStyle.Fill;
            this.detailsTextBox.Multiline = true;
            this.detailsTextBox.ReadOnly = true;
            this.detailsTextBox.ScrollBars = ScrollBars.Both;
            this.detailsTextBox.Font = new Font("Consolas", 9F);

            // LevelFilterLabel
            this.levelFilterLabel.Text = "日志级别:";
            this.levelFilterLabel.Location = new Point(12, 12);
            this.levelFilterLabel.Size = new Size(70, 23);
            this.levelFilterLabel.AutoSize = true;

            // LevelFilterComboBox
            this.levelFilterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.levelFilterComboBox.Location = new Point(88, 9);
            this.levelFilterComboBox.Size = new Size(120, 23);
            this.levelFilterComboBox.Items.AddRange(new object[] { "全部", "调试", "信息", "警告", "错误" });
            this.levelFilterComboBox.SelectedIndex = 1; // Info
            this.levelFilterComboBox.SelectedIndexChanged += LevelFilterComboBox_SelectedIndexChanged;

            // RefreshButton
            this.refreshButton.Text = "刷新";
            this.refreshButton.Location = new Point(220, 8);
            this.refreshButton.Size = new Size(75, 25);
            this.refreshButton.Click += RefreshButton_Click;

            // ClearButton
            this.clearButton.Text = "清除";
            this.clearButton.Location = new Point(300, 8);
            this.clearButton.Size = new Size(75, 25);
            this.clearButton.Click += ClearButton_Click;

            // ExportButton
            this.exportButton.Text = "导出";
            this.exportButton.Location = new Point(380, 8);
            this.exportButton.Size = new Size(75, 25);
            this.exportButton.Click += ExportButton_Click;

            // RefreshTimer
            this.refreshTimer.Interval = 2000; // 2 seconds
            this.refreshTimer.Tick += RefreshTimer_Tick;

            // Add controls to form
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.levelFilterLabel);
            this.Controls.Add(this.levelFilterComboBox);
            this.Controls.Add(this.refreshButton);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.exportButton);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Sets up the log list view columns.
        /// </summary>
        private void SetupLogListView()
        {
            this.logListView.Columns.Clear();
            this.logListView.Columns.Add("时间", 180);
            this.logListView.Columns.Add("级别", 80);
            this.logListView.Columns.Add("类别", 150);
            this.logListView.Columns.Add("消息", 400);
        }

        /// <summary>
        /// Loads logs into the list view.
        /// </summary>
        private void LoadLogs()
        {
            this.logListView.Items.Clear();

            ClientLogger.LogLevel minLevel = GetSelectedLogLevel();
            var entries = ClientLogger.GetEntries(minLevel);

            foreach (var entry in entries.OrderByDescending(e => e.Timestamp))
            {
                var item = new ListViewItem(entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                item.SubItems.Add(entry.Level.ToString());
                item.SubItems.Add(entry.Category);
                item.SubItems.Add(entry.Message);
                item.Tag = entry;

                // Set color based on level
                switch (entry.Level)
                {
                    case ClientLogger.LogLevel.Error:
                        item.ForeColor = Color.Red;
                        break;
                    case ClientLogger.LogLevel.Warning:
                        item.ForeColor = Color.Orange;
                        break;
                    case ClientLogger.LogLevel.Info:
                        item.ForeColor = Color.Blue;
                        break;
                    case ClientLogger.LogLevel.Debug:
                        item.ForeColor = Color.Gray;
                        break;
                }

                this.logListView.Items.Add(item);
            }

            // Auto-select first item if available
            if (this.logListView.Items.Count > 0 && this.logListView.SelectedItems.Count == 0)
            {
                this.logListView.Items[0].Selected = true;
            }
        }

        /// <summary>
        /// Gets the selected log level filter.
        /// </summary>
        private ClientLogger.LogLevel GetSelectedLogLevel()
        {
            switch (this.levelFilterComboBox.SelectedIndex)
            {
                case 0: return ClientLogger.LogLevel.Debug; // All
                case 1: return ClientLogger.LogLevel.Info;
                case 2: return ClientLogger.LogLevel.Warning;
                case 3: return ClientLogger.LogLevel.Error;
                default: return ClientLogger.LogLevel.Info;
            }
        }

        /// <summary>
        /// Starts auto-refresh timer.
        /// </summary>
        private void StartAutoRefresh()
        {
            this.refreshTimer.Start();
        }

        /// <summary>
        /// Stops auto-refresh timer.
        /// </summary>
        private void StopAutoRefresh()
        {
            this.refreshTimer.Stop();
        }

        /// <summary>
        /// Handles log list view selection change.
        /// </summary>
        private void LogListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.logListView.SelectedItems.Count > 0)
            {
                var entry = this.logListView.SelectedItems[0].Tag as ClientLogger.LogEntry;
                if (entry != null)
                {
                    this.detailsTextBox.Text = entry.ToString();
                }
            }
            else
            {
                this.detailsTextBox.Text = string.Empty;
            }
        }

        /// <summary>
        /// Handles level filter combo box selection change.
        /// </summary>
        private void LevelFilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadLogs();
        }

        /// <summary>
        /// Handles refresh button click.
        /// </summary>
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadLogs();
        }

        /// <summary>
        /// Handles clear button click.
        /// </summary>
        private void ClearButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定要清除所有日志吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ClientLogger.Clear();
                LoadLogs();
            }
        }

        /// <summary>
        /// Handles export button click.
        /// </summary>
        private void ExportButton_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
                dialog.FileName = string.Format("MakeMeAdmin_Log_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now);
                dialog.DefaultExt = "txt";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ClientLogger.ExportToFile(dialog.FileName);
                        MessageBox.Show("日志已导出到: " + dialog.FileName, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Handles refresh timer tick.
        /// </summary>
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            LoadLogs();
        }

        /// <summary>
        /// Clean up resources.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopAutoRefresh();
            base.OnFormClosing(e);
        }
    }
}
