using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a WIPS (Wireless Intrusion Prevention System) alert.
    /// </summary>
    public sealed class CiscoWipsAlertInfo
    {
        public string AlertType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string SourceMac { get; set; } = string.Empty;
        public string SourceApName { get; set; } = string.Empty;
        public string TargetMac { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
