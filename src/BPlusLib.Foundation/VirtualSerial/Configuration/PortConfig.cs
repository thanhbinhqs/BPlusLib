using System;
using Newtonsoft.Json;
using BPlusLib.Foundation.VirtualSerial.Endpoints;
using BPlusLib.Foundation.VirtualSerial.Routing;

namespace BPlusLib.Foundation.VirtualSerial.Configuration
{
    /// <summary>
    /// Configuration for a single virtual serial port.
    /// </summary>
    public sealed class PortConfig
    {
        /// <summary>Unique port identifier.</summary>
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString("D");

        /// <summary>COM port name (e.g., "COM20").</summary>
        [JsonProperty("name")]
        public required string Name { get; set; }

        /// <summary>Friendly display name.</summary>
        [JsonProperty("friendlyName")]
        public string? FriendlyName { get; set; }

        /// <summary>Allow multiple simultaneous opens.</summary>
        [JsonProperty("multiOpen")]
        public bool MultiOpen { get; set; } = true;

        /// <summary>How data is distributed to multiple sessions.</summary>
        [JsonProperty("readDistribution")]
        public ReceiveDistribution ReadDistribution { get; set; } = ReceiveDistribution.Broadcast;

        /// <summary>How concurrent writes are handled.</summary>
        [JsonProperty("writePolicy")]
        public WriteArbitrationPolicy WritePolicy { get; set; } = WriteArbitrationPolicy.Serialized;

        /// <summary>Session receive buffer size in bytes.</summary>
        [JsonProperty("sessionBufferSize")]
        public int SessionBufferSize { get; set; } = 1048576;

        /// <summary>Behavior when buffer is full.</summary>
        [JsonProperty("overflowPolicy")]
        public OverflowPolicy OverflowPolicy { get; set; } = OverflowPolicy.DropOldest;

        /// <summary>Whether to persist this port across reboots.</summary>
        [JsonProperty("persist")]
        public bool Persist { get; set; } = true;

        /// <summary>Serial port settings.</summary>
        [JsonProperty("serialSettings")]
        public SerialSettingsConfig? SerialSettings { get; set; }
    }

    /// <summary>
    /// Serial port settings for configuration.
    /// </summary>
    public sealed class SerialSettingsConfig
    {
        [JsonProperty("baudRate")]
        public int BaudRate { get; set; } = 9600;

        [JsonProperty("dataBits")]
        public int DataBits { get; set; } = 8;

        [JsonProperty("parity")]
        public string Parity { get; set; } = "None";

        [JsonProperty("stopBits")]
        public string StopBits { get; set; } = "One";

        [JsonProperty("handshake")]
        public string Handshake { get; set; } = "None";

        /// <summary>Converts to SerialSettings model.</summary>
        public SerialSettings ToSerialSettings() => new()
        {
            BaudRate = BaudRate,
            DataBits = DataBits,
            Parity = Parity.ToLowerInvariant() switch
            {
                "odd" => ParityMode.Odd,
                "even" => ParityMode.Even,
                "mark" => ParityMode.Mark,
                "space" => ParityMode.Space,
                _ => ParityMode.None
            },
            StopBits = StopBits.ToLowerInvariant() switch
            {
                "1.5" or "onepointfive" => StopBitsMode.OnePointFive,
                "2" or "two" => StopBitsMode.Two,
                _ => StopBitsMode.One
            },
            Handshake = Handshake.ToLowerInvariant() switch
            {
                "xonxoff" or "xoff" => HandshakeMode.XOnXOff,
                "requesttosend" or "rts" => HandshakeMode.RequestToSend,
                "rtsxonxoff" => HandshakeMode.RequestToSendXOnXOff,
                _ => HandshakeMode.None
            }
        };
    }
}
