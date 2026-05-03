using Microsoft.UI.Xaml.Data;
using System;

namespace eBRestarter.Desktop.WinUI3.Converters;

public partial class TimerStateToTextConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? "Stop" : "Start";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();

}
