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

    /// <summary>
    /// This interface defines the WCF service contract.
    /// </summary>
    /// <remarks>
    /// For duplex communication, use IAdminGroupDuplex instead.
    /// </remarks>
    [ServiceContract(Namespace = "http://apps.sinclair.edu/makemeadmin/2017/10/")]
    public interface IAdminGroup
    {
        /// <summary>
        /// Adds a user to the Administrators group.
        /// </summary>
        [OperationContract]
        void AddUserToAdministratorsGroup();

        /// <summary>
        /// Adds the current user to the Administrators group with the given approved timeout.
        /// Used by the user client when the server has approved a request with a specific duration.
        /// This avoids relying on HKLM registry write (which normal users cannot do).
        /// </summary>
        /// <param name="approvedTimeoutMinutes">Approved duration: 0 = permanent, &gt;0 = minutes, null = use registry/default.</param>
        [OperationContract]
        void AddUserToAdministratorsGroupWithTimeout(int? approvedTimeoutMinutes);

        /// <summary>
        /// Removes a user from the Administrators group.
        /// </summary>
        /// <param name="reason">
        /// The reason that the user is being removed.
        /// </param>
        [OperationContract]
        void RemoveUserFromAdministratorsGroup(RemovalReason reason);

        /// <summary>
        /// Returns a value indicating whether a user is 
        /// already in the list of added users.
        /// </summary>
        /// <returns>
        /// Returns true if the users is already in the list
        /// of added users.
        /// </returns>
        [OperationContract]
        bool UserIsInList();

        /// <summary>
        /// Gets all pending requests for administrator rights.
        /// </summary>
        /// <returns>
        /// Returns an array of pending requests.
        /// </returns>
        [OperationContract]
        PendingRequest[] GetPendingRequests();

        /// <summary>
        /// Approves a pending request for administrator rights.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to approve.
        /// </param>
        [OperationContract]
        void ApproveRequest(string requestId);

        /// <summary>
        /// Approves a pending request for administrator rights with custom timeout.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to approve.
        /// </param>
        /// <param name="timeoutMinutes">
        /// The timeout in minutes for the administrator rights. If null, uses the requested timeout.
        /// </param>
        [OperationContract]
        void ApproveRequestWithTimeout(string requestId, int? timeoutMinutes);

        /// <summary>
        /// Rejects a pending request for administrator rights.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to reject.
        /// </param>
        [OperationContract]
        void RejectRequest(string requestId);

        /// <summary>
        /// Checks if the current user has a pending request.
        /// </summary>
        /// <returns>
        /// Returns true if the current user has a pending request.
        /// </returns>
        [OperationContract]
        bool HasPendingRequest();

        /// <summary>
        /// Gets the approved timeout in minutes for the current user.
        /// </summary>
        /// <returns>
        /// Returns the approved timeout in minutes if available, null otherwise.
        /// </returns>
        [OperationContract]
        int? GetApprovedTimeout();

        /// <summary>
        /// Gets the expiration time for the current user's administrator rights (from the service's store).
        /// Used by the user client to show countdown; the service stores data in SYSTEM profile, so the client cannot read it directly.
        /// </summary>
        /// <returns>Expiration time if the user has a time-limited grant, null if permanent or not in list.</returns>
        [OperationContract]
        DateTime? GetMyExpirationTime();

        /// <summary>
        /// Triggers an immediate expiration check on the local service so that any expired users are removed.
        /// Called by the user client on exit so that privilege revocation does not depend on the user app staying open.
        /// </summary>
        [OperationContract]
        void RunExpirationCheckNow();
    }
}
