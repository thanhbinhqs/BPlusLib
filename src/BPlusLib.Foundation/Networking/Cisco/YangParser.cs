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

        // ─────────────────────────────────────────────────────────────────────
        // 1. AAA Servers
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses RADIUS/TACACS+ AAA server configuration.
        /// </summary>
        public static List<CiscoAaaServerInfo> ParseAaaServers(JObject? json)
        {
            var results = new List<CiscoAaaServerInfo>();
            try
            {
                if (json == null) return results;
                var servers = json["Cisco-IOS-XE-aaa:aaa"]?["radius"]?["servers"]?["server"];
                if (servers == null) return results;
                var arr = servers is JArray ? servers : new JArray(servers);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoAaaServerInfo
                        {
                            ServerType = GetString(obj, "server-type"),
                            IpAddress = GetString(obj, "address"),
                            Port = GetInt(obj, "auth-port"),
                            Key = GetString(obj, "key"),
                            State = GetString(obj, "state"),
                            Timeout = GetInt(obj, "timeout"),
                            RetransmitCount = GetInt(obj, "retransmit"),
                            DeadTime = GetInt(obj, "dead-time"),
                            IsEnabled = GetBool(obj, "state"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. QoS Profiles
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses QoS profile configuration.
        /// </summary>
        public static List<CiscoQosProfileInfo> ParseQosProfiles(JObject? json)
        {
            var results = new List<CiscoQosProfileInfo>();
            try
            {
                if (json == null) return results;
                var profiles = json["Cisco-IOS-XE-wireless-qos-cfg:qos-cfg"]?["qos-profile"];
                if (profiles == null) return results;
                var arr = profiles is JArray ? profiles : new JArray(profiles);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoQosProfileInfo
                        {
                            Name = GetString(obj, "profile-name"),
                            Description = GetString(obj, "description"),
                            AverageDataRate = GetInt(obj, "avg-data-rate"),
                            BurstDataRate = GetInt(obj, "burst-data-rate"),
                            AverageVoiceRate = GetInt(obj, "avg-voice-rate"),
                            BurstVoiceRate = GetInt(obj, "burst-voice-rate"),
                            QosDirection = GetString(obj, "direction"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. Rogue APs
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses rogue access point operational data.
        /// </summary>
        public static List<CiscoRogueApInfo> ParseRogueAps(JObject? json)
        {
            var results = new List<CiscoRogueApInfo>();
            try
            {
                if (json == null) return results;
                var rogues = json["Cisco-IOS-XE-wireless-rogue-oper:rogue-oper-data"]?["rogue"];
                if (rogues == null) return results;
                var arr = rogues is JArray ? rogues : new JArray(rogues);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoRogueApInfo
                        {
                            MacAddress = GetString(obj, "rogue-mac"),
                            RadioType = GetString(obj, "radio-type"),
                            Channel = GetInt(obj, "channel"),
                            Rssi = GetInt(obj, "rssi"),
                            Classification = GetString(obj, "classification"),
                            State = GetString(obj, "state"),
                            Severity = GetString(obj, "severity"),
                            DetectedBy = GetString(obj, "detected-by"),
                            ContainmentState = GetString(obj, "containment-state"),
                            ContainmentLevel = GetInt(obj, "containment-level"),
                            FirstSeen = GetString(obj, "first-time"),
                            LastSeen = GetString(obj, "last-time"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. WIPS Alerts
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses WIPS (Wireless Intrusion Prevention System) alert data.
        /// </summary>
        public static List<CiscoWipsAlertInfo> ParseWipsAlerts(JObject? json)
        {
            var results = new List<CiscoWipsAlertInfo>();
            try
            {
                if (json == null) return results;
                var alerts = json["Cisco-IOS-XE-wireless-wips-oper:wips-oper-data"]?["wips-alert"];
                if (alerts == null) return results;
                var arr = alerts is JArray ? alerts : new JArray(alerts);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        DateTimeOffset ts = DateTimeOffset.MinValue;
                        var tsToken = obj["timestamp"];
                        if (tsToken != null && DateTimeOffset.TryParse(tsToken.ToString(), out var parsed))
                            ts = parsed;

                        results.Add(new CiscoWipsAlertInfo
                        {
                            AlertType = GetString(obj, "alert-type"),
                            Severity = GetString(obj, "severity"),
                            SourceMac = GetString(obj, "source-mac"),
                            SourceApName = GetString(obj, "source-ap-name"),
                            TargetMac = GetString(obj, "target-mac"),
                            Description = GetString(obj, "description"),
                            State = GetString(obj, "state"),
                            Timestamp = ts,
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. Licenses
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses license / UDI information.
        /// </summary>
        public static List<CiscoLicenseInfo> ParseLicenses(JObject? json)
        {
            var results = new List<CiscoLicenseInfo>();
            try
            {
                if (json == null) return results;
                var licenseSection = json["Cisco-IOS-XE-native:native"]?["license"];
                if (licenseSection == null) return results;

                // Try to parse feature-based licenses under "feature" array
                var features = licenseSection["feature"];
                if (features != null)
                {
                    var arr = features is JArray ? features : new JArray(features);
                    foreach (var item in arr)
                    {
                        if (item is JObject obj)
                        {
                            results.Add(new CiscoLicenseInfo
                            {
                                LicenseType = GetString(obj, "feature-name"),
                                Description = GetString(obj, "description"),
                                EntitlementCount = GetInt(obj, "count"),
                                UsedCount = GetInt(obj, "used"),
                                AvailableCount = GetInt(obj, "available"),
                                Status = GetString(obj, "status"),
                                ExpiryDate = GetString(obj, "expiry"),
                                IsEnabled = GetBool(obj, "enabled"),
                                LastQueried = DateTimeOffset.UtcNow
                            });
                        }
                    }
                }

                // Also parse the UDI section for a single entry if no features found
                if (results.Count == 0)
                {
                    var udi = licenseSection["udi"];
                    if (udi is JObject udiObj)
                    {
                        results.Add(new CiscoLicenseInfo
                        {
                            LicenseType = "UDI",
                            Description = GetString(udiObj, "pid"),
                            Status = GetString(udiObj, "sn"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. Mobility Peers
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses mobility peer/anchor configuration.
        /// </summary>
        public static List<CiscoMobilityInfo> ParseMobilityPeers(JObject? json)
        {
            var results = new List<CiscoMobilityInfo>();
            try
            {
                if (json == null) return results;
                var peers = json["Cisco-IOS-XE-wireless-mobility:mobility"]?["peer"];
                if (peers == null) return results;
                var arr = peers is JArray ? peers : new JArray(peers);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoMobilityInfo
                        {
                            PeerIpAddress = GetString(obj, "peer-ip-address"),
                            PeerName = GetString(obj, "peer-name"),
                            PeerMacAddress = GetString(obj, "peer-mac-address"),
                            State = GetString(obj, "state"),
                            Type = GetString(obj, "type"),
                            GroupId = GetString(obj, "group-id"),
                            IsAnchor = GetBool(obj, "anchor"),
                            TunnelType = GetInt(obj, "tunnel-type"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. ACLs
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses wireless ACL configuration.
        /// </summary>
        public static List<CiscoAclInfo> ParseAcls(JObject? json)
        {
            var results = new List<CiscoAclInfo>();
            try
            {
                if (json == null) return results;
                var acls = json["Cisco-IOS-XE-wireless-acl-cfg:acl-cfg"]?["acl"];
                if (acls == null) return results;
                var arr = acls is JArray ? acls : new JArray(acls);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoAclInfo
                        {
                            Name = GetString(obj, "acl-name"),
                            Description = GetString(obj, "description"),
                            Direction = GetString(obj, "direction"),
                            RuleCount = GetInt(obj, "rule-count"),
                            AclType = GetString(obj, "acl-type"),
                            IsEnabled = GetBool(obj, "enabled"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 8. Management
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses management interface configuration (SSH, HTTP, SNMP, Telnet).
        /// </summary>
        public static CiscoManagementInfo ParseManagement(JObject? json)
        {
            try
            {
                if (json == null)
                    return new CiscoManagementInfo { LastQueried = DateTimeOffset.UtcNow };

                var mgmt = json["Cisco-IOS-XE-native:native"]?["management"];
                if (mgmt is not JObject mgmtObj)
                    return new CiscoManagementInfo { LastQueried = DateTimeOffset.UtcNow };

                // SSH
                var ssh = mgmtObj["ssh"];
                string sshVersion = ssh != null ? GetString(ssh, "version") : string.Empty;
                int sshPort = ssh != null ? GetInt(ssh, "port") : 22;
                bool sshEnabled = ssh != null && GetBool(ssh, "enable");

                // HTTP / HTTPS
                var http = mgmtObj["http"];
                bool httpEnabled = http != null && GetBool(http, "secure-server") == false;
                var https = mgmtObj["https"];
                bool httpsEnabled = https != null && GetBool(https, "server");
                int httpsPort = https != null ? GetInt(https, "port") : 443;

                // SNMP
                var snmp = mgmtObj["snmp-server"];
                string snmpVersion = snmp != null ? GetString(snmp, "version") : string.Empty;
                string snmpCommunity = snmp != null ? GetString(snmp, "community") : string.Empty;
                bool snmpEnabled = snmp != null && !string.IsNullOrEmpty(snmpVersion);

                // Telnet
                var telnet = mgmtObj["telnet"];
                string telnetState = telnet != null ? GetString(telnet, "state") : string.Empty;

                // Console timeout
                var line = mgmtObj["line"] ?? mgmtObj["line-vty"];
                string consoleTimeout = line != null ? GetString(line, "exec-timeout") : string.Empty;

                return new CiscoManagementInfo
                {
                    SshVersion = sshVersion,
                    SshPort = sshPort,
                    SshEnabled = sshEnabled,
                    HttpEnabled = httpEnabled,
                    HttpsEnabled = httpsEnabled,
                    HttpsPort = httpsPort,
                    SnmpVersion = snmpVersion,
                    SnmpCommunity = snmpCommunity,
                    SnmpEnabled = snmpEnabled,
                    TelnetState = telnetState,
                    ConsoleTimeout = consoleTimeout,
                    LastQueried = DateTimeOffset.UtcNow
                };
            }
            catch
            {
                return new CiscoManagementInfo { LastQueried = DateTimeOffset.UtcNow };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 9. Statistics
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses radio/WLAN statistics and counters from access-point-oper-data.
        /// </summary>
        public static List<CiscoStatisticsInfo> ParseStatistics(JObject? json)
        {
            var results = new List<CiscoStatisticsInfo>();
            try
            {
                if (json == null) return results;
                var apData = json["Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data"];
                if (apData == null) return results;
                var apArray = apData is JArray ? apData : new JArray(apData);

                foreach (var ap in apArray)
                {
                    if (ap is not JObject apObj) continue;
                    string apName = GetString(apObj, "name");
                    string apMac = GetString(apObj, "mac");

                    var slots = apObj["slot"];
                    if (slots == null) continue;
                    var slotArray = slots is JArray ? slots : new JArray(slots);

                    foreach (var slot in slotArray)
                    {
                        if (slot is not JObject slotObj) continue;
                        string band = DetermineBand(slotObj, GetString(slotObj, "radio-type"));

                        results.Add(new CiscoStatisticsInfo
                        {
                            ApName = apName,
                            ApMacAddress = apMac,
                            SlotId = GetInt(slotObj, "slot-id"),
                            Band = band,
                            TotalClients = GetLong(slotObj, "client-count"),
                            TxBytes = GetLong(slotObj, "tx-bytes"),
                            RxBytes = GetLong(slotObj, "rx-bytes"),
                            TxFrames = GetLong(slotObj, "tx-frames"),
                            RxFrames = GetLong(slotObj, "rx-frames"),
                            TxErrors = GetLong(slotObj, "tx-errors"),
                            RxErrors = GetLong(slotObj, "rx-errors"),
                            TxRetries = GetLong(slotObj, "tx-retries"),
                            RxRetries = GetLong(slotObj, "rx-retries"),
                            NoiseFloor = GetInt(slotObj, "noise-floor"),
                            Utilization = GetDouble(slotObj, "utilization"),
                            ClientCount = GetInt(slotObj, "client-count"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 10. FlexConnect
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses FlexConnect configuration.
        /// </summary>
        public static List<CiscoFlexConnectInfo> ParseFlexConnect(JObject? json)
        {
            var results = new List<CiscoFlexConnectInfo>();
            try
            {
                if (json == null) return results;
                var apFlex = json["Cisco-IOS-XE-wireless-flexconnect-cfg:flexconnect-cfg"]?["ap-flex"];
                if (apFlex == null) return results;
                var arr = apFlex is JArray ? apFlex : new JArray(apFlex);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoFlexConnectInfo
                        {
                            ApName = GetString(obj, "ap-name"),
                            ApMacAddress = GetString(obj, "ap-mac"),
                            Mode = GetString(obj, "mode"),
                            AuthList = GetString(obj, "auth-list"),
                            Vlan = GetString(obj, "vlan"),
                            NativeVlan = GetString(obj, "native-vlan"),
                            JumboFrame = GetString(obj, "jumbo-frame"),
                            IsConnected = GetBool(obj, "connected"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 11. AP Authorization
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses AP authorization list entries.
        /// </summary>
        public static List<CiscoApAuthorizationInfo> ParseApAuthorization(JObject? json)
        {
            var results = new List<CiscoApAuthorizationInfo>();
            try
            {
                if (json == null) return results;
                var authList = json["Cisco-IOS-XE-wireless-ap-cfg:ap-cfg"]?["ap-auth-list"]?["ap-auth"];
                if (authList == null) return results;
                var arr = authList is JArray ? authList : new JArray(authList);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoApAuthorizationInfo
                        {
                            MacAddress = GetString(obj, "ap-mac"),
                            Name = GetString(obj, "ap-name"),
                            AuthState = GetString(obj, "auth-state"),
                            Priority = GetString(obj, "priority"),
                            Group = GetString(obj, "group"),
                            IsAuthorized = GetBool(obj, "authorized"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 12. Clean Air
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses Clean Air air quality metrics.
        /// </summary>
        public static List<CiscoCleanAirInfo> ParseCleanAir(JObject? json)
        {
            var results = new List<CiscoCleanAirInfo>();
            try
            {
                if (json == null) return results;
                var cleanAir = json["Cisco-IOS-XE-wireless-cleanair-oper:cleanair-oper-data"]?["cleanair"];
                if (cleanAir == null) return results;
                var arr = cleanAir is JArray ? cleanAir : new JArray(cleanAir);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoCleanAirInfo
                        {
                            ApName = GetString(obj, "ap-name"),
                            ApMacAddress = GetString(obj, "ap-mac"),
                            SlotId = GetInt(obj, "slot-id"),
                            Band = GetString(obj, "band"),
                            AirQuality = GetInt(obj, "air-quality"),
                            AirQualityStatus = GetInt(obj, "air-quality-status"),
                            InterferenceDeviceCount = GetInt(obj, "interference-device-count"),
                            InterferenceType = GetString(obj, "interference-type"),
                            NonWifiInterference = GetInt(obj, "non-wifi-interference"),
                            WifiInterference = GetInt(obj, "wifi-interference"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 13. Wired Clients
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses wired clients connected to AP Ethernet ports.
        /// </summary>
        public static List<CiscoWiredClientInfo> ParseWiredClients(JObject? json)
        {
            var results = new List<CiscoWiredClientInfo>();
            try
            {
                if (json == null) return results;
                var wiredClients = json["Cisco-IOS-XE-wireless-wired-client-oper:wired-client-oper-data"]?["wired-client"];
                if (wiredClients == null) return results;
                var arr = wiredClients is JArray ? wiredClients : new JArray(wiredClients);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoWiredClientInfo
                        {
                            MacAddress = GetString(obj, "mac-address"),
                            IpAddress = GetString(obj, "ip-address"),
                            ApName = GetString(obj, "ap-name"),
                            ApMacAddress = GetString(obj, "ap-mac"),
                            Interface = GetString(obj, "interface"),
                            Vlan = GetString(obj, "vlan"),
                            Status = GetString(obj, "status"),
                            TxBytes = GetLong(obj, "tx-bytes"),
                            RxBytes = GetLong(obj, "rx-bytes"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 14. Mesh Info
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses mesh networking configuration.
        /// </summary>
        public static List<CiscoMeshInfo> ParseMeshInfo(JObject? json)
        {
            var results = new List<CiscoMeshInfo>();
            try
            {
                if (json == null) return results;
                var mesh = json["Cisco-IOS-XE-wireless-mesh-cfg:mesh-cfg"]?["mesh"];
                if (mesh == null) return results;
                var arr = mesh is JArray ? mesh : new JArray(mesh);
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        results.Add(new CiscoMeshInfo
                        {
                            ApName = GetString(obj, "ap-name"),
                            ApMacAddress = GetString(obj, "ap-mac"),
                            Role = GetString(obj, "role"),
                            BridgeGroupId = GetString(obj, "bridge-group-id"),
                            HopCount = GetString(obj, "hop-count"),
                            Backhaul = GetString(obj, "backhaul"),
                            Parent = GetString(obj, "parent"),
                            IsMeshEnabled = GetBool(obj, "mesh-enabled"),
                            LastQueried = DateTimeOffset.UtcNow
                        });
                    }
                }
            }
            catch { }
            return results;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 15. NTP
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses NTP and time configuration.
        /// </summary>
        public static CiscoNtpInfo ParseNtp(JObject? json)
        {
            try
            {
                if (json == null)
                    return new CiscoNtpInfo { LastQueried = DateTimeOffset.UtcNow };

                var clock = json["Cisco-IOS-XE-native:native"]?["clock"];
                if (clock is not JObject clockObj)
                    return new CiscoNtpInfo { LastQueried = DateTimeOffset.UtcNow };

                // Parse NTP servers (may be under "ntp" child)
                var ntp = clockObj["ntp"];
                string server1 = string.Empty, server2 = string.Empty, server3 = string.Empty;
                bool ntpEnabled = false;

                if (ntp is JObject ntpObj)
                {
                    ntpEnabled = GetBool(ntpObj, "enable");
                    var servers = ntpObj["server"];
                    if (servers is JArray srvArray)
                    {
                        for (int i = 0; i < Math.Min(srvArray.Count, 3); i++)
                        {
                            string addr = GetString(srvArray[i], "address");
                            if (string.IsNullOrEmpty(addr)) addr = srvArray[i]?.ToString() ?? string.Empty;
                            switch (i)
                            {
                                case 0: server1 = addr; break;
                                case 1: server2 = addr; break;
                                case 2: server3 = addr; break;
                            }
                        }
                    }
                    else if (servers is JObject srvObj)
                    {
                        server1 = GetString(srvObj, "address");
                    }
                }

                // Timezone
                var timezone = clockObj["timezone"];
                string tz = timezone != null ? GetString(timezone, "name") : string.Empty;
                string tzOffset = timezone != null ? GetString(timezone, "offset") : string.Empty;

                // Daylight saving
                var dst = clockObj["daylight-saving-time"];
                string dstStr = dst != null ? GetString(dst, "enable") : string.Empty;

                // System time
                string sysTime = GetString(clockObj, "datetime");

                return new CiscoNtpInfo
                {
                    NtpServer1 = server1,
                    NtpServer2 = server2,
                    NtpServer3 = server3,
                    NtpEnabled = ntpEnabled,
                    TimeZone = tz,
                    TimeZoneOffset = tzOffset,
                    DaylightSaving = dstStr,
                    SystemTime = sysTime,
                    LastQueried = DateTimeOffset.UtcNow
                };
            }
            catch
            {
                return new CiscoNtpInfo { LastQueried = DateTimeOffset.UtcNow };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 16. DNS
        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Parses DNS configuration.
        /// </summary>
        public static CiscoDnsInfo ParseDns(JObject? json)
        {
            try
            {
                if (json == null)
                    return new CiscoDnsInfo { LastQueried = DateTimeOffset.UtcNow };

                var ip = json["Cisco-IOS-XE-native:native"]?["ip"];
                if (ip is not JObject ipObj)
                    return new CiscoDnsInfo { LastQueried = DateTimeOffset.UtcNow };

                string domainName = GetString(ipObj, "domain-name");
                var nameServers = ipObj["name-server"];
                string ns1 = string.Empty, ns2 = string.Empty, ns3 = string.Empty;

                if (nameServers is JArray nsArray)
                {
                    for (int i = 0; i < Math.Min(nsArray.Count, 3); i++)
                    {
                        string addr = nsArray[i]?.ToString() ?? string.Empty;
                        switch (i)
                        {
                            case 0: ns1 = addr; break;
                            case 1: ns2 = addr; break;
                            case 2: ns3 = addr; break;
                        }
                    }
                }
                else if (nameServers is JObject nsObj)
                {
                    ns1 = GetString(nsObj, "address");
                }

                return new CiscoDnsInfo
                {
                    DomainName = domainName,
                    NameServer1 = ns1,
                    NameServer2 = ns2,
                    NameServer3 = ns3,
                    DnsEnabled = !string.IsNullOrEmpty(domainName) || !string.IsNullOrEmpty(ns1),
                    LastQueried = DateTimeOffset.UtcNow
                };
            }
            catch
            {
                return new CiscoDnsInfo { LastQueried = DateTimeOffset.UtcNow };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Additional helper: GetLong
        // ─────────────────────────────────────────────────────────────────────
        private static long GetLong(JToken element, string propertyName)
        {
            try
            {
                var token = element[propertyName];
                if (token is JValue jv && jv.Value != null)
                    return Convert.ToInt64(jv.Value);
            }
            catch { }
            return 0L;
        }

        #endregion
    }
}
