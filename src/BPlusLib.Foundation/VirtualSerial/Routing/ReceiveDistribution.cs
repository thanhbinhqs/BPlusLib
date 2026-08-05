namespace BPlusLib.Foundation.VirtualSerial.Routing
{
    /// <summary>
    /// Determines how received data is distributed to multiple sessions.
    /// </summary>
    public enum ReceiveDistribution
    {
        /// <summary>Each session receives a copy of all data (default).</summary>
        Broadcast,

        /// <summary>Each byte/frame goes to exactly one session (round-robin).</summary>
        CompetingConsumer
    }
}
