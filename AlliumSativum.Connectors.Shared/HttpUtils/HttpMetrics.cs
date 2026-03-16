using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace AlliumSativum.Connectors.Shared.HttpUtils;

public sealed class HttpMetrics<T> where T : new()
{
    public T? Response { get; set; }

    // Connection Phase
    public long DnsLookupMs { get; internal set; }
    public long TcpConnectMs { get; internal set; }
    public long TlsHandshakeMs { get; internal set; }
    public long ConnectionOpenTotal => DnsLookupMs + TcpConnectMs + TlsHandshakeMs;

    // Request Phase
    public long RequestSendMs { get; internal set; }
    public long ServerWaitMs { get; internal set; } // Time between Request Sent and First Byte
    public long TimeToFirstByteMs { get; internal set; } // Points 1 through 6 combined

    public long TotalElapsed { get; internal set; }
}

public sealed class HttpMetricsScraper
{
    public static async Task<HttpMetrics<T>> MeasureRequestAsync<T>(string url) where T : new()
    {
        var metrics = new HttpMetrics<T>();
        var uri = new Uri(url);
        var totalSw = Stopwatch.StartNew();
        var stepSw = new Stopwatch();

        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                // 1. DNS Resolution
                stepSw.Restart();
                var _ = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
                metrics.DnsLookupMs = stepSw.ElapsedMilliseconds;

                // 2. TCP Connection
                stepSw.Restart();
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
                metrics.TcpConnectMs = stepSw.ElapsedMilliseconds;

                // Return raw TCP stream - SocketsHttpHandler will do TLS
                return new NetworkStream(socket, true);
            }
        };

        using var client = new HttpClient(handler);

        // Measure TLS + Request as combined
        stepSw.Restart();
        var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        var elapsed = stepSw.ElapsedMilliseconds;

        // TLS time = total time - (DNS + TCP)
        metrics.TlsHandshakeMs = totalSw.ElapsedMilliseconds - metrics.DnsLookupMs - metrics.TcpConnectMs - elapsed;
        metrics.RequestSendMs = elapsed;
        metrics.TotalElapsed = totalSw.ElapsedMilliseconds;

        var parsedString = await response.Content.ReadAsStringAsync();
        metrics.Response = JsonSerializer.Deserialize<T>(parsedString);
        return metrics;
    }
}