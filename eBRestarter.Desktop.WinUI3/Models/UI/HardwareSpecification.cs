using CommunityToolkit.Mvvm.ComponentModel;

namespace eBRestarter.Desktop.WinUI3.Models.UI;

public sealed partial class HardwareSpecification : ObservableObject
{
    [ObservableProperty] public partial string Processor { get; set; } = string.Empty;
    [ObservableProperty] public partial string Graphics { get; set; } = string.Empty;
    [ObservableProperty] public partial string Ram { get; set; } = string.Empty;
}
