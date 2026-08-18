using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_RestartTask : UserControl
{
    public ViewModelRestartTask ViewModelRestartTask { get; }

    public UC_RestartTask()
    {
        ViewModelRestartTask = App.AppHost!.Services.GetRequiredService<ViewModelRestartTask>();

        InitializeComponent();
    }
}
