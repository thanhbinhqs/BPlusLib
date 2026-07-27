using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents an AP profile (AP group) configured on a Cisco WLC.
    /// AP groups define which WLANs, RF profiles, and policies are applied to a set of APs.
    /// </summary>
    public sealed class CiscoApProfileInfo
    {
        /// <summary>The AP profile (group) name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>The profile description.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>The number of APs in this group.</summary>
        public int ApCount { get; set; }

        /// <summary>The 2.4 GHz RF profile name applied to this group.</summary>
        public string RfProfile24Ghz { get; set; } = string.Empty;

        /// <summary>The 5 GHz RF profile name applied to this group.</summary>
        public string RfProfile5Ghz { get; set; } = string.Empty;

        /// <summary>The 6 GHz RF profile name applied to this group.</summary>
        public string RfProfile6Ghz { get; set; } = string.Empty;

        /// <summary>The associated WLAN profile names (comma-separated).</summary>
        public string AssociatedWlans { get; set; } = string.Empty;

        /// <summary>The 802.11b/g channel mode (auto/custom).</summary>
        public string Dot11bChannelMode { get; set; } = string.Empty;

        /// <summary>The 802.11a channel mode (auto/custom).</summary>
        public string Dot11aChannelMode { get; set; } = string.Empty;

        /// <summary>The 802.11b/g channel list.</summary>
        public string Dot11bChannelList { get; set; } = string.Empty;

        /// <summary>The 802.11a channel list.</summary>
        public string Dot11aChannelList { get; set; } = string.Empty;

        /// <summary>The 802.11b/g channel width.</summary>
        public string Dot11bChannelWidth { get; set; } = string.Empty;

        /// <summary>The 802.11a channel width.</summary>
        public string Dot11aChannelWidth { get; set; } = string.Empty;

        /// <summary>The 802.11b/g Tx power level.</summary>
        public double Dot11bTxPower { get; set; }

        /// <summary>The 802.11a Tx power level.</summary>
        public double Dot11aTxPower { get; set; }

        /// <summary>The FlexConnect VLAN configuration.</summary>
        public string FlexConnectVlan { get; set; } = string.Empty;

        /// <summary>When this data was last queried.</summary>
        public DateTimeOffset LastQueried { get; set; }
    }
}
