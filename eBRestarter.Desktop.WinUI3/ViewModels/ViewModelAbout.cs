using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Desktop.WinUI3.Models;
using eBRestarter.Desktop.WinUI3.Models.Constants;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using System.Collections.ObjectModel;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelAbout : ObservableObject
    {
        #region FieldsAndInjectedServices

        private readonly IAppInfoService _appInfoService;
        private readonly ILocalizationService _localizationService;
        private readonly IIconCreditService _iconCreditService;

        #endregion

        #region ObservableProperties

        [ObservableProperty] public partial string AppVersion { get; set; }

        #endregion

        #region PublicProperties

        public ObservableCollection<IconCredit> IconCredits { get; } = [];

        #endregion

        #region ConstructorAndFinalizer

        public ViewModelAbout(
            IAppInfoService appInfoService,
            ILocalizationService localizationService,
            IIconCreditService iconCreditService)
        {
            _appInfoService = appInfoService;
            _localizationService = localizationService;
            _iconCreditService = iconCreditService;
            AppVersion = _localizationService.GetString("About_Loading");
            LoadData();
        }

        #endregion

        #region PrivateHelperMethods

        private void LoadData()
        {
            string prefix = _localizationService.GetString("About_VersionPrefix");
            AppVersion = $"{prefix} {_appInfoService.GetAppVersion()}";
            var credits = _iconCreditService.GetIconCredits();
            IconCredits.Clear();
            foreach (var iconCredit in credits)
                IconCredits.Add(iconCredit);
        }

        #endregion
    }
}
