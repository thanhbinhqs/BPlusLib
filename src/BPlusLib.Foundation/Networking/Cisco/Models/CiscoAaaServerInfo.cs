using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a RADIUS/TACACS+ AAA server configured on a Cisco WLC.
    /// </summary>
    public sealed class CiscoAaaServerInfo
    {
        public string ServerType { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; }
        public string AuthType { get; set; } = string.Empty;
        public int Timeout { get; set; }
        public int RetransmitCount { get; set; }
        public string Key { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int DeadTime { get; set; }
        public bool IsEnabled { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
