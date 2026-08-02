using System;
using System.Collections.Generic;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralUIComponents.Navigation;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Services;

public sealed class NavigationService : INavigationService
{
    private INavigationFrame? _frameAdapter;

    private readonly Dictionary<string, Type> _pages = [];


    public void AttachFrame(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        _frameAdapter = new WinUIFrameNavigator(frame);
    }

    public bool NavigateTo(
        string key,
        object parameter = null!,
        NavigationTransitionInfo transitionInfo = null!)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (_frameAdapter is null)
        {
            return false;
        }

        if (!_pages.TryGetValue(key, out var pageType))
        {
            return false;
        }

        if (_frameAdapter.Content?.GetType() == pageType)
        {
            return false;
        }

        return _frameAdapter.Navigate(pageType, parameter!, transitionInfo!);
    }

    public void RegisterRoute(string key, Type pageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(pageType);

        _pages.TryAdd(key, pageType);
    }

    internal void SetFrameAdapter(INavigationFrame frameAdapter)
    {
        ArgumentNullException.ThrowIfNull(frameAdapter);
        _frameAdapter = frameAdapter;
    }
}
