using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

using eBRestarter.Core.Application.BehavioralComponents.Extensions;
using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.ObservableModel;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelNetworkTraffic : ObservableObject, IDisposable
{
    private const string NetworkCardDefaultForegroundHex = "#FFFFFF";
    private const string NetworkCardIconPath = "/Resources/Visuals/Icons/LightTheme/network-interface-card_light_theme.png";
    private const string NetworkCardPrefixResourceKey = "Network_CardPrefix";
    private const string NetworkNotAvailableResourceKey = "Network_NotAvailable";
    private const string NetworkOfflineForegroundHex = "#FF0000";
    private const string NetworkReceivedResourceKey = "Network_Received";
    private const string NetworkSentResourceKey = "Network_Sent";
    private const double NetworkStatsPollIntervalMilliseconds = 1000;
    private const string OfflineDataPlaceholder = "-";
    private const string ReceivedDataIconPath = "/Resources/Visuals/Icons/LightTheme/download_light_theme.png";
    private const string SentDataIconPath = "/Resources/Visuals/Icons/LightTheme/send-data-light_theme.png";

    private readonly IInboundPortLocalizationProvider _localizationService;
    private readonly ILogger<ViewModelNetworkTraffic> _logger;
    private readonly IInboundPortNetworkInfoProvider _networkService;

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Timer _timer;

    private volatile bool _disposed;


    public ObservableCollection<NetworkCardDisplayModel> NetworkCards { get; } = [];

    public ViewModelNetworkTraffic(
        IInboundPortLocalizationProvider localizationService,
        ILogger<ViewModelNetworkTraffic> logger,
        IInboundPortNetworkInfoProvider networkService)
    {
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(networkService);

        _localizationService = localizationService;
        _logger = logger;
        _networkService = networkService;

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
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();

        GC.SuppressFinalize(this);
    }

    public void StartPolling()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Start();
        Task.Run(PerformUpdate);
    }

    public void StopPolling()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Stop();
    }

    private void ApplyDataToUi(bool isNetworkAvailable, IEnumerable<NetworkStats>? stats)
    {
        if (!isNetworkAvailable || stats is null)
        {
            ShowOfflineState();
            return;
        }

        string adapterNamePrefix = _localizationService.RetrieveString(NetworkCardPrefixResourceKey);
        string receivedPrefix = _localizationService.RetrieveString(NetworkReceivedResourceKey);
        string sentPrefix = _localizationService.RetrieveString(NetworkSentResourceKey);

        var activeIds = stats.Select(networkStat => $"{adapterNamePrefix}: {networkStat.Name}").ToHashSet();

        var cardsToRemove = NetworkCards.Where(card => !activeIds.Contains(card.AdapterName)).ToList();

        foreach (var cardToRemove in cardsToRemove)
        {
            NetworkCards.Remove(cardToRemove);
        }

        foreach (var stat in stats)
        {
            string name = $"{adapterNamePrefix}: {stat.Name}";
            var existingCard = NetworkCards.FirstOrDefault(card => card.AdapterName == name);

            string received = $"{receivedPrefix}: {stat.BytesReceived.ToSizeSuffix()}";
            string sent = $"{sentPrefix}: {stat.BytesSent.ToSizeSuffix()}";

            if (existingCard is not null)
            {
                existingCard.ReceivedData = received;
                existingCard.SentData = sent;
                existingCard.ForegroundColor = NetworkCardDefaultForegroundHex;
                continue;
            }

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

    private void OnTimerElapsed(object? sender, ElapsedEventArgs elapsedEventArgs)
    {
        if (_disposed)
        {
            return;
        }

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
                currentStats = [.. _networkService.RetrieveActiveInterfaces()];
            }
        }
        catch (Exception exception)
        {
            _logger.LogDebug(
                LogEventIds.UserInterface.ViewModelOperationFailed,
                "Reading network interface statistics failed: {Reason}",
                exception.Message);

            isAvailable = false;
        }

        _dispatcherQueue.TryEnqueue(() => ApplyDataToUi(isAvailable, currentStats));
    }

    private void ShowOfflineState()
    {
        string adapterNamePrefix = _localizationService.RetrieveString(NetworkCardPrefixResourceKey);
        string notAvailableLabel = _localizationService.RetrieveString(NetworkNotAvailableResourceKey);
        string receivedPrefix = _localizationService.RetrieveString(NetworkReceivedResourceKey);
        string sentPrefix = _localizationService.RetrieveString(NetworkSentResourceKey);

        string fullName = $"{adapterNamePrefix}: {notAvailableLabel}";

        if (NetworkCards.Count == 1 && NetworkCards[0].AdapterName == fullName)
        {
            return;
        }

        NetworkCards.Clear();

        NetworkCards.Add(new NetworkCardDisplayModel
        {
            AdapterName = fullName,
            ReceivedData = $"{receivedPrefix}: {OfflineDataPlaceholder}",
            SentData = $"{sentPrefix}: {OfflineDataPlaceholder}",
            ForegroundColor = NetworkOfflineForegroundHex,
            ImagePathNetworkCard = NetworkCardIconPath,
            ImagePathReceivedData = ReceivedDataIconPath,
            ImagePathSendData = SentDataIconPath
        });
    }
}
