using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents mobility peer/anchor information for inter-WLC roaming.
    /// </summary>
    public sealed class CiscoMobilityInfo
    {
        public string PeerIpAddress { get; set; } = string.Empty;
        public string PeerName { get; set; } = string.Empty;
        public string PeerMacAddress { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public bool IsAnchor { get; set; }
        public int TunnelType { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
