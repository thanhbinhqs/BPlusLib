using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents DNS configuration on the WLC.
    /// </summary>
    public sealed class CiscoDnsInfo
    {
        public string DomainName { get; set; } = string.Empty;
        public string NameServer1 { get; set; } = string.Empty;
        public string NameServer2 { get; set; } = string.Empty;
        public string NameServer3 { get; set; } = string.Empty;
        public bool DnsEnabled { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
