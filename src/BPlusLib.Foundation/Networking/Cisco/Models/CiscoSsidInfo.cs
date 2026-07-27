using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents an SSID (WLAN) configured on a Cisco WLC.
    /// </summary>
    public sealed class CiscoSsidInfo
    {
        public string ProfileName { get; set; } = string.Empty;
        public string Ssid { get; set; } = string.Empty;
        public int VlanId { get; set; }
        public bool IsEnabled { get; set; }
        public int ClientCount { get; set; }
        public string SecurityMode { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
        public string RadioPolicy { get; set; } = string.Empty;
        public int WlanId { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
