using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Services.Interfaces
{
    public interface INavigationFrame
    {
        bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride);

        bool CanGoBack { get; }

        void GoBack();

        object Content { get; }
    }
}
