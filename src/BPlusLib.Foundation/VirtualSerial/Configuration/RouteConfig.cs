using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using BPlusLib.Foundation.VirtualSerial.Endpoints;
using BPlusLib.Foundation.VirtualSerial.Routing;

namespace BPlusLib.Foundation.VirtualSerial.Configuration
{
    /// <summary>
    /// Configuration for a route between endpoints.
    /// </summary>
    public sealed class RouteConfig
    {
        /// <summary>Unique route identifier.</summary>
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("D");

        /// <summary>Human-readable name.</summary>
        [JsonProperty("name")]
        public string? Name { get; set; }

        /// <summary>Route type.</summary>
        [JsonProperty("type")]
        public required string Type { get; set; }

        /// <summary>Source endpoint name or ID.</summary>
        [JsonProperty("source")]
        public string? Source { get; set; }

        /// <summary>Multiple sources (for splitter routes).</summary>
        [JsonProperty("sources")]
        public List<string> Sources { get; set; } = new();

        /// <summary>Destination endpoint name or ID.</summary>
        [JsonProperty("destination")]
        public string? Destination { get; set; }

        /// <summary>Multiple destinations (for broadcast/splitter routes).</summary>
        [JsonProperty("destinations")]
        public List<string> Destinations { get; set; } = new();

        /// <summary>Physical port name (for physical splitter routes).</summary>
        [JsonProperty("physicalPort")]
        public string? PhysicalPort { get; set; }

        /// <summary>Virtual port names (for physical splitter routes).</summary>
        [JsonProperty("virtualPorts")]
        public List<string> VirtualPorts { get; set; } = new();

        /// <summary>How received data is distributed.</summary>
        [JsonProperty("receiveDistribution")]
        public ReceiveDistribution ReceiveDistribution { get; set; } = ReceiveDistribution.Broadcast;

        /// <summary>How concurrent writes are handled.</summary>
        [JsonProperty("writePolicy")]
        public WriteArbitrationPolicy WritePolicy { get; set; } = WriteArbitrationPolicy.Serialized;

        /// <summary>Frame boundary detection.</summary>
        [JsonProperty("frameBoundary")]
        public FrameBoundary FrameBoundary { get; set; } = FrameBoundary.Raw;

        /// <summary>Whether modem signal mapping is enabled (for pairs).</summary>
        [JsonProperty("modemSignalMapping")]
        public bool ModemSignalMapping { get; set; } = true;

        /// <summary>Whether this route is enabled.</summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>TCP client/server settings.</summary>
        [JsonProperty("tcp")]
        public TcpConfig? Tcp { get; set; }

        /// <summary>UDP settings.</summary>
        [JsonProperty("udp")]
        public UdpConfig? Udp { get; set; }
    }

    /// <summary>
    /// TCP endpoint configuration.
    /// </summary>
    public sealed class TcpConfig
    {
        [JsonProperty("remoteHost")]
        public string? RemoteHost { get; set; }

        [JsonProperty("remotePort")]
        public int RemotePort { get; set; }

        [JsonProperty("listenAddress")]
        public string? ListenAddress { get; set; }

        [JsonProperty("listenPort")]
        public int ListenPort { get; set; }

        [JsonProperty("connectTimeoutMs")]
        public int ConnectTimeoutMs { get; set; } = 5000;

        [JsonProperty("keepAlive")]
        public bool KeepAlive { get; set; } = true;

        [JsonProperty("noDelay")]
        public bool NoDelay { get; set; } = true;

        [JsonProperty("autoReconnect")]
        public bool AutoReconnect { get; set; } = true;

        [JsonProperty("reconnectDelayMs")]
        public int ReconnectDelayMs { get; set; } = 5000;

        [JsonProperty("maximumClients")]
        public int MaximumClients { get; set; } = 32;
    }

    /// <summary>
    /// UDP endpoint configuration.
    /// </summary>
    public sealed class UdpConfig
    {
        [JsonProperty("remoteHost")]
        public string? RemoteHost { get; set; }

        [JsonProperty("remotePort")]
        public int RemotePort { get; set; }

        [JsonProperty("localPort")]
        public int LocalPort { get; set; }

        [JsonProperty("maximumPacketSize")]
        public int MaximumPacketSize { get; set; } = 1400;
    }
}
