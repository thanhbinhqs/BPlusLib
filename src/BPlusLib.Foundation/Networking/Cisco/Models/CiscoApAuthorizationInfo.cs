using System;

namespace BPlusLib.Foundation.Networking.Cisco.Models
{
    /// <summary>
    /// Represents an AP authorization entry (AP auth list).
    /// </summary>
    public sealed class CiscoApAuthorizationInfo
    {
        public string MacAddress { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AuthState { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public bool IsAuthorized { get; set; }
        public DateTimeOffset LastQueried { get; set; }
    }
}
