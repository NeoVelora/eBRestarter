using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace eBRestarter.Desktop.WinUI3.Converters;

public partial class BoolToImageConverter : IValueConverter
{
    #region PublicProperties

    public string? ImagePathWhenFalse { get; set; }
    public string? ImagePathWhenTrue { get; set; }

    #endregion

    #region PublicMethods

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool flag = value is bool b && b;
        string? path = flag ? ImagePathWhenTrue : ImagePathWhenFalse;
        if (string.IsNullOrEmpty(path))
            return null!;
        return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    #endregion
}
