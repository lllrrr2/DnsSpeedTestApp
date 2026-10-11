namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static void SaveFailures()
    {
        using var fixture = new TestWorkspace();
        var vm = fixture.CreateViewModel();
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(fixture.Root, "custom_dns.json"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(fixture.Root, "custom_domains.json"));
        vm.NewDnsName = "Unsaved";
        vm.NewPrimaryDns = "127.0.0.1";
        vm.AddCustomDnsCommand.Execute(null);
        Assert(vm.StatusMessage.Contains("失败") && vm.StatusMessage.Contains("未保存"), "A failed DNS save must not report success.");
        vm.NewTestDomainName = "Unsaved";
        vm.NewTestDomainValue = "example.com";
        vm.AddTestDomainCommand.Execute(null);
        Assert(vm.StatusMessage.Contains("失败") && vm.StatusMessage.Contains("未保存"), "A failed domain save must not report success.");
        vm.SelectedDnsServer = null;
        vm.SelectedDnsServer = vm.DnsServers[0];
        vm.NewDnsName = "Edited";
        vm.AddCustomDnsCommand.Execute(null);
        Assert(vm.StatusMessage.Contains("失败"), "Editing DNS must propagate save failures.");
        vm.SelectedTestDomain = null;
        vm.SelectedTestDomain = vm.TestDomains[0];
        vm.NewTestDomainName = "Edited";
        vm.AddTestDomainCommand.Execute(null);
        Assert(vm.StatusMessage.Contains("失败"), "Editing domains must propagate save failures.");
    }

    private static async Task ResultProtocol()
    {
        EnsureApplication();
        using var fixture = new TestWorkspace();
        var vm = fixture.CreateViewModel();
        await using var doh = new LoopbackDoh(bodyDelay: TimeSpan.FromMilliseconds(100));
        vm.DnsServers.Add(new DNSSpeedTester.Models.DnsServer("DoH", "127.0.0.1", dohUrl: doh.Url));
        vm.SelectedTestDomain = new DNSSpeedTester.Models.TestDomain("Example", "example.com", "Test");
        vm.SelectedProtocol = DNSSpeedTester.Models.DnsProtocol.DoH;
        var test = vm.StartTestCommand.ExecuteAsync(null);
        vm.SelectedProtocol = DNSSpeedTester.Models.DnsProtocol.DoT;
        await test;
        Assert(vm.StatusMessage.Contains("协议: DoH"), "Completion must name the tested protocol.");
        Assert(vm.DisplayedProtocol == DNSSpeedTester.Models.DnsProtocol.DoH && vm.DnsServers[0].Latency.HasValue,
            "Endpoint columns must retain the result protocol even if selection changes during testing.");
        vm.SelectedProtocol = DNSSpeedTester.Models.DnsProtocol.UdpTcp;
        Assert(vm.DnsServers[0].Latency is null && !vm.DnsServers[0].IsFastest && vm.IsAddressColumnVisible,
            "Changing protocol after testing must clear old measurements and update columns.");
    }

    private static void BusyEditing()
    {
        using var fixture = new TestWorkspace();
        var vm = fixture.CreateViewModel();
        var server = new DNSSpeedTester.Models.DnsServer("Original", "127.0.0.1", isCustom: true);
        vm.DnsServers.Add(server);
        vm.SelectedDnsServer = server;
        vm.NewDnsName = "Changed";
        vm.NewTestDomainName = "New domain";
        vm.NewTestDomainValue = "example.com";
        vm.IsBusy = true;
        Assert(!vm.AddCustomDnsCommand.CanExecute(null), "DNS editing must be disabled while busy.");
        Assert(!vm.AddTestDomainCommand.CanExecute(null), "Domain editing must be disabled while busy.");
        vm.AddCustomDnsCommand.Execute(null);
        Assert(server.Name == "Original", "Even a direct command call must not alter an active test target.");
        vm.IsBusy = false;
        Assert(vm.AddCustomDnsCommand.CanExecute(null), "Editing must be available again after testing.");
    }

    private static void ConsecutiveAdditions()
    {
        using var fixture = new TestWorkspace();
        var vm = fixture.CreateViewModel();
        foreach (var name in new[] { "First", "Second" })
        {
            vm.NewDnsName = name;
            vm.NewPrimaryDns = "127.0.0.1";
            vm.AddCustomDnsCommand.Execute(null);
            vm.NewTestDomainName = name;
            vm.NewTestDomainValue = $"{name.ToLowerInvariant()}.example.com";
            vm.AddTestDomainCommand.Execute(null);
            Assert(!vm.IsEditingDns && !vm.IsEditingTestDomain, "Adding must leave an empty add form.");
        }
        Assert(vm.DnsServers.Count == 2 && vm.DnsServers[0].Name == "First", "Adding DNS must preserve the previous item.");
        Assert(vm.TestDomains.Count == 2 && vm.TestDomains[0].Name == "First", "Adding a domain must preserve the previous item.");
        Assert(fixture.Persistence.LoadCustomDnsServers().Count == 2, "Both DNS entries must be persisted.");
        Assert(fixture.Persistence.LoadCustomTestDomains().Count == 2, "Both domain entries must be persisted.");
    }
}
