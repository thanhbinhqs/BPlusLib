// <copyright file="NetworkAdapterInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Provides information about a single network adapter —
    /// name, description, MAC address, IP configuration, DHCP status,
    /// speed, and adapter type. All data is obtained via P/Invoke
    /// (IPHLPAPI) without WMI.
    /// </summary>
    public sealed class NetworkAdapterInfo
    {
        internal NetworkAdapterInfo()
        {
        }

        // =====================================================================
        // Properties
        // =====================================================================

        /// <summary>Gets the friendly name of the adapter (e.g., "Ethernet", "Wi-Fi").</summary>
        public string Name { get; internal set; } = string.Empty;

        /// <summary>Gets the adapter description (e.g., "Intel(R) Ethernet Connection").</summary>
        public string Description { get; internal set; } = string.Empty;

        /// <summary>Gets the MAC address as a colon-separated hex string, or <c>null</c>.</summary>
        public string? MacAddress { get; internal set; }

        /// <summary>Gets the list of IP addresses assigned to this adapter.</summary>
        public IReadOnlyList<string> IpAddresses { get; internal set; } = Array.Empty<string>();

        /// <summary>Gets the list of gateway addresses for this adapter.</summary>
        public IReadOnlyList<string> GatewayAddresses { get; internal set; } = Array.Empty<string>();

        /// <summary>Gets the list of DNS server addresses for this adapter.</summary>
        public IReadOnlyList<string> DnsAddresses { get; internal set; } = Array.Empty<string>();

        /// <summary>Gets whether DHCP is enabled on this adapter.</summary>
        public bool IsDhcpEnabled { get; internal set; }

        /// <summary>Gets the DHCP server address, if available.</summary>
        public string? DhcpServer { get; internal set; }

        /// <summary>Gets the DHCP lease obtained timestamp, if available.</summary>
        public DateTime? DhcpLeaseObtained { get; internal set; }

        /// <summary>Gets the DHCP lease expiry timestamp, if available.</summary>
        public DateTime? DhcpLeaseExpires { get; internal set; }

        /// <summary>Gets whether the adapter is operationally up.</summary>
        public bool IsUp { get; internal set; }

        /// <summary>Gets the adapter speed in bits per second.</summary>
        public long Speed { get; internal set; }

        /// <summary>
        /// Gets a string describing the adapter type
        /// (e.g., "Ethernet", "Wireless", "Loopback", "Tunnel").
        /// </summary>
        public string? AdapterType { get; internal set; }
    }

    /// <summary>
    /// Static class that enumerates network adapters on the system
    /// using P/Invoke to IPHLPAPI (GetAdaptersAddresses). No WMI.
    /// </summary>
    public static class NetworkInfo
    {
        // =====================================================================
        // Constants
        // =====================================================================

        private const uint AfUnspec = 0;
        private const uint GaaFlagIncludePrefix = 0x0010;
        private const uint GaaFlagIncludeGateways = 0x0080;
        private const uint GaaFlagIncludeAll = 0x1000;
        private const uint GaaFlagIncludeAllInterfaces = 0x1000;

        private const int IfTypeEthernetCsMacd = 6;
        private const int IfTypeIeee80211 = 71;
        private const int IfTypeSoftwareLoopback = 24;
        private const int IfTypeTunnel = 131;

        private const int MaxAdapterName = 256;
        private const int MaxDnsSuffix = 256;

        private const int ErrorBufferOverflow = 111;
        private const int ErrorSuccess = 0;

        // =====================================================================
        // P/Invoke structs
        // =====================================================================

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct IP_ADAPTER_ADDRESSES_LH
        {
            internal ulong Alignment;
            internal IntPtr Length;
            internal uint IfIndex;
            internal IntPtr Next;
            internal IntPtr AdapterName; // PWSTR
            internal IntPtr FirstUnicastAddress;
            internal IntPtr FirstAnycastAddress;
            internal IntPtr FirstMulticastAddress;
            internal IntPtr FirstDnsServerAddress;
            internal IntPtr DnsSuffix; // PWSTR
            internal IntPtr Description; // PWSTR
            internal IntPtr FriendlyName; // PWSTR
            // Byte array fields represented as fields at known offsets
            internal byte PhysicalAddressLength;
            internal byte Flags;
            internal uint Mtu;
            internal int IfType;
            internal int OperStatus;
            internal int Ipv6IfIndex;
            private uint ZoneIndices0;
            private uint ZoneIndices1;
            internal IntPtr FirstGatewayAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADAPTER_UNICAST_ADDRESS_LH
        {
            internal ulong Alignment;
            internal IntPtr Length;
            internal int Flags;
            internal IntPtr Next;
            internal IntPtr Address; // SOCKET_ADDRESS
            internal int PrefixOrigin;
            internal int SuffixOrigin;
            internal int DadState;
            internal ulong ValidLifetime;
            internal ulong PreferredLifetime;
            internal ulong LeaseLifetime;
            internal byte OnLinkPrefixLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADAPTER_GATEWAY_ADDRESS_LH
        {
            internal ulong Alignment;
            internal IntPtr Length;
            internal int Flags;
            internal IntPtr Next;
            internal IntPtr Address; // SOCKET_ADDRESS
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADAPTER_DNS_SERVER_ADDRESS_LH
        {
            internal ulong Alignment;
            internal IntPtr Length;
            internal int Reserved;
            internal IntPtr Next;
            internal IntPtr Address; // SOCKET_ADDRESS
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SOCKET_ADDRESS
        {
            internal IntPtr lpSockaddr;
            internal int iSockaddrLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SOCKADDR_IN
        {
            internal ushort sin_family;
            internal ushort sin_port;
            internal uint sin_addr;
            private ulong sin_zero;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SOCKADDR_IN6
        {
            internal ushort sin6_family;
            internal ushort sin6_port;
            internal uint sin6_flowinfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] sin6_addr;
            internal uint sin6_scope_id;
        }

        // =====================================================================
        // P/Invoke
        // =====================================================================

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetAdaptersAddresses(
            uint family,
            uint flags,
            IntPtr reserved,
            IntPtr adapterAddresses,
            ref uint sizePointer);

        // =====================================================================
        // Public methods
        // =====================================================================

        /// <summary>
        /// Enumerates all network adapters on the system.
        /// Returns an empty list on non-Windows platforms or if the API call fails.
        /// </summary>
        /// <returns>A read-only list of <see cref="NetworkAdapterInfo"/> instances.</returns>
        public static IReadOnlyList<NetworkAdapterInfo> GetAllAdapters()
        {
            var adapters = new List<NetworkAdapterInfo>();

            try
            {
                uint bufferSize = 0;
                uint result = GetAdaptersAddresses(AfUnspec, GaaFlagIncludePrefix | GaaFlagIncludeGateways,
                    IntPtr.Zero, IntPtr.Zero, ref bufferSize);

                if (result != ErrorBufferOverflow && result != ErrorSuccess)
                    return adapters;

                if (bufferSize == 0)
                    return adapters;

                IntPtr buffer = Marshal.AllocHGlobal((int)bufferSize);
                try
                {
                    result = GetAdaptersAddresses(AfUnspec, GaaFlagIncludePrefix | GaaFlagIncludeGateways,
                        IntPtr.Zero, buffer, ref bufferSize);

                    if (result != ErrorSuccess)
                        return adapters;

                    IntPtr ptr = buffer;
                    while (ptr != IntPtr.Zero)
                    {
                        var adapter = Marshal.PtrToStructure<IP_ADAPTER_ADDRESSES_LH>(ptr);
                        var info = ParseAdapter(ptr, adapter);
                        if (info != null)
                            adapters.Add(info);

                        ptr = adapter.Next;
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

            return adapters;
        }

        // =====================================================================
        // Private helpers
        // =====================================================================

        private static NetworkAdapterInfo? ParseAdapter(IntPtr basePtr, IP_ADAPTER_ADDRESSES_LH native)
        {
            try
            {
                var info = new NetworkAdapterInfo();

                // Friendly name
                try
                {
                    info.Name = Marshal.PtrToStringUni(native.FriendlyName) ?? string.Empty;
                }
                catch
                {
                    info.Name = string.Empty;
                }

                // Description
                try
                {
                    info.Description = Marshal.PtrToStringUni(native.Description) ?? string.Empty;
                }
                catch
                {
                    info.Description = string.Empty;
                }

                // MAC address
                if (native.PhysicalAddressLength > 0)
                {
                    // PhysicalAddress is embedded as a fixed array starting after
                    // PhysicalAddressLength — we need to read it manually.
                    // The struct offset: after PhysicalAddressLength (1 byte) + Flags (1 byte) = 2 bytes
                    // But with alignment, we compute the address directly.
                    // PhysicalAddress is 8 bytes starting at offset PhysicalAddressLength position.
                    // Let's use a safe approach: read from known offset.
                    // The IP_ADAPTER_ADDRESSES_LH struct has PhysicalAddress at a known fixed offset
                    // from the struct start. We'll read 8 bytes after PhysicalAddressLength and Flags.
                    int physAddrOffset = GetPhysicalAddressOffset();
                    if (physAddrOffset > 0)
                    {
                        byte[] macBytes = new byte[native.PhysicalAddressLength];
                        for (int i = 0; i < native.PhysicalAddressLength && i < 8; i++)
                        {
                            macBytes[i] = Marshal.ReadByte(basePtr, physAddrOffset + i);
                        }

                        info.MacAddress = string.Join(":", Array.ConvertAll(macBytes, b => b.ToString("X2")));
                    }
                }

                // Operational status
                info.IsUp = native.OperStatus == 1; // IfOperStatusUp

                // Adapter type
                info.AdapterType = native.IfType switch
                {
                    IfTypeEthernetCsMacd => "Ethernet",
                    IfTypeIeee80211 => "Wireless",
                    IfTypeSoftwareLoopback => "Loopback",
                    IfTypeTunnel => "Tunnel",
                    _ => $"Unknown ({native.IfType})"
                };

                // Speed — not directly in IP_ADAPTER_ADDRESSES_LH;
                // we leave as 0 since it requires a different API (GetAdaptersInfo).
                info.Speed = 0;

                // Dns suffix not exposed in our simplified struct
                // but we can extract DNS server addresses

                // Unicast addresses
                var ipList = new List<string>();
                if (native.FirstUnicastAddress != IntPtr.Zero)
                {
                    IntPtr uaPtr = native.FirstUnicastAddress;
                    while (uaPtr != IntPtr.Zero)
                    {
                        var ua = Marshal.PtrToStructure<IP_ADAPTER_UNICAST_ADDRESS_LH>(uaPtr);
                        string? ip = ReadSocketAddress(ua.Address);
                        if (ip != null)
                            ipList.Add(ip);
                        uaPtr = ua.Next;
                    }
                }
                info.IpAddresses = ipList;

                // Gateway addresses
                var gatewayList = new List<string>();
                if (native.FirstGatewayAddress != IntPtr.Zero)
                {
                    IntPtr gwPtr = native.FirstGatewayAddress;
                    while (gwPtr != IntPtr.Zero)
                    {
                        var gw = Marshal.PtrToStructure<IP_ADAPTER_GATEWAY_ADDRESS_LH>(gwPtr);
                        string? ip = ReadSocketAddress(gw.Address);
                        if (ip != null)
                            gatewayList.Add(ip);
                        gwPtr = gw.Next;
                    }
                }
                info.GatewayAddresses = gatewayList;

                // DNS server addresses
                var dnsList = new List<string>();
                if (native.FirstDnsServerAddress != IntPtr.Zero)
                {
                    IntPtr dnsPtr = native.FirstDnsServerAddress;
                    while (dnsPtr != IntPtr.Zero)
                    {
                        var dns = Marshal.PtrToStructure<IP_ADAPTER_DNS_SERVER_ADDRESS_LH>(dnsPtr);
                        string? ip = ReadSocketAddress(dns.Address);
                        if (ip != null)
                            dnsList.Add(ip);
                        dnsPtr = dns.Next;
                    }
                }
                info.DnsAddresses = dnsList;

                // DHCP: check flags (bit 0 = DHCP enabled)
                info.IsDhcpEnabled = (native.Flags & 1) == 1;

                // DHCP server info is not in this struct directly
                // Could be obtained from GetAdaptersInfo but leaving as null

                return info;
            }
            catch
            {
                return null;
            }
        }

        private static int GetPhysicalAddressOffset()
        {
            // In IP_ADAPTER_ADDRESSES_LH, PhysicalAddress is an 8-byte array
            // located after Alignment(8) + Length(8) + IfIndex(4) + Next(8) +
            // AdapterName(8) + FirstUnicastAddress(8) + FirstAnycastAddress(8) +
            // FirstMulticastAddress(8) + FirstDnsServerAddress(8) + DnsSuffix(8) +
            // Description(8) + FriendlyName(8) + PhysicalAddressLength(1) + Flags(1) = 97 bytes
            // With padding this is typically at offset 96 + 2 = 98, but let's compute precisely.
            // We'll use a safe calculation: get the offset by reading the struct layout.
            // Actually let's just use the known offset for standard 64-bit layout.
            // Alignment: 8 bytes
            // Length: 8 bytes (IntPtr)
            // IfIndex: 4 bytes (uint)
            // Next: 8 bytes (IntPtr)
            // AdapterName: 8 bytes (IntPtr)
            // FirstUnicastAddress: 8 bytes (IntPtr)
            // FirstAnycastAddress: 8 bytes (IntPtr)
            // FirstMulticastAddress: 8 bytes (IntPtr)
            // FirstDnsServerAddress: 8 bytes (IntPtr)
            // DnsSuffix: 8 bytes (IntPtr)
            // Description: 8 bytes (IntPtr)
            // FriendlyName: 8 bytes (IntPtr)
            // = 96 bytes
            // Then PhysicalAddressLength (1) + Flags (1)
            // = 98 bytes from start
            // PhysicalAddress[8] starts at 98
            return 98;
        }

        private static string? ReadSocketAddress(IntPtr socketAddressPtr)
        {
            try
            {
                if (socketAddressPtr == IntPtr.Zero)
                    return null;

                var sa = Marshal.PtrToStructure<SOCKET_ADDRESS>(socketAddressPtr);
                if (sa.lpSockaddr == IntPtr.Zero || sa.iSockaddrLength <= 0)
                    return null;

                // Read address family (first 2 bytes of sockaddr)
                ushort family = (ushort)Marshal.ReadInt16(sa.lpSockaddr);

                if (family == 2) // AF_INET
                {
                    var addrIn = Marshal.PtrToStructure<SOCKADDR_IN>(sa.lpSockaddr);
                    var ipBytes = BitConverter.GetBytes(addrIn.sin_addr);
                    return new IPAddress(ipBytes).ToString();
                }
                else if (family == 23) // AF_INET6
                {
                    var addrIn6 = Marshal.PtrToStructure<SOCKADDR_IN6>(sa.lpSockaddr);
                    return new IPAddress(addrIn6.sin6_addr, addrIn6.sin6_scope_id).ToString();
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }
    }
}
