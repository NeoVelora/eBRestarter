using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;

using SkiaSharp;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces;
using eBRestarter.Core.Application.Ports.Outbound.Interfaces.Config;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.DTOs.SignalDTO.Messages;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelGeneralOverview : ObservableObject,
                                                      IDisposable,
                                                      IRecipient<ApiCredentialsUpdatedMessage>,
                                                      IRecipient<ApiCredentialsRemovedMessage>
{
    private const double ChartValueChangeEpsilon = 0.0001;
    private const int ColumnSeriesCornerRadius = 5;
    private const int DaysPerMonthLabelCount = 31;
    private const int EarningsRefreshTriggerMinute = 5;
    private const int EarningsRefreshTriggerSecond = 0;
    private const int InitialHourlyChartSlotCount = 24;
    private const int MidnightHour = 0;
    private const int TimerTickIntervalSeconds = 1;
    private const int YAxisMinStep = 100;

    private const string BtpDisplayFormat = "BTP: {0:N0}";
    private const string DateFormatDayMonthYear = "dd.MM.yyyy";
    private const string DateFormatMonthFull = "MMMM";
    private const string DateFormatTimeHourMinute = "HH:mm";
    private const string DateFormatYearFull = "yyyy";
    private const string DefaultPlaceholderDash = "-";
    private const string RouteNameOptions = "Options";

    private const string KeyApiIsNotEnabled = "API_isNotEnabled";
    private const string KeyChartFakeDataMode = "Chart_FakeDataMode";
    private const string KeyChartMonthsShort = "Chart_MonthsShort";
    private const string KeyChartSeriesEarnings = "Chart_SeriesEarnings";
    private const string KeyChartTitleDaily = "Chart_TitleDaily";
    private const string KeyChartTitleHourly = "Chart_TitleHourly";
    private const string KeyChartTitleOverview = "Chart_TitleOverview";
    private const string KeyChartTitleYearly = "Chart_TitleYearly";
    private const string KeyChartXAxisDay = "Chart_XAxisDay";
    private const string KeyChartXAxisMonth = "Chart_XAxisMonth";
    private const string KeyChartXAxisTime = "Chart_XAxisTime";
    private const string KeyChartYAxisPoints = "Chart_YAxisPoints";
    private const string KeyGeneralNextRefresh = "General_NextRefresh";
    private const string KeyGeneralToday = "General_Today";

    private readonly IOutboundPortEVisitorConfigRepository _configService;
    private readonly ILogger<ViewModelGeneralOverview> _logger;
    private readonly IOutboundPortEVisitorApiProvider _eVisitorApiService;
    private readonly IInboundPortLocalizationProvider _localizationService;
    private readonly INavigationService _navigationService;

    private volatile bool _disposed;
    private bool _isApiConfigured;

    private EarningsData? _cachedEarnings;
    private readonly ObservableCollection<ObservableValue> _chartValues;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ColumnSeries<ObservableValue> _mainColumnSeries;
    private readonly DispatcherTimer _timer;

    private readonly ICartesianAxis[][] _xAxesByPivotIndex;


    public ViewModelGeneralOverview(
        INavigationService navigationService,
        IOutboundPortEVisitorApiProvider eVisitorApiService,
        IInboundPortLocalizationProvider localizationService,
        ILogger<ViewModelGeneralOverview> logger,
        IOutboundPortEVisitorConfigRepository configService)
    {
        ArgumentNullException.ThrowIfNull(navigationService);
        ArgumentNullException.ThrowIfNull(eVisitorApiService);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configService);

        _logger = logger;

        _navigationService = navigationService;
        _eVisitorApiService = eVisitorApiService;
        _localizationService = localizationService;
        _configService = configService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        var config = _configService.LoadConfig();
        _isApiConfigured = !string.IsNullOrEmpty(config?.Settings?.ApiKey);

        CurrentDay = _localizationService.RetrieveString(KeyGeneralToday);

        YAxes = (ICartesianAxis[])
        [
            new Axis
            {
                Name = _localizationService.RetrieveString(KeyChartYAxisPoints),
                LabelsDensity = 1,
                SeparatorsPaint = new SolidColorPaint(new SKColor(40, 40, 40)) { StrokeThickness = 1 },
                MinStep = YAxisMinStep,
                MinLimit = 0,
                TextSize = 12
            }
        ];

        _chartValues = [];

        for (int index = 0; index < InitialHourlyChartSlotCount; index++)
        {
            _chartValues.Add(new ObservableValue(0));
        }

        _mainColumnSeries = new ColumnSeries<ObservableValue>
        {
            Values = _chartValues,
            Name = _localizationService.RetrieveString(KeyChartSeriesEarnings),
            Rx = ColumnSeriesCornerRadius,
            Ry = ColumnSeriesCornerRadius,
            Fill = new SolidColorPaint(new SKColor(0, 120, 215)),
            DataLabelsPaint = new SolidColorPaint(new SKColor(12, 142, 168)),
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
        };

        Series = [_mainColumnSeries];

        _xAxesByPivotIndex = BuildXAxesForAllPivots();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(TimerTickIntervalSeconds) };
        _timer.Tick += OnTimerTick;
        _timer.Start();

        Task.Run(LoadDataAsync);

        WeakReferenceMessenger.Default.RegisterAll(this);
    }


    public string ChartTitle
    {
        get
        {
            if (!_isApiConfigured && !UseScreenshotFakeData)
            {
                return _localizationService.RetrieveString(KeyApiIsNotEnabled);
            }

            string title = SelectedPivotIndex switch
            {
                0 => string.Format(CultureInfo.CurrentCulture, _localizationService.RetrieveString(KeyChartTitleHourly), DateTime.Now.ToString(DateFormatDayMonthYear, CultureInfo.CurrentCulture)),
                1 => string.Format(CultureInfo.CurrentCulture, _localizationService.RetrieveString(KeyChartTitleDaily), DateTime.Now.ToString(DateFormatMonthFull, CultureInfo.CurrentCulture)),
                2 => string.Format(CultureInfo.CurrentCulture, _localizationService.RetrieveString(KeyChartTitleYearly), DateTime.Now.ToString(DateFormatYearFull, CultureInfo.CurrentCulture)),
                _ => _localizationService.RetrieveString(KeyChartTitleOverview)
            };

            if (UseScreenshotFakeData)
            {
                title += _localizationService.RetrieveString(KeyChartFakeDataMode);
            }

            return title;
        }
    }

    [ObservableProperty]
    public partial string ClockNextEarningsRefresh { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    public partial string CountryCode { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    public partial string CountryName { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    public partial string CurrentDay { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    public partial string CurrentMonth { get; set; } = DateTime.Now.ToString(DateFormatMonthFull, CultureInfo.CurrentCulture);

    [ObservableProperty]
    public partial string CurrentYear { get; set; } = DateTime.Now.ToString(DateFormatYearFull, CultureInfo.CurrentCulture);

    [ObservableProperty]
    public partial string EarningsThisDaySum { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    public partial string EarningsThisMonthSum { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    public partial string EarningsThisYearSum { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    public partial string Host { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    public partial string IpAddress { get; set; } = DefaultPlaceholderDash;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(XAxes))]
    [NotifyPropertyChangedFor(nameof(ChartTitle))]
    public partial int SelectedPivotIndex { get; set; } = 0;

    [ObservableProperty]
    public partial ObservableCollection<ISeries> Series { get; set; }

    public bool UseScreenshotFakeData { get; set; } = false;

    public IEnumerable<ICartesianAxis> XAxes => _xAxesByPivotIndex[Math.Clamp(SelectedPivotIndex, 0, _xAxesByPivotIndex.Length - 1)];

    public IEnumerable<ICartesianAxis> YAxes { get; set; } = null!;


    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _timer.Stop();
        _timer.Tick -= OnTimerTick;

        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(ApiCredentialsUpdatedMessage message)
    {
        if (_disposed)
        {
            return;
        }

        _isApiConfigured = true;
        _dispatcherQueue.TryEnqueue(() => OnPropertyChanged(nameof(ChartTitle)));
        Task.Run(LoadDataAsync);
    }

    public void Receive(ApiCredentialsRemovedMessage message)
    {
        if (_disposed)
        {
            return;
        }

        _isApiConfigured = false;

        _dispatcherQueue.TryEnqueue(() =>
        {
            ResetChart();
            _cachedEarnings = null;
            UpdateChartData();
            EarningsThisDaySum = DefaultPlaceholderDash;
            EarningsThisMonthSum = DefaultPlaceholderDash;
            EarningsThisYearSum = DefaultPlaceholderDash;
            ClockNextEarningsRefresh = DefaultPlaceholderDash;
            OnPropertyChanged(nameof(ChartTitle));
        });
    }

    private double[] GenerateFakeEarningsData()
    {
        var rnd = new Random(SelectedPivotIndex);
        int count = SelectedPivotIndex switch
        {
            0 => 24,
            1 => DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month),
            2 => 12,
            _ => 0
        };

        var earningsValuesForPivot = new double[count];
        for (int i = 0; i < count; i++)
        {
            earningsValuesForPivot[i] = SelectedPivotIndex switch
            {
                0 => rnd.Next(500, 801),
                1 => rnd.Next(12000, 17001),
                2 => rnd.Next(500000, 650001),
                _ => 0
            };
        }
        return earningsValuesForPivot;
    }

    private double[] GetRealEarningsData()
    {
        return SelectedPivotIndex switch
        {
            0 => _cachedEarnings!.HourlyEarnings,
            1 => _cachedEarnings!.DailyEarnings,
            2 => _cachedEarnings!.MonthlyEarnings,
            _ => []
        };
    }

    private ICartesianAxis[][] BuildXAxesForAllPivots()
    {
        var dayOfMonthLabels = new string[DaysPerMonthLabelCount];

        for (int day = 1; day <= DaysPerMonthLabelCount; day++)
        {
            dayOfMonthLabels[day - 1] = day.ToString(CultureInfo.InvariantCulture);
        }

        string monthsString = _localizationService.RetrieveString(KeyChartMonthsShort);

        string[] monthLabels = !string.IsNullOrEmpty(monthsString)
            ? monthsString.Split(',')
            : ["Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez"];

        return
        [
            [new Axis
            {
                TextSize = 12,
                LabelsRotation = 0,
                Name = _localizationService.RetrieveString(KeyChartXAxisTime)
            }],
            [new Axis
            {
                TextSize = 12,
                LabelsRotation = 0,
                Name = _localizationService.RetrieveString(KeyChartXAxisDay),
                Labels = dayOfMonthLabels
            }],
            [new Axis
            {
                TextSize = 12,
                LabelsRotation = 0,
                Name = _localizationService.RetrieveString(KeyChartXAxisMonth),
                Labels = monthLabels
            }]
        ];
    }

    [RelayCommand]
    private void GoToAPILogin() => _navigationService.NavigateTo(RouteNameOptions, parameter: 1);

    private async Task LoadDataAsync()
    {
        try
        {
            IpInfoData? ipInfo = null;

            if (!UseScreenshotFakeData)
            {
                Task<EarningsData?> earningsTask = _eVisitorApiService.RetrieveEarningsAsync();
                Task<IpInfoData?> ipInfoTask = _eVisitorApiService.RetrieveIpInfoAsync();

                await Task.WhenAll(earningsTask, ipInfoTask);

                _cachedEarnings = await earningsTask;
                ipInfo = await ipInfoTask;
            }

            _dispatcherQueue.TryEnqueue(() =>
            {
                if (ipInfo != null)
                {
                    IpAddress = ipInfo.IpAddress;
                    Host = ipInfo.Hostname;
                    CountryCode = ipInfo.CountryCode;
                    CountryName = ipInfo.CountryName;
                }

                if (UseScreenshotFakeData)
                {
                    EarningsThisDaySum = string.Format(CultureInfo.CurrentCulture, BtpDisplayFormat, 16450);
                    EarningsThisMonthSum = string.Format(CultureInfo.CurrentCulture, BtpDisplayFormat, 425800);
                    EarningsThisYearSum = string.Format(CultureInfo.CurrentCulture, BtpDisplayFormat, 5120000);
                    UpdateChartData();
                }
                else if (_cachedEarnings != null)
                {
                    EarningsThisDaySum = string.Format(CultureInfo.CurrentCulture, BtpDisplayFormat, _cachedEarnings.TodaySum);
                    EarningsThisMonthSum = string.Format(CultureInfo.CurrentCulture, BtpDisplayFormat, _cachedEarnings.MonthlySum);
                    EarningsThisYearSum = string.Format(CultureInfo.CurrentCulture, BtpDisplayFormat, _cachedEarnings.YearlySum);
                    UpdateChartData();
                }

                var now = DateTime.Now;

                CurrentMonth = now.ToString(DateFormatMonthFull, CultureInfo.CurrentCulture);
                CurrentYear = now.ToString(DateFormatYearFull, CultureInfo.CurrentCulture);

                var nextRefresh = now.AddMinutes(60 - now.Minute + EarningsRefreshTriggerMinute);
                var nextRefreshTimeFormat = _localizationService.RetrieveString(KeyGeneralNextRefresh);
                ClockNextEarningsRefresh = _isApiConfigured
                    ? string.Format(CultureInfo.CurrentCulture, nextRefreshTimeFormat, nextRefresh.ToString(DateFormatTimeHourMinute, CultureInfo.CurrentCulture))
                    : DefaultPlaceholderDash;
            });
        }
        catch (Exception ex)
        {
            LogEarningsRefreshFailed(ex);
        }
    }

    partial void OnSelectedPivotIndexChanged(int value)
    {
        UpdateChartData();
    }

    private void OnTimerTick(object? sender, object eventArgs)
    {
        if (_disposed)
        {
            return;
        }

        var now = DateTime.Now;
        if (now.Minute == EarningsRefreshTriggerMinute && now.Second == EarningsRefreshTriggerSecond)
        {
            if (now.Hour == MidnightHour)
            {
                ResetChart();
            }
            Task.Run(LoadDataAsync);
        }
    }

    private void ResetChart()
    {
        foreach (var chartValue in _chartValues)
        {
            chartValue.Value = 0;
        }
    }

    private void SyncChartValues(double[] earningsValuesForPivot)
    {
        while (_chartValues.Count < earningsValuesForPivot.Length)
        {
            _chartValues.Add(new ObservableValue(0));
        }

        while (_chartValues.Count > earningsValuesForPivot.Length)
        {
            _chartValues.RemoveAt(_chartValues.Count - 1);
        }

        for (int index = 0; index < earningsValuesForPivot.Length; index++)
        {
            double currentValue = _chartValues[index].Value ?? 0.0;

            if (Math.Abs(currentValue - earningsValuesForPivot[index]) > ChartValueChangeEpsilon)
            {
                _chartValues[index].Value = earningsValuesForPivot[index];
            }
        }
    }

    private void UpdateChartData()
    {
        if (_cachedEarnings == null && !UseScreenshotFakeData)
        {
            return;
        }

        double[] earningsValuesForPivot = UseScreenshotFakeData
            ? GenerateFakeEarningsData()
            : GetRealEarningsData();

        SyncChartValues(earningsValuesForPivot);
    }

    [LoggerMessage(
        EventId = LogEventIds.Api.EarningsRetrievalFailed,
        Level = LogLevel.Error,
        Message = "Refreshing the earnings overview failed.")]
    private partial void LogEarningsRefreshFailed(Exception exception);
}
