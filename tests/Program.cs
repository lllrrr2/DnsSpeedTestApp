using DNSSpeedTester.Services;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            QueryRequestsRecursion();
            Console.WriteLine("All regression checks passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void QueryRequestsRecursion()
    {
        var query = DnsTestService.BuildDnsQuery("example.com", 1, 0x1234);
        Assert(query[0] == 0x12 && query[1] == 0x34, "Transaction ID must use network byte order.");
        Assert(query[2] == 0x01 && query[3] == 0x00, "Queries must request recursion.");
        Assert(query[4] == 0 && query[5] == 1, "Queries must contain one question.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
