using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Pages;

public sealed partial class P_RestarterProperties : Page
{
    public ViewModelRestarterProperties ViewModelRestarterProperties { get; }

    public P_RestarterProperties()
    {
        ViewModelRestarterProperties = App.AppHost!.Services.GetRequiredService<ViewModelRestarterProperties>();

        this.InitializeComponent();


        this.DataContext = ViewModelRestarterProperties;
    }
}