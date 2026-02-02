using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace AionDpsMeter.UI.Converters
{
    /// <summary>
    /// Converts a string path to an ImageSource, returning null if the path is null/empty or the image cannot be loaded.
    /// This prevents binding errors when images are missing or paths are invalid.
    /// </summary>
    public class SafeImageSourceConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                // Build the pack URI for WPF resources in the UI assembly
                // The path should be like "/Assets/Classes/assassin.png"
                var uriString = path.StartsWith("/") 
                    ? $"pack://application:,,,{path}" 
                    : $"pack://application:,,,/{path}";

                var uri = new Uri(uriString, UriKind.Absolute);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();

                if (bitmap.CanFreeze)
                {
                    bitmap.Freeze();
                }

                return bitmap;
            }
            catch
            {
                // Return null for any loading errors - the image will simply not display
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
