using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents license information for a Cisco WLC.
    /// </summary>
    public sealed class CiscoLicenseInfo
    {
        public string LicenseType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int EntitlementCount { get; set; }
        public int UsedCount { get; set; }
        public int AvailableCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
