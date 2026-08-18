using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

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
