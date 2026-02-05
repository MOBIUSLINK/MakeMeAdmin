// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//

namespace SinclairCC.MakeMeAdmin
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Description;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Security;
    using System.Net.Security;

    /// <summary>
    /// WCF diagnostic utilities for troubleshooting connection issues.
    /// </summary>
    public static class WCFDiagnostics
    {
        /// <summary>
        /// Creates a TCP channel with detailed diagnostics enabled.
        /// </summary>
        public static Tuple<ChannelFactory<IAdminGroup>, IAdminGroup> CreateTcpChannelWithDiagnostics()
        {
            string serverAddress = Settings.ServerAddress;
            string tcpAddress = Shared.GetTcpServiceAddress(serverAddress);

            ClientLogger.Info("WCF Diagnostics", "Creating TCP channel with diagnostics", 
                string.Format("Server: {0}, Address: {1}, SecureMode: {2}", 
                serverAddress, tcpAddress, Settings.UseSecureMode));

            NetTcpBinding binding;
            if (Settings.UseSecureMode)
            {
                binding = new NetTcpBinding(SecurityMode.Transport);
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                
                // Enable detailed diagnostics
                binding.Security.Transport.ProtectionLevel = ProtectionLevel.EncryptAndSign;
                
                ClientLogger.Debug("WCF Diagnostics", "Binding configuration", 
                    string.Format("SecurityMode: Transport, ClientCredentialType: {0}, ProtectionLevel: {1}",
                    binding.Security.Transport.ClientCredentialType,
                    binding.Security.Transport.ProtectionLevel));
            }
            else
            {
                binding = new NetTcpBinding(SecurityMode.None);
                ClientLogger.Warning("WCF Diagnostics", "Using insecure mode", "SecurityMode.None");
            }

            // Log binding details
            LogBindingDetails(binding);

            ChannelFactory<IAdminGroup> factory = new ChannelFactory<IAdminGroup>(binding, tcpAddress);

            // Add message inspector for diagnostics
            factory.Endpoint.Behaviors.Add(new DiagnosticMessageInspector());

            IAdminGroup channel = factory.CreateChannel();

            // Open the channel with detailed error handling
            ICommunicationObject commObj = channel as ICommunicationObject;
            if (commObj != null)
            {
                try
                {
                    ClientLogger.Debug("WCF Diagnostics", "Channel state before open", 
                        string.Format("State: {0}", commObj.State));

                    if (commObj.State == CommunicationState.Created)
                    {
                        commObj.Open();
                        ClientLogger.Info("WCF Diagnostics", "Channel opened successfully", 
                            string.Format("State: {0}", commObj.State));
                    }
                    else if (commObj.State == CommunicationState.Faulted)
                    {
                        ClientLogger.Warning("WCF Diagnostics", "Channel is faulted", 
                            string.Format("State: {0}", commObj.State));
                        throw new InvalidOperationException("Channel is in faulted state");
                    }
                }
                catch (MessageSecurityException ex)
                {
                    LogSecurityException(ex, "MessageSecurityException");
                    throw;
                }
                catch (SecurityNegotiationException ex)
                {
                    LogSecurityException(ex, "SecurityNegotiationException");
                    throw;
                }
                catch (Exception ex)
                {
                    ClientLogger.Error("WCF Diagnostics", "Failed to open channel", 
                        string.Format("Exception Type: {0}, Message: {1}, StackTrace: {2}",
                        ex.GetType().Name, ex.Message, ex.StackTrace));
                    throw;
                }
            }

            return Tuple.Create(factory, channel);
        }

        /// <summary>
        /// Logs binding configuration details.
        /// </summary>
        private static void LogBindingDetails(NetTcpBinding binding)
        {
            try
            {
                var details = new System.Text.StringBuilder();
                details.AppendLine("Binding Configuration:");
                details.AppendFormat("  Security Mode: {0}", binding.Security.Mode);
                details.AppendLine();
                
                if (binding.Security.Mode == SecurityMode.Transport)
                {
                    details.AppendFormat("  Transport ClientCredentialType: {0}", 
                        binding.Security.Transport.ClientCredentialType);
                    details.AppendLine();
                    details.AppendFormat("  Transport ProtectionLevel: {0}", 
                        binding.Security.Transport.ProtectionLevel);
                    details.AppendLine();
                }

                details.AppendFormat("  MaxReceivedMessageSize: {0}", binding.MaxReceivedMessageSize);
                details.AppendLine();
                details.AppendFormat("  MaxBufferSize: {0}", binding.MaxBufferSize);
                details.AppendLine();
                details.AppendFormat("  ReaderQuotas MaxStringContentLength: {0}", 
                    binding.ReaderQuotas.MaxStringContentLength);
                details.AppendLine();

                ClientLogger.Debug("WCF Diagnostics", "Binding details", details.ToString());
            }
            catch (Exception ex)
            {
                ClientLogger.Warning("WCF Diagnostics", "Failed to log binding details", ex.Message);
            }
        }

        /// <summary>
        /// Logs security exception details.
        /// </summary>
        private static void LogSecurityException(Exception ex, string exceptionType)
        {
            try
            {
                var details = new System.Text.StringBuilder();
                details.AppendFormat("Exception Type: {0}", exceptionType);
                details.AppendLine();
                details.AppendFormat("Message: {0}", ex.Message);
                details.AppendLine();
                details.AppendFormat("HResult: 0x{0:X8}", ex.HResult);
                details.AppendLine();

                if (ex.InnerException != null)
                {
                    details.AppendLine("Inner Exception:");
                    details.AppendFormat("  Type: {0}", ex.InnerException.GetType().Name);
                    details.AppendLine();
                    details.AppendFormat("  Message: {0}", ex.InnerException.Message);
                    details.AppendLine();

                    if (ex.InnerException.InnerException != null)
                    {
                        details.AppendLine("Inner Exception 2:");
                        details.AppendFormat("  Type: {0}", ex.InnerException.InnerException.GetType().Name);
                        details.AppendLine();
                        details.AppendFormat("  Message: {0}", ex.InnerException.InnerException.Message);
                        details.AppendLine();
                    }
                }

                details.AppendLine("StackTrace:");
                details.Append(ex.StackTrace);

                ClientLogger.Error("WCF Diagnostics", exceptionType, details.ToString());
            }
            catch
            {
                // Silently fail if logging fails
            }
        }

        /// <summary>
        /// Message inspector for diagnostics.
        /// </summary>
        private class DiagnosticMessageInspector : IEndpointBehavior, IClientMessageInspector
        {
            public void AddBindingParameters(ServiceEndpoint endpoint, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
            {
            }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.ClientRuntime clientRuntime)
            {
                clientRuntime.MessageInspectors.Add(this);
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, System.ServiceModel.Dispatcher.EndpointDispatcher endpointDispatcher)
            {
            }

            public void Validate(ServiceEndpoint endpoint)
            {
            }

            public object BeforeSendRequest(ref Message request, IClientChannel channel)
            {
                try
                {
                    ClientLogger.Debug("WCF Diagnostics", "BeforeSendRequest", 
                        string.Format("Action: {0}, Headers: {1}", 
                        request.Headers.Action, request.Headers.Count));
                }
                catch
                {
                    // Silently fail
                }
                return null;
            }

            public void AfterReceiveReply(ref Message reply, object correlationState)
            {
                try
                {
                    ClientLogger.Debug("WCF Diagnostics", "AfterReceiveReply", 
                        string.Format("Action: {0}, IsFault: {1}", 
                        reply.Headers.Action, reply.IsFault));
                }
                catch
                {
                    // Silently fail
                }
            }
        }
    }
}
