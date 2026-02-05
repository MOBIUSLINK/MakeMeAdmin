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
    using System.Windows.Forms;

    /// <summary>
    /// Callback implementation for receiving real-time request status notifications from the server.
    /// </summary>
    /// <remarks>
    /// This class implements IAdminGroupCallback to receive push notifications when
    /// a request is approved or rejected, eliminating the need for frequent polling.
    /// </remarks>
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Single, UseSynchronizationContext = true)]
    internal class RequestStatusCallback : IAdminGroupCallback
    {
        /// <summary>
        /// Reference to the form that will handle the UI updates.
        /// </summary>
        private SubmitRequestForm form;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="form">
        /// The form that will handle UI updates when notifications are received.
        /// </param>
        public RequestStatusCallback(SubmitRequestForm form)
        {
            this.form = form;
        }

        /// <summary>
        /// Called by the server when a request has been approved.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the approved request.
        /// </param>
        public void OnRequestApproved(string requestId)
        {
            try
            {
                ClientLogger.Info("Callback", "Request approved notification received", 
                    string.Format("Request ID: {0}", requestId));

                // Update the form on the UI thread
                if (this.form != null && !this.form.IsDisposed)
                {
                    if (this.form.InvokeRequired)
                    {
                        this.form.Invoke(new Action(() =>
                        {
                            this.form.HandleRequestApproved(requestId);
                        }));
                    }
                    else
                    {
                        this.form.HandleRequestApproved(requestId);
                    }
                }
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Callback", "Error handling approval notification", ex.ToString());
            }
        }

        /// <summary>
        /// Called by the server when a request has been rejected.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the rejected request.
        /// </param>
        public void OnRequestRejected(string requestId)
        {
            try
            {
                ClientLogger.Info("Callback", "Request rejected notification received", 
                    string.Format("Request ID: {0}", requestId));

                // Update the form on the UI thread
                if (this.form != null && !this.form.IsDisposed)
                {
                    if (this.form.InvokeRequired)
                    {
                        this.form.Invoke(new Action(() =>
                        {
                            this.form.HandleRequestRejected(requestId);
                        }));
                    }
                    else
                    {
                        this.form.HandleRequestRejected(requestId);
                    }
                }
            }
            catch (Exception ex)
            {
                ClientLogger.Error("Callback", "Error handling rejection notification", ex.ToString());
            }
        }
    }
}
