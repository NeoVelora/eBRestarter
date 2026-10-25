using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls;

public sealed partial class UC_RestartTask : UserControl
{
    public ViewModelRestartTask ViewModelRestartTask { get; }

    public UC_RestartTask()
    {
        ViewModelRestartTask = App.AppHost!.Services.GetRequiredService<ViewModelRestartTask>();

        InitializeComponent();
    }
}
