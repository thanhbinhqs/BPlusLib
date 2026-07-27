namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents Cisco Wireless LAN Controller (WLC) device information retrieved via RESTCONF/YANG.
    /// </summary>
    public sealed class CiscoDeviceInfo
    {
        /// <summary>
        /// Gets the device's IP address.
        /// </summary>
        public string IpAddress { get; init; } = string.Empty;

        /// <summary>
        /// Gets the device's hostname.
        /// </summary>
        public string Hostname { get; init; } = string.Empty;

        /// <summary>
        /// Gets the device's hardware model identifier.
        /// </summary>
        public string Model { get; init; } = string.Empty;

        /// <summary>
        /// Gets the device's serial number.
        /// </summary>
        public string SerialNumber { get; init; } = string.Empty;

        /// <summary>
        /// Gets the running software version.
        /// </summary>
        public string SoftwareVersion { get; init; } = string.Empty;

        /// <summary>
        /// Gets the firmware/build version string.
        /// </summary>
        public string FirmwareVersion { get; init; } = string.Empty;

        /// <summary>
        /// Gets the device's system uptime as reported by YANG.
        /// </summary>
        public string SystemUptime { get; init; } = string.Empty;

        /// <summary>
        /// Gets the date and time the device information was last queried.
        /// </summary>
        public DateTimeOffset LastQueried { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Returns a human-readable summary of this device info.
        /// </summary>
        /// <returns>A string containing the hostname, model, and software version.</returns>
        public override string ToString()
        {
            return $"CiscoDeviceInfo[Hostname={Hostname}, Model={Model}, SW={SoftwareVersion}, IP={IpAddress}]";
        }
    }
}
