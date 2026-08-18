using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using eBRestarter.Desktop.WinUI3.ViewModels;

namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.Dialogs;

public sealed partial class DeleteBrowserContentDialog : ContentDialog
{
    public ViewModelDeleteBrowserContent ViewModelDeleteBrowserContent { get; }


    public DeleteBrowserContentDialog()
    {
        ViewModelDeleteBrowserContent = App.AppHost!.Services.GetRequiredService<ViewModelDeleteBrowserContent>();

        InitializeComponent();

        DataContext = ViewModelDeleteBrowserContent;
        Loaded += DeleteBrowserContentDialog_Loaded;
        Unloaded += DeleteBrowserContentDialog_Unloaded;
    }


    private void CloseDialog() => Hide();

    private void DeleteBrowserContentDialog_Loaded(object? sender, RoutedEventArgs eventArgs) =>
        ViewModelDeleteBrowserContent.RequestCloseDialog += CloseDialog;

    private void DeleteBrowserContentDialog_Unloaded(object? sender, RoutedEventArgs eventArgs) =>
        ViewModelDeleteBrowserContent.RequestCloseDialog -= CloseDialog;
}
