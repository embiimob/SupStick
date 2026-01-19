using System.Globalization;

namespace SupStick.Converters
{
    /// <summary>
    /// Converter to check if string is not empty
    /// </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                return !string.IsNullOrWhiteSpace(strValue);
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
