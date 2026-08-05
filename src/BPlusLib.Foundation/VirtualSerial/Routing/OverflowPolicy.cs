namespace BPlusLib.Foundation.VirtualSerial.Routing
{
    /// <summary>
    /// Determines behavior when a session's receive buffer is full.
    /// </summary>
    public enum OverflowPolicy
    {
        /// <summary>Discard oldest data in buffer (default).</summary>
        DropOldest,

        /// <summary>Discard new incoming data.</summary>
        DropNewest,

        /// <summary>Disconnect the slow consumer session.</summary>
        DisconnectSlow,

        /// <summary>Block the producer until space is available.</summary>
        BlockProducer,

        /// <summary>Expand buffer up to maximum limit.</summary>
        Expand
    }
}
