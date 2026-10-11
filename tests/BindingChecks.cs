using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DNSSpeedTester.Models;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static void AdapterBindings()
    {
        var adapter = new NetworkAdapter("0", "Test", "Test")
        {
            IsDhcpEnabled = true,
            DnsServers = [IPAddress.Parse("1.1.1.1")]
        };
        var automatic = new CheckBox();
        automatic.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(adapter.IsDhcpEnabled)) { Source = adapter, Mode = BindingMode.OneWay });
        var addresses = new ItemsControl();
        addresses.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(adapter.DnsServers)) { Source = adapter });
        adapter.DnsServers = [IPAddress.Parse("8.8.8.8")];
        adapter.IsDhcpEnabled = false;
        Assert(automatic.IsChecked == false && Equals(addresses.Items[0], IPAddress.Parse("8.8.8.8")),
            "Setting DNS must refresh WPF address and automatic-mode bindings.");
        adapter.DnsServers = [];
        adapter.IsDhcpEnabled = true;
        Assert(automatic.IsChecked == true && addresses.Items.Count == 0, "Resetting DNS must refresh both bindings.");
    }
}
