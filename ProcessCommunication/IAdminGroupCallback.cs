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
    using System.ServiceModel;

    /// <summary>
    /// Callback interface for WCF duplex communication.
    /// Allows the server to notify clients about request status changes.
    /// </summary>
    /// <remarks>
    /// This callback interface enables push notifications from server to client,
    /// eliminating the need for frequent polling and providing real-time updates.
    /// </remarks>
    [ServiceContract(Namespace = "http://apps.sinclair.edu/makemeadmin/2017/10/")]
    public interface IAdminGroupCallback
    {
        /// <summary>
        /// Notifies the client that their request has been approved.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the approved request.
        /// </param>
        /// <remarks>
        /// When this callback is invoked, the client should perform local privilege elevation
        /// by calling AddUserToAdministratorsGroup() on the local ClientService.
        /// </remarks>
        [OperationContract(IsOneWay = true)]
        void OnRequestApproved(string requestId);

        /// <summary>
        /// Notifies the client that their request has been rejected.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the rejected request.
        /// </param>
        [OperationContract(IsOneWay = true)]
        void OnRequestRejected(string requestId);
    }
}
