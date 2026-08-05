using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BPlusLib.Foundation.VirtualSerial.Endpoints;

namespace BPlusLib.Foundation.VirtualSerial.Routing
{
    /// <summary>
    /// Manages routes between serial endpoints. Creates and manages RouteWorker
    /// instances for each active route.
    /// </summary>
    public sealed class RouteEngine : IRouteEngine
    {
        private readonly ConcurrentDictionary<Guid, ISerialEndpoint> _endpoints = new();
        private readonly ConcurrentDictionary<Guid, SerialRoute> _routes = new();
        private readonly ConcurrentDictionary<Guid, RouteWorker> _workers = new();
        private readonly ConcurrentDictionary<Guid, RouteStatistics> _statistics = new();
        private bool _disposed;

        public event EventHandler<Guid>? RouteStarted;
        public event EventHandler<Guid>? RouteStopped;
        public event EventHandler<SerialFrame>? FrameRouted;
        public event EventHandler<RouteErrorEventArgs>? Error;

        public void AddEndpoint(ISerialEndpoint endpoint)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            _endpoints[endpoint.Id] = endpoint;
        }

        public void RemoveEndpoint(Guid endpointId)
        {
            _endpoints.TryRemove(endpointId, out _);
        }

        public ISerialEndpoint? GetEndpoint(Guid endpointId)
        {
            _endpoints.TryGetValue(endpointId, out var endpoint);
            return endpoint;
        }

        public IReadOnlyList<ISerialEndpoint> GetEndpoints()
        {
            return _endpoints.Values.ToList();
        }

        public void AddRoute(SerialRoute route)
        {
            ArgumentNullException.ThrowIfNull(route);
            _routes[route.Id] = route;
            _statistics[route.Id] = new RouteStatistics();
        }

        public void RemoveRoute(Guid routeId)
        {
            _routes.TryRemove(routeId, out _);
            _statistics.TryRemove(routeId, out _);
        }

        public SerialRoute? GetRoute(Guid routeId)
        {
            _routes.TryGetValue(routeId, out var route);
            return route;
        }

        public IReadOnlyList<SerialRoute> GetRoutes()
        {
            return _routes.Values.ToList();
        }

        public async ValueTask StartRouteAsync(Guid routeId, CancellationToken cancellationToken = default)
        {
            if (!_routes.TryGetValue(routeId, out var route))
                throw new InvalidOperationException($"Route {routeId} not found.");

            if (_workers.ContainsKey(routeId))
                return; // Already running

            var sourceEndpoints = route.Sources
                .Select(id => _endpoints.TryGetValue(id, out var ep) ? ep : null)
                .Where(ep => ep != null)
                .ToList()!;

            var destEndpoints = route.Destinations
                .Select(id => _endpoints.TryGetValue(id, out var ep) ? ep : null)
                .Where(ep => ep != null)
                .ToList()!;

            if (sourceEndpoints.Count == 0)
                throw new InvalidOperationException($"Route {routeId} has no valid source endpoints.");

            if (destEndpoints.Count == 0)
                throw new InvalidOperationException($"Route {routeId} has no valid destination endpoints.");

            var stats = _statistics.GetOrAdd(routeId, _ => new RouteStatistics());
            stats.StartedAt = DateTime.UtcNow;

            var worker = new RouteWorker(route, sourceEndpoints, destEndpoints, stats);
            worker.FrameRouted += (s, f) => FrameRouted?.Invoke(this, f);
            worker.Error += (s, e) => Error?.Invoke(this, e);

            _workers[routeId] = worker;
            await worker.StartAsync(cancellationToken).ConfigureAwait(false);

            RouteStarted?.Invoke(this, routeId);
        }

        public async ValueTask StopRouteAsync(Guid routeId, CancellationToken cancellationToken = default)
        {
            if (_workers.TryRemove(routeId, out var worker))
            {
                await worker.StopAsync(cancellationToken).ConfigureAwait(false);
                RouteStopped?.Invoke(this, routeId);
            }
        }

        public async ValueTask StartAllAsync(CancellationToken cancellationToken = default)
        {
            foreach (var route in _routes.Values.Where(r => r.Enabled))
            {
                await StartRouteAsync(route.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask StopAllAsync(CancellationToken cancellationToken = default)
        {
            foreach (var routeId in _workers.Keys.ToList())
            {
                await StopRouteAsync(routeId, cancellationToken).ConfigureAwait(false);
            }
        }

        public RouteStatistics GetStatistics(Guid routeId)
        {
            return _statistics.GetOrAdd(routeId, _ => new RouteStatistics());
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await StopAllAsync().ConfigureAwait(false);

            foreach (var worker in _workers.Values)
            {
                await worker.DisposeAsync().ConfigureAwait(false);
            }
            _workers.Clear();
        }
    }
}
