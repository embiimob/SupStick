using System.Globalization;

namespace SupStick.Converters
{
    /// <summary>
    /// Converter to convert boolean to background color for tab buttons
    /// </summary>
    public class BoolToTabBackgroundConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Color.FromArgb("#512BD4") : Color.FromArgb("#E0E0E0");
            }
            return Color.FromArgb("#E0E0E0");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
