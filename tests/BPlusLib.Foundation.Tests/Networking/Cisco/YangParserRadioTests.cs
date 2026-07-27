using FluentAssertions;
using Newtonsoft.Json.Linq;
using BPlusLib.Foundation.Networking.Cisco;
using BPlusLib.Foundation.Networking.Cisco.Models;
using Xunit;

namespace BPlusLib.Foundation.Tests.Networking.Cisco
{
    public class YangParserRadioTests
    {
        [Fact]
        public void ParseRadioInfo_WithValidData_ReturnsRadioList()
        {
            // Arrange — simulate Cisco-IOS-XE-wireless-access-point-oper response
            var json = JObject.Parse(@"{
                ""Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data"": [
                    {
                        ""mac"": ""aa:bb:cc:dd:ee:ff"",
                        ""name"": ""AP-Floor1"",
                        ""slot"": [
                            {
                                ""slot-id"": 0,
                                ""radio-type"": ""802.11b/g/n"",
                                ""channel"": 6,
                                ""chan-width"": 20,
                                ""power-level"": 15.0,
                                ""admin-state"": ""enabled"",
                                ""oper-state"": ""up"",
                                ""client-count"": 12,
                                ""ssid-name"": ""CorporateWiFi"",
                                ""bssid"": ""aa:bb:cc:dd:ee:f0""
                            },
                            {
                                ""slot-id"": 1,
                                ""radio-type"": ""802.11ac/ax"",
                                ""channel"": 36,
                                ""chan-width"": 80,
                                ""power-level"": 17.0,
                                ""admin-state"": ""enabled"",
                                ""oper-state"": ""up"",
                                ""client-count"": 25,
                                ""ssid-name"": ""CorporateWiFi-5G"",
                                ""bssid"": ""aa:bb:cc:dd:ee:f1""
                            }
                        ]
                    }
                ]
            }");

            // Act
            var radios = YangParser.ParseRadioInfo(json);

            // Assert
            radios.Should().HaveCount(2);

            // 2.4 GHz radio
            radios[0].ApMacAddress.Should().Be("aa:bb:cc:dd:ee:ff");
            radios[0].ApName.Should().Be("AP-Floor1");
            radios[0].SlotId.Should().Be(0);
            radios[0].Band.Should().Be("2.4GHz");
            radios[0].RadioType.Should().Be("802.11b/g/n");
            radios[0].Channel.Should().Be(6);
            radios[0].ChannelWidth.Should().Be(20);
            radios[0].TxPower.Should().Be(15.0);
            radios[0].AdminState.Should().Be("enabled");
            radios[0].OperState.Should().Be("up");
            radios[0].ClientCount.Should().Be(12);
            radios[0].SsidName.Should().Be("CorporateWiFi");
            radios[0].Bssid.Should().Be("aa:bb:cc:dd:ee:f0");

            // 5 GHz radio
            radios[1].SlotId.Should().Be(1);
            radios[1].Band.Should().Be("5GHz");
            radios[1].RadioType.Should().Be("802.11ac/ax");
            radios[1].Channel.Should().Be(36);
            radios[1].ChannelWidth.Should().Be(80);
            radios[1].TxPower.Should().Be(17.0);
            radios[1].ClientCount.Should().Be(25);
        }

        [Fact]
        public void ParseRadioInfo_WithNullJson_ReturnsEmptyList()
        {
            var radios = YangParser.ParseRadioInfo(null);
            radios.Should().BeEmpty();
        }

        [Fact]
        public void ParseRadioInfo_WithMissingFields_ReturnsWithDefaults()
        {
            var json = JObject.Parse(@"{
                ""Cisco-IOS-XE-wireless-access-point-oper:access-point-oper-data"": [
                    {
                        ""mac"": ""11:22:33:44:55:66"",
                        ""slot"": []
                    }
                ]
            }");

            var radios = YangParser.ParseRadioInfo(json);
            radios.Should().BeEmpty();
        }

        [Fact]
        public void ParseRfProfiles_WithValidData_ReturnsProfileList()
        {
            var json = JObject.Parse(@"{
                ""Cisco-IOS-XE-wireless-rf-cfg:rf-profiles"": {
                    ""rf-profile"": [
                        {
                            ""profile-name"": ""RF-5GHz-High"",
                            ""radio-band"": ""5GHz"",
                            ""channel-mode"": ""auto"",
                            ""channel-def"": [""36"", ""40"", ""44"", ""48"", ""149"", ""153"", ""157"", ""161""],
                            ""chan-width"": 80,
                            ""power-level"": 17.0,
                            ""max-tx-power-level"": 23.0,
                            ""min-tx-power-level"": 8.0,
                            ""mandatory-data-rate"": 6.0,
                            ""max-data-rate"": 1300.0,
                            ""client-min-rssi"": -80,
                            ""rrm-auto"": true,
                            ""coverage-hole-detection"": true,
                            ""coverage-hole-rssi"": -85,
                            ""neighbor-list"": true,
                            ""dot11a"": true,
                            ""description"": ""High performance 5GHz profile""
                        },
                        {
                            ""profile-name"": ""RF-2.4GHz-Standard"",
                            ""radio-band"": ""2.4GHz"",
                            ""channel-mode"": ""custom"",
                            ""channel-def"": ""1,6,11"",
                            ""chan-width"": 20,
                            ""power-level"": 15.0,
                            ""max-tx-power-level"": 20.0,
                            ""min-tx-power-level"": 5.0,
                            ""mandatory-data-rate"": 12.0,
                            ""max-data-rate"": 300.0,
                            ""client-min-rssi"": -85,
                            ""rrm-auto"": false,
                            ""coverage-hole-detection"": false,
                            ""dot11b"": true,
                            ""description"": ""Standard 2.4GHz profile""
                        }
                    ]
                }
            }");

            var profiles = YangParser.ParseRfProfiles(json);

            profiles.Should().HaveCount(2);

            // 5GHz profile
            profiles[0].Name.Should().Be("RF-5GHz-High");
            profiles[0].RadioBand.Should().Be("5GHz");
            profiles[0].ChannelMode.Should().Be("auto");
            profiles[0].ChannelList.Should().Be("36,40,44,48,149,153,157,161");
            profiles[0].DefaultChannelWidth.Should().Be(80);
            profiles[0].TxPowerLevel.Should().Be(17.0);
            profiles[0].MaxTxPower.Should().Be(23.0);
            profiles[0].MinTxPower.Should().Be(8.0);
            profiles[0].MandatoryDataRate.Should().Be(6.0);
            profiles[0].MaxDataRate.Should().Be(1300.0);
            profiles[0].ClientMinRssi.Should().Be(-80);
            profiles[0].RrmEnabled.Should().BeTrue();
            profiles[0].CoverageHoleDetection.Should().BeTrue();
            profiles[0].CoverageHoleRssi.Should().Be(-85);
            profiles[0].NeighborReportEnabled.Should().BeTrue();
            profiles[0].Dot11aEnabled.Should().BeTrue();
            profiles[0].Description.Should().Be("High performance 5GHz profile");

            // 2.4GHz profile
            profiles[1].Name.Should().Be("RF-2.4GHz-Standard");
            profiles[1].RadioBand.Should().Be("2.4GHz");
            profiles[1].ChannelMode.Should().Be("custom");
            profiles[1].ChannelList.Should().Be("1,6,11");
            profiles[1].DefaultChannelWidth.Should().Be(20);
            profiles[1].Dot11bEnabled.Should().BeTrue();
        }

        [Fact]
        public void ParseRfProfiles_WithNullJson_ReturnsEmptyList()
        {
            var profiles = YangParser.ParseRfProfiles(null);
            profiles.Should().BeEmpty();
        }

        [Fact]
        public void ParseApProfiles_WithValidData_ReturnsProfileList()
        {
            var json = JObject.Parse(@"{
                ""Cisco-IOS-XE-wireless-ap-cfg:ap-cfg"": {
                    ""ap-profile-groups"": {
                        ""ap-profile-group"": [
                            {
                                ""profile-name"": ""APGroup-Floor1"",
                                ""description"": ""First floor AP group"",
                                ""rf-profile-name-24ghz"": ""RF-2.4GHz-Standard"",
                                ""rf-profile-name-5ghz"": ""RF-5GHz-High"",
                                ""dot11b-channel-mode"": ""auto"",
                                ""dot11a-channel-mode"": ""auto"",
                                ""dot11b-channel-width"": ""CHAN_WIDTH_20MHZ"",
                                ""dot11a-channel-width"": ""CHAN_WIDTH_80MHZ"",
                                ""dot11b-power-level"": 15.0,
                                ""dot11a-power-level"": 17.0,
                                ""wlan"": [""CorporateWiFi"", ""GuestWiFi""]
                            }
                        ]
                    }
                }
            }");

            var profiles = YangParser.ParseApProfiles(json);

            profiles.Should().HaveCount(1);
            profiles[0].Name.Should().Be("APGroup-Floor1");
            profiles[0].Description.Should().Be("First floor AP group");
            profiles[0].RfProfile24Ghz.Should().Be("RF-2.4GHz-Standard");
            profiles[0].RfProfile5Ghz.Should().Be("RF-5GHz-High");
            profiles[0].Dot11bChannelMode.Should().Be("auto");
            profiles[0].Dot11aChannelMode.Should().Be("auto");
            profiles[0].Dot11bChannelWidth.Should().Be("CHAN_WIDTH_20MHZ");
            profiles[0].Dot11aChannelWidth.Should().Be("CHAN_WIDTH_80MHZ");
            profiles[0].Dot11bTxPower.Should().Be(15.0);
            profiles[0].Dot11aTxPower.Should().Be(17.0);
            profiles[0].AssociatedWlans.Should().Be("CorporateWiFi,GuestWiFi");
        }

        [Fact]
        public void ParseApProfiles_WithNullJson_ReturnsEmptyList()
        {
            var profiles = YangParser.ParseApProfiles(null);
            profiles.Should().BeEmpty();
        }
    }
}
