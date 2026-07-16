using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;

public interface INavigationFrame
{
    bool CanGoBack { get; }
    object Content { get; }

    void GoBack();
    bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride);

}
