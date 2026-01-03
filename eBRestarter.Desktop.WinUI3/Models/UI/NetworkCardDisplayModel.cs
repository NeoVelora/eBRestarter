using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.Models.UI
{
    public partial class NetworkCardDisplayModel : ObservableObject
    {
        public string Id => AdapterName;

        [ObservableProperty] public partial string AdapterName { get; set; } = string.Empty;
        [ObservableProperty] public partial string ReceivedData { get; set; } = string.Empty;
        [ObservableProperty] public partial string SentData { get; set; } = string.Empty;

        [ObservableProperty] public partial string ImagePathNetworkCard { get; set; } = string.Empty;
        [ObservableProperty] public partial string ImagePathReceivedData { get; set; } = string.Empty;
        [ObservableProperty] public partial string ImagePathSendData { get; set; } = string.Empty;

        [ObservableProperty] public partial string ForegroundColor { get; set; } = "#000000";
    }
}
