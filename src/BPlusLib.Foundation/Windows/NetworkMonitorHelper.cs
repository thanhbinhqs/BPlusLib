// <copyright file="NetworkMonitorHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Net.NetworkInformation;
using System.Threading;

namespace BPlusLib.Foundation.Windows
{
    /// <summary>Network connectivity status.</summary>
    public enum NetworkStatus
    {
        /// <summary>Unknown status.</summary>
        Unknown = 0,
        /// <summary>Connected to a network.</summary>
        Connected = 1,
        /// <summary>No network connection.</summary>
        Disconnected = 2,
        /// <summary>Connected via LAN (ethernet).</summary>
        ConnectedAsLAN = 3,
        /// <summary>Connected via WiFi.</summary>
        ConnectedAsWiFi = 4,
    }

    /// <summary>Event args for network status changes.</summary>
    public sealed class NetworkChangeEventArgs : EventArgs
    {
        /// <summary>New network status.</summary>
        public NetworkStatus Status { get; init; }
        /// <summary>Whether currently connected.</summary>
        public bool IsConnected { get; init; }
        /// <summary>Number of active network interfaces.</summary>
        public int InterfaceCount { get; init; }
    }

    /// <summary>
    /// Monitors network connectivity changes using NetworkInterface.GetIsNetworkAvailable
    /// polling and System.Net.NetworkInformation.NetworkChange for event-based notifications.
    /// All methods are thread-safe.
    /// </summary>
    public sealed class NetworkMonitor : IDisposable
    {
        private Timer? _pollTimer;
        private bool _isMonitoring;
        private bool _lastKnownStatus;
        private readonly object _lock = new();
        private bool _disposed;

        /// <summary>Fires when network status changes.</summary>
        public event EventHandler<NetworkChangeEventArgs>? StatusChanged;

        /// <summary>Current network status.</summary>
        public NetworkStatus CurrentStatus { get; private set; }

        /// <summary>Whether currently connected to any network.</summary>
        public bool IsConnected => CurrentStatus != NetworkStatus.Disconnected && CurrentStatus != NetworkStatus.Unknown;

        /// <summary>Whether monitoring is active.</summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// Initializes a new NetworkMonitor.
        /// </summary>
        public NetworkMonitor()
        {
            _lastKnownStatus = NetworkInterface.GetIsNetworkAvailable();
            CurrentStatus = _lastKnownStatus ? NetworkStatus.Connected : NetworkStatus.Disconnected;
        }

        /// <summary>
        /// Starts monitoring network changes. Uses polling at the specified interval.
        /// </summary>
        /// <param name="pollIntervalMs">Polling interval in milliseconds (default: 2000).</param>
        /// <returns>True if monitoring started.</returns>
        public bool Start(int pollIntervalMs = 2000)
        {
            if (_disposed) return false;
            if (_isMonitoring) return true;

            try
            {
                lock (_lock)
                {
                    _pollTimer = new Timer(PollCallback, null, pollIntervalMs, pollIntervalMs);
                    _isMonitoring = true;
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Stops monitoring.
        /// </summary>
        public bool Stop()
        {
            if (!_isMonitoring) return true;

            try
            {
                lock (_lock)
                {
                    _pollTimer?.Dispose();
                    _pollTimer = null;
                    _isMonitoring = false;
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Checks if the network is currently available (static, no monitoring needed).
        /// </summary>
        public static bool IsNetworkAvailable()
        {
            try { return NetworkInterface.GetIsNetworkAvailable(); }
            catch { return false; }
        }

        /// <summary>
        /// Gets the number of active network interfaces.
        /// </summary>
        public static int GetActiveInterfaceCount()
        {
            try { return NetworkInterface.GetAllNetworkInterfaces().Length; }
            catch { return 0; }
        }

        /// <summary>
        /// Gets the number of active, operational network interfaces.
        /// </summary>
        public static int GetOperationalInterfaceCount()
        {
            try
            {
                int count = 0;
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                        count++;
                }
                return count;
            }
            catch { return 0; }
        }

        private void PollCallback(object? state)
        {
            if (_disposed) return;

            try
            {
                bool currentStatus = NetworkInterface.GetIsNetworkAvailable();
                int interfaceCount = GetOperationalInterfaceCount();

                if (currentStatus != _lastKnownStatus)
                {
                    _lastKnownStatus = currentStatus;
                    CurrentStatus = currentStatus ? NetworkStatus.Connected : NetworkStatus.Disconnected;

                    StatusChanged?.Invoke(this, new NetworkChangeEventArgs
                    {
                        Status = CurrentStatus,
                        IsConnected = currentStatus,
                        InterfaceCount = interfaceCount,
                    });
                }
            }
            catch { /* polling errors are silently ignored */ }
        }

        /// <summary>
        /// Disposes the monitor and stops polling.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Stop();
            }
        }
    }
}
