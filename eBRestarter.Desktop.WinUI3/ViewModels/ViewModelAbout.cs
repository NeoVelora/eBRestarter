using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Desktop.WinUI3.Models;
using eBRestarter.Desktop.WinUI3.Providers.Interfaces;
using System.Collections.ObjectModel;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Application;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelAbout : ObservableObject
{
    private readonly IOutboundPortAppVersionInfoProvider _appInfoProvider;

    private readonly IIconCreditProvider _iconCreditService;

    private readonly IInboundPortLocalizationProvider _localizationService;

    [ObservableProperty] public partial string AppVersion { get; set; }

    public ObservableCollection<IconCredit> IconCredits { get; } = [];

    public ViewModelAbout(
        IOutboundPortAppVersionInfoProvider appInfoProvider,
        IInboundPortLocalizationProvider LocalizationProvider,
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


