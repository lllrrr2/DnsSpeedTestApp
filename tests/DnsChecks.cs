using System.Net;
using DNSSpeedTester.Models;
using DNSSpeedTester.Services;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static void DomainEncoding()
    {
        var plain = DnsTestService.BuildDnsQuery("example.com", 28, 123);
        var dotted = DnsTestService.BuildDnsQuery("example.com.", 28, 123);
        Assert(plain.SequenceEqual(dotted), "A trailing root dot must not change the question.");
        Assert(dotted.TakeLast(4).SequenceEqual(new byte[] { 0, 28, 0, 1 }), "QTYPE and QCLASS must follow the root terminator.");
        Assert(DnsTestService.BuildDnsQuery("例子.测试.", 1, 123).SequenceEqual(
            DnsTestService.BuildDnsQuery("xn--fsqu00a.xn--0zwm56d", 1, 123)), "International names must be encoded as IDNA.");
        foreach (var invalid in new[] { "example..com", new string('a', 64) + ".com", "." })
        {
            try { DnsTestService.BuildDnsQuery(invalid, 1); }
            catch (ArgumentException) { continue; }
            throw new InvalidOperationException($"Invalid domain accepted: {invalid}");
        }
    }

    private static async Task NegativeResponses()
    {
        var query = DnsTestService.BuildDnsQuery("missing.example.com", 1);
        Assert(!DnsTestService.IsValidDnsResponse(query, query), "A query echo is not a DNS response.");
        foreach (var code in new[] { 0, 2, 3, 5 })
        {
            var response = query.ToArray();
            response[2] = 0x81;
            response[3] = (byte)(0x80 | code);
            Assert(DnsTestService.IsValidDnsResponse(query, response) == (code is 0 or 3), "Only NoError and NXDOMAIN are usable responses.");
        }
        await using var doh = new LoopbackDoh(rcode: 3);
        var result = await new DnsTestService().TestDnsServerAsync(
            new DnsServer("Negative DoH", "127.0.0.1", dohUrl: doh.Url), "missing.example.com", DnsProtocol.DoH);
        Assert(result.Latency.HasValue, "A normal NXDOMAIN response must produce a DoH measurement.");
        await using var dns = new LoopbackDns(IPAddress.Parse("127.0.0.65"), rcode: 3);
        result = await new DnsTestService().TestDnsServerAsync(new DnsServer("Negative DNS", dns.Address.ToString()),
            "missing.example.com", DnsProtocol.UdpTcp);
        Assert(result.Latency.HasValue, "A normal NXDOMAIN response must produce a UDP/TCP measurement.");
    }

    private static async Task CancellationStopsQueries()
    {
        await using var dns = new LoopbackDns(IPAddress.Parse("127.0.0.64"), silentTcp: true);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        var result = await new DnsTestService().TestDnsServerAsync(new DnsServer("Cancel", dns.Address.ToString()),
            "example.com", DnsProtocol.UdpTcp, cts.Token).WaitAsync(TimeSpan.FromSeconds(2));
        Assert(result.Status == "已取消" && result.Latency is null, "Cancellation must complete promptly without a result.");
        var queries = (dns.TcpQueries, dns.UdpQueries);
        await Task.Delay(3500);
        Assert(queries == (dns.TcpQueries, dns.UdpQueries), "No queries may be sent after cancellation completes.");
    }

    private static async Task UdpSurvivesSilentTcp()
    {
        await using var dns = new LoopbackDns(IPAddress.Parse("127.0.0.63"), silentTcp: true);
        var result = await new DnsTestService().TestDnsServerAsync(new DnsServer("UDP only", dns.Address.ToString()),
            "example.com", DnsProtocol.UdpTcp).WaitAsync(TimeSpan.FromSeconds(16));
        Assert(dns.TcpQueries > 0 && dns.UdpQueries > 0, "Both transports must be attempted.");
        Assert(result.Latency.HasValue, "A silent TCP service must not discard working UDP results.");
    }

    private static async Task CompleteResponseTiming()
    {
        await using var doh = new LoopbackDoh(bodyDelay: TimeSpan.FromMilliseconds(400));
        var result = await new DnsTestService().TestDnsServerAsync(
            new DnsServer("Delayed body", "127.0.0.1", dohUrl: doh.Url), "example.com", DnsProtocol.DoH);
        Assert(result.Status == "成功" && result.Latency >= 350, "Latency must include the complete DNS response body.");
    }

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
