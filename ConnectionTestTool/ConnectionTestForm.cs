// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//

namespace SinclairCC.MakeMeAdmin.ConnectionTestTool
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Security;
    using System.Text;
    using System.Windows.Forms;
    using SinclairCC.MakeMeAdmin;
    using Microsoft.Win32;

    /// <summary>
    /// Form for testing WCF connection to Make Me Admin service.
    /// </summary>
    public partial class ConnectionTestForm : Form
    {
        private TextBox serverAddressTextBox;
        private CheckBox useSecureModeCheckBox;
        private Button testConnectionButton;
        private TextBox resultTextBox;
        private Button clearButton;
        private Label statusLabel;
        private ProgressBar progressBar;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ConnectionTestForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        /// <summary>
        /// Initializes the form components.
        /// </summary>
        private void InitializeComponent()
        {
            this.serverAddressTextBox = new TextBox();
            this.useSecureModeCheckBox = new CheckBox();
            this.testConnectionButton = new Button();
            this.resultTextBox = new TextBox();
            this.clearButton = new Button();
            this.statusLabel = new Label();
            this.progressBar = new ProgressBar();

            this.SuspendLayout();

            // Form
            this.Text = "Make Me Admin - 连接测试工具";
            this.Size = new System.Drawing.Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(600, 400);

            // Server Address Label
            Label serverAddressLabel = new Label();
            serverAddressLabel.Text = "服务器地址:";
            serverAddressLabel.Location = new System.Drawing.Point(12, 15);
            serverAddressLabel.Size = new System.Drawing.Size(80, 23);
            serverAddressLabel.AutoSize = true;

            // Server Address TextBox
            this.serverAddressTextBox.Location = new System.Drawing.Point(98, 12);
            this.serverAddressTextBox.Size = new System.Drawing.Size(300, 23);
            this.serverAddressTextBox.TabIndex = 0;

            // Use Secure Mode CheckBox
            this.useSecureModeCheckBox.Text = "使用安全模式 (Windows 身份验证)";
            this.useSecureModeCheckBox.Location = new System.Drawing.Point(98, 45);
            this.useSecureModeCheckBox.Size = new System.Drawing.Size(300, 23);
            this.useSecureModeCheckBox.Checked = true;
            this.useSecureModeCheckBox.TabIndex = 1;

            // Test Connection Button
            this.testConnectionButton.Text = "测试连接";
            this.testConnectionButton.Location = new System.Drawing.Point(410, 12);
            this.testConnectionButton.Size = new System.Drawing.Size(100, 50);
            this.testConnectionButton.TabIndex = 2;
            this.testConnectionButton.Click += TestConnectionButton_Click;

            // Clear Button
            this.clearButton.Text = "清除";
            this.clearButton.Location = new System.Drawing.Point(520, 12);
            this.clearButton.Size = new System.Drawing.Size(75, 50);
            this.clearButton.TabIndex = 3;
            this.clearButton.Click += ClearButton_Click;

            // Status Label
            this.statusLabel.Text = "就绪";
            this.statusLabel.Location = new System.Drawing.Point(12, 80);
            this.statusLabel.Size = new System.Drawing.Size(600, 23);
            this.statusLabel.AutoSize = true;

            // Progress Bar
            this.progressBar.Location = new System.Drawing.Point(12, 110);
            this.progressBar.Size = new System.Drawing.Size(760, 23);
            this.progressBar.Style = ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;

            // Result TextBox
            this.resultTextBox.Location = new System.Drawing.Point(12, 145);
            this.resultTextBox.Size = new System.Drawing.Size(760, 400);
            this.resultTextBox.Multiline = true;
            this.resultTextBox.ReadOnly = true;
            this.resultTextBox.ScrollBars = ScrollBars.Both;
            this.resultTextBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.resultTextBox.TabIndex = 4;
            this.resultTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Add controls to form
            this.Controls.Add(serverAddressLabel);
            this.Controls.Add(this.serverAddressTextBox);
            this.Controls.Add(this.useSecureModeCheckBox);
            this.Controls.Add(this.testConnectionButton);
            this.Controls.Add(this.clearButton);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.resultTextBox);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// Loads settings from registry.
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                // Load server address
                string serverAddress = null;
                try
                {
                    var regPath = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Sinclair Community College\Make Me Admin");
                    if (regPath != null)
                    {
                        serverAddress = regPath.GetValue("Server Address") as string;
                        regPath.Close();
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(serverAddress))
                {
                    try
                    {
                        var regPath = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Sinclair Community College\Make Me Admin");
                        if (regPath != null)
                        {
                            serverAddress = regPath.GetValue("Server Address") as string;
                            regPath.Close();
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(serverAddress) && serverAddress != "localhost")
                {
                    this.serverAddressTextBox.Text = serverAddress;
                }

                // Load secure mode
                bool useSecureMode = true; // default
                try
                {
                    var regPath = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Sinclair Community College\Make Me Admin");
                    if (regPath != null)
                    {
                        string value = regPath.GetValue("Use Secure Mode") as string;
                        if (!string.IsNullOrEmpty(value) && bool.TryParse(value, out bool result))
                        {
                            useSecureMode = result;
                        }
                        regPath.Close();
                    }
                }
                catch { }

                this.useSecureModeCheckBox.Checked = useSecureMode;
            }
            catch (Exception ex)
            {
                AppendResult("加载设置失败: " + ex.Message);
            }
        }

        /// <summary>
        /// Handles test connection button click.
        /// </summary>
        private async void TestConnectionButton_Click(object sender, EventArgs e)
        {
            this.testConnectionButton.Enabled = false;
            this.progressBar.Visible = true;
            this.statusLabel.Text = "正在测试连接...";
            this.resultTextBox.Clear();

            string serverAddress = this.serverAddressTextBox.Text.Trim();
            if (string.IsNullOrEmpty(serverAddress))
            {
                serverAddress = "localhost";
            }

            bool useSecureMode = this.useSecureModeCheckBox.Checked;

            AppendResult("========================================");
            AppendResult("Make Me Admin 连接测试");
            AppendResult("========================================");
            AppendResult("");
            AppendResult(string.Format("测试时间: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            AppendResult(string.Format("服务器地址: {0}", serverAddress));
            AppendResult(string.Format("安全模式: {0}", useSecureMode ? "启用 (SecurityMode.Transport + Windows)" : "禁用 (SecurityMode.None)"));
            AppendResult("");

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    TestConnection(serverAddress, useSecureMode);
                });
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("测试过程中发生异常: {0}", ex.Message));
                AppendResult(ex.StackTrace);
            }
            finally
            {
                this.testConnectionButton.Enabled = true;
                this.progressBar.Visible = false;
                this.statusLabel.Text = "测试完成";
            }
        }

        /// <summary>
        /// Tests the connection.
        /// </summary>
        private void TestConnection(string serverAddress, bool useSecureMode)
        {
            ChannelFactory<IAdminGroup> factory = null;
            IAdminGroup channel = null;

            try
            {
                // Step 1: Construct address
                AppendResult("[步骤 1] 构造服务地址...");
                if (string.IsNullOrEmpty(serverAddress))
                {
                    serverAddress = "localhost";
                }
                string tcpAddress = string.Format("net.tcp://{0}/MakeMeAdmin/Service", serverAddress);
                AppendResult(string.Format("  地址: {0}", tcpAddress));
                AppendResult("  ✓ 成功");
                AppendResult("");

                // Step 2: Create binding
                AppendResult("[步骤 2] 创建绑定...");
                NetTcpBinding binding;
                if (useSecureMode)
                {
                    binding = new NetTcpBinding(SecurityMode.Transport);
                    binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                    AppendResult("  安全模式: Transport");
                    AppendResult("  客户端凭据类型: Windows");
                }
                else
                {
                    binding = new NetTcpBinding(SecurityMode.None);
                    AppendResult("  安全模式: None (无加密和身份验证)");
                }
                AppendResult(string.Format("  MaxReceivedMessageSize: {0}", binding.MaxReceivedMessageSize));
                AppendResult(string.Format("  MaxBufferSize: {0}", binding.MaxBufferSize));
                AppendResult("  ✓ 成功");
                AppendResult("");

                // Step 3: Create channel factory
                AppendResult("[步骤 3] 创建通道工厂...");
                factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
                AppendResult("  ✓ 成功");
                AppendResult("");

                // Step 4: Create channel
                AppendResult("[步骤 4] 创建通道...");
                channel = factory.CreateChannel();
                AppendResult("  ✓ 成功");
                AppendResult("");

                // Step 5: Open channel
                AppendResult("[步骤 5] 打开通道...");
                ICommunicationObject commObj = channel as ICommunicationObject;
                if (commObj != null)
                {
                    AppendResult(string.Format("  通道状态 (打开前): {0}", commObj.State));

                    if (commObj.State == CommunicationState.Created)
                    {
                        commObj.Open();
                        AppendResult(string.Format("  通道状态 (打开后): {0}", commObj.State));
                        AppendResult("  ✓ 成功");
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("通道状态异常: {0}", commObj.State));
                    }
                }
                AppendResult("");

                // Step 6: Test service call
                AppendResult("[步骤 6] 测试服务调用...");
                try
                {
                    // Try to call a simple method
                    bool hasPending = channel.HasPendingRequest();
                    AppendResult("  调用 HasPendingRequest() 成功");
                    AppendResult(string.Format("  返回值: {0}", hasPending));
                    AppendResult("  ✓ 服务调用成功");
                }
                catch (Exception ex)
                {
                    AppendResult(string.Format("  ⚠ 服务调用失败: {0}", ex.Message));
                    AppendResult(string.Format("  类型: {0}", ex.GetType().Name));
                    if (ex.InnerException != null)
                    {
                        AppendResult(string.Format("  内部异常: {0}", ex.InnerException.Message));
                    }
                }
                AppendResult("");

                // Success
                AppendResult("========================================");
                AppendResult("✓ 连接测试成功！");
                AppendResult("========================================");
                AppendResult("");
                AppendResult("连接信息:");
                AppendResult(string.Format("  服务器地址: {0}", serverAddress));
                AppendResult(string.Format("  服务地址: {0}", tcpAddress));
                AppendResult(string.Format("  安全模式: {0}", useSecureMode ? "Transport + Windows" : "None"));
                AppendResult(string.Format("  通道状态: {0}", commObj?.State ?? CommunicationState.Closed));

                this.Invoke(new Action(() =>
                {
                    this.statusLabel.Text = "连接成功！";
                    this.statusLabel.ForeColor = System.Drawing.Color.Green;
                }));
            }
            catch (MessageSecurityException ex)
            {
                HandleSecurityException(ex, "MessageSecurityException");
            }
            catch (SecurityNegotiationException ex)
            {
                HandleSecurityException(ex, "SecurityNegotiationException");
            }
            catch (EndpointNotFoundException ex)
            {
                HandleConnectionException(ex, "EndpointNotFoundException", "无法找到服务端点");
            }
            catch (CommunicationObjectFaultedException ex)
            {
                HandleConnectionException(ex, "CommunicationObjectFaultedException", "通信通道已故障");
            }
            catch (CommunicationException ex)
            {
                HandleConnectionException(ex, "CommunicationException", "通信错误");
            }
            catch (Exception ex)
            {
                AppendResult("========================================");
                AppendResult("✗ 连接测试失败");
                AppendResult("========================================");
                AppendResult("");
                AppendResult(string.Format("异常类型: {0}", ex.GetType().Name));
                AppendResult(string.Format("错误消息: {0}", ex.Message));
                AppendResult(string.Format("HResult: 0x{0:X8}", ex.HResult));
                AppendResult("");

                if (ex.InnerException != null)
                {
                    AppendResult("内部异常 1:");
                    AppendResult(string.Format("  类型: {0}", ex.InnerException.GetType().Name));
                    AppendResult(string.Format("  消息: {0}", ex.InnerException.Message));
                    AppendResult("");

                    if (ex.InnerException.InnerException != null)
                    {
                        AppendResult("内部异常 2:");
                        AppendResult(string.Format("  类型: {0}", ex.InnerException.InnerException.GetType().Name));
                        AppendResult(string.Format("  消息: {0}", ex.InnerException.InnerException.Message));
                        AppendResult("");
                    }
                }

                AppendResult("堆栈跟踪:");
                AppendResult(ex.StackTrace);

                this.Invoke(new Action(() =>
                {
                    this.statusLabel.Text = "连接失败！";
                    this.statusLabel.ForeColor = System.Drawing.Color.Red;
                }));
            }
            finally
            {
                // Clean up
                if (channel != null)
                {
                    try
                    {
                        ICommunicationObject commObj = channel as ICommunicationObject;
                        if (commObj != null && commObj.State == CommunicationState.Opened)
                        {
                            commObj.Close();
                        }
                    }
                    catch { }
                }

                if (factory != null)
                {
                    try
                    {
                        ICommunicationObject factoryCommObj = factory as ICommunicationObject;
                        if (factoryCommObj != null && factoryCommObj.State == CommunicationState.Faulted)
                        {
                            factoryCommObj.Abort();
                        }
                        else
                        {
                            factory.Close();
                        }
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Handles security exceptions.
        /// </summary>
        private void HandleSecurityException(Exception ex, string exceptionType)
        {
            AppendResult("========================================");
            AppendResult(string.Format("✗ 安全错误: {0}", exceptionType));
            AppendResult("========================================");
            AppendResult("");
            AppendResult(string.Format("错误消息: {0}", ex.Message));
            AppendResult(string.Format("HResult: 0x{0:X8}", ex.HResult));
            AppendResult("");

            if (ex.InnerException != null)
            {
                AppendResult("内部异常 1:");
                AppendResult(string.Format("  类型: {0}", ex.InnerException.GetType().Name));
                AppendResult(string.Format("  消息: {0}", ex.InnerException.Message));
                AppendResult("");

                if (ex.InnerException.InnerException != null)
                {
                    AppendResult("内部异常 2:");
                    AppendResult(string.Format("  类型: {0}", ex.InnerException.InnerException.GetType().Name));
                    AppendResult(string.Format("  消息: {0}", ex.InnerException.InnerException.Message));
                    AppendResult("");
                }
            }

            AppendResult("可能的原因:");
            AppendResult("  1. 客户端和服务器的安全配置不匹配");
            AppendResult("  2. 服务器上的服务版本与客户端不兼容");
            AppendResult("  3. Windows 身份验证配置问题");
            AppendResult("  4. 跨域/跨工作组身份验证失败");
            AppendResult("");
            AppendResult("解决方法:");
            AppendResult("  1. 确保服务端和客户端使用相同的安全模式配置");
            AppendResult("  2. 重新编译并重新安装服务端服务");
            AppendResult("  3. 检查服务器事件日志");
            AppendResult("  4. 如果跨环境，考虑使用 SecurityMode.None（不安全）");

            this.Invoke(new Action(() =>
            {
                this.statusLabel.Text = "安全错误！";
                this.statusLabel.ForeColor = System.Drawing.Color.Red;
            }));
        }

        /// <summary>
        /// Handles connection exceptions.
        /// </summary>
        private void HandleConnectionException(Exception ex, string exceptionType, string description)
        {
            AppendResult("========================================");
            AppendResult(string.Format("✗ 连接错误: {0}", exceptionType));
            AppendResult("========================================");
            AppendResult("");
            AppendResult(string.Format("描述: {0}", description));
            AppendResult(string.Format("错误消息: {0}", ex.Message));
            AppendResult("");

            AppendResult("可能的原因:");
            AppendResult("  1. 服务器地址配置错误");
            AppendResult("  2. 服务器上的 Make Me Admin 服务未运行");
            AppendResult("  3. 防火墙阻止了连接");
            AppendResult("  4. 网络连接问题");
            AppendResult("");
            AppendResult("解决方法:");
            AppendResult("  1. 检查服务器地址是否正确");
            AppendResult("  2. 确认服务器上的服务正在运行");
            AppendResult("  3. 检查防火墙设置");
            AppendResult("  4. 测试网络连接 (ping)");

            this.Invoke(new Action(() =>
            {
                this.statusLabel.Text = "连接失败！";
                this.statusLabel.ForeColor = System.Drawing.Color.Red;
            }));
        }

        /// <summary>
        /// Appends text to result textbox.
        /// </summary>
        private void AppendResult(string text)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AppendResult(text)));
                return;
            }

            this.resultTextBox.AppendText(text + Environment.NewLine);
            this.resultTextBox.SelectionStart = this.resultTextBox.Text.Length;
            this.resultTextBox.ScrollToCaret();
        }

        /// <summary>
        /// Handles clear button click.
        /// </summary>
        private void ClearButton_Click(object sender, EventArgs e)
        {
            this.resultTextBox.Clear();
            this.statusLabel.Text = "就绪";
            this.statusLabel.ForeColor = System.Drawing.Color.Black;
        }
    }
}
