using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using DNSSpeedTester.Models;
using DNSSpeedTester.Services;
using DNSSpeedTester.ViewModels;

namespace DNSSpeedTester.Checks;

internal sealed class TestWorkspace : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "DNSSpeedTester.Checks", Guid.NewGuid().ToString("N"));
    public DataPersistenceService Persistence { get; }

    public TestWorkspace()
    {
        Directory.CreateDirectory(Root);
        // Bypass constructors so checks never access the user's saved configuration.
        Persistence = (DataPersistenceService)RuntimeHelpers.GetUninitializedObject(typeof(DataPersistenceService));
        foreach (var (field, file) in new[]
                 {
                     ("_customDnsFilePath", "custom_dns.json"), ("_customDomainsFilePath", "custom_domains.json"),
                     ("_bootstrapDnsFilePath", "bootstrap_dns.json"), ("_appSettingsFilePath", "app_settings.json"),
                     ("_logFilePath", "error_logs.txt")
                 })
            SetField(Persistence, field, Path.Combine(Root, file));
        SetField(Persistence, "_jsonOptions", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public MainViewModel CreateViewModel()
    {
        var vm = (MainViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainViewModel));
        foreach (var field in typeof(MainViewModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            if (field.FieldType == typeof(string)) field.SetValue(vm, string.Empty);
        SetField(vm, "_themeMode", "System");
        SetField(vm, "_newDotPort", "853");
        SetField(vm, "_newDoqPort", "784");
        SetField(vm, "_dataPersistenceService", Persistence);
        SetField(vm, "_dnsTestService", new DnsTestService());
        SetField(vm, "_dnsSettingService", new DnsSettingService());
        SetField(vm, "<DnsServers>k__BackingField", new ObservableCollection<DnsServer>());
        SetField(vm, "<TestDomains>k__BackingField", new ObservableCollection<TestDomain>());
        SetField(vm, "<NetworkAdapters>k__BackingField", new ObservableCollection<NetworkAdapter>());
        return vm;
    }

    public static void SetField(object target, string name, object? value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
