using eBRestarter.Core.Domain.Models;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Desktop.WinUI3.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.Views.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.Services
{
    public class DialogService : IDialogService
    {
        private XamlRoot _xamlRoot => App.MainWindoweBRestarter!.Content.XamlRoot;

        public Task ShowMessageAsync(string title, string message)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ShowYesNoDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = _xamlRoot,
                Title = title,
                Content = message,
                PrimaryButtonText = "Ja",
                CloseButtonText = "Nein"
            };

            var result = await dialog.ShowAsync();

            return result == ContentDialogResult.Primary;
        }

        public async Task<AutoLogonDialogResult?> ShowAutoLogonDialogAsync(string defaultUser = null, string defaultDomain = null)
        {
            var dialog = new AutoLogonDialog
            {
                XamlRoot = _xamlRoot
            };

            dialog.SetDefaults(defaultUser, defaultDomain);

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                return AutoLogonDialogResult.Save(dialog.GetCredentials());
            }
            else if (result == ContentDialogResult.Secondary)
            {
                return AutoLogonDialogResult.Deactivate();
            }

            return null;
        }

        public async Task ShowAboutDialogAsync()
        {
            var dialog = new AboutDialog
            {
                XamlRoot = _xamlRoot
            };

            await dialog.ShowAsync();
        }

        public async Task ShowBrowserDeleteContentDialogAsync()
        {
            var dialog = new DeleteBrowserContentDialog
            {
                XamlRoot = _xamlRoot
            };

            await dialog.ShowAsync();
        }

        public async Task ShowInstallAddOnDialogAsync()
        {
            var dialog = new InstallAddOnDialog
            {
                XamlRoot = _xamlRoot
            };
            await dialog.ShowAsync();
        }

        public async Task ShowActivateApiDialogAsync()
        {
            var dialog = new ActivateApiDialog
            {
                XamlRoot = _xamlRoot
            };
            await dialog.ShowAsync();
        }

        public async Task ShowImportApiDialogAsync()
        {
            var dialog = new ImportApiDialog
            {
                XamlRoot = _xamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
