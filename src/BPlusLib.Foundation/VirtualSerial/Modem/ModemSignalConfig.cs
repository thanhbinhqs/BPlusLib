namespace BPlusLib.Foundation.VirtualSerial.Modem
{
    /// <summary>
    /// Configuration for modem signal mapping between endpoints.
    /// </summary>
    public sealed record ModemSignalConfig
    {
        /// <summary>Map local RTS to peer CTS. Default: true.</summary>
        public bool RtsToPeerCts { get; init; } = true;

        /// <summary>Map local DTR to peer DSR. Default: true.</summary>
        public bool DtrToPeerDsr { get; init; } = true;

        /// <summary>Map local DTR to peer DCD. Default: true.</summary>
        public bool DtrToPeerDcd { get; init; } = true;

        /// <summary>Map local RTS to peer RTS (loopback). Default: false.</summary>
        public bool RtsToPeerRts { get; init; }

        /// <summary>Map local DTR to peer DTR (loopback). Default: false.</summary>
        public bool DtrToPeerDtr { get; init; }

        /// <summary>Ring indicator mode.</summary>
        public RingIndicatorMode RingIndicatorMode { get; init; } = RingIndicatorMode.Manual;

        /// <summary>Default pair mapping configuration.</summary>
        public static ModemSignalConfig Default => new();

        /// <summary>Configuration with all signals mapped.</summary>
        public static ModemSignalConfig AllMapped => new()
        {
            RtsToPeerCts = true,
            DtrToPeerDsr = true,
            DtrToPeerDcd = true,
            RtsToPeerRts = true,
            DtrToPeerDtr = true
        };
    }

    /// <summary>
    /// How the Ring Indicator signal is controlled.
    /// </summary>
    public enum RingIndicatorMode
    {
        /// <summary>RI is controlled manually via API.</summary>
        Manual,

        /// <summary>RI is toggled periodically (for modem simulation).</summary>
        Periodic,

        /// <summary>RI follows DTR of the peer (like physical modem).</summary>
        FollowPeerDtr
    }
}
