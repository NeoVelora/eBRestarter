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

    }
}
