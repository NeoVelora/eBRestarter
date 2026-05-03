using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;


namespace eBRestarter.Desktop.WinUI3.Views.Dialogs;

public sealed partial class ImportApiDialog : ContentDialog
{
    public ViewModelImportApi ViewModelImportApi { get; }

    public ImportApiDialog()
    {
        InitializeComponent();
        ViewModelImportApi = App.AppHost!.Services.GetRequiredService<ViewModelImportApi>();
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(e);

        e.AcceptedOperation = DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Hier ablegen";
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);

        if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count > 0 && items[0] is Windows.Storage.StorageFile file)
            {
                ViewModelImportApi.HandleFileDrop(file.Path);
            }
        }
    }

    private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(e);
    }
}
