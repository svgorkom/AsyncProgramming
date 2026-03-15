using System.Globalization;
using System.Windows.Data;

namespace AsynAwaitExamples.Converters;

/// <summary>
/// Converts a boolean value to its inverse. Used in XAML bindings where
/// IsEnabled should be the opposite of a ViewModel bool property.
/// </summary>
public sealed class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b ? !b : value;
    }
}
