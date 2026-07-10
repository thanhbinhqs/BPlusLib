// <copyright file="NetClientHelper.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
#if !NET472
using System.Net.Http;
#endif
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SYSLIB0014 // WebRequest/HttpWebRequest/WebClient are obsolete — required for FTP and net472 compat

namespace BPlusLib.Foundation.Networking
{
    /// <summary>
    /// Provides HTTP and FTP networking helpers with synchronous and
    /// asynchronous APIs. All methods are thread-safe and handle errors
    /// gracefully by returning <see langword="null"/> or <see langword="false"/>
    /// on failure instead of throwing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On .NET 6.0+ / .NET 8.0, <see cref="System.Net.Http.HttpClient"/> is used
    /// for all HTTP operations. On .NET Framework 4.7.2, <see cref="System.Net.HttpWebRequest"/>
    /// and <see cref="System.Net.WebClient"/> are used instead.
    /// </para>
    /// <para>
    /// FTP operations use <see cref="FtpWebRequest"/> on all targets.
    /// </para>
    /// <para>
    /// A statically cached <see cref="HttpClient"/> instance is used on modern
    /// .NET to reuse connections. On .NET Framework, each request creates a new
    /// <see cref="HttpWebRequest"/> / <see cref="WebClient"/>.
    /// </para>
    /// </remarks>
    public static class NetClientHelper
    {
        // -----------------------------------------------------------------
        // Fields
        // -----------------------------------------------------------------

        /// <summary>
        /// A lazily-initialized, shared <see cref="HttpClient"/> for connection
        /// pooling on .NET 6+. Thread-safe via <see cref="Lazy{T}"/>.
        /// </summary>
#if !NET472
        private static readonly Lazy<HttpClient> _sharedClient = new(
            () =>
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "BPlusLib.Foundation/2.0");
                client.DefaultRequestHeaders.Add("Accept", "*/*");
                return client;
            },
            LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the shared <see cref="HttpClient"/> instance. On .NET Framework
        /// this returns <see langword="null"/>; callers should use
        /// <see cref="HttpWebRequest"/> directly instead.
        /// </summary>
        private static HttpClient SharedClient => _sharedClient.Value;
#endif

        // -----------------------------------------------------------------
        // HTTP - Synchronous
        // -----------------------------------------------------------------

        /// <summary>
        /// Performs a synchronous HTTP GET request.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30000).</param>
        /// <param name="headers">Optional HTTP headers to include.</param>
        /// <returns>The response body as a string, or <see langword="null"/> on failure.</returns>
        public static string? HttpGet(string url, int timeoutMs = 30000, Dictionary<string, string>? headers = null)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
#if NET472
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = timeoutMs;
                request.UserAgent = "BPlusLib.Foundation/2.0";

                if (headers is not null)
                {
                    foreach (var kvp in headers)
                    {
                        request.Headers[kvp.Key] = kvp.Value;
                    }
                }

                using var response = (HttpWebResponse)request.GetResponse();
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                if (headers is not null)
                {
                    foreach (var kvp in headers)
                    {
                        request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                    }
                }

                var response = SharedClient!.Send(request, cts.Token);
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Performs a synchronous HTTP POST request.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="body">The request body string.</param>
        /// <param name="contentType">The content type header (default: "application/json").</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30000).</param>
        /// <param name="headers">Optional HTTP headers to include.</param>
        /// <returns>The response body as a string, or <see langword="null"/> on failure.</returns>
        public static string? HttpPost(
            string url,
            string body,
            string contentType = "application/json",
            int timeoutMs = 30000,
            Dictionary<string, string>? headers = null)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
#if NET472
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.Timeout = timeoutMs;
                request.ContentType = contentType;
                request.UserAgent = "BPlusLib.Foundation/2.0";

                if (headers is not null)
                {
                    foreach (var kvp in headers)
                    {
                        request.Headers[kvp.Key] = kvp.Value;
                    }
                }

                byte[] bodyBytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
                request.ContentLength = bodyBytes.Length;

                using (var reqStream = request.GetRequestStream())
                {
                    reqStream.Write(bodyBytes, 0, bodyBytes.Length);
                }

                using var response = (HttpWebResponse)request.GetResponse();
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(body ?? string.Empty, Encoding.UTF8, contentType),
                };

                if (headers is not null)
                {
                    foreach (var kvp in headers)
                    {
                        request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                    }
                }

                var response = SharedClient!.Send(request, cts.Token);
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads binary data from the specified URL as a byte array.
        /// </summary>
        /// <param name="url">The URL to download from.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 60000).</param>
        /// <returns>The downloaded bytes, or <see langword="null"/> on failure.</returns>
        public static byte[]? HttpDownload(string url, int timeoutMs = 60000)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
#if NET472
                using var webClient = new WebClient();
                webClient.Headers.Add("User-Agent", "BPlusLib.Foundation/2.0");
                return webClient.DownloadData(url);
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                var response = SharedClient!.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                    .GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsByteArrayAsync(CancellationToken.None)
                    .GetAwaiter().GetResult();
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads a file from the specified URL to a local path.
        /// </summary>
        /// <param name="url">The URL to download from.</param>
        /// <param name="outputPath">The local file path to write to.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 60000).</param>
        /// <returns><see langword="true"/> if the download succeeded; otherwise <see langword="false"/>.</returns>
        public static bool TryDownloadFile(string url, string outputPath, int timeoutMs = 60000)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(outputPath))
                return false;

            try
            {
                byte[]? data = HttpDownload(url, timeoutMs);
                if (data is null)
                    return false;

                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.WriteAllBytes(outputPath, data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // HTTP - Asynchronous
        // -----------------------------------------------------------------

        /// <summary>
        /// Performs an asynchronous HTTP GET request.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30000).</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result is the response body string, or <see langword="null"/> on failure.
        /// </returns>
        public static async Task<string?> HttpGetAsync(string url, int timeoutMs = 30000)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
#if NET472
                return await Task.Run(() => HttpGet(url, timeoutMs)).ConfigureAwait(false);
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                var response = await SharedClient!.GetAsync(url, cts.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false);
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Performs an asynchronous HTTP POST request.
        /// </summary>
        /// <param name="url">The request URL.</param>
        /// <param name="body">The request body string.</param>
        /// <param name="contentType">The content type header (default: "application/json").</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30000).</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result is the response body string, or <see langword="null"/> on failure.
        /// </returns>
        public static async Task<string?> HttpPostAsync(
            string url,
            string body,
            string contentType = "application/json",
            int timeoutMs = 30000)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
#if NET472
                return await Task.Run(() => HttpPost(url, body, contentType, timeoutMs)).ConfigureAwait(false);
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                var content = new StringContent(body ?? string.Empty, Encoding.UTF8, contentType);
                var response = await SharedClient!.PostAsync(url, content, cts.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false);
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads binary data from the specified URL asynchronously.
        /// </summary>
        /// <param name="url">The URL to download from.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 60000).</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result is the downloaded bytes, or <see langword="null"/> on failure.
        /// </returns>
        public static async Task<byte[]?> HttpDownloadAsync(string url, int timeoutMs = 60000)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
#if NET472
                return await Task.Run(() => HttpDownload(url, timeoutMs)).ConfigureAwait(false);
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                var response = await SharedClient!.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(CancellationToken.None)
                    .ConfigureAwait(false);
#endif
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads a file from the specified URL to a local path asynchronously.
        /// </summary>
        /// <param name="url">The URL to download from.</param>
        /// <param name="outputPath">The local file path to write to.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 60000).</param>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result is <see langword="true"/> if the download succeeded;
        /// otherwise <see langword="false"/>.
        /// </returns>
        public static async Task<bool> TryDownloadFileAsync(string url, string outputPath, int timeoutMs = 60000)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(outputPath))
                return false;

            try
            {
                byte[]? data = await HttpDownloadAsync(url, timeoutMs).ConfigureAwait(false);
                if (data is null)
                    return false;

                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

#if NET472
                File.WriteAllBytes(outputPath, data);
                return await Task.FromResult(true).ConfigureAwait(false);
#else
                await File.WriteAllBytesAsync(outputPath, data).ConfigureAwait(false);
                return true;
#endif
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // FTP Operations
        // -----------------------------------------------------------------

        /// <summary>
        /// Lists the contents of an FTP directory.
        /// </summary>
        /// <param name="url">The FTP directory URL (e.g. "ftp://server.com/path/").</param>
        /// <param name="username">Optional FTP username (default: anonymous).</param>
        /// <param name="password">Optional FTP password (default: anonymous).</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 30000).</param>
        /// <returns>
        /// An array of directory listing strings (one per line), or
        /// <see langword="null"/> on failure.
        /// </returns>
        public static string[]? FtpListDirectory(string url, string? username = null, string? password = null, int timeoutMs = 30000)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Timeout = timeoutMs;
                request.ReadWriteTimeout = timeoutMs;
                request.EnableSsl = false;

                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    request.Credentials = new NetworkCredential(username ?? string.Empty, password ?? string.Empty);
                }
                else
                {
                    request.Credentials = new NetworkCredential("anonymous", "anonymous@bpluslib.com");
                }

                using var response = (FtpWebResponse)request.GetResponse();
                using var stream = response.GetResponseStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                string allText = reader.ReadToEnd();

                if (string.IsNullOrEmpty(allText))
                    return Array.Empty<string>();

                return allText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads a file from an FTP server to a local path.
        /// </summary>
        /// <param name="url">The FTP file URL.</param>
        /// <param name="outputPath">The local file path to write to.</param>
        /// <param name="username">Optional FTP username.</param>
        /// <param name="password">Optional FTP password.</param>
        /// <returns><see langword="true"/> if the download succeeded; otherwise <see langword="false"/>.</returns>
        public static bool FtpDownloadFile(string url, string outputPath, string? username = null, string? password = null)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(outputPath))
                return false;

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.EnableSsl = false;

                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    request.Credentials = new NetworkCredential(username ?? string.Empty, password ?? string.Empty);
                }
                else
                {
                    request.Credentials = new NetworkCredential("anonymous", "anonymous@bpluslib.com");
                }

                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using var response = (FtpWebResponse)request.GetResponse();
                using var stream = response.GetResponseStream();
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
                stream.CopyTo(fileStream);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Uploads a file to an FTP server.
        /// </summary>
        /// <param name="url">The destination FTP URL (including filename).</param>
        /// <param name="localPath">The local file path to upload.</param>
        /// <param name="username">Optional FTP username.</param>
        /// <param name="password">Optional FTP password.</param>
        /// <returns><see langword="true"/> if the upload succeeded; otherwise <see langword="false"/>.</returns>
        public static bool FtpUploadFile(string url, string localPath, string? username = null, string? password = null)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(localPath))
                return false;

            if (!File.Exists(localPath))
                return false;

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.EnableSsl = false;

                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    request.Credentials = new NetworkCredential(username ?? string.Empty, password ?? string.Empty);
                }
                else
                {
                    request.Credentials = new NetworkCredential("anonymous", "anonymous@bpluslib.com");
                }

                byte[] fileBytes = File.ReadAllBytes(localPath);
                request.ContentLength = fileBytes.Length;

                using (var reqStream = request.GetRequestStream())
                {
                    reqStream.Write(fileBytes, 0, fileBytes.Length);
                }

                using var response = (FtpWebResponse)request.GetResponse();
                return response.StatusCode == FtpStatusCode.ClosingData ||
                       response.StatusCode == FtpStatusCode.CommandOK;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a directory on an FTP server.
        /// </summary>
        /// <param name="url">The FTP directory URL to create.</param>
        /// <param name="username">Optional FTP username.</param>
        /// <param name="password">Optional FTP password.</param>
        /// <returns><see langword="true"/> if the directory was created; otherwise <see langword="false"/>.</returns>
        public static bool FtpCreateDirectory(string url, string? username = null, string? password = null)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                request.EnableSsl = false;

                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    request.Credentials = new NetworkCredential(username ?? string.Empty, password ?? string.Empty);
                }
                else
                {
                    request.Credentials = new NetworkCredential("anonymous", "anonymous@bpluslib.com");
                }

                using var response = (FtpWebResponse)request.GetResponse();
                return response.StatusCode == FtpStatusCode.PathnameCreated ||
                       response.StatusCode == FtpStatusCode.CommandOK;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a file from an FTP server.
        /// </summary>
        /// <param name="url">The FTP file URL to delete.</param>
        /// <param name="username">Optional FTP username.</param>
        /// <param name="password">Optional FTP password.</param>
        /// <returns><see langword="true"/> if the file was deleted; otherwise <see langword="false"/>.</returns>
        public static bool FtpDeleteFile(string url, string? username = null, string? password = null)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.EnableSsl = false;

                if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
                {
                    request.Credentials = new NetworkCredential(username ?? string.Empty, password ?? string.Empty);
                }
                else
                {
                    request.Credentials = new NetworkCredential("anonymous", "anonymous@bpluslib.com");
                }

                using var response = (FtpWebResponse)request.GetResponse();
                return response.StatusCode == FtpStatusCode.FileActionOK ||
                       response.StatusCode == FtpStatusCode.CommandOK;
            }
            catch
            {
                return false;
            }
        }

        // -----------------------------------------------------------------
        // Network Info Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Checks whether any network interface is available and connected.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if a network interface is available and
        /// connected; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsNetworkAvailable()
        {
            try
            {
                return NetworkInterface.GetIsNetworkAvailable();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether internet connectivity is available by pinging a
        /// well-known host (8.8.8.8 by default).
        /// </summary>
        /// <param name="timeoutMs">Ping timeout in milliseconds (default: 3000).</param>
        /// <returns>
        /// <see langword="true"/> if the ping succeeded; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsInternetAvailable(int timeoutMs = 3000)
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send("8.8.8.8", timeoutMs);
                return reply is not null && reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to retrieve the public IP address of the current machine
        /// by querying an external service (https://api.ipify.org).
        /// </summary>
        /// <param name="timeoutMs">HTTP timeout in milliseconds (default: 5000).</param>
        /// <returns>
        /// The public IP address as a string, or <see langword="null"/> if
        /// it could not be determined.
        /// </returns>
        public static string? GetPublicIpAddress(int timeoutMs = 5000)
        {
            try
            {
                // Try ipify.org first, fall back to other services
                string? ip = HttpGet("https://api.ipify.org", timeoutMs);
                if (!string.IsNullOrEmpty(ip))
                    return ip?.Trim();

                ip = HttpGet("https://icanhazip.com", timeoutMs);
                if (!string.IsNullOrEmpty(ip))
                    return ip?.Trim();

                ip = HttpGet("https://checkip.amazonaws.com", timeoutMs);
                return ip?.Trim();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the HTTP response status code for the specified URL by
        /// performing an HTTP HEAD request.
        /// </summary>
        /// <param name="url">The URL to check.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (default: 5000).</param>
        /// <returns>
        /// The HTTP status code as an integer, or <see langword="null"/> if
        /// the request failed.
        /// </returns>
        public static int? GetHttpResponseStatusCode(string url, int timeoutMs = 5000)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
#if NET472
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = timeoutMs;
                request.UserAgent = "BPlusLib.Foundation/2.0";

                using var response = (HttpWebResponse)request.GetResponse();
                return (int)response.StatusCode;
#else
                using var cts = new CancellationTokenSource(timeoutMs);
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                var response = SharedClient!.Send(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                return (int)response.StatusCode;
#endif
            }
            catch
            {
                return null;
            }
        }
    }
}
