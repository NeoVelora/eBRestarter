using System;

using Microsoft.UI.Xaml.Data;

namespace eBRestarter.Desktop.WinUI3.Converters;

public sealed class SecondsToTimeFormatConverter : IValueConverter
{
    private const string DefaultTimeFormat = "00:00:00";
    private const string TimeSpanFormatPattern = @"hh\:mm\:ss";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is int seconds)
        {
            if (seconds < 0)
            {
                return DefaultTimeFormat;
            }

            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return FormatTimeSpan(time);
        }

        if (value is TimeSpan timeSpan)
        {
            return FormatTimeSpan(timeSpan);
        }

        return DefaultTimeFormat;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static string FormatTimeSpan(TimeSpan time)
    {
        return time.ToString(TimeSpanFormatPattern);
    }
}
