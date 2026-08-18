using System;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace eBRestarter.Desktop.WinUI3.Converters;

public sealed class BoolToImageConverter : IValueConverter
{
    public string? ImagePathWhenFalse { get; set; }

    public string? ImagePathWhenTrue { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isTrue = value is bool boolValue && boolValue;
        string? imagePath = isTrue ? ImagePathWhenTrue : ImagePathWhenFalse;

        if (string.IsNullOrEmpty(imagePath))
        {
            return null!;
        }

        return new BitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
