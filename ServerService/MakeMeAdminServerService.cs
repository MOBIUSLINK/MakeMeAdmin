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
    using System.ServiceModel;
    using System.ServiceProcess;

    /// <summary>
    /// This class is the Windows Service for server-side request management.
    /// It provides TCP service for managing pending requests from remote clients.
    /// </summary>
    /// <remarks>
    /// This service is installed on administrator computers and only provides TCP service
    /// for remote communication. It does NOT provide NamedPipe service or perform local privilege elevation.
    /// </remarks>
    public partial class MakeMeAdminServerService : ServiceBase
    {
        /// <summary>
        /// A Windows Communication Foundation (WCF) service host which communicates over TCP.
        /// </summary>
        /// <remarks>
        /// This service host exists for communication from remote computers.
        /// It is the ONLY service host for the server service.
        /// </remarks>
        private ServiceHost tcpServiceHost = null;

        /// <summary>
        /// Singleton instance of AdminGroupManipulatorDuplex service.
        /// Required because ServiceBehavior uses InstanceContextMode.Single.
        /// </summary>
        private AdminGroupManipulatorDuplex serviceInstance = null;

        /// <summary>
        /// Instantiate a new instance of the Make Me Admin Server Service.
        /// </summary>
        public MakeMeAdminServerService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates the WCF Service Host which is accessible via TCP.
        /// </summary>
        /// <remarks>
        /// This is the ONLY service host for the server service.
        /// It only provides remote communication via TCP.
        /// </remarks>
        private void OpenTcpServiceHost()
        {
            try
            {
                // Use AdminGroupManipulatorDuplex for real-time notifications via callback
                // Create singleton instance because ServiceBehavior uses InstanceContextMode.Single
                this.serviceInstance = new AdminGroupManipulatorDuplex();
                this.tcpServiceHost = new ServiceHost(this.serviceInstance, new Uri(Shared.TcpServiceBaseAddress));
                this.tcpServiceHost.Faulted += ServiceHostFaulted;
                
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
                
                // Set timeouts to match client-side settings and allow for longer operations
                // This prevents connection abort during GetPendingRequests() which requires file I/O and decryption
                binding.ReceiveTimeout = TimeSpan.FromMinutes(10);  // Default is 10 minutes, but be explicit
                binding.SendTimeout = TimeSpan.FromMinutes(10);      // Increase from default 1 minute
                binding.OpenTimeout = TimeSpan.FromMinutes(1);
                binding.CloseTimeout = TimeSpan.FromMinutes(1);
                
                // Set message size limits
                binding.MaxReceivedMessageSize = 65536;
                binding.MaxBufferSize = 65536;
                
                // Add endpoint for duplex service contract (with callback support)
                // IMPORTANT: Only add ONE endpoint because IAdminGroupDuplex extends IAdminGroup
                // WCF does not allow multiple endpoints with the same ListenUri on the same ServiceHost
                // Clients can connect using either IAdminGroup or IAdminGroupDuplex contract to this endpoint
                this.tcpServiceHost.AddServiceEndpoint(typeof(IAdminGroupDuplex), binding, Shared.TcpServiceBaseAddress);
                
                this.tcpServiceHost.Open();
                
                ApplicationLog.WriteEvent(
                    "Make Me Admin Server Service started successfully. TCP service host opened.",
                    EventID.DebugMessage,
                    System.Diagnostics.EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                // Log detailed error information
                try
                {
                    ApplicationLog.WriteEvent(
                        string.Format("Failed to open TCP service host: {0}\nType: {1}\nStackTrace: {2}", 
                            ex.Message, ex.GetType().Name, ex.StackTrace),
                        EventID.DebugMessage,
                        System.Diagnostics.EventLogEntryType.Error);
                }
                catch { }
                
                // Clean up on failure
                if (this.tcpServiceHost != null)
                {
                    try
                    {
                        if (this.tcpServiceHost.State == CommunicationState.Faulted)
                        {
                            this.tcpServiceHost.Abort();
                        }
                        else
                        {
                            this.tcpServiceHost.Close();
                        }
                    }
                    catch { }
                    this.tcpServiceHost = null;
                }
                
                throw;
            }
        }

        /// <summary>
        /// Handles the faulted event for a WCF service host.
        /// </summary>
        /// <param name="sender">
        /// The service host that has entered the faulted state.
        /// </param>
        /// <param name="e">
        /// Data related to the event.
        /// </param>
        private void ServiceHostFaulted(object sender, EventArgs e)
        {
            ApplicationLog.WriteEvent(Properties.Resources.ServiceHostFaulted, EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Warning);
        }

        /// <summary>
        /// Handles the startup of the service. 
        /// </summary>
        /// <param name="args">
        /// Data passed the start command.
        /// </param>
        /// <remarks>
        /// This function executes when a Start command is sent to the service by the
        /// Service Control Manager (SCM) or when the operating system starts
        /// (for a service that starts automatically).
        /// </remarks>
        protected override void OnStart(string[] args)
        {
            try
            {
                base.OnStart(args);
            }
            catch (Exception) { }

            try
            {
                // Create the Windows Event Log source for this application.
                ApplicationLog.CreateSource();

                // Open the TCP service host for server-client architecture.
                // This allows UserRequestApp and AdminUI to connect from remote machines.
                this.OpenTcpServiceHost();
            }
            catch (Exception ex)
            {
                // Log the exception to event log for debugging
                try
                {
                    ApplicationLog.WriteEvent(
                        string.Format("Failed to start Make Me Admin Server Service: {0}\nStackTrace: {1}", ex.Message, ex.StackTrace),
                        EventID.DebugMessage,
                        System.Diagnostics.EventLogEntryType.Error);
                }
                catch
                {
                    // If logging fails, try direct event log write
                    try
                    {
                        System.Diagnostics.EventLog.WriteEntry(
                            "Application",
                            string.Format("Make Me Admin Server Service: Failed to start - {0}", ex.Message),
                            System.Diagnostics.EventLogEntryType.Error);
                    }
                    catch { }
                }
                
                // Re-throw the exception to fail service startup
                // This ensures the service doesn't start in a broken state
                throw;
            }
        }

        /// <summary>
        /// Handles the stopping of the service.
        /// </summary>
        /// <remarks>
        /// Executes when a stop command is sent to the service by the Service Control Manager (SCM).
        /// </remarks>
        protected override void OnStop()
        {
            try
            {
                if (this.tcpServiceHost != null)
                {
                    if (this.tcpServiceHost.State == CommunicationState.Opened)
                    {
                        this.tcpServiceHost.Close();
                    }
                    else if (this.tcpServiceHost.State == CommunicationState.Faulted)
                    {
                        this.tcpServiceHost.Abort();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    ApplicationLog.WriteEvent(
                        string.Format("Error stopping TCP service host: {0}", ex.Message),
                        EventID.DebugMessage,
                        System.Diagnostics.EventLogEntryType.Warning);
                }
                catch { }
            }
            finally
            {
                this.tcpServiceHost = null;
                this.serviceInstance = null;
            }

            base.OnStop();
        }
    }
}
