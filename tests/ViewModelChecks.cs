namespace DNSSpeedTester.Checks;

internal static partial class Program
{
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
