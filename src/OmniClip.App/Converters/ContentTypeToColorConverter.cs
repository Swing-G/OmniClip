using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace OmniClip.App.Converters;

public class ContentTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var type = value?.ToString()?.ToLowerInvariant();
        return type switch
        {
            "code" => new SolidColorBrush(Color.FromRgb(0x00, 0x5F, 0xAA)),   // action blue
            "url" => new SolidColorBrush(Color.FromRgb(0x97, 0x47, 0x00)),    // tertiary brown
            "image" => new SolidColorBrush(Color.FromRgb(0x5D, 0x5F, 0x5F)),  // neutral
            "file" => new SolidColorBrush(Color.FromRgb(0x5D, 0x5F, 0x5F)),   // neutral
            _ => new SolidColorBrush(Color.FromRgb(0x5D, 0x5F, 0x5F)),         // text
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ContentTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var type = value?.ToString()?.ToLowerInvariant();
        return type switch
        {
            "code" => "",    // code brackets
            "url" => "",     // link chain
            "image" => "",   // image outline
            "file" => "",    // attachment
            _ => "",        // plain text lines
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class FilePathToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var path = value as string;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return System.Windows.Data.Binding.DoNothing;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 128;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
