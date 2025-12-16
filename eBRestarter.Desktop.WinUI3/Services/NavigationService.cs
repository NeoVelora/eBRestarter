using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.Services
{
    public sealed class NavigationService : INavigationService
    {
        private readonly Dictionary<string, Type> _pages = new();

        private Frame? _frame;

        public void AttachFrame(Frame frame) => _frame = frame;

        public bool CanGoBack => _frame?.CanGoBack ?? false;

        public bool Navigate<TPage>(object? parameter = null, NavigationTransitionInfo? infoOverride = null) where TPage : Page
        {
            infoOverride = new DrillInNavigationTransitionInfo();

            if (_frame is null) return false;

            if (infoOverride is null)
            {
                return _frame.Navigate(typeof(TPage), parameter);
            }

            return _frame.Navigate(typeof(TPage), parameter, infoOverride);
        }

        public bool Navigate( Type pageType, object? parameter = null, NavigationTransitionInfo? infoOverride = null)
        {
            infoOverride = new DrillInNavigationTransitionInfo();

            if (_frame is null) return false;

            if (infoOverride is null)
            {
                return _frame.Navigate(pageType, parameter);
            }

            return _frame.Navigate(pageType, parameter, infoOverride);
        }

        public bool NavigateTo(string key, object parameter = null, NavigationTransitionInfo transitionInfo = null)
        {
            if (_frame == null) return false;

            if (_pages.TryGetValue(key, out var pageType))
            {
                if (_frame.Content?.GetType() == pageType) return false;

                return _frame.Navigate(pageType, parameter, transitionInfo);
            }

            return false;
        }

        public void RegisterRoute(string key, Type pageType)
        {
            if (!_pages.ContainsKey(key))
            {
                _pages.Add(key, pageType);
            }
        }

        public bool GoBack()
        {
            if (CanGoBack)
            {
                _frame.GoBack();
                return true;
            }
            return false;
        }
    }
}
