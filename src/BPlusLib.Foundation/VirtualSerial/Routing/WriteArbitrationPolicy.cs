namespace BPlusLib.Foundation.VirtualSerial.Routing
{
    /// <summary>
    /// Determines how concurrent writes from multiple sessions are handled.
    /// </summary>
    public enum WriteArbitrationPolicy
    {
        /// <summary>Each WriteFile call is an atomic frame. Frames are serialized but not interleaved.</summary>
        Serialized,

        /// <summary>Only one session can write. Others receive access denied or wait.</summary>
        SingleWriter,

        /// <summary>Session acquires exclusive write lock, sends, then releases.</summary>
        TransactionLock
    }
}
