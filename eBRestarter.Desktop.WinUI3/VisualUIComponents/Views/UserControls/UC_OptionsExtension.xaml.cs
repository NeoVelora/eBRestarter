using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_OptionsExtension : UserControl
{
    public ViewModelOptionsExtension ViewModel { get; }

    public UC_OptionsExtension()
    {
        ViewModel = App.AppHost!.Services.GetRequiredService<ViewModelOptionsExtension>();

        InitializeComponent();
        this.DataContext = ViewModel;
    }
}
