using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a parsed RFC 5424 syslog message received from a Cisco WLC.
    /// </summary>
    public sealed class CiscoSyslogEntry
    {
        public int Version { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string Hostname { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string ProcessId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public int Severity { get; set; }
        public int Facility { get; set; }
        public string SeverityName => Severity switch
        {
            0 => "Emergency",
            1 => "Alert",
            2 => "Critical",
            3 => "Error",
            4 => "Warning",
            5 => "Notice",
            6 => "Informational",
            7 => "Debug",
            _ => "Unknown"
        };
        public string Message { get; set; } = string.Empty;
        public string RawMessage { get; set; } = string.Empty;
        public string SourceIp { get; set; } = string.Empty;
        public int SourcePort { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
    }
}
