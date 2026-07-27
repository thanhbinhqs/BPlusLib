using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a wired client connected to an AP Ethernet port.
    /// </summary>
    public sealed class CiscoWiredClientInfo
    {
        public string MacAddress { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string ApName { get; set; } = string.Empty;
        public string ApMacAddress { get; set; } = string.Empty;
        public string Interface { get; set; } = string.Empty;
        public string Vlan { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long TxBytes { get; set; }
        public long RxBytes { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
