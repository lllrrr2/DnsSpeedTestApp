using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using DNSSpeedTester.Services;

namespace DNSSpeedTester.Checks;

internal sealed class LoopbackDns : IAsyncDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly UdpClient _udp;
    private readonly TcpListener _tcp;
    private readonly Task _udpLoop;
    private readonly Task _tcpLoop;
    private readonly ConcurrentBag<Task> _clients = [];
    private readonly int _rcode;
    private readonly bool _silentTcp;
    private int _tcpQueries;
    private int _udpQueries;

    public IPAddress Address { get; }
    public int TcpQueries => Volatile.Read(ref _tcpQueries);
    public int UdpQueries => Volatile.Read(ref _udpQueries);

    public LoopbackDns(IPAddress address, int rcode = 0, bool silentTcp = false)
    {
        Address = address;
        _rcode = rcode;
        _silentTcp = silentTcp;
        _udp = new UdpClient(new IPEndPoint(address, 53));
        _tcp = new TcpListener(address, 53);
        _tcp.Start();
        _udpLoop = Task.Run(ServeUdp);
        _tcpLoop = Task.Run(AcceptTcp);
    }

    private byte[] Respond(byte[] query)
    {
        var response = query.ToArray();
        response[2] = 0x81;
        response[3] = (byte)(0x80 | _rcode);
        Array.Clear(response, 6, 6);
        return response;
    }

    private async Task ServeUdp()
    {
        try
        {
            while (true)
            {
                var request = await _udp.ReceiveAsync(_stop.Token);
                Interlocked.Increment(ref _udpQueries);
                await _udp.SendAsync(Respond(request.Buffer), request.RemoteEndPoint, _stop.Token);
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    private async Task AcceptTcp()
    {
        try
        {
            while (true)
            {
                var client = await _tcp.AcceptTcpClientAsync(_stop.Token);
                _clients.Add(ServeTcp(client));
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    private async Task ServeTcp(TcpClient client)
    {
        using (client)
        {
            try
            {
                while (true)
                {
                    var query = await DnsTestService.ReadTcpPrefixedMessageAsync(client.GetStream(), _stop.Token);
                    Interlocked.Increment(ref _tcpQueries);
                    if (_silentTcp) await Task.Delay(Timeout.Infinite, _stop.Token);
                    await client.GetStream().WriteAsync(DnsTestService.AddTcpLengthPrefix(Respond(query)), _stop.Token);
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
            catch (IOException) { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await Task.WhenAll(_tcpLoop, _udpLoop);
        await Task.WhenAll(_clients);
        _tcp.Stop();
        _udp.Dispose();
        _stop.Dispose();
    }
}
