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
    using System.Windows.Forms;

    /// <summary>
    /// Form for configuring server address settings.
    /// </summary>
    internal partial class ServerSettingsForm : Form
    {
        private TextBox serverAddressTextBox;
        private Button okButton;
        private Button cancelButton;
        private Label serverAddressLabel;
        private Label descriptionLabel;

        /// <summary>
        /// Initializes a new instance of the ServerSettingsForm class.
        /// </summary>
        public ServerSettingsForm()
        {
            this.InitializeComponent();
            this.LoadSettings();
        }

        /// <summary>
        /// Initializes the form components.
        /// </summary>
        private void InitializeComponent()
        {
            this.serverAddressLabel = new Label();
            this.serverAddressTextBox = new TextBox();
            this.descriptionLabel = new Label();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.SuspendLayout();
            // 
            // descriptionLabel
            // 
            this.descriptionLabel.AutoSize = true;
            this.descriptionLabel.Location = new System.Drawing.Point(12, 12);
            this.descriptionLabel.Name = "descriptionLabel";
            this.descriptionLabel.Size = new System.Drawing.Size(350, 13);
            this.descriptionLabel.Text = "请输入 Make Me Admin 服务器的地址（IP地址或主机名）：";
            // 
            // serverAddressLabel
            // 
            this.serverAddressLabel.AutoSize = true;
            this.serverAddressLabel.Location = new System.Drawing.Point(12, 45);
            this.serverAddressLabel.Name = "serverAddressLabel";
            this.serverAddressLabel.Size = new System.Drawing.Size(65, 13);
            this.serverAddressLabel.Text = "服务器地址：";
            // 
            // serverAddressTextBox
            // 
            this.serverAddressTextBox.Location = new System.Drawing.Point(12, 65);
            this.serverAddressTextBox.Name = "serverAddressTextBox";
            this.serverAddressTextBox.Size = new System.Drawing.Size(350, 20);
            this.serverAddressTextBox.TabIndex = 1;
            // 
            // okButton
            // 
            this.okButton.DialogResult = DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(206, 100);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "确定";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new EventHandler(this.OkButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(287, 100);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "取消";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // ServerSettingsForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(374, 135);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.serverAddressTextBox);
            this.Controls.Add(this.serverAddressLabel);
            this.Controls.Add(this.descriptionLabel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ServerSettingsForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "服务器设置";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// Loads the current settings into the form.
        /// </summary>
        private void LoadSettings()
        {
            string currentAddress = Settings.ServerAddress;
            if (currentAddress == "localhost")
            {
                this.serverAddressTextBox.Text = "";
            }
            else
            {
                this.serverAddressTextBox.Text = currentAddress;
            }
        }

        /// <summary>
        /// Handles the OK button click event.
        /// </summary>
        private void OkButton_Click(object sender, EventArgs e)
        {
            string address = this.serverAddressTextBox.Text.Trim();
            
            // If empty, use localhost
            if (string.IsNullOrEmpty(address))
            {
                address = "localhost";
            }

            // Validate address format (basic validation)
            if (address != "localhost" && 
                !System.Text.RegularExpressions.Regex.IsMatch(address, @"^([a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$") && // hostname
                !System.Text.RegularExpressions.Regex.IsMatch(address, @"^(\d{1,3}\.){3}\d{1,3}$")) // IP address
            {
                MessageBox.Show(
                    "请输入有效的服务器地址（IP地址或主机名）。\n例如：192.168.1.100 或 makemeadmin-server",
                    "无效的服务器地址",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Save the setting
            try
            {
                Settings.ServerAddress = address;
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("保存设置时发生错误：{0}", ex.Message),
                    "错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
            }
        }
    }
}
