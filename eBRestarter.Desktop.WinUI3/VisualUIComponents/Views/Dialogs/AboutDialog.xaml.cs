using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.Dialogs;

public sealed partial class AboutDialog : ContentDialog
{
    public ViewModelAbout ViewModelAbout { get; }

    public AboutDialog()
    {
        ViewModelAbout = App.AppHost!.Services.GetRequiredService<ViewModelAbout>();

        this.InitializeComponent();


        this.DataContext = ViewModelAbout;
    }
}
