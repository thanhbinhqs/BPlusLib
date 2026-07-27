namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a wireless SSID (WLAN) configured on the Cisco WLC,
    /// parsed from the wlan-global-oper-data YANG model.
    /// </summary>
    public sealed class CiscoSsidInfo
    {
        /// <summary>
        /// Gets the WLAN profile name (internal identifier).
        /// </summary>
        public string ProfileName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the SSID name broadcast to clients.
        /// </summary>
        public string Ssid { get; init; } = string.Empty;

        /// <summary>
        /// Gets the VLAN ID assigned to this WLAN.
        /// </summary>
        public int VlanId { get; init; }

        /// <summary>
        /// Gets the administrative status (enabled/disabled).
        /// </summary>
        public bool IsEnabled { get; init; }

        /// <summary>
        /// Gets the number of currently associated clients.
        /// </summary>
        public int ClientCount { get; init; }

        /// <summary>
        /// Gets the security mode (e.g., "Open", "WPA2-Enterprise", "WPA3-SAE").
        /// </summary>
        public string SecurityMode { get; init; } = string.Empty;

        /// <summary>
        /// Gets the authentication type (e.g., PSK, 802.1X, SAE).
        /// </summary>
        public string AuthType { get; init; } = string.Empty;

        /// <summary>
        /// Gets the radio policy (e.g., "Both", "2.4 GHz Only", "5 GHz Only").
        /// </summary>
        public string RadioPolicy { get; init; } = string.Empty;

        /// <summary>
        /// Gets the WLAN ID within the WLC.
        /// </summary>
        public int WlanId { get; init; }

        /// <summary>
        /// Gets the date and time this SSID information was last queried.
        /// </summary>
        public DateTimeOffset LastQueried { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Returns a human-readable summary of this SSID info.
        /// </summary>
        /// <returns>A string containing the SSID, VLAN, security mode, and client count.</returns>
        public override string ToString()
        {
            return $"CiscoSsidInfo[SSID={Ssid}, VLAN={VlanId}, Security={SecurityMode}, Clients={ClientCount}, Enabled={IsEnabled}]";
        }
    }
}
