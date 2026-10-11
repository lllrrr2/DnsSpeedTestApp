using System.Net;
using System.Net.Sockets;
using System.Reflection;
using DNSSpeedTester.Services;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static async Task Ipv6Bootstrap()
    {
        using var udp = new UdpClient(new IPEndPoint(IPAddress.IPv6Loopback, 53));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
        var service = new DnsTestService { BootstrapDnsIp = IPAddress.IPv6Loopback };
        var method = typeof(DnsTestService).GetMethod("ResolveViaBootstrapDnsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var lookup = (Task<IPAddress?>)method.Invoke(service, ["example.com", AddressFamily.InterNetwork, timeout.Token])!;
        var request = await udp.ReceiveAsync(timeout.Token);
        var response = request.Buffer.Concat(new byte[]
        {
            0xc0, 0x0c, 0, 1, 0, 1, 0, 0, 0, 60, 0, 4, 192, 0, 2, 1
        }).ToArray();
        response[2] = 0x81;
        response[3] = 0x80;
        response[7] = 1;
        await udp.SendAsync(response, request.RemoteEndPoint, timeout.Token);
        Assert(Equals(await lookup, IPAddress.Parse("192.0.2.1")), "IPv6 Bootstrap must return the configured server's answer.");
    }
}
