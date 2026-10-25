using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Dialogs;

public partial class TurnOffEdgeStartupBoostDialog : ContentDialog
{
    public ViewModelTurnOffEdgeStartupBoost ViewModel { get; }

    public TurnOffEdgeStartupBoostDialog()
    {
        ViewModel = App.AppHost!.Services.GetRequiredService<ViewModelTurnOffEdgeStartupBoost>();

        this.InitializeComponent();


        this.DataContext = ViewModel;
    }
}
