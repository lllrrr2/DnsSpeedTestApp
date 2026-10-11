using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DNSSpeedTester.Models;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static void CapabilityBindings()
    {
        var server = new DnsServer("Test", "127.0.0.1");
        var text = new TextBlock();
        var binding = new MultiBinding { Converter = new DNSSpeedTester.Converters.CapabilitiesConverter() };
        foreach (var property in new[] { nameof(server.DohUrl), nameof(server.DotHost), nameof(server.DoqHost) })
            binding.Bindings.Add(new Binding(property) { Source = server });
        text.SetBinding(TextBlock.TextProperty, binding);
        Assert(text.Text == "—", "Unconfigured endpoints must show no capabilities.");
        server.DohUrl = "https://example.com/dns-query";
        Assert(text.Text == "DoH", "Adding DoH must refresh the capability binding.");
        server.DotHost = "example.com";
        server.DoqHost = "example.com";
        Assert(text.Text == "DoH · DoT · DoQ", "Each endpoint must update the capability binding.");
        server.DohUrl = null;
        server.DotHost = " ";
        Assert(text.Text == "DoQ", "Cleared and whitespace-only endpoints must remove capabilities.");
        server.DoqHost = null;
        Assert(text.Text == "—", "Clearing all endpoints must clear all capabilities.");
    }

    private static void AdapterBindings()
    {
        var adapter = new NetworkAdapter("0", "Test", "Test")
        {
            IsDnsAutomatic = true,
            DnsServers = [IPAddress.Parse("1.1.1.1")]
        };
        var automatic = new CheckBox();
        automatic.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(adapter.IsDnsAutomatic)) { Source = adapter, Mode = BindingMode.OneWay });
        var addresses = new ItemsControl();
        addresses.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(adapter.DnsServers)) { Source = adapter });
        adapter.DnsServers = [IPAddress.Parse("8.8.8.8")];
        adapter.IsDnsAutomatic = false;
        Assert(automatic.IsChecked == false && Equals(addresses.Items[0], IPAddress.Parse("8.8.8.8")),
            "Setting DNS must refresh WPF address and automatic-mode bindings.");
        adapter.DnsServers = [];
        adapter.IsDnsAutomatic = true;
        Assert(automatic.IsChecked == true && addresses.Items.Count == 0, "Resetting DNS must refresh both bindings.");
    }
}
