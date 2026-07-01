using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Desktop.WinUI3.Models;
using eBRestarter.Desktop.WinUI3.Providers.Interfaces;
using System.Collections.ObjectModel;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelAbout : ObservableObject
{
    private readonly IAppInfoProviderOutboundPort _appInfoProvider;

    private readonly IIconCreditProvider _iconCreditService;

    private readonly ILocalizationProvider _localizationService;

    [ObservableProperty] public partial string AppVersion { get; set; }

    public ObservableCollection<IconCredit> IconCredits { get; } = [];

    public ViewModelAbout(
        IAppInfoProviderOutboundPort appInfoProvider,
        ILocalizationProvider LocalizationProvider,
        IIconCreditProvider iconCreditService)
    {
        _appInfoProvider = appInfoProvider;
        _localizationService = LocalizationProvider;
        _iconCreditService = iconCreditService;
        AppVersion = _localizationService.RetrieveString("About_Loading");
        LoadVersionAndIconCredits();
    }

    private void LoadVersionAndIconCredits()
    {
        string prefix = _localizationService.RetrieveString("About_VersionPrefix");
        AppVersion = $"{prefix} {_appInfoProvider.RetrieveAppVersion()}";
        var credits = _iconCreditService.GetIconCredits();
        IconCredits.Clear();
        foreach (var iconCredit in credits)
            IconCredits.Add(iconCredit);
    }
}


