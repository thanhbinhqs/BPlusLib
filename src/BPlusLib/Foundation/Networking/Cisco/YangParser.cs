using System;
using System.Collections.Generic;
using System.Text.Json;
using BPlusLib.Foundation.Networking.Cisco.Models;

namespace BPlusLib.Foundation.Networking.Cisco
{
    /// <summary>
    /// Parses Cisco IOS XE / EWC YANG model JSON responses into strongly-typed model objects.
    /// All methods are static and thread-safe.
    /// </summary>
    public static class YangParser
    {
        /// <summary>
        /// Parses device/global information from a Cisco-IOS-XE-native or
        /// Cisco-IOS-XE-wireless-wlan-global-oper YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON element from RESTCONF.</param>
        /// <param name="defaultIpAddress">Default IP to use if not found in JSON.</param>
        /// <returns>A <see cref="CiscoDeviceInfo"/> populated from the response, or an empty instance on failure.</returns>
        public static CiscoDeviceInfo ParseDeviceInfo(JsonElement json, string defaultIpAddress = "")
        {
            try
            {
                string hostname = JsonHelpers.GetStringValue(json, "Cisco-IOS-XE-native:native/Cisco-IOS-XE-native:hostname");
                string model = JsonHelpers.GetStringValue(json, "Cisco-IOS-XE-native:native/Cisco-IOS-XE-native:hardware/Cisco-IOS-XE-native:model[0]");
                string serialNumber = JsonHelpers.GetStringValue(json, "Cisco-IOS-XE-native:native/Cisco-IOS-XE-native:license/Cisco-IOS-XE-native:udi/Cisco-IOS-XE-native:sn");
                string softwareVersion = JsonHelpers.GetStringValue(json, "Cisco-IOS-XE-native:native/Cisco-IOS-XE-native:version");

                return new CiscoDeviceInfo
                {
                    IpAddress = defaultIpAddress,
                    Hostname = hostname,
                    Model = model,
                    SerialNumber = serialNumber,
                    SoftwareVersion = softwareVersion,
                    FirmwareVersion = softwareVersion,
                    LastQueried = DateTimeOffset.UtcNow
                };
            }
            catch
            {
                return new CiscoDeviceInfo { IpAddress = defaultIpAddress };
            }
        }

        /// <summary>
        /// Parses access point operational data from a Cisco-IOS-XE-wireless-access-point-oper
        /// YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON element from RESTCONF.</param>
        /// <returns>A list of <see cref="CiscoApInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoApInfo> ParseAccessPoints(JsonElement json)
        {
            var results = new List<CiscoApInfo>();
            try
            {
                var apData = JsonHelpers.NavigateJsonPath(json,
                    "Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data");

                if (apData.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in apData.EnumerateArray())
                    {
                        results.Add(ParseSingleAp(item));
                    }
                }
                else if (apData.ValueKind == JsonValueKind.Object)
                {
                    results.Add(ParseSingleAp(apData));
                }
            }
            catch
            {
                // Return empty list on parse failure.
            }
            return results;
        }

        /// <summary>
        /// Parses client operational data from a Cisco-IOS-XE-wireless-client-oper
        /// YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON element from RESTCONF.</param>
        /// <returns>A list of <see cref="CiscoClientInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoClientInfo> ParseClients(JsonElement json)
        {
            var results = new List<CiscoClientInfo>();
            try
            {
                var clientData = JsonHelpers.NavigateJsonPath(json,
                    "Cisco-IOS-XE-wireless-client-oper:client-oper-data");

                if (clientData.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in clientData.EnumerateArray())
                    {
                        results.Add(ParseSingleClient(item));
                    }
                }
                else if (clientData.ValueKind == JsonValueKind.Object)
                {
                    results.Add(ParseSingleClient(clientData));
                }
            }
            catch
            {
                // Return empty list on parse failure.
            }
            return results;
        }

        /// <summary>
        /// Parses SSID (WLAN) operational data from a Cisco-IOS-XE-wireless-wlan-global-oper
        /// YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON element from RESTCONF.</param>
        /// <returns>A list of <see cref="CiscoSsidInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoSsidInfo> ParseSsids(JsonElement json)
        {
            var results = new List<CiscoSsidInfo>();
            try
            {
                var wlanData = JsonHelpers.NavigateJsonPath(json,
                    "Cisco-IOS-XE-wireless-wlan-global-oper:wlan-global-oper-data/wlan-data");

                if (wlanData.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in wlanData.EnumerateArray())
                    {
                        results.Add(ParseSingleSsid(item));
                    }
                }
                else if (wlanData.ValueKind == JsonValueKind.Object)
                {
                    results.Add(ParseSingleSsid(wlanData));
                }
            }
            catch
            {
                // Return empty list on parse failure.
            }
            return results;
        }

        #region Private Helpers

        private static CiscoApInfo ParseSingleAp(JsonElement item)
        {
            return new CiscoApInfo
            {
                MacAddress = JsonHelpers.GetStringValue(item, "mac"),
                Name = JsonHelpers.GetStringValue(item, "name"),
                OperationalStatus = JsonHelpers.GetStringValue(item, "ap-serial"),
                SoftwareVersion = JsonHelpers.GetStringValue(item, "software-version"),
                Model = JsonHelpers.GetStringValue(item, "ap-model"),
                SerialNumber = JsonHelpers.GetStringValue(item, "ap-serial"),
                Location = JsonHelpers.GetStringValue(item, "location"),
                IpAddress = JsonHelpers.GetStringValue(item, "ap-ip-addr"),
                ClientCount = JsonHelpers.GetIntValue(item, "client-count"),
                Channel = JsonHelpers.GetIntValue(item, "channel"),
                TxPower = JsonHelpers.GetDoubleValue(item, "tx-power"),
                LastQueried = DateTimeOffset.UtcNow
            };
        }

        private static CiscoClientInfo ParseSingleClient(JsonElement item)
        {
            return new CiscoClientInfo
            {
                MacAddress = JsonHelpers.GetStringValue(item, "mac"),
                IpAddress = JsonHelpers.GetStringValue(item, "ipv4-addr"),
                Hostname = JsonHelpers.GetStringValue(item, "device-name"),
                UserName = JsonHelpers.GetStringValue(item, "username"),
                Ssid = JsonHelpers.GetStringValue(item, "wlan-id"),
                ApMacAddress = JsonHelpers.GetStringValue(item, "ap-mac"),
                ApName = JsonHelpers.GetStringValue(item, "ap-name"),
                RadioBand = JsonHelpers.GetStringValue(item, "radio-type"),
                Channel = JsonHelpers.GetIntValue(item, "channel"),
                Rssi = JsonHelpers.GetIntValue(item, "rssi"),
                DataRate = JsonHelpers.GetDoubleValue(item, "data-rate"),
                Status = JsonHelpers.GetStringValue(item, "state"),
                AuthMethod = JsonHelpers.GetStringValue(item, "auth-type"),
                LastQueried = DateTimeOffset.UtcNow
            };
        }

        private static CiscoSsidInfo ParseSingleSsid(JsonElement item)
        {
            return new CiscoSsidInfo
            {
                ProfileName = JsonHelpers.GetStringValue(item, "profile-name"),
                Ssid = JsonHelpers.GetStringValue(item, "ssid"),
                VlanId = JsonHelpers.GetIntValue(item, "vlan-id"),
                IsEnabled = JsonHelpers.GetBoolValue(item, "admin-status"),
                ClientCount = JsonHelpers.GetIntValue(item, "client-count"),
                SecurityMode = JsonHelpers.GetStringValue(item, "security-mode"),
                AuthType = JsonHelpers.GetStringValue(item, "auth-type"),
                RadioPolicy = JsonHelpers.GetStringValue(item, "radio-policy"),
                WlanId = JsonHelpers.GetIntValue(item, "wlan-id"),
                LastQueried = DateTimeOffset.UtcNow
            };
        }

        #endregion

        /// <summary>
        /// Internal helper methods for safe JSON navigation and value extraction.
        /// </summary>
        internal static class JsonHelpers
        {
            /// <summary>
            /// Navigates a dot-separated path through a JSON element.
            /// Returns <c>default</c> if any segment is missing.
            /// </summary>
            public static JsonElement NavigateJsonPath(JsonElement root, string path)
            {
                var segments = path.Split('/');
                var current = root;

                foreach (var segment in segments)
                {
                    if (string.IsNullOrEmpty(segment))
                        continue;

                    if (current.ValueKind == JsonValueKind.Object &&
                        current.TryGetProperty(segment, out var next))
                    {
                        current = next;
                    }
                    else
                    {
                        return default;
                    }
                }

                return current;
            }

            /// <summary>
            /// Gets a string value from a JSON object property. Returns <see cref="string.Empty"/> on failure.
            /// </summary>
            public static string GetStringValue(JsonElement element, string propertyName)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Object &&
                        element.TryGetProperty(propertyName, out var value))
                    {
                        return value.ValueKind == JsonValueKind.String
                            ? value.GetString() ?? string.Empty
                            : value.ToString();
                    }
                }
                catch
                {
                    // Fall through to return empty.
                }
                return string.Empty;
            }

            /// <summary>
            /// Gets an integer value from a JSON object property. Returns 0 on failure.
            /// </summary>
            public static int GetIntValue(JsonElement element, string propertyName)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Object &&
                        element.TryGetProperty(propertyName, out var value) &&
                        value.ValueKind == JsonValueKind.Number)
                    {
                        return value.GetInt32();
                    }
                }
                catch
                {
                    // Fall through to return 0.
                }
                return 0;
            }

            /// <summary>
            /// Gets a double value from a JSON object property. Returns 0.0 on failure.
            /// </summary>
            public static double GetDoubleValue(JsonElement element, string propertyName)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Object &&
                        element.TryGetProperty(propertyName, out var value) &&
                        value.ValueKind == JsonValueKind.Number)
                    {
                        return value.GetDouble();
                    }
                }
                catch
                {
                    // Fall through to return 0.0.
                }
                return 0.0;
            }

            /// <summary>
            /// Gets a boolean value from a JSON object property. Returns <c>false</c> on failure.
            /// </summary>
            public static bool GetBoolValue(JsonElement element, string propertyName)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Object &&
                        element.TryGetProperty(propertyName, out var value) &&
                        value.ValueKind == JsonValueKind.True)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Fall through to return false.
                }
                return false;
            }
        }
    }
}
