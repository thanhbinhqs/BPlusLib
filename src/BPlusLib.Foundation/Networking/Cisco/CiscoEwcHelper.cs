using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using BPlusLib.Foundation.Networking.Cisco.Models;

namespace BPlusLib.Foundation.Networking.Cisco
{
    /// <summary>
    /// Static facade for interacting with Cisco EWC (Embedded Wireless Controller) devices
    /// via RESTCONF and syslog. All methods are thread-safe and return empty/null on failure.
    /// </summary>
    public static class CiscoEwcHelper
    {
        /// <summary>
        /// Gets device information from a Cisco WLC via RESTCONF.
        /// </summary>
        public static async Task<CiscoDeviceInfo> GetDeviceInfoAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync("/restconf/data/Cisco-IOS-XE-native:native", cancellationToken)
                    .ConfigureAwait(false);
                if (json == null)
                    return new CiscoDeviceInfo { IpAddress = host };
                return YangParser.ParseDeviceInfo(json, host);
            }
            catch
            {
                return new CiscoDeviceInfo { IpAddress = host };
            }
        }

        /// <summary>
        /// Gets all access points managed by a Cisco WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoApInfo>> GetAccessPointsAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoApInfo>();
                return YangParser.ParseAccessPoints(json);
            }
            catch
            {
                return new List<CiscoApInfo>();
            }
        }

        /// <summary>
        /// Gets all wireless clients associated to a Cisco WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoClientInfo>> GetClientsAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-client-oper:client-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoClientInfo>();
                return YangParser.ParseClients(json);
            }
            catch
            {
                return new List<CiscoClientInfo>();
            }
        }

        /// <summary>
        /// Gets all SSIDs (WLANs) configured on a Cisco WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoSsidInfo>> GetSsidsAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-wlan-global-oper:wlan-global-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoSsidInfo>();
                return YangParser.ParseSsids(json);
            }
            catch
            {
                return new List<CiscoSsidInfo>();
            }
        }

        /// <summary>
        /// Gets radio slot information (RF data) for all access points via RESTCONF.
        /// Returns channel, channel width, power, band, and operational state for each radio.
        /// </summary>
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of <see cref="CiscoRadioInfo"/> objects, or an empty list on failure.</returns>
        public static async Task<List<CiscoRadioInfo>> GetRadioInfoAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoRadioInfo>();
                return YangParser.ParseRadioInfo(json);
            }
            catch
            {
                return new List<CiscoRadioInfo>();
            }
        }

        /// <summary>
        /// Gets RF profile configuration from the WLC via RESTCONF.
        /// RF profiles define radio frequency settings: band, channel, power, data rate, thresholds.
        /// </summary>
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of <see cref="CiscoRfProfileInfo"/> objects, or an empty list on failure.</returns>
        public static async Task<List<CiscoRfProfileInfo>> GetRfProfilesAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-rf-cfg:rf-profiles",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoRfProfileInfo>();
                return YangParser.ParseRfProfiles(json);
            }
            catch
            {
                return new List<CiscoRfProfileInfo>();
            }
        }

        /// <summary>
        /// Gets AP profile (AP group) configuration from the WLC via RESTCONF.
        /// AP groups define which WLANs, RF profiles, and policies are applied to APs.
        /// </summary>
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of <see cref="CiscoApProfileInfo"/> objects, or an empty list on failure.</returns>
        public static async Task<List<CiscoApProfileInfo>> GetApProfilesAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-ap-cfg:ap-cfg",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoApProfileInfo>();
                return YangParser.ParseApProfiles(json);
            }
            catch
            {
                return new List<CiscoApProfileInfo>();
            }
        }

        /// <summary>
        /// Gets radio information for a specific access point by MAC address.
        /// </summary>
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="apMac">The MAC address of the AP (e.g., "aa:bb:cc:dd:ee:ff").</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of <see cref="CiscoRadioInfo"/> for the specified AP, or an empty list on failure.</returns>
        public static async Task<List<CiscoRadioInfo>> GetRadioInfoForApAsync(
            string host,
            string username,
            string password,
            string apMac,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var allRadios = await GetRadioInfoAsync(host, username, password, port, cancellationToken)
                    .ConfigureAwait(false);
                var result = new List<CiscoRadioInfo>();
                foreach (var radio in allRadios)
                {
                    if (string.Equals(radio.ApMacAddress, apMac, StringComparison.OrdinalIgnoreCase))
                        result.Add(radio);
                }
                return result;
            }
            catch
            {
                return new List<CiscoRadioInfo>();
            }
        }

        /// <summary>
        /// Creates and starts a syslog listener for receiving messages from Cisco WLCs.
        /// </summary>
        public static SyslogServer StartSyslogListener(
            int port = 514,
            Action<CiscoSyslogEntry>? onMessageReceived = null)
        {
            var server = new SyslogServer(port);
            if (onMessageReceived != null)
                server.MessageReceived += onMessageReceived;
            server.Start();
            return server;
        }

        /// <summary>
        /// Pings the specified host to determine reachability.
        /// </summary>
        public static bool Ping(string host, int timeoutMs = 3000)
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send(host, timeoutMs);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Asynchronously checks whether a Cisco WLC is reachable via RESTCONF.
        /// </summary>
        public static async Task<bool> IsReachableAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                return await client.IsReachableAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the raw YANG data from a specific RESTCONF path.
        /// </summary>
        public static async Task<string?> GetYangDataAsync(
            string host,
            string username,
            string password,
            string yangPath,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                return await client.GetRawAsync($"/restconf/data{yangPath}", cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the RESTCONF capabilities of the WLC.
        /// </summary>
        public static async Task<Newtonsoft.Json.Linq.JObject?> GetCapabilitiesAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                return await client.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets AAA (Authentication, Authorization, Accounting) server configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoAaaServerInfo>> GetAaaServersAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-aaa:aaa",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoAaaServerInfo>();
                return YangParser.ParseAaaServers(json);
            }
            catch
            {
                return new List<CiscoAaaServerInfo>();
            }
        }

        /// <summary>
        /// Gets QoS (Quality of Service) profile configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoQosProfileInfo>> GetQosProfilesAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-qos-cfg:qos-cfg",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoQosProfileInfo>();
                return YangParser.ParseQosProfiles(json);
            }
            catch
            {
                return new List<CiscoQosProfileInfo>();
            }
        }

        /// <summary>
        /// Gets rogue access point detection data from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoRogueApInfo>> GetRogueApsAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-rogue-oper:rogue-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoRogueApInfo>();
                return YangParser.ParseRogueAps(json);
            }
            catch
            {
                return new List<CiscoRogueApInfo>();
            }
        }

        /// <summary>
        /// Gets WIPS (Wireless Intrusion Prevention System) alert data from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoWipsAlertInfo>> GetWipsAlertsAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-wips-oper:wips-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoWipsAlertInfo>();
                return YangParser.ParseWipsAlerts(json);
            }
            catch
            {
                return new List<CiscoWipsAlertInfo>();
            }
        }

        /// <summary>
        /// Gets license information (UDI) from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoLicenseInfo>> GetLicenseInfoAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-native:native/license/udi",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoLicenseInfo>();
                return YangParser.ParseLicenses(json);
            }
            catch
            {
                return new List<CiscoLicenseInfo>();
            }
        }

        /// <summary>
        /// Gets mobility peer configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoMobilityInfo>> GetMobilityPeersAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-mobility:mobility",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoMobilityInfo>();
                return YangParser.ParseMobilityPeers(json);
            }
            catch
            {
                return new List<CiscoMobilityInfo>();
            }
        }

        /// <summary>
        /// Gets ACL (Access Control List) configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoAclInfo>> GetAclsAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-acl-cfg:acl-cfg",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoAclInfo>();
                return YangParser.ParseAcls(json);
            }
            catch
            {
                return new List<CiscoAclInfo>();
            }
        }

        /// <summary>
        /// Gets management configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<CiscoManagementInfo> GetManagementInfoAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-native:native/management",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new CiscoManagementInfo();
                return YangParser.ParseManagement(json);
            }
            catch
            {
                return new CiscoManagementInfo();
            }
        }

        /// <summary>
        /// Gets access point operational statistics from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoStatisticsInfo>> GetStatisticsAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoStatisticsInfo>();
                return YangParser.ParseStatistics(json);
            }
            catch
            {
                return new List<CiscoStatisticsInfo>();
            }
        }

        /// <summary>
        /// Gets FlexConnect configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoFlexConnectInfo>> GetFlexConnectAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-flexconnect-cfg:flexconnect-cfg",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoFlexConnectInfo>();
                return YangParser.ParseFlexConnect(json);
            }
            catch
            {
                return new List<CiscoFlexConnectInfo>();
            }
        }

        /// <summary>
        /// Gets AP authorization configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoApAuthorizationInfo>> GetApAuthorizationAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-ap-cfg:ap-cfg",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoApAuthorizationInfo>();
                return YangParser.ParseApAuthorization(json);
            }
            catch
            {
                return new List<CiscoApAuthorizationInfo>();
            }
        }

        /// <summary>
        /// Gets CleanAir interference detection data from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoCleanAirInfo>> GetCleanAirAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-cleanair-oper:cleanair-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoCleanAirInfo>();
                return YangParser.ParseCleanAir(json);
            }
            catch
            {
                               return new List<CiscoCleanAirInfo>();
            }
        }

        /// <summary>
        /// Gets wired client operational data from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoWiredClientInfo>> GetWiredClientsAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-wired-client-oper:wired-client-oper-data",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoWiredClientInfo>();
                return YangParser.ParseWiredClients(json);
            }
            catch
            {
                return new List<CiscoWiredClientInfo>();
            }
        }

        /// <summary>
        /// Gets mesh network configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<List<CiscoMeshInfo>> GetMeshInfoAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-wireless-mesh-cfg:mesh-cfg",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new List<CiscoMeshInfo>();
                return YangParser.ParseMeshInfo(json);
            }
            catch
            {
                return new List<CiscoMeshInfo>();
            }
        }

        /// <summary>
        /// Gets NTP (Network Time Protocol) configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<CiscoNtpInfo> GetNtpInfoAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-native:native/clock",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new CiscoNtpInfo();
                return YangParser.ParseNtp(json);
            }
            catch
            {
                return new CiscoNtpInfo();
            }
        }

        /// <summary>
        /// Gets DNS (Domain Name System) configuration from the WLC via RESTCONF.
        /// </summary>
        public static async Task<CiscoDnsInfo> GetDnsInfoAsync(
            string host,
            string username,
            string password,
            int port = 443,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = CreateClient(host, username, password, port);
                var json = await client.GetAsync(
                    "/restconf/data/Cisco-IOS-XE-native:native/ip/name-server",
                    cancellationToken).ConfigureAwait(false);
                if (json == null)
                    return new CiscoDnsInfo();
                return YangParser.ParseDns(json);
            }
            catch
            {
                return new CiscoDnsInfo();
            }
        }

        private static RestConfClient CreateClient(string host, string username, string password, int port)
        {
            return new RestConfClient(host, username, password, port, ignoreCertificateErrors: true);
        }
    }
}
