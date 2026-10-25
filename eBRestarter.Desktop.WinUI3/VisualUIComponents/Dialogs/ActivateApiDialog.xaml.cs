using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Dialogs;

public sealed partial class ActivateApiDialog : ContentDialog
{
    public ViewModelActivateApi ViewModelActivateApi { get; }

    public ActivateApiDialog()
    {
        ViewModelActivateApi = App.AppHost!.Services.GetRequiredService<ViewModelActivateApi>();

        InitializeComponent();


        this.DataContext = ViewModelActivateApi;
    }
}
