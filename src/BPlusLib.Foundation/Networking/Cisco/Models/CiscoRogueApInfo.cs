using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a rogue access point detected by the WLC.
    /// </summary>
    public sealed class CiscoRogueApInfo
    {
        public string MacAddress { get; set; } = string.Empty;
        public string RadioType { get; set; } = string.Empty;
        public int Channel { get; set; }
        public int Rssi { get; set; }
        public string Classification { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string DetectedBy { get; set; } = string.Empty;
        public string ContainmentState { get; set; } = string.Empty;
        public int ContainmentLevel { get; set; }
        public string FirstSeen { get; set; } = string.Empty;
        public string LastSeen { get; set; } = string.Empty;
        public DateTimeOffset LastQueried { get; set; }
    }
}
