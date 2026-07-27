using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents NTP and time configuration on the WLC.
    /// </summary>
    public sealed class CiscoNtpInfo
    {
        public string NtpServer1 { get; set; } = string.Empty;
        public string NtpServer2 { get; set; } = string.Empty;
        public string NtpServer3 { get; set; } = string.Empty;
        public bool NtpEnabled { get; set; }
        public string TimeZone { get; set; } = string.Empty;
        public string TimeZoneOffset { get; set; } = string.Empty;
        public string DaylightSaving { get; set; } = string.Empty;
        public string SystemTime { get; set; } = string.Empty;
        public DateTimeOffset LastQueried { get; set; }
    }
}
