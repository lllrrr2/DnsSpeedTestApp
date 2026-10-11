namespace DNSSpeedTester.Checks;

internal static partial class Program
{
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
