using System.Globalization;

namespace SupStick.Converters
{
    /// <summary>
    /// Converter to convert boolean to text color for tab buttons
    /// </summary>
    public class BoolToTabTextColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Colors.White : Colors.Black;
            }
            return Colors.Black;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
