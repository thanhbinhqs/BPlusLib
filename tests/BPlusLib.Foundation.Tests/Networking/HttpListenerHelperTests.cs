// <copyright file="HttpListenerHelperTests.cs" company="BPlusLib">
// Copyright (c) BPlusLib. All rights reserved.
// Licensed under the MIT license.
// </copyright>

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BPlusLib.Foundation.Networking;
using FluentAssertions;
using Xunit;

namespace BPlusLib.Foundation.Tests.Networking
{
    /// <summary>
    /// Unit tests for the <see cref="HttpListenerHelper"/> class.
    /// All tests are skipped on non-Windows platforms (HttpListener requires Windows).
    /// </summary>
    [Trait("Category", "Networking")]
    public sealed class HttpListenerHelperTests
    {
        /// <summary>
        /// Verifies that HttpListener can be started and stopped.
        /// </summary>
        [SkippableFact]
        public void StartStop_Succeeds()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            int port = HttpListenerHelper.GetFreePort();
            string prefix = $"http://localhost:{port}/";
            var listener = HttpListenerHelper.Start(prefix);
            listener.Should().NotBeNull();
            listener!.IsListening.Should().BeTrue();

            bool stopped = HttpListenerHelper.Stop(listener);
            stopped.Should().BeTrue();
        }

        /// <summary>
        /// Verifies a full roundtrip: start server, send GET request, receive response.
        /// </summary>
        [SkippableFact]
        public async Task GetRequest_Roundtrip_ReturnsResponse()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            int port = HttpListenerHelper.GetFreePort();
            string prefix = $"http://localhost:{port}/";
            using var listener = HttpListenerHelper.Start(prefix);
            listener.Should().NotBeNull();

            // Handle request in background
            var responseContent = "Hello, World!";
            var serverTask = Task.Run(() =>
            {
                var ctx = HttpListenerHelper.GetRequest(listener, 10000);
                ctx.Should().NotBeNull();
                HttpListenerHelper.SendText(ctx!.Response, responseContent);
            });

            // Send GET request
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"http://localhost:{port}/");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Be(responseContent);

            await serverTask;
        }

        /// <summary>
        /// Verifies that SendJson sends the correct content type.
        /// </summary>
        [SkippableFact]
        public async Task SendJson_SendsJsonContentType()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            int port = HttpListenerHelper.GetFreePort();
            string prefix = $"http://localhost:{port}/";
            using var listener = HttpListenerHelper.Start(prefix);
            listener.Should().NotBeNull();

            var serverTask = Task.Run(() =>
            {
                var ctx = HttpListenerHelper.GetRequest(listener, 10000);
                ctx.Should().NotBeNull();
                HttpListenerHelper.SendJson(ctx!.Response, "{\"key\":\"value\"}");
            });

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync($"http://localhost:{port}/");
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

            await serverTask;
        }

        /// <summary>
        /// Verifies that Start returns null for invalid prefix.
        /// </summary>
        [SkippableFact]
        public void Start_InvalidPrefix_ReturnsNull()
        {
            // HttpListener.Start throws on invalid prefix regardless of OS
            var listener = HttpListenerHelper.Start("not-a-valid-prefix");
            listener.Should().BeNull();
        }

        /// <summary>
        /// Verifies that Stop on a null or already-stopped listener does not throw.
        /// </summary>
        [SkippableFact]
        public void Stop_AlreadyStopped_DoesNotThrow()
        {
            Skip.IfNot(OperatingSystem.IsWindows());

            bool result = HttpListenerHelper.Stop(null!);
            result.Should().BeFalse();
        }
    }
}
