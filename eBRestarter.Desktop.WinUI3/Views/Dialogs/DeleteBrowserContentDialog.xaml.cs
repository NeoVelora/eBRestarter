using eBRestarter.Desktop.WinUI3.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace eBRestarter.Desktop.WinUI3.Views.Dialogs
{
    public partial class DeleteBrowserContentDialog : ContentDialog
    {
        public ViewModelDeleteBrowserContent ViewModelDeleteBrowserContent { get; }

        public DeleteBrowserContentDialog()
        {
            this.InitializeComponent();

            ViewModelDeleteBrowserContent = App.AppHost!.Services.GetRequiredService<ViewModelDeleteBrowserContent>();
            this.DataContext = ViewModelDeleteBrowserContent;

            this.Loaded += DeleteBrowserContentDialog_Loaded;
            this.Unloaded += DeleteBrowserContentDialog_Unloaded;
        }

        private void DeleteBrowserContentDialog_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModelDeleteBrowserContent.RequestCloseDialog += CloseDialog;
        }

        private void DeleteBrowserContentDialog_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            ViewModelDeleteBrowserContent.RequestCloseDialog -= CloseDialog;
        }

        private void CloseDialog()
        {
            this.Hide();
        }
    }
}
