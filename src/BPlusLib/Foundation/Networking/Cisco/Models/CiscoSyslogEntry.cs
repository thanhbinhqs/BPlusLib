namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a single syslog message received from a Cisco WLC,
    /// parsed per RFC 5424 (The Syslog Protocol).
    /// </summary>
    public sealed class CiscoSyslogEntry
    {
        /// <summary>
        /// Gets the RFC 5424 version number (typically 1).
        /// </summary>
        public int Version { get; init; } = 1;

        /// <summary>
        /// Gets the timestamp of the syslog message.
        /// </summary>
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets the hostname or IP address of the sender.
        /// </summary>
        public string Hostname { get; init; } = string.Empty;

        /// <summary>
        /// Gets the application name on the sending device.
        /// </summary>
        public string AppName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the process ID of the generating process.
        /// </summary>
        public string ProcessId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the message ID or structured data identifier.
        /// </summary>
        public string MessageId { get; init; } = string.Empty;

        /// <summary>
        /// Gets the severity level (0–7) of the syslog message.
        /// 0 = Emergency, 1 = Alert, 2 = Critical, 3 = Error,
        /// 4 = Warning, 5 = Notice, 6 = Informational, 7 = Debug.
        /// </summary>
        public int Severity { get; init; }

        /// <summary>
        /// Gets the facility code of the syslog message.
        /// </summary>
        public int Facility { get; init; }

        /// <summary>
        /// Gets the severity level as a human-readable string.
        /// </summary>
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
            _ => $"Unknown({Severity})"
        };

        /// <summary>
        /// Gets the full message text.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets the raw syslog line as received from the UDP socket.
        /// </summary>
        public string RawMessage { get; init; } = string.Empty;

        /// <summary>
        /// Gets the IP address of the remote endpoint that sent this message.
        /// </summary>
        public string SourceIp { get; init; } = string.Empty;

        /// <summary>
        /// Gets the source port of the remote endpoint.
        /// </summary>
        public int SourcePort { get; init; }

        /// <summary>
        /// Gets the date and time this entry was received by the listener.
        /// </summary>
        public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Returns a human-readable summary of this syslog entry.
        /// </summary>
        /// <returns>A string containing the timestamp, severity, source, and message.</returns>
        public override string ToString()
        {
            return $"CiscoSyslogEntry[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{SeverityName}] {SourceIp}: {Message}";
        }
    }
}
