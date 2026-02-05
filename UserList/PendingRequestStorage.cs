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
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// This class stores pending requests in an encrypted file.
    /// </summary>
    [Serializable]
    [XmlRootAttribute("pendingRequests")]
    public class PendingRequestStorage
    {
        /// <summary>
        /// The path of the file containing the pending requests.
        /// </summary>
        private string filePath;

        /// <summary>
        /// The list of pending requests.
        /// </summary>
        [XmlArray("requests")]
        [XmlArrayItem("request")]
        public List<PendingRequest> Requests { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constructor is used by the serialization process.
        /// </remarks>
        private PendingRequestStorage()
        {
            this.filePath = PendingRequestStorage.PendingRequestsFilePath;
            if (this.Requests == null)
            {
                this.Requests = new List<PendingRequest>();
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">
        /// The path of a file containing an XML-serialized PendingRequestStorage object.
        /// </param>
        public PendingRequestStorage(string filePath)
        {
            if (this.Requests == null)
            {
                this.Requests = new List<PendingRequest>();
            }
            this.filePath = filePath;
            try
            {
                if (File.Exists(filePath))
                {
                    this.Load(filePath);
                }
                else
                {
                    if (this.Requests == null)
                    {
                        this.Requests = new List<PendingRequest>();
                    }
                    // Ensure directory exists before saving
                    string directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    this.Save(filePath);
                }
            }
            catch (Exception ex)
            {
                // If loading fails (e.g., decryption error, file corruption), initialize with empty list
                // This prevents the service from crashing when the file is corrupted or encrypted with different key
                this.Requests = new List<PendingRequest>();
                
                // Log the error for debugging
                try
                {
                    System.Diagnostics.EventLog.WriteEntry(
                        "Application",
                        string.Format("Make Me Admin: Failed to load pending requests from {0}. Error: {1}\nStackTrace: {2}", 
                            filePath, ex.Message, ex.StackTrace),
                        System.Diagnostics.EventLogEntryType.Warning);
                }
                catch
                {
                    // Ignore event log errors
                }
            }
        }

        /// <summary>
        /// Gets the path of the file in which pending requests are stored.
        /// </summary>
        public static string PendingRequestsFilePath
        {
            get
            {
                const string PendingRequestsFile = "pendingRequests.xml";
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Make Me Admin");
                Directory.CreateDirectory(filePath);
                filePath = Path.Combine(filePath, PendingRequestsFile);
                return filePath;
            }
        }

        /// <summary>
        /// Adds a pending request.
        /// </summary>
        /// <param name="request">
        /// The pending request to add.
        /// </param>
        public void AddRequest(PendingRequest request)
        {
            if (!this.Requests.Any(r => r.RequestId == request.RequestId))
            {
                this.Requests.Add(request);
                this.Save();
            }
        }

        /// <summary>
        /// Removes a pending request.
        /// </summary>
        /// <param name="requestId">
        /// The unique identifier of the request to remove.
        /// </param>
        public void RemoveRequest(string requestId)
        {
            var request = this.Requests.FirstOrDefault(r => r.RequestId == requestId);
            if (request != null)
            {
                this.Requests.Remove(request);
                this.Save();
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
            return this.Requests.FirstOrDefault(r => r.RequestId == requestId);
        }

        /// <summary>
        /// Gets all pending requests.
        /// </summary>
        /// <returns>
        /// An array of all pending requests.
        /// </returns>
        public PendingRequest[] GetAllRequests()
        {
            return this.Requests.ToArray();
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
            return this.Requests.Where(r => r.Sid.Equals(sid)).ToArray();
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
            return this.Requests.Any(r => r.Sid.Equals(sid));
        }

        /// <summary>
        /// Gets an XML serializer for the PendingRequestStorage class.
        /// </summary>
        public static XmlSerializer Serializer
        {
            get
            {
                XmlSerializer serializer = new XmlSerializer(typeof(PendingRequestStorage));
                return serializer;
            }
        }

        /// <summary>
        /// Saves the pending requests to the default file path.
        /// </summary>
        private void Save()
        {
            this.Save(PendingRequestStorage.PendingRequestsFilePath);
        }

        /// <summary>
        /// Saves the pending requests to the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The path of the file to which pending requests are to be saved.
        /// </param>
        private void Save(string filePath)
        {
            try
            {
                // Ensure directory exists before saving
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize the current object to a memory stream.
                MemoryStream plaintextStream = new MemoryStream();
                XmlTextWriter plaintextWriter = new XmlTextWriter(plaintextStream, System.Text.Encoding.Unicode);
                XmlSerializer serializer = PendingRequestStorage.Serializer;
                lock (serializer)
                {
                    plaintextWriter.Indentation = 0;
                    plaintextWriter.Formatting = Formatting.None;
                    serializer.Serialize(plaintextWriter, this);
                }

                // Convert the plaintext memory stream to an array of bytes.
                byte[] plaintextBytes = plaintextStream.ToArray();

                // Encrypt the plaintext byte array.
                byte[] ciphertextBytes = ProtectedData.Protect(plaintextBytes, null, DataProtectionScope.LocalMachine);

                plaintextWriter.Close();
                plaintextWriter.Dispose();

                // Write the encrypted byte array to the file.
                FileStream ciphertextStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                try
                {
                    ciphertextStream.Write(ciphertextBytes, 0, ciphertextBytes.Length);
                    ciphertextStream.Flush();
                }
                finally
                {
                    ciphertextStream.Close();
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
        }

        /// <summary>
        /// Loads pending requests from the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The path of the file from which pending requests should be loaded.
        /// </param>
        private void Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    byte[] buffer = new byte[128];
                    int bytesRead = int.MinValue;

                    // Read the encrypted bytes from the file.
                    MemoryStream ciphertextMemoryStream = new MemoryStream();
                    FileStream ciphertextFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        while ((bytesRead = ciphertextFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ciphertextMemoryStream.Write(buffer, 0, bytesRead);
                        }
                    }
                    finally
                    {
                        ciphertextFileStream.Close();
                    }

                    // Convert the encrypted bytes to an array.
                    byte[] ciphertextBytes = ciphertextMemoryStream.ToArray();
                    ciphertextMemoryStream.Close();

                    // Decrypt the byte array.
                    byte[] plaintextBytes = null;
                    try
                    {
                        plaintextBytes = ProtectedData.Unprotect(ciphertextBytes, null, DataProtectionScope.LocalMachine);
                    }
                    catch (CryptographicException)
                    {
                        // Decryption failed - file may be encrypted with different user's key
                        // This can happen if service runs under different account than the one that created the file
                        throw new InvalidOperationException("Failed to decrypt pending requests file. The file may be encrypted with a different user's key.");
                    }

                    // Deserialize the plaintext byte array.
                    PendingRequestStorage deserializedStorage = null;
                    MemoryStream plaintextStream = new MemoryStream(plaintextBytes);
                    XmlTextReader reader = new XmlTextReader(plaintextStream);
                    try
                    {
                        XmlSerializer serializer = PendingRequestStorage.Serializer;
                        lock (serializer)
                        {
                            deserializedStorage = (PendingRequestStorage)serializer.Deserialize(reader);
                        }
                    }
                    finally
                    {
                        reader.Close();
                        plaintextStream.Close();
                    }

                    if (deserializedStorage != null && deserializedStorage.Requests != null)
                    {
                        this.Requests = deserializedStorage.Requests;
                    }
                    else
                    {
                        this.Requests = new List<PendingRequest>();
                    }
                }
                catch (Exception ex)
                {
                    // If loading fails, initialize with empty list and log the error
                    this.Requests = new List<PendingRequest>();
                    throw; // Re-throw to be handled by constructor
                }
            }
            else
            {
                this.Requests = new List<PendingRequest>();
                // Ensure directory exists before saving
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                this.Save(filePath);
            }
        }
    }
}
