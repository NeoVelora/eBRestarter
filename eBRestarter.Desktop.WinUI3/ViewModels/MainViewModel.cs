using eBRestarter.Desktop.WinUI3.Services.Interfaces;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public class MainViewModel
    {
        #region FieldsAndInjectedServices

        private readonly INavigationService _navigationService;

        #endregion

        #region ConstructorAndFinalizer

        public MainViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        #endregion
    }
}
