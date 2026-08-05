namespace BPlusLib.Foundation.VirtualSerial.Endpoints
{
    /// <summary>
    /// Identifies the type of serial endpoint.
    /// </summary>
    public enum EndpointType
    {
        /// <summary>Virtual COM port via KMDF driver.</summary>
        VirtualSerial,

        /// <summary>Physical serial port via System.IO.Ports.</summary>
        PhysicalSerial,

        /// <summary>TCP client bridge.</summary>
        TcpClient,

        /// <summary>TCP server bridge.</summary>
        TcpServer,

        /// <summary>UDP bridge.</summary>
        Udp,

        /// <summary>TLS client bridge.</summary>
        TlsClient,

        /// <summary>TLS server bridge.</summary>
        TlsServer,

        /// <summary>WebSocket bridge.</summary>
        WebSocket,

        /// <summary>Named pipe bridge.</summary>
        NamedPipe
    }
}
