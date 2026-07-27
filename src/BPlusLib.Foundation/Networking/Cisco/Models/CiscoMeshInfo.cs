using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents mesh networking configuration for an AP.
    /// </summary>
    public sealed class CiscoMeshInfo
    {
        public string ApName { get; set; } = string.Empty;
        public string ApMacAddress { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string BridgeGroupId { get; set; } = string.Empty;
        public string HopCount { get; set; } = string.Empty;
        public string Backhaul { get; set; } = string.Empty;
        public string Parent { get; set; } = string.Empty;
        public bool IsMeshEnabled { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
