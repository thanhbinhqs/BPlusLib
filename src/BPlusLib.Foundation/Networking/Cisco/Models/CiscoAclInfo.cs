using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a wireless ACL configured on a Cisco WLC.
    /// </summary>
    public sealed class CiscoAclInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public int RuleCount { get; set; }
        public bool IsEnabled { get; set; }
        public string AclType { get; set; } = string.Empty;
        public DateTimeOffset LastQueried { get; set; }
    }
}
