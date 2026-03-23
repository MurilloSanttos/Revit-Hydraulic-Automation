using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HidraulicoPlugin.UI.Converters;

/// <summary>
/// Converte status da etapa (string) para cor correspondente.
/// Valores: Pending, Running, Completed, Failed, WaitingApproval, RolledBack.
/// </summary>
public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "Pending";

        return status switch
        {
            "Pending" => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
            "Running" => new SolidColorBrush(Color.FromRgb(0x4A, 0x7C, 0xFF)),
            "Completed" => new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99)),
            "Failed" => new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)),
            "WaitingApproval" => new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24)),
            "Rejected" => new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)),
            "RolledBack" => new SolidColorBrush(Color.FromRgb(0xA7, 0x8B, 0xFA)),
            "Skipped" => new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x88)),
            _ => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
