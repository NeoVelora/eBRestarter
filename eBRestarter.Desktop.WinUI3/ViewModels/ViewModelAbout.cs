using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Desktop.WinUI3.Models;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using System.Collections.ObjectModel;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public partial class ViewModelAbout : ObservableObject
{
    private readonly IAppInfoUseCase _appInfoUseCase;

    private readonly IIconCreditHandler _iconCreditService;

    private readonly ILocalizationService _localizationService;

    [ObservableProperty] public partial string AppVersion { get; set; }

    public ObservableCollection<IconCredit> IconCredits { get; } = [];

    public ViewModelAbout(
        IAppInfoUseCase AppInfoAdapter,
        ILocalizationService localizationService,
        IIconCreditHandler iconCreditService)
    {
        _appInfoUseCase = AppInfoAdapter;
        _localizationService = localizationService;
        _iconCreditService = iconCreditService;
        AppVersion = _localizationService.RetrieveString("About_Loading");
        LoadVersionAndIconCredits();
    }

    private void LoadVersionAndIconCredits()
    {
        string prefix = _localizationService.RetrieveString("About_VersionPrefix");
        AppVersion = $"{prefix} {_appInfoUseCase.RetrieveAppVersion()}";
        var credits = _iconCreditService.GetIconCredits();
        IconCredits.Clear();
        foreach (var iconCredit in credits)
            IconCredits.Add(iconCredit);
    }
}

