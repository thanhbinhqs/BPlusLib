using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents a QoS profile configured on a Cisco WLC.
    /// </summary>
    public sealed class CiscoQosProfileInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AverageDataRate { get; set; }
        public int BurstDataRate { get; set; }
        public int AverageVoiceRate { get; set; }
        public int BurstVoiceRate { get; set; }
        public string QosDirection { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
