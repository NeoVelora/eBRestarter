using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Extensions;
using eBRestarter.Desktop.WinUI3.Models.UI;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Timers;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class NetworkTrafficViewModel : ObservableObject, IDisposable
    {
        private readonly IWindowsNetworkInfoService _networkService;
        private readonly System.Timers.Timer _timer;
        private readonly DispatcherQueue _dispatcherQueue;

        public ObservableCollection<NetworkCardDisplayModel> NetworkCards { get; } = [];

        private const string ImgCard = "/Resources/Visuals/Icons/LightTheme/network-interface-card_light_theme.png";
        private const string ImgDown = "/Resources/Visuals/Icons/LightTheme/download_light_theme.png";
        private const string ImgUp = "/Resources/Visuals/Icons/LightTheme/send-data-light_theme.png";
        private const string ColorDefault = "#FFFFFF";
        private const string ColorError = "#FF0000";

        public NetworkTrafficViewModel(IWindowsNetworkInfoService networkService)
        {
            _networkService = networkService;

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();

            UpdateTrafficData();
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                UpdateTrafficData();
            });
        }

        private void UpdateTrafficData()
        {
            if (!_networkService.IsNetworkAvailable())
            {
                ShowOfflineState();
                return;
            }

            var currentStats = _networkService.GetActiveInterfaces().ToList();


            var activeIds = currentStats.Select(x => $"Netzwerkkarte: {x.Name}").ToList();
            var itemsToRemove = NetworkCards.Where(x => !activeIds.Contains(x.AdapterName)).ToList();
            foreach (var item in itemsToRemove) NetworkCards.Remove(item);

            foreach (var stat in currentStats)
            {
                string name = $"Netzwerkkarte: {stat.Name}";
                var existingItem = NetworkCards.FirstOrDefault(x => x.AdapterName == name);

                if (existingItem != null)
                {
                    existingItem.ReceivedData = $"Empfangen: {FormatExtensions.ToSizeSuffix(stat.BytesReceived)}";
                    existingItem.SentData = $"Gesendet: {FormatExtensions.ToSizeSuffix(stat.BytesSent)}";
                    existingItem.ForegroundColor = ColorDefault;
                }
                else
                {
                    NetworkCards.Add(new NetworkCardDisplayModel
                    {
                        AdapterName = name,
                        ReceivedData = $"Empfangen: {FormatExtensions.ToSizeSuffix(stat.BytesReceived)}",
                        SentData = $"Gesendet: {FormatExtensions.ToSizeSuffix(stat.BytesSent)}",
                        ImagePathNetworkCard = ImgCard,
                        ImagePathReceivedData = ImgDown,
                        ImagePathSendData = ImgUp,
                        ForegroundColor = ColorDefault
                    });
                }
            }
        }

        private void ShowOfflineState()
        {
            NetworkCards.Clear();
            NetworkCards.Add(new NetworkCardDisplayModel
            {
                AdapterName = "Netzwerkkarte: nicht verfügbar",
                ReceivedData = "Empfangen: -",
                SentData = "Gesendet: -",
                ForegroundColor = ColorError,
                ImagePathNetworkCard = ImgCard,
                ImagePathReceivedData = ImgDown,
                ImagePathSendData = ImgUp
            });
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
