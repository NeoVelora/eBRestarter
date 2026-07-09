using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Extensions;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.ObservableModel;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelNetworkTraffic : ObservableObject, IDisposable
{
    private const string NetworkCardDefaultForegroundHex = "#FFFFFF";

    private const string NetworkCardIconPath = "/Resources/Visuals/Icons/LightTheme/network-interface-card_light_theme.png";

    private const string NetworkOfflineForegroundHex = "#FF0000";

    private const double NetworkStatsPollIntervalMilliseconds = 1000;

    private const string ReceivedDataIconPath = "/Resources/Visuals/Icons/LightTheme/download_light_theme.png";

    private const string SentDataIconPath = "/Resources/Visuals/Icons/LightTheme/send-data-light_theme.png";

    private readonly IInboundPortLocalizationProvider _localizationService;

    private readonly IInboundPortNetworkInfoProvider _networkService;

    private readonly DispatcherQueue _dispatcherQueue;

    private readonly Timer _timer;

    public ObservableCollection<NetworkCardDisplayModel> NetworkCards { get; } = [];


    public ViewModelNetworkTraffic(
        IInboundPortNetworkInfoProvider networkService,
        IInboundPortLocalizationProvider LocalizationProvider)
    {
        ArgumentNullException.ThrowIfNull(networkService);
        ArgumentNullException.ThrowIfNull(LocalizationProvider);

        _networkService = networkService;
        _localizationService = LocalizationProvider;

        _dispatcherQueue =
            DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException(
                $"{nameof(ViewModelNetworkTraffic)} must be constructed on a thread with a WinUI DispatcherQueue (UI thread).");

        _timer = new Timer(NetworkStatsPollIntervalMilliseconds);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();

        Task.Run(PerformUpdate);
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();

        GC.SuppressFinalize(this);
    }

    private void ApplyDataToUi(bool isNetworkAvailable, IEnumerable<NetworkStats>? stats)
    {
        if (!isNetworkAvailable || stats == null)
        {
            ShowOfflineState();
            return;
        }

        string adapterNamePrefix = _localizationService.RetrieveString("Network_CardPrefix");
        string receivedPrefix = _localizationService.RetrieveString("Network_Received");
        string sentPrefix = _localizationService.RetrieveString("Network_Sent");

        var activeIds = stats.Select(networkStat => $"{adapterNamePrefix}: {networkStat.Name}").ToList();

        var cardsToRemove = NetworkCards.Where(card => !activeIds.Contains(card.AdapterName)).ToList();

        foreach (var cardToRemove in cardsToRemove)
            NetworkCards.Remove(cardToRemove);

        foreach (var stat in stats)
        {
            string name = $"{adapterNamePrefix}: {stat.Name}";
            var existingCard = NetworkCards.FirstOrDefault(card => card.AdapterName == name);

            string received = $"{receivedPrefix}: {stat.BytesReceived.ToSizeSuffix()}";
            string sent = $"{sentPrefix}: {stat.BytesSent.ToSizeSuffix()}";

            if (existingCard != null)
            {
                existingCard.ReceivedData = received;
                existingCard.SentData = sent;
                existingCard.ForegroundColor = NetworkCardDefaultForegroundHex;
            }
            else
            {
                NetworkCards.Add(new NetworkCardDisplayModel
                {
                    AdapterName = name,
                    ReceivedData = received,
                    SentData = sent,
                    ImagePathNetworkCard = NetworkCardIconPath,
                    ImagePathReceivedData = ReceivedDataIconPath,
                    ImagePathSendData = SentDataIconPath,
                    ForegroundColor = NetworkCardDefaultForegroundHex
                });
            }
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs elapsedEventArgs)
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
                currentStats = [.. _networkService.RetrieveActiveInterfaces()];
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            isAvailable = false;
        }

        _dispatcherQueue.TryEnqueue(() => ApplyDataToUi(isAvailable, currentStats));
    }

    private void ShowOfflineState()
    {
        string adapterNamePrefix = _localizationService.RetrieveString("Network_CardPrefix");
        string notAvailableLabel = _localizationService.RetrieveString("Network_NotAvailable");
        string receivedPrefix = _localizationService.RetrieveString("Network_Received");
        string sentPrefix = _localizationService.RetrieveString("Network_Sent");

        string fullName = $"{adapterNamePrefix}: {notAvailableLabel}";

        if (NetworkCards.Count == 1 && NetworkCards[0].AdapterName == fullName)
            return;

        NetworkCards.Clear();

        NetworkCards.Add(new NetworkCardDisplayModel
        {
            AdapterName = fullName,
            ReceivedData = $"{receivedPrefix}: -",
            SentData = $"{sentPrefix}: -",
            ForegroundColor = NetworkOfflineForegroundHex,
            ImagePathNetworkCard = NetworkCardIconPath,
            ImagePathReceivedData = ReceivedDataIconPath,
            ImagePathSendData = SentDataIconPath
        });
    }
}


