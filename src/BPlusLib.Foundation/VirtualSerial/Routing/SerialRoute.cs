using System;
using System.Collections.Generic;
using BPlusLib.Foundation.VirtualSerial.Endpoints;

namespace BPlusLib.Foundation.VirtualSerial.Routing
{
    /// <summary>
    /// Defines a route between serial endpoints.
    /// </summary>
    public sealed record SerialRoute
    {
        /// <summary>Unique route identifier.</summary>
        public required Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>Human-readable name.</summary>
        public string? Name { get; init; }

        /// <summary>Route type.</summary>
        public required RouteType Type { get; init; }

        /// <summary>Source endpoint IDs.</summary>
        public required IReadOnlyList<Guid> Sources { get; init; }

        /// <summary>Destination endpoint IDs.</summary>
        public required IReadOnlyList<Guid> Destinations { get; init; }

        /// <summary>How received data is distributed to sessions.</summary>
        public ReceiveDistribution ReceiveDistribution { get; init; } = ReceiveDistribution.Broadcast;

        /// <summary>How concurrent writes are handled.</summary>
        public WriteArbitrationPolicy WriteArbitration { get; init; } = WriteArbitrationPolicy.Serialized;

        /// <summary>Frame boundary detection.</summary>
        public FrameBoundary FrameBoundary { get; init; } = FrameBoundary.Raw;

        /// <summary>Overflow policy for full buffers.</summary>
        public OverflowPolicy Overflow { get; init; } = OverflowPolicy.DropOldest;

        /// <summary>Whether this route is enabled.</summary>
        public bool Enabled { get; init; } = true;
    }

    /// <summary>
    /// Types of routes.
    /// </summary>
    public enum RouteType
    {
        /// <summary>Bidirectional pair between two endpoints.</summary>
        Pair,

        /// <summary>One-to-many broadcast.</summary>
        Broadcast,

        /// <summary>Physical serial splitter.</summary>
        PhysicalSplitter,

        /// <summary>Serial-to-socket bridge.</summary>
        SocketBridge
    }
}
