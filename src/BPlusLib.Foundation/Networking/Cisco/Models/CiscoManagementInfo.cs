using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents management interface configuration (SNMP, SSH, HTTPS).
    /// </summary>
    public sealed class CiscoManagementInfo
    {
        public string SshVersion { get; set; } = string.Empty;
        public int SshPort { get; set; }
        public bool SshEnabled { get; set; }
        public bool HttpEnabled { get; set; }
        public bool HttpsEnabled { get; set; }
        public int HttpsPort { get; set; }
        public string SnmpVersion { get; set; } = string.Empty;
        public string SnmpCommunity { get; set; } = string.Empty;
        public bool SnmpEnabled { get; set; }
        public string TelnetState { get; set; } = string.Empty;
        public string ConsoleTimeout { get; set; } = string.Empty;
        public DateTimeOffset LastQueried { get; set; }
    }
}
