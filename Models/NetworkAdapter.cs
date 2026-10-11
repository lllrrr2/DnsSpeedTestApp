using System.Net;

using CommunityToolkit.Mvvm.ComponentModel;

namespace DNSSpeedTester.Models;

public partial class NetworkAdapter : ObservableObject
{
    public NetworkAdapter(string id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsConnected { get; set; }
    [ObservableProperty] private bool _isDnsAutomatic;
    [ObservableProperty] private List<IPAddress> _dnsServers = new();

    public override string ToString()
    {
        return Description;
    }
}
