namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a Cisco Lightweight Access Point (AP) managed by the WLC,
    /// parsed from the access-point-oper-data YANG model.
    /// </summary>
    public sealed class CiscoApInfo
    {
        /// <summary>
        /// Gets the access point's MAC address.
        /// </summary>
        public string MacAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets the access point's name (AP Ethernet MAC or user-assigned name).
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets the radio band or operational status summary.
        /// </summary>
        public string OperationalStatus { get; init; } = string.Empty;

        /// <summary>
        /// Gets the access point's software version.
        /// </summary>
        public string SoftwareVersion { get; init; } = string.Empty;

        /// <summary>
        /// Gets the access point's hardware model.
        /// </summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets the serial number of the access point.
        /// </summary>
        public string SerialNumber { get; init; } = string.Empty;

        /// <summary>
        /// Gets the location description assigned to the access point.
        /// </summary>
        public string Location { get; init; } = string.Empty;

        /// <summary>
        /// Gets the access point's IP address.
        /// </summary>
        public string IpAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets the total number of associated clients on this access point.
        /// </summary>
        public int ClientCount { get; init; }

        /// <summary>
        /// Gets the radio channel used by the primary radio interface.
        /// </summary>
        public int Channel { get; init; }

        /// <summary>
        /// Gets the transmit power level (in dBm or percent).
        /// </summary>
        public double TxPower { get; init; }

        /// <summary>
        /// Gets the date and time this AP information was last queried.
        /// </summary>
        public DateTimeOffset LastQueried { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Returns a human-readable summary of this AP info.
        /// </summary>
        /// <returns>A string containing the AP name, MAC, and client count.</returns>
        public override string ToString()
        {
            return $"CiscoApInfo[Name={Name}, MAC={MacAddress}, Clients={ClientCount}, Status={OperationalStatus}]";
        }
    }
}
