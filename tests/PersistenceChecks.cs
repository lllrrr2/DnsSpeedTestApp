using System.IO;

namespace DNSSpeedTester.Checks;

internal static partial class Program
{
    private static void BackupRecovery()
    {
        using var fixture = new TestWorkspace();
        var file = Path.Combine(fixture.Root, "app_settings.json");
        fixture.Persistence.SaveAppSettings("Dark");
        fixture.Persistence.SaveAppSettings("Light");
        Assert(fixture.Persistence.LoadThemeMode() == "Light", "Replacing configuration must publish the new value.");
        Assert(File.ReadAllText(file + ".bak").Contains("Dark"), "Atomic replacement must retain the previous configuration.");
        Assert(!File.Exists(file + ".tmp"), "Successful replacement must consume the temporary file.");
        foreach (var broken in new string?[] { null, "", "  ", "null", "{broken" })
        {
            if (broken is null) File.Delete(file);
            else File.WriteAllText(file, broken);
            Assert(fixture.Persistence.LoadThemeMode() == "Dark", "Missing, empty, null and corrupt configurations must load the backup.");
        }
        File.WriteAllText(file + ".bak", "{broken");
        Assert(fixture.Persistence.LoadThemeMode() is null, "An invalid backup must fail gracefully.");
        File.Delete(file);
        fixture.Persistence.SaveAppSettings("System");
        Assert(fixture.Persistence.LoadThemeMode() == "System", "Saving must also work when only a backup exists.");
    }
}
