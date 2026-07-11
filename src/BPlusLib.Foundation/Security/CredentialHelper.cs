// <copyright file="CredentialHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.Security
{
    /// <summary>
    /// Type of credential stored in the Credential Manager.
    /// </summary>
    public enum CredentialType
    {
        /// <summary>Generic credential (username/password).</summary>
        Generic = 1,
        /// <summary>Domain password credential.</summary>
        DomainPassword = 2,
        /// <summary>Domain certificate credential.</summary>
        DomainCertificate = 3,
        /// <summary>Domain visible password credential.</summary>
        DomainVisiblePassword = 4,
        /// <summary>Generic certificate credential.</summary>
        GenericCertificate = 5,
        /// <summary>Domain extended credential.</summary>
        DomainExtended = 6,
    }

    /// <summary>
    /// Persistence type for a stored credential.
    /// </summary>
    public enum CredentialPersistence
    {
        /// <summary>Only valid for the current logon session.</summary>
        Session = 1,
        /// <summary>Persists on the local machine.</summary>
        LocalMachine = 2,
        /// <summary>Persists across the enterprise (roams with the user).</summary>
        Enterprise = 3,
    }

    /// <summary>
    /// Represents a credential stored in the Windows Credential Manager vault.
    /// </summary>
    public sealed class CredentialEntry
    {
        /// <summary>Target name (e.g., "myapp:user123").</summary>
        public string TargetName { get; init; } = string.Empty;
        /// <summary>Username or account name.</summary>
        public string? UserName { get; init; }
        /// <summary>Password (decoded from blob for Generic credentials).</summary>
        public string? Password { get; init; }
        /// <summary>Raw credential blob bytes.</summary>
        public byte[]? CredentialBlob { get; init; }
        /// <summary>Type of credential.</summary>
        public CredentialType Type { get; init; }
        /// <summary>Persistence setting.</summary>
        public CredentialPersistence Persist { get; init; }
        /// <summary>Optional comment.</summary>
        public string? Comment { get; init; }
        /// <summary>Last modified timestamp.</summary>
        public DateTime LastWritten { get; init; }
    }

    /// <summary>
    /// Provides read/write/enumerate/delete access to the Windows Credential Manager
    /// via pure P/Invoke. All methods are thread-safe and return null/false on error.
    /// </summary>
    public static class CredentialHelper
    {
        /// <summary>
        /// Reads a stored credential by target name.
        /// </summary>
        public static CredentialEntry? Read(
            string targetName,
            CredentialType type = CredentialType.Generic)
        {
            if (string.IsNullOrEmpty(targetName))
                return null;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            try
            {
                if (!AdvApi32.CredReadW(
                        targetName,
                        (uint)type,
                        0,
                        out IntPtr credPtr))
                    return null;

                try
                {
                    return MarshalCredential(credPtr);
                }
                finally
                {
                    AdvApi32.CredFree(credPtr);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Writes (creates or updates) a credential in the Credential Manager.
        /// </summary>
        /// <param name="targetName">Target name (e.g., "myapp:username").</param>
        /// <param name="userName">Account/user name.</param>
        /// <param name="password">Password (converted to ANSI blob for storage).</param>
        /// <param name="type">Credential type (default: Generic).</param>
        /// <param name="persist">Persistence (default: LocalMachine).</param>
        /// <param name="comment">Optional comment.</param>
        /// <returns>True if written successfully.</returns>
        public static bool Write(
            string targetName,
            string? userName,
            string? password,
            CredentialType type = CredentialType.Generic,
            CredentialPersistence persist = CredentialPersistence.LocalMachine,
            string? comment = null)
        {
            if (string.IsNullOrEmpty(targetName))
                return false;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                byte[] blob = Encoding.Unicode.GetBytes(password ?? string.Empty);
                IntPtr targetPtr = Marshal.StringToHGlobalUni(targetName);
                IntPtr userPtr = Marshal.StringToHGlobalUni(userName ?? string.Empty);
                IntPtr commentPtr = Marshal.StringToHGlobalUni(comment ?? string.Empty);

                try
                {
                    var cred = new CREDENTIALW
                    {
                        Type = (uint)type,
                        TargetName = targetPtr,
                        UserName = userPtr,
                        Comment = commentPtr,
                        CredentialBlobSize = (uint)blob.Length,
                        CredentialBlob = Marshal.AllocHGlobal(blob.Length),
                        Persist = (uint)persist,
                        Flags = 0,
                        AttributeCount = 0,
                        Attributes = IntPtr.Zero,
                        LastWritten = 0,
                        TargetAlias = IntPtr.Zero,
                    };

                    try
                    {
                        Marshal.Copy(blob, 0, cred.CredentialBlob, blob.Length);
                        return AdvApi32.CredWriteW(ref cred, 0);
                    }
                    finally
                    {
                        if (cred.CredentialBlob != IntPtr.Zero)
                            Marshal.FreeHGlobal(cred.CredentialBlob);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(targetPtr);
                    Marshal.FreeHGlobal(userPtr);
                    Marshal.FreeHGlobal(commentPtr);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Enumerates all stored credentials matching the optional filter.
        /// </summary>
        /// <param name="filter">Optional filter string (e.g., "myapp*"). Pass null to enumerate all.</param>
        /// <returns>List of matching credentials.</returns>
        public static List<CredentialEntry> Enumerate(string? filter = null)
        {
            var results = new List<CredentialEntry>();

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return results;

            try
            {
                if (!AdvApi32.CredEnumerateW(
                        filter,
                        AdvApi32.CRED_ENUMERATE_ALL_CREDENTIALS,
                        out int count,
                        out IntPtr credArray))
                    return results;

                try
                {
                    int ptrSize = IntPtr.Size;
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr credPtr = Marshal.ReadIntPtr(credArray + (i * ptrSize));
                        if (credPtr != IntPtr.Zero)
                        {
                            var entry = MarshalCredential(credPtr);
                            if (entry is not null)
                                results.Add(entry);
                        }
                    }
                }
                finally
                {
                    AdvApi32.CredFree(credArray);
                }
            }
            catch
            {
                // Silently continue
            }

            return results;
        }

        /// <summary>
        /// Deletes a stored credential.
        /// </summary>
        /// <returns>True if deleted successfully.</returns>
        public static bool Delete(
            string targetName,
            CredentialType type = CredentialType.Generic)
        {
            if (string.IsNullOrEmpty(targetName))
                return false;

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            try
            {
                return AdvApi32.CredDeleteW(targetName, (uint)type, 0);
            }
            catch
            {
                return false;
            }
        }

        private static CredentialEntry? MarshalCredential(IntPtr credPtr)
        {
            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIALW>(credPtr);
                string? password = null;
                byte[]? blob = null;

                if (cred.CredentialBlob != IntPtr.Zero && cred.CredentialBlobSize > 0)
                {
                    blob = new byte[cred.CredentialBlobSize];
                    Marshal.Copy(cred.CredentialBlob, blob, 0, blob.Length);

                    // For generic credentials, the blob is typically UTF-16
                    if (cred.Type == AdvApi32.CRED_TYPE_GENERIC && blob.Length > 0)
                    {
                        int charLen = blob.Length / 2;
                        if (blob[blob.Length - 1] == 0 && blob[blob.Length - 2] == 0)
                            charLen--;
                        password = Encoding.Unicode.GetString(blob, 0, Math.Max(0, charLen * 2));
                    }
                }

                return new CredentialEntry
                {
                    TargetName = Marshal.PtrToStringUni(cred.TargetName) ?? string.Empty,
                    UserName = Marshal.PtrToStringUni(cred.UserName),
                    Password = password,
                    CredentialBlob = blob,
                    Type = (CredentialType)cred.Type,
                    Persist = (CredentialPersistence)cred.Persist,
                    Comment = Marshal.PtrToStringUni(cred.Comment),
                    LastWritten = DateTime.FromFileTime(cred.LastWritten),
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
