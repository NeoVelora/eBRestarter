using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Desktop.WinUI3.Messages;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelGeneralOverview : ObservableObject
    {
        private const double ChartValueChangeEpsilon = 0.0001;

        private const int ColumnSeriesCornerRadius = 5;

        private const int EarningsRefreshTriggerMinute = 5;

        private const int EarningsRefreshTriggerSecond = 0;

        private const int InitialHourlyChartSlotCount = 24;

        private const int MidnightHour = 0;

        private const int TimerTickIntervalSeconds = 1;

        private const int YAxisMinStep = 100;

        private readonly IEVisitorApiService _eVisitorApiService;

        private readonly ILocalizationService _localizationService;

        private EarningsData? _cachedEarnings;

        private readonly ObservableCollection<ObservableValue> _chartValues;

        private readonly DispatcherQueue _dispatcherQueue;

        private readonly ColumnSeries<ObservableValue> _mainColumnSeries;

        private readonly DispatcherTimer _timer;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(XAxes))]
        [NotifyPropertyChangedFor(nameof(ChartTitle))]
        public partial int SelectedPivotIndex { get; set; } = 0;

        [ObservableProperty]
        public partial string ClockNextEarningsRefresh { get; set; } = "-";

        [ObservableProperty]
        public partial string CountryCode { get; set; } = "-";

        [ObservableProperty]
        public partial string CountryName { get; set; } = "-";

        [ObservableProperty]
        public partial string CurrentDay { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string CurrentMonth { get; set; } = DateTime.Now.ToString("MMMM");

        [ObservableProperty]
        public partial string CurrentYear { get; set; } = DateTime.Now.ToString("yyyy");

        [ObservableProperty]
        public partial string EarningsThisDaySum { get; set; } = "-";

        [ObservableProperty]
        public partial string EarningsThisMonthSum { get; set; } = "-";

        [ObservableProperty]
        public partial string EarningsThisYearSum { get; set; } = "-";

        [ObservableProperty]
        public partial string Host { get; set; } = "-";

        [ObservableProperty]
        public partial string IpAddress { get; set; } = "-";

        [ObservableProperty]
        public partial ObservableCollection<ISeries> Series { get; set; } = null!;

        public string ChartTitle => SelectedPivotIndex switch
        {
            0 => string.Format(_localizationService.GetString("Chart_TitleHourly"), DateTime.Now.ToString("dd.MM.yyyy")),
            1 => string.Format(_localizationService.GetString("Chart_TitleDaily"), DateTime.Now.ToString("MMMM")),
            2 => string.Format(_localizationService.GetString("Chart_TitleYearly"), DateTime.Now.ToString("yyyy")),
            _ => _localizationService.GetString("Chart_TitleOverview")
        };

        public Axis[] XAxes => GetXAxesForCurrentPivot();

        public Axis[] YAxes { get; set; } = null!;

        public ViewModelGeneralOverview(
            IEVisitorApiService eVisitorApiService,
            ILocalizationService localizationService)
        {
            ArgumentNullException.ThrowIfNull(eVisitorApiService);
            ArgumentNullException.ThrowIfNull(localizationService);

            _eVisitorApiService = eVisitorApiService;
            _localizationService = localizationService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            CurrentDay = _localizationService.GetString("General_Today");

            YAxes =
            [
                new Axis
                {
                    Name = _localizationService.GetString("Chart_YAxisPoints"),
                    LabelsDensity = 1,
                    SeparatorsPaint = new SolidColorPaint(new SKColor(40, 40, 40)) { StrokeThickness = 1 },
                    MinStep = YAxisMinStep,
                    TextSize = 12
                }
            ];

            _chartValues = [];

            for (int index = 0; index < InitialHourlyChartSlotCount; index++)
                _chartValues.Add(new ObservableValue(0));

            _mainColumnSeries = new ColumnSeries<ObservableValue>
            {
                Values = _chartValues,
                Name = _localizationService.GetString("Chart_SeriesEarnings"),
                Rx = ColumnSeriesCornerRadius,
                Ry = ColumnSeriesCornerRadius,
                Fill = new SolidColorPaint(new SKColor(0, 120, 215)),
                DataLabelsPaint = new SolidColorPaint(new SKColor(12, 142, 168)),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
            };

            Series = [_mainColumnSeries];

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(TimerTickIntervalSeconds) };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            Task.Run(LoadDataAsync);

            WeakReferenceMessenger.Default.Register<ApiCredentialsUpdatedMessage>(this, (_, __) => Task.Run(LoadDataAsync));

            WeakReferenceMessenger.Default.Register<ApiCredentialsRemovedMessage>(this, (_, __) =>
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    ResetChart();
                    _cachedEarnings = null;
                    UpdateChartData();
                    EarningsThisDaySum = "-";
                    EarningsThisMonthSum = "-";
                    EarningsThisYearSum = "-";
                });
            });
        }

        private Axis[] GetXAxesForCurrentPivot()
        {
            var xAxis = new Axis { TextSize = 12, LabelsRotation = 0 };

            switch (SelectedPivotIndex)
            {
                case 0:
                    xAxis.Name = _localizationService.GetString("Chart_XAxisTime");
                    break;

                case 1:
                    xAxis.Name = _localizationService.GetString("Chart_XAxisDay");
                    xAxis.Labels = [.. Enumerable.Range(1, 31).Select(index => index.ToString())];
                    break;

                case 2:
                    xAxis.Name = _localizationService.GetString("Chart_XAxisMonth");
                    string monthsString = _localizationService.GetString("Chart_MonthsShort");
                    xAxis.Labels = !string.IsNullOrEmpty(monthsString)
                        ? monthsString.Split(',')
                        : ["Jan", "Feb", "Mär", "Apr", "Mai", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dez"];
                    break;
            }

            return [xAxis];
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Task<EarningsData?> earningsTask = _eVisitorApiService.GetEarningsAsync();
                Task<IpInfoData?> ipInfoTask = _eVisitorApiService.GetIpInfoAsync();

                await Task.WhenAll(earningsTask, ipInfoTask);

                _cachedEarnings = await earningsTask;
                IpInfoData? ipInfo = await ipInfoTask;

                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (ipInfo != null)
                    {
                        IpAddress = ipInfo.IpAddress;
                        Host = ipInfo.Hostname;
                        CountryCode = ipInfo.CountryCode;
                        CountryName = ipInfo.CountryName;
                    }

                    if (_cachedEarnings != null)
                    {
                        EarningsThisDaySum = $"BTP: {_cachedEarnings.TodaySum:N0}";
                        EarningsThisMonthSum = $"BTP: {_cachedEarnings.MonthlySum:N0}";
                        EarningsThisYearSum = $"BTP: {_cachedEarnings.YearlySum:N0}";
                        UpdateChartData();
                    }

                    var now = DateTime.Now;

                    CurrentMonth = now.ToString("MMMM");
                    CurrentYear = now.ToString("yyyy");

                    var nextRefresh = now.AddMinutes(60 - now.Minute + EarningsRefreshTriggerMinute);
                    var nextRefreshTimeFormat = _localizationService.GetString("General_NextRefresh");
                    ClockNextEarningsRefresh = string.Format(nextRefreshTimeFormat, nextRefresh.ToString("HH:mm"));
                });
            }
            catch (OperationCanceledException ex)
            {
                Debug.WriteLine(ex);
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        partial void OnSelectedPivotIndexChanged(int value)
        {
            UpdateChartData();
        }

        private void OnTimerTick(object? sender, object eventArgs)
        {
            var now = DateTime.Now;
            if (now.Minute == EarningsRefreshTriggerMinute && now.Second == EarningsRefreshTriggerSecond)
            {
                if (now.Hour == MidnightHour) ResetChart();
                Task.Run(LoadDataAsync);
            }
        }

        private void ResetChart()
        {
            foreach (var chartValue in _chartValues) chartValue.Value = 0;
        }

        private void UpdateChartData()
        {
            if (_cachedEarnings == null) return;

            double[] earningsValuesForPivot = SelectedPivotIndex switch
            {
                0 => _cachedEarnings.HourlyEarnings,
                1 => _cachedEarnings.DailyEarnings,
                2 => _cachedEarnings.MonthlyEarnings,
                _ => []
            };

            while (_chartValues.Count < earningsValuesForPivot.Length)
                _chartValues.Add(new ObservableValue(0));

            while (_chartValues.Count > earningsValuesForPivot.Length)
                _chartValues.RemoveAt(_chartValues.Count - 1);

            for (int index = 0; index < earningsValuesForPivot.Length; index++)
            {
                double currentValue = _chartValues[index].Value ?? 0.0;

                if (Math.Abs(currentValue - earningsValuesForPivot[index]) > ChartValueChangeEpsilon)
                    _chartValues[index].Value = earningsValuesForPivot[index];
            }
        }
    }
}
