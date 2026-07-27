using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
        /// Parses device/global information from a Cisco-IOS-XE-native YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON object from RESTCONF.</param>
        /// <param name="defaultIpAddress">Default IP to use if not found in JSON.</param>
        /// <returns>A <see cref="CiscoDeviceInfo"/> populated from the response, or an empty instance on failure.</returns>
        public static CiscoDeviceInfo ParseDeviceInfo(JObject? json, string defaultIpAddress = "")
        {
            try
            {
                if (json == null)
                    return new CiscoDeviceInfo { IpAddress = defaultIpAddress };

                var native = json["Cisco-IOS-XE-native:native"] as JObject;
                if (native == null)
                    return new CiscoDeviceInfo { IpAddress = defaultIpAddress };

                string hostname = native["Cisco-IOS-XE-native:hostname"]?.ToString() ?? string.Empty;
                string softwareVersion = native["Cisco-IOS-XE-native:version"]?.ToString() ?? string.Empty;
                string serialNumber = (native["Cisco-IOS-XE-native:license"]?["Cisco-IOS-XE-native:udi"]?["Cisco-IOS-XE-native:sn"])?.ToString() ?? string.Empty;

                var modelToken = native["Cisco-IOS-XE-native:hardware"]?["Cisco-IOS-XE-native:model"];
                string model = string.Empty;
                if (modelToken is JArray modelArray && modelArray.Count > 0)
                    model = modelArray[0]?.ToString() ?? string.Empty;
                else if (modelToken != null)
                    model = modelToken.ToString();

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
        /// Parses access point operational data from a Cisco-IOS-XE-wireless-access-point-oper YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON object from RESTCONF.</param>
        /// <returns>A list of <see cref="CiscoApInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoApInfo> ParseAccessPoints(JObject? json)
        {
            var results = new List<CiscoApInfo>();
            try
            {
                if (json == null) return results;

                var apData = json["Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data"];

                if (apData is JArray apArray)
                {
                    foreach (var item in apArray)
                    {
                        if (item is JObject apObj)
                            results.Add(ParseSingleAp(apObj));
                    }
                }
                else if (apData is JObject apObj)
                {
                    results.Add(ParseSingleAp(apObj));
                }
            }
            catch { }
            return results;
        }

        /// <summary>
        /// Parses client operational data from a Cisco-IOS-XE-wireless-client-oper YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON object from RESTCONF.</param>
        /// <returns>A list of <see cref="CiscoClientInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoClientInfo> ParseClients(JObject? json)
        {
            var results = new List<CiscoClientInfo>();
            try
            {
                if (json == null) return results;

                var clientData = json["Cisco-IOS-XE-wireless-client-oper:client-oper-data"];

                if (clientData is JArray clientArray)
                {
                    foreach (var item in clientArray)
                    {
                        if (item is JObject clientObj)
                            results.Add(ParseSingleClient(clientObj));
                    }
                }
                else if (clientData is JObject clientObj)
                {
                    results.Add(ParseSingleClient(clientObj));
                }
            }
            catch { }
            return results;
        }

        /// <summary>
        /// Parses SSID (WLAN) operational data from a Cisco-IOS-XE-wireless-wlan-global-oper YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON object from RESTCONF.</param>
        /// <returns>A list of <see cref="CiscoSsidInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoSsidInfo> ParseSsids(JObject? json)
        {
            var results = new List<CiscoSsidInfo>();
            try
            {
                if (json == null) return results;

                var wlanData = json["Cisco-IOS-XE-wireless-wlan-global-oper:wlan-global-oper-data"]?["wlan-data"];

                if (wlanData is JArray wlanArray)
                {
                    foreach (var item in wlanArray)
                    {
                        if (item is JObject wlanObj)
                            results.Add(ParseSingleSsid(wlanObj));
                    }
                }
                else if (wlanData is JObject wlanObj)
                {
                    results.Add(ParseSingleSsid(wlanObj));
                }
            }
            catch { }
            return results;
        }

        #region Private Helpers

        private static CiscoApInfo ParseSingleAp(JObject item)
        {
            return new CiscoApInfo
            {
                MacAddress = GetString(item, "mac"),
                Name = GetString(item, "name"),
                OperationalStatus = GetString(item, "ap-serial"),
                SoftwareVersion = GetString(item, "software-version"),
                Model = GetString(item, "ap-model"),
                SerialNumber = GetString(item, "ap-serial"),
                Location = GetString(item, "location"),
                IpAddress = GetString(item, "ap-ip-addr"),
                ClientCount = GetInt(item, "client-count"),
                Channel = GetInt(item, "channel"),
                TxPower = GetDouble(item, "tx-power"),
                LastQueried = DateTimeOffset.UtcNow
            };
        }

        private static CiscoClientInfo ParseSingleClient(JObject item)
        {
            return new CiscoClientInfo
            {
                MacAddress = GetString(item, "mac"),
                IpAddress = GetString(item, "ipv4-addr"),
                Hostname = GetString(item, "device-name"),
                UserName = GetString(item, "username"),
                Ssid = GetString(item, "wlan-id"),
                ApMacAddress = GetString(item, "ap-mac"),
                ApName = GetString(item, "ap-name"),
                RadioBand = GetString(item, "radio-type"),
                Channel = GetInt(item, "channel"),
                Rssi = GetInt(item, "rssi"),
                DataRate = GetDouble(item, "data-rate"),
                Status = GetString(item, "state"),
                AuthMethod = GetString(item, "auth-type"),
                LastQueried = DateTimeOffset.UtcNow
            };
        }

        private static CiscoSsidInfo ParseSingleSsid(JObject item)
        {
            return new CiscoSsidInfo
            {
                ProfileName = GetString(item, "profile-name"),
                Ssid = GetString(item, "ssid"),
                VlanId = GetInt(item, "vlan-id"),
                IsEnabled = GetBool(item, "admin-status"),
                ClientCount = GetInt(item, "client-count"),
                SecurityMode = GetString(item, "security-mode"),
                AuthType = GetString(item, "auth-type"),
                RadioPolicy = GetString(item, "radio-policy"),
                WlanId = GetInt(item, "wlan-id"),
                LastQueried = DateTimeOffset.UtcNow
            };
        }

        private static string GetString(JToken element, string propertyName)
        {
            try
            {
                var token = element[propertyName];
                if (token == null) return string.Empty;
                if (token is JValue jv)
                    return jv.Value?.ToString() ?? string.Empty;
                return token.ToString() ?? string.Empty;
            }
            catch { return string.Empty; }
        }

        private static int GetInt(JToken element, string propertyName)
        {
            try
            {
                var token = element[propertyName];
                if (token is JValue jv && jv.Value != null)
                    return Convert.ToInt32(jv.Value);
            }
            catch { }
            return 0;
        }

        private static double GetDouble(JToken element, string propertyName)
        {
            try
            {
                var token = element[propertyName];
                if (token is JValue jv && jv.Value != null)
                    return Convert.ToDouble(jv.Value);
            }
            catch { }
            return 0.0;
        }

        private static bool GetBool(JToken element, string propertyName)
        {
            try
            {
                var token = element[propertyName];
                if (token is JValue jv && jv.Value != null)
                    return Convert.ToBoolean(jv.Value);
            }
            catch { }
            return false;
        }

        #endregion
    }
}
