using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using BPlusLib.Foundation.VirtualSerial;
using BPlusLib.Foundation.VirtualSerial.Endpoints;
using BPlusLib.Foundation.VirtualSerial.Routing;
using BPlusLib.Foundation.VirtualSerial.Arbitration;
using BPlusLib.Foundation.VirtualSerial.Framing;
using BPlusLib.Foundation.VirtualSerial.Modem;
using BPlusLib.Foundation.VirtualSerial.Configuration;
using Xunit;

namespace BPlusLib.Foundation.Tests.VirtualSerial
{
    [Trait("Category", "VirtualSerial")]
    public class VirtualSerialTests
    {
        #region Core Types

        [Fact]
        public void SerialSettings_Default_HasCorrectValues()
        {
            var settings = SerialSettings.Default;
            settings.BaudRate.Should().Be(9600);
            settings.DataBits.Should().Be(8);
            settings.Parity.Should().Be(ParityMode.None);
            settings.StopBits.Should().Be(StopBitsMode.One);
            settings.Handshake.Should().Be(HandshakeMode.None);
        }

        [Fact]
        public void SerialSettings_HighSpeed_Sets115200()
        {
            var settings = SerialSettings.HighSpeed;
            settings.BaudRate.Should().Be(115200);
        }

        [Fact]
        public void SerialSettings_ModbusRtu_SetsCorrectValues()
        {
            var settings = SerialSettings.ModbusRtu;
            settings.BaudRate.Should().Be(9600);
            settings.Parity.Should().Be(ParityMode.Even);
        }

        [Fact]
        public void SerialFrame_CreatesCorrectly()
        {
            var frame = new SerialFrame
            {
                Timestamp = DateTime.UtcNow,
                Source = "TestPort",
                Direction = FrameDirection.Receive,
                Data = new byte[] { 0x41, 0x42, 0x43 }
            };

            frame.Source.Should().Be("TestPort");
            frame.Direction.Should().Be(FrameDirection.Receive);
            frame.Length.Should().Be(3);
        }

        [Fact]
        public void ModemSignals_ToBits_ConvertsCorrectly()
        {
            var signals = new ModemSignals { Cts = true, Dsr = true, Dcd = true };
            byte bits = signals.ToModemStatusBits();
            bits.Should().Be(0xB0); // CTS + DSR + DCD
        }

        #endregion

        #region Frame Decoders

        [Fact]
        public void RawFramer_FeedAndRead()
        {
            var framer = new RawFramer();
            framer.Feed(new byte[] { 0x41, 0x42, 0x43 });

            framer.TryGetFrame(out var frame).Should().BeTrue();
            frame.Length.Should().Be(3);
        }

        [Fact]
        public void DelimiterFramer_SplitsOnDelimiter()
        {
            var framer = new DelimiterFramer(new byte[] { 0x0D, 0x0A });
            framer.Feed(new byte[] { 0x41, 0x42, 0x0D, 0x0A, 0x43 });

            framer.TryGetFrame(out var frame).Should().BeTrue();
            frame.Length.Should().Be(4); // A + B + CR + LF

            // Remaining: only 'C', not enough for another frame
            framer.TryGetFrame(out _).Should().BeFalse();
        }

        [Fact]
        public void FixedLengthFramer_SplitsOnCount()
        {
            var framer = new FixedLengthFramer(3);
            framer.Feed(new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45 });

            framer.TryGetFrame(out var frame).Should().BeTrue();
            frame.Length.Should().Be(3);

            // Only 2 bytes remain — not enough for frame of 3
            framer.TryGetFrame(out _).Should().BeFalse();
        }

        [Fact]
        public void StxEtxFramer_ExtractsFrame()
        {
            var framer = new StxEtxFramer();
            framer.Feed(new byte[] { 0x00, 0x02, 0x41, 0x42, 0x03, 0x00 });

            framer.TryGetFrame(out var frame).Should().BeTrue();
            frame.Length.Should().Be(4); // STX(0x02) + A + B + ETX(0x03)
        }

        [Fact]
        public void ModbusRtuFramer_BufferedUntilSilence()
        {
            var framer = new ModbusRtuFramer(9600);
            framer.Feed(new byte[] { 0x01, 0x03, 0x00 });

            // No silence yet — should not return frame
            framer.TryGetFrame(out _).Should().BeFalse();
        }

        #endregion

        #region Write Arbitration

        [Fact]
        public async Task SerializedWriteArbiter_AcquireRelease()
        {
            var arbiter = new SerializedWriteArbiter();
            var token = await arbiter.AcquireAsync(Guid.NewGuid());
            token.SessionId.Should().NotBe(Guid.Empty);
            await arbiter.ReleaseAsync(token);
        }

        [Fact]
        public async Task SingleWriterArbiter_AcquireRelease()
        {
            var arbiter = new SingleWriterArbiter { RejectNonOwner = true };
            var session1 = Guid.NewGuid();
            var session2 = Guid.NewGuid();

            var token1 = await arbiter.AcquireAsync(session1);
            token1.SessionId.Should().Be(session1);
            await arbiter.ReleaseAsync(token1);

            var token2 = await arbiter.AcquireAsync(session2);
            token2.SessionId.Should().Be(session2);
            await arbiter.ReleaseAsync(token2);
        }

        [Fact]
        public async Task TransactionArbiter_AcquireRelease()
        {
            var arbiter = new TransactionArbiter();
            var session = Guid.NewGuid();

            var token = await arbiter.AcquireAsync(session);
            token.SessionId.Should().Be(session);

            await arbiter.ReleaseAsync(token);
        }

        #endregion

        #region Modem Signal Mapping

        [Fact]
        public void ModemSignalMapper_MapsRtsToCts()
        {
            var mapper = new ModemSignalMapper();
            var source = new ModemSignals { Rts = true };
            var dest = new ModemSignals();

            var result = mapper.MapToDestination(source, dest);
            result.Cts.Should().BeTrue();
        }

        [Fact]
        public void ModemSignalMapper_MapsDtrToDsrDcd()
        {
            var mapper = new ModemSignalMapper();
            var source = new ModemSignals { Dtr = true };
            var dest = new ModemSignals();

            var result = mapper.MapToDestination(source, dest);
            result.Dsr.Should().BeTrue();
            result.Dcd.Should().BeTrue();
        }

        #endregion

        #region Configuration

        [Fact]
        public void ConfigurationLoader_CreateDefault()
        {
            var config = ConfigurationLoader.CreateDefault();
            config.Version.Should().Be(1);
            config.Ports.Should().HaveCount(2);
            config.Routes.Should().HaveCount(1);
        }

        [Fact]
        public void ConfigurationLoader_RoundTrip()
        {
            var config = ConfigurationLoader.CreateDefault();
            string json = ConfigurationLoader.ToJson(config);
            var loaded = ConfigurationLoader.LoadFromJson(json);

            loaded.Version.Should().Be(1);
            loaded.Ports.Should().HaveCount(2);
        }

        [Fact]
        public void ConfigurationLoader_SaveAndLoadFile()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var config = ConfigurationLoader.CreateDefault();
                ConfigurationLoader.SaveToFile(config, tempFile);

                var loaded = ConfigurationLoader.LoadFromFile(tempFile);
                loaded.Ports.Should().HaveCount(2);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        #endregion

        #region VirtualSerialHelper

        [Fact]
        public void Helper_CreateTcpClient()
        {
            var endpoint = VirtualSerialHelper.CreateTcpClient("Test", "127.0.0.1", 5000);
            endpoint.Type.Should().Be(EndpointType.TcpClient);
            endpoint.Name.Should().Be("Test");
        }

        [Fact]
        public void Helper_CreateTcpServer()
        {
            var endpoint = VirtualSerialHelper.CreateTcpServer("Test", 5000);
            endpoint.Type.Should().Be(EndpointType.TcpServer);
        }

        [Fact]
        public void Helper_CreateUdp()
        {
            var endpoint = VirtualSerialHelper.CreateUdp("Test", "127.0.0.1", 5000);
            endpoint.Type.Should().Be(EndpointType.Udp);
        }

        [Fact]
        public void Helper_CreateRouteEngine()
        {
            var engine = VirtualSerialHelper.CreateRouteEngine();
            engine.Should().NotBeNull();
        }

        [Fact]
        public void Helper_CreateFramers()
        {
            var crlf = VirtualSerialHelper.CreateCrLfFramer();
            var modbus = VirtualSerialHelper.CreateModbusRtuFramer();
            var delimiter = VirtualSerialHelper.CreateDelimiterFramer(new byte[] { 0xFF });

            crlf.Should().NotBeNull();
            modbus.Should().NotBeNull();
            delimiter.Should().NotBeNull();
        }

        #endregion
    }
}
