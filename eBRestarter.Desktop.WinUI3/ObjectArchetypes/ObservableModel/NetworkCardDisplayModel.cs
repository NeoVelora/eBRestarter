using CommunityToolkit.Mvvm.ComponentModel;

namespace eBRestarter.Desktop.WinUI3.ObjectArchetypes.ObservableModel;

public sealed partial class NetworkCardDisplayModel : ObservableObject
{
    [ObservableProperty] public partial string AdapterName { get; set; } = string.Empty;
    [ObservableProperty] public partial string ReceivedData { get; set; } = string.Empty;
    [ObservableProperty] public partial string SentData { get; set; } = string.Empty;
    [ObservableProperty] public partial string ImagePathNetworkCard { get; set; } = string.Empty;
    [ObservableProperty] public partial string ImagePathReceivedData { get; set; } = string.Empty;
    [ObservableProperty] public partial string ImagePathSendData { get; set; } = string.Empty;
    [ObservableProperty] public partial string ForegroundColor { get; set; } = "#000000";


    public string Id => AdapterName;
}
