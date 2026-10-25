using eBRestarter.Desktop.WinUI3.UIStateComponents.ViewModels.Components;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls;

public sealed partial class UC_Browser : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(ViewModelBrowserItem),
            typeof(UC_Browser),
            new PropertyMetadata(null, OnViewModelPropertyChanged));

    public ViewModelBrowserItem? ViewModel
    {
        get => (ViewModelBrowserItem?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public UC_Browser()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private static void OnViewModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UC_Browser control && control.DataContext != e.NewValue)
        {
            control.DataContext = e.NewValue;
        }
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (DataContext is ViewModelBrowserItem vm && ViewModel != vm)
        {
            ViewModel = vm;
        }
    }
}
