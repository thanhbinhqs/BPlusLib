// <copyright file="NetHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BPlusLib.Foundation.SystemInfo;

namespace BPlusLib.Foundation.Network
{
    // =====================================================================
    // Enums
    // =====================================================================

    /// <summary>
    /// TCP connection states as defined by the MIB (RFC 793).
    /// </summary>
    public enum MibTcpState
    {
        /// <summary>Closed.</summary>
        Closed = 1,

        /// <summary>Listening.</summary>
        Listen = 2,

        /// <summary>SYN sent.</summary>
        SynSent = 3,

        /// <summary>SYN received.</summary>
        SynRcvd = 4,

        /// <summary>Established.</summary>
        Established = 5,

        /// <summary>FIN WAIT 1.</summary>
        FinWait1 = 6,

        /// <summary>FIN WAIT 2.</summary>
        FinWait2 = 7,

        /// <summary>Close wait.</summary>
        CloseWait = 8,

        /// <summary>Closing.</summary>
        Closing = 9,

        /// <summary>Last ACK.</summary>
        LastAck = 10,

        /// <summary>Time wait.</summary>
        TimeWait = 11,

        /// <summary>Delete TCB.</summary>
        DeleteTcb = 12,
    }

    /// <summary>
    /// ARP cache entry states.
    /// </summary>
    public enum ArpEntryState
    {
        /// <summary>Incomplete — address resolution is in progress.</summary>
        Incomplete = 1,

        /// <summary>Reachable — the entry is valid and reachable.</summary>
        Reachable = 2,

        /// <summary>Stale — the entry is stale.</summary>
        Stale = 3,

        /// <summary>Delay — a delay before probing.</summary>
        Delay = 4,

        /// <summary>Probe — actively probing the address.</summary>
        Probe = 5,

        /// <summary>Invalid — the entry is invalid.</summary>
        Invalid = 6,

        /// <summary>Unknown — the entry is in an unknown state.</summary>
        Unknown = 7,

        /// <summary>Permanent — the entry is permanent (static).</summary>
        Permanent = 8,

        /// <summary>Published — the entry is published (proxy ARP).</summary>
        Published = 9,

        /// <summary>Other — the entry type is undefined (MIB_IPNETROW dwType=1).</summary>
        Other = 10,

        /// <summary>Dynamic — the entry was added dynamically via ARP (dwType=3).</summary>
        Dynamic = 11,

        /// <summary>Static — the entry was added statically (dwType=4).</summary>
        Static = 12,
    }

    // =====================================================================
    // P/Invoke enums & structs
    // =====================================================================

    /// <summary>
    /// Table class for GetExtendedTcpTable.
    /// </summary>
    internal enum TcpTableClass
    {
        /// <summary>Basic listener table.</summary>
        TcpTableBasicListener = 0,

        /// <summary>Basic connections table.</summary>
        TcpTableBasicConnections = 1,

        /// <summary>Basic all table.</summary>
        TcpTableBasicAll = 2,

        /// <summary>Owner PID listener table.</summary>
        TcpTableOwnerPidListener = 3,

        /// <summary>Owner PID connections table.</summary>
        TcpTableOwnerPidConnections = 4,

        /// <summary>Owner PID all table.</summary>
        TcpTableOwnerPidAll = 5,
    }

    /// <summary>
    /// Table class for GetExtendedUdpTable.
    /// </summary>
    internal enum UdpTableClass
    {
        /// <summary>Basic UDP table.</summary>
        UdpTableBasic = 0,

        /// <summary>UDP table with owning PID.</summary>
        UdpTableOwnerPid = 1,

        /// <summary>UDP table with owning module.</summary>
        UdpTableOwnerModule = 2,
    }

    /// <summary>
    /// Native MIB_TCPROW_OWNER_PID structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCPROW_OWNER_PID
    {
        /// <summary>Connection state.</summary>
        internal uint State;

        /// <summary>Local IPv4 address (network byte order).</summary>
        internal uint LocalAddr;

        /// <summary>Local port (network byte order).</summary>
        internal uint LocalPort;

        /// <summary>Remote IPv4 address (network byte order).</summary>
        internal uint RemoteAddr;

        /// <summary>Remote port (network byte order).</summary>
        internal uint RemotePort;

        /// <summary>Owning process PID.</summary>
        internal uint OwningPid;
    }

    /// <summary>
    /// Native MIB_UDPROW_OWNER_PID structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_UDPROW_OWNER_PID
    {
        /// <summary>Local IPv4 address (network byte order).</summary>
        internal uint LocalAddr;

        /// <summary>Local port (network byte order).</summary>
        internal uint LocalPort;

        /// <summary>Owning process PID.</summary>
        internal uint OwningPid;
    }

    /// <summary>
    /// Native MIB_IPNETROW structure (ARP entry).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_IPNETROW
    {
        /// <summary>Interface index.</summary>
        internal int DwIndex;

        /// <summary>Physical address length.</summary>
        internal uint DwPhysAddrLen;

        // Physical address bytes (8 bytes fixed)
        internal byte Mac0;
        internal byte Mac1;
        internal byte Mac2;
        internal byte Mac3;
        internal byte Mac4;
        internal byte Mac5;
        internal byte Mac6;
        internal byte Mac7;

        /// <summary>IP address (network byte order).</summary>
        internal uint DwAddr;

        /// <summary>Entry type: 1=other, 2=invalid, 3=dynamic, 4=static.</summary>
        internal uint DwType;
    }

    /// <summary>
    /// Native MIB_IPNETROW2 structure for GetIpNetTable2.
    /// Simplified structure containing only the fields we need.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_IPNETROW2
    {
        /// <summary>Interface index (LUID-like, but stored as IntPtr for simplicity).</summary>
        internal IntPtr InterfaceIndex;

        /// <summary>IP address structure.</summary>
        internal MIB_IPNET_ADDRESS Address;

        /// <summary>Physical address.</summary>
        internal MIB_IPNET_PHYSADDRESS PhysicalAddress;

        /// <summary>Physical address length.</summary>
        internal uint PhysicalAddressLength;

        /// <summary>Neighbor state (NL_NEIGHBOR_STATE).</summary>
        internal uint State;

        /// <summary>Flags.</summary>
        internal uint Flags;

        /// <summary>Reachability timestamp.</summary>
        internal ulong ReachabilityTime;
    }

    /// <summary>
    /// Native IP address union used by MIB_IPNETROW2.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_IPNET_ADDRESS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        internal byte[] Address;
    }

    /// <summary>
    /// Native physical address struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_IPNET_PHYSADDRESS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        internal byte[] Address;
    }

    // =====================================================================
    // Result types
    // =====================================================================

    /// <summary>
    /// Represents the result of an ICMP ping operation.
    /// </summary>
    public sealed class PingResult
    {
        /// <summary>
        /// Gets a value indicating whether the ping succeeded (received a reply).
        /// </summary>
        public bool Success { get; internal set; }

        /// <summary>
        /// Gets the round-trip time in milliseconds, or -1 if the ping failed.
        /// </summary>
        public long RoundtripTimeMs { get; internal set; }

        /// <summary>
        /// Gets a human-readable status description, or <c>null</c> on success.
        /// </summary>
        public string? Status { get; internal set; }

        /// <summary>
        /// Gets the IP address that replied, or <c>null</c> if no reply was received.
        /// </summary>
        public string? IpAddress { get; internal set; }

        /// <summary>
        /// Returns a string summary of this ping result.
        /// </summary>
        /// <returns>A formatted string describing the result.</returns>
        public override string ToString()
        {
            if (Success)
                return $"Ping reply from {IpAddress} time={RoundtripTimeMs}ms";
            return $"Ping failed: {Status ?? "Unknown error"}";
        }
    }

    /// <summary>
    /// Represents a TCP connection entry from the system's connection table.
    /// </summary>
    public sealed class TcpConnectionInfo
    {
        /// <summary>
        /// Gets the local IP address string.
        /// </summary>
        public string LocalAddress { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets the local port number.
        /// </summary>
        public int LocalPort { get; internal set; }

        /// <summary>
        /// Gets the remote IP address string.
        /// </summary>
        public string RemoteAddress { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets the remote port number.
        /// </summary>
        public int RemotePort { get; internal set; }

        /// <summary>
        /// Gets the TCP connection state.
        /// </summary>
        public MibTcpState State { get; internal set; }

        /// <summary>
        /// Gets the PID of the process that owns this connection.
        /// </summary>
        public int OwningPid { get; internal set; }

        /// <summary>
        /// Gets the process name that owns this connection, or <c>null</c> if it
        /// could not be resolved.
        /// </summary>
        public string? OwningProcessName { get; internal set; }

        /// <summary>
        /// Returns a formatted string describing this connection.
        /// </summary>
        /// <returns>A human-readable connection description.</returns>
        public override string ToString()
        {
            return $"{State, -10} {LocalAddress}:{LocalPort} -> {RemoteAddress}:{RemotePort} PID={OwningPid}";
        }
    }

    /// <summary>
    /// Represents a UDP listener entry from the system's listener table.
    /// </summary>
    public sealed class UdpListenerInfo
    {
        /// <summary>
        /// Gets the local IP address string.
        /// </summary>
        public string LocalAddress { get; internal set; } = string.Empty;

        /// <summary>
        /// Gets the local port number.
        /// </summary>
        public int LocalPort { get; internal set; }

        /// <summary>
        /// Gets the PID of the process that owns this listener.
        /// </summary>
        public int OwningPid { get; internal set; }

        /// <summary>
        /// Gets the process name that owns this listener, or <c>null</c> if it
        /// could not be resolved.
        /// </summary>
        public string? OwningProcessName { get; internal set; }

        /// <summary>
        /// Returns a formatted string describing this UDP listener.
        /// </summary>
        /// <returns>A human-readable listener description.</returns>
        public override string ToString()
        {
            return $"UDP {LocalAddress}:{LocalPort} PID={OwningPid}";
        }
    }

    /// <summary>
    /// Represents a single entry in the system's ARP (Address Resolution Protocol) table.
    /// </summary>
    public struct ArpTableEntry
    {
        /// <summary>
        /// The IP address of the entry.
        /// </summary>
        public string IpAddress;

        /// <summary>
        /// The MAC (physical) address as a colon-separated hex string, or <c>null</c>.
        /// </summary>
        public string? MacAddress;

        /// <summary>
        /// The interface index for this entry.
        /// </summary>
        public string InterfaceIndex;

        /// <summary>
        /// The state of the ARP entry.
        /// </summary>
        public ArpEntryState State;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArpTableEntry"/> struct.
        /// </summary>
        /// <param name="ipAddress">The IP address string.</param>
        /// <param name="macAddress">The MAC address string, or <c>null</c>.</param>
        /// <param name="interfaceIndex">The interface index string.</param>
        /// <param name="state">The ARP entry state.</param>
        public ArpTableEntry(string ipAddress, string? macAddress, string interfaceIndex, ArpEntryState state)
        {
            IpAddress = ipAddress ?? string.Empty;
            MacAddress = macAddress;
            InterfaceIndex = interfaceIndex ?? string.Empty;
            State = state;
        }

        /// <summary>
        /// Returns a formatted string describing this ARP entry.
        /// </summary>
        /// <returns>A human-readable string.</returns>
        public override string ToString()
        {
            return $"{IpAddress, -16} {MacAddress, -18} IF={InterfaceIndex} {State}";
        }
    }

    // =====================================================================
    // P/Invoke declarations
    // =====================================================================

    /// <summary>
    /// Provides low-level network information via P/Invoke to IPHLPAPI
    /// and built-in .NET networking APIs. No WMI is used anywhere in
    /// this class.
    /// </summary>
    public static partial class NetHelper
    {
        // -----------------------------------------------------------------
        // iphlpapi.dll
        // -----------------------------------------------------------------

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr tcpTable,
            ref int tcpTableLength,
            bool order,
            uint ipVersion,
            TcpTableClass tableClass,
            uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(
            IntPtr udpTable,
            ref int udpTableLength,
            bool order,
            uint ipVersion,
            UdpTableClass tableClass,
            uint reserved);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int GetIpNetTable(
            IntPtr ipNetTable,
            ref int size,
            bool order);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetIpNetTable2(
            IntPtr interfaceIndex,
            IntPtr ipNetTable2,
            ref int size,
            bool order);

        // -----------------------------------------------------------------
        // Kernel32 — for resolving process names from PID
        // -----------------------------------------------------------------

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetModuleBaseName(
            IntPtr hProcess,
            IntPtr hModule,
            StringBuilder lpBaseName,
            uint nSize);

        // -----------------------------------------------------------------
        // Constants
        // -----------------------------------------------------------------

        private const uint ProcessQueryLimitedInformation = 0x1000;
        private const uint AfInet = 2;
        private const int MaxProcessNameLength = 260;
        private const int ErrorInsufficientBuffer = 122;
        private const int NoError = 0;

        // Wake-on-LAN magic packet constants
        private const int WoLSyncLength = 6;
        private const int WoLMacRepeats = 16;
        private const int WoLMacLength = 6;
        private const int WoLDefaultPort = 9;

        // MAC address string length (17 chars: "XX:XX:XX:XX:XX:XX")
        private const int MacAddressStringLength = 17;

        // =====================================================================
        // 1. Ping
        // =====================================================================

        /// <summary>
        /// Sends an ICMP echo request to the specified host and returns the result.
        /// Uses the built-in <see cref="System.Net.NetworkInformation.Ping"/> class.
        /// </summary>
        /// <param name="host">The host name or IP address to ping.</param>
        /// <param name="timeoutMs">The timeout in milliseconds (default: 3000).</param>
        /// <param name="ttl">The Time-To-Live value (default: 64).</param>
        /// <returns>A <see cref="PingResult"/> describing the outcome.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="host"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="host"/> is empty or whitespace.</exception>
        public static PingResult Ping(string host, int timeoutMs = 3000, int ttl = 64)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host cannot be empty or whitespace.", nameof(host));

            var result = new PingResult();

            try
            {
                using (var ping = new System.Net.NetworkInformation.Ping())
                {
                    var options = new PingOptions(ttl, true);
                    PingReply reply = ping.Send(host, timeoutMs, buffer: Array.Empty<byte>(), options);

                    result.Success = reply.Status == IPStatus.Success;
                    result.RoundtripTimeMs = result.Success ? reply.RoundtripTime : -1;
                    result.Status = reply.Status.ToString();
                    result.IpAddress = reply.Address?.ToString();
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.RoundtripTimeMs = -1;
                result.Status = ex.GetType().Name + ": " + ex.Message;
            }

            return result;
        }

        // =====================================================================
        // 2. Get TCP connections
        // =====================================================================

        /// <summary>
        /// Enumerates all active TCP connections with owning process PID.
        /// Uses GetExtendedTcpTable from iphlpapi.dll.
        /// </summary>
        /// <returns>A read-only list of <see cref="TcpConnectionInfo"/> entries.</returns>
        public static IReadOnlyList<TcpConnectionInfo> GetTcpConnections()
        {
            var connections = new List<TcpConnectionInfo>();

            try
            {
                int bufferSize = 0;
                uint result = GetExtendedTcpTable(
                    IntPtr.Zero,
                    ref bufferSize,
                    order: false,
                    AfInet,
                    TcpTableClass.TcpTableOwnerPidAll,
                    reserved: 0);

                if (result != ErrorInsufficientBuffer && result != NoError)
                    return connections;

                if (bufferSize <= 0)
                    return connections;

                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    result = GetExtendedTcpTable(
                        buffer,
                        ref bufferSize,
                        order: false,
                        AfInet,
                        TcpTableClass.TcpTableOwnerPidAll,
                        reserved: 0);

                    if (result != NoError)
                        return connections;

                    // First 4 bytes = number of entries (dwNumEntries)
                    int entryCount = Marshal.ReadInt32(buffer);
                    int rowSize = Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
                    IntPtr current = buffer + 4; // skip dwNumEntries

                    for (int i = 0; i < entryCount; i++)
                    {
                        try
                        {
                            var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(current);
                            var info = ParseTcpRow(row);
                            if (info != null)
                                connections.Add(info);
                        }
                        catch
                        {
                            // Skip malformed entries
                        }

                        current += rowSize;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                // Non-Windows or API failure — return empty list
            }

            return connections;
        }

        /// <summary>
        /// Parses a native MIB_TCPROW_OWNER_PID into a managed TcpConnectionInfo.
        /// </summary>
        private static TcpConnectionInfo? ParseTcpRow(MIB_TCPROW_OWNER_PID row)
        {
            try
            {
                var info = new TcpConnectionInfo();

                // Local address
                byte[] localBytes = BitConverter.GetBytes(row.LocalAddr);
                info.LocalAddress = new IPAddress(localBytes).ToString();

                // Local port (network byte order → host byte order)
                info.LocalPort = (ushort)IPAddress.NetworkToHostOrder((short)(row.LocalPort & 0xFFFF));

                // Remote address
                byte[] remoteBytes = BitConverter.GetBytes(row.RemoteAddr);
                info.RemoteAddress = new IPAddress(remoteBytes).ToString();

                // Remote port
                info.RemotePort = (ushort)IPAddress.NetworkToHostOrder((short)(row.RemotePort & 0xFFFF));

                // State
                info.State = (MibTcpState)row.State;

                // PID
                info.OwningPid = (int)row.OwningPid;

                // Process name
                info.OwningProcessName = GetProcessName((int)row.OwningPid);

                return info;
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // 3. Get UDP listeners
        // =====================================================================

        /// <summary>
        /// Enumerates all UDP listeners with owning process PID.
        /// Uses GetExtendedUdpTable from iphlpapi.dll.
        /// </summary>
        /// <returns>A read-only list of <see cref="UdpListenerInfo"/> entries.</returns>
        public static IReadOnlyList<UdpListenerInfo> GetUdpListeners()
        {
            var listeners = new List<UdpListenerInfo>();

            try
            {
                int bufferSize = 0;
                uint result = GetExtendedUdpTable(
                    IntPtr.Zero,
                    ref bufferSize,
                    order: false,
                    AfInet,
                    UdpTableClass.UdpTableOwnerPid,
                    reserved: 0);

                if (result != ErrorInsufficientBuffer && result != NoError)
                    return listeners;

                if (bufferSize <= 0)
                    return listeners;

                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    result = GetExtendedUdpTable(
                        buffer,
                        ref bufferSize,
                        order: false,
                        AfInet,
                        UdpTableClass.UdpTableOwnerPid,
                        reserved: 0);

                    if (result != NoError)
                        return listeners;

                    // First 4 bytes = number of entries
                    int entryCount = Marshal.ReadInt32(buffer);
                    int rowSize = Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();
                    IntPtr current = buffer + 4;

                    for (int i = 0; i < entryCount; i++)
                    {
                        try
                        {
                            var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(current);
                            var info = ParseUdpRow(row);
                            if (info != null)
                                listeners.Add(info);
                        }
                        catch
                        {
                            // Skip malformed entries
                        }

                        current += rowSize;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                // Non-Windows or API failure — return empty list
            }

            return listeners;
        }

        /// <summary>
        /// Parses a native MIB_UDPROW_OWNER_PID into a managed UdpListenerInfo.
        /// </summary>
        private static UdpListenerInfo? ParseUdpRow(MIB_UDPROW_OWNER_PID row)
        {
            try
            {
                var info = new UdpListenerInfo();

                // Local address
                byte[] localBytes = BitConverter.GetBytes(row.LocalAddr);
                info.LocalAddress = new IPAddress(localBytes).ToString();

                // Local port (network byte order → host byte order)
                info.LocalPort = (ushort)IPAddress.NetworkToHostOrder((short)(row.LocalPort & 0xFFFF));

                // PID
                info.OwningPid = (int)row.OwningPid;

                // Process name
                info.OwningProcessName = GetProcessName((int)row.OwningPid);

                return info;
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // 4. Get ARP table
        // =====================================================================

        /// <summary>
        /// Enumerates the system's ARP (Address Resolution Protocol) table.
        /// Tries GetIpNetTable2 first (IPv6-capable, newer API), and falls back
        /// to GetIpNetTable (older XP-compatible API) if unavailable.
        /// </summary>
        /// <returns>An array of <see cref="ArpTableEntry"/> entries.</returns>
        public static ArpTableEntry[] GetArpTable()
        {
            // Try GetIpNetTable2 first (newer API)
            try
            {
                ArpTableEntry[]? entries = GetArpTableViaGetIpNetTable2();
                if (entries != null)
                    return entries;
            }
            catch
            {
                // Fall through to legacy API
            }

            // Fallback to GetIpNetTable (legacy)
            return GetArpTableViaGetIpNetTable();
        }

        /// <summary>
        /// Attempts to read the ARP table using GetIpNetTable2 (newer API).
        /// Returns null if the API is unavailable.
        /// </summary>
        private static ArpTableEntry[]? GetArpTableViaGetIpNetTable2()
        {
            try
            {
                int bufferSize = 0;
                uint result = GetIpNetTable2(
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ref bufferSize,
                    order: true);

                if (result != ErrorInsufficientBuffer && result != NoError)
                    return null;

                if (bufferSize <= 0)
                    return Array.Empty<ArpTableEntry>();

                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    result = GetIpNetTable2(
                        IntPtr.Zero,
                        buffer,
                        ref bufferSize,
                        order: true);

                    if (result != NoError)
                        return null;

                    // The structure of GetIpNetTable2 output is complex with
                    // variable-length entries. Since GetIpNetTable2 is a newer API
                    // that may not be available on all target platforms (e.g., Win7),
                    // and its buffer layout is non-trivial with aligned variable-length
                    // records, we fall back to the simpler GetIpNetTable.
                    return null;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads the ARP table using GetIpNetTable (legacy, XP-compatible API).
        /// </summary>
        private static ArpTableEntry[] GetArpTableViaGetIpNetTable()
        {
            var entries = new List<ArpTableEntry>();

            try
            {
                int bufferSize = 0;
                int result = GetIpNetTable(IntPtr.Zero, ref bufferSize, order: true);

                if (result != ErrorInsufficientBuffer && result != NoError)
                    return Array.Empty<ArpTableEntry>();

                if (bufferSize <= 0)
                    return Array.Empty<ArpTableEntry>();

                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    result = GetIpNetTable(buffer, ref bufferSize, order: true);
                    if (result != NoError)
                        return Array.Empty<ArpTableEntry>();

                    // First 4 bytes = number of entries (dwNumEntries)
                    int entryCount = Marshal.ReadInt32(buffer);
                    int rowSize = Marshal.SizeOf<MIB_IPNETROW>();
                    IntPtr current = buffer + 4;

                    for (int i = 0; i < entryCount; i++)
                    {
                        try
                        {
                            var row = Marshal.PtrToStructure<MIB_IPNETROW>(current);
                            var entry = ParseIpNetRow(row);
                            if (entry.HasValue)
                                entries.Add(entry.Value);
                        }
                        catch
                        {
                            // Skip malformed entries
                        }

                        current += rowSize;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch
            {
                // Non-Windows or API failure — return empty
            }

            return entries.ToArray();
        }

        /// <summary>
        /// Parses a native MIB_IPNETROW into a managed ArpTableEntry.
        /// </summary>
        private static ArpTableEntry? ParseIpNetRow(MIB_IPNETROW row)
        {
            try
            {
                // IP address
                byte[] ipBytes = BitConverter.GetBytes(row.DwAddr);
                string ipAddress = new IPAddress(ipBytes).ToString();

                // MAC address (take only valid bytes from the physical address length)
                string? macAddress = null;
                if (row.DwPhysAddrLen >= 6)
                {
                    byte[] macBytes = new byte[] { row.Mac0, row.Mac1, row.Mac2, row.Mac3, row.Mac4, row.Mac5 };
                    macAddress = string.Join(":", Array.ConvertAll(macBytes, b => b.ToString("X2")));
                }

                // Interface index
                string interfaceIndex = row.DwIndex.ToString();

                // Entry type → ArpEntryState
                // MIB_IPNETROW dwType: 1=other, 2=invalid, 3=dynamic, 4=static
                ArpEntryState state = row.DwType switch
                {
                    1 => ArpEntryState.Reachable,  // "other" → treat as reachable
                    2 => ArpEntryState.Invalid,
                    3 => ArpEntryState.Reachable,  // dynamic → treat as reachable
                    4 => ArpEntryState.Permanent,  // static → permanent
                    _ => ArpEntryState.Unknown,
                };

                return new ArpTableEntry(ipAddress, macAddress, interfaceIndex, state);
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // 5. DNS lookup
        // =====================================================================

        /// <summary>
        /// Performs a DNS lookup for the specified hostname and returns all
        /// associated IP addresses as strings.
        /// </summary>
        /// <param name="hostname">The host name or IP address to resolve.</param>
        /// <returns>An array of IP address strings.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="hostname"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="hostname"/> is empty or whitespace.</exception>
        public static string[] LookupDns(string hostname)
        {
            if (hostname == null)
                throw new ArgumentNullException(nameof(hostname));
            if (string.IsNullOrWhiteSpace(hostname))
                throw new ArgumentException("Hostname cannot be empty or whitespace.", nameof(hostname));

            try
            {
                var addresses = Task.Run(() => Dns.GetHostAddressesAsync(hostname))
                                    .GetAwaiter()
                                    .GetResult();

                if (addresses == null || addresses.Length == 0)
                    return Array.Empty<string>();

                var results = new string[addresses.Length];
                for (int i = 0; i < addresses.Length; i++)
                    results[i] = addresses[i].ToString();

                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"DNS lookup failed for '{hostname}': {ex.Message}", ex);
            }
        }

        // =====================================================================
        // 6. Wake-on-LAN
        // =====================================================================

        /// <summary>
        /// Sends a Wake-on-LAN (WoL) magic packet to the specified MAC address
        /// on the specified broadcast IP and port.
        /// </summary>
        /// <param name="macAddress">
        /// The target MAC address. Acceptable formats:
        /// <c>XX:XX:XX:XX:XX:XX</c>, <c>XX-XX-XX-XX-XX-XX</c>, <c>XXXXXXXXXXXX</c>.
        /// </param>
        /// <param name="broadcastIp">
        /// The broadcast IP address (default: <c>255.255.255.255</c>).
        /// </param>
        /// <returns><c>true</c> if the magic packet was sent successfully; otherwise <c>false</c>.</returns>
        public static bool WakeOnLan(string macAddress, string? broadcastIp = "255.255.255.255")
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                return false;

            try
            {
                // Parse MAC address
                byte[]? mac = ParseMacAddress(macAddress);
                if (mac is null || mac.Length != WoLMacLength)
                    return false;

                // Construct magic packet: 6 bytes of 0xFF + 16 copies of MAC
                byte[] packet = new byte[WoLSyncLength + (WoLMacRepeats * WoLMacLength)];

                // First 6 bytes: 0xFF
                for (int i = 0; i < WoLSyncLength; i++)
                    packet[i] = 0xFF;

                // Next 16 copies of MAC address
                for (int i = 0; i < WoLMacRepeats; i++)
                {
                    for (int j = 0; j < WoLMacLength; j++)
                    {
                        packet[WoLSyncLength + (i * WoLMacLength) + j] = mac[j];
                    }
                }

                // Resolve the broadcast address
                IPAddress broadcastAddress;
                if (string.IsNullOrWhiteSpace(broadcastIp))
                    broadcastAddress = IPAddress.Broadcast;
                else
                    broadcastAddress = IPAddress.Parse(broadcastIp);

                using (var udpClient = new UdpClient())
                {
                    udpClient.EnableBroadcast = true;
                    udpClient.Send(packet, packet.Length, new IPEndPoint(broadcastAddress, WoLDefaultPort));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parses a MAC address string into a byte array.
        /// Accepts formats: XX:XX:XX:XX:XX:XX, XX-XX-XX-XX-XX-XX, XXXXXXXXXXXX.
        /// </summary>
        private static byte[]? ParseMacAddress(string macAddress)
        {
            try
            {
                // Normalize: remove separators
                string clean = macAddress.Replace(":", string.Empty)
                                         .Replace("-", string.Empty)
                                         .Replace(".", string.Empty)
                                         .Trim();

                if (clean.Length != 12)
                    return null;

                byte[] bytes = new byte[6];
                for (int i = 0; i < 6; i++)
                    bytes[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);

                return bytes;
            }
            catch
            {
                return null;
            }
        }

        // =====================================================================
        // 7. VPN detection
        // =====================================================================

        /// <summary>
        /// Determines whether a VPN connection is currently active by inspecting
        /// network adapters for tunnel-type interfaces that have IP addresses.
        /// </summary>
        /// <returns>
        /// <c>true</c> if at least one tunnel-type adapter with an IP address
        /// is found; otherwise <c>false</c>.
        /// </returns>
        public static bool IsVpnConnected()
        {
            try
            {
                // Use the existing NetworkInfo.GetAllAdapters() from the SystemInfo
                // namespace to enumerate adapters and look for tunnel-type interfaces
                // with assigned IP addresses — a strong indicator of an active VPN.
                IReadOnlyList<NetworkAdapterInfo> adapters = NetworkInfo.GetAllAdapters();

                for (int i = 0; i < adapters.Count; i++)
                {
                    NetworkAdapterInfo adapter = adapters[i];

                    // Check for tunnel type (typical VPN adapters)
                    if (string.Equals(adapter.AdapterType, "Tunnel", StringComparison.OrdinalIgnoreCase))
                    {
                        // A tunnel adapter with IP addresses is likely a VPN connection
                        if (adapter.IpAddresses != null && adapter.IpAddresses.Count > 0)
                            return true;
                    }

                    // Also check description-based heuristics for known VPN software
                    if (!string.IsNullOrEmpty(adapter.Description))
                    {
                        string desc = adapter.Description;

                        // Use IndexOf instead of Contains(string, StringComparison)
                        // for net472 compatibility
                        if (desc.IndexOf("VPN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("Tunnel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("OpenVPN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("WireGuard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("Cisco AnyConnect", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("PANGP Virtual Ethernet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("Pulse Secure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("NordVPN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("ExpressVPN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            desc.IndexOf("ProtonVPN", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (adapter.IpAddresses != null && adapter.IpAddresses.Count > 0)
                                return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                // Non-Windows or enumeration failure — assume not connected
                return false;
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        /// <summary>
        /// Attempts to resolve the process name for a given PID.
        /// Returns <c>null</c> if the process cannot be queried.
        /// </summary>
        /// <param name="pid">The process ID.</param>
        /// <returns>The process name (without extension), or <c>null</c>.</returns>
        internal static string? GetProcessName(int pid)
        {
            if (pid <= 0)
                return null;

            try
            {
                IntPtr hProcess = OpenProcess(ProcessQueryLimitedInformation, false, pid);
                if (hProcess == IntPtr.Zero || hProcess == (IntPtr)(-1))
                {
                    // Fallback: try Process.GetProcessById
                    try
                    {
                        var proc = System.Diagnostics.Process.GetProcessById(pid);
                        return proc.ProcessName;
                    }
                    catch
                    {
                        return null;
                    }
                }

                try
                {
                    var sb = new StringBuilder(MaxProcessNameLength);
                    uint length = GetModuleBaseName(hProcess, IntPtr.Zero, sb, (uint)sb.Capacity);
                    if (length > 0)
                        return sb.ToString();
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }
            catch
            {
                // Ignore
            }

            // Last resort fallback
            try
            {
                var proc = System.Diagnostics.Process.GetProcessById(pid);
                return proc.ProcessName;
            }
            catch
            {
                return null;
            }
        }
    }
}
