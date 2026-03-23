using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace HidraulicoPlugin.UI.Converters;

/// <summary>
/// Converte bool → Visibility (true = Visible, false = Collapsed).
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;

        // Se parameter = "Invert", inverte a lógica
        if (parameter is string param && param == "Invert")
            boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}
