using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BPlusLib.Foundation.VirtualSerial.Routing;

namespace BPlusLib.Foundation.VirtualSerial.Configuration
{
    /// <summary>
    /// Loads and saves VirtualSerial configuration from JSON files.
    /// </summary>
    public static class ConfigurationLoader
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include
        };

        /// <summary>
        /// Load configuration from a JSON file.
        /// </summary>
        public static VirtualSerialConfig LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Configuration file not found: {filePath}");

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return LoadFromJson(json);
        }

        /// <summary>
        /// Load configuration from a JSON string.
        /// </summary>
        public static VirtualSerialConfig LoadFromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON content is empty.", nameof(json));

            var config = JsonConvert.DeserializeObject<VirtualSerialConfig>(json, JsonSettings);
            if (config == null)
                throw new InvalidOperationException("Failed to deserialize configuration.");

            Validate(config);
            return config;
        }

        /// <summary>
        /// Save configuration to a JSON file.
        /// </summary>
        public static void SaveToFile(VirtualSerialConfig config, string filePath)
        {
            Validate(config);

            string json = JsonConvert.SerializeObject(config, JsonSettings);
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        /// <summary>
        /// Serialize configuration to JSON string.
        /// </summary>
        public static string ToJson(VirtualSerialConfig config)
        {
            Validate(config);
            return JsonConvert.SerializeObject(config, JsonSettings);
        }

        /// <summary>
        /// Create a default configuration with sample ports and routes.
        /// </summary>
        public static VirtualSerialConfig CreateDefault()
        {
            return new VirtualSerialConfig
            {
                Version = 1,
                Driver = new DriverConfig
                {
                    DefaultSessionBufferSize = 1048576,
                    DefaultTxBufferSize = 1048576,
                    MaximumPorts = 256,
                    MaximumSessionsPerPort = 64
                },
                Ports = new()
                {
                    new PortConfig
                    {
                        Id = Guid.NewGuid().ToString("D"),
                        Name = "COM20",
                        FriendlyName = "Virtual Pair A",
                        MultiOpen = true,
                        ReadDistribution = ReceiveDistribution.Broadcast,
                        WritePolicy = WriteArbitrationPolicy.Serialized,
                        SessionBufferSize = 1048576,
                        OverflowPolicy = OverflowPolicy.DropOldest,
                        Persist = true
                    },
                    new PortConfig
                    {
                        Id = Guid.NewGuid().ToString("D"),
                        Name = "COM21",
                        FriendlyName = "Virtual Pair B",
                        MultiOpen = true,
                        ReadDistribution = ReceiveDistribution.Broadcast,
                        WritePolicy = WriteArbitrationPolicy.Serialized,
                        SessionBufferSize = 1048576,
                        OverflowPolicy = OverflowPolicy.DropOldest,
                        Persist = true
                    }
                },
                Routes = new()
                {
                    new RouteConfig
                    {
                        Id = Guid.NewGuid().ToString("D"),
                        Name = "Pair COM20 ↔ COM21",
                        Type = "pair",
                        Source = "COM20",
                        Destination = "COM21",
                        ModemSignalMapping = true,
                        Enabled = true
                    }
                }
            };
        }

        /// <summary>
        /// Validate configuration.
        /// </summary>
        public static void Validate(VirtualSerialConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Version < 1) throw new InvalidOperationException("Configuration version must be >= 1.");
            if (config.Ports == null) throw new InvalidOperationException("Ports list is null.");
            if (config.Routes == null) throw new InvalidOperationException("Routes list is null.");
            if (config.Driver == null) throw new InvalidOperationException("Driver config is null.");

            // Validate port names are unique
            var portNames = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var port in config.Ports)
            {
                if (string.IsNullOrWhiteSpace(port.Name))
                    throw new InvalidOperationException($"Port '{port.Id}' has no name.");

                if (!portNames.Add(port.Name))
                    throw new InvalidOperationException($"Duplicate port name: {port.Name}");
            }
        }
    }
}
