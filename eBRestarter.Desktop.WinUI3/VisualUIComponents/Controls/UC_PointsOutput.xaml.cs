using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls;

public sealed partial class UC_PointsOutput : UserControl
{
    public ViewModelGeneralOverview ViewModelGeneralOverview { get; }

    public UC_PointsOutput()
    {
        ViewModelGeneralOverview = App.AppHost!.Services.GetRequiredService<ViewModelGeneralOverview>();

        InitializeComponent();
    }
}
