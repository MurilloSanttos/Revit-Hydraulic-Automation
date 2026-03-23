using System.Globalization;
using System.Windows.Data;

namespace HidraulicoPlugin.UI.Converters;

/// <summary>
/// Converte porcentagem (0-100) + largura do container → largura em pixels.
/// Usado na barra de progresso.
/// </summary>
public class PercentToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return 0.0;

        double percent = values[0] is double p ? p : 0;
        double containerWidth = values[1] is double w ? w : 0;

        if (containerWidth <= 0 || percent <= 0) return 0.0;

        return Math.Max(0, Math.Min(containerWidth, containerWidth * percent / 100.0));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
