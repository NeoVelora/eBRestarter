using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_InformationCardUrls : UserControl
{
    public ViewModelInfocenter ViewModelInfocenter { get; }

    public UC_InformationCardUrls()
    {
        ViewModelInfocenter = App.AppHost!.Services.GetRequiredService<ViewModelInfocenter>();

        InitializeComponent();
    }
}
