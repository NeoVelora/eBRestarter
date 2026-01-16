using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces.OperatingSystem.WindowsOS;
using eBRestarter.Core.Domain.Extensions;
using eBRestarter.Core.Domain.Models.Records;
using eBRestarter.Desktop.WinUI3.Models.UI;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelNetworkTraffic : ObservableObject, IDisposable
    {
        #region Constants
        private const string ColorDefault = "#FFFFFF";
        private const string ColorError = "#FF0000";
        private const string ImgCard = "/Resources/Visuals/Icons/LightTheme/network-interface-card_light_theme.png";
        private const string ImgDown = "/Resources/Visuals/Icons/LightTheme/download_light_theme.png";
        private const string ImgUp = "/Resources/Visuals/Icons/LightTheme/send-data-light_theme.png";
        #endregion

        #region Fields
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IWindowsNetworkInfoService _networkService;
        private readonly Timer _timer;
        #endregion

        #region Properties
        public ObservableCollection<NetworkCardDisplayModel> NetworkCards { get; } = [];
        #endregion

        #region Constructors
        public ViewModelNetworkTraffic(IWindowsNetworkInfoService networkService)
        {
            _networkService = networkService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();

            System.Threading.Tasks.Task.Run(() => PerformUpdate());
        }
        #endregion

        #region Methods
        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }

        private void ApplyDataToUi(bool isNetworkAvailable, IEnumerable<NetworkStats>? stats)
        {
            if (!isNetworkAvailable || stats == null)
            {
                ShowOfflineState();
                return;
            }


            var activeIds = stats.Select(x => $"Netzwerkkarte: {x.Name}").ToList();
            var itemsToRemove = NetworkCards.Where(x => !activeIds.Contains(x.AdapterName)).ToList();

            foreach (var item in itemsToRemove)
            {
                NetworkCards.Remove(item);
            }

            foreach (var stat in stats)
            {
                string name = $"Netzwerkkarte: {stat.Name}";
                var existingItem = NetworkCards.FirstOrDefault(x => x.AdapterName == name);

                string received = $"Empfangen: {FormatExtensions.ToSizeSuffix(stat.BytesReceived)}";
                string sent = $"Gesendet: {FormatExtensions.ToSizeSuffix(stat.BytesSent)}";

                if (existingItem != null)
                {
                    existingItem.ReceivedData = received;
                    existingItem.SentData = sent;
                    existingItem.ForegroundColor = ColorDefault;
                }
                else
                {
                    NetworkCards.Add(new NetworkCardDisplayModel
                    {
                        AdapterName = name,
                        ReceivedData = received,
                        SentData = sent,
                        ImagePathNetworkCard = ImgCard,
                        ImagePathReceivedData = ImgDown,
                        ImagePathSendData = ImgUp,
                        ForegroundColor = ColorDefault
                    });
                }
            }
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            PerformUpdate();
        }

        private void PerformUpdate()
        {

            bool isAvailable = false;
            List<NetworkStats>? currentStats = null;

            try
            {
                isAvailable = _networkService.IsNetworkAvailable();
                if (isAvailable)
                {
                    currentStats = _networkService.GetActiveInterfaces().ToList();
                }
            }
            catch (Exception)
            {
                isAvailable = false;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                ApplyDataToUi(isAvailable, currentStats);
            });
        }

        private void ShowOfflineState()
        {
            if (NetworkCards.Count == 1 && NetworkCards[0].AdapterName == "Netzwerkkarte: nicht verfügbar")
                return;

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
        #endregion
    }
}
