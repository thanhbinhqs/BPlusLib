// <copyright file="MemoryHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using BPlusLib.Foundation.Native;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Provides safe access to a memory-mapped file view.
    /// Unmaps the view and closes the file mapping handle on disposal.
    /// </summary>
    public sealed class MemoryMappedView : IDisposable
    {
        private IntPtr _fileMappingHandle;
        private IntPtr _pointer;
        private long _size;
        private bool _disposed;

        internal MemoryMappedView(IntPtr fileMappingHandle, IntPtr pointer, long size)
        {
            _fileMappingHandle = fileMappingHandle;
            _pointer = pointer;
            _size = size;
        }

        /// <summary>Gets the pointer to the mapped view of the file.</summary>
        public IntPtr Pointer
        {
            get
            {
                ThrowIfDisposed();
                return _pointer;
            }
        }

        /// <summary>Gets the size of the mapped view in bytes.</summary>
        public long Size
        {
            get
            {
                ThrowIfDisposed();
                return _size;
            }
        }

        /// <summary>Gets whether the view has been disposed.</summary>
        public bool IsDisposed => _disposed;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MemoryMappedView));
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_pointer != IntPtr.Zero)
                {
                    Kernel32.UnmapViewOfFile(_pointer);
                    _pointer = IntPtr.Zero;
                }

                if (_fileMappingHandle != IntPtr.Zero &&
                    _fileMappingHandle != Kernel32.INVALID_HANDLE_VALUE)
                {
                    Kernel32.CloseHandle(_fileMappingHandle);
                    _fileMappingHandle = IntPtr.Zero;
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Process memory usage information obtained from the OS.
    /// </summary>
    public sealed class ProcessMemoryCounters
    {
        /// <summary>Gets the current working set size in bytes.</summary>
        public long WorkingSetSize { get; init; }

        /// <summary>Gets the peak working set size in bytes.</summary>
        public long PeakWorkingSetSize { get; init; }

        /// <summary>Gets the current page file usage in bytes.</summary>
        public long PageFileUsage { get; init; }

        /// <summary>Gets the peak page file usage in bytes.</summary>
        public long PeakPageFileUsage { get; init; }

        /// <summary>Gets the number of page faults.</summary>
        public uint PageFaultCount { get; init; }

        /// <summary>Gets the current paged pool usage in bytes.</summary>
        public long PagedPoolUsage { get; init; }

        /// <summary>Gets the current non-paged pool usage in bytes.</summary>
        public long NonPagedPoolUsage { get; init; }
    }

    /// <summary>
    /// Static helper for memory-mapped file operations and process memory information.
    /// All methods are Windows-only; they will fail gracefully on non-Windows platforms.
    /// </summary>
    public static class MemoryHelper
    {
        /// <summary>
        /// Creates or opens a named memory-mapped file backed by the system paging file.
        /// </summary>
        /// <param name="name">The name of the file mapping object.</param>
        /// <param name="size">The size of the file mapping in bytes. Must be greater than zero.</param>
        /// <param name="readWrite">
        /// If <c>true</c> (default), the view is opened with read/write access;
        /// otherwise, read-only access.
        /// </param>
        /// <returns>
        /// A <see cref="MemoryMappedView"/> if successful, or <c>null</c> if the operation
        /// failed (e.g., on non-Windows platforms).
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
        public static MemoryMappedView? CreateOrOpen(string name, long size, bool readWrite = true)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");
            if (size == 0)
                return null;

            uint protection = readWrite ? Kernel32.PAGE_READWRITE : Kernel32.PAGE_READONLY;
            uint access = readWrite ? Kernel32.FILE_MAP_WRITE : Kernel32.FILE_MAP_READ;

            IntPtr hMapping = Kernel32.CreateFileMappingW(
                Kernel32.INVALID_HANDLE_VALUE,
                IntPtr.Zero,
                protection,
                (uint)(size >> 32),
                (uint)(size & 0xFFFFFFFF),
                name);

            if (hMapping == IntPtr.Zero || hMapping == Kernel32.INVALID_HANDLE_VALUE)
                return null;

            IntPtr ptr = Kernel32.MapViewOfFile(hMapping, access, 0, 0, new IntPtr(size));
            if (ptr == IntPtr.Zero)
            {
                Kernel32.CloseHandle(hMapping);
                return null;
            }

            return new MemoryMappedView(hMapping, ptr, size);
        }

        /// <summary>
        /// Opens an existing named memory-mapped file.
        /// </summary>
        /// <param name="name">The name of the file mapping object to open.</param>
        /// <param name="readWrite">
        /// If <c>true</c>, the view is opened with read/write access;
        /// otherwise (default), read-only access.
        /// </param>
        /// <returns>
        /// A <see cref="MemoryMappedView"/> if successful, or <c>null</c> if the mapping
        /// does not exist or the operation failed.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is null or empty.</exception>
        public static MemoryMappedView? Open(string name, bool readWrite = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            uint access = readWrite ? Kernel32.FILE_MAP_WRITE : Kernel32.FILE_MAP_READ;

            IntPtr hMapping = Kernel32.OpenFileMappingW(access, false, name);
            if (hMapping == IntPtr.Zero)
                return null;

            // Map the entire view (IntPtr.Zero = entire file mapping)
            IntPtr ptr = Kernel32.MapViewOfFile(hMapping, access, 0, 0, IntPtr.Zero);
            if (ptr == IntPtr.Zero)
            {
                Kernel32.CloseHandle(hMapping);
                return null;
            }

            // Determine the region size using VirtualQuery
            long viewSize = 0;
            var mbi = default(Kernel32.MEMORY_BASIC_INFORMATION64);
            int result = Kernel32.VirtualQuery(ptr, out mbi, Marshal.SizeOf<Kernel32.MEMORY_BASIC_INFORMATION64>());
            if (result != 0)
            {
                viewSize = mbi.RegionSize;
            }

            return new MemoryMappedView(hMapping, ptr, viewSize);
        }

        /// <summary>
        /// Gets the memory usage counters for the current process.
        /// </summary>
        /// <returns>
        /// A <see cref="ProcessMemoryCounters"/> with the current process memory statistics,
        /// or <c>null</c> if information could not be obtained.
        /// </returns>
        public static ProcessMemoryCounters? GetProcessMemoryCounters()
        {
            IntPtr hProcess = Kernel32.GetCurrentProcess();

            if (!PsApi.GetProcessMemoryInfo(
                hProcess,
                out PROCESS_MEMORY_COUNTERS counters,
                (uint)Marshal.SizeOf<PROCESS_MEMORY_COUNTERS>()))
            {
                return null;
            }

            return new ProcessMemoryCounters
            {
                WorkingSetSize = counters.WorkingSetSize.ToInt64(),
                PeakWorkingSetSize = counters.PeakWorkingSetSize.ToInt64(),
                PageFileUsage = counters.PagefileUsage.ToInt64(),
                PeakPageFileUsage = counters.PeakPagefileUsage.ToInt64(),
                PageFaultCount = counters.PageFaultCount,
                PagedPoolUsage = counters.QuotaPagedPoolUsage.ToInt64(),
                NonPagedPoolUsage = counters.QuotaNonPagedPoolUsage.ToInt64(),
            };
        }

        /// <summary>
        /// Gets the working set size limits for the current process.
        /// </summary>
        /// <param name="min">Receives the minimum working set size in bytes.</param>
        /// <param name="max">Receives the maximum working set size in bytes.</param>
        /// <returns>
        /// <c>true</c> if the limits were successfully retrieved; otherwise <c>false</c>.
        /// </returns>
        public static bool GetWorkingSetLimits(out long min, out long max)
        {
            IntPtr hProcess = Kernel32.GetCurrentProcess();

            if (Kernel32.GetProcessWorkingSetSizeEx(
                hProcess,
                out IntPtr minPtr,
                out IntPtr maxPtr,
                out _))
            {
                min = minPtr.ToInt64();
                max = maxPtr.ToInt64();
                return true;
            }

            min = 0;
            max = 0;
            return false;
        }
    }
}
