using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls;

public sealed partial class UC_DeleteBrowserContentTask : UserControl
{
    public ViewModelRestartTask ViewModelRestartTask { get; }

    public UC_DeleteBrowserContentTask()
    {
        ViewModelRestartTask = App.AppHost!.Services.GetRequiredService<ViewModelRestartTask>();

        InitializeComponent();
    }
}
