using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls;

public sealed partial class UC_Infocenter : UserControl
{
    public ViewModelInfocenter ViewModelInfocenter { get; }

    public UC_Infocenter()
    {
        ViewModelInfocenter = App.AppHost!.Services.GetRequiredService<ViewModelInfocenter>();

        InitializeComponent();


        this.DataContext = ViewModelInfocenter;
    }
}
