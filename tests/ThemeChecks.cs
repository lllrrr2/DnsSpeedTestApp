using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using DNSSpeedTester.Services;
using DNSSpeedTester.ViewModels;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static void SavedTheme()
    {
        EnsureApplication();
        using var fixture = new TestWorkspace();
        var vm = fixture.CreateViewModel();
        var dark = new CheckBox();
        dark.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(vm.IsDarkTheme)) { Source = vm, Mode = BindingMode.OneWay });
        var load = typeof(MainViewModel).GetMethod("LoadData", BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var mode in new[] { AppThemeMode.Dark, AppThemeMode.Light, AppThemeMode.System })
        {
            fixture.Persistence.SaveAppSettings(mode.ToString());
            load.Invoke(vm, null);
            Assert(vm.ThemeMode == mode.ToString() && ThemeService.CurrentMode == mode,
                "Loading a theme must synchronize selection and applied appearance.");
            Assert(dark.IsChecked == (mode == AppThemeMode.Dark), "Theme selection bindings must refresh when settings load.");
            vm.SetThemeCommand.Execute("System");
            Assert(vm.IsSystemTheme && ThemeService.CurrentMode == AppThemeMode.System,
                "Selecting System after loading a saved theme must apply it.");
        }
    }
}
