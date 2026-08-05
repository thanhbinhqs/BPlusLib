using System;
using BPlusLib.Foundation.VirtualSerial.Endpoints;

namespace BPlusLib.Foundation.VirtualSerial.Modem
{
    /// <summary>
    /// Maps modem control signals between two endpoints (e.g., for virtual pair).
    /// Bidirectional: maps A→B and B→A.
    /// </summary>
    public sealed class ModemSignalMapper
    {
        private readonly ModemSignalConfig _config;

        public ModemSignalMapper(ModemSignalConfig? config = null)
        {
            _config = config ?? ModemSignalConfig.Default;
        }

        /// <summary>Current configuration.</summary>
        public ModemSignalConfig Config => _config;

        /// <summary>
        /// Maps source signals to destination signals based on configuration.
        /// Returns the new modem signals that should be applied to the destination.
        /// </summary>
        public ModemSignals MapToDestination(ModemSignals source, ModemSignals currentDestination)
        {
            return currentDestination with
            {
                Cts = _config.RtsToPeerCts ? source.Rts : currentDestination.Cts,
                Dsr = _config.DtrToPeerDsr ? source.Dtr : currentDestination.Dsr,
                Dcd = _config.DtrToPeerDcd ? source.Dtr : currentDestination.Dcd,
                Ri = _config.RingIndicatorMode switch
                {
                    RingIndicatorMode.FollowPeerDtr => source.Dtr,
                    _ => currentDestination.Ri
                }
            };
        }

        /// <summary>
        /// Maps bidirectional signals between two endpoints.
        /// Returns a tuple of (signalsForA, signalsForB).
        /// </summary>
        public (ModemSignals forA, ModemSignals forB) MapBidirectional(
            ModemSignals signalsA, ModemSignals signalsB)
        {
            var forA = MapToDestination(signalsB, signalsA);
            var forB = MapToDestination(signalsA, signalsB);
            return (forA, forB);
        }

        /// <summary>
        /// Creates default pair mapping (A.RTS→B.CTS, A.DTR→B.DSR, etc.).
        /// </summary>
        public static ModemSignalMapper CreatePairMapper()
        {
            return new ModemSignalMapper(ModemSignalConfig.Default);
        }
    }
}
