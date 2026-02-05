// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//

namespace SinclairCC.MakeMeAdmin.ServerDiagnosticsTool
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.ServiceModel;
    using System.Text;
    using System.Windows.Forms;
    using SinclairCC.MakeMeAdmin;

    /// <summary>
    /// Form for diagnosing server-side issues.
    /// </summary>
    public partial class ServerDiagnosticsForm : Form
    {
        private TextBox serverAddressTextBox;
        private CheckBox useSecureModeCheckBox;
        private Button runDiagnosticsButton;
        private TextBox resultTextBox;
        private Button clearButton;
        private Label statusLabel;
        private ProgressBar progressBar;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ServerDiagnosticsForm()
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
            this.runDiagnosticsButton = new Button();
            this.resultTextBox = new TextBox();
            this.clearButton = new Button();
            this.statusLabel = new Label();
            this.progressBar = new ProgressBar();

            this.SuspendLayout();

            // Form
            this.Text = "Make Me Admin - 服务端诊断工具";
            this.Size = new System.Drawing.Size(900, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new System.Drawing.Size(700, 500);

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

            // Run Diagnostics Button
            this.runDiagnosticsButton.Text = "运行诊断";
            this.runDiagnosticsButton.Location = new System.Drawing.Point(410, 12);
            this.runDiagnosticsButton.Size = new System.Drawing.Size(120, 50);
            this.runDiagnosticsButton.TabIndex = 2;
            this.runDiagnosticsButton.Click += RunDiagnosticsButton_Click;

            // Clear Button
            this.clearButton.Text = "清除";
            this.clearButton.Location = new System.Drawing.Point(540, 12);
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
            this.progressBar.Size = new System.Drawing.Size(860, 23);
            this.progressBar.Style = ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;

            // Result TextBox
            this.resultTextBox.Location = new System.Drawing.Point(12, 145);
            this.resultTextBox.Size = new System.Drawing.Size(860, 520);
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
            this.Controls.Add(this.runDiagnosticsButton);
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
        /// Handles run diagnostics button click.
        /// </summary>
        private async void RunDiagnosticsButton_Click(object sender, EventArgs e)
        {
            this.runDiagnosticsButton.Enabled = false;
            this.progressBar.Visible = true;
            this.statusLabel.Text = "正在运行诊断...";
            this.resultTextBox.Clear();

            string serverAddress = this.serverAddressTextBox.Text.Trim();
            if (string.IsNullOrEmpty(serverAddress))
            {
                serverAddress = "localhost";
            }

            bool useSecureMode = this.useSecureModeCheckBox.Checked;

            AppendResult("========================================");
            AppendResult("Make Me Admin 服务端诊断工具");
            AppendResult("========================================");
            AppendResult("");
            AppendResult(string.Format("诊断时间: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            AppendResult(string.Format("服务器地址: {0}", serverAddress));
            AppendResult(string.Format("安全模式: {0}", useSecureMode ? "启用 (SecurityMode.Transport + Windows)" : "禁用 (SecurityMode.None)"));
            AppendResult("");

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    RunServerDiagnostics(serverAddress, useSecureMode);
                });
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("诊断过程中发生异常: {0}", ex.Message));
                AppendResult(ex.StackTrace);
            }
            finally
            {
                this.runDiagnosticsButton.Enabled = true;
                this.progressBar.Visible = false;
                this.statusLabel.Text = "诊断完成";
            }
        }

        /// <summary>
        /// Runs comprehensive server diagnostics.
        /// </summary>
        private void RunServerDiagnostics(string serverAddress, bool useSecureMode)
        {
            // Section 1: Check service status
            AppendResult("========================================");
            AppendResult("1. 检查服务状态");
            AppendResult("========================================");
            CheckServiceStatus();
            AppendResult("");

            // Section 2: Check server configuration
            AppendResult("========================================");
            AppendResult("2. 检查服务端配置");
            AppendResult("========================================");
            CheckServerConfiguration();
            AppendResult("");

            // Section 3: Test basic connection
            AppendResult("========================================");
            AppendResult("3. 测试基本连接");
            AppendResult("========================================");
            TestBasicConnection(serverAddress, useSecureMode);
            AppendResult("");

            // Section 4: Test HasPendingRequest method
            AppendResult("========================================");
            AppendResult("4. 测试 HasPendingRequest() 方法");
            AppendResult("========================================");
            TestHasPendingRequest(serverAddress, useSecureMode);
            AppendResult("");

            // Section 5: Test GetPendingRequests method (the problematic one)
            AppendResult("========================================");
            AppendResult("5. 测试 GetPendingRequests() 方法（问题方法）");
            AppendResult("========================================");
            TestGetPendingRequests(serverAddress, useSecureMode);
            AppendResult("");

            // Section 6: Check file permissions and paths
            AppendResult("========================================");
            AppendResult("6. 检查文件权限和路径");
            AppendResult("========================================");
            CheckFilePermissions();
            AppendResult("");

            // Section 7: Check service logs
            AppendResult("========================================");
            AppendResult("7. 检查服务端事件日志");
            AppendResult("========================================");
            CheckServiceLogs();
            AppendResult("");

            // Section 8: Summary and recommendations
            AppendResult("========================================");
            AppendResult("8. 诊断总结和建议");
            AppendResult("========================================");
            AppendSummary();
        }

        /// <summary>
        /// Checks the service status.
        /// </summary>
        private void CheckServiceStatus()
        {
            try
            {
                AppendResult("[检查] Windows 服务状态...");
                
                System.ServiceProcess.ServiceController service = null;
                try
                {
                    service = new System.ServiceProcess.ServiceController("Make Me Admin");
                    AppendResult(string.Format("  服务名称: {0}", service.ServiceName));
                    AppendResult(string.Format("  显示名称: {0}", service.DisplayName));
                    AppendResult(string.Format("  服务状态: {0}", service.Status));
                    AppendResult(string.Format("  服务类型: {0}", service.ServiceType));
                    AppendResult(string.Format("  可接受命令: {0}", string.Join(", ", service.CanStop, service.CanPauseAndContinue, service.CanShutdown)));
                    
                    if (service.Status == System.ServiceProcess.ServiceControllerStatus.Running)
                    {
                        AppendResult("  ✓ 服务正在运行");
                    }
                    else
                    {
                        AppendResult(string.Format("  ⚠ 服务未运行 (状态: {0})", service.Status));
                        AppendResult("  建议: 启动服务");
                    }
                }
                catch (Exception ex)
                {
                    AppendResult(string.Format("  ✗ 无法访问服务: {0}", ex.Message));
                    AppendResult("  可能原因: 服务未安装或名称不正确");
                }
                finally
                {
                    service?.Dispose();
                }
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("检查服务状态时发生错误: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Checks server configuration.
        /// </summary>
        private void CheckServerConfiguration()
        {
            try
            {
                AppendResult("[检查] 服务端注册表配置...");
                
                // Check HKLM configuration
                try
                {
                    var regPath = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Sinclair Community College\Make Me Admin");
                    if (regPath != null)
                    {
                        AppendResult("  HKLM 配置:");
                        
                        string useSecureMode = regPath.GetValue("Use Secure Mode") as string;
                        AppendResult(string.Format("    Use Secure Mode: {0}", useSecureMode ?? "(未设置)"));
                        
                        string serverAddress = regPath.GetValue("Server Address") as string;
                        AppendResult(string.Format("    Server Address: {0}", serverAddress ?? "(未设置)"));
                        
                        regPath.Close();
                    }
                    else
                    {
                        AppendResult("  HKLM 配置: (未找到)");
                    }
                }
                catch (Exception ex)
                {
                    AppendResult(string.Format("  读取 HKLM 配置失败: {0}", ex.Message));
                }
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("检查服务端配置时发生错误: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Tests basic connection.
        /// </summary>
        private void TestBasicConnection(string serverAddress, bool useSecureMode)
        {
            ChannelFactory<IAdminGroup> factory = null;
            IAdminGroup channel = null;

            try
            {
                AppendResult("[测试] 基本连接...");
                
                if (string.IsNullOrEmpty(serverAddress))
                {
                    serverAddress = "localhost";
                }
                string tcpAddress = string.Format("net.tcp://{0}/MakeMeAdmin/Service", serverAddress);
                
                AppendResult(string.Format("  服务地址: {0}", tcpAddress));
                
                NetTcpBinding binding;
                if (useSecureMode)
                {
                    binding = new NetTcpBinding(SecurityMode.Transport);
                    binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                }
                else
                {
                    binding = new NetTcpBinding(SecurityMode.None);
                }
                
                AppendResult(string.Format("  绑定配置:"));
                AppendResult(string.Format("    SecurityMode: {0}", binding.Security.Mode));
                AppendResult(string.Format("    ReceiveTimeout: {0}", binding.ReceiveTimeout));
                AppendResult(string.Format("    SendTimeout: {0}", binding.SendTimeout));
                
                factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
                channel = factory.CreateChannel();
                
                ICommunicationObject commObj = channel as ICommunicationObject;
                if (commObj != null)
                {
                    AppendResult(string.Format("  通道状态 (打开前): {0}", commObj.State));
                    
                    if (commObj.State == CommunicationState.Created)
                    {
                        DateTime startTime = DateTime.Now;
                        commObj.Open();
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;
                        
                        AppendResult(string.Format("  通道状态 (打开后): {0}", commObj.State));
                        AppendResult(string.Format("  打开耗时: {0} 毫秒", duration.TotalMilliseconds));
                        
                        if (commObj.State == CommunicationState.Opened)
                        {
                            AppendResult("  ✓ 基本连接成功");
                        }
                        else
                        {
                            AppendResult(string.Format("  ⚠ 通道状态异常: {0}", commObj.State));
                        }
                    }
                    else
                    {
                        AppendResult(string.Format("  ⚠ 通道状态异常 (打开前): {0}", commObj.State));
                    }
                }
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("  ✗ 基本连接失败: {0}", ex.Message));
                AppendResult(string.Format("  异常类型: {0}", ex.GetType().Name));
                if (ex.InnerException != null)
                {
                    AppendResult(string.Format("  内部异常: {0}", ex.InnerException.Message));
                }
            }
            finally
            {
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
        /// Tests HasPendingRequest method.
        /// </summary>
        private void TestHasPendingRequest(string serverAddress, bool useSecureMode)
        {
            ChannelFactory<IAdminGroup> factory = null;
            IAdminGroup channel = null;

            try
            {
                AppendResult("[测试] HasPendingRequest() 方法...");
                
                if (string.IsNullOrEmpty(serverAddress))
                {
                    serverAddress = "localhost";
                }
                string tcpAddress = string.Format("net.tcp://{0}/MakeMeAdmin/Service", serverAddress);
                
                NetTcpBinding binding;
                if (useSecureMode)
                {
                    binding = new NetTcpBinding(SecurityMode.Transport);
                    binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                }
                else
                {
                    binding = new NetTcpBinding(SecurityMode.None);
                }
                
                factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
                channel = factory.CreateChannel();
                
                ICommunicationObject commObj = channel as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Created)
                {
                    commObj.Open();
                }
                
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    AppendResult("  通道已打开，准备调用方法...");
                    
                    DateTime startTime = DateTime.Now;
                    bool result = channel.HasPendingRequest();
                    DateTime endTime = DateTime.Now;
                    TimeSpan duration = endTime - startTime;
                    
                    AppendResult(string.Format("  调用成功，耗时: {0} 毫秒", duration.TotalMilliseconds));
                    AppendResult(string.Format("  返回值: {0}", result));
                    AppendResult("  ✓ HasPendingRequest() 调用成功");
                }
                else
                {
                    AppendResult(string.Format("  ✗ 通道未打开 (状态: {0})", commObj?.State ?? CommunicationState.Closed));
                }
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("  ✗ HasPendingRequest() 调用失败: {0}", ex.Message));
                AppendResult(string.Format("  异常类型: {0}", ex.GetType().Name));
                AppendResult(string.Format("  HResult: 0x{0:X8}", ex.HResult));
                if (ex.InnerException != null)
                {
                    AppendResult(string.Format("  内部异常: {0}", ex.InnerException.Message));
                    AppendResult(string.Format("  内部异常类型: {0}", ex.InnerException.GetType().Name));
                }
            }
            finally
            {
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
        /// Tests GetPendingRequests method with detailed timing and error analysis.
        /// </summary>
        private void TestGetPendingRequests(string serverAddress, bool useSecureMode)
        {
            ChannelFactory<IAdminGroup> factory = null;
            IAdminGroup channel = null;

            try
            {
                AppendResult("[测试] GetPendingRequests() 方法...");
                AppendResult("  注意: 这是导致连接失败的方法，将进行详细测试");
                
                if (string.IsNullOrEmpty(serverAddress))
                {
                    serverAddress = "localhost";
                }
                string tcpAddress = string.Format("net.tcp://{0}/MakeMeAdmin/Service", serverAddress);
                
                NetTcpBinding binding;
                if (useSecureMode)
                {
                    binding = new NetTcpBinding(SecurityMode.Transport);
                    binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                }
                else
                {
                    binding = new NetTcpBinding(SecurityMode.None);
                }
                
                AppendResult(string.Format("  绑定超时设置:"));
                AppendResult(string.Format("    ReceiveTimeout: {0}", binding.ReceiveTimeout));
                AppendResult(string.Format("    SendTimeout: {0}", binding.SendTimeout));
                
                factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
                channel = factory.CreateChannel();
                
                ICommunicationObject commObj = channel as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Created)
                {
                    commObj.Open();
                }
                
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    AppendResult("  通道已打开，准备调用 GetPendingRequests()...");
                    AppendResult("  开始计时...");
                    
                    DateTime startTime = DateTime.Now;
                    PendingRequest[] requests = null;
                    Exception caughtException = null;
                    
                    try
                    {
                        requests = channel.GetPendingRequests();
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;
                        
                        AppendResult(string.Format("  调用成功，耗时: {0} 毫秒 ({1} 秒)", duration.TotalMilliseconds, duration.TotalSeconds));
                        AppendResult(string.Format("  返回请求数量: {0}", requests != null ? requests.Length : 0));
                        
                        if (requests != null && requests.Length > 0)
                        {
                            AppendResult("  请求详情:");
                            foreach (var req in requests.Take(5)) // Show first 5
                            {
                                AppendResult(string.Format("    - {0} ({1})", req.UserName, req.RequestId));
                            }
                            if (requests.Length > 5)
                            {
                                AppendResult(string.Format("    ... 还有 {0} 个请求", requests.Length - 5));
                            }
                        }
                        
                        AppendResult("  ✓ GetPendingRequests() 调用成功");
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                        DateTime endTime = DateTime.Now;
                        TimeSpan duration = endTime - startTime;
                        
                        AppendResult(string.Format("  ✗ GetPendingRequests() 调用失败，耗时: {0} 毫秒 ({1} 秒)", duration.TotalMilliseconds, duration.TotalSeconds));
                        AppendResult(string.Format("  异常类型: {0}", ex.GetType().Name));
                        AppendResult(string.Format("  错误消息: {0}", ex.Message));
                        AppendResult(string.Format("  HResult: 0x{0:X8}", ex.HResult));
                        
                        if (ex.InnerException != null)
                        {
                            AppendResult("  内部异常 1:");
                            AppendResult(string.Format("    类型: {0}", ex.InnerException.GetType().Name));
                            AppendResult(string.Format("    消息: {0}", ex.InnerException.Message));
                            AppendResult(string.Format("    HResult: 0x{0:X8}", ex.InnerException.HResult));
                            
                            if (ex.InnerException.InnerException != null)
                            {
                                AppendResult("  内部异常 2:");
                                AppendResult(string.Format("    类型: {0}", ex.InnerException.InnerException.GetType().Name));
                                AppendResult(string.Format("    消息: {0}", ex.InnerException.InnerException.Message));
                                AppendResult(string.Format("    HResult: 0x{0:X8}", ex.InnerException.InnerException.HResult));
                            }
                        }
                        
                        // Analyze the error
                        if (duration.TotalSeconds > 55 && duration.TotalSeconds < 65)
                        {
                            AppendResult("  ⚠ 分析: 调用耗时约1分钟，可能是服务端超时设置问题");
                            AppendResult("  建议: 检查服务端 NetTcpBinding 的 ReceiveTimeout 和 SendTimeout 设置");
                        }
                        else if (ex.Message.Contains("套接字连接已中止") || ex.Message.Contains("socket connection") || ex.Message.Contains("aborted"))
                        {
                            AppendResult("  ⚠ 分析: 连接被中止，可能是:");
                            AppendResult("    1. 服务端处理时间过长，超过超时限制");
                            AppendResult("    2. 服务端在处理 GetPendingRequests() 时发生异常");
                            AppendResult("    3. 文件 I/O 或加密/解密操作耗时过长");
                            AppendResult("  建议: 检查服务端日志，查看是否有异常记录");
                        }
                    }
                }
                else
                {
                    AppendResult(string.Format("  ✗ 通道未打开 (状态: {0})", commObj?.State ?? CommunicationState.Closed));
                }
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("  ✗ 测试 GetPendingRequests() 时发生错误: {0}", ex.Message));
                AppendResult(string.Format("  异常类型: {0}", ex.GetType().Name));
            }
            finally
            {
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
        /// Checks file permissions and paths.
        /// </summary>
        private void CheckFilePermissions()
        {
            try
            {
                AppendResult("[检查] 待审批请求文件...");
                
                string filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Make Me Admin",
                    "pendingRequests.xml");
                
                AppendResult(string.Format("  文件路径: {0}", filePath));
                
                if (File.Exists(filePath))
                {
                    AppendResult("  ✓ 文件存在");
                    
                    FileInfo fileInfo = new FileInfo(filePath);
                    AppendResult(string.Format("  文件大小: {0} 字节", fileInfo.Length));
                    AppendResult(string.Format("  最后修改时间: {0}", fileInfo.LastWriteTime));
                    
                    try
                    {
                        // Try to read file attributes
                        FileAttributes attributes = fileInfo.Attributes;
                        AppendResult(string.Format("  文件属性: {0}", attributes));
                    }
                    catch (Exception ex)
                    {
                        AppendResult(string.Format("  ⚠ 无法读取文件属性: {0}", ex.Message));
                    }
                    
                    // Check if service account can access the file
                    try
                    {
                        using (FileStream fs = File.OpenRead(filePath))
                        {
                            AppendResult("  ✓ 文件可读");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendResult(string.Format("  ✗ 文件读取失败: {0}", ex.Message));
                        AppendResult("  可能原因: 权限不足或文件被锁定");
                    }
                }
                else
                {
                    AppendResult("  ⚠ 文件不存在");
                    AppendResult("  这可能是正常的（如果没有待审批请求）");
                    
                    // Check directory
                    string directory = Path.GetDirectoryName(filePath);
                    if (Directory.Exists(directory))
                    {
                        AppendResult(string.Format("  ✓ 目录存在: {0}", directory));
                    }
                    else
                    {
                        AppendResult(string.Format("  ✗ 目录不存在: {0}", directory));
                    }
                }
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("检查文件权限时发生错误: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Checks service event logs.
        /// </summary>
        private void CheckServiceLogs()
        {
            try
            {
                AppendResult("[检查] 服务端事件日志...");
                
                try
                {
                    EventLog eventLog = new EventLog("Application");
                    var recentEvents = eventLog.Entries.Cast<EventLogEntry>()
                        .Where(e => e.Source == "Make Me Admin")
                        .OrderByDescending(e => e.TimeGenerated)
                        .Take(10)
                        .ToList();
                    
                    if (recentEvents.Any())
                    {
                        AppendResult(string.Format("  找到 {0} 条最近的日志:", recentEvents.Count));
                        foreach (var evt in recentEvents)
                        {
                            AppendResult(string.Format("    [{0}] {1}: {2}", 
                                evt.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss"),
                                evt.EntryType,
                                evt.Message.Substring(0, Math.Min(100, evt.Message.Length))));
                        }
                    }
                    else
                    {
                        AppendResult("  ⚠ 未找到最近的日志");
                    }
                }
                catch (Exception ex)
                {
                    AppendResult(string.Format("  读取事件日志失败: {0}", ex.Message));
                }
            }
            catch (Exception ex)
            {
                AppendResult(string.Format("检查服务日志时发生错误: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Appends summary and recommendations.
        /// </summary>
        private void AppendSummary()
        {
            AppendResult("诊断完成。请查看上述各部分的测试结果。");
            AppendResult("");
            AppendResult("常见问题解决方案:");
            AppendResult("1. 如果 GetPendingRequests() 超时:");
            AppendResult("   - 检查服务端 NetTcpBinding 的 ReceiveTimeout 和 SendTimeout 设置");
            AppendResult("   - 确保服务端代码已重新编译并重新安装");
            AppendResult("   - 检查服务端日志是否有异常");
            AppendResult("");
            AppendResult("2. 如果文件权限问题:");
            AppendResult("   - 确保服务运行账户有权限访问 AppData\\Make Me Admin 目录");
            AppendResult("   - 检查文件是否被其他进程锁定");
            AppendResult("");
            AppendResult("3. 如果服务未运行:");
            AppendResult("   - 启动 Make Me Admin 服务");
            AppendResult("   - 检查服务是否已正确安装");
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
