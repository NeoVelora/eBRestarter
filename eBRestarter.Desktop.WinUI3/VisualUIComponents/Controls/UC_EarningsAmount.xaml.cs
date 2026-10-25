using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace eBRestarter.Desktop.WinUI3.VisualUIComponents.Controls;

public sealed partial class UC_EarningsAmount : UserControl
{
    public static readonly DependencyProperty EarningsAmountProperty =
        DependencyProperty.Register(
            nameof(EarningsAmount),
            typeof(string),
            typeof(UC_EarningsAmount),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HeaderImageEarningsProperty =
        DependencyProperty.Register(
            nameof(HeaderImageEarnings),
            typeof(ImageSource),
            typeof(UC_EarningsAmount),
            new PropertyMetadata(null));

    public static readonly DependencyProperty HeaderTitleEarningsProperty =
        DependencyProperty.Register(
            nameof(HeaderTitleEarnings),
            typeof(string),
            typeof(UC_EarningsAmount),
            new PropertyMetadata(string.Empty));


    public string EarningsAmount
    {
        get => (string)GetValue(EarningsAmountProperty);
        set => SetValue(EarningsAmountProperty, value);
    }

    public string HeaderTitleEarnings
    {
        get => (string)GetValue(HeaderTitleEarningsProperty);
        set => SetValue(HeaderTitleEarningsProperty, value);
    }

    public ImageSource HeaderImageEarnings
    {
        get => (ImageSource)GetValue(HeaderImageEarningsProperty);
        set => SetValue(HeaderImageEarningsProperty, value);
    }


    public UC_EarningsAmount()
    {
        InitializeComponent();
    }
}
