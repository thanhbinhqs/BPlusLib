using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BPlusLib.Foundation.Networking.Cisco.Models;

namespace BPlusLib.Foundation.Networking.Cisco
{
    /// <summary>
    /// Lightweight UDP syslog listener that receives RFC 5424 messages from Cisco WLCs
    /// and stores them in a thread-safe concurrent queue.
    /// Implements <see cref="IDisposable"/>.
    /// </summary>
    public sealed class SyslogServer : IDisposable
    {
        private readonly int _port;
        private readonly ConcurrentQueue<CiscoSyslogEntry> _messageQueue;
        private UdpClient? _udpClient;
        private CancellationTokenSource? _cts;
        private Task? _listenerTask;
        private bool _disposed;

        /// <summary>
        /// Event raised when a new syslog message is received and parsed.
        /// </summary>
        public event Action<CiscoSyslogEntry>? MessageReceived;

        /// <summary>
        /// Gets the UDP port the syslog server is listening on.
        /// </summary>
        public int Port => _port;

        /// <summary>
        /// Gets whether the syslog listener is currently running.
        /// </summary>
        public bool IsListening => _listenerTask != null && !_listenerTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyslogServer"/> class.
        /// </summary>
        /// <param name="port">The UDP port to listen on (default: 514, which requires elevated privileges).</param>
        public SyslogServer(int port = 514)
        {
            _port = port;
            _messageQueue = new ConcurrentQueue<CiscoSyslogEntry>();
        }

        /// <summary>
        /// Starts the UDP syslog listener on the configured port.
        /// If already listening, this method is a no-op.
        /// </summary>
        public void Start()
        {
            if (_listenerTask != null && !_listenerTask.IsCompleted)
                return;

            _cts = new CancellationTokenSource();
            _udpClient = new UdpClient(_port);

            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Asynchronously starts the UDP syslog listener.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to stop the listener.</param>
        /// <returns>A task representing the asynchronous start operation.</returns>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Start();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the syslog listener gracefully.
        /// </summary>
        public void Stop()
        {
            _cts?.Cancel();
            try
            {
                _udpClient?.Close();
            }
            catch
            {
                // Suppress exceptions during disposal.
            }

            try
            {
                _listenerTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Suppress task cancellation exceptions.
            }

            _udpClient?.Dispose();
            _udpClient = null;
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>
        /// Tries to dequeue a syslog message from the buffer.
        /// </summary>
        /// <param name="entry">The dequeued syslog entry, if available.</param>
        /// <returns><c>true</c> if a message was available; otherwise, <c>false</c>.</returns>
        public bool TryDequeue(out CiscoSyslogEntry? entry)
        {
            return _messageQueue.TryDequeue(out entry);
        }

        /// <summary>
        /// Attempts to dequeue all available messages from the buffer.
        /// </summary>
        /// <returns>An array of all available syslog entries (may be empty).</returns>
        public CiscoSyslogEntry[] DequeueAll()
        {
            var results = new System.Collections.Generic.List<CiscoSyslogEntry>();
            while (_messageQueue.TryDequeue(out var entry))
            {
                if (entry != null)
                    results.Add(entry);
            }
            return results.ToArray();
        }

        /// <summary>
        /// Gets the approximate count of unread messages in the queue.
        /// </summary>
        public int PendingCount => _messageQueue.Count;

        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient!.ReceiveAsync(cancellationToken).ConfigureAwait(false);

                    var entry = ParseRfc5424(
                        Encoding.UTF8.GetString(result.Buffer),
                        result.RemoteEndPoint.Address.ToString(),
                        result.RemoteEndPoint.Port);

                    _messageQueue.Enqueue(entry);

                    try
                    {
                        MessageReceived?.Invoke(entry);
                    }
                    catch
                    {
                        // Subscriber exception should not crash the listener.
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    // Socket closed; stop listening.
                    break;
                }
                catch
                {
                    // Swallow unexpected exceptions and continue listening.
                }
            }
        }

        /// <summary>
        /// Parses an RFC 5424 syslog message into a <see cref="CiscoSyslogEntry"/>.
        /// Falls back gracefully on malformed messages.
        /// </summary>
        /// <param name="rawMessage">The raw syslog message string.</param>
        /// <param name="sourceIp">The IP address of the sender.</param>
        /// <param name="sourcePort">The port of the sender.</param>
        /// <returns>A parsed <see cref="CiscoSyslogEntry"/>.</returns>
        internal static CiscoSyslogEntry ParseRfc5424(string rawMessage, string sourceIp, int sourcePort)
        {
            try
            {
                // RFC 5424 format: <PRI>VERSION TIMESTAMP HOSTNAME APP-NAME PROCID MSGID STRUCTURED-DATA MSG
                // Example: <134>1 2024-01-15T10:30:00.000Z WLC01 cisco_wlc - - - Some message text

                int version = 1;
                string timestamp = string.Empty;
                string hostname = string.Empty;
                string appName = string.Empty;
                string processId = string.Empty;
                string messageId = string.Empty;
                int severity = 6;
                int facility = 16;
                string message = string.Empty;

                // Parse PRI field (<FACILITY*8+SEVERITY>)
                if (rawMessage.StartsWith('<'))
                {
                    int priEnd = rawMessage.IndexOf('>');
                    if (priEnd > 0)
                    {
                        int pri = int.Parse(rawMessage.Substring(1, priEnd - 1));
                        facility = pri / 8;
                        severity = pri % 8;
                    }
                }

                // Try RFC 5424 regex: <PRI>VERSION SP TIMESTAMP SP HOSTNAME SP APP-NAME SP PROCID SP MSGID SP STRUCTURED-DATA [SP MSG]
                var match = Regex.Match(rawMessage,
                    @"^<(\d+)>\s*(\d+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s+(.*)",
                    RegexOptions.Compiled);

                if (match.Success)
                {
                    version = int.Parse(match.Groups[2].Value);
                    timestamp = match.Groups[3].Value;
                    hostname = match.Groups[4].Value;
                    appName = match.Groups[5].Value;
                    processId = match.Groups[6].Value;
                    messageId = match.Groups[7].Value;
                    message = match.Groups[9].Value;
                }
                else
                {
                    // Fallback: try to extract what we can
                    message = rawMessage;
                    hostname = sourceIp;
                }

                // Parse timestamp
                DateTimeOffset timestampOffset = DateTimeOffset.UtcNow;
                if (!string.IsNullOrEmpty(timestamp) && timestamp != "-")
                {
                    if (DateTimeOffset.TryParse(timestamp,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal,
                        out var parsed))
                    {
                        timestampOffset = parsed;
                    }
                }

                return new CiscoSyslogEntry
                {
                    Version = version,
                    Timestamp = timestampOffset,
                    Hostname = hostname,
                    AppName = appName,
                    ProcessId = processId,
                    MessageId = messageId,
                    Severity = severity,
                    Facility = facility,
                    Message = message,
                    RawMessage = rawMessage,
                    SourceIp = sourceIp,
                    SourcePort = sourcePort,
                    ReceivedAt = DateTimeOffset.UtcNow
                };
            }
            catch
            {
                // Return a minimal entry with the raw message on parse failure.
                return new CiscoSyslogEntry
                {
                    RawMessage = rawMessage,
                    Message = rawMessage,
                    SourceIp = sourceIp,
                    SourcePort = sourcePort,
                    ReceivedAt = DateTimeOffset.UtcNow
                };
            }
        }

        /// <summary>
        /// Disposes the syslog server and releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Stop();
        }
    }
}
