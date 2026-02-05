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
    using System.Security.Principal;

    /// <summary>
    /// Maintains a collection of pending requests for administrator rights.
    /// </summary>
    public class PendingRequestList
    {
        /// <summary>
        /// A collection of pending requests.
        /// </summary>
        private Dictionary<Guid, PendingRequest> requests;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PendingRequestList()
        {
            this.requests = new Dictionary<Guid, PendingRequest>();
        }

        /// <summary>
        /// Adds a pending request to the collection.
        /// </summary>
        /// <param name="request">
        /// The pending request to add.
        /// </param>
        public void Add(PendingRequest request)
        {
            Guid requestId = new Guid(request.RequestId);
            if (!this.requests.ContainsKey(requestId))
            {
                this.requests.Add(requestId, request);
            }
        }

        /// <summary>
        /// Removes a pending request from the collection.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to remove.
        /// </param>
        public void Remove(string requestId)
        {
            Guid id = new Guid(requestId);
            if (this.requests.ContainsKey(id))
            {
                this.requests.Remove(id);
            }
        }

        /// <summary>
        /// Gets a pending request by its unique identifier.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request.
        /// </param>
        /// <returns>
        /// The pending request, or null if not found.
        /// </returns>
        public PendingRequest GetRequest(string requestId)
        {
            Guid id = new Guid(requestId);
            if (this.requests.ContainsKey(id))
            {
                return this.requests[id];
            }
            return null;
        }

        /// <summary>
        /// Gets all pending requests.
        /// </summary>
        /// <returns>
        /// An array of all pending requests.
        /// </returns>
        public PendingRequest[] GetAllRequests()
        {
            return this.requests.Values.ToArray();
        }

        /// <summary>
        /// Gets all pending requests for a specific user.
        /// </summary>
        /// <param name="sid">
        /// The security identifier of the user.
        /// </param>
        /// <returns>
        /// An array of pending requests for the user.
        /// </returns>
        public PendingRequest[] GetRequestsForUser(SecurityIdentifier sid)
        {
            return this.requests.Values.Where(r => r.Sid.Equals(sid)).ToArray();
        }

        /// <summary>
        /// Checks if a request exists for the given user.
        /// </summary>
        /// <param name="sid">
        /// The security identifier of the user.
        /// </param>
        /// <returns>
        /// True if a pending request exists for the user.
        /// </returns>
        public bool HasRequestForUser(SecurityIdentifier sid)
        {
            return this.requests.Values.Any(r => r.Sid.Equals(sid));
        }
    }
}
