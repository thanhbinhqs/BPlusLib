using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents radio/WLAN statistics and counters.
    /// </summary>
    public sealed class CiscoStatisticsInfo
    {
        public string ApName { get; set; } = string.Empty;
        public string ApMacAddress { get; set; } = string.Empty;
        public int SlotId { get; set; }
        public string Band { get; set; } = string.Empty;
        public long TotalClients { get; set; }
        public long TxBytes { get; set; }
        public long RxBytes { get; set; }
        public long TxFrames { get; set; }
        public long RxFrames { get; set; }
        public long TxErrors { get; set; }
        public long RxErrors { get; set; }
        public long TxRetries { get; set; }
        public long RxRetries { get; set; }
        public int NoiseFloor { get; set; }
        public double Utilization { get; set; }
        public int ClientCount { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
