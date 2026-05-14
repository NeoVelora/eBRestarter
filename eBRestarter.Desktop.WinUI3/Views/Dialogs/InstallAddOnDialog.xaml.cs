using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.Views.Dialogs;

public sealed partial class InstallAddOnDialog : ContentDialog
{
    public ViewModelInstallAddOn ViewModelInstallAddOn { get; }

    public InstallAddOnDialog()
    {
        InitializeComponent();
        ViewModelInstallAddOn = App.AppHost!.Services.GetRequiredService<ViewModelInstallAddOn>();

        this.DataContext = ViewModelInstallAddOn;

        this.Closed += (_, __) => ViewModelInstallAddOn.Dispose();
    }
}
