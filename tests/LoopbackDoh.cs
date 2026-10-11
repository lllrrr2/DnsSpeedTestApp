using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DNSSpeedTester.Checks;

internal sealed class LoopbackDoh : IAsyncDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly ConcurrentBag<Task> _clients = [];
    private readonly Task _acceptLoop;
    private readonly int _rcode;
    private readonly TimeSpan _bodyDelay;
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource RequestReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public string Url { get; }

    public LoopbackDoh(int rcode = 0, TimeSpan bodyDelay = default, bool holdResponse = false)
    {
        _rcode = rcode;
        _bodyDelay = bodyDelay;
        if (!holdResponse) _release.SetResult();
        _listener.Start();
        Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/dns-query";
        _acceptLoop = Task.Run(Accept);
    }

    public void ReleaseResponse() => _release.TrySetResult();

    private async Task Accept()
    {
        try
        {
            while (true)
                _clients.Add(Respond(await _listener.AcceptTcpClientAsync(_stop.Token)));
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    private async Task Respond(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var header = new List<byte>();
                var one = new byte[1];
                do
                {
                    await stream.ReadExactlyAsync(one, _stop.Token);
                    header.Add(one[0]);
                    if (header.Count > 16384) throw new InvalidDataException("HTTP header too large.");
                } while (!header.TakeLast(4).SequenceEqual(new byte[] { 13, 10, 13, 10 }));
                var length = int.Parse(Encoding.ASCII.GetString(header.ToArray()).Split("\r\n")
                    .First(line => line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)).Split(':')[1]);
                var query = new byte[length];
                await stream.ReadExactlyAsync(query, _stop.Token);
                RequestReceived.TrySetResult();
                query[2] = 0x81;
                query[3] = (byte)(0x80 | _rcode);
                Array.Clear(query, 6, 6);
                var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/dns-message\r\nContent-Length: {query.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(headers, _stop.Token);
                await _release.Task.WaitAsync(_stop.Token);
                await Task.Delay(_bodyDelay, _stop.Token);
                await stream.WriteAsync(query, _stop.Token);
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
            catch (IOException) { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await _acceptLoop;
        await Task.WhenAll(_clients);
        _listener.Stop();
        _stop.Dispose();
    }
}
