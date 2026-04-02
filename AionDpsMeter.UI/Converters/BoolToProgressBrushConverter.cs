using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AionDpsMeter.UI.Converters
{
    /// <summary>
    /// Returns the user progress-bar gradient brush when the value is <c>true</c>,
    /// otherwise returns the default green gradient brush.
    /// </summary>
    public sealed class BoolToProgressBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isUser = value is true;
            string key = isUser ? "ProgressBarUserGradientBrush" : "ProgressBarGradientBrush";

            if (Application.Current.Resources[key] is Brush brush)
                return brush;

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}
