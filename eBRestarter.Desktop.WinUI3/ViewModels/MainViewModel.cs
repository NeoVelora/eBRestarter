using CommunityToolkit.Mvvm.Input;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

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
