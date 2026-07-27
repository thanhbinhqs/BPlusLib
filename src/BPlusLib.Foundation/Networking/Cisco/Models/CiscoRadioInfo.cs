using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents radio slot information for a Cisco access point.
    /// Contains RF operational data: channel, width, power, band.
    /// </summary>
    public sealed class CiscoRadioInfo
    {
        /// <summary>The MAC address of the parent AP.</summary>
        public string ApMacAddress { get; set; } = string.Empty;

        /// <summary>The AP name.</summary>
        public string ApName { get; set; } = string.Empty;

        /// <summary>The radio slot index (0 = 2.4 GHz, 1 = 5 GHz, 2 = 6 GHz).</summary>
        public int SlotId { get; set; }

        /// <summary>The radio band: 2.4GHz, 5GHz, 6GHz.</summary>
        public string Band { get; set; } = string.Empty;

        /// <summary>The radio type string (e.g., "802.11ac", "802.11ax").</summary>
        public string RadioType { get; set; } = string.Empty;

        /// <summary>The operating channel number.</summary>
        public int Channel { get; set; }

        /// <summary>The channel width in MHz (20, 40, 80, 160).</summary>
        public int ChannelWidth { get; set; }

        /// <summary>The channel width string (e.g., "CHAN_WIDTH_20MHZ", "CHAN_WIDTH_80MHZ").</summary>
        public string ChannelWidthString { get; set; } = string.Empty;

        /// <summary>The transmit power level (dBm or percentage).</summary>
        public double TxPower { get; set; }

        /// <summary>The admin state (enabled/disabled).</summary>
        public string AdminState { get; set; } = string.Empty;

        /// <summary>The operational state.</summary>
        public string OperState { get; set; } = string.Empty;

        /// <summary>The noise floor level (dBm).</summary>
        public double NoiseFloor { get; set; }

        /// <summary>The current client count on this radio.</summary>
        public int ClientCount { get; set; }

        /// <summary>The associated SSID name.</summary>
        public string SsidName { get; set; } = string.Empty;

        /// <summary>Whether the radio is in promiscuous mode.</summary>
        public bool IsPromiscuous { get; set; }

        /// <summary>The BSSID (basic service set identifier).</summary>
        public string Bssid { get; set; } = string.Empty;

        /// <summary>When this data was last queried.</summary>
        public DateTimeOffset LastQueried { get; set; }
    }
}
