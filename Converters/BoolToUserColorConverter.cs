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
                return isUser 
                    ? new SolidColorBrush(Color.FromRgb(0, 120, 215))  // Blue for user
                    : new SolidColorBrush(Color.FromRgb(16, 110, 190)); // Darker blue for assistant
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
