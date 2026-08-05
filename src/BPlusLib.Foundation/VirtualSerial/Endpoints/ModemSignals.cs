using System;

namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// Modem control and status signals for serial communication.
    /// </summary>
    public sealed record ModemSignals
    {
        /// <summary>Data Terminal Ready (output from DTE).</summary>
        public bool Dtr { get; init; }

        /// <summary>Request To Send (output from DTE).</summary>
        public bool Rts { get; init; }

        /// <summary>Clear To Send (input to DTE).</summary>
        public bool Cts { get; init; }

        /// <summary>Data Set Ready (input to DTE).</summary>
        public bool Dsr { get; init; }

        /// <summary>Data Carrier Detect (input to DTE).</summary>
        public bool Dcd { get; init; }

        /// <summary>Ring Indicator (input to DTE).</summary>
        public bool Ri { get; init; }

        /// <summary>Break signal active.</summary>
        public bool Break { get; init; }

        /// <summary>Converts to Win32 SERIAL_MODEM_CONTROL bits.</summary>
        public byte ToModemControlBits()
        {
            byte bits = 0;
            if (Dtr) bits |= 0x01; // SERIAL_DTR_STATE
            if (Rts) bits |= 0x02; // SERIAL_RTS_STATE
            return bits;
        }

        /// <summary>Converts to Win32 SERIAL_MODEM_STATUS bits.</summary>
        public byte ToModemStatusBits()
        {
            byte bits = 0;
            if (Cts) bits |= 0x10; // SERIAL_CTS_STATE
            if (Dsr) bits |= 0x20; // SERIAL_DSR_STATE
            if (Ri) bits |= 0x40;  // SERIAL_RING_STATE
            if (Dcd) bits |= 0x80; // SERIAL_DCD_STATE
            return bits;
        }

        /// <summary>Creates from Win32 modem control bits.</summary>
        public static ModemSignals FromModemControlBits(byte bits) => new()
        {
            Dtr = (bits & 0x01) != 0,
            Rts = (bits & 0x02) != 0
        };

        /// <summary>Creates from Win32 modem status bits.</summary>
        public static ModemSignals FromModemStatusBits(byte bits) => new()
        {
            Cts = (bits & 0x10) != 0,
            Dsr = (bits & 0x20) != 0,
            Ri = (bits & 0x40) != 0,
            Dcd = (bits & 0x80) != 0
        };

        /// <summary>Raises a modem signal change event.</summary>
        public event EventHandler<ModemSignalChangedEventArgs>? SignalChanged;

        /// <summary>Notifies listeners of signal changes.</summary>
        public void NotifyChanged(ModemSignals previous)
        {
            if (Dtr != previous.Dtr || Rts != previous.Rts ||
                Cts != previous.Cts || Dsr != previous.Dsr ||
                Dcd != previous.Dcd || Ri != previous.Ri)
            {
                SignalChanged?.Invoke(this, new ModemSignalChangedEventArgs(previous, this));
            }
        }
    }

    /// <summary>
    /// Event args for modem signal changes.
    /// </summary>
    public sealed class ModemSignalChangedEventArgs : EventArgs
    {
        public ModemSignals Previous { get; }
        public ModemSignals Current { get; }

        public ModemSignalChangedEventArgs(ModemSignals previous, ModemSignals current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
