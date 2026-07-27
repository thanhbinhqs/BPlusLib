using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents Clean Air air quality metrics for a radio.
    /// </summary>
    public sealed class CiscoCleanAirInfo
    {
        public string ApName { get; set; } = string.Empty;
        public string ApMacAddress { get; set; } = string.Empty;
        public int SlotId { get; set; }
        public string Band { get; set; } = string.Empty;
        public int AirQuality { get; set; }
        public int AirQualityStatus { get; set; }
        public int InterferenceDeviceCount { get; set; }
        public string InterferenceType { get; set; } = string.Empty;
        public int NonWifiInterference { get; set; }
        public int WifiInterference { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
