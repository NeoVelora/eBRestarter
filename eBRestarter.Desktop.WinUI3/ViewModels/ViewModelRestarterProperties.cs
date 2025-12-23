using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using eBRestarter.Application.Facade.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelRestarterProperties : ObservableObject
    {
        private readonly IOperatingSystemFacade _os;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddeVVisitorUsernameCommand))]
        public partial string Username { get; set; } = string.Empty;

        [ObservableProperty]
        private partial string StandardBrowser { get; set; } = string.Empty;

        public ViewModelRestarterProperties(IOperatingSystemFacade os)
        {
            _os = os;

            StandardBrowser = _os.SystemInfo.GetCurrentStandardBrowserName();
        }

        [RelayCommand(CanExecute = nameof(CanAddUsername))]
        private void AddeVVisitorUsername()
        {
            System.Diagnostics.Debug.WriteLine($"User {Username} hinzugefügt.");
        }

        private bool CanAddUsername()
        {
            return !string.IsNullOrWhiteSpace(Username);
        }

        [RelayCommand]
        public void RegisterToEVisitor()
        {
            Debug.WriteLine("ViewModelRestarterProperties: RegisterToEVisitor");
        }
    }
}
