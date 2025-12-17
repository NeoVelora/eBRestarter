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
        private readonly Frame _frame;

        public WinUIFrameAdapter(Frame frame) => _frame = frame;

        public bool CanGoBack => _frame.CanGoBack;

        public object Content => _frame.Content;

        public void GoBack() => _frame.GoBack();

        public bool Navigate(Type sourcePageType, object parameter, NavigationTransitionInfo infoOverride)
        {
            return _frame.Navigate(sourcePageType, parameter, infoOverride);
        }
    }
}
