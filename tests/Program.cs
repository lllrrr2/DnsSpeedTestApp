using DNSSpeedTester.Services;
using System.Windows.Threading;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            (string Name, Action Run)[] checks =
            [
                ("query-flags", QueryRequestsRecursion),
                ("failed-dns", () => RunAsync(RejectFailedDns)),
                ("consecutive-additions", ConsecutiveAdditions),
                ("busy-editing", BusyEditing),
                ("complete-response-timing", () => RunAsync(CompleteResponseTiming)),
                ("udp-fallback", () => RunAsync(UdpSurvivesSilentTcp)),
                ("cancellation", () => RunAsync(CancellationStopsQueries)),
                ("negative-responses", () => RunAsync(NegativeResponses)),
                ("domain-encoding", DomainEncoding),
                ("ipv6-bootstrap", () => RunAsync(Ipv6Bootstrap))
            ];
            var selected = checks.Where(check => args.Length == 0 || args.Contains(check.Name)).ToArray();
            Assert(selected.Length > 0, "No matching checks.");
            foreach (var check in selected)
            {
                check.Run();
                Console.WriteLine($"PASS {check.Name}");
            }
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

    private static void RunAsync(Func<Task> check)
    {
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        var task = check();
        var frame = new DispatcherFrame();
        var dispatcher = Dispatcher.CurrentDispatcher;
        _ = task.ContinueWith(_ => dispatcher.BeginInvoke(() => frame.Continue = false), TaskScheduler.Default);
        Dispatcher.PushFrame(frame);
        task.GetAwaiter().GetResult();
    }
}
