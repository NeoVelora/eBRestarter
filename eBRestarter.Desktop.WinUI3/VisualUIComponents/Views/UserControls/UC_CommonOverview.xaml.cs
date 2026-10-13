using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_CommonOverview : UserControl
{
    public ViewModelGeneralOverview ViewModelGeneralOverView { get; }
    
    public UC_CommonOverview()
    {
        ViewModelGeneralOverView = App.AppHost!.Services.GetRequiredService<ViewModelGeneralOverview>();

        InitializeComponent();


        this.DataContext = ViewModelGeneralOverView;
    }
}
