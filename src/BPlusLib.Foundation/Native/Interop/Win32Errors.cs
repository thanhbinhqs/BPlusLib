// <copyright file="Win32Errors.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Runtime.InteropServices;

namespace BPlusLib.Foundation.Native.Interop
{
    /// <summary>
    /// Win32 error code constants, HRESULT helpers, and NTSTATUS values.
    /// </summary>
    internal static class Win32Errors
    {
        // =====================================================================
        // HRESULT constants
        // =====================================================================

        /// <summary>S_OK — Operation successful.</summary>
        internal const int S_OK = 0;

        /// <summary>S_FALSE — Operation successful but returned false.</summary>
        internal const int S_FALSE = 1;

        /// <summary>E_FAIL — Unspecified failure.</summary>
        internal const int E_FAIL = unchecked((int)0x80004005);

        /// <summary>E_INVALIDARG — Invalid argument.</summary>
        internal const int E_INVALIDARG = unchecked((int)0x80070057);

        /// <summary>E_OUTOFMEMORY — Out of memory.</summary>
        internal const int E_OUTOFMEMORY = unchecked((int)0x8007000E);

        /// <summary>E_NOTIMPL — Not implemented.</summary>
        internal const int E_NOTIMPL = unchecked((int)0x80004001);

        /// <summary>E_POINTER — Invalid pointer.</summary>
        internal const int E_POINTER = unchecked((int)0x80004003);

        /// <summary>E_ACCESSDENIED — Access denied.</summary>
        internal const int E_ACCESSDENIED = unchecked((int)0x80070005);

        /// <summary>E_ABORT — Operation aborted.</summary>
        internal const int E_ABORT = unchecked((int)0x80004004);

        /// <summary>E_HANDLE — Invalid handle.</summary>
        internal const int E_HANDLE = unchecked((int)0x80070006);

        /// <summary>Returns true if the HRESULT indicates success (hr &gt;= 0).</summary>
        internal static bool Succeeded(int hr) => hr >= 0;

        /// <summary>Returns true if the HRESULT indicates failure (hr &lt; 0).</summary>
        internal static bool Failed(int hr) => hr < 0;

        // =====================================================================
        // NTSTATUS constants
        // =====================================================================

        /// <summary>STATUS_SUCCESS.</summary>
        internal const int STATUS_SUCCESS = 0;

        /// <summary>STATUS_INFO_LENGTH_MISMATCH.</summary>
        internal const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

        /// <summary>STATUS_BUFFER_TOO_SMALL.</summary>
        internal const int STATUS_BUFFER_TOO_SMALL = unchecked((int)0xC0000023);

        /// <summary>STATUS_BUFFER_OVERFLOW.</summary>
        internal const int STATUS_BUFFER_OVERFLOW = unchecked((int)0x80000005);

        /// <summary>STATUS_ACCESS_DENIED.</summary>
        internal const int STATUS_ACCESS_DENIED = unchecked((int)0xC0000022);

        /// <summary>STATUS_INVALID_HANDLE.</summary>
        internal const int STATUS_INVALID_HANDLE = unchecked((int)0xC0000008);

        /// <summary>STATUS_OBJECT_NAME_NOT_FOUND.</summary>
        internal const int STATUS_OBJECT_NAME_NOT_FOUND = unchecked((int)0xC0000034);

        /// <summary>Returns true if the NTSTATUS indicates success.</summary>
        internal static bool NtSucceeded(int status) => status >= 0;

        /// <summary>Returns true if the NTSTATUS indicates failure.</summary>
        internal static bool NtFailed(int status) => status < 0;

        // =====================================================================
        // Common Win32 error codes
        // =====================================================================

        /// <summary>ERROR_SUCCESS.</summary>
        internal const int ERROR_SUCCESS = 0;

        /// <summary>ERROR_FILE_NOT_FOUND.</summary>
        internal const int ERROR_FILE_NOT_FOUND = 2;

        /// <summary>ERROR_ACCESS_DENIED.</summary>
        internal const int ERROR_ACCESS_DENIED = 5;

        /// <summary>ERROR_INVALID_HANDLE.</summary>
        internal const int ERROR_INVALID_HANDLE = 6;

        /// <summary>ERROR_NOT_ENOUGH_MEMORY.</summary>
        internal const int ERROR_NOT_ENOUGH_MEMORY = 8;

        /// <summary>ERROR_INVALID_PARAMETER.</summary>
        internal const int ERROR_INVALID_PARAMETER = 87;

        /// <summary>ERROR_INSUFFICIENT_BUFFER.</summary>
        internal const int ERROR_INSUFFICIENT_BUFFER = 122;

        /// <summary>ERROR_MOD_NOT_FOUND.</summary>
        internal const int ERROR_MOD_NOT_FOUND = 126;

        /// <summary>ERROR_PROC_NOT_FOUND.</summary>
        internal const int ERROR_PROC_NOT_FOUND = 127;

        /// <summary>ERROR_CALL_NOT_IMPLEMENTED.</summary>
        internal const int ERROR_CALL_NOT_IMPLEMENTED = 120;

        /// <summary>WAIT_TIMEOUT.</summary>
        internal const int WAIT_TIMEOUT = 258;

        /// <summary>ERROR_OPERATION_ABORTED.</summary>
        internal const int ERROR_OPERATION_ABORTED = 995;

        /// <summary>ERROR_NO_MORE_ITEMS.</summary>
        internal const int ERROR_NO_MORE_ITEMS = 259;

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>
        /// Gets the last Win32 error code wrapped in a <see cref="Common.Result{T}"/>.
        /// </summary>
        /// <returns>A failed Result with the error code as the value (for logging).</returns>
        internal static Common.Result<int> GetLastWin32Error()
        {
            int errorCode = Marshal.GetLastWin32Error();
            return Common.Result<int>.Ok(errorCode);
        }
    }
}
