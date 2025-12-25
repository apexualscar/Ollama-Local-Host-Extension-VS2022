using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OllamaLocalHostIntergration.Converters
{
    public class BoolToUserColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                // Use theme-compatible colors
                // These colors work well in both light and dark themes
                return isUser 
                    ? new SolidColorBrush(Color.FromRgb(0, 120, 215))  // Blue for user
                    : new SolidColorBrush(Color.FromRgb(76, 155, 215)); // Light blue for assistant
            }
            return new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Gray fallback
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
