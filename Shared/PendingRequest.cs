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
    using System.Runtime.Serialization;
    using System.Security.Principal;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Represents a pending request for administrator rights that requires approval.
    /// </summary>
    [Serializable]
    [DataContract(Namespace = "http://apps.sinclair.edu/makemeadmin/2017/10/")]
    [XmlRoot(ElementName = "pendingRequest")]
    public class PendingRequest
    {
        /// <summary>
        /// The security identifier (SID) of the user requesting admin rights.
        /// </summary>
        private SecurityIdentifier userSecurityIdentifier;

        /// <summary>
        /// The name of the user (e.g., DOMAIN\UserName).
        /// </summary>
        private string userName;

        /// <summary>
        /// The date and time at which the request was submitted.
        /// </summary>
        private DateTime requestDateTime;

        /// <summary>
        /// The address of the remote computer from which a request for
        /// admin rights came, if applicable.
        /// </summary>
        private string remoteHostAddress;

        /// <summary>
        /// The requested timeout in minutes.
        /// </summary>
        private int requestedTimeoutMinutes;

        /// <summary>
        /// Unique identifier for this request.
        /// </summary>
        private Guid requestId;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constructor only exists to support serialization.
        /// WCF DataContractSerializer requires a public parameterless constructor.
        /// </remarks>
        public PendingRequest()
        {
            this.requestId = Guid.NewGuid();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userIdentity">
        /// The identity of the user requesting admin rights.
        /// </param>
        /// <param name="requestedTimeoutMinutes">
        /// The requested timeout in minutes.
        /// </param>
        /// <param name="remoteHostAddress">
        /// The address from which the remote administrator rights request came.
        /// </param>
        public PendingRequest(WindowsIdentity userIdentity, int requestedTimeoutMinutes, string remoteHostAddress)
        {
            this.userSecurityIdentifier = userIdentity.User;
            this.userName = userIdentity.Name;
            this.requestDateTime = DateTime.Now;
            this.requestedTimeoutMinutes = requestedTimeoutMinutes;
            this.remoteHostAddress = remoteHostAddress;
            this.requestId = Guid.NewGuid();
        }

        /// <summary>
        /// Gets the security identifier (SID) of the user.
        /// </summary>
        [XmlIgnore]
        public SecurityIdentifier Sid
        {
            get { return this.userSecurityIdentifier; }
        }

        /// <summary>
        /// Gets or sets the security identifier (SID) of the user, in SDDL format.
        /// </summary>
        [DataMember(Name = "SidString", Order = 1)]
        [XmlAttribute("sid")]
        public string SidString
        {
            get { return this.userSecurityIdentifier != null ? this.userSecurityIdentifier.Value : null; }
            set { this.userSecurityIdentifier = value != null ? new SecurityIdentifier(value) : null; }
        }

        /// <summary>
        /// Gets the name of the user (e.g., DOMAIN\UserName).
        /// </summary>
        [DataMember(Name = "UserName", Order = 2)]
        [XmlElement(ElementName = "userName")]
        public string UserName
        {
            get { return this.userName; }
            set { this.userName = value; }
        }

        /// <summary>
        /// Gets or sets the date and time at which the request was submitted.
        /// </summary>
        [DataMember(Name = "RequestDateTime", Order = 3)]
        [XmlElement(ElementName = "requestDateTime")]
        public DateTime RequestDateTime
        {
            get { return this.requestDateTime; }
            set { this.requestDateTime = value; }
        }

        /// <summary>
        /// Gets or sets the address from which a remote request for
        /// administrator rights came.
        /// </summary>
        [DataMember(Name = "RemoteAddress", Order = 4)]
        [XmlElement(ElementName = "remoteAddress")]
        public string RemoteAddress
        {
            get { return this.remoteHostAddress; }
            set { this.remoteHostAddress = value; }
        }

        /// <summary>
        /// Gets or sets the requested timeout in minutes.
        /// </summary>
        [DataMember(Name = "RequestedTimeoutMinutes", Order = 5)]
        [XmlElement(ElementName = "requestedTimeoutMinutes")]
        public int RequestedTimeoutMinutes
        {
            get { return this.requestedTimeoutMinutes; }
            set { this.requestedTimeoutMinutes = value; }
        }

        /// <summary>
        /// Gets or sets the unique identifier for this request.
        /// </summary>
        [DataMember(Name = "RequestId", Order = 6)]
        [XmlAttribute("requestId")]
        public string RequestId
        {
            get { return this.requestId.ToString(); }
            set { this.requestId = !string.IsNullOrEmpty(value) ? new Guid(value) : Guid.NewGuid(); }
        }
    }
}
