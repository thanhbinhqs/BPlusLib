using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a wireless client connected to a Cisco WLC.
    /// </summary>
    public sealed class CiscoClientInfo
    {
        public string MacAddress { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Ssid { get; set; } = string.Empty;
        public string ApMacAddress { get; set; } = string.Empty;
        public string ApName { get; set; } = string.Empty;
        public string RadioBand { get; set; } = string.Empty;
        public int Channel { get; set; }
        public int Rssi { get; set; }
        public double DataRate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AuthMethod { get; set; } = string.Empty;
        public DateTimeOffset LastQueried { get; set; }
    }
}
