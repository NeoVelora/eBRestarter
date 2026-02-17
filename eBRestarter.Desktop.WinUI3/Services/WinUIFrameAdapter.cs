using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Services
{
    public class WinUIFrameAdapter : INavigationFrame
    {
        #region FieldsAndInjectedServices

        private readonly Frame _frame;

        #endregion

        #region ConstructorAndFinalizer

        public WinUIFrameAdapter(Frame frame) => _frame = frame;

        #endregion

        #region PublicProperties

        public bool CanGoBack => _frame.CanGoBack;
        public object Content => _frame.Content;

        #endregion

        #region PublicMethods

        public void GoBack() => _frame.GoBack();

        public bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride)
        {
            return _frame.Navigate(sourcePageType, parameter, infoOverride);
        }

        #endregion
    }
}
