using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents an RF profile configured on a Cisco WLC.
    /// Contains radio frequency configuration: band, channel, power, data rate, thresholds.
    /// </summary>
    public sealed class CiscoRfProfileInfo
    {
        /// <summary>The RF profile name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The radio band this profile applies to (2.4GHz, 5GHz, 6GHz).</summary>
        public string RadioBand { get; set; } = string.Empty;

        /// <summary>The channel assignment mode (auto, specific).</summary>
        public string ChannelMode { get; set; } = string.Empty;

        /// <summary>The channel list (comma-separated, e.g., "1,6,11" or "36,40,44,48").</summary>
        public string ChannelList { get; set; } = string.Empty;

        /// <summary>The default channel width (20, 40, 80, 160 MHz).</summary>
        public int DefaultChannelWidth { get; set; }

        /// <summary>The channel width string (e.g., "CHAN_WIDTH_20MHZ").</summary>
        public string ChannelWidthString { get; set; } = string.Empty;

        /// <summary>The transmit power level (percentage or dBm).</summary>
        public double TxPowerLevel { get; set; }

        /// <summary>The maximum power level allowed.</summary>
        public double MaxTxPower { get; set; }

        /// <summary>The minimum power level allowed.</summary>
        public double MinTxPower { get; set; }

        /// <summary>The mandatory data rate (Mbps).</summary>
        public double MandatoryDataRate { get; set; }

        /// <summary>The maximum data rate supported (Mbps).</summary>
        public double MaxDataRate { get; set; }

        /// <summary>The client minimum RSSI threshold (dBm).</summary>
        public int ClientMinRssi { get; set; }

        /// <summary>The client disconnect threshold (dBm).</summary>
        public int ClientDpackSensitivity { get; set; }

        /// <summary>Whether RRM (Radio Resource Management) is enabled.</summary>
        public bool RrmEnabled { get; set; }

        /// <summary>Whether coverage-hole detection is enabled.</summary>
        public bool CoverageHoleDetection { get; set; }

        /// <summary>The coverage-hole RSSI threshold (dBm).</summary>
        public int CoverageHoleRssi { get; set; }

        /// <summary>Whether 802.11k neighbor reports are enabled.</summary>
        public bool NeighborReportEnabled { get; set; }

        /// <summary>Whether 802.11b/g is enabled (for 2.4 GHz).</summary>
        public bool Dot11bEnabled { get; set; }

        /// <summary>Whether 802.11a is enabled (for 5 GHz).</summary>
        public bool Dot11aEnabled { get; set; }

        /// <summary>The profile description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>When this data was last queried.</summary>
        public DateTimeOffset LastQueried { get; set; }
    }
}
