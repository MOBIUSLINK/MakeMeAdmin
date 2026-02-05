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
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Security.Principal;

    /// <summary>
    /// Duplex implementation of the WCF service contract with callback support.
    /// This class manages client callback channels and sends notifications when request status changes.
    /// </summary>
    /// <remarks>
    /// This class extends AdminGroupManipulator with duplex communication capabilities.
    /// It maintains a dictionary of user SIDs to callback channels for real-time notifications.
    /// </remarks>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class AdminGroupManipulatorDuplex : AdminGroupManipulator, IAdminGroupDuplex
    {
        /// <summary>
        /// Dictionary to store callback channels for each user.
        /// Key: User SID (string), Value: Callback channel
        /// </summary>
        private static readonly Dictionary<string, IAdminGroupCallback> callbackChannels = new Dictionary<string, IAdminGroupCallback>();

        /// <summary>
        /// Lock object for thread-safe access to callbackChannels dictionary.
        /// </summary>
        private static readonly object callbackLock = new object();

        /// <summary>
        /// Registers the client for real-time notifications about request status changes.
        /// </summary>
        public void RegisterForNotifications()
        {
            try
            {
                WindowsIdentity userIdentity = null;
                if (ServiceSecurityContext.Current != null)
                {
                    userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
                }

                if (userIdentity != null)
                {
                    string userSid = userIdentity.User.ToString();
                    IAdminGroupCallback callbackChannel = OperationContext.Current.GetCallbackChannel<IAdminGroupCallback>();

                    lock (callbackLock)
                    {
                        // Remove existing callback if any
                        if (callbackChannels.ContainsKey(userSid))
                        {
                            callbackChannels.Remove(userSid);
                        }

                        // Add new callback channel
                        callbackChannels[userSid] = callbackChannel;

                        // Monitor channel for faults
                        ICommunicationObject commObj = callbackChannel as ICommunicationObject;
                        if (commObj != null)
                        {
                            commObj.Faulted += (sender, e) =>
                            {
                                lock (callbackLock)
                                {
                                    if (callbackChannels.ContainsKey(userSid) && callbackChannels[userSid] == callbackChannel)
                                    {
                                        callbackChannels.Remove(userSid);
                                    }
                                }
                            };

                            commObj.Closed += (sender, e) =>
                            {
                                lock (callbackLock)
                                {
                                    if (callbackChannels.ContainsKey(userSid) && callbackChannels[userSid] == callbackChannel)
                                    {
                                        callbackChannels.Remove(userSid);
                                    }
                                }
                            };
                        }
                    }

                    ApplicationLog.WriteEvent(
                        string.Format("Client registered for notifications: {0} (SID: {1})", userIdentity.Name, userSid),
                        EventID.RemoteRequestInformation,
                        System.Diagnostics.EventLogEntryType.Information);
                }
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteEvent(
                    string.Format("Error registering client for notifications: {0}", ex.Message),
                    EventID.RemoteRequestInformation,
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Unregisters the client from receiving notifications.
        /// </summary>
        public void UnregisterForNotifications()
        {
            try
            {
                WindowsIdentity userIdentity = null;
                if (ServiceSecurityContext.Current != null)
                {
                    userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
                }

                if (userIdentity != null)
                {
                    string userSid = userIdentity.User.ToString();

                    lock (callbackLock)
                    {
                        if (callbackChannels.ContainsKey(userSid))
                        {
                            callbackChannels.Remove(userSid);
                        }
                    }

                    ApplicationLog.WriteEvent(
                        string.Format("Client unregistered from notifications: {0} (SID: {1})", userIdentity.Name, userSid),
                        EventID.RemoteRequestInformation,
                        System.Diagnostics.EventLogEntryType.Information);
                }
            }
            catch (Exception ex)
            {
                ApplicationLog.WriteEvent(
                    string.Format("Error unregistering client from notifications: {0}", ex.Message),
                    EventID.RemoteRequestInformation,
                    System.Diagnostics.EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Sends approval notification to the client via callback channel.
        /// </summary>
        /// <param name="userSid">The SID of the user whose request was approved.</param>
        /// <param name="requestId">The unique identifier of the approved request.</param>
        private void NotifyClientApproved(string userSid, string requestId)
        {
            IAdminGroupCallback callback = null;

            lock (callbackLock)
            {
                if (callbackChannels.ContainsKey(userSid))
                {
                    callback = callbackChannels[userSid];
                }
            }

            if (callback != null)
            {
                try
                {
                    callback.OnRequestApproved(requestId);
                    ApplicationLog.WriteEvent(
                        string.Format("Approval notification sent to client: SID {0}, Request {1}", userSid, requestId),
                        EventID.RemoteRequestInformation,
                        System.Diagnostics.EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    // Channel may be faulted or closed, remove it
                    lock (callbackLock)
                    {
                        if (callbackChannels.ContainsKey(userSid) && callbackChannels[userSid] == callback)
                        {
                            callbackChannels.Remove(userSid);
                        }
                    }

                    ApplicationLog.WriteEvent(
                        string.Format("Error sending approval notification to client (SID: {0}): {1}", userSid, ex.Message),
                        EventID.RemoteRequestInformation,
                        System.Diagnostics.EventLogEntryType.Warning);
                }
            }
            else
            {
                ApplicationLog.WriteEvent(
                    string.Format("No callback channel found for user (SID: {0}), notification not sent. Client may need to poll.", userSid),
                    EventID.RemoteRequestInformation,
                    System.Diagnostics.EventLogEntryType.Warning);
            }
        }

        /// <summary>
        /// Sends rejection notification to the client via callback channel.
        /// </summary>
        /// <param name="userSid">The SID of the user whose request was rejected.</param>
        /// <param name="requestId">The unique identifier of the rejected request.</param>
        private void NotifyClientRejected(string userSid, string requestId)
        {
            IAdminGroupCallback callback = null;

            lock (callbackLock)
            {
                if (callbackChannels.ContainsKey(userSid))
                {
                    callback = callbackChannels[userSid];
                }
            }

            if (callback != null)
            {
                try
                {
                    callback.OnRequestRejected(requestId);
                    ApplicationLog.WriteEvent(
                        string.Format("Rejection notification sent to client: SID {0}, Request {1}", userSid, requestId),
                        EventID.RemoteRequestInformation,
                        System.Diagnostics.EventLogEntryType.Information);
                }
                catch (Exception ex)
                {
                    // Channel may be faulted or closed, remove it
                    lock (callbackLock)
                    {
                        if (callbackChannels.ContainsKey(userSid) && callbackChannels[userSid] == callback)
                        {
                            callbackChannels.Remove(userSid);
                        }
                    }

                    ApplicationLog.WriteEvent(
                        string.Format("Error sending rejection notification to client (SID: {0}): {1}", userSid, ex.Message),
                        EventID.RemoteRequestInformation,
                        System.Diagnostics.EventLogEntryType.Warning);
                }
            }
        }

        /// <summary>
        /// Approves a pending request and notifies the client.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to approve.
        /// </param>
        public new void ApproveRequest(string requestId)
        {
            ApproveRequestWithTimeout(requestId, null);
        }

        /// <summary>
        /// Approves a pending request with custom timeout and notifies the client.
        /// </summary>
        public new void ApproveRequestWithTimeout(string requestId, int? timeoutMinutes)
        {
            // Get the request details BEFORE removing it
            PendingRequestStorage storage = new PendingRequestStorage(PendingRequestStorage.PendingRequestsFilePath);
            PendingRequest request = storage.GetRequest(requestId);
            
            string userSid = null;
            if (request != null)
            {
                userSid = request.Sid != null ? request.Sid.ToString() : null;
            }

            // Call base implementation to remove the pending request and set timeout override
            base.ApproveRequestWithTimeout(requestId, timeoutMinutes);

            // If request was found, try to notify the client via callback
            if (!string.IsNullOrEmpty(userSid))
            {
                NotifyClientApproved(userSid, requestId);
            }
        }

        /// <summary>
        /// Gets the approved timeout in minutes for the current user.
        /// </summary>
        /// <returns>
        /// Returns the approved timeout in minutes if available, null otherwise.
        /// </returns>
        public new int? GetApprovedTimeout()
        {
            return base.GetApprovedTimeout();
        }

        /// <summary>
        /// Rejects a pending request and notifies the client.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to reject.
        /// </param>
        public new void RejectRequest(string requestId)
        {
            // Get the request details BEFORE removing it
            PendingRequestStorage storage = new PendingRequestStorage(PendingRequestStorage.PendingRequestsFilePath);
            PendingRequest request = storage.GetRequest(requestId);
            
            string userSid = null;
            if (request != null)
            {
                userSid = request.Sid != null ? request.Sid.ToString() : null;
            }

            // Call base implementation to remove the pending request
            base.RejectRequest(requestId);

            // If request was found, try to notify the client via callback
            if (!string.IsNullOrEmpty(userSid))
            {
                NotifyClientRejected(userSid, requestId);
            }
        }
    }
}
