using CommunityToolkit.Mvvm.ComponentModel;

namespace eBRestarter.Desktop.WinUI3.Models.UI;

public partial class HardwareSpecification : ObservableObject
{
    #region ObservableProperties

    [ObservableProperty] public partial string Processor { get; set; } = string.Empty;
    [ObservableProperty] public partial string Graphics { get; set; } = string.Empty;
    [ObservableProperty] public partial string Ram { get; set; } = string.Empty;

    #endregion
}
