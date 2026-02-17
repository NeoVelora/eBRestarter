using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI;

namespace eBRestarter.Desktop.WinUI3.Converters
{
    public class TimerStateToColorConverter : IValueConverter
    {
        #region PublicMethods

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
            {
                var color = ColorHelper.FromArgb(179, 12, 46, 0);
                return new SolidColorBrush(color);
            }
            var startColor = ColorHelper.FromArgb(13, 178, 144, 0);
            return new SolidColorBrush(startColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();

        #endregion
    }
}
