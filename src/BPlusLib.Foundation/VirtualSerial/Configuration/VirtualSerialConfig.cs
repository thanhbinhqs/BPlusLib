using System.Collections.Generic;
using Newtonsoft.Json;

namespace BPlusLib.Foundation.VirtualSerial.Configuration
{
    /// <summary>
    /// Root configuration for the Virtual Serial platform.
    /// </summary>
    public sealed class VirtualSerialConfig
    {
        /// <summary>Configuration schema version.</summary>
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        /// <summary>Global driver settings.</summary>
        [JsonProperty("driver")]
        public DriverConfig Driver { get; set; } = new();

        /// <summary>Port definitions.</summary>
        [JsonProperty("ports")]
        public List<PortConfig> Ports { get; set; } = new();

        /// <summary>Route definitions.</summary>
        [JsonProperty("routes")]
        public List<RouteConfig> Routes { get; set; } = new();
    }

    /// <summary>
    /// Global driver configuration.
    /// </summary>
    public sealed class DriverConfig
    {
        /// <summary>Default session receive buffer size in bytes.</summary>
        [JsonProperty("defaultSessionBufferSize")]
        public int DefaultSessionBufferSize { get; set; } = 1048576;

        /// <summary>Default transmit buffer size in bytes.</summary>
        [JsonProperty("defaultTxBufferSize")]
        public int DefaultTxBufferSize { get; set; } = 1048576;

        /// <summary>Maximum number of virtual ports.</summary>
        [JsonProperty("maximumPorts")]
        public int MaximumPorts { get; set; } = 256;

        /// <summary>Maximum sessions per port.</summary>
        [JsonProperty("maximumSessionsPerPort")]
        public int MaximumSessionsPerPort { get; set; } = 64;
    }
}
