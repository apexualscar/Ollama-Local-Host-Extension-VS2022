using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OllamaLocalHostIntergration.Converters
{
    /// <summary>
    /// Converter to align messages based on IsUser property
    /// User messages: Right, AI messages: Left
    /// </summary>
    public class BoolToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
