using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace OmniClip.App.Converters;

public class ContentTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var type = value?.ToString()?.ToLowerInvariant();
        return type switch
        {
            "code" => new SolidColorBrush(Color.FromRgb(0x97, 0x47, 0x00)),   // tertiary
            "url" => new SolidColorBrush(Color.FromRgb(0x00, 0x5F, 0xAA)),     // primary
            "image" => new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),    // green
            "file" => new SolidColorBrush(Color.FromRgb(0x8E, 0x24, 0xAA)),     // purple
            _ => new SolidColorBrush(Color.FromRgb(0x5D, 0x5F, 0x5F)),         // secondary = text
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
            "code" => "",    // code icon
            "url" => "",     // link icon
            "image" => "",   // image icon
            "file" => "",    // attach/file icon
            _ => "",        // document/text icon
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
