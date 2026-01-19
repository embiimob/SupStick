using System.Globalization;

namespace SupStick.Converters
{
    /// <summary>
    /// Converter to convert boolean to color for button states
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Colors.Blue : Colors.Gray;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
