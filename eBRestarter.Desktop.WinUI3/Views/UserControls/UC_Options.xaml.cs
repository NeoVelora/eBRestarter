using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.Views.UserControls;

public sealed partial class UC_Options : UserControl
{
    public ViewModelOptions ViewModelOptions { get; }

    public UC_Options()
    {
        InitializeComponent();

        ViewModelOptions = App.AppHost!.Services.GetRequiredService<ViewModelOptions>();

        this.DataContext = ViewModelOptions;
    }

    public void SelectPivotIndex(int index)
    {
        OptionsPivot.SelectedIndex = index;
    }
}
