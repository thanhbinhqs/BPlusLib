using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text.Json;
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
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="CiscoDeviceInfo"/>, or an empty instance on failure.</returns>
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

                if (json.ValueKind == JsonValueKind.Undefined)
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
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of <see cref="CiscoApInfo"/> objects, or an empty list on failure.</returns>
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

                if (json.ValueKind == JsonValueKind.Undefined)
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
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of <see cref="CiscoClientInfo"/> objects, or an empty list on failure.</returns>
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

                if (json.ValueKind == JsonValueKind.Undefined)
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
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of <see cref="CiscoSsidInfo"/> objects, or an empty list on failure.</returns>
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

                if (json.ValueKind == JsonValueKind.Undefined)
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
        /// The returned <see cref="SyslogServer"/> must be disposed by the caller.
        /// </summary>
        /// <param name="port">The UDP port to listen on (default: 514).</param>
        /// <param name="onMessageReceived">Optional callback invoked when a message is received.</param>
        /// <returns>A started <see cref="SyslogServer"/> instance.</returns>
        public static SyslogServer StartSyslogListener(
            int port = 514,
            Action<CiscoSyslogEntry>? onMessageReceived = null)
        {
            var server = new SyslogServer(port);

            if (onMessageReceived != null)
            {
                server.MessageReceived += onMessageReceived;
            }

            server.Start();
            return server;
        }

        /// <summary>
        /// Pings the specified host to determine reachability.
        /// </summary>
        /// <param name="host">The IP address or hostname to ping.</param>
        /// <param name="timeoutMs">Ping timeout in milliseconds (default: 3000).</param>
        /// <returns><c>true</c> if the host replied; otherwise, <c>false</c>.</returns>
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
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username.</param>
        /// <param name="password">The RESTCONF password.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if the device responded; otherwise, <c>false</c>.</returns>
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

        private static RestConfClient CreateClient(string host, string username, string password, int port)
        {
            return new RestConfClient(host, username, password, port, ignoreCertificateErrors: true);
        }
    }
}
