// <copyright file="HttpListenerHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BPlusLib.Foundation.Networking
{
    /// <summary>
    /// Basic embedded HTTP server helper using <see cref="System.Net.HttpListener"/>.
    /// Provides simple wrappers for common HTTP server operations.
    /// </summary>
    public static class HttpListenerHelper
    {
        /// <summary>
        /// Starts an <see cref="HttpListener"/> on the specified prefix.
        /// </summary>
        /// <param name="prefix">The URI prefix to listen on (e.g., "http://localhost:8080/").</param>
        /// <param name="user">Optional Windows user account to restrict access (e.g., "BUILTIN\\Users").</param>
        /// <returns>The started <see cref="HttpListener"/>, or null if startup failed.</returns>
        public static HttpListener? Start(string prefix, string? user = null)
        {
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add(prefix);

                if (!string.IsNullOrEmpty(user))
                {
                    listener.AuthenticationSchemes = AuthenticationSchemes.IntegratedWindowsAuthentication;
                    listener.Realm = user;
                }

                listener.Start();
                return listener;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Stops an <see cref="HttpListener"/>.
        /// </summary>
        /// <param name="listener">The listener to stop.</param>
        /// <returns>True if the listener was stopped successfully.</returns>
        public static bool Stop(HttpListener listener)
        {
            try
            {
                if (listener == null)
                {
                    return false;
                }

                if (listener.IsListening)
                {
                    listener.Stop();
                    listener.Close();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for an incoming HTTP request with a timeout.
        /// </summary>
        /// <param name="listener">The listener to get the request from.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 5000).</param>
        /// <returns>The <see cref="HttpListenerContext"/> or null if timed out.</returns>
        public static HttpListenerContext? GetRequest(HttpListener listener, int timeoutMs = 5000)
        {
            try
            {
                var task = listener.GetContextAsync();
                if (task.Wait(TimeSpan.FromMilliseconds(timeoutMs)))
                {
                    return task.Result;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sends a plain text response.
        /// </summary>
        /// <param name="response">The HTTP response object.</param>
        /// <param name="text">The text to send.</param>
        /// <param name="contentType">The content type (default: "text/plain").</param>
        public static void SendText(HttpListenerResponse response, string text, string contentType = "text/plain")
        {
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                response.ContentType = contentType;
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Flush();
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        /// <summary>
        /// Sends a JSON response.
        /// </summary>
        /// <param name="response">The HTTP response object.</param>
        /// <param name="json">The JSON string to send.</param>
        public static void SendJson(HttpListenerResponse response, string json)
        {
            SendText(response, json, "application/json");
        }

        /// <summary>
        /// Sends a binary response.
        /// </summary>
        /// <param name="response">The HTTP response object.</param>
        /// <param name="data">The binary data to send.</param>
        /// <param name="contentType">The MIME content type.</param>
        public static void SendBinary(HttpListenerResponse response, byte[] data, string contentType)
        {
            try
            {
                response.ContentType = contentType;
                response.ContentLength64 = data.Length;
                response.OutputStream.Write(data, 0, data.Length);
                response.OutputStream.Flush();
            }
            finally
            {
                try { response.Close(); } catch { }
            }
        }

        /// <summary>
        /// Finds a free TCP port on localhost.
        /// </summary>
        /// <returns>A free port number.</returns>
        public static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
