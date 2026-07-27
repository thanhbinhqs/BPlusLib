using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a Cisco Lightweight Access Point managed by the WLC.
    /// </summary>
    public sealed class CiscoApInfo
    {
        public string MacAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string OperationalStatus { get; set; } = string.Empty;
        public string SoftwareVersion { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int ClientCount { get; set; }
        public int Channel { get; set; }
        public double TxPower { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
