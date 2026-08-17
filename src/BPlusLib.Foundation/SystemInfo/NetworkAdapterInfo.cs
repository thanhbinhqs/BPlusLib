// <copyright file="NetworkAdapterInfo.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BPlusLib.Foundation.SystemInfo
{
    /// <summary>
    /// Provides information about a single network adapter —
    /// name, description, MAC address, IP configuration, DHCP status,
    /// speed, and adapter type.
    /// </summary>
    public sealed class NetworkAdapterInfo
    {
        internal NetworkAdapterInfo()
        {
        }

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
    /// Enumerates network adapters using managed <see cref="NetworkInterface"/> APIs.
    /// This avoids fragile manual native structure parsing and remains compatible with
    /// .NET Framework and modern Windows targets.
    /// </summary>
    public static class NetworkInfo
    {
        /// <summary>
        /// Enumerates all network adapters on the system.
        /// Returns an empty list if enumeration fails.
        /// </summary>
        public static IReadOnlyList<NetworkAdapterInfo> GetAllAdapters()
        {
            var adapters = new List<NetworkAdapterInfo>();

            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    try
                    {
                        adapters.Add(CreateAdapterInfo(nic));
                    }
                    catch
                    {
                        // Skip a single problematic adapter and keep enumerating.
                    }
                }
            }
            catch
            {
                // Return empty list on unsupported platform / API failure.
            }

            return adapters;
        }

        private static NetworkAdapterInfo CreateAdapterInfo(NetworkInterface nic)
        {
            IPInterfaceProperties properties = nic.GetIPProperties();
            var ipAddresses = new List<string>();
            var gatewayAddresses = new List<string>();
            var dnsAddresses = new List<string>();

            foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
            {
                if (address?.Address != null)
                    ipAddresses.Add(address.Address.ToString());
            }

            foreach (GatewayIPAddressInformation gateway in properties.GatewayAddresses)
            {
                if (gateway?.Address != null)
                    gatewayAddresses.Add(gateway.Address.ToString());
            }

            foreach (System.Net.IPAddress dns in properties.DnsAddresses)
            {
                if (dns != null)
                    dnsAddresses.Add(dns.ToString());
            }

            string? dhcpServer = null;
            try
            {
                foreach (System.Net.IPAddress dns in properties.DhcpServerAddresses)
                {
                    if (dns != null)
                    {
                        dhcpServer = dns.ToString();
                        break;
                    }
                }
            }
            catch
            {
                // Some adapters/platforms do not expose DHCP server addresses.
            }

            return new NetworkAdapterInfo
            {
                Name = nic.Name ?? string.Empty,
                Description = nic.Description ?? string.Empty,
                MacAddress = FormatMacAddress(nic.GetPhysicalAddress()),
                IpAddresses = ipAddresses,
                GatewayAddresses = gatewayAddresses,
                DnsAddresses = dnsAddresses,
                IsDhcpEnabled = GetIsDhcpEnabled(properties),
                DhcpServer = dhcpServer,
                IsUp = nic.OperationalStatus == OperationalStatus.Up,
                Speed = nic.Speed,
                AdapterType = MapAdapterType(nic.NetworkInterfaceType),
            };
        }

        private static bool GetIsDhcpEnabled(IPInterfaceProperties properties)
        {
            try
            {
                IPv4InterfaceProperties? ipv4 = properties.GetIPv4Properties();
                return ipv4 != null && ipv4.IsDhcpEnabled;
            }
            catch
            {
                return false;
            }
        }

        private static string? FormatMacAddress(PhysicalAddress? address)
        {
            if (address == null)
                return null;

            byte[] bytes = address.GetAddressBytes();
            if (bytes.Length == 0)
                return null;

            return string.Join(":", Array.ConvertAll(bytes, static b => b.ToString("X2")));
        }

        private static string MapAdapterType(NetworkInterfaceType interfaceType)
        {
            switch (interfaceType)
            {
                case NetworkInterfaceType.Ethernet:
                case NetworkInterfaceType.Ethernet3Megabit:
                case NetworkInterfaceType.FastEthernetFx:
                case NetworkInterfaceType.FastEthernetT:
                case NetworkInterfaceType.GigabitEthernet:
                    return "Ethernet";

                case NetworkInterfaceType.Wireless80211:
                    return "Wireless";

                case NetworkInterfaceType.Loopback:
                    return "Loopback";

                case NetworkInterfaceType.Tunnel:
                case NetworkInterfaceType.Ppp:
                    return "Tunnel";

                default:
                    return interfaceType.ToString();
            }
        }
    }
}
