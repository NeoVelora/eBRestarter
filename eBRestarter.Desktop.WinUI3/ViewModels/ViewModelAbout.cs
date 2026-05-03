using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Desktop.WinUI3.Models;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using System.Collections.ObjectModel;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public partial class ViewModelAbout : ObservableObject
{
    private readonly IAppInfoService _appInfoService;

    private readonly IIconCreditService _iconCreditService;

    private readonly ILocalizationService _localizationService;

    [ObservableProperty] public partial string AppVersion { get; set; }

    public ObservableCollection<IconCredit> IconCredits { get; } = [];

    public ViewModelAbout(
        IAppInfoService appInfoService,
        ILocalizationService localizationService,
        IIconCreditService iconCreditService)
    {
        _appInfoService = appInfoService;
        _localizationService = localizationService;
        _iconCreditService = iconCreditService;
        AppVersion = _localizationService.GetString("About_Loading");
        LoadVersionAndIconCredits();
    }

    private void LoadVersionAndIconCredits()
    {
        string prefix = _localizationService.GetString("About_VersionPrefix");
        AppVersion = $"{prefix} {_appInfoService.GetAppVersion()}";
        var credits = _iconCreditService.GetIconCredits();
        IconCredits.Clear();
        foreach (var iconCredit in credits)
            IconCredits.Add(iconCredit);
    }
}
