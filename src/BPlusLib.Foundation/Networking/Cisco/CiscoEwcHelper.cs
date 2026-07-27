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

        private static RestConfClient CreateClient(string host, string username, string password, int port)
        {
            return new RestConfClient(host, username, password, port, ignoreCertificateErrors: true);
        }
    }
}
