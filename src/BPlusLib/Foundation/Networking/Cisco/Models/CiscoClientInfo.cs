namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a wireless client associated to a Cisco WLC,
    /// parsed from the client-oper-data YANG model.
    /// </summary>
    public sealed class CiscoClientInfo
    {
        /// <summary>
        /// Gets the client's MAC address.
        /// </summary>
        public string MacAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets the client's IP address (IPv4 or IPv6).
        /// </summary>
        public string IpAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets the client's hostname or device name.
        /// </summary>
        public string Hostname { get; init; } = string.Empty;

        /// <summary>
        /// Gets the username the client authenticated as (if available).
        /// </summary>
        public string UserName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the SSID the client is associated to.
        /// </summary>
        public string Ssid { get; init; } = string.Empty;

        /// <summary>
        /// Gets the AP MAC address the client is connected to.
        /// </summary>
        public string ApMacAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets the AP name the client is connected to.
        /// </summary>
        public string ApName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the radio band (e.g., "2.4 GHz", "5 GHz").
        /// </summary>
        public string RadioBand { get; init; } = string.Empty;

        /// <summary>
        /// Gets the channel the client is operating on.
        /// </summary>
        public int Channel { get; init; }

        /// <summary>
        /// Gets the client's signal strength (RSSI) in dBm.
        /// </summary>
        public int Rssi { get; init; }

        /// <summary>
        /// Gets the data rate (Mbps).
        /// </summary>
        public double DataRate { get; init; }

        /// <summary>
        /// Gets the client's operational status (e.g., "Associated", "Disassociated").
        /// </summary>
        public string Status { get; init; } = string.Empty;

        /// <summary>
        /// Gets the authentication method used (e.g., WPA2-Enterprise, Open).
        /// </summary>
        public string AuthMethod { get; init; } = string.Empty;

        /// <summary>
        /// Gets the date and time this client information was last queried.
        /// </summary>
        public DateTimeOffset LastQueried { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Returns a human-readable summary of this client info.
        /// </summary>
        /// <returns>A string containing the MAC, hostname, SSID, and status.</returns>
        public override string ToString()
        {
            return $"CiscoClientInfo[MAC={MacAddress}, Host={Hostname}, SSID={Ssid}, Status={Status}]";
        }
    }
}
