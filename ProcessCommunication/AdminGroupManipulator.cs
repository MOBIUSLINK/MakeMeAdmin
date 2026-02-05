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
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.Security.Principal;
    using LsaLogonSessions;

    /// <summary>
    /// This class implements the WCF service contract.
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = false)]
    public class AdminGroupManipulator : IAdminGroup
    {
        /// <summary>
        /// Adds a user to the local Administrators group.
        /// </summary>
        public void AddUserToAdministratorsGroup()
        {
            string remoteAddress = null;

            WindowsIdentity userIdentity = null;

            if (ServiceSecurityContext.Current != null)
            {
                userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
            }

            if (OperationContext.Current != null)
            {
                if (OperationContext.Current.IncomingMessageProperties != null)
                {
                    if (OperationContext.Current.IncomingMessageProperties.ContainsKey(RemoteEndpointMessageProperty.Name))
                    {
                        remoteAddress = ((RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name]).Address;
                        if (remoteAddress != null)
                        {
                            ApplicationLog.WriteEvent(string.Format(Properties.Resources.RequestSentFromHost, remoteAddress), EventID.RemoteRequestInformation, System.Diagnostics.EventLogEntryType.Information);
                        }

                    }
                }
            }

            if (userIdentity != null)
            {
                // Determine if this is a client-side call (via NamedPipe) or server-side call (via TCP)
                bool isClientSideCall = false;
                if (OperationContext.Current != null && OperationContext.Current.IncomingMessageProperties != null)
                {
                    // Check if the endpoint is NamedPipe (client-side) or TCP (server-side)
                    var endpoint = OperationContext.Current.EndpointDispatcher?.EndpointAddress?.Uri;
                    if (endpoint != null && endpoint.Scheme == "net.pipe")
                    {
                        isClientSideCall = true;
                    }
                }
                
                if (isClientSideCall)
                {
                    // Client-side service: Always directly add user (request was already approved on server)
                    // This is called from UserRequestApp after detecting approval from server
                    int timeoutMinutes = Shared.GetTimeoutForUser(userIdentity);
                    
                    // If timeoutMinutes is 0, it means permanent (no expiration)
                    // Otherwise, calculate expiration time
                    DateTime? expirationTime = null;
                    if (timeoutMinutes > 0)
                    {
                        expirationTime = DateTime.Now.AddMinutes(timeoutMinutes);
                    }
                    // If timeoutMinutes is 0, expirationTime remains null (permanent)
                    
                    LocalAdministratorGroup.AddUser(userIdentity, expirationTime, remoteAddress);
                    
                    if (timeoutMinutes == 0)
                    {
                        ApplicationLog.WriteEvent(
                            string.Format("User {0} added to Administrators group via client service (permanent, no expiration)", userIdentity.Name),
                            EventID.RemoteRequestInformation,
                            System.Diagnostics.EventLogEntryType.Information);
                    }
                    else
                    {
                        ApplicationLog.WriteEvent(
                            string.Format("User {0} added to Administrators group via client service (timeout: {1} minutes, expires: {2})", 
                                userIdentity.Name, timeoutMinutes, expirationTime.Value),
                            EventID.RemoteRequestInformation,
                            System.Diagnostics.EventLogEntryType.Information);
                    }
                }
                else
                {
                    // Server-side service: Check if approval is required
                    if (Settings.RequireApproval)
                    {
                        // Create a pending request instead of directly adding the user
                        int timeoutMinutes = Shared.GetTimeoutForUser(userIdentity);
                        PendingRequest request = new PendingRequest(userIdentity, timeoutMinutes, remoteAddress);
                        
                        PendingRequestStorage storage = new PendingRequestStorage(PendingRequestStorage.PendingRequestsFilePath);
                        storage.AddRequest(request);
                        
                        ApplicationLog.WriteEvent(
                            string.Format("Pending request created for user {0} (Request ID: {1})", userIdentity.Name, request.RequestId),
                            EventID.RemoteRequestInformation,
                            System.Diagnostics.EventLogEntryType.Information);
                    }
                    else
                    {
                        // Directly add user if approval is not required
                        int timeoutMinutes = Shared.GetTimeoutForUser(userIdentity);
                        
                        // If timeoutMinutes is 0, it means permanent (no expiration)
                        // Otherwise, calculate expiration time
                        DateTime? expirationTime = null;
                        if (timeoutMinutes > 0)
                        {
                            expirationTime = DateTime.Now.AddMinutes(timeoutMinutes);
                        }
                        // If timeoutMinutes is 0, expirationTime remains null (permanent)
                        
                        LocalAdministratorGroup.AddUser(userIdentity, expirationTime, remoteAddress);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the current user to the Administrators group with the given approved timeout.
        /// Used by the user client so the service does not depend on HKLM Timeout Overrides (which the user cannot write).
        /// </summary>
        /// <param name="approvedTimeoutMinutes">Approved duration: 0 = permanent, &gt;0 = minutes, null = use GetTimeoutForUser (registry/default).</param>
        public void AddUserToAdministratorsGroupWithTimeout(int? approvedTimeoutMinutes)
        {
            string remoteAddress = null;
            WindowsIdentity userIdentity = null;

            if (ServiceSecurityContext.Current != null)
            {
                userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
            }

            if (OperationContext.Current != null && OperationContext.Current.IncomingMessageProperties != null
                && OperationContext.Current.IncomingMessageProperties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                remoteAddress = ((RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name]).Address;
            }

            if (userIdentity == null)
                return;

            bool isClientSideCall = false;
            if (OperationContext.Current != null && OperationContext.Current.IncomingMessageProperties != null)
            {
                var endpoint = OperationContext.Current.EndpointDispatcher?.EndpointAddress?.Uri;
                if (endpoint != null && endpoint.Scheme == "net.pipe")
                    isClientSideCall = true;
            }

            if (!isClientSideCall)
            {
                // Server-side: ignore the parameter and use standard add (registry/default)
                AddUserToAdministratorsGroup();
                return;
            }

            // Client-side: use approvedTimeoutMinutes when provided, else fall back to registry
            int timeoutMinutes = approvedTimeoutMinutes.HasValue
                ? approvedTimeoutMinutes.Value
                : Shared.GetTimeoutForUser(userIdentity);

            DateTime? expirationTime = null;
            if (timeoutMinutes > 0)
                expirationTime = DateTime.Now.AddMinutes(timeoutMinutes);

            LocalAdministratorGroup.AddUser(userIdentity, expirationTime, remoteAddress);

            if (timeoutMinutes == 0)
            {
                ApplicationLog.WriteEvent(
                    string.Format("User {0} added to Administrators group via client service (permanent, approvedTimeout param)", userIdentity.Name),
                    EventID.RemoteRequestInformation,
                    System.Diagnostics.EventLogEntryType.Information);
            }
            else
            {
                ApplicationLog.WriteEvent(
                    string.Format("User {0} added to Administrators group via client service (timeout: {1} minutes, expires: {2})", userIdentity.Name, timeoutMinutes, expirationTime.Value),
                    EventID.RemoteRequestInformation,
                    System.Diagnostics.EventLogEntryType.Information);
            }
        }

        /// <summary>
        /// Removes a user from the local Administrators group.
        /// </summary>
        /// <param name="reason">
        /// The reason that the rights are being removed.
        /// </param>
        public void RemoveUserFromAdministratorsGroup(RemovalReason reason)
        {
            WindowsIdentity userIdentity = null;

            if (ServiceSecurityContext.Current != null)
            {
                userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
            }

            if (userIdentity != null)
            {
                LocalAdministratorGroup.RemoveUser(userIdentity.User, reason);
            }
        }

        /// <summary>
        /// Returns a value indicating whether a user is in the
        /// list of added users.
        /// </summary>
        /// <returns>
        /// Returns true if the given user is already in the list of added
        /// users. Otherwise, false is returned.
        /// </returns>
        public bool UserIsInList()
        {
            WindowsIdentity userIdentity = null;

            if (ServiceSecurityContext.Current != null)
            {
                userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
            }

            if (userIdentity != null)
            {
                EncryptedSettings encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
                return encryptedSettings.ContainsSID(userIdentity.User);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets all pending requests for administrator rights.
        /// </summary>
        /// <returns>
        /// Returns an array of pending requests.
        /// </returns>
        /// <remarks>
        /// This method should only be called from server-side service (TCP endpoint).
        /// Client-side service (NamedPipe endpoint) does not manage pending requests.
        /// </remarks>
        public PendingRequest[] GetPendingRequests()
        {
            try
            {
                // Only allow this call from server-side (TCP) endpoints
                // Client-side (NamedPipe) endpoints should not call this method
                if (OperationContext.Current != null && OperationContext.Current.EndpointDispatcher != null)
                {
                    var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress?.Uri;
                    if (endpoint != null && endpoint.Scheme == "net.pipe")
                    {
                        // This is a client-side call - return empty array
                        ApplicationLog.WriteEvent(
                            "GetPendingRequests() called from client-side service (NamedPipe) - returning empty array",
                            EventID.RemoteRequestInformation,
                            System.Diagnostics.EventLogEntryType.Warning);
                        return new PendingRequest[0];
                    }
                }
                
                PendingRequestStorage storage = new PendingRequestStorage(PendingRequestStorage.PendingRequestsFilePath);
                return storage.GetAllRequests();
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                try
                {
                    ApplicationLog.WriteEvent(
                        string.Format("Error in GetPendingRequests(): {0}\nStackTrace: {1}", ex.Message, ex.StackTrace),
                        EventID.RemoteRequestInformation,
                        System.Diagnostics.EventLogEntryType.Error);
                }
                catch
                {
                    // If logging fails, try direct event log write
                    try
                    {
                        System.Diagnostics.EventLog.WriteEntry(
                            "Application",
                            string.Format("Make Me Admin: Error in GetPendingRequests(): {0}", ex.Message),
                            System.Diagnostics.EventLogEntryType.Error);
                    }
                    catch
                    {
                        // Ignore event log errors
                    }
                }
                
                // Return empty array instead of throwing exception to prevent connection abort
                // The client can handle empty array gracefully
                return new PendingRequest[0];
            }
        }

        /// <summary>
        /// Approves a pending request for administrator rights.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to approve.
        /// </param>
        /// <remarks>
        /// IMPORTANT: This method should only be called from server-side service (TCP endpoint).
        /// It only marks the request as approved in the server.
        /// The actual privilege elevation must be performed on the user's computer
        /// by the user's local service. The user client will detect the approval
        /// and call AddUserToAdministratorsGroup() on their local service.
        /// </remarks>
        public void ApproveRequest(string requestId)
        {
            ApproveRequestWithTimeout(requestId, null);
        }

        /// <summary>
        /// Approves a pending request for administrator rights with custom timeout.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to approve.
        /// </param>
        /// <param name="timeoutMinutes">
        /// The timeout in minutes for the administrator rights. If null, uses the requested timeout.
        /// </param>
        /// <remarks>
        /// IMPORTANT: This method should only be called from server-side service (TCP endpoint).
        /// It only marks the request as approved in the server.
        /// The actual privilege elevation must be performed on the user's computer
        /// by the user's local service. The user client will detect the approval
        /// and call AddUserToAdministratorsGroup() on their local service.
        /// 
        /// If a custom timeout is specified, it will be stored in Settings.TimeoutOverrides
        /// so that the user client will use it when performing local privilege elevation.
        /// </remarks>
        public void ApproveRequestWithTimeout(string requestId, int? timeoutMinutes)
        {
            PendingRequestStorage storage = new PendingRequestStorage(PendingRequestStorage.PendingRequestsFilePath);
            PendingRequest request = storage.GetRequest(requestId);
            
            if (request != null)
            {
                // Determine the timeout to use:
                // - If admin specified a custom timeout, use it (including 0 for permanent)
                // - Otherwise, use the requested timeout from the request
                int timeoutToUse = timeoutMinutes.HasValue 
                    ? timeoutMinutes.Value 
                    : request.RequestedTimeoutMinutes;
                
                // Always store the timeout in TimeoutOverrides (even if 0 for permanent)
                // This ensures the user client will use the correct timeout when performing local privilege elevation
                string userSid = request.Sid != null ? request.Sid.Value : null;
                if (!string.IsNullOrEmpty(userSid))
                {
                    Dictionary<string, string> overrides = Settings.TimeoutOverrides;
                    if (overrides == null)
                    {
                        overrides = new Dictionary<string, string>();
                    }
                    overrides[userSid] = timeoutToUse.ToString();
                    Settings.TimeoutOverrides = overrides;
                    
                    if (timeoutMinutes.HasValue)
                    {
                        ApplicationLog.WriteEvent(
                            string.Format("Custom timeout {0} minutes set for user {1} (SID: {2})", 
                                timeoutToUse, request.UserName, userSid),
                            EventID.RemoteRequestInformation,
                            System.Diagnostics.EventLogEntryType.Information);
                    }
                    else
                    {
                        ApplicationLog.WriteEvent(
                            string.Format("Using requested timeout {0} minutes for user {1} (SID: {2})", 
                                timeoutToUse, request.UserName, userSid),
                            EventID.RemoteRequestInformation,
                            System.Diagnostics.EventLogEntryType.Information);
                    }
                }
                
                // IMPORTANT: Do NOT add user to administrators group here!
                // The user is on a different computer, and privilege elevation
                // must happen on the user's computer, not on the server.
                // 
                // Instead, we just remove the pending request, which signals
                // to the user client that the request has been approved.
                // The user client will detect this (via HasPendingRequest() returning false
                // and userIsAdmin becoming true) and will call AddUserToAdministratorsGroup()
                // on their LOCAL service to perform the actual elevation.
                
                // Remove the pending request - this signals approval to the user client
                storage.RemoveRequest(requestId);
                
                string timeoutInfo = timeoutMinutes.HasValue 
                    ? string.Format(" (Custom timeout: {0} minutes)", timeoutMinutes.Value)
                    : string.Format(" (Requested timeout: {0} minutes)", request.RequestedTimeoutMinutes);
                
                ApplicationLog.WriteEvent(
                    string.Format("Request {0} approved for user {1} (User: {2}, Remote: {3}){4}. User client will perform local privilege elevation.",
                        requestId, request.UserName, request.Sid, request.RemoteAddress, timeoutInfo),
                    EventID.RemoteRequestInformation,
                    System.Diagnostics.EventLogEntryType.Information);
            }
        }

        /// <summary>
        /// Gets the approved timeout in minutes for the current user.
        /// </summary>
        /// <returns>
        /// Returns the approved timeout in minutes if available, null otherwise.
        /// </returns>
        public int? GetApprovedTimeout()
        {
            WindowsIdentity userIdentity = null;

            if (ServiceSecurityContext.Current != null)
            {
                userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
            }

            if (userIdentity != null && userIdentity.User != null)
            {
                Dictionary<string, string> overrides = Settings.TimeoutOverrides;
                if (overrides != null && overrides.ContainsKey(userIdentity.User.Value))
                {
                    int timeoutMinutes;
                    if (int.TryParse(overrides[userIdentity.User.Value], out timeoutMinutes))
                    {
                        // Return the timeout value (including 0 for permanent)
                        // Note: 0 means permanent, any positive value means timeout in minutes
                        return timeoutMinutes;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the expiration time for the current user's administrator rights from the service's EncryptedSettings.
        /// The user client cannot read the service's store (different path), so it calls this over NamedPipe.
        /// </summary>
        public DateTime? GetMyExpirationTime()
        {
            WindowsIdentity userIdentity = null;
            if (ServiceSecurityContext.Current != null)
                userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
            if (userIdentity == null || userIdentity.User == null)
                return null;
            try
            {
                var encryptedSettings = new EncryptedSettings(EncryptedSettings.SettingsFilePath);
                return encryptedSettings.GetExpirationTime(userIdentity.User);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Triggers an immediate expiration check so that any expired users are removed.
        /// Only runs when called via NamedPipe (client-side); no-op on server.
        /// </summary>
        public void RunExpirationCheckNow()
        {
            bool isClientSideCall = false;
            if (OperationContext.Current != null && OperationContext.Current.IncomingMessageProperties != null)
            {
                var endpoint = OperationContext.Current.EndpointDispatcher?.EndpointAddress?.Uri;
                if (endpoint != null && endpoint.Scheme == "net.pipe")
                    isClientSideCall = true;
            }
            if (isClientSideCall)
            {
                LocalAdministratorGroup.RunExpirationCheck();
            }
        }

        /// <summary>
        /// Rejects a pending request for administrator rights.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to reject.
        /// </param>
        public void RejectRequest(string requestId)
        {
            PendingRequestStorage storage = new PendingRequestStorage(PendingRequestStorage.PendingRequestsFilePath);
            PendingRequest request = storage.GetRequest(requestId);
            
            if (request != null)
            {
                // Remove the pending request
                storage.RemoveRequest(requestId);
                
                ApplicationLog.WriteEvent(
                    string.Format("Request {0} rejected for user {1}", requestId, request.UserName),
                    EventID.RemoteRequestInformation,
                    System.Diagnostics.EventLogEntryType.Information);
            }
        }

        /// <summary>
        /// Checks if the current user has a pending request.
        /// </summary>
        /// <returns>
        /// Returns true if the current user has a pending request.
        /// </returns>
        /// <remarks>
        /// This method should only be called from server-side service (TCP endpoint).
        /// Client-side service (NamedPipe endpoint) does not manage pending requests.
        /// </remarks>
        public bool HasPendingRequest()
        {
            // Only allow this call from server-side (TCP) endpoints
            // Client-side (NamedPipe) endpoints should not call this method
            if (OperationContext.Current != null && OperationContext.Current.EndpointDispatcher != null)
            {
                var endpoint = OperationContext.Current.EndpointDispatcher.EndpointAddress?.Uri;
                if (endpoint != null && endpoint.Scheme == "net.pipe")
                {
                    // This is a client-side call - return false
                    return false;
                }
            }
            
            WindowsIdentity userIdentity = null;

            if (ServiceSecurityContext.Current != null)
            {
                userIdentity = ServiceSecurityContext.Current.WindowsIdentity;
            }

            if (userIdentity != null)
            {
                PendingRequestStorage storage = new PendingRequestStorage(PendingRequestStorage.PendingRequestsFilePath);
                return storage.HasRequestForUser(userIdentity.User);
            }
            else
            {
                return false;
            }
        }
    }
}
