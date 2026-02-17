using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Converters
{
    public class TimerStateToTextConverter : IValueConverter
    {
        #region PublicMethods

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? "Stop" : "Start";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();

        #endregion
    }
}
