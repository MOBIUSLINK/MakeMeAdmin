// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//
// Make Me Admin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3.
//
// Make Me Admin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Make Me Admin. If not, see <http://www.gnu.org/licenses/>.
//

namespace SinclairCC.MakeMeAdmin
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.ServiceModel;
    using System.ServiceModel.Security;
    using System.Windows.Forms;

    /// <summary>
    /// Form for administrators to approve or reject pending requests.
    /// </summary>
    public partial class AdminApprovalForm : Form
    {
        private System.Timers.Timer refreshTimer;
        private IAdminGroup channel;
        private ChannelFactory<IAdminGroup> channelFactory;
        private bool connectionFailed = false; // Flag to prevent repeated connection attempts

        // Windows API for dark title bar
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        /// <summary>
        /// Sets the window title bar to dark mode.
        /// </summary>
        private void SetDarkTitleBar()
        {
            try
            {
                if (DwmSetWindowAttribute(Handle, IsWindows10OrGreater(17763) ? DWMWA_USE_IMMERSIVE_DARK_MODE : DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref intValue, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref intValue, sizeof(int));
                }
            }
            catch
            {
                // Ignore errors - dark mode may not be supported on older Windows versions
            }
        }

        private int intValue = 1; // 1 = enable dark mode, 0 = disable

        private bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && (build == -1 || Environment.OSVersion.Version.Build >= build);
        }

        /// <summary>
        /// Initializes a new instance of the AdminApprovalForm class.
        /// </summary>
        public AdminApprovalForm()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.SecurityLock;
            
            // Enable keyboard shortcuts
            this.KeyPreview = true;
            this.KeyDown += AdminApprovalForm_KeyDown;
            
            // Configure tooltip
            this.toolTip.IsBalloon = true;
            this.toolTip.ToolTipTitle = "提示";
            this.toolTip.AutomaticDelay = 500;
            
            // Configure dark mode for ListView
            this.requestsListView.OwnerDraw = false; // Use default drawing for better compatibility
            // Note: ListView selection colors are controlled by system theme in dark mode
            
            // Don't setup channel in constructor - do it when form loads
            // This allows the form to be created even if connection fails initially
            
            // Load requests after form is shown (window handle will be created)
            this.Load += (sender, e) =>
            {
                // Set dark title bar
                if (this.IsHandleCreated)
                {
                    this.SetDarkTitleBar();
                }
                else
                {
                    this.HandleCreated += (s, args) => this.SetDarkTitleBar();
                }
                
                // Try to setup channel when form loads, but don't block on errors
                // This allows user to access settings even if connection fails
                try
                {
                    this.SetupChannel();
                }
                catch
                {
                    // Ignore errors during initial setup - LoadRequests will handle them
                    // User can still access settings to change server address
                    this.connectionFailed = true;
                }
                
                // Try to load requests, but don't show blocking dialog if it fails
                // User can click refresh or access settings
                try
                {
                    this.LoadRequests();
                }
                catch
                {
                    // Silently fail - user can see the error when they try to interact
                    // Update status label to indicate connection issue
                    if (this.IsHandleCreated)
                    {
                        try
                        {
                            this.statusLabel.Text = "连接失败 - 请点击\"设置\"更改服务器地址";
                        }
                        catch { }
                    }
                }
            };
            
            // Setup refresh timer (refresh every 5 seconds)
            this.refreshTimer = new System.Timers.Timer(5000);
            this.refreshTimer.Elapsed += (sender, e) => 
            {
                // Don't auto-refresh if connection has failed - prevent error loops
                if (this.connectionFailed)
                {
                    return;
                }
                
                // Only refresh if window handle is created
                if (this.IsHandleCreated)
                {
                    if (this.InvokeRequired)
                    {
                        try
                        {
                            this.Invoke(new Action(() => this.LoadRequests()));
                        }
                        catch (InvalidOperationException)
                        {
                            // Window handle was destroyed, ignore
                        }
                    }
                    else
                    {
                        this.LoadRequests();
                    }
                }
            };
            this.refreshTimer.Start();
        }

        /// <summary>
        /// Checks if the WCF channel is in a usable state.
        /// </summary>
        private bool IsChannelUsable()
        {
            if (this.channel == null)
            {
                return false;
            }

            ICommunicationObject commObj = this.channel as ICommunicationObject;
            if (commObj != null)
            {
                return commObj.State == CommunicationState.Opened || commObj.State == CommunicationState.Created;
            }

            return true;
        }

        /// <summary>
        /// Closes and disposes the current channel and factory.
        /// </summary>
        private void CloseChannel()
        {
            if (this.channel != null)
            {
                try
                {
                    ICommunicationObject commObj = this.channel as ICommunicationObject;
                    if (commObj != null)
                    {
                        if (commObj.State == CommunicationState.Opened || commObj.State == CommunicationState.Opening)
                        {
                            commObj.Close();
                        }
                        else if (commObj.State == CommunicationState.Faulted)
                        {
                            commObj.Abort();
                        }
                    }
                }
                catch { }
                this.channel = null;
            }

            if (this.channelFactory != null)
            {
                try
                {
                    if (this.channelFactory.State == CommunicationState.Opened || this.channelFactory.State == CommunicationState.Opening)
                    {
                        this.channelFactory.Close();
                    }
                    else if (this.channelFactory.State == CommunicationState.Faulted)
                    {
                        this.channelFactory.Abort();
                    }
                }
                catch { }
                this.channelFactory = null;
            }
        }

        /// <summary>
        /// Sets up the WCF channel to communicate with the service.
        /// </summary>
        private void SetupChannel()
        {
            // Close existing channel if it exists
            this.CloseChannel();

            try
            {
                // Use TCP to connect to the server
                string serverAddress = Settings.ServerAddress;
                string tcpAddress = Shared.GetTcpServiceAddress(serverAddress);
                
                NetTcpBinding binding;
                if (Settings.UseSecureMode)
                {
                    // Secure mode: Use Windows authentication (for same-domain scenarios)
                    binding = new NetTcpBinding(SecurityMode.Transport);
                    binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                }
                else
                {
                    // Insecure mode: No authentication (for cross-environment scenarios: domain <-> workgroup)
                    // WARNING: This has no encryption or authentication - use only in trusted networks!
                    binding = new NetTcpBinding(SecurityMode.None);
                }
                
                // Use same timeout settings as connection test tool (default values)
                // The default ReceiveTimeout is 10 minutes, which should be sufficient
                // Setting explicit timeouts might cause issues - let WCF use defaults
                // binding.ReceiveTimeout = TimeSpan.FromMinutes(5);  // Removed - use default
                // binding.SendTimeout = TimeSpan.FromMinutes(5);     // Removed - use default
                
                // Keep message size limits consistent
                binding.MaxReceivedMessageSize = 65536;
                binding.MaxBufferSize = 65536;
                
                this.channelFactory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
                
                // Note: WCF automatically uses the current Windows identity when ClientCredentialType is Windows
                // We don't need to explicitly set credentials - doing so can cause "login failed" errors
                
                this.channel = this.channelFactory.CreateChannel();
                
                // Open the channel (same as connection test tool)
                ICommunicationObject commObj = this.channel as ICommunicationObject;
                if (commObj != null)
                {
                    // Check state before opening (same as connection test tool)
                    if (commObj.State == CommunicationState.Created)
                    {
                        commObj.Open();
                    }
                    else if (commObj.State != CommunicationState.Opened)
                    {
                        throw new InvalidOperationException(string.Format("通道状态异常: {0}", commObj.State));
                    }
                }
            }
            catch (MessageSecurityException ex)
            {
                // Don't show blocking dialog in SetupChannel - let LoadRequests handle it
                // This allows user to access settings even if connection fails
                // Just store the exception for later handling
                throw new InvalidOperationException(
                    string.Format("无法连接到 Make Me Admin 服务 (服务器地址: {0})。安全协议错误: {1}", 
                        Settings.ServerAddress, ex.Message), 
                    ex);
            }
            catch (SecurityNegotiationException ex)
            {
                // Don't show blocking dialog in SetupChannel - let LoadRequests handle it
                // This allows user to access settings even if connection fails
                throw new InvalidOperationException(
                    string.Format("无法连接到 Make Me Admin 服务 (服务器地址: {0})。安全协商错误: {1}", 
                        Settings.ServerAddress, ex.Message), 
                    ex);
            }
            catch (EndpointNotFoundException ex)
            {
                // Don't show blocking dialog in SetupChannel - let LoadRequests handle it
                // This allows user to access settings even if connection fails
                throw new InvalidOperationException(
                    string.Format("无法连接到 Make Me Admin 服务 (服务器地址: {0})。端点未找到: {1}", 
                        Settings.ServerAddress, ex.Message), 
                    ex);
            }
            catch (CommunicationException ex)
            {
                // Don't show blocking dialog in SetupChannel - let LoadRequests handle it
                // This allows user to access settings even if connection fails
                string errorMsg = string.Format("无法连接到 Make Me Admin 服务 (服务器地址: {0})。通信错误: {1}", 
                    Settings.ServerAddress, ex.Message);
                throw new InvalidOperationException(errorMsg, ex);
            }
            catch (Exception ex)
            {
                // Don't show blocking dialog in SetupChannel - let LoadRequests handle it
                // This allows user to access settings even if connection fails
                string errorMsg = string.Format("无法连接到 Make Me Admin 服务 (服务器地址: {0})。错误: {1}", 
                    Settings.ServerAddress, ex.Message);
                if (ex.InnerException != null)
                {
                    errorMsg += string.Format(" (内部异常: {0})", ex.InnerException.Message);
                }
                throw new InvalidOperationException(errorMsg, ex);
            }
        }

        /// <summary>
        /// Updates the requests list view with the given requests.
        /// </summary>
        private void UpdateRequestsList(PendingRequest[] requests)
        {
            this.requestsListView.Items.Clear();
            
            // Show/hide empty state
            bool hasRequests = requests != null && requests.Length > 0;
            if (this.emptyStateLabel != null)
            {
                this.emptyStateLabel.Visible = !hasRequests;
            }
            
            if (hasRequests)
            {
                foreach (var request in requests.OrderByDescending(r => r.RequestDateTime))
                {
                    ListViewItem item = new ListViewItem(request.UserName);
                    item.SubItems.Add(request.RequestDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.SubItems.Add(request.RequestedTimeoutMinutes.ToString());
                    item.SubItems.Add(string.IsNullOrEmpty(request.RemoteAddress) ? "本地" : request.RemoteAddress);
                    item.SubItems.Add(request.RequestId);
                    item.Tag = request;
                    item.UseItemStyleForSubItems = true;
                    
                    // Dark mode: Set colors for better visibility
                    item.ForeColor = System.Drawing.Color.FromArgb(241, 241, 241); // Light text
                    item.BackColor = System.Drawing.Color.FromArgb(37, 37, 38); // Dark background
                    
                    // Set font for better readability
                    item.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
                    
                    this.requestsListView.Items.Add(item);
                }
            }

            this.UpdateStatusLabel();
        }

        /// <summary>
        /// Runs detailed connection diagnostics (same logic as connection test tool).
        /// </summary>
        private string RunConnectionDiagnostics()
        {
            System.Text.StringBuilder diagnostics = new System.Text.StringBuilder();
            
            try
            {
                string serverAddress = Settings.ServerAddress;
                bool useSecureMode = Settings.UseSecureMode;
                
                diagnostics.AppendLine("========================================");
                diagnostics.AppendLine("连接诊断报告");
                diagnostics.AppendLine("========================================");
                diagnostics.AppendLine("");
                diagnostics.AppendLine(string.Format("诊断时间: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                diagnostics.AppendLine(string.Format("服务器地址: {0}", serverAddress));
                diagnostics.AppendLine(string.Format("安全模式: {0}", useSecureMode ? "启用 (SecurityMode.Transport + Windows)" : "禁用 (SecurityMode.None)"));
                diagnostics.AppendLine("");
                
                ChannelFactory<IAdminGroup> factory = null;
                IAdminGroup channel = null;
                
                try
                {
                    // Step 1: Construct address
                    diagnostics.AppendLine("[步骤 1] 构造服务地址...");
                    if (string.IsNullOrEmpty(serverAddress))
                    {
                        serverAddress = "localhost";
                    }
                    string tcpAddress = Shared.GetTcpServiceAddress(serverAddress);
                    diagnostics.AppendLine(string.Format("  地址: {0}", tcpAddress));
                    diagnostics.AppendLine("  ✓ 成功");
                    diagnostics.AppendLine("");
                    
                    // Step 2: Create binding
                    diagnostics.AppendLine("[步骤 2] 创建绑定...");
                    NetTcpBinding binding;
                    if (useSecureMode)
                    {
                        binding = new NetTcpBinding(SecurityMode.Transport);
                        binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                        diagnostics.AppendLine("  安全模式: Transport");
                        diagnostics.AppendLine("  客户端凭据类型: Windows");
                    }
                    else
                    {
                        binding = new NetTcpBinding(SecurityMode.None);
                        diagnostics.AppendLine("  安全模式: None (无加密和身份验证)");
                    }
                    diagnostics.AppendLine(string.Format("  MaxReceivedMessageSize: {0}", binding.MaxReceivedMessageSize));
                    diagnostics.AppendLine(string.Format("  MaxBufferSize: {0}", binding.MaxBufferSize));
                    diagnostics.AppendLine(string.Format("  ReceiveTimeout: {0}", binding.ReceiveTimeout));
                    diagnostics.AppendLine(string.Format("  SendTimeout: {0}", binding.SendTimeout));
                    diagnostics.AppendLine("  ✓ 成功");
                    diagnostics.AppendLine("");
                    
                    // Step 3: Create channel factory
                    diagnostics.AppendLine("[步骤 3] 创建通道工厂...");
                    factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
                    diagnostics.AppendLine("  ✓ 成功");
                    diagnostics.AppendLine("");
                    
                    // Step 4: Create channel
                    diagnostics.AppendLine("[步骤 4] 创建通道...");
                    channel = factory.CreateChannel();
                    diagnostics.AppendLine("  ✓ 成功");
                    diagnostics.AppendLine("");
                    
                    // Step 5: Open channel
                    diagnostics.AppendLine("[步骤 5] 打开通道...");
                    ICommunicationObject commObj = channel as ICommunicationObject;
                    if (commObj != null)
                    {
                        diagnostics.AppendLine(string.Format("  通道状态 (打开前): {0}", commObj.State));
                        
                        if (commObj.State == CommunicationState.Created)
                        {
                            commObj.Open();
                            diagnostics.AppendLine(string.Format("  通道状态 (打开后): {0}", commObj.State));
                            diagnostics.AppendLine("  ✓ 成功");
                        }
                        else
                        {
                            throw new InvalidOperationException(string.Format("通道状态异常: {0}", commObj.State));
                        }
                    }
                    diagnostics.AppendLine("");
                    
                    // Step 6: Test service call
                    diagnostics.AppendLine("[步骤 6] 测试服务调用...");
                    try
                    {
                        bool hasPending = channel.HasPendingRequest();
                        diagnostics.AppendLine("  调用 HasPendingRequest() 成功");
                        diagnostics.AppendLine(string.Format("  返回值: {0}", hasPending));
                        diagnostics.AppendLine("  ✓ 服务调用成功");
                    }
                    catch (Exception ex)
                    {
                        diagnostics.AppendLine(string.Format("  ⚠ 服务调用失败: {0}", ex.Message));
                        diagnostics.AppendLine(string.Format("  类型: {0}", ex.GetType().Name));
                        if (ex.InnerException != null)
                        {
                            diagnostics.AppendLine(string.Format("  内部异常: {0}", ex.InnerException.Message));
                        }
                    }
                    diagnostics.AppendLine("");
                    
                    // Success
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("✓ 连接诊断成功！");
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("");
                    diagnostics.AppendLine("连接信息:");
                    diagnostics.AppendLine(string.Format("  服务器地址: {0}", serverAddress));
                    diagnostics.AppendLine(string.Format("  服务地址: {0}", tcpAddress));
                    diagnostics.AppendLine(string.Format("  安全模式: {0}", useSecureMode ? "Transport + Windows" : "None"));
                    diagnostics.AppendLine(string.Format("  通道状态: {0}", commObj?.State ?? CommunicationState.Closed));
                }
                catch (MessageSecurityException ex)
                {
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("✗ 连接诊断失败: MessageSecurityException");
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("");
                    diagnostics.AppendLine(string.Format("错误消息: {0}", ex.Message));
                    diagnostics.AppendLine(string.Format("HResult: 0x{0:X8}", ex.HResult));
                    diagnostics.AppendLine("");
                    if (ex.InnerException != null)
                    {
                        diagnostics.AppendLine("内部异常 1:");
                        diagnostics.AppendLine(string.Format("  类型: {0}", ex.InnerException.GetType().Name));
                        diagnostics.AppendLine(string.Format("  消息: {0}", ex.InnerException.Message));
                        diagnostics.AppendLine("");
                        if (ex.InnerException.InnerException != null)
                        {
                            diagnostics.AppendLine("内部异常 2:");
                            diagnostics.AppendLine(string.Format("  类型: {0}", ex.InnerException.InnerException.GetType().Name));
                            diagnostics.AppendLine(string.Format("  消息: {0}", ex.InnerException.InnerException.Message));
                            diagnostics.AppendLine("");
                        }
                    }
                }
                catch (SecurityNegotiationException ex)
                {
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("✗ 连接诊断失败: SecurityNegotiationException");
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("");
                    diagnostics.AppendLine(string.Format("错误消息: {0}", ex.Message));
                    diagnostics.AppendLine(string.Format("HResult: 0x{0:X8}", ex.HResult));
                    diagnostics.AppendLine("");
                    if (ex.InnerException != null)
                    {
                        diagnostics.AppendLine("内部异常 1:");
                        diagnostics.AppendLine(string.Format("  类型: {0}", ex.InnerException.GetType().Name));
                        diagnostics.AppendLine(string.Format("  消息: {0}", ex.InnerException.Message));
                        diagnostics.AppendLine("");
                    }
                }
                catch (EndpointNotFoundException ex)
                {
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("✗ 连接诊断失败: EndpointNotFoundException");
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("");
                    diagnostics.AppendLine(string.Format("错误消息: {0}", ex.Message));
                    diagnostics.AppendLine(string.Format("HResult: 0x{0:X8}", ex.HResult));
                    diagnostics.AppendLine("");
                }
                catch (CommunicationException ex)
                {
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("✗ 连接诊断失败: CommunicationException");
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("");
                    diagnostics.AppendLine(string.Format("错误消息: {0}", ex.Message));
                    diagnostics.AppendLine(string.Format("HResult: 0x{0:X8}", ex.HResult));
                    diagnostics.AppendLine("");
                    if (ex.InnerException != null)
                    {
                        diagnostics.AppendLine("内部异常 1:");
                        diagnostics.AppendLine(string.Format("  类型: {0}", ex.InnerException.GetType().Name));
                        diagnostics.AppendLine(string.Format("  消息: {0}", ex.InnerException.Message));
                        diagnostics.AppendLine("");
                        if (ex.InnerException.InnerException != null)
                        {
                            diagnostics.AppendLine("内部异常 2:");
                            diagnostics.AppendLine(string.Format("  类型: {0}", ex.InnerException.InnerException.GetType().Name));
                            diagnostics.AppendLine(string.Format("  消息: {0}", ex.InnerException.InnerException.Message));
                            diagnostics.AppendLine("");
                        }
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("✗ 连接诊断失败");
                    diagnostics.AppendLine("========================================");
                    diagnostics.AppendLine("");
                    diagnostics.AppendLine(string.Format("异常类型: {0}", ex.GetType().Name));
                    diagnostics.AppendLine(string.Format("错误消息: {0}", ex.Message));
                    diagnostics.AppendLine(string.Format("HResult: 0x{0:X8}", ex.HResult));
                    diagnostics.AppendLine("");
                    
                    if (ex.InnerException != null)
                    {
                        diagnostics.AppendLine("内部异常 1:");
                        diagnostics.AppendLine(string.Format("  类型: {0}", ex.InnerException.GetType().Name));
                        diagnostics.AppendLine(string.Format("  消息: {0}", ex.InnerException.Message));
                        diagnostics.AppendLine("");
                        
                        if (ex.InnerException.InnerException != null)
                        {
                            diagnostics.AppendLine("内部异常 2:");
                            diagnostics.AppendLine(string.Format("  类型: {0}", ex.InnerException.InnerException.GetType().Name));
                            diagnostics.AppendLine(string.Format("  消息: {0}", ex.InnerException.InnerException.Message));
                            diagnostics.AppendLine("");
                        }
                    }
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
            catch (Exception ex)
            {
                diagnostics.AppendLine(string.Format("诊断过程发生异常: {0}", ex.Message));
                diagnostics.AppendLine(ex.StackTrace);
            }
            
            return diagnostics.ToString();
        }

        /// <summary>
        /// Loads pending requests from the service.
        /// </summary>
        private void LoadRequests()
        {
            // Don't attempt connection if it has already failed
            if (this.connectionFailed)
            {
                return;
            }
            
            try
            {
                // Reset connection failed flag on successful attempt
                this.connectionFailed = false;
                
                // Check if channel is usable, recreate if necessary
                if (!this.IsChannelUsable())
                {
                    try
                    {
                        this.SetupChannel();
                    }
                    catch (MessageSecurityException ex)
                    {
                        // Re-throw security exceptions with better context
                        throw new InvalidOperationException(
                            string.Format("无法连接到 Make Me Admin 服务。\n\n安全协议错误：{0}\n\n请检查客户端和服务器的安全模式配置是否一致。", ex.Message),
                            ex);
                    }
                    catch (SecurityNegotiationException ex)
                    {
                        // Re-throw security negotiation exceptions with better context
                        throw new InvalidOperationException(
                            string.Format("无法连接到 Make Me Admin 服务。\n\n安全协商错误：{0}\n\n请检查身份验证配置。", ex.Message),
                            ex);
                    }
                    catch (EndpointNotFoundException ex)
                    {
                        // Re-throw endpoint not found exceptions with better context
                        throw new InvalidOperationException(
                            string.Format("无法连接到 Make Me Admin 服务。\n\n端点未找到：{0}\n\n请检查服务器地址配置和服务是否正在运行。", ex.Message),
                            ex);
                    }
                    catch (Exception ex)
                    {
                        // Re-throw other exceptions
                        throw new InvalidOperationException(
                            string.Format("无法连接到 Make Me Admin 服务：{0}", ex.Message),
                            ex);
                    }
                }

                // Double check after setup
                if (!this.IsChannelUsable())
                {
                    throw new InvalidOperationException("无法建立与服务端的连接。请检查服务器地址配置和服务是否正在运行。");
                }

                // First verify connection with a simple call (like connection test tool does)
                // This helps ensure the channel is truly ready before making the more complex call
                try
                {
                    bool hasPending = this.channel.HasPendingRequest();
                }
                catch (Exception verifyEx)
                {
                    // If simple call fails, connection is not ready
                    throw new InvalidOperationException(
                        string.Format("连接验证失败：{0}", verifyEx.Message),
                        verifyEx);
                }

                // Call service method with retry logic for connection issues
                PendingRequest[] requests = null;
                try
                {
                    requests = this.channel.GetPendingRequests();
                }
                catch (CommunicationException commEx)
                {
                    // If connection was aborted, try to recreate channel and retry once
                    if (commEx.Message.Contains("套接字连接已中止") || commEx.Message.Contains("socket connection") || commEx.Message.Contains("aborted"))
                    {
                        // Close and recreate channel
                        this.CloseChannel();
                        this.SetupChannel();
                        
                        // Verify connection again
                        if (this.IsChannelUsable())
                        {
                            try
                            {
                                // Verify with simple call first
                                this.channel.HasPendingRequest();
                                
                                // Then retry the complex call
                                requests = this.channel.GetPendingRequests();
                            }
                            catch
                            {
                                throw;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
                
                // Connection successful, reset flag
                this.connectionFailed = false;
                
                // Only update UI if window handle is created
                if (!this.IsHandleCreated)
                {
                    return;
                }
                
                if (this.InvokeRequired)
                {
                    try
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.UpdateRequestsList(requests);
                        }));
                    }
                    catch (InvalidOperationException)
                    {
                        // Window handle was destroyed, ignore
                    }
                }
                else
                {
                    this.UpdateRequestsList(requests);
                }
            }
            catch (Exception ex)
            {
                // Close faulted channel
                this.CloseChannel();
                
                // Mark connection as failed to prevent repeated attempts
                this.connectionFailed = true;

                // Run connection diagnostics automatically
                string diagnostics = "";
                try
                {
                    diagnostics = this.RunConnectionDiagnostics();
                }
                catch
                {
                    // If diagnostics fail, continue with error message
                }

                string errorMessage = "加载请求失败。\n\n";
                
                // Check if it's a connection error (from SetupChannel)
                if (ex.Message.Contains("无法连接到 Make Me Admin 服务"))
                {
                    // Extract the original exception type from inner exception
                    string originalExceptionType = "";
                    if (ex.InnerException != null)
                    {
                        originalExceptionType = ex.InnerException.GetType().Name;
                    }
                    
                    errorMessage = "无法连接到 Make Me Admin 服务。\n\n";
                    
                    if (originalExceptionType == "MessageSecurityException")
                    {
                        errorMessage += "错误类型：安全协议错误 (MessageSecurityException)\n\n";
                        errorMessage += string.Format("错误详情：{0}\n\n", ex.InnerException?.Message ?? ex.Message);
                        
                        // Add HResult for more diagnostic info
                        if (ex.InnerException != null)
                        {
                            errorMessage += string.Format("HResult：0x{0:X8}\n\n", ex.InnerException.HResult);
                            
                            // Add inner exception details if available
                            if (ex.InnerException.InnerException != null)
                            {
                                errorMessage += "内部异常：\n";
                                errorMessage += string.Format("  类型：{0}\n", ex.InnerException.InnerException.GetType().Name);
                                errorMessage += string.Format("  消息：{0}\n", ex.InnerException.InnerException.Message);
                                errorMessage += string.Format("  HResult：0x{0:X8}\n\n", ex.InnerException.InnerException.HResult);
                            }
                        }
                        
                        errorMessage += "当前配置：\n";
                        errorMessage += string.Format("  - 服务器地址：{0}\n", Settings.ServerAddress);
                        errorMessage += string.Format("  - 服务地址：{0}\n", Shared.GetTcpServiceAddress(Settings.ServerAddress));
                        errorMessage += string.Format("  - 安全模式：{0}\n\n", Settings.UseSecureMode ? "启用 (Transport + Windows)" : "禁用 (None)");
                        
                        errorMessage += "可能的原因：\n";
                        errorMessage += "1. 客户端和服务器的安全配置不匹配（最常见）\n";
                        errorMessage += "   - 客户端 UseSecureMode 与服务器端不一致\n";
                        errorMessage += "   - 服务器端服务未重新编译/重新安装\n";
                        errorMessage += "2. 服务器上的服务版本与客户端不兼容\n";
                        errorMessage += "3. Windows 身份验证配置问题\n";
                        errorMessage += "4. 跨域/跨工作组身份验证失败\n\n";
                        
                        errorMessage += "解决方法：\n";
                        errorMessage += "1. 点击下方\"设置\"按钮更改服务器地址\n";
                        errorMessage += "2. 确保服务端和客户端使用相同的安全模式配置\n";
                        errorMessage += "   - 检查注册表中的 UseSecureMode 设置\n";
                        errorMessage += "   - 客户端：" + (Settings.UseSecureMode ? "启用" : "禁用") + "\n";
                        errorMessage += "   - 服务器端：请检查服务器注册表\n";
                        errorMessage += "3. 使用连接测试工具验证连接\n";
                        errorMessage += "4. 重新编译并重新安装服务端服务\n";
                        errorMessage += "5. 如果跨域/跨工作组，考虑使用 SecurityMode.None（不安全）";
                    }
                    else if (originalExceptionType == "SecurityNegotiationException")
                    {
                        errorMessage += "错误类型：安全协商错误 (SecurityNegotiationException)\n\n";
                        errorMessage += string.Format("错误详情：{0}\n\n", ex.InnerException?.Message ?? ex.Message);
                        
                        // Add HResult for more diagnostic info
                        if (ex.InnerException != null)
                        {
                            errorMessage += string.Format("HResult：0x{0:X8}\n\n", ex.InnerException.HResult);
                            
                            // Add inner exception details if available
                            if (ex.InnerException.InnerException != null)
                            {
                                errorMessage += "内部异常：\n";
                                errorMessage += string.Format("  类型：{0}\n", ex.InnerException.InnerException.GetType().Name);
                                errorMessage += string.Format("  消息：{0}\n", ex.InnerException.InnerException.Message);
                                errorMessage += string.Format("  HResult：0x{0:X8}\n\n", ex.InnerException.InnerException.HResult);
                            }
                        }
                        
                        errorMessage += "当前配置：\n";
                        errorMessage += string.Format("  - 服务器地址：{0}\n", Settings.ServerAddress);
                        errorMessage += string.Format("  - 服务地址：{0}\n", Shared.GetTcpServiceAddress(Settings.ServerAddress));
                        errorMessage += string.Format("  - 安全模式：{0}\n\n", Settings.UseSecureMode ? "启用 (Transport + Windows)" : "禁用 (None)");
                        
                        errorMessage += "可能的原因：\n";
                        errorMessage += "1. 用户端和服务器不在同一域中（跨域身份验证失败）\n";
                        errorMessage += "2. 服务器拒绝当前用户的 Windows 凭据\n";
                        errorMessage += "3. 服务运行账户配置问题\n";
                        errorMessage += "4. Kerberos/SPN 配置问题\n\n";
                        
                        errorMessage += "解决方法：\n";
                        errorMessage += "1. 点击下方\"设置\"按钮更改服务器地址\n";
                        errorMessage += "2. 检查用户端和服务器是否在同一域中\n";
                        errorMessage += "3. 如果跨域，需要配置 Kerberos/SPN（需要域管理员权限）\n";
                        errorMessage += "4. 或使用 SecurityMode.None（不安全）\n";
                        errorMessage += "5. 使用连接测试工具验证连接";
                    }
                    else if (originalExceptionType == "EndpointNotFoundException")
                    {
                        errorMessage += "错误类型：端点未找到 (EndpointNotFoundException)\n\n";
                        errorMessage += string.Format("错误详情：{0}\n\n", ex.InnerException?.Message ?? ex.Message);
                        
                        // Add HResult for more diagnostic info
                        if (ex.InnerException != null)
                        {
                            errorMessage += string.Format("HResult：0x{0:X8}\n\n", ex.InnerException.HResult);
                        }
                        
                        errorMessage += "当前配置：\n";
                        errorMessage += string.Format("  - 服务器地址：{0}\n", Settings.ServerAddress);
                        errorMessage += string.Format("  - 服务地址：{0}\n\n", Shared.GetTcpServiceAddress(Settings.ServerAddress));
                        
                        errorMessage += "可能的原因：\n";
                        errorMessage += "1. 服务器地址配置错误\n";
                        errorMessage += "2. 服务器上的 Make Me Admin 服务未运行\n";
                        errorMessage += "3. 防火墙阻止了连接\n";
                        errorMessage += "4. 网络连接问题\n";
                        errorMessage += "5. 服务监听端口被占用\n\n";
                        
                        errorMessage += "解决方法：\n";
                        errorMessage += "1. 点击下方\"设置\"按钮更改服务器地址\n";
                        errorMessage += "2. 确认服务器上的服务正在运行（检查服务状态）\n";
                        errorMessage += "3. 测试网络连接（ping 服务器地址）\n";
                        errorMessage += "4. 检查防火墙设置（确保端口 808 或 NetTcp 端口开放）\n";
                        errorMessage += "5. 使用连接测试工具验证连接";
                    }
                    else
                    {
                        errorMessage += string.Format("错误详情：{0}", ex.Message);
                        if (ex.InnerException != null)
                        {
                            errorMessage += string.Format("\n\n内部异常：{0}", ex.InnerException.Message);
                        }
                        errorMessage += "\n\n提示：点击下方\"设置\"按钮可以更改服务器地址";
                    }
                }
                else if (ex.Message.Contains("ContractFilter") || ex.Message.Contains("EndpointDispatcher"))
                {
                    errorMessage += "错误类型：服务版本不匹配\n\n";
                    errorMessage += string.Format("错误详情：{0}\n\n", ex.Message);
                    
                    // Add inner exception details if available
                    if (ex.InnerException != null)
                    {
                        errorMessage += "内部异常：\n";
                        errorMessage += string.Format("  类型：{0}\n", ex.InnerException.GetType().Name);
                        errorMessage += string.Format("  消息：{0}\n\n", ex.InnerException.Message);
                    }
                    
                    errorMessage += "当前配置：\n";
                    errorMessage += string.Format("  - 服务器地址：{0}\n", Settings.ServerAddress);
                    errorMessage += string.Format("  - 服务地址：{0}\n\n", Shared.GetTcpServiceAddress(Settings.ServerAddress));
                    
                    errorMessage += "错误原因：\n";
                    errorMessage += "服务端缺少新的接口方法或接口定义不匹配。\n";
                    errorMessage += "这通常发生在客户端和服务端版本不一致时。\n\n";
                    
                    errorMessage += "解决方法：\n";
                    errorMessage += "1. 停止 Make Me Admin 服务\n";
                    errorMessage += "2. 重新编译整个解决方案（特别是 Service 项目）\n";
                    errorMessage += "3. 重新安装并启动服务\n";
                    errorMessage += "4. 确保客户端和服务端使用相同版本的代码\n\n";
                    errorMessage += "详细步骤请查看：服务端重新安装指南.md";
                }
                else if (ex.Message.Contains("出错") || ex.Message.Contains("faulted") || ex.Message.Contains("Faulted"))
                {
                    errorMessage += "错误类型：通信通道故障\n\n";
                    errorMessage += string.Format("错误详情：{0}\n\n", ex.Message);
                    
                    // Add inner exception details if available
                    if (ex.InnerException != null)
                    {
                        errorMessage += "内部异常：\n";
                        errorMessage += string.Format("  类型：{0}\n", ex.InnerException.GetType().Name);
                        errorMessage += string.Format("  消息：{0}\n\n", ex.InnerException.Message);
                        
                        if (ex.InnerException.InnerException != null)
                        {
                            errorMessage += "内部异常 2：\n";
                            errorMessage += string.Format("  类型：{0}\n", ex.InnerException.InnerException.GetType().Name);
                            errorMessage += string.Format("  消息：{0}\n\n", ex.InnerException.InnerException.Message);
                        }
                    }
                    
                    errorMessage += "当前配置：\n";
                    errorMessage += string.Format("  - 服务器地址：{0}\n", Settings.ServerAddress);
                    errorMessage += string.Format("  - 服务地址：{0}\n", Shared.GetTcpServiceAddress(Settings.ServerAddress));
                    errorMessage += string.Format("  - 安全模式：{0}\n\n", Settings.UseSecureMode ? "启用 (Transport + Windows)" : "禁用 (None)");
                    
                    errorMessage += "可能的原因：\n";
                    errorMessage += "1. 网络连接中断\n";
                    errorMessage += "2. 服务器服务已停止或重启\n";
                    errorMessage += "3. 服务器地址配置错误\n";
                    errorMessage += "4. 防火墙阻止了连接\n";
                    errorMessage += "5. 客户端和服务器的安全配置不匹配\n\n";
                    
                    errorMessage += "解决方法：\n";
                    errorMessage += "1. 点击下方\"设置\"按钮检查并更改服务器地址\n";
                    errorMessage += "2. 确认服务器上的 Make Me Admin 服务正在运行\n";
                    errorMessage += "3. 检查网络连接是否正常\n";
                    errorMessage += "4. 使用连接测试工具验证连接\n";
                    errorMessage += "5. 检查防火墙设置";
                }
                else
                {
                    errorMessage += string.Format("错误类型：{0}\n\n", ex.GetType().Name);
                    errorMessage += string.Format("错误详情：{0}\n\n", ex.Message);
                    
                    // Add inner exception details if available
                    if (ex.InnerException != null)
                    {
                        errorMessage += "内部异常：\n";
                        errorMessage += string.Format("  类型：{0}\n", ex.InnerException.GetType().Name);
                        errorMessage += string.Format("  消息：{0}\n", ex.InnerException.Message);
                        errorMessage += string.Format("  HResult：0x{0:X8}\n\n", ex.InnerException.HResult);
                        
                        if (ex.InnerException.InnerException != null)
                        {
                            errorMessage += "内部异常 2：\n";
                            errorMessage += string.Format("  类型：{0}\n", ex.InnerException.InnerException.GetType().Name);
                            errorMessage += string.Format("  消息：{0}\n", ex.InnerException.InnerException.Message);
                            errorMessage += string.Format("  HResult：0x{0:X8}\n\n", ex.InnerException.InnerException.HResult);
                        }
                    }
                    
                    errorMessage += "当前配置：\n";
                    errorMessage += string.Format("  - 服务器地址：{0}\n", Settings.ServerAddress);
                    errorMessage += string.Format("  - 服务地址：{0}\n", Shared.GetTcpServiceAddress(Settings.ServerAddress));
                    errorMessage += string.Format("  - 安全模式：{0}\n\n", Settings.UseSecureMode ? "启用 (Transport + Windows)" : "禁用 (None)");
                    
                    errorMessage += "提示：点击下方\"设置\"按钮可以更改服务器地址";
                }
                
                // Append diagnostics if available
                if (!string.IsNullOrEmpty(diagnostics))
                {
                    errorMessage += "\n\n";
                    errorMessage += "========================================";
                    errorMessage += "\n详细诊断信息：";
                    errorMessage += "\n========================================";
                    errorMessage += "\n";
                    errorMessage += diagnostics;
                }
                
                // Show error dialog with Settings button
                this.ShowErrorDialogWithSettings(errorMessage);
            }
        }

        /// <summary>
        /// Handles the Approve button click event.
        /// </summary>
        private void approveButton_Click(object sender, EventArgs e)
        {
            // Get selected items (using row selection)
            var selectedItems = this.requestsListView.SelectedItems.Cast<ListViewItem>().ToList();
            
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("请选择要批准的请求（点击行即可选择）\n\n提示：按住 Ctrl 键可多选，按住 Shift 键可连续选择", 
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get custom timeout from UI
            int? customTimeout = null;
            if (this.timeoutNumericUpDown.Value > 0)
            {
                customTimeout = (int)this.timeoutNumericUpDown.Value;
            }

            // Build confirmation message
            string confirmMessage;
            string timeoutInfo = customTimeout.HasValue 
                ? string.Format("\n\n提权时长: {0} 分钟", customTimeout.Value)
                : "";
            
            if (selectedItems.Count == 1)
            {
                PendingRequest request = selectedItems[0].Tag as PendingRequest;
                if (string.IsNullOrEmpty(timeoutInfo) && request != null)
                {
                    timeoutInfo = string.Format("\n\n提权时长: {0} 分钟（使用请求中的默认值）", request.RequestedTimeoutMinutes);
                }
                confirmMessage = string.Format("确定要批准用户 {0} 的管理员权限请求吗？{1}", request?.UserName ?? "未知", timeoutInfo);
            }
            else
            {
                confirmMessage = string.Format("确定要批准选中的 {0} 个管理员权限请求吗？{1}", selectedItems.Count, timeoutInfo);
            }

            DialogResult result = MessageBox.Show(
                confirmMessage,
                "确认批准",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Reset connection failed flag when user manually tries to approve
                    this.connectionFailed = false;
                    
                    // Check if channel is usable, recreate if necessary
                    if (!this.IsChannelUsable())
                    {
                        this.SetupChannel();
                    }

                        if (!this.IsChannelUsable())
                        {
                            throw new InvalidOperationException("无法建立与服务端的连接");
                        }

                        // Process all selected requests
                        int successCount = 0;
                        int failCount = 0;
                        string failMessages = "";

                        // Use customTimeout variable already declared above

                        foreach (ListViewItem item in selectedItems)
                        {
                            PendingRequest request = item.Tag as PendingRequest;
                            if (request != null)
                            {
                                try
                                {
                                    // Use custom timeout if specified, otherwise use default
                                    if (customTimeout.HasValue)
                                    {
                                        this.channel.ApproveRequestWithTimeout(request.RequestId, customTimeout.Value);
                                    }
                                    else
                                    {
                                        this.channel.ApproveRequest(request.RequestId);
                                    }
                                    successCount++;
                                }
                                catch (Exception ex)
                                {
                                    failCount++;
                                    if (string.IsNullOrEmpty(failMessages))
                                    {
                                        failMessages = string.Format("用户 {0}: {1}", request.UserName, ex.Message);
                                    }
                                    else
                                    {
                                        failMessages += string.Format("\n用户 {0}: {1}", request.UserName, ex.Message);
                                    }
                                }
                            }
                        }

                    // Show result message
                    if (failCount == 0)
                    {
                        MessageBox.Show(
                            string.Format("成功批准 {0} 个请求", successCount),
                            "成功",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            string.Format("成功批准 {0} 个请求\n失败 {1} 个请求\n\n失败详情：\n{2}", 
                                successCount, failCount, failMessages),
                            "部分成功",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    this.LoadRequests();
                }
                catch (Exception ex)
                {
                    // Close faulted channel
                    this.CloseChannel();

                    string errorMsg = string.Format("批准请求失败: {0}", ex.Message);
                    if (ex.Message.Contains("出错") || ex.Message.Contains("faulted") || ex.Message.Contains("Faulted"))
                    {
                        errorMsg += "\n\n通信通道已故障，请重试。";
                    }
                    MessageBox.Show(
                        errorMsg,
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Handles the Reject button click event.
        /// </summary>
        private void rejectButton_Click(object sender, EventArgs e)
        {
            // Get selected items (using row selection)
            var selectedItems = this.requestsListView.SelectedItems.Cast<ListViewItem>().ToList();
            
            if (selectedItems.Count == 0)
            {
                MessageBox.Show("请选择要拒绝的请求（点击行即可选择）\n\n提示：按住 Ctrl 键可多选，按住 Shift 键可连续选择", 
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build confirmation message
            string confirmMessage;
            if (selectedItems.Count == 1)
            {
                PendingRequest request = selectedItems[0].Tag as PendingRequest;
                confirmMessage = string.Format("确定要拒绝用户 {0} 的管理员权限请求吗？", request?.UserName ?? "未知");
            }
            else
            {
                confirmMessage = string.Format("确定要拒绝选中的 {0} 个管理员权限请求吗？", selectedItems.Count);
            }

            DialogResult result = MessageBox.Show(
                confirmMessage,
                "确认拒绝",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    // Reset connection failed flag when user manually tries to reject
                    this.connectionFailed = false;
                    
                    // Check if channel is usable, recreate if necessary
                    if (!this.IsChannelUsable())
                    {
                        this.SetupChannel();
                    }

                        if (!this.IsChannelUsable())
                        {
                            throw new InvalidOperationException("无法建立与服务端的连接");
                        }

                        // Process all selected requests
                        int successCount = 0;
                        int failCount = 0;
                        string failMessages = "";

                        foreach (ListViewItem item in selectedItems)
                        {
                            PendingRequest request = item.Tag as PendingRequest;
                            if (request != null)
                            {
                                try
                                {
                                    this.channel.RejectRequest(request.RequestId);
                                    successCount++;
                                }
                                catch (Exception ex)
                                {
                                    failCount++;
                                    if (string.IsNullOrEmpty(failMessages))
                                    {
                                        failMessages = string.Format("用户 {0}: {1}", request.UserName, ex.Message);
                                    }
                                    else
                                    {
                                        failMessages += string.Format("\n用户 {0}: {1}", request.UserName, ex.Message);
                                    }
                                }
                            }
                        }

                    // Show result message
                    if (failCount == 0)
                    {
                        MessageBox.Show(
                            string.Format("成功拒绝 {0} 个请求", successCount),
                            "成功",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            string.Format("成功拒绝 {0} 个请求\n失败 {1} 个请求\n\n失败详情：\n{2}", 
                                successCount, failCount, failMessages),
                            "部分成功",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    this.LoadRequests();
                }
                catch (Exception ex)
                {
                    // Close faulted channel
                    this.CloseChannel();

                    string errorMsg = string.Format("拒绝请求失败: {0}", ex.Message);
                    if (ex.Message.Contains("出错") || ex.Message.Contains("faulted") || ex.Message.Contains("Faulted"))
                    {
                        errorMsg += "\n\n通信通道已故障，请重试。";
                    }
                    MessageBox.Show(
                        errorMsg,
                        "错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Handles the Refresh button click event.
        /// </summary>
        private void refreshButton_Click(object sender, EventArgs e)
        {
            // Reset connection failed flag when user manually clicks refresh
            this.connectionFailed = false;
            this.LoadRequests();
        }

        /// <summary>
        /// Handles the Select All button click event.
        /// </summary>
        private void selectAllButton_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in this.requestsListView.Items)
            {
                item.Selected = true;
            }
            this.UpdateStatusLabel();
        }

        /// <summary>
        /// Handles the Deselect All button click event.
        /// </summary>
        private void deselectAllButton_Click(object sender, EventArgs e)
        {
            this.requestsListView.SelectedItems.Clear();
            this.UpdateStatusLabel();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event to update status label.
        /// </summary>
        private void requestsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.UpdateStatusLabel();
            
            // Update timeout input based on selected request
            if (this.requestsListView.SelectedItems.Count == 1)
            {
                PendingRequest request = this.requestsListView.SelectedItems[0].Tag as PendingRequest;
                if (request != null)
                {
                    // Show the requested timeout as a hint (but don't change the value if user has customized it)
                    // Only set if current value is 0 (default)
                    if (this.timeoutNumericUpDown.Value == 0)
                    {
                        this.timeoutNumericUpDown.Value = request.RequestedTimeoutMinutes;
                    }
                }
            }
            else if (this.requestsListView.SelectedItems.Count == 0)
            {
                // Reset to 0 when no selection
                this.timeoutNumericUpDown.Value = 0;
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts in the ListView.
        /// </summary>
        private void requestsListView_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+A: Select all
            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true;
                this.selectAllButton_Click(sender, e);
            }
        }

        /// <summary>
        /// Handles form-level keyboard shortcuts.
        /// </summary>
        private void AdminApprovalForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+A: Select all (when ListView has focus)
            if (e.Control && e.KeyCode == Keys.A && this.requestsListView.Focused)
            {
                e.Handled = true;
                this.selectAllButton_Click(sender, e);
            }
            // F5: Refresh
            else if (e.KeyCode == Keys.F5)
            {
                e.Handled = true;
                this.refreshButton_Click(sender, e);
            }
            // Enter: Approve (when ListView has focus and items are selected)
            else if (e.KeyCode == Keys.Enter && this.requestsListView.Focused && this.requestsListView.SelectedItems.Count > 0)
            {
                e.Handled = true;
                this.approveButton_Click(sender, e);
            }
            // Delete: Reject (when ListView has focus and items are selected)
            else if (e.KeyCode == Keys.Delete && this.requestsListView.Focused && this.requestsListView.SelectedItems.Count > 0)
            {
                e.Handled = true;
                this.rejectButton_Click(sender, e);
            }
        }

        /// <summary>
        /// Updates the status label with request count and selected count.
        /// </summary>
        private void UpdateStatusLabel()
        {
            int totalCount = this.requestsListView.Items.Count;
            int selectedCount = this.requestsListView.SelectedItems.Count;
            
            if (selectedCount > 0)
            {
                this.statusLabel.Text = string.Format("📊 待审批: {0}  |  ✅ 已选中: {1}", totalCount, selectedCount);
                this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(100, 181, 246); // Light blue for active selection (dark mode)
            }
            else
            {
                this.statusLabel.Text = string.Format("📊 待审批请求: {0}", totalCount);
                this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(241, 241, 241); // Light gray for normal state (dark mode)
            }
        }

        /// <summary>
        /// Handles the form closing event.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (this.refreshTimer != null)
            {
                this.refreshTimer.Stop();
                this.refreshTimer.Dispose();
            }

            // Close channel properly
            this.CloseChannel();

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Shows an error dialog with a Settings button.
        /// </summary>
        private void ShowErrorDialogWithSettings(string errorMessage)
        {
            if (!this.IsHandleCreated)
            {
                return;
            }
            
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => ShowErrorDialogWithSettings(errorMessage)));
                return;
            }
            
            // Create custom dialog with Settings button
            Form errorDialog = new Form();
            errorDialog.Text = "连接错误";
            errorDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            errorDialog.MaximizeBox = false;
            errorDialog.MinimizeBox = false;
            errorDialog.StartPosition = FormStartPosition.CenterParent;
            errorDialog.Width = 700;
            errorDialog.Height = 600;
            
            // Use TextBox for scrollable content
            TextBox messageTextBox = new TextBox();
            messageTextBox.Text = errorMessage;
            messageTextBox.Multiline = true;
            messageTextBox.ReadOnly = true;
            messageTextBox.ScrollBars = ScrollBars.Both;
            messageTextBox.Width = 680;
            messageTextBox.Height = 520;
            messageTextBox.Location = new System.Drawing.Point(10, 10);
            messageTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            messageTextBox.Font = new System.Drawing.Font("Consolas", 9F);
            
            Button settingsButton = new Button();
            settingsButton.Text = "设置";
            settingsButton.DialogResult = DialogResult.OK;
            settingsButton.Location = new System.Drawing.Point(250, 540);
            settingsButton.Size = new System.Drawing.Size(100, 30);
            settingsButton.Click += (s, e) =>
            {
                errorDialog.DialogResult = DialogResult.Yes; // Use Yes to indicate Settings was clicked
                errorDialog.Close();
            };
            
            Button okButton = new Button();
            okButton.Text = "确定";
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new System.Drawing.Point(370, 540);
            okButton.Size = new System.Drawing.Size(75, 30);
            
            errorDialog.Controls.Add(messageTextBox);
            errorDialog.Controls.Add(settingsButton);
            errorDialog.Controls.Add(okButton);
            errorDialog.AcceptButton = okButton;
            
            DialogResult result = errorDialog.ShowDialog(this);
            
            // If Settings button was clicked, open settings form
            if (result == DialogResult.Yes)
            {
                this.OpenSettingsAndRetry();
            }
        }
        
        /// <summary>
        /// Opens settings form and retries connection.
        /// </summary>
        private void OpenSettingsAndRetry()
        {
            using (ServerSettingsForm settingsForm = new ServerSettingsForm())
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    // Reset connection failed flag
                    this.connectionFailed = false;
                    
                    // Reconnect with new server address
                    this.CloseChannel();
                    try
                    {
                        this.SetupChannel();
                        this.LoadRequests();
                        
                        MessageBox.Show(
                            "服务器设置已保存并已重新连接。",
                            "设置已保存",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch
                    {
                        // If connection still fails, show error again
                        this.statusLabel.Text = "连接失败 - 请点击\"设置\"更改服务器地址";
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the Settings button.
        /// </summary>
        private void settingsButton_Click(object sender, EventArgs e)
        {
            this.OpenSettingsAndRetry();
        }
    }
}
