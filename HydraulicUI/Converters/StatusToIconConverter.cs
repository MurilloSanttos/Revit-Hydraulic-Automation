using System.Globalization;
using System.Windows.Data;

namespace HidraulicoPlugin.UI.Converters;

/// <summary>
/// Converte status para ícone emoji correspondente.
/// </summary>
public class StatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "Pending";

        return status switch
        {
            "Pending" => "⚪",
            "Running" => "🔄",
            "Completed" => "✅",
            "Failed" => "❌",
            "WaitingApproval" => "⏸",
            "Rejected" => "👎",
            "RolledBack" => "⏪",
            "Skipped" => "⏭",
            _ => "❓"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
