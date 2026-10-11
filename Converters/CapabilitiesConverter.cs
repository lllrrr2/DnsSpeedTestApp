using System.Globalization;
using System.Windows.Data;

namespace DNSSpeedTester.Converters;

/// <summary>
///     将 DnsServer 支持的加密协议汇总为紧凑标签（如 "DoH · DoT · DoQ"），
///     以便结果表格用单列展示协议能力，避免表格过宽。
/// </summary>
public class CapabilitiesConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var capabilities = new List<string>(3);
        if (values.Length > 0 && !string.IsNullOrWhiteSpace(values[0] as string)) capabilities.Add("DoH");
        if (values.Length > 1 && !string.IsNullOrWhiteSpace(values[1] as string)) capabilities.Add("DoT");
        if (values.Length > 2 && !string.IsNullOrWhiteSpace(values[2] as string)) capabilities.Add("DoQ");

        return capabilities.Count > 0 ? string.Join(" · ", capabilities) : "—";
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
