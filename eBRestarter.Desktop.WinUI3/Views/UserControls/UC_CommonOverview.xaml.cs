using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.Views.UserControls;

public sealed partial class UC_CommonOverview : UserControl
{
    public ViewModelGeneralOverview ViewModelGeneralOverView { get; }
    public UC_CommonOverview()
    {
        InitializeComponent();

        ViewModelGeneralOverView = App.AppHost!.Services.GetRequiredService<ViewModelGeneralOverview>();

        this.DataContext = ViewModelGeneralOverView;
    }

    private void BtnGoToeVisitorAPI_Click(object sender, RoutedEventArgs e)
    {
        System.ArgumentNullException.ThrowIfNull(sender);
        System.ArgumentNullException.ThrowIfNull(e);
    }
}
