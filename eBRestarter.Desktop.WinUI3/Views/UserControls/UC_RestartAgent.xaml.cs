using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.Views.UserControls;

public sealed partial class UC_RestartAgent : UserControl
{
    public ViewModelRestartTask ViewModelRestartTask { get; }

    public UC_RestartAgent()
    {
        InitializeComponent();

        ViewModelRestartTask = App.AppHost!.Services.GetRequiredService<ViewModelRestartTask>();

        this.DataContext = ViewModelRestartTask;
    }
}
