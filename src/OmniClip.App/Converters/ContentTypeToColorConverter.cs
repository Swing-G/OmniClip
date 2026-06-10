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
            "code" => new SolidColorBrush(Color.FromRgb(0x00, 0x5F, 0xAA)),   // blue
            "url" => new SolidColorBrush(Color.FromRgb(0x97, 0x47, 0x00)),    // amber/brown
            "image" => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),  // green
            "file" => new SolidColorBrush(Color.FromRgb(0x8E, 0x24, 0xAA)),   // purple
            _ => new SolidColorBrush(Color.FromRgb(0x5D, 0x5F, 0x5F)),         // gray
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

public class FileNameToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var name = value as string ?? "";
        var ext = Path.GetExtension(name).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "",           // PDF
            ".doc" or ".docx" => "", // Word
            ".xls" or ".xlsx" => "", // Excel
            ".ppt" or ".pptx" => "", // PowerPoint
            ".txt" or ".md" or ".json" or ".xml" or ".csv" or ".log" => "", // text
            ".mp4" or ".avi" or ".mov" or ".mkv" or ".wmv" or ".webm" => "", // video
            ".mp3" or ".wav" or ".flac" or ".aac" or ".ogg" => "", // audio
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "",   // archive
            ".exe" or ".msi" => "",   // app
            ".html" or ".htm" => "",  // HTML
            _ => "",                  // default file
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
