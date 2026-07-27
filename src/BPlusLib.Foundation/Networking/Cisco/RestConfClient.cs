using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BPlusLib.Foundation.Networking.Cisco
{
    /// <summary>
    /// HTTPS client for Cisco IOS XE / EWC RESTCONF API with Basic authentication.
    /// All methods are thread-safe.
    /// </summary>
    public sealed class RestConfClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestConfClient"/> class.
        /// </summary>
        /// <param name="host">The IP address or hostname of the WLC.</param>
        /// <param name="username">The RESTCONF username for Basic authentication.</param>
        /// <param name="password">The RESTCONF password for Basic authentication.</param>
        /// <param name="port">The HTTPS port (default: 443).</param>
        /// <param name="ignoreCertificateErrors">If <c>true</c>, SSL certificate validation is disabled (use only for testing).</param>
        public RestConfClient(
            string host,
            string username,
            string password,
            int port = 443,
            bool ignoreCertificateErrors = true)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host cannot be null or empty.", nameof(host));
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username cannot be null or empty.", nameof(username));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            _baseUrl = $"https://{host}:{port}";

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password),
                PreAuthenticate = true,
                UseDefaultCredentials = false
            };

            if (ignoreCertificateErrors)
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/yang-data+json"));
        }

        /// <summary>
        /// Gets the base URL used by this client.
        /// </summary>
        public string BaseUrl => _baseUrl;

        /// <summary>
        /// Performs a GET request and deserializes the JSON response.
        /// Returns <c>null</c> on failure.
        /// </summary>
        /// <param name="path">The RESTCONF resource path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The parsed <see cref="JObject"/>, or <c>null</c> on failure.</returns>
        public async Task<JObject?> GetAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JObject.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Performs a POST request with a JSON body.
        /// Returns the response status code on success, or <c>null</c> on failure.
        /// </summary>
        /// <param name="path">The RESTCONF resource path.</param>
        /// <param name="jsonBody">The JSON payload to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The HTTP status code, or <c>null</c> on failure.</returns>
        public async Task<int?> PostAsync(string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            try
            {
                using var content = new StringContent(jsonBody, Encoding.UTF8, "application/yang-data+json");
                using var response = await _httpClient.PostAsync(path, content, cancellationToken).ConfigureAwait(false);
                return (int)response.StatusCode;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Performs a GET request and returns the raw response string.
        /// Returns <c>null</c> on failure.
        /// </summary>
        /// <param name="path">The RESTCONF resource path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The raw JSON response string, or <c>null</c> on failure.</returns>
        public async Task<string?> GetRawAsync(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync(path, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tests whether the WLC is reachable over HTTPS.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if the device responded; otherwise, <c>false</c>.</returns>
        public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync("/restconf", cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the list of YANG modules supported by the WLC.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="JObject"/> containing the YANG module list, or <c>null</c> on failure.</returns>
        public async Task<JObject?> GetYangModulesAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync("/restconf/data/netconf-state/schemas", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the RESTCONF server capabilities.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="JObject"/> containing the capabilities, or <c>null</c> on failure.</returns>
        public async Task<JObject?> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            return await GetAsync("/restconf", cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Disposes the underlying HTTP client.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _httpClient.Dispose();
        }
    }
}
