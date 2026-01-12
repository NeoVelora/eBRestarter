using CommunityToolkit.Mvvm.ComponentModel;
using eBRestarter.Core.Application.Interfaces;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;

namespace eBRestarter.Desktop.WinUI3.ViewModels
{
    public partial class ViewModelGeneralOverView : ObservableObject
    {
        private readonly IEVisitorApiService _apiService;
        private readonly DispatcherTimer _timer;

        private readonly ObservableCollection<ObservableValue> _chartValues;
        public ObservableCollection<ISeries> Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        [ObservableProperty] public partial string ChartTitle { get; set; } = "Lade Daten...";
        [ObservableProperty] public partial string EarningsThisMonthSum { get; set; } = "-";
        [ObservableProperty] public partial string EarningsThisDaySum { get; set; } = "-";
        [ObservableProperty] public partial string ClockNextEarningsRefresh { get; set; } = "-";
        [ObservableProperty] public partial string CurrentMonth { get; set; } = DateTime.Now.ToString("MMMM");
        [ObservableProperty] public partial string CurrentDay { get; set; } = "Heute";

        [ObservableProperty] public partial string IpAddress { get; set; } = "-";
        [ObservableProperty] public partial string Host { get; set; } = "-";
        [ObservableProperty] public partial string CountryName { get; set; } = "-";

        public ViewModelGeneralOverView(IEVisitorApiService apiService)
        {
            _apiService = apiService;

            _chartValues = new ObservableCollection<ObservableValue>();
            for (int i = 0; i < 24; i++) _chartValues.Add(new ObservableValue(0));

            Series = new ObservableCollection<ISeries>
            {
                new ColumnSeries<ObservableValue>
                {
                    Values = _chartValues,
                    Name = "Punkte",
                    DataLabelsPaint = new SolidColorPaint(new SKColor(12, 142, 168)),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
                }
            };

            InitializeAxes();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            UpdateData();
        }

        private void InitializeAxes()
        {
            XAxes = [new Axis { Name = "Uhrzeit", LabelsPaint = new SolidColorPaint(new SKColor(109, 142, 168)) }];
            YAxes = [new Axis { Name = "Punkte", LabelsPaint = new SolidColorPaint(new SKColor(109, 142, 168)) }];
        }

        private async void UpdateData()
        {
            ChartTitle = $"Tagesübersicht {DateTime.Today:d}";

            var ipData = await _apiService.GetIpInfoAsync();

            if (ipData != null)
            {
                IpAddress = $"IP: {ipData.IpAddress}";
                Host = $"Host: {ipData.Hostname}";
                CountryName = $"{ipData.CountryName} ({ipData.CountryCode})";
            }

            var earnings = await _apiService.GetEarningsAsync();

            if (earnings != null)
            {
                EarningsThisDaySum = $"BTP: {earnings.TodaySum}";
                EarningsThisMonthSum = $"BTP: {earnings.MonthlySum}";

                for (int i = 0; i < 24; i++)
                {
                    if (i < earnings.HourlyEarnings.Length)
                    {
                        if (_chartValues[i].Value != earnings.HourlyEarnings[i])
                        {
                            _chartValues[i].Value = earnings.HourlyEarnings[i];
                        }
                    }
                }
            }

            var now = DateTime.Now;
            var nextRefresh = now.AddMinutes(60 - now.Minute + 5);
            ClockNextEarningsRefresh = $"Nächste Aktualisierung: {nextRefresh:HH:mm}";
        }

        private void OnTimerTick(object? sender, object e)
        {
            var now = DateTime.Now;

            if (now.Minute == 5 && now.Second == 0)
            {
                if (now.Hour == 0) ResetChart();

                UpdateData();
            }
            else if (now.Minute == 30 && now.Second == 0)
            {
                UpdateData();
            }
        }

        private void ResetChart()
        {
            foreach (var val in _chartValues) val.Value = 0;
        }
    }
}

