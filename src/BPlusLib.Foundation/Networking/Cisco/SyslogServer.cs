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
    /// </summary>
    public sealed class SyslogServer : IDisposable
    {
        private readonly int _port;
        private readonly ConcurrentQueue<CiscoSyslogEntry> _messageQueue;
        private UdpClient? _udpClient;
        private CancellationTokenSource? _cts;
        private Task? _listenerTask;
        private bool _disposed;

        public event Action<CiscoSyslogEntry>? MessageReceived;
        public int Port => _port;
        public bool IsListening => _listenerTask != null && !_listenerTask.IsCompleted;

        public SyslogServer(int port = 514)
        {
            _port = port;
            _messageQueue = new ConcurrentQueue<CiscoSyslogEntry>();
        }

        public void Start()
        {
            if (_listenerTask != null && !_listenerTask.IsCompleted)
                return;
            _cts = new CancellationTokenSource();
            _udpClient = new UdpClient(_port);
            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Start();
            return Task.CompletedTask;
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _udpClient?.Close(); } catch { }
            try { _listenerTask?.Wait(TimeSpan.FromSeconds(5)); } catch { }
            _udpClient?.Dispose();
            _udpClient = null;
            _cts?.Dispose();
            _cts = null;
        }

        public bool TryDequeue(out CiscoSyslogEntry? entry)
        {
            return _messageQueue.TryDequeue(out entry);
        }

        public CiscoSyslogEntry[] DequeueAll()
        {
            var results = new System.Collections.Generic.List<CiscoSyslogEntry>();
            while (_messageQueue.TryDequeue(out var entry))
            {
                if (entry != null) results.Add(entry);
            }
            return results.ToArray();
        }

        public int PendingCount => _messageQueue.Count;

        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient!.ReceiveAsync().ConfigureAwait(false);
                    var entry = ParseRfc5424(
                        Encoding.UTF8.GetString(result.Buffer),
                        result.RemoteEndPoint.Address.ToString(),
                        result.RemoteEndPoint.Port);
                    _messageQueue.Enqueue(entry);
                    try { MessageReceived?.Invoke(entry); } catch { }
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch { }
            }
        }

        internal static CiscoSyslogEntry ParseRfc5424(string rawMessage, string sourceIp, int sourcePort)
        {
            try
            {
                int version = 1;
                string timestamp = string.Empty;
                string hostname = string.Empty;
                string appName = string.Empty;
                string processId = string.Empty;
                string messageId = string.Empty;
                int severity = 6;
                int facility = 16;
                string message = string.Empty;

                if (rawMessage.StartsWith("<"))
                {
                    int priEnd = rawMessage.IndexOf('>');
                    if (priEnd > 0)
                    {
                        int pri = int.Parse(rawMessage.Substring(1, priEnd - 1));
                        facility = pri / 8;
                        severity = pri % 8;
                    }
                }

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
                    message = rawMessage;
                    hostname = sourceIp;
                }

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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
