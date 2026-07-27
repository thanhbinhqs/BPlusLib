using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents device information for a Cisco EWC / WLC.
    /// </summary>
    public sealed class CiscoDeviceInfo
    {
        public string IpAddress { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string SoftwareVersion { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public string SystemUptime { get; set; } = string.Empty;
        public DateTimeOffset LastQueried { get; set; }
    }
}
