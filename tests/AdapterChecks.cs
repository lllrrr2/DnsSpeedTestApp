using System.Reflection;
using DNSSpeedTester.Services;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static void DnsConfigSource()
    {
        var method = typeof(DnsSettingService).GetMethod("IsAutomaticDns", BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var (ipv4, ipv6, automatic) in new (string?, string?, bool)[]
        {
            (null, null, true), ("", " ", true), ("8.8.8.8", null, false),
            (null, "2001:4860:4860::8888", false), ("1.1.1.1", "2606:4700:4700::1111", false)
        })
            Assert((bool)method.Invoke(null, [ipv4, ipv6])! == automatic,
                "Automatic DNS must depend on manual DNS configuration in both address families, independently of IP DHCP.");
        // Reading actual adapters must not change the machine's configuration.
        var adapters = new DnsSettingService().GetNetworkAdaptersSafe(out var error);
        Assert(error is null && adapters.Count > 0, "The DNS-source lookup must work for real local adapters.");
    }
}
