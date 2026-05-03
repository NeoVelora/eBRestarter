using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.Views.UserControls;

public sealed partial class UC_Networktraffic : UserControl
{
    public ViewModelNetworkTraffic ViewModelNetworkTraffic { get; }

    public UC_Networktraffic()
    {
        InitializeComponent();

        ViewModelNetworkTraffic = App.AppHost!.Services.GetRequiredService<ViewModelNetworkTraffic>();

        this.DataContext = ViewModelNetworkTraffic;
    }
}

