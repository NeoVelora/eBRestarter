using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_AdditionalOptions : UserControl
{
    public ViewModelRestarterProperties ViewModelRestarterProperties { get; }

    public UC_AdditionalOptions()
    {
        ViewModelRestarterProperties = App.AppHost!.Services.GetRequiredService<ViewModelRestarterProperties>();

        InitializeComponent();
    }
}
