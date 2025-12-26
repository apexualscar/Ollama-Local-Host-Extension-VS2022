using System;
using System.Globalization;
using System.Windows.Data;

namespace OllamaLocalHostIntergration.Converters
{
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                // Use Segoe MDL2 Assets icon codes
                return isUser ? "\uE77B" : "\uE8AD";  // Person icon : Robot icon
            }
            return "\u25CF";  // Bullet point
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
