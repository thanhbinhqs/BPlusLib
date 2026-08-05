namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// Serial port configuration settings.
    /// </summary>
    public sealed record SerialSettings
    {
        /// <summary>Baud rate (e.g., 9600, 115200).</summary>
        public int BaudRate { get; init; } = 9600;

        /// <summary>Data bits (5, 6, 7, or 8).</summary>
        public int DataBits { get; init; } = 8;

        /// <summary>Parity mode.</summary>
        public ParityMode Parity { get; init; } = ParityMode.None;

        /// <summary>Stop bits.</summary>
        public StopBitsMode StopBits { get; init; } = StopBitsMode.One;

        /// <summary>Flow control mode.</summary>
        public HandshakeMode Handshake { get; init; } = HandshakeMode.None;

        /// <summary>Read timeout in milliseconds. -1 = infinite, 0 = immediate.</summary>
        public int ReadTimeoutMs { get; init; } = -1;

        /// <summary>Write timeout in milliseconds. -1 = infinite, 0 = immediate.</summary>
        public int WriteTimeoutMs { get; init; } = -1;

        /// <summary>Receive buffer size in bytes.</summary>
        public int RxBufferSize { get; init; } = 4096;

        /// <summary>Transmit buffer size in bytes.</summary>
        public int TxBufferSize { get; init; } = 4096;

        /// <summary>DTR (Data Terminal Ready) signal state.</summary>
        public bool DtrEnable { get; init; }

        /// <summary>RTS (Request To Send) signal state.</summary>
        public bool RtsEnable { get; init; }

        /// <summary>Returns a default configuration for 9600 8N1.</summary>
        public static SerialSettings Default => new();

        /// <summary>Returns a common 115200 8N1 configuration.</summary>
        public static SerialSettings HighSpeed => new() { BaudRate = 115200 };

        /// <summary>Returns a Modbus RTU configuration (9600 8E1).</summary>
        public static SerialSettings ModbusRtu => new()
        {
            BaudRate = 9600,
            DataBits = 8,
            Parity = ParityMode.Even,
            StopBits = StopBitsMode.One
        };

        public override string ToString() =>
            $"{BaudRate} {DataBits}{Parity switch { ParityMode.None => "N", ParityMode.Odd => "O", ParityMode.Even => "E", ParityMode.Mark => "M", ParityMode.Space => "S", _ => "?" }}{DataBits switch { 5 => "5", 6 => "6", 7 => "7", _ => "8" }}{StopBits switch { StopBitsMode.One => "1", StopBitsMode.OnePointFive => "1.5", _ => "2" }}";
    }

    /// <summary>Parity modes.</summary>
    public enum ParityMode
    {
        None = 0,
        Odd = 1,
        Even = 2,
        Mark = 3,
        Space = 4
    }

    /// <summary>Stop bit modes.</summary>
    public enum StopBitsMode
    {
        One = 0,
        OnePointFive = 1,
        Two = 2
    }

    /// <summary>Handshake/flow control modes.</summary>
    public enum HandshakeMode
    {
        None = 0,
        XOnXOff = 1,
        RequestToSend = 2,
        RequestToSendXOnXOff = 3
    }
}
