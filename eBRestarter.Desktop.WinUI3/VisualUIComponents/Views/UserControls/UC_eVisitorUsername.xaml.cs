using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_eVisitorUsername : UserControl
{
    public ViewModelRestarterProperties ViewModelRestarterProperties { get; }

    public UC_eVisitorUsername()
    {
        ViewModelRestarterProperties = App.AppHost!.Services.GetRequiredService<ViewModelRestarterProperties>();

        InitializeComponent();
    }
}
