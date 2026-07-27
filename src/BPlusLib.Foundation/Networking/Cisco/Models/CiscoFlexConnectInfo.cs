using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents FlexConnect configuration for an AP or AP group.
    /// </summary>
    public sealed class CiscoFlexConnectInfo
    {
        public string ApName { get; set; } = string.Empty;
        public string ApMacAddress { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string AuthList { get; set; } = string.Empty;
        public string Vlan { get; set; } = string.Empty;
        public string NativeVlan { get; set; } = string.Empty;
        public string JumboFrame { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
