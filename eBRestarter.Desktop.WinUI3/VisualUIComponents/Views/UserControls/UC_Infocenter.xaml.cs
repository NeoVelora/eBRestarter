using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_Infocenter : UserControl
{
    public ViewModelInfocenter ViewModelInfocenter { get; }

    public UC_Infocenter()
    {
        InitializeComponent();

        ViewModelInfocenter = App.AppHost!.Services.GetRequiredService<ViewModelInfocenter>();

        this.DataContext = ViewModelInfocenter;
    }
}
