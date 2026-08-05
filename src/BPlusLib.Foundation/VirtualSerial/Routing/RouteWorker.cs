using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BPlusLib.Foundation.VirtualSerial.Endpoints;
using BPlusLib.Foundation.VirtualSerial.Arbitration;

namespace BPlusLib.Foundation.VirtualSerial.Routing
{
    /// <summary>
    /// Pumps data from source endpoints to destination endpoints for a single route.
    /// </summary>
    internal sealed class RouteWorker : IAsyncDisposable
    {
        private readonly SerialRoute _route;
        private readonly IReadOnlyList<ISerialEndpoint> _sources;
        private readonly IReadOnlyList<ISerialEndpoint> _destinations;
        private readonly RouteStatistics _statistics;
        private readonly List<Task> _pumpTasks = new();
        private CancellationTokenSource? _cts;
        private IWriteArbiter? _arbiter;
        private bool _disposed;

        public event EventHandler<SerialFrame>? FrameRouted;
        public event EventHandler<RouteErrorEventArgs>? Error;

        public RouteWorker(
            SerialRoute route,
            IReadOnlyList<ISerialEndpoint> sources,
            IReadOnlyList<ISerialEndpoint> destinations,
            RouteStatistics statistics)
        {
            _route = route ?? throw new ArgumentNullException(nameof(route));
            _sources = sources ?? throw new ArgumentNullException(nameof(sources));
            _destinations = destinations ?? throw new ArgumentNullException(nameof(destinations));
            _statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));

            // Create arbiter based on policy
            _arbiter = _route.WriteArbitration switch
            {
                WriteArbitrationPolicy.Serialized => new SerializedWriteArbiter(),
                WriteArbitrationPolicy.SingleWriter => new SingleWriterArbiter(),
                WriteArbitrationPolicy.TransactionLock => new TransactionArbiter(),
                _ => null
            };
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) return;

            _cts = new CancellationTokenSource();

            // Start each source → destinations pump
            foreach (var source in _sources)
            {
                if (!source.IsRunning)
                {
                    try { await source.StartAsync(cancellationToken).ConfigureAwait(false); }
                    catch (Exception ex)
                    {
                        Error?.Invoke(this, new RouteErrorEventArgs
                        {
                            RouteId = _route.Id,
                            Exception = ex,
                            Message = $"Failed to start source {source.Name}"
                        });
                    }
                }

                _pumpTasks.Add(Task.Run(() => PumpAsync(source, _cts.Token), _cts.Token));
            }
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken = default)
        {
            if (_cts != null)
            {
                await _cts.CancelAsync().ConfigureAwait(false);

                foreach (var task in _pumpTasks)
                {
                    try { await task.ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                    catch { /* ignore */ }
                }
                _pumpTasks.Clear();
            }
        }

        private async Task PumpAsync(ISerialEndpoint source, CancellationToken ct)
        {
            try
            {
                await foreach (var frame in source.ReadAllAsync(ct).ConfigureAwait(false))
                {
                    // Route to all destinations
                    foreach (var dest in _destinations)
                    {
                        if (!dest.IsRunning)
                        {
                            try { await dest.StartAsync(ct).ConfigureAwait(false); }
                            catch { continue; }
                        }

                        try
                        {
                            if (_arbiter != null)
                            {
                                await using var token = await _arbiter.AcquireAsync(
                                    source.Id, ct).ConfigureAwait(false);
                                await dest.SendAsync(frame.Data, ct).ConfigureAwait(false);
                            }
                            else
                            {
                                await dest.SendAsync(frame.Data, ct).ConfigureAwait(false);
                            }
                            _statistics.FramesRouted++;
                            _statistics.BytesRouted += frame.Data.Length;
                            FrameRouted?.Invoke(this, frame);
                        }
                        catch (Exception ex)
                        {
                            _statistics.Errors++;
                            Error?.Invoke(this, new RouteErrorEventArgs
                            {
                                RouteId = _route.Id,
                                Exception = ex,
                                Message = $"Failed to route from {source.Name} to {dest.Name}"
                            });
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Error?.Invoke(this, new RouteErrorEventArgs
                {
                    RouteId = _route.Id,
                    Exception = ex,
                    Message = $"Pump error for source {source.Name}"
                });
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            await StopAsync().ConfigureAwait(false);
            _cts?.Dispose();
        }
    }
}
