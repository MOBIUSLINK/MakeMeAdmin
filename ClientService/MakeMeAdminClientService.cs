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
    using System.Security.Principal;
    using System.ServiceModel;
    using System.ServiceProcess;

    /// <summary>
    /// This class is the Windows Service for client-side privilege elevation.
    /// It provides local NamedPipe service for performing privilege elevation operations
    /// on the user's computer.
    /// </summary>
    /// <remarks>
    /// This service is installed on user computers and only provides NamedPipe service
    /// for local communication. It does NOT provide TCP service or handle pending requests.
    /// </remarks>
    public partial class MakeMeAdminClientService : ServiceBase
    {
        /// <summary>
        /// A timer to monitor when administrator rights should be removed.
        /// </summary>
        private System.Timers.Timer removalTimer;

        /// <summary>
        /// A Windows Communication Foundation (WCF) service host which communicates over named pipes.
        /// </summary>
        /// <remarks>
        /// This service host exists for communication on the local computer only.
        /// It is not accessible from remote computers.
        /// </remarks>
        private ServiceHost namedPipeServiceHost = null;

        /// <summary>
        /// Instantiate a new instance of the Make Me Admin Client Service.
        /// </summary>
        public MakeMeAdminClientService()
        {
            InitializeComponent();

            this.removalTimer = new System.Timers.Timer()
            {
                Interval = 5000,    // Every 5 seconds so expired users are removed soon after timeout (even if user app is closed).
                AutoReset = true
            };
            this.removalTimer.Elapsed += RemovalTimerElapsed;
        }

        /// <summary>
        /// Handles the Elapsed event for the rights removal timer.
        /// </summary>
        /// <param name="sender">
        /// The timer whose Elapsed event is firing.
        /// </param>
        /// <param name="e">
        /// Data related to the event.
        /// </param>
        private void RemovalTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                LocalAdministratorGroup.RunExpirationCheck();
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteEvent(
                    string.Format("Rights removal timer error: {0}", ex.Message),
                    EventID.DebugMessage,
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Creates the WCF Service Host which is accessible via named pipes.
        /// </summary>
        /// <remarks>
        /// This is the ONLY service host for the client service.
        /// It only provides local communication via NamedPipe.
        /// </remarks>
        private void OpenNamedPipeServiceHost()
        {
            this.namedPipeServiceHost = new ServiceHost(typeof(AdminGroupManipulator), new Uri(Shared.NamedPipeServiceBaseAddress));
            this.namedPipeServiceHost.Faulted += ServiceHostFaulted;
            NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.Transport);
            this.namedPipeServiceHost.AddServiceEndpoint(typeof(IAdminGroup), binding, Shared.NamedPipeServiceBaseAddress);
            this.namedPipeServiceHost.Open();
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

            // Create the Windows Event Log source for this application.
            ApplicationLog.CreateSource();

            // Open the service host which is accessible via named pipes (for local communication only).
            this.OpenNamedPipeServiceHost();

            // Start the timer that watches for expired administrator rights.
            this.removalTimer.Start();
        }

        /// <summary>
        /// Handles the stopping of the service.
        /// </summary>
        /// <remarks>
        /// Executes when a stop command is sent to the service by the Service Control Manager (SCM).
        /// </remarks>
        protected override void OnStop()
        {
            if ((this.namedPipeServiceHost != null) && (this.namedPipeServiceHost.State == CommunicationState.Opened))
            {
                this.namedPipeServiceHost.Close();
            }

            this.removalTimer.Stop();

            EncryptedSettings encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
            SecurityIdentifier[] sids = encryptedSettings.AddedUserSIDs;
            for (int i = 0; i < sids.Length; i++)
            {
                LocalAdministratorGroup.RemoveUser(sids[i], RemovalReason.ServiceStopped);
            }

            base.OnStop();
        }

        /// <summary>
        /// Executes when a change event is received from a Terminal Server session.
        /// </summary>
        /// <param name="changeDescription">
        /// Identifies the type of session change and the session to which it applies.
        /// </param>
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            switch (changeDescription.Reason)
            {
                // The user has logged off from a session, either locally or remotely.
                case SessionChangeReason.SessionLogoff:

                    EncryptedSettings encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
                    System.Collections.Generic.List<SecurityIdentifier> sidsToRemove = new System.Collections.Generic.List<SecurityIdentifier>(encryptedSettings.AddedUserSIDs);

                    int[] sessionIds = LsaLogonSessions.LogonSessions.GetLoggedOnUserSessionIds();

                    // For any user that is still logged on, remove their SID from the list of
                    // SIDs to be removed from Administrators. That is, let the users who are still
                    // logged on stay in the Administrators group.
                    foreach (int id in sessionIds)
                    {
                        SecurityIdentifier sid = LsaLogonSessions.LogonSessions.GetSidForSessionId(id);
                        if (sid != null)
                        {
                            if (sidsToRemove.Contains(sid))
                            {
                                sidsToRemove.Remove(sid);
                            }
                        }
                    }

                    // Process the list of SIDs to be removed from Administrators.
                    for (int i = 0; i < sidsToRemove.Count; i++)
                    {
                        if (
                            // If the user is not remote.
                            (!(encryptedSettings.ContainsSID(sidsToRemove[i]) && encryptedSettings.IsRemote(sidsToRemove[i])))
                            &&
                            // If admin rights are to be removed on logoff, or the user's rights do not expire.
                            (Settings.RemoveAdminRightsOnLogout || !encryptedSettings.GetExpirationTime(sidsToRemove[i]).HasValue)
                            )
                        {
                            LocalAdministratorGroup.RemoveUser(sidsToRemove[i], RemovalReason.UserLogoff);
                        }
                    }

                    break;

                // The user has logged on to a session, either locally or remotely.
                case SessionChangeReason.SessionLogon:

                    WindowsIdentity userIdentity = LsaLogonSessions.LogonSessions.GetWindowsIdentityForSessionId(changeDescription.SessionId);

                    if (userIdentity != null)
                    {
                        // If the user is in the automatic add list, then add them to the Administrators group.
                        if (
                            (Settings.AutomaticAddAllowed != null) &&
                            (Settings.AutomaticAddAllowed.Length > 0) &&
                            (Shared.UserIsAuthorized(userIdentity, Settings.AutomaticAddAllowed, Settings.AutomaticAddDenied))
                           )
                        {
                            LocalAdministratorGroup.AddUser(userIdentity, null, null);
                        }
                    }
                    else
                    {
                        ApplicationLog.WriteEvent(Properties.Resources.UserIdentifyIsNull, EventID.DebugMessage, System.Diagnostics.EventLogEntryType.Warning);
                    }

                    break;
            }

            base.OnSessionChange(changeDescription);
        }
    }
}
