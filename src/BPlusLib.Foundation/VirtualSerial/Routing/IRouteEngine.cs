using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BPlusLib.Foundation.VirtualSerial.Endpoints;

namespace BPlusLib.Foundation.VirtualSerial.Routing
{
    /// <summary>
    /// Manages routes between serial endpoints. Handles lifecycle, data pumping,
    /// and arbitration.
    /// </summary>
    public interface IRouteEngine : IAsyncDisposable
    {
        /// <summary>Event raised when a route starts.</summary>
        event EventHandler<Guid>? RouteStarted;

        /// <summary>Event raised when a route stops.</summary>
        event EventHandler<Guid>? RouteStopped;

        /// <summary>Event raised when a frame is routed.</summary>
        event EventHandler<SerialFrame>? FrameRouted;

        /// <summary>Event raised on routing errors.</summary>
        event EventHandler<RouteErrorEventArgs>? Error;

        /// <summary>Add an endpoint to the engine.</summary>
        void AddEndpoint(ISerialEndpoint endpoint);

        /// <summary>Remove an endpoint from the engine.</summary>
        void RemoveEndpoint(Guid endpointId);

        /// <summary>Get an endpoint by ID.</summary>
        ISerialEndpoint? GetEndpoint(Guid endpointId);

        /// <summary>Get all registered endpoints.</summary>
        IReadOnlyList<ISerialEndpoint> GetEndpoints();

        /// <summary>Add a route.</summary>
        void AddRoute(SerialRoute route);

        /// <summary>Remove a route.</summary>
        void RemoveRoute(Guid routeId);

        /// <summary>Get a route by ID.</summary>
        SerialRoute? GetRoute(Guid routeId);

        /// <summary>Get all routes.</summary>
        IReadOnlyList<SerialRoute> GetRoutes();

        /// <summary>Start a specific route.</summary>
        ValueTask StartRouteAsync(Guid routeId, CancellationToken cancellationToken = default);

        /// <summary>Stop a specific route.</summary>
        ValueTask StopRouteAsync(Guid routeId, CancellationToken cancellationToken = default);

        /// <summary>Start all routes.</summary>
        ValueTask StartAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Stop all routes.</summary>
        ValueTask StopAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Get route statistics.</summary>
        RouteStatistics GetStatistics(Guid routeId);
    }

    /// <summary>
    /// Statistics for a route.
    /// </summary>
    public sealed class RouteStatistics
    {
        public long FramesRouted { get; set; }
        public long BytesRouted { get; set; }
        public long FramesDropped { get; set; }
        public long BytesDropped { get; set; }
        public long Errors { get; set; }
        public DateTime? StartedAt { get; set; }
        public TimeSpan Uptime => StartedAt.HasValue ? DateTime.UtcNow - StartedAt.Value : TimeSpan.Zero;
    }

    /// <summary>
    /// Event args for routing errors.
    /// </summary>
    public sealed class RouteErrorEventArgs : EventArgs
    {
        public Guid RouteId { get; init; }
        public Exception Exception { get; init; } = null!;
        public string? Message { get; init; }
    }
}
