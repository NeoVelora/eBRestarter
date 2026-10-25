using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls;

public sealed partial class UC_InformationCardUrls : UserControl
{
    public ViewModelInfocenter ViewModelInfocenter { get; }

    public UC_InformationCardUrls()
    {
        ViewModelInfocenter = App.AppHost!.Services.GetRequiredService<ViewModelInfocenter>();

        InitializeComponent();
    }
}
