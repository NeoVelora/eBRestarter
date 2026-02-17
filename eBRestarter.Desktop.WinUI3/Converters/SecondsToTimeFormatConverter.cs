using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Converters
{
    public class SecondsToTimeFormatConverter : IValueConverter
    {
        #region PublicMethods

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int seconds)
            {
                if (seconds < 0) return "00:00:00";
                TimeSpan time = TimeSpan.FromSeconds(seconds);
                return FormatTimeSpan(time);
            }
            if (value is TimeSpan timeSpan)
                return FormatTimeSpan(timeSpan);
            return "00:00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region PrivateHelperMethods

        private string FormatTimeSpan(TimeSpan time)
        {
            return time.ToString(@"hh\:mm\:ss");
        }

        #endregion
    }
}
