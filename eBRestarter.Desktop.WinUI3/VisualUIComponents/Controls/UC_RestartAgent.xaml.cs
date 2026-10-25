using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls;

public sealed partial class UC_RestartAgent : UserControl
{
    public ViewModelRestartTask ViewModelRestartTask { get; }

    public UC_RestartAgent()
    {
        ViewModelRestartTask = App.AppHost!.Services.GetRequiredService<ViewModelRestartTask>();

        InitializeComponent();


        this.DataContext = ViewModelRestartTask;
    }
}
