namespace BPlusLib.Foundation.VirtualSerial.Routing
{
    /// <summary>
    /// Defines how byte streams are split into frames.
    /// </summary>
    public enum FrameBoundary
    {
        /// <summary>No framing — raw byte stream.</summary>
        Raw,

        /// <summary>Each WriteFile call = one frame.</summary>
        PerWrite,

        /// <summary>Split on delimiter bytes (e.g., 0x0D 0x0A).</summary>
        Delimiter,

        /// <summary>Fixed number of bytes per frame.</summary>
        FixedLength,

        /// <summary>Split on idle timeout gap.</summary>
        IdleTimeout,

        /// <summary>Modbus RTU: 3.5 character silence interval.</summary>
        ModbusRtu,

        /// <summary>STX (0x02) to ETX (0x03) framing.</summary>
        StxEtx
    }
}
