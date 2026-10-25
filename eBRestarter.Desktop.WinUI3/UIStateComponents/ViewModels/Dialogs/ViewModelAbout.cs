using System;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Application;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Providers.Interfaces;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.DTOs.Presentation;

namespace eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Dialogs;

public sealed partial class ViewModelAbout : ObservableObject
{
    private const string KeyAboutLoading = "About_Loading";
    private const string KeyAboutVersionPrefix = "About_VersionPrefix";

    private readonly IOutboundPortAppVersionInfoProvider _appInfoProvider;
    private readonly IIconCreditProvider _iconCreditService;
    private readonly IInboundPortLocalizationProvider _localizationService;


    public ViewModelAbout(
        IOutboundPortAppVersionInfoProvider appInfoProvider,
        IIconCreditProvider iconCreditService,
        IInboundPortLocalizationProvider localizationService)
    {
        _appInfoProvider = appInfoProvider ?? throw new ArgumentNullException(nameof(appInfoProvider));
        _iconCreditService = iconCreditService ?? throw new ArgumentNullException(nameof(iconCreditService));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

        AppVersion = _localizationService.RetrieveString(KeyAboutLoading);
        LoadVersionAndIconCredits();
    }


    [ObservableProperty]
    public partial string AppVersion { get; set; }

    public ObservableCollection<IconCredit> IconCredits { get; } = [];


    private void LoadVersionAndIconCredits()
    {
        string prefix = _localizationService.RetrieveString(KeyAboutVersionPrefix);
        AppVersion = $"{prefix} {_appInfoProvider.RetrieveAppVersion()}";

        var credits = _iconCreditService.GetIconCredits();
        IconCredits.Clear();
        foreach (var iconCredit in credits)
        {
            IconCredits.Add(iconCredit);
        }
    }
}
