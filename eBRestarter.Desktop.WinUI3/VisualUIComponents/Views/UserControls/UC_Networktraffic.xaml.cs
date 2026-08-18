using System;

using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_Networktraffic : UserControl
{
    public ViewModelNetworkTraffic ViewModelNetworkTraffic { get; }

    public UC_Networktraffic()
    {
        InitializeComponent();

        ViewModelNetworkTraffic = App.AppHost!.Services.GetRequiredService<Func<ViewModelNetworkTraffic>>()();

        this.DataContext = ViewModelNetworkTraffic;

        Loaded += (_, _) => ViewModelNetworkTraffic.StartPolling();
        Unloaded += (_, _) => ViewModelNetworkTraffic.StopPolling();
    }
}

