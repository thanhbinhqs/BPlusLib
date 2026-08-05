using System;
using BPlusLib.Foundation.VirtualSerial.Endpoints;
using BPlusLib.Foundation.VirtualSerial.Routing;
using BPlusLib.Foundation.VirtualSerial.Configuration;
using BPlusLib.Foundation.VirtualSerial.Arbitration;
using BPlusLib.Foundation.VirtualSerial.Framing;
using BPlusLib.Foundation.VirtualSerial.Modem;

namespace BPlusLib.Foundation.VirtualSerial
{
    /// <summary>
    /// Static facade for creating VirtualSerial components.
    /// Provides convenient factory methods for common operations.
    /// </summary>
    public static class VirtualSerialHelper
    {
        /// <summary>Create a TCP client endpoint.</summary>
        public static TcpClientEndpoint CreateTcpClient(string name, string host, int port)
        {
            return new TcpClientEndpoint(name, host, port);
        }

        /// <summary>Create a TCP server endpoint.</summary>
        public static TcpServerEndpoint CreateTcpServer(string name, int port)
        {
            return new TcpServerEndpoint(name, port);
        }

        /// <summary>Create a UDP endpoint.</summary>
        public static UdpSerialEndpoint CreateUdp(string name, string remoteHost, int remotePort, int localPort = 0)
        {
            return new UdpSerialEndpoint(name, remoteHost, remotePort, localPort);
        }

        /// <summary>Create a physical serial endpoint.</summary>
        public static PhysicalSerialEndpoint CreatePhysical(string name, SerialSettings? settings = null)
        {
            return new PhysicalSerialEndpoint(name, settings);
        }

        /// <summary>Create a TLS client endpoint.</summary>
        public static TlsClientEndpoint CreateTlsClient(string name, string host, int port)
        {
            return new TlsClientEndpoint(name, host, port);
        }

        /// <summary>Create a virtual COM placeholder (requires driver).</summary>
        public static VirtualComEndpoint CreateVirtual(string portName)
        {
            return new VirtualComEndpoint(portName);
        }

        /// <summary>Create a route engine.</summary>
        public static IRouteEngine CreateRouteEngine()
        {
            return new RouteEngine();
        }

        /// <summary>Create a serialized write arbiter.</summary>
        public static IWriteArbiter CreateSerializedArbiter()
        {
            return new SerializedWriteArbiter();
        }

        /// <summary>Create a single writer arbiter.</summary>
        public static SingleWriterArbiter CreateSingleWriterArbiter(bool rejectNonOwner = false)
        {
            return new SingleWriterArbiter { RejectNonOwner = rejectNonOwner };
        }

        /// <summary>Create a transaction arbiter.</summary>
        public static TransactionArbiter CreateTransactionArbiter(TimeSpan? timeout = null)
        {
            var arbiter = new TransactionArbiter();
            if (timeout.HasValue) arbiter.TransactionTimeout = timeout.Value;
            return arbiter;
        }

        /// <summary>Create a delimiter framer.</summary>
        public static DelimiterFramer CreateDelimiterFramer(byte[] delimiter, int maxFrameLength = 65536)
        {
            return new DelimiterFramer(delimiter, maxFrameLength);
        }

        /// <summary>Create a CR/LF delimiter framer.</summary>
        public static DelimiterFramer CreateCrLfFramer(int maxFrameLength = 65536)
        {
            return new DelimiterFramer(new byte[] { 0x0D, 0x0A }, maxFrameLength);
        }

        /// <summary>Create a Modbus RTU framer.</summary>
        public static ModbusRtuFramer CreateModbusRtuFramer(int baudRate = 9600, int maxFrameLength = 256)
        {
            return new ModbusRtuFramer(baudRate, maxFrameLength);
        }

        /// <summary>Create a modem signal mapper for pairs.</summary>
        public static ModemSignalMapper CreatePairModemMapper(ModemSignalConfig? config = null)
        {
            return new ModemSignalMapper(config ?? ModemSignalConfig.Default);
        }

        /// <summary>Load configuration from file.</summary>
        public static VirtualSerialConfig LoadConfig(string filePath)
        {
            return ConfigurationLoader.LoadFromFile(filePath);
        }

        /// <summary>Save configuration to file.</summary>
        public static void SaveConfig(VirtualSerialConfig config, string filePath)
        {
            ConfigurationLoader.SaveToFile(config, filePath);
        }

        /// <summary>Create a default configuration.</summary>
        public static VirtualSerialConfig CreateDefaultConfig()
        {
            return ConfigurationLoader.CreateDefault();
        }
    }
}
