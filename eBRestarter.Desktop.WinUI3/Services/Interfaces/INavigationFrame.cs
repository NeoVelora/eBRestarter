using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace eBRestarter.Desktop.WinUI3.Services.Interfaces;

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
