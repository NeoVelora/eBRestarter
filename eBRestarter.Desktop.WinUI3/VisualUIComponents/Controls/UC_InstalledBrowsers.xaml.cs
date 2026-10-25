using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls
{
    public sealed partial class UC_InstalledBrowsers : UserControl
    {
        public ViewModelInstalledBrowsers ViewModelInstalledBrowsers { get; }

        public UC_InstalledBrowsers()
        {
        ViewModelInstalledBrowsers = App.AppHost!.Services.GetRequiredService<ViewModelInstalledBrowsers>();

        InitializeComponent();


            this.DataContext = ViewModelInstalledBrowsers;
        }
    }
}