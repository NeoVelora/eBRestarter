using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Services.Interfaces
{
    public interface INavigationFrame
    {
        #region PublicProperties

        bool CanGoBack { get; }
        object Content { get; }

        #endregion

        #region PublicMethods

        void GoBack();
        bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride);

        #endregion
    }
}
