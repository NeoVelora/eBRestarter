using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.Views.UserControls
{
    public sealed partial class UC_InstalledBrowsers : UserControl
    {
        public ViewModelInstalledBrowsers ViewModelInstalledBrowsers { get; }

        public UC_InstalledBrowsers()
        {
            InitializeComponent();

            ViewModelInstalledBrowsers = App.AppHost!.Services.GetRequiredService<ViewModelInstalledBrowsers>();

            this.DataContext = ViewModelInstalledBrowsers;
        }
    }
}