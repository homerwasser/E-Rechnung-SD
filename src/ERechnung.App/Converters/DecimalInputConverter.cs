using System.Globalization;
using System.Windows.Data;

namespace ERechnung.App.Converters;

public sealed class DecimalInputConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is decimal decimalValue
            ? decimalValue.ToString("G29", culture)
            : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString();
        if (decimal.TryParse(text, NumberStyles.Number, culture, out var result))
        {
            return result;
        }

        throw new FormatException(
            $"„{text}“ ist keine gültige Dezimalzahl. Beispiel: {12.5m.ToString("0.0", culture)}");
    }
}
