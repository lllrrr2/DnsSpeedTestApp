using System.Net;
using DNSSpeedTester.Models;
using DNSSpeedTester.Services;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static async Task RejectFailedDns()
    {
        await using var refused = new LoopbackDns(IPAddress.Parse("127.0.0.61"), rcode: 5);
        var service = new DnsTestService();
        var result = await service.TestDnsServerAsync(new DnsServer("Refused", refused.Address.ToString()),
            "example.com", DnsProtocol.UdpTcp);
        Assert(result.Latency is null, "A DNS server returning REFUSED must not be ranked.");
        result = await service.TestDnsServerAsync(new DnsServer("No listener", "127.0.0.62"),
            "example.com", DnsProtocol.UdpTcp);
        Assert(result.Latency is null, "A pingable address without DNS must not be ranked.");
    }
}
