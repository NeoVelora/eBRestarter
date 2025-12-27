using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Extensions;
using eBRestarter.Desktop.WinUI3.Models.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace eBRestarter.Desktop.WinUI3.Views.UserControls;

public sealed partial class UC_Networktraffic : UserControl
{
    private readonly IWindowsNetworkInfoService _networkService;

    public ObservableCollection<NetworkCardDisplayModel> NetworkCards { get; set; } = new();

    private readonly DispatcherTimer _timer;

    private string _foregroundColor = "#FFFFFF";
    private string _imgCard = "pack://application:,,,/Images/card.png";
    private string _imgRec = "pack://application:,,,/Images/down.png";
    private string _imgSend = "pack://application:,,,/Images/up.png";

    public UC_Networktraffic()
    {
        InitializeComponent();


        icNetworkCardsList.ItemsSource = NetworkCards;
    }

    public void Initialize(IWindowsNetworkInfoService networkService)
    {
    }

    private void UpdateTrafficData(object? sender, EventArgs e)
    {
        if (_networkService == null) return;

        if (!_networkService.IsNetworkAvailable())
        {
            NetworkCards.Clear();
            NetworkCards.Add(CreateOfflineModel());
            return;
        }

        var currentStats = _networkService.GetActiveInterfaces().ToList();

        NetworkCards.Clear();

        foreach (var stat in currentStats)
        {
            NetworkCards.Add(new NetworkCardDisplayModel
            {
                AdapterName = $"Netzwerkkarte: {stat.Name}",
                ReceivedData = $"Empfangen: {FormatExtensions.ToSizeSuffix(stat.BytesReceived)}",
                SentData = $"Gesendet: {FormatExtensions.ToSizeSuffix(stat.BytesSent)}",

                ImagePathNetworkCard = _imgCard,
                ImagePathReceivedData = _imgRec,
                ImagePathSendData = _imgSend,
                ForegroundColor = _foregroundColor
            });
        }
    }

    private NetworkCardDisplayModel CreateOfflineModel()
    {
        return new NetworkCardDisplayModel
        {
            AdapterName = "Netzwerkkarte: nicht verf�gbar",
            ReceivedData = "Empfangen: -",
            SentData = "Gesendet: -",
            ForegroundColor = "Red",
            ImagePathNetworkCard = _imgCard,
            ImagePathReceivedData = _imgRec,
            ImagePathSendData = _imgSend
        };
    }
}

