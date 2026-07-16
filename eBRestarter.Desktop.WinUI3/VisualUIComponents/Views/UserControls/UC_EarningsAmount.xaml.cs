using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Views.UserControls;

public sealed partial class UC_EarningsAmount : UserControl
{
    public static readonly DependencyProperty HeaderTitleEarningsProperty =
        DependencyProperty.Register(
            nameof(HeaderTitleEarnings),
            typeof(string),
            typeof(UC_EarningsAmount),
            new PropertyMetadata(string.Empty));

    public string HeaderTitleEarnings
    {
        get => (string)GetValue(HeaderTitleEarningsProperty);
        set => SetValue(HeaderTitleEarningsProperty, value);
    }

    public static readonly DependencyProperty EarningsAmountProperty =
        DependencyProperty.Register(
            nameof(EarningsAmount),
            typeof(string),
            typeof(UC_EarningsAmount),
            new PropertyMetadata(string.Empty));

    public string EarningsAmount
    {
        get => (string)GetValue(EarningsAmountProperty);
        set => SetValue(EarningsAmountProperty, value);
    }

    public static readonly DependencyProperty HeaderImageEarningsProperty =
        DependencyProperty.Register(
            nameof(HeaderImageEarnings),
            typeof(object),
            typeof(UC_EarningsAmount),
            new PropertyMetadata(null));

    public object HeaderImageEarnings
    {
        get => (object)GetValue(HeaderImageEarningsProperty);
        set => SetValue(HeaderImageEarningsProperty, value);
    }

    public UC_EarningsAmount()
    {
        InitializeComponent();
    }
}

