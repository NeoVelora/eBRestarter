using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_DeleteBrowserContentTask : UserControl
{
    public ViewModelRestartTask ViewModelRestartTask { get; }

    public UC_DeleteBrowserContentTask()
    {
        ViewModelRestartTask = App.AppHost!.Services.GetRequiredService<ViewModelRestartTask>();

        InitializeComponent();
    }
}
