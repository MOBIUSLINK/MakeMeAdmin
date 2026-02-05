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
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.ServiceModel;
    using System.Windows.Forms;

    /// <summary>
    /// This form allows the user to submit a request for administrator-level rights.
    /// </summary>
    internal partial class SubmitRequestForm : Form
    {
        /// <summary>
        /// Whether the user is a direct member of the Administrator's group.
        /// </summary>
        /// <remarks>
        /// This is stored in a variable because it is a rather expensive operation to check.
        /// </remarks>
        private bool userIsDirectAdmin = false;

        /// <summary>
        /// Whether the user is a member of the Administrator's group, either directly or
        /// via nested group memberships.
        /// </summary>
        /// <remarks>
        /// This is stored in a variable because it is a rather expensive operation to check.
        /// </remarks>
        private bool userIsAdmin = false;

        /// <summary>
        /// Whether the user had administrator rights the last time the check was performed.
        /// </summary>
        /// <remarks>
        /// This is used to determine when the user's administrator status changes.
        /// </remarks>
        private bool userWasAdminOnLastCheck = false;

        /// <summary>
        /// Whether the user had a pending request the last time the check was performed.
        /// </summary>
        /// <remarks>
        /// This is used to determine when a pending request is approved or rejected.
        /// </remarks>
        private bool userHadPendingRequestOnLastCheck = false;

        /// <summary>
        /// Timer to notify users when their administrator rights expire or request status changes.
        /// </summary>
        private System.Timers.Timer notifyIconTimer;

        /// <summary>
        /// Duplex channel factory for real-time notifications.
        /// </summary>
        private DuplexChannelFactory<IAdminGroupDuplex> duplexChannelFactory = null;

        /// <summary>
        /// Duplex channel for real-time notifications.
        /// </summary>
        private IAdminGroupDuplex duplexChannel = null;

        /// <summary>
        /// Callback instance for receiving notifications.
        /// </summary>
        private RequestStatusCallback callbackInstance = null;

        // Windows API for dark title bar
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private int intValue = 1; // 1 = enable dark mode, 0 = disable

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

        private bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && (build == -1 || Environment.OSVersion.Version.Build >= build);
        }

        /// <summary>
        /// Creates a duplex WCF channel for real-time notifications.
        /// </summary>
        /// <returns>A duplex channel factory and channel tuple. Caller is responsible for closing the factory.</returns>
        private Tuple<DuplexChannelFactory<IAdminGroupDuplex>, IAdminGroupDuplex> CreateDuplexChannel()
        {
            string serverAddress = Settings.ServerAddress;
            string tcpAddress = Shared.GetTcpServiceAddress(serverAddress);
            
            ClientLogger.Info("WCF", "Creating duplex TCP channel", 
                string.Format("Server: {0}, Address: {1}, SecureMode: {2}", 
                serverAddress, tcpAddress, Settings.UseSecureMode));
            
            NetTcpBinding binding;
            if (Settings.UseSecureMode)
            {
                binding = new NetTcpBinding(SecurityMode.Transport);
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
            }
            else
            {
                binding = new NetTcpBinding(SecurityMode.None);
            }
            
            // Note: NetTcpBinding supports duplex communication by default when using DuplexChannelFactory
            // No need to set a Duplex property - it's implicit in the channel factory type
            
            // Set timeouts
            binding.ReceiveTimeout = TimeSpan.FromMinutes(10);
            binding.SendTimeout = TimeSpan.FromMinutes(10);
            binding.OpenTimeout = TimeSpan.FromMinutes(1);
            binding.CloseTimeout = TimeSpan.FromMinutes(1);
            
            // Set message size limits
            binding.MaxReceivedMessageSize = 65536;
            binding.MaxBufferSize = 65536;
            
            // Create callback instance
            if (this.callbackInstance == null)
            {
                this.callbackInstance = new RequestStatusCallback(this);
            }
            
            // Create duplex channel factory
            InstanceContext callbackContext = new InstanceContext(this.callbackInstance);
            DuplexChannelFactory<IAdminGroupDuplex> factory = new DuplexChannelFactory<IAdminGroupDuplex>(callbackContext, binding, tcpAddress);
            
            IAdminGroupDuplex channel = factory.CreateChannel();
            
            // Open the channel
            ICommunicationObject commObj = channel as ICommunicationObject;
            if (commObj != null)
            {
                try
                {
                    if (commObj.State == CommunicationState.Created)
                    {
                        commObj.Open();
                        ClientLogger.Info("WCF", "Duplex channel opened successfully", string.Format("State: {0}", commObj.State));
                    }
                }
                catch (Exception ex)
                {
                    ClientLogger.Error("WCF", "Failed to open duplex channel", ex.ToString());
                    factory.Abort();
                    throw;
                }
            }
            
            return new Tuple<DuplexChannelFactory<IAdminGroupDuplex>, IAdminGroupDuplex>(factory, channel);
        }

        /// <summary>
        /// Registers for real-time notifications using duplex communication.
        /// </summary>
        private void RegisterForNotifications()
        {
            try
            {
                // Close existing channel if any
                this.UnregisterForNotifications();

                // Create duplex channel
                var duplexTuple = this.CreateDuplexChannel();
                this.duplexChannelFactory = duplexTuple.Item1;
                this.duplexChannel = duplexTuple.Item2;

                // Register for notifications
                this.duplexChannel.RegisterForNotifications();

                ClientLogger.Info("Application", "Registered for real-time notifications", "");
            }
            catch (Exception ex)
            {
                ClientLogger.Warning("Application", "Failed to register for notifications, will use polling fallback", ex.Message);
                
                // Clean up on failure
                this.UnregisterForNotifications();
            }
        }

        /// <summary>
        /// Unregisters from real-time notifications.
        /// </summary>
        private void UnregisterForNotifications()
        {
            try
            {
                if (this.duplexChannel != null)
                {
                    try
                    {
                        this.duplexChannel.UnregisterForNotifications();
                    }
                    catch { }
                    
                    ICommunicationObject commObj = this.duplexChannel as ICommunicationObject;
                    if (commObj != null)
                    {
                        try
                        {
                            if (commObj.State == CommunicationState.Opened)
                            {
                                commObj.Close();
                            }
                        }
                        catch
                        {
                            commObj.Abort();
                        }
                    }
                    
                    this.duplexChannel = null;
                }

                if (this.duplexChannelFactory != null)
                {
                    try
                    {
                        this.duplexChannelFactory.Close();
                    }
                    catch
                    {
                        this.duplexChannelFactory.Abort();
                    }
                    this.duplexChannelFactory = null;
                }
            }
            catch (Exception ex)
            {
                ClientLogger.Warning("Application", "Error unregistering from notifications", ex.Message);
            }
        }

        /// <summary>
        /// Creates a WCF channel to connect to the service using TCP.
        /// </summary>
        /// <returns>A channel factory and channel tuple. Caller is responsible for closing the factory.</returns>
        private Tuple<ChannelFactory<IAdminGroup>, IAdminGroup> CreateTcpChannel()
        {
            string serverAddress = Settings.ServerAddress;
            string tcpAddress = Shared.GetTcpServiceAddress(serverAddress);
            
            ClientLogger.Info("WCF", "Creating TCP channel", 
                string.Format("Server: {0}, Address: {1}, SecureMode: {2}", 
                serverAddress, tcpAddress, Settings.UseSecureMode));
            
            NetTcpBinding binding;
            if (Settings.UseSecureMode)
            {
                // Secure mode: Use Windows authentication (for same-domain scenarios)
                binding = new NetTcpBinding(SecurityMode.Transport);
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                ClientLogger.Debug("WCF", "Using secure mode", "SecurityMode.Transport + Windows authentication");
            }
            else
            {
                // Insecure mode: No authentication (for cross-environment scenarios: domain <-> workgroup)
                // WARNING: This has no encryption or authentication - use only in trusted networks!
                binding = new NetTcpBinding(SecurityMode.None);
                ClientLogger.Warning("WCF", "Using insecure mode", "SecurityMode.None - no encryption or authentication");
            }
            
            ChannelFactory<IAdminGroup> factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
            
            // Note: WCF automatically uses the current Windows identity when ClientCredentialType is Windows
            // We don't need to explicitly set credentials - doing so can cause "login failed" errors
            
            IAdminGroup channel = factory.CreateChannel();
            
            // Open the channel to ensure it's ready for communication
            ICommunicationObject commObj = channel as ICommunicationObject;
            if (commObj != null)
            {
                try
                {
                    if (commObj.State == CommunicationState.Created)
                    {
                        ClientLogger.Debug("WCF", "Opening channel", "Channel state: Created");
                        commObj.Open();
                        ClientLogger.Info("WCF", "Channel opened successfully", string.Format("State: {0}", commObj.State));
                    }
                    else if (commObj.State == CommunicationState.Faulted)
                    {
                        // Channel is faulted, abort and recreate
                        ClientLogger.Warning("WCF", "Channel is faulted, recreating", string.Format("State: {0}", commObj.State));
                        commObj.Abort();
                        factory.Abort();
                        factory.Close();
                        
                        // Recreate
                        factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);
                        channel = factory.CreateChannel();
                        commObj = channel as ICommunicationObject;
                        if (commObj != null)
                        {
                            commObj.Open();
                            ClientLogger.Info("WCF", "Channel recreated and opened", string.Format("State: {0}", commObj.State));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ClientLogger.Error("WCF", "Failed to open channel", ex.ToString());
                    // If opening fails, abort the factory
                    if (factory != null)
                    {
                        try
                        {
                            factory.Abort();
                        }
                        catch { }
                    }
                    throw;
                }
            }
            
            return Tuple.Create(factory, channel);
        }

        /// <summary>
        /// Initializes a new instance of the SubmitRequestForm class.
        /// </summary>
        public SubmitRequestForm()
        {
            this.InitializeComponent();

            this.Icon = Properties.Resources.SecurityLock;
            this.notifyIcon.Icon = Properties.Resources.SecurityLock;

            // Configure ToolTip - Apple style: subtle and elegant
            this.toolTip.IsBalloon = false;
            this.toolTip.AutomaticDelay = 600;
            this.toolTip.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.toolTip.ForeColor = System.Drawing.Color.White;

            // Initialize bottom status bar (only status display)
            if (this.appStatus != null)
            {
                this.appStatus.Text = Properties.Resources.ApplicationIsReady;
            }

            // Rounded corners are now drawn using Paint events with anti-aliasing

            this.SetFormText();

            // Log application startup
            ClientLogger.Info("Application", "Application started", 
                string.Format("User: {0}, Server: {1}, SecureMode: {2}", 
                System.Security.Principal.WindowsIdentity.GetCurrent().Name, 
                Settings.ServerAddress, 
                Settings.UseSecureMode));

            // Configure the notification timer (used as fallback if callback fails)
            this.notifyIconTimer = new System.Timers.Timer()
            {
                Interval = 5000  // 5 seconds - longer interval since we use callbacks for real-time updates
            };
            this.notifyIconTimer.AutoReset = true;
            this.notifyIconTimer.Elapsed += NotifyIconTimerElapsed;

            // Handle form closing to clean up duplex channel
            this.FormClosing += SubmitRequestForm_FormClosing;
        }

        /// <summary>
        /// Handles the FormClosing event to clean up resources.
        /// Triggers an immediate expiration check on the local service so that privilege
        /// revocation does not depend on the user app staying open (security fix).
        /// </summary>
        private void SubmitRequestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Trigger one immediate expiration check so expired users are removed even after app exit
            try
            {
                string namedPipeAddress = Shared.NamedPipeServiceBaseAddress;
                NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport);
                ChannelFactory<IAdminGroup> localFactory = new ChannelFactory<IAdminGroup>(binding, namedPipeAddress);
                IAdminGroup localChannel = localFactory.CreateChannel();
                ICommunicationObject commObj = localChannel as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Created)
                    commObj.Open();
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    localChannel.RunExpirationCheckNow();
                    try { commObj.Close(); } catch { }
                }
                try { localFactory.Close(); } catch { }
            }
            catch (Exception ex)
            {
                ClientLogger.Debug("Application", "RunExpirationCheckNow on exit failed (non-fatal)", ex.Message);
            }

            // Unregister from notifications and close duplex channel
            this.UnregisterForNotifications();
        }

        /// <summary>
        /// Handles request approval notification from server.
        /// </summary>
        /// <param name="requestId">The unique identifier of the approved request.</param>
        public void HandleRequestApproved(string requestId)
        {
            try
            {
                ClientLogger.Info("Application", "Handling request approval", 
                    string.Format("Request ID: {0}", requestId));

                // Stop polling timer (we got notification via callback)
                this.notifyIconTimer.Stop();
                this.userHadPendingRequestOnLastCheck = false;

                // Get approved timeout from server (same as polling path, so we don't depend on HKLM)
                int? approvedTimeout = null;
                try
                {
                    var channelTuple = this.CreateTcpChannel();
                    if (channelTuple != null)
                    {
                        approvedTimeout = channelTuple.Item2.GetApprovedTimeout();
                        try { (channelTuple.Item2 as ICommunicationObject)?.Close(); } catch { }
                        try { channelTuple.Item1?.Close(); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    ClientLogger.Warning("Application", "HandleRequestApproved: failed to get approved timeout", ex.Message);
                }

                // Perform local privilege elevation with approved timeout
                this.PerformLocalPrivilegeElevation(approvedTimeout);
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Application", "Error handling request approval", ex.ToString());
            }
        }

        /// <summary>
        /// Handles request rejection notification from server.
        /// </summary>
        /// <param name="requestId">The unique identifier of the rejected request.</param>
        public void HandleRequestRejected(string requestId)
        {
            try
            {
                ClientLogger.Info("Application", "Handling request rejection", 
                    string.Format("Request ID: {0}", requestId));

                // Stop polling timer
                this.notifyIconTimer.Stop();
                this.userHadPendingRequestOnLastCheck = false;

                // Update administrator status
                this.UpdateUserAdministratorStatus();

                // Check if user is currently admin (may have been approved before)
                // If yes, remove admin rights as the request was rejected
                if (this.userIsAdmin || this.userIsDirectAdmin)
                {
                    // User has admin rights but request was rejected - remove admin rights
                    this.PerformLocalPrivilegeRemoval("请求已被拒绝，正在移除管理员权限...");
                }
                else
                {
                    // User doesn't have admin rights - just update UI
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.UpdateStatusDisplay("请求已被拒绝");
                            this.addMeButton.Enabled = true;
                            this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                            this.StopCountdown();
                            notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                "您的管理员权限请求已被拒绝", ToolTipIcon.Warning);
                        }));
                    }
                    else
                    {
                        this.UpdateStatusDisplay("请求已被拒绝");
                        this.addMeButton.Enabled = true;
                        this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                        this.StopCountdown();
                        notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                            "您的管理员权限请求已被拒绝", ToolTipIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Application", "Error handling request rejection", ex.ToString());
            }
        }


        /// <summary>
        /// Handles the Elapsed event for the notification area icon.
        /// </summary>
        /// <param name="sender">
        /// The timer whose Elapsed event is firing.
        /// </param>
        /// <param name="e">
        /// Data related to the event.
        /// </param>
        void NotifyIconTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // Force refresh of administrator status by checking directly against the local group
            // This ensures we detect when user is added to administrators group even if token hasn't refreshed
            bool previousAdminStatus = this.userIsAdmin;
            this.UpdateUserAdministratorStatus();

            var channelTuple = this.CreateTcpChannel();
            ChannelFactory<IAdminGroup> channelFactory = channelTuple.Item1;
            IAdminGroup channel = channelTuple.Item2;

            try
            {
                // Check if pending request status has changed (approval/rejection detected)
                bool hasPendingRequest = false;
                try
                {
                    hasPendingRequest = channel.HasPendingRequest();
                }
                catch { }

                // If user had a pending request and it's now gone, check if approved or rejected
                if (this.userHadPendingRequestOnLastCheck && !hasPendingRequest)
                {
                    // Request was processed (approved or rejected)
                    this.userHadPendingRequestOnLastCheck = false;
                    
                    if (this.userIsAdmin)
                    {
                        // Request was APPROVED - user is now admin
                        // Get approved timeout from server and pass it to local service (avoids HKLM write which normal users cannot do)
                        int? approvedTimeout = null;
                        try
                        {
                            approvedTimeout = channel.GetApprovedTimeout();
                            if (approvedTimeout.HasValue)
                            {
                                if (approvedTimeout.Value == 0)
                                    ClientLogger.Info("Application", "Server approved timeout: permanent (0 min)", WindowsIdentity.GetCurrent().Name);
                                else
                                    ClientLogger.Info("Application", "Server approved timeout (minutes)", string.Format("{0}", approvedTimeout.Value));
                            }
                            else
                                ClientLogger.Warning("Application", "No approved timeout from server, local service will use registry/default");
                        }
                        catch (Exception ex)
                        {
                            ClientLogger.Warning("Application", "Failed to get approved timeout from server", ex.ToString());
                        }
                        
                        // Perform local privilege elevation and pass the approved timeout so the service does not depend on HKLM
                        this.PerformLocalPrivilegeElevation(approvedTimeout);
                    }
                    else
                    {
                        // Request was REJECTED
                        this.notifyIconTimer.Stop();
                        
                        // Check if user is currently admin (may have been approved before)
                        // If yes, remove admin rights as the request was rejected
                        if (this.userIsAdmin || this.userIsDirectAdmin)
                        {
                            // User has admin rights but request was rejected - remove admin rights
                            this.PerformLocalPrivilegeRemoval("请求已被拒绝，正在移除管理员权限...");
                        }
                        else
                        {
                            // User doesn't have admin rights - just update UI
                            if (this.InvokeRequired)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    this.addMeButton.Enabled = true;
                                    this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                                    this.removeMeButton.Enabled = false;
                                    this.StopCountdown();
                                    // Update status display based on current state
                                    this.UpdateStatusDisplayBasedOnState();
                                    notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                        "您的管理员权限请求已被拒绝", ToolTipIcon.Warning);
                                }));
                            }
                            else
                            {
                                this.addMeButton.Enabled = true;
                                this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                                this.removeMeButton.Enabled = false;
                                this.StopCountdown();
                                // Update status display based on current state
                                this.UpdateStatusDisplayBasedOnState();
                                notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                    "您的管理员权限请求已被拒绝", ToolTipIcon.Warning);
                            }
                        }
                    }
                }
                else if (!this.userHadPendingRequestOnLastCheck && hasPendingRequest)
                {
                    // New request was created
                    this.userHadPendingRequestOnLastCheck = true;
                }
                
                // Check if administrator status has changed (for other reasons, e.g., manual removal)
                if (this.userIsAdmin != this.userWasAdminOnLastCheck)
                {
                    this.userWasAdminOnLastCheck = this.userIsAdmin;

                    if (this.userIsAdmin)
                    {
                        // User was added to administrators group
                        this.userHadPendingRequestOnLastCheck = false;
                        
                        // Update UI on UI thread
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() =>
                            {
                                this.addMeButton.Enabled = false;
                                this.addMeButton.Text = Properties.Resources.UIMessageAlreadyHaveRights;
                                // Enable remove button since user is now admin
                                this.removeMeButton.Enabled = true;
                                this.StartCountdown();
                                // Update status display based on current state (will show expiration time if available)
                                this.UpdateStatusDisplayBasedOnState();
                                notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                    string.Format(Properties.Resources.UIMessageAddedToGroup, LocalAdministratorGroup.LocalAdminGroupName), 
                                    ToolTipIcon.Info);
                            }));
                        }
                        else
                        {
                            this.addMeButton.Enabled = false;
                            this.addMeButton.Text = Properties.Resources.UIMessageAlreadyHaveRights;
                            // Enable remove button since user is now admin
                            this.removeMeButton.Enabled = true;
                            this.StartCountdown();
                            // Update status display based on current state (will show expiration time if available)
                            this.UpdateStatusDisplayBasedOnState();
                            notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                string.Format(Properties.Resources.UIMessageAddedToGroup, LocalAdministratorGroup.LocalAdminGroupName), 
                                ToolTipIcon.Info);
                        }
                        
                        ClientLogger.Info("Application", "User added to administrators group", 
                            string.Format("User: {0}", WindowsIdentity.GetCurrent().Name));
                    }
                    else if (!channel.UserIsInList())
                    {
                        // User was removed from administrators group
                        this.notifyIconTimer.Stop();
                        
                        // Update UI on UI thread
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() =>
                            {
                                // Update button states
                                this.removeMeButton.Enabled = false;
                                // Update status display based on current state
                                this.UpdateStatusDisplayBasedOnState();
                                // Refresh button states using worker
                                if (!buttonStateWorker.IsBusy)
                                {
                                    buttonStateWorker.RunWorkerAsync();
                                }
                                notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                    string.Format(Properties.Resources.UIMessageRemovedFromGroup, LocalAdministratorGroup.LocalAdminGroupName), 
                                    ToolTipIcon.Info);
                            }));
                        }
                        else
                        {
                            // Update button states
                            this.removeMeButton.Enabled = false;
                            // Update status display based on current state
                            this.UpdateStatusDisplayBasedOnState();
                            // Refresh button states using worker
                            if (!buttonStateWorker.IsBusy)
                            {
                                buttonStateWorker.RunWorkerAsync();
                            }
                            notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                string.Format(Properties.Resources.UIMessageRemovedFromGroup, LocalAdministratorGroup.LocalAdminGroupName), 
                                ToolTipIcon.Info);
                        }
                    }
                    else
                    {
                        // No change in admin status, but update status display in case expiration time changed
                        if (this.userIsAdmin)
                        {
                            // Update status display to reflect current expiration time
                            if (this.InvokeRequired)
                            {
                                this.Invoke(new Action(() => this.UpdateStatusDisplayBasedOnState()));
                            }
                            else
                            {
                                this.UpdateStatusDisplayBasedOnState();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Application", "Error in timer elapsed handler", ex.ToString());
            }
            finally
            {
                try
                {
                    if (channelFactory != null)
                    {
                        channelFactory.Close();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Performs local privilege removal on the user's computer.
        /// This is called when a request is rejected or admin rights need to be removed.
        /// </summary>
        /// <param name="statusMessage">
        /// Status message to display during removal.
        /// </param>
        private void PerformLocalPrivilegeRemoval(string statusMessage)
        {
            try
            {
                ClientLogger.Info("Application", "Removing local administrator privileges", 
                    string.Format("User: {0}, Reason: Request rejected", WindowsIdentity.GetCurrent().Name));
                
                // Update status
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => this.UpdateStatusDisplay(statusMessage)));
                }
                else
                {
                    this.UpdateStatusDisplay(statusMessage);
                }
                
                // Connect to local service via NamedPipe to perform privilege removal
                string namedPipeAddress = Shared.NamedPipeServiceBaseAddress;
                NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport);
                
                ChannelFactory<IAdminGroup> localFactory = new ChannelFactory<IAdminGroup>(binding, namedPipeAddress);
                IAdminGroup localChannel = localFactory.CreateChannel();
                
                ICommunicationObject commObj = localChannel as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Created)
                {
                    commObj.Open();
                }
                
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    // Call RemoveUserFromAdministratorsGroup on LOCAL service
                    localChannel.RemoveUserFromAdministratorsGroup(RemovalReason.UserRequest);
                    
                    ClientLogger.Info("Application", "Local privilege removal completed", 
                        string.Format("User: {0}", WindowsIdentity.GetCurrent().Name));
                    
                    // Update status
                    this.UpdateUserAdministratorStatus();
                    
                    // Update UI on UI thread
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.addMeButton.Enabled = true;
                            this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                            this.removeMeButton.Enabled = false;
                            this.userWasAdminOnLastCheck = false;
                            this.StopCountdown();
                            // Update status display based on current state
                            this.UpdateStatusDisplayBasedOnState();
                            notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                "您的管理员权限请求已被拒绝，管理员权限已移除", ToolTipIcon.Warning);
                        }));
                    }
                    else
                    {
                        this.addMeButton.Enabled = true;
                        this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                        this.removeMeButton.Enabled = false;
                        this.userWasAdminOnLastCheck = false;
                        this.StopCountdown();
                        // Update status display based on current state
                        this.UpdateStatusDisplayBasedOnState();
                        notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                            "您的管理员权限请求已被拒绝，管理员权限已移除", ToolTipIcon.Warning);
                    }
                }
                
                if (commObj != null)
                {
                    commObj.Close();
                }
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Application", "Error removing local administrator privileges", ex.ToString());
                
                // Update UI on UI thread
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        this.UpdateStatusDisplay("请求已被拒绝，但移除权限时出错");
                        this.addMeButton.Enabled = true;
                        this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                        this.StopCountdown();
                        notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                            "您的管理员权限请求已被拒绝，但移除权限时出错", ToolTipIcon.Error);
                    }));
                }
                else
                {
                    this.UpdateStatusDisplay("请求已被拒绝，但移除权限时出错");
                    this.addMeButton.Enabled = true;
                    this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                    this.StopCountdown();
                    notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                        "您的管理员权限请求已被拒绝，但移除权限时出错", ToolTipIcon.Error);
                }
            }
        }

        /// <summary>
        /// Removes the current user from the local Administrators group when the approved time has expired.
        /// Updates UI and status to indicate that privileges have been removed.
        /// </summary>
        private void PerformExpirationRemoval()
        {
            try
            {
                ClientLogger.Info("Application", "Privilege time expired, removing local administrator rights", 
                    string.Format("User: {0}", WindowsIdentity.GetCurrent().Name));
                
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => this.UpdateStatusDisplay("管理员权限已到期，正在移除…")));
                }
                else
                {
                    this.UpdateStatusDisplay("管理员权限已到期，正在移除…");
                }
                
                string namedPipeAddress = Shared.NamedPipeServiceBaseAddress;
                NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport);
                ChannelFactory<IAdminGroup> localFactory = new ChannelFactory<IAdminGroup>(binding, namedPipeAddress);
                IAdminGroup localChannel = localFactory.CreateChannel();
                ICommunicationObject commObj = localChannel as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Created)
                    commObj.Open();
                
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    localChannel.RemoveUserFromAdministratorsGroup(RemovalReason.Timeout);
                    ClientLogger.Info("Application", "Local privilege removal (expiration) completed", 
                        string.Format("User: {0}", WindowsIdentity.GetCurrent().Name));
                    this.UpdateUserAdministratorStatus();
                    
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.addMeButton.Enabled = true;
                            this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                            this.removeMeButton.Enabled = false;
                            this.userWasAdminOnLastCheck = false;
                            this.StopCountdown();
                            this.UpdateStatusDisplayBasedOnState();
                            notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                "管理员权限已到期，已自动移除。", ToolTipIcon.Info);
                        }));
                    }
                    else
                    {
                        this.addMeButton.Enabled = true;
                        this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                        this.removeMeButton.Enabled = false;
                        this.userWasAdminOnLastCheck = false;
                        this.StopCountdown();
                        this.UpdateStatusDisplayBasedOnState();
                        notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                            "管理员权限已到期，已自动移除。", ToolTipIcon.Info);
                    }
                }
                
                if (commObj != null)
                {
                    try { commObj.Close(); } catch { }
                }
                try { localFactory.Close(); } catch { }
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Application", "Error removing local administrator privileges (expiration)", ex.ToString());
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        this.UpdateStatusDisplay("管理员权限已到期，但移除时出错");
                        this.addMeButton.Enabled = true;
                        this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                        this.StopCountdown();
                        notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                            "管理员权限已到期，但移除时出错，请尝试手动点击「将当前账户从管理员组移除」。", ToolTipIcon.Error);
                    }));
                }
                else
                {
                    this.UpdateStatusDisplay("管理员权限已到期，但移除时出错");
                    this.addMeButton.Enabled = true;
                    this.addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                    this.StopCountdown();
                    notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                        "管理员权限已到期，但移除时出错，请尝试手动点击「将当前账户从管理员组移除」。", ToolTipIcon.Error);
                }
            }
        }

        /// <summary>
        /// Performs local privilege elevation on the user's computer.
        /// This is called when a request is approved by the administrator.
        /// </summary>
        /// <param name="approvedTimeoutMinutes">Approved duration from server (0 = permanent, &gt;0 = minutes). Passed to local service so it does not depend on HKLM write.</param>
        private void PerformLocalPrivilegeElevation(int? approvedTimeoutMinutes)
        {
            try
            {
                ClientLogger.Info("Application", "Request approved, performing local privilege elevation", 
                    string.Format("User: {0}, Timeout: {1}", WindowsIdentity.GetCurrent().Name, approvedTimeoutMinutes.HasValue ? approvedTimeoutMinutes.Value.ToString() + " min" : "registry/default"));
                
                // Connect to local service via NamedPipe to perform privilege elevation
                string namedPipeAddress = Shared.NamedPipeServiceBaseAddress;
                NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport);
                
                ChannelFactory<IAdminGroup> localFactory = new ChannelFactory<IAdminGroup>(binding, namedPipeAddress);
                IAdminGroup localChannel = localFactory.CreateChannel();
                
                ICommunicationObject commObj = localChannel as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Created)
                {
                    commObj.Open();
                }
                
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    bool succeeded = false;
                    try
                    {
                        // Prefer new method: pass approved timeout so service does not rely on HKLM
                        localChannel.AddUserToAdministratorsGroupWithTimeout(approvedTimeoutMinutes);
                        succeeded = true;
                    }
                    catch (Exception fallbackEx) when (
                        fallbackEx is System.ServiceModel.ProtocolException ||
                        fallbackEx is System.ServiceModel.FaultException ||
                        (fallbackEx is InvalidOperationException && (fallbackEx.Message?.Contains("ContractFilter") == true || fallbackEx.Message?.Contains("EndpointDispatcher") == true)))
                    {
                        // ContractFilter/EndpointDispatcher mismatch: local service is old and does not have the new method
                        ClientLogger.Warning("Application", "Local service does not support AddUserToAdministratorsGroupWithTimeout, falling back to AddUserToAdministratorsGroup. Update Client Service for timeout support.", fallbackEx.Message);
                    }

                    if (!succeeded)
                    {
                        try { if (commObj.State == CommunicationState.Opened) commObj.Close(); } catch { }
                        try { localFactory.Close(); } catch { }
                        // Fallback: use old method (service will use registry/default; may show permanent if user cannot write HKLM)
                        localFactory = new ChannelFactory<IAdminGroup>(binding, namedPipeAddress);
                        localChannel = localFactory.CreateChannel();
                        commObj = localChannel as ICommunicationObject;
                        if (commObj != null && commObj.State == CommunicationState.Created) commObj.Open();
                        if (commObj != null && commObj.State == CommunicationState.Opened)
                        {
                            localChannel.AddUserToAdministratorsGroup();
                            succeeded = true;
                        }
                    }

                    if (succeeded)
                    {
                        ClientLogger.Info("Application", "Local privilege elevation completed", 
                            string.Format("User: {0}", WindowsIdentity.GetCurrent().Name));
                        // Update status
                        this.UpdateUserAdministratorStatus();
                    }
                    
                    if (succeeded && this.userIsAdmin)
                    {
                        // Update UI on UI thread
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() =>
                            {
                                this.addMeButton.Enabled = false;
                                this.addMeButton.Text = Properties.Resources.UIMessageAlreadyHaveRights;
                                // Enable remove button since user is now admin
                                this.removeMeButton.Enabled = true;
                                this.userWasAdminOnLastCheck = true;
                                this.StartCountdown();
                                // Update status display based on current state (will show expiration time if available)
                                this.UpdateStatusDisplayBasedOnState();
                                notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                    string.Format(Properties.Resources.UIMessageAddedToGroup, LocalAdministratorGroup.LocalAdminGroupName), 
                                    ToolTipIcon.Info);
                            }));
                        }
                        else
                        {
                            this.addMeButton.Enabled = false;
                            this.addMeButton.Text = Properties.Resources.UIMessageAlreadyHaveRights;
                            // Enable remove button since user is now admin
                            this.removeMeButton.Enabled = true;
                            this.userWasAdminOnLastCheck = true;
                            this.StartCountdown();
                            // Update status display based on current state (will show expiration time if available)
                            this.UpdateStatusDisplayBasedOnState();
                            notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                                string.Format(Properties.Resources.UIMessageAddedToGroup, LocalAdministratorGroup.LocalAdminGroupName), 
                                ToolTipIcon.Info);
                        }
                    }
                    
                    commObj.Close();
                }
                
                localFactory.Close();
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Application", "Failed to perform local privilege elevation", ex.ToString());
                
                // Update UI on UI thread
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        this.UpdateStatusDisplay(string.Format("请求已批准，但本地提权失败: {0}", ex.Message));
                        notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                            string.Format("请求已批准，但本地提权失败: {0}", ex.Message), 
                            ToolTipIcon.Error);
                    }));
                }
                else
                {
                    this.appStatus.Text = string.Format("请求已批准，但本地提权失败: {0}", ex.Message);
                    notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, 
                        string.Format("请求已批准，但本地提权失败: {0}", ex.Message), 
                        ToolTipIcon.Error);
                }
            }
        }


        /// <summary>
        /// Sets the form's text to the name of the application plus a partial version number.
        /// </summary>
        private void SetFormText()
        {
            System.Text.StringBuilder formText = new System.Text.StringBuilder();
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (attributes.Length == 0)
            {
                formText.Append(Properties.Resources.ApplicationName);
            }
            else
            {
                formText.Append(((AssemblyProductAttribute)attributes[0]).Product);
            }
            formText.Append(' ');
            formText.Append(Assembly.GetExecutingAssembly().GetName().Version.ToString(3));

            this.Text = formText.ToString();
            this.notifyIcon.Text = formText.ToString();
        }


        /// <summary>
        /// This function handles the Click event for the Submit button.
        /// </summary>
        /// <param name="sender">
        /// The button being clicked.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void ClickSubmitButton(object sender, EventArgs e)
        {
            this.DisableButtons();
            this.UpdateStatusDisplay(string.Format(Properties.Resources.UIMessageAddingToGroup, LocalAdministratorGroup.LocalAdminGroupName));
            addUserBackgroundWorker.RunWorkerAsync();
        }


        /// <summary>
        /// This function runs when RunWorkerAsync() is called by the "grant admin rights" BackgroundWorker object.
        /// </summary>
        /// <param name="sender">
        /// The BackgroundWorker that triggered the event.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void addUserBackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            ClientLogger.Info("Application", "Submitting admin rights request", 
                string.Format("User: {0}", System.Security.Principal.WindowsIdentity.GetCurrent().Name));
            
            var channelTuple = this.CreateTcpChannel();
            ChannelFactory<IAdminGroup> channelFactory = channelTuple.Item1;
            IAdminGroup channel = channelTuple.Item2;

            try
            {
                // Check channel state before using
                ICommunicationObject commObj = channel as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Faulted)
                {
                    ClientLogger.Error("WCF", "Channel is faulted before use", string.Format("State: {0}", commObj.State));
                    throw new InvalidOperationException("通信通道处于错误状态。请检查服务器是否运行，以及服务器地址配置是否正确。");
                }

                ClientLogger.Info("WCF", "Calling AddUserToAdministratorsGroup", "");
                channel.AddUserToAdministratorsGroup();
                ClientLogger.Info("Application", "Admin rights request submitted successfully", "");
            }
            catch (System.ServiceModel.EndpointNotFoundException ex)
            {
                ClientLogger.Error("WCF", "Endpoint not found", 
                    string.Format("Server: {0}, Error: {1}", Settings.ServerAddress, ex.Message));
                throw new InvalidOperationException(
                    string.Format("无法连接到服务器。\n\n请检查：\n1. 服务器地址是否正确（当前配置：{0}）\n2. 服务器上的 Make Me Admin 服务是否正在运行\n3. 网络连接是否正常\n\n错误详情：{1}", 
                    Settings.ServerAddress, ex.Message), ex);
            }
            catch (System.ServiceModel.CommunicationObjectFaultedException ex)
            {
                // CommunicationObjectFaultedException 继承自 CommunicationException，必须先 catch
                ClientLogger.Error("WCF", "Channel faulted", 
                    string.Format("Server: {0}, Error: {1}", Settings.ServerAddress, ex.Message));
                throw new InvalidOperationException(
                    string.Format("通信通道处于错误状态。\n\n请检查：\n1. 服务器地址是否正确（当前配置：{0}）\n2. 服务器上的 Make Me Admin 服务是否正在运行\n3. 服务版本是否与客户端兼容\n\n错误详情：{1}", 
                    Settings.ServerAddress, ex.Message), ex);
            }
            catch (System.ServiceModel.Security.SecurityNegotiationException ex)
            {
                // SecurityNegotiationException 继承自 SecurityException，而 SecurityException 继承自 CommunicationException
                // 必须在 CommunicationException 之前 catch
                // 这个异常通常表示身份验证协商失败，服务器拒绝了客户端凭据
                ClientLogger.Error("WCF", "Security negotiation failed", 
                    string.Format("Server: {0}, Error: {1}, Inner: {2}", 
                    Settings.ServerAddress, ex.Message, ex.InnerException?.Message));
                throw new InvalidOperationException(
                    string.Format("身份验证失败：服务器已拒绝客户端凭据。\n\n可能的原因：\n1. 用户端和服务器不在同一域中（跨域身份验证失败）\n2. 服务器拒绝当前用户的 Windows 凭据\n3. 服务运行账户配置问题\n4. 网络身份验证配置问题\n\n解决方法：\n1. 确认服务器地址是否正确（当前配置：{0}）\n2. 检查用户端和服务器是否在同一域中\n3. 如果跨域，需要配置 Kerberos/SPN（需要域管理员权限）\n4. 检查服务器上的服务运行账户\n5. 查看服务器事件日志（Windows 日志 → 安全）获取详细错误信息\n\n错误详情：{1}", 
                    Settings.ServerAddress, ex.Message), ex);
            }
            catch (System.ServiceModel.Security.MessageSecurityException ex)
            {
                // MessageSecurityException 继承自 SecurityException，而 SecurityException 继承自 CommunicationException
                // 必须在 CommunicationException 之前 catch
                ClientLogger.Error("WCF", "Message security exception", 
                    string.Format("Server: {0}, Error: {1}, Inner: {2}", 
                    Settings.ServerAddress, ex.Message, ex.InnerException?.Message));
                throw new InvalidOperationException(
                    string.Format("安全协议错误：服务器拒绝了安全升级请求。\n\n可能的原因：\n1. 客户端和服务器的安全配置不匹配\n2. 服务器上的服务版本与客户端不兼容\n3. Windows 身份验证配置问题\n\n请检查：\n1. 服务器地址是否正确（当前配置：{0}）\n2. 服务器上的 Make Me Admin 服务是否正在运行\n3. 服务器和客户端是否使用相同版本\n4. 查看服务器事件日志获取详细错误信息\n\n错误详情：{1}", 
                    Settings.ServerAddress, ex.Message), ex);
            }
            catch (System.ServiceModel.CommunicationException ex)
            {
                // CommunicationException 是更通用的异常，放在所有派生类之后
                ClientLogger.Error("WCF", "Communication exception", 
                    string.Format("Server: {0}, Error: {1}, Inner: {2}", 
                    Settings.ServerAddress, ex.Message, ex.InnerException?.Message));
                throw new InvalidOperationException(
                    string.Format("通信错误：无法连接到服务器。\n\n请检查：\n1. 服务器地址是否正确（当前配置：{0}）\n2. 服务器上的 Make Me Admin 服务是否正在运行\n3. 防火墙是否阻止了连接\n\n错误详情：{1}", 
                    Settings.ServerAddress, ex.Message), ex);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Win32Exception 通常表示 Windows 身份验证失败
                throw new InvalidOperationException(
                    string.Format("Windows 身份验证失败。\n\n可能的原因：\n1. 用户端和服务器不在同一域中\n2. 服务器拒绝客户端凭据\n3. 网络身份验证配置问题\n\n请检查：\n1. 服务器地址是否正确（当前配置：{0}）\n2. 用户端和服务器是否在同一域中\n3. 服务器上的服务是否正常运行\n4. 查看服务器事件日志获取详细错误信息\n\n错误详情：{1}", 
                    Settings.ServerAddress, ex.Message), ex);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                if (channelFactory != null)
                {
                    try
                    {
                        ICommunicationObject factoryCommObj = channelFactory as ICommunicationObject;
                        if (factoryCommObj != null && factoryCommObj.State == CommunicationState.Faulted)
                        {
                            factoryCommObj.Abort();
                        }
                        else
                        {
                            channelFactory.Close();
                        }
                    }
                    catch { }
                }
            }
        }


        /// <summary>
        /// Occurs when the background operation has completed, has been canceled, or has raised an exception.
        /// </summary>
        /// <param name="sender">
        /// The "grant admin rights" BackgroundWorker object, which triggered the event.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void addUserBackgroundWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                System.Text.StringBuilder message = new System.Text.StringBuilder(Properties.Resources.UIMessageErrorWhileAdding);
                message.Append(System.Environment.NewLine);
                message.Append(System.Environment.NewLine);
                message.Append(Properties.Resources.ErrorMessage);
                message.Append(": ");
                message.Append(e.Error.Message);

                // Add helpful suggestions for common errors
                if (e.Error is InvalidOperationException || 
                    e.Error is System.ServiceModel.EndpointNotFoundException ||
                    e.Error is System.ServiceModel.CommunicationException)
                {
                    message.Append(System.Environment.NewLine);
                    message.Append(System.Environment.NewLine);
                    message.Append("提示：");
                    message.Append(System.Environment.NewLine);
                    message.Append("1. 检查服务器地址配置（点击\"设置\"按钮）");
                    message.Append(System.Environment.NewLine);
                    message.Append("2. 确认服务器上的 Make Me Admin 服务正在运行");
                    message.Append(System.Environment.NewLine);
                    message.Append("3. 检查网络连接和防火墙设置");
                }

                MessageBox.Show(this, message.ToString(), Properties.Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0);
            }

            this.UpdateUserAdministratorStatus();

            if (this.userIsAdmin)
            { // Display a message that the user now has administrator rights.
                this.appStatus.Text = Properties.Resources.ApplicationIsReady;
                this.userWasAdminOnLastCheck = this.userIsAdmin;
                this.userHadPendingRequestOnLastCheck = false;
                this.StartCountdown();
                this.notifyIconTimer.Start();
                notifyIcon.Visible = true;
                this.Visible = false;
                this.ShowInTaskbar = false;
                notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, string.Format(Properties.Resources.UIMessageAddedToGroup, LocalAdministratorGroup.LocalAdminGroupName), ToolTipIcon.Info);
            }
            else
            {
                // User no longer has admin rights, stop countdown
                this.StopCountdown();
                
                // Check if approval is required and if user has a pending request
                var channelTuple = this.CreateTcpChannel();
                ChannelFactory<IAdminGroup> channelFactory = channelTuple.Item1;
                IAdminGroup channel = channelTuple.Item2;
                
                try
                {
                    bool hasPendingRequest = channel.HasPendingRequest();
                    if (hasPendingRequest)
                    {
                        this.UpdateStatusDisplay("请求已提交，等待管理员审批...");
                        this.userHadPendingRequestOnLastCheck = true;
                        
                        // Register for real-time notifications using duplex channel
                        this.RegisterForNotifications();
                        
                        // Also start timer as fallback (in case callback fails)
                        if (!this.notifyIconTimer.Enabled)
                        {
                            this.notifyIconTimer.Start();
                        }
                        
                        notifyIcon.ShowBalloonTip(5000, Properties.Resources.ApplicationName, "您的请求已提交，正在等待管理员审批", ToolTipIcon.Info);
                    }
                    else
                    {
                        this.userHadPendingRequestOnLastCheck = false;
                        // Update status display based on current state
                        this.UpdateStatusDisplayBasedOnState();
                    }
                }
                catch { }
                finally
                {
                    if (channelFactory != null)
                    {
                        channelFactory.Close();
                    }
                }

                if (!buttonStateWorker.IsBusy)
                {
                    buttonStateWorker.RunWorkerAsync();
                }
            }
        }


        /// <summary>
        /// Handles the Click event for the rights removal button.
        /// </summary>
        /// <param name="sender">
        /// The button being clicked.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void ClickRemoveRightsButton(object sender, EventArgs e)
        {
            this.DisableButtons();
            this.UpdateStatusDisplay(string.Format(Properties.Resources.UIMessageRemovingFromGroup, LocalAdministratorGroup.LocalAdminGroupName));
            removeUserBackgroundWorker.RunWorkerAsync();
        }


        /// <summary>
        /// This function runs when RunWorkerAsync() is called by the rights removal BackgroundWorker object.
        /// </summary>
        /// <param name="sender">
        /// The BackgroundWorker that triggered the event.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void removeUserBackgroundWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var channelTuple = this.CreateTcpChannel();
            ChannelFactory<IAdminGroup> channelFactory = channelTuple.Item1;
            IAdminGroup channel = channelTuple.Item2;
            
            try
            {
                channel.RemoveUserFromAdministratorsGroup(RemovalReason.UserRequest);
            }
            finally
            {
                if (channelFactory != null)
                {
                    channelFactory.Close();
                }
            }
        }


        /// <summary>
        /// Occurs when the background operation has completed, has been canceled, or has raised an exception.
        /// </summary>
        /// <param name="sender">
        /// The rights removal BackgroundWorker object, which triggered the event.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void removeUserBackgroundWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                System.Text.StringBuilder message = new System.Text.StringBuilder(Properties.Resources.UIMessageErrorWhileRemoving);
                message.Append(System.Environment.NewLine);
                message.Append(Properties.Resources.UIMessageEnsureServiceRunning);
                message.Append(System.Environment.NewLine);
                message.Append(Properties.Resources.ErrorMessage);
                message.Append(": ");
                message.Append(e.Error.Message);
                message.Append(System.Environment.NewLine);
                message.Append(Properties.Resources.StackTrace);
                message.Append(": ");
                message.Append(e.Error.StackTrace);
                message.Append(System.Environment.NewLine);

                if (e.Error.InnerException != null)
                {
                    message.Append(System.Environment.NewLine);
                    message.Append(e.Error.InnerException.Message);
                    if (e.Error.InnerException.InnerException != null)
                    { // This is quite ridiculous.
                        message.Append(System.Environment.NewLine);
                        message.Append(e.Error.InnerException.InnerException.Message);
                    }
                }

                MessageBox.Show(this, message.ToString(), Properties.Resources.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, 0);
            }

            if (!buttonStateWorker.IsBusy)
            {
                buttonStateWorker.RunWorkerAsync();
            }
        }


        /// <summary>
        /// This function handles the Click event for the Exit button.
        /// </summary>
        /// <param name="sender">
        /// The button being clicked.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void ClickExitButton(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles the Click event for the Settings button.
        /// </summary>
        /// <summary>
        /// Handles the click event for the log viewer button.
        /// </summary>
        private void ClickLogViewerButton(object sender, EventArgs e)
        {
            try
            {
                LogViewerForm logViewer = new LogViewerForm();
                logViewer.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开日志查看器: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the privilege timeout diagnostics menu item.
        /// </summary>
        private void ClickDiagnosticsButton(object sender, EventArgs e)
        {
            try
            {
                using (var diagnosticsForm = new PrivilegeTimeoutDiagnosticsForm())
                {
                    diagnosticsForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开提权诊断: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClickSettingsButton(object sender, EventArgs e)
        {
            using (ServerSettingsForm settingsForm = new ServerSettingsForm())
            {
                if (settingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    MessageBox.Show(
                        "服务器设置已保存。\n如果更改了服务器地址，请重新启动应用程序以确保连接正常。",
                        "设置已保存",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }


        /// <summary>
        /// Disables the add and remove buttons.
        /// </summary>
        private void DisableButtons()
        {
            this.addMeButton.Enabled = false;
            this.removeMeButton.Enabled = false;
        }

        /// <summary>
        /// Updates status display in the bottom status bar only (all status/countdown merged here).
        /// </summary>
        private void UpdateStatusDisplay(string statusText)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatusDisplay(statusText)));
                return;
            }

            if (this.appStatus != null)
            {
                this.appStatus.Text = statusText;
            }
        }

        /// <summary>
        /// Updates the status display based on the current application state.
        /// This method intelligently determines what status message to show based on:
        /// - User's administrator status
        /// - Pending request status
        /// - Authorization status
        /// - Expiration time
        /// </summary>
        private void UpdateStatusDisplayBasedOnState()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatusDisplayBasedOnState()));
                return;
            }

            string statusText = this.GetStatusTextForCurrentState();
            this.UpdateStatusDisplay(statusText);
        }

        /// <summary>
        /// Gets the appropriate status text based on the current application state.
        /// </summary>
        /// <returns>
        /// Returns a status message string that reflects the current state.
        /// </returns>
        private string GetStatusTextForCurrentState()
        {
            // Check if user is currently an administrator
            if (this.userIsAdmin)
            {
                // Check if there's an expiration time
                DateTime? expirationTime = this.GetUserExpirationTime();
                if (expirationTime.HasValue && expirationTime.Value > DateTime.Now)
                {
                    // User has admin rights with expiration time (format precise to second)
                    TimeSpan remaining = expirationTime.Value - DateTime.Now;
                    return FormatCountdownWithSeconds(remaining);
                }
                else if (expirationTime.HasValue && expirationTime.Value <= DateTime.Now)
                {
                    // Expiration time has passed (should be removed soon)
                    return "管理员权限已过期，即将移除...";
                }
                else
                {
                    // User has admin rights without expiration
                    return "您拥有管理员权限（永久）";
                }
            }

            // User is not an administrator
            // Check if there's a pending request
            if (this.userHadPendingRequestOnLastCheck)
            {
                return "请求已提交，等待管理员审批...";
            }

            // Check if user is authorized to request admin rights
            bool userIsAuthorizedLocally = Shared.UserIsAuthorized(
                WindowsIdentity.GetCurrent(), 
                Settings.LocalAllowedEntities, 
                Settings.LocalDeniedEntities);

            if (!userIsAuthorizedLocally)
            {
                return "您未被授权申请管理员权限";
            }

            // User can request admin rights
            return "您可以申请管理员权限";
        }


        /// <summary>
        /// Handles the Load event for the form.
        /// </summary>
        /// <param name="sender">
        /// The form being loaded.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void FormLoad(object sender, EventArgs e)
        {
            this.DisableButtons();
            this.UpdateStatusDisplay(Properties.Resources.CheckingAdminStatus);
            buttonStateWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Handles the HandleCreated event to set dark title bar.
        /// </summary>
        private void SubmitRequestForm_HandleCreated(object sender, EventArgs e)
        {
            this.SetDarkTitleBar();
        }


        /// <summary>
        /// Updates the variables which store the user's administrator status.
        /// </summary>
        private void UpdateUserAdministratorStatus()
        {
            this.userIsAdmin = LocalAdministratorGroup.IsMemberOfAdministrators(WindowsIdentity.GetCurrent());
            this.userIsDirectAdmin = LocalAdministratorGroup.IsMemberOfAdministratorsDirectly(WindowsIdentity.GetCurrent());
        }


        /// <summary>
        /// This function runs when RunWorkerAsync() is called by the button state BackgroundWorker object.
        /// </summary>
        /// <param name="sender">
        /// The BackgroundWorker that triggered the event.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void DoButtonStateWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            this.UpdateUserAdministratorStatus();
        }


        /// <summary>
        /// Occurs when the background operation has completed, has been canceled, or has raised an exception.
        /// </summary>
        /// <param name="sender">
        /// The button state BackgroundWorker object, which triggered the event.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void ButtonStateWorkCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            // Handle errors during status check
            if (e.Error != null)
            {
                ClientLogger.Error("Application", "Error checking administrator status", e.Error.ToString());
                this.UpdateStatusDisplay("检查管理员状态时出错");
                this.DisableButtons();
                return;
            }

            bool userIsAuthorizedLocally = Shared.UserIsAuthorized(WindowsIdentity.GetCurrent(), Settings.LocalAllowedEntities, Settings.LocalDeniedEntities);

            // Enable the "grant admin rights" button, if the user is not already
            // an administrator and is authorized to obtain those rights.
            this.addMeButton.Enabled = !this.userIsAdmin && userIsAuthorizedLocally;
            if (addMeButton.Enabled)
            {
                addMeButton.Text = Properties.Resources.GrantRightsButtonText;
                this.StopCountdown();
            }
            else if (this.userIsAdmin)
            {
                addMeButton.Text = Properties.Resources.UIMessageAlreadyHaveRights;
                this.StartCountdown();
            }
            else if (!userIsAuthorizedLocally)
            {
                addMeButton.Text = Properties.Resources.UIMessageUnauthorized;
                this.StopCountdown();
            }

            // Enable the rights removal button if the user is a member of the administrators group
            // (either directly or through nested group memberships)
            // Note: We check userIsAdmin instead of userIsDirectAdmin because the user might
            // have been added through nested groups, and they should still be able to remove themselves
            this.removeMeButton.Enabled = this.userIsAdmin;

            // Update status display based on current state (intelligent status display)
            this.UpdateStatusDisplayBasedOnState();

            if (this.addMeButton.Enabled)
            {
                this.addMeButton.Focus();
            }
            else if (this.removeMeButton.Enabled)
            {
                this.removeMeButton.Focus();
            }
        }


        /// <summary>
        /// Handles the MouseDoubleClick event for the notification area icon.
        /// </summary>
        /// <param name="sender">
        /// The notification icon that is being double-clicked.
        /// </param>
        /// <param name="e">
        /// Data specific to this event.
        /// </param>
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.ShowInTaskbar = true;
            this.Visible = true;
            // Refresh countdown display when window becomes visible
            if (this.userIsAdmin)
            {
                this.StartCountdown();
            }
            // Update status display based on current state
            this.UpdateStatusDisplayBasedOnState();
        }


        /// <summary>
        /// Handles the VisibleChanged event for the form.
        /// </summary>
        /// <param name="sender">
        /// The form whose visibility has changed.
        /// </param>
        /// <param name="e">
        /// Data specific to the event.
        /// </param>
        private void SubmitRequestForm_VisibleChanged(object sender, EventArgs e)
        {
            // Update the enabled/disabled state of the buttons, if the worker
            // is not already doing so.
            if (!buttonStateWorker.IsBusy)
            {
                buttonStateWorker.RunWorkerAsync();
            }
            
            // Refresh countdown display when window becomes visible
            if (this.Visible && this.userIsAdmin)
            {
                this.StartCountdown();
            }
        }


        /// <summary>
        /// Handles the BalloonTipClosed event for the notification icon.
        /// </summary>
        /// <param name="sender">
        /// The notification icon whose balloon tip was closed.
        /// </param>
        /// <param name="e">
        /// Data specific to the event.
        /// </param>
        private void notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            if (!this.userIsAdmin)
            {
                /*
                notifyIcon.Visible = false;
                this.Visible = true;
                this.ShowInTaskbar = true;
                */
                this.Close();
            }
        }

        /// <summary>
        /// Applies rounded corners to buttons for Apple-style appearance.
        /// </summary>
        /// <summary>
        /// Handles mouse enter event for buttons to trigger repaint.
        /// </summary>
        private void Button_MouseEnter(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                if (!buttonMouseOver.ContainsKey(button))
                {
                    buttonMouseOver[button] = false;
                }
                buttonMouseOver[button] = true;
                button.Invalidate();
            }
        }

        /// <summary>
        /// Handles mouse leave event for buttons to trigger repaint.
        /// </summary>
        private void Button_MouseLeave(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                if (!buttonMouseOver.ContainsKey(button))
                {
                    buttonMouseOver[button] = false;
                }
                buttonMouseOver[button] = false;
                buttonMouseDown[button] = false;
                button.Invalidate();
            }
        }

        /// <summary>
        /// Handles mouse down event for buttons to trigger repaint.
        /// </summary>
        private void Button_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Button button && e.Button == MouseButtons.Left)
            {
                if (!buttonMouseDown.ContainsKey(button))
                {
                    buttonMouseDown[button] = false;
                }
                buttonMouseDown[button] = true;
                button.Invalidate();
            }
        }

        /// <summary>
        /// Handles mouse up event for buttons to trigger repaint.
        /// </summary>
        private void Button_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is Button button && e.Button == MouseButtons.Left)
            {
                if (!buttonMouseDown.ContainsKey(button))
                {
                    buttonMouseDown[button] = false;
                }
                buttonMouseDown[button] = false;
                button.Invalidate();
            }
        }

        /// <summary>
        /// Paints the addMeButton with rounded corners and anti-aliasing.
        /// </summary>
        private void addMeButton_Paint(object sender, PaintEventArgs e)
        {
            PaintRoundedButton(this.addMeButton, e.Graphics);
        }

        /// <summary>
        /// Paints the removeMeButton with rounded corners and anti-aliasing.
        /// </summary>
        private void removeMeButton_Paint(object sender, PaintEventArgs e)
        {
            PaintRoundedButton(this.removeMeButton, e.Graphics);
        }


        /// <summary>
        /// Stores the current mouse state for each button.
        /// </summary>
        private System.Collections.Generic.Dictionary<Button, bool> buttonMouseOver = new System.Collections.Generic.Dictionary<Button, bool>();
        private System.Collections.Generic.Dictionary<Button, bool> buttonMouseDown = new System.Collections.Generic.Dictionary<Button, bool>();

        /// <summary>
        /// Paints a button with rounded corners using anti-aliasing for smooth edges.
        /// </summary>
        private void PaintRoundedButton(Button button, System.Drawing.Graphics g)
        {
            int cornerRadius = 10;
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, button.Width, button.Height);
            
            // Enable anti-aliasing for smooth rounded corners
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            // Determine button state color
            System.Drawing.Color backColor = button.BackColor;
            if (!button.Enabled)
            {
                // Disabled state - use darker color
                int r = Math.Max(0, backColor.R - 40);
                int green = Math.Max(0, backColor.G - 40);
                int b = Math.Max(0, backColor.B - 40);
                backColor = System.Drawing.Color.FromArgb(r, green, b);
            }
            else
            {
                // Check mouse state
                bool isMouseOver = buttonMouseOver.ContainsKey(button) && buttonMouseOver[button];
                bool isMouseDown = buttonMouseDown.ContainsKey(button) && buttonMouseDown[button];

                if (isMouseDown)
                {
                    // Mouse down state
                    backColor = button.FlatAppearance.MouseDownBackColor;
                }
                else if (isMouseOver)
                {
                    // Mouse over state
                    backColor = button.FlatAppearance.MouseOverBackColor;
                }
            }

            // Create rounded rectangle path
            using (System.Drawing.Drawing2D.GraphicsPath path = CreateRoundedRectanglePath(0, 0, button.Width, button.Height, cornerRadius))
            {
                // Clear the button area first to avoid default drawing artifacts
                g.Clear(button.Parent != null ? button.Parent.BackColor : System.Drawing.Color.FromArgb(37, 37, 38));

                // Fill button background
                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(backColor))
                {
                    g.FillPath(brush, path);
                }

                // Draw button text
                System.Drawing.StringFormat format = new System.Drawing.StringFormat
                {
                    Alignment = System.Drawing.StringAlignment.Center,
                    LineAlignment = System.Drawing.StringAlignment.Center,
                    Trimming = System.Drawing.StringTrimming.EllipsisCharacter,
                    FormatFlags = System.Drawing.StringFormatFlags.NoWrap
                };

                System.Drawing.Color textColor = button.Enabled ? button.ForeColor : System.Drawing.Color.FromArgb(128, button.ForeColor);
                using (System.Drawing.SolidBrush textBrush = new System.Drawing.SolidBrush(textColor))
                {
                    g.DrawString(button.Text, button.Font, textBrush, rect, format);
                }
            }
        }

        /// <summary>
        /// Creates a rounded rectangle graphics path.
        /// </summary>
        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(int x, int y, int width, int height, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        /// <summary>
        /// Paints shadows for buttons in the main content panel (Minimalist design - no shadows needed).
        /// </summary>
        private void mainContentPanel_Paint(object sender, PaintEventArgs e)
        {
            // Minimalist design: No shadows, clean flat design
        }

        /// <summary>
        /// Starts the countdown timer to display remaining administrator rights time.
        /// </summary>
        private void StartCountdown()
        {
            if (!this.userIsAdmin)
            {
                this.StopCountdown();
                return;
            }

            // Get expiration time for current user
            DateTime? expirationTime = this.GetUserExpirationTime();
            
            if (expirationTime.HasValue && expirationTime.Value > DateTime.Now)
            {
                // User has a valid expiration time; countdown shown in bottom status bar only
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        this.UpdateCountdownDisplay();
                        if (!this.countdownTimer.Enabled)
                        {
                            this.countdownTimer.Start();
                        }
                    }));
                }
                else
                {
                    this.UpdateCountdownDisplay();
                    if (!this.countdownTimer.Enabled)
                    {
                        this.countdownTimer.Start();
                    }
                }
                
                ClientLogger.Info("Application", "Countdown started", 
                    string.Format("Expiration time: {0}", expirationTime.Value));
            }
            else
            {
                // No expiration time or already expired, hide countdown
                this.StopCountdown();
                
                if (expirationTime.HasValue)
                {
                    ClientLogger.Info("Application", "Countdown not started - already expired", 
                        string.Format("Expiration time: {0}, Current time: {1}", expirationTime.Value, DateTime.Now));
                }
                else
                {
                    ClientLogger.Info("Application", "Countdown not started - no expiration time");
                }
            }
        }

        /// <summary>
        /// Stops the countdown timer and hides the countdown label.
        /// </summary>
        private void StopCountdown()
        {
            this.countdownTimer.Stop();
            this.countdownLabel.Visible = false;
            this.countdownLabel.Text = string.Empty;
        }

        /// <summary>
        /// Gets the expiration time for the current user.
        /// Prefer the local service's store (SYSTEM's users.xml) via GetMyExpirationTime; fall back to local file if unavailable.
        /// </summary>
        /// <returns>
        /// Returns the expiration DateTime if the user has administrator rights with expiration, null otherwise.
        /// </returns>
        private DateTime? GetUserExpirationTime()
        {
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            SecurityIdentifier currentUserSid = currentIdentity?.User;
            if (currentUserSid == null)
            {
                ClientLogger.Warning("Application", "Cannot get user SID for expiration time lookup", "WindowsIdentity.GetCurrent().User is null");
                return null;
            }

            // Prefer local service (it stores in SYSTEM's users.xml; we cannot read that file directly)
            try
            {
                string namedPipeAddress = Shared.NamedPipeServiceBaseAddress;
                NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport);
                ChannelFactory<IAdminGroup> localFactory = new ChannelFactory<IAdminGroup>(binding, namedPipeAddress);
                IAdminGroup localChannel = localFactory.CreateChannel();
                ICommunicationObject commObj = localChannel as ICommunicationObject;
                if (commObj != null && commObj.State == CommunicationState.Created)
                    commObj.Open();
                if (commObj != null && commObj.State == CommunicationState.Opened)
                {
                    DateTime? fromService = localChannel.GetMyExpirationTime();
                    try { if (commObj.State == CommunicationState.Opened) commObj.Close(); } catch { }
                    try { localFactory.Close(); } catch { }
                    if (fromService.HasValue)
                        return fromService.Value;
                }
            }
            catch (Exception ex)
            {
                ClientLogger.Debug("Application", "GetMyExpirationTime from local service failed, using local file", ex.Message);
            }

            try
            {
                EncryptedSettings encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
                return encryptedSettings.GetExpirationTime(currentUserSid);
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Application", "Error getting user expiration time", ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Updates the countdown display label with remaining time.
        /// </summary>
        private void UpdateCountdownDisplay()
        {
            if (!this.userIsAdmin)
            {
                this.StopCountdown();
                return;
            }

            DateTime? expirationTime = this.GetUserExpirationTime();
            
            if (!expirationTime.HasValue)
            {
                // No expiration (permanent), just keep countdown stopped if it was running
                this.StopCountdown();
                return;
            }
            
            if (expirationTime.Value <= DateTime.Now)
            {
                // Already expired: stop countdown, remove admin rights and update UI
                this.StopCountdown();
                this.PerformExpirationRemoval();
                return;
            }

            TimeSpan remaining = expirationTime.Value - DateTime.Now;
            
            if (remaining.TotalSeconds <= 0)
            {
                // Time has just expired: stop countdown, remove admin rights and update UI
                this.StopCountdown();
                this.PerformExpirationRemoval();
                return;
            }

            // Show countdown in bottom status bar, precise to second
            if (this.appStatus != null)
                this.appStatus.Text = FormatCountdownWithSeconds(remaining);
        }

        /// <summary>
        /// Formats remaining time for bottom status bar, precise to the second.
        /// </summary>
        private static string FormatCountdownWithSeconds(TimeSpan remaining)
        {
            if (remaining.TotalDays >= 1)
                return string.Format("您拥有管理员权限，剩余 {0} 天 {1} 小时 {2} 分 {3} 秒", remaining.Days, remaining.Hours, remaining.Minutes, remaining.Seconds);
            if (remaining.TotalHours >= 1)
                return string.Format("您拥有管理员权限，剩余 {0} 小时 {1} 分 {2} 秒", remaining.Hours, remaining.Minutes, remaining.Seconds);
            if (remaining.TotalMinutes >= 1)
                return string.Format("您拥有管理员权限，剩余 {0} 分 {1} 秒", remaining.Minutes, remaining.Seconds);
            return string.Format("您拥有管理员权限，剩余 {0} 秒", remaining.Seconds);
        }

        /// <summary>
        /// Handles the countdown timer tick event to update the countdown display.
        /// </summary>
        /// <param name="sender">
        /// The timer that triggered the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void countdownTimer_Tick(object sender, EventArgs e)
        {
            this.UpdateCountdownDisplay();
            // Also update status display to show remaining time in status bar
            this.UpdateStatusDisplayBasedOnState();
        }

        private void appStatus_Click(object sender, EventArgs e)
        {

        }
    }
}
