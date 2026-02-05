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
    /// Duplex service contract for real-time request status notifications.
    /// </summary>
    /// <remarks>
    /// This interface extends IAdminGroup with duplex communication capabilities,
    /// allowing the server to push notifications to clients when request status changes.
    /// 
    /// Usage:
    /// 1. Client calls RegisterForNotifications() to subscribe to status updates
    /// 2. Server stores the callback channel
    /// 3. When request is approved/rejected, server calls the callback
    /// 4. Client receives notification and performs appropriate action
    /// </remarks>
    [ServiceContract(Namespace = "http://apps.sinclair.edu/makemeadmin/2017/10/", CallbackContract = typeof(IAdminGroupCallback))]
    public interface IAdminGroupDuplex : IAdminGroup
    {
        /// <summary>
        /// Registers the client for real-time notifications about request status changes.
        /// </summary>
        /// <remarks>
        /// After calling this method, the server will use the callback channel to notify
        /// the client when their request is approved or rejected.
        /// 
        /// The client should keep the channel open to receive notifications.
        /// If the channel is closed, the client should re-register.
        /// </remarks>
        [OperationContract]
        void RegisterForNotifications();

        /// <summary>
        /// Unregisters the client from receiving notifications.
        /// </summary>
        /// <remarks>
        /// Call this method when the client no longer needs notifications,
        /// or before closing the application.
        /// </remarks>
        [OperationContract]
        void UnregisterForNotifications();
    }
}
