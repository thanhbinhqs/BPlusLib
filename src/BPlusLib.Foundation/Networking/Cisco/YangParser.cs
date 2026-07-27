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

        /// <summary>
        /// Parses radio slot operational data from a Cisco-IOS-XE-wireless-access-point-oper YANG JSON response.
        /// Extracts RF information: channel, width, power, band for each radio slot.
        /// </summary>
        /// <param name="json">The raw JSON object from RESTCONF (access-point-oper-data).</param>
        /// <returns>A list of <see cref="CiscoRadioInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoRadioInfo> ParseRadioInfo(JObject? json)
        {
            var results = new List<CiscoRadioInfo>();
            try
            {
                if (json == null) return results;

                var apData = json["Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data"];
                if (apData == null) return results;

                var apArray = apData is JArray ? apData : new JArray(apData);
                foreach (var ap in apArray)
                {
                    if (ap is not JObject apObj) continue;

                    string apMac = GetString(apObj, "mac");
                    string apName = GetString(apObj, "name");

                    // Parse radio slots
                    var slots = apObj["slot"];
                    if (slots is JArray slotArray)
                    {
                        foreach (var slot in slotArray)
                        {
                            if (slot is JObject slotObj)
                                results.Add(ParseSingleRadio(slotObj, apMac, apName));
                        }
                    }
                    else if (slots is JObject slotObj)
                    {
                        results.Add(ParseSingleRadio(slotObj, apMac, apName));
                    }
                }
            }
            catch { }
            return results;
        }

        /// <summary>
        /// Parses RF profile configuration from a Cisco-IOS-XE-wireless-rf-cfg YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON object from RESTCONF (rf-profiles).</param>
        /// <returns>A list of <see cref="CiscoRfProfileInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoRfProfileInfo> ParseRfProfiles(JObject? json)
        {
            var results = new List<CiscoRfProfileInfo>();
            try
            {
                if (json == null) return results;

                var rfProfiles = json["Cisco-IOS-XE-wireless-rf-cfg:rf-profiles"]?["rf-profile"];
                if (rfProfiles == null) return results;

                var profileArray = rfProfiles is JArray ? rfProfiles : new JArray(rfProfiles);
                foreach (var profile in profileArray)
                {
                    if (profile is JObject profileObj)
                        results.Add(ParseSingleRfProfile(profileObj));
                }
            }
            catch { }
            return results;
        }

        /// <summary>
        /// Parses AP profile (AP group) configuration from a Cisco-IOS-XE-wireless-ap-cfg YANG JSON response.
        /// </summary>
        /// <param name="json">The raw JSON object from RESTCONF (ap-cfg).</param>
        /// <returns>A list of <see cref="CiscoApProfileInfo"/> objects, or an empty list on failure.</returns>
        public static List<CiscoApProfileInfo> ParseApProfiles(JObject? json)
        {
            var results = new List<CiscoApProfileInfo>();
            try
            {
                if (json == null) return results;

                var apProfiles = json["Cisco-IOS-XE-wireless-ap-cfg:ap-cfg"]?["ap-profile-groups"]?["ap-profile-group"];
                if (apProfiles == null) return results;

                var profileArray = apProfiles is JArray ? apProfiles : new JArray(apProfiles);
                foreach (var profile in profileArray)
                {
                    if (profile is JObject profileObj)
                        results.Add(ParseSingleApProfile(profileObj));
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

        private static CiscoRadioInfo ParseSingleRadio(JObject item, string apMac, string apName)
        {
            // Determine band from radio-type or slot-id
            string radioType = GetString(item, "radio-type");
            string band = DetermineBand(item, radioType);

            // Parse channel width from admin-channel or oper-channel-width
            int channelWidth = GetInt(item, "chan-width");
            string channelWidthString = GetString(item, "chan-width");

            // Try alternate field names for channel width
            if (channelWidth == 0)
                channelWidth = GetInt(item, "admin-chan-width");
            if (string.IsNullOrEmpty(channelWidthString))
                channelWidthString = GetString(item, "admin-chan-width");

            return new CiscoRadioInfo
            {
                ApMacAddress = apMac,
                ApName = apName,
                SlotId = GetInt(item, "slot-id"),
                Band = band,
                RadioType = radioType,
                Channel = GetInt(item, "channel"),
                ChannelWidth = channelWidth,
                ChannelWidthString = channelWidthString,
                TxPower = GetDouble(item, "power-level"),
                AdminState = GetString(item, "admin-state"),
                OperState = GetString(item, "oper-state"),
                NoiseFloor = GetDouble(item, "noise-floor"),
                ClientCount = GetInt(item, "client-count"),
                SsidName = GetString(item, "ssid-name"),
                IsPromiscuous = GetBool(item, "is-promsc"),
                Bssid = GetString(item, "bssid"),
                LastQueried = DateTimeOffset.UtcNow
            };
        }

        private static string DetermineBand(JObject slot, string radioType)
        {
            // Try to determine band from radio-type string
            if (!string.IsNullOrEmpty(radioType))
            {
                if (radioType.Contains("802.11b") || radioType.Contains("802.11g") || radioType.Contains("2.4"))
                    return "2.4GHz";
                if (radioType.Contains("802.11a") || radioType.Contains("802.11ac") || radioType.Contains("802.11ax") || radioType.Contains("5"))
                    return "5GHz";
                if (radioType.Contains("6"))
                    return "6GHz";
            }

            // Fallback: try to determine from slot-id or band field
            string bandFromField = GetString(slot, "band");
            if (!string.IsNullOrEmpty(bandFromField))
                return bandFromField;

            int slotId = GetInt(slot, "slot-id");
            return slotId switch
            {
                0 => "2.4GHz",
                1 => "5GHz",
                2 => "6GHz",
                _ => "Unknown"
            };
        }

        private static CiscoRfProfileInfo ParseSingleRfProfile(JObject item)
        {
            // Parse channel list
            var channelDef = item["channel-def"];
            string channelList = string.Empty;
            if (channelDef is JArray chArray)
            {
                var channels = new List<string>();
                foreach (var ch in chArray)
                    channels.Add(ch?.ToString() ?? string.Empty);
                channelList = string.Join(",", channels);
            }
            else if (channelDef != null)
            {
                channelList = channelDef.ToString();
            }

            return new CiscoRfProfileInfo
            {
                Name = GetString(item, "profile-name"),
                RadioBand = GetString(item, "radio-band"),
                ChannelMode = GetString(item, "channel-mode"),
                ChannelList = channelList,
                DefaultChannelWidth = GetInt(item, "chan-width"),
                ChannelWidthString = GetString(item, "chan-width"),
                TxPowerLevel = GetDouble(item, "power-level"),
                MaxTxPower = GetDouble(item, "max-tx-power-level"),
                MinTxPower = GetDouble(item, "min-tx-power-level"),
                MandatoryDataRate = GetDouble(item, "mandatory-data-rate"),
                MaxDataRate = GetDouble(item, "max-data-rate"),
                ClientMinRssi = GetInt(item, "client-min-rssi"),
                ClientDpackSensitivity = GetInt(item, "client-dpack-sensitivity"),
                RrmEnabled = GetBool(item, "rrm-auto"),
                CoverageHoleDetection = GetBool(item, "coverage-hole-detection"),
                CoverageHoleRssi = GetInt(item, "coverage-hole-rssi"),
                NeighborReportEnabled = GetBool(item, "neighbor-list"),
                Dot11bEnabled = GetBool(item, "dot11b"),
                Dot11aEnabled = GetBool(item, "dot11a"),
                Description = GetString(item, "description"),
                LastQueried = DateTimeOffset.UtcNow
            };
        }

        private static CiscoApProfileInfo ParseSingleApProfile(JObject item)
        {
            // Parse associated WLANs
            var wlans = item["wlan"];
            string associatedWlans = string.Empty;
            if (wlans is JArray wlanArray)
            {
                var wlanNames = new List<string>();
                foreach (var wlan in wlanArray)
                    wlanNames.Add(wlan?.ToString() ?? string.Empty);
                associatedWlans = string.Join(",", wlanNames);
            }
            else if (wlans != null)
            {
                associatedWlans = wlans.ToString();
            }

            return new CiscoApProfileInfo
            {
                Name = GetString(item, "profile-name"),
                Description = GetString(item, "description"),
                ApCount = GetInt(item, "ap-count"),
                RfProfile24Ghz = GetString(item, "rf-profile-name-24ghz"),
                RfProfile5Ghz = GetString(item, "rf-profile-name-5ghz"),
                RfProfile6Ghz = GetString(item, "rf-profile-name-6ghz"),
                AssociatedWlans = associatedWlans,
                Dot11bChannelMode = GetString(item, "dot11b-channel-mode"),
                Dot11aChannelMode = GetString(item, "dot11a-channel-mode"),
                Dot11bChannelList = GetString(item, "dot11b-channel-list"),
                Dot11aChannelList = GetString(item, "dot11a-channel-list"),
                Dot11bChannelWidth = GetString(item, "dot11b-channel-width"),
                Dot11aChannelWidth = GetString(item, "dot11a-channel-width"),
                Dot11bTxPower = GetDouble(item, "dot11b-power-level"),
                Dot11aTxPower = GetDouble(item, "dot11a-power-level"),
                FlexConnectVlan = GetString(item, "flex-vlan"),
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
