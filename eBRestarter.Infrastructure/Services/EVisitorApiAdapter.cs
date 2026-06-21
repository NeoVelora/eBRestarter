using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Application.Interfaces.Config;
using eBRestarter.Core.Application.Interfaces.RestClient;
using eBRestarter.Core.Application.Models.Api;
using eBRestarter.Core.Application.Models.Records;
using eBRestarter.Core.Application.Constants;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace eBRestarter.Infrastructure.Services;

public class EVisitorApiAdapter(
    IRestClientUseCase restClientUseCase,
    IEVisitorConfigService configService,
    ILogger<EVisitorApiAdapter> logger) : IEVisitorApiService
{
    private readonly IRestClientUseCase _restClientUseCase = restClientUseCase;
    private readonly IEVisitorConfigService _configService = configService;
    private readonly ILogger<EVisitorApiAdapter> _logger = logger;

    public async Task<IpInfoData?> RetrieveIpInfoAsync()
    {
        var request = new ApiRequest
        {
            Url = ApiWebLinks.IpLink,
            Method = Core.Application.Enums.HttpMethod.GET
        };

        var response = await _restClientUseCase.ExecuteGetAsync(request);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
            var root = doc.RootElement;

            return new IpInfoData(
                IpAddress: RetrieveStringSafe(root, "ip"),
                Hostname: RetrieveStringSafe(root, "host"),
                CountryCode: RetrieveStringSafe(root, "countryCode"),
                CountryName: RetrieveStringSafe(root, "countryName")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Parsen der IP Daten");
            return null;
        }
    }

    public async Task<EarningsData?> RetrieveEarningsAsync()
    {
        var config = _configService.LoadConfig();

        if (string.IsNullOrWhiteSpace(config.Settings.ApiUsername)
            || string.IsNullOrWhiteSpace(config.Settings.ApiKey))
        {
            return null;
        }

        var username = config.Settings.ApiUsername;
        var apiKey = config.Settings.ApiKey;
        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local);

        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);

        try
        {
            var tHourly = RetrieveHourlyEarningsRawAsync(username, apiKey);

            var tMonthlyDetails = RetrieveDailyEarningsRawAsync(username, apiKey, startOfMonth, now);

            var tYearlyDetails = RetrieveMonthlyEarningsRawAsync(username, apiKey, startOfYear, now);

            await Task.WhenAll(tHourly, tMonthlyDetails, tYearlyDetails);
            var hourlyArray = tHourly.Result;
            var dailyArray = tMonthlyDetails.Result;
            var monthlyArray = tYearlyDetails.Result;

            var monthlySum = dailyArray.Sum();
            var yearlySum = monthlyArray.Sum();
            var todaySum = hourlyArray.Sum();

            return new EarningsData(hourlyArray, dailyArray, monthlyArray , yearlySum, monthlySum, todaySum);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der Earnings (Gesamtprozess).");
            return null;
        }
    }

    private async Task<double[]> RetrieveHourlyEarningsRawAsync(string username, string apiKey)
    {
        var request = new ApiRequest
        {
            Url = ApiWebLinks.HourlyEarnings,
            Method = Core.Application.Enums.HttpMethod.GET,
            Username = username,
            Password = apiKey
        };

        var response = await _restClientUseCase.ExecuteGetAsync(request);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
            return new double[24];

        try
        {
            return ParseHourlyEarningsJson(response.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Parsen der Hourly Earnings.");
            return new double[24];
        }
    }

    private static double[] ParseHourlyEarningsJson(string content)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        double[] hourly = new double[24];

        if (root.ValueKind == JsonValueKind.Object)
        {
            ParseHourlyEarningsFromJsonObject(root, hourly);
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            ParseHourlyEarningsFromJsonArray(root, hourly);
        }

        return hourly;
    }

    private static void ParseHourlyEarningsFromJsonObject(JsonElement root, double[] hourly)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (int.TryParse(property.Name, out int hour) && hour >= 1 && hour <= 24)
            {
                hourly[hour - 1] = property.Value.TryGetDouble(out double val) ? Math.Round(val, 2) : 0.0;
            }
        }
    }

    private static void ParseHourlyEarningsFromJsonArray(JsonElement root, double[] hourly)
    {
        var parsedArray = root.EnumerateArray()
            .Select(x => x.TryGetDouble(out double val) ? Math.Round(val, 2) : 0.0)
            .ToArray();

        Array.Copy(parsedArray, hourly, Math.Min(parsedArray.Length, 24));
    }

    private async Task<double[]> RetrieveDailyEarningsRawAsync(string username, string apiKey, DateTime start, DateTime end)
    {
        var unixStart = ((DateTimeOffset)start).ToUnixTimeSeconds();
        var unixEnd = ((DateTimeOffset)end).ToUnixTimeSeconds();

        string url = $"{ApiWebLinks.EarningsThisMonth}{unixStart}-{unixEnd}";

        var request = new ApiRequest
        {
            Url = url,
            Method = Core.Application.Enums.HttpMethod.GET,
            Username = username,
            Password = apiKey
        };

        var daysInMonth = DateTime.DaysInMonth(start.Year, start.Month);
        var dailyEarnings = new double[daysInMonth];

        var response = await _restClientUseCase.ExecuteGetAsync(request);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
            return dailyEarnings;

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
            ParseDailyEarningsFromJsonArray(doc.RootElement, dailyEarnings, daysInMonth);

            for (int i = 0; i < dailyEarnings.Length; i++)
            {
                dailyEarnings[i] = Math.Round(dailyEarnings[i], 2);
            }

            return dailyEarnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Parsen der Daily Earnings.");
            return dailyEarnings;
        }
    }

    private async Task<double[]> RetrieveMonthlyEarningsRawAsync(string username, string apiKey, DateTime start, DateTime end)
    {
        var unixStart = ((DateTimeOffset)start).ToUnixTimeSeconds();
        var unixEnd = ((DateTimeOffset)end).ToUnixTimeSeconds();

        string url = $"{ApiWebLinks.EarningsThisMonth}{unixStart}-{unixEnd}";

        var request = new ApiRequest
        {
            Url = url,
            Method = Core.Application.Enums.HttpMethod.GET,
            Username = username,
            Password = apiKey
        };

        var monthlyEarnings = new double[12];

        var response = await _restClientUseCase.ExecuteGetAsync(request);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
            return monthlyEarnings;

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
            ParseMonthlyEarningsFromJsonArray(doc.RootElement, monthlyEarnings);

            for (int i = 0; i < monthlyEarnings.Length; i++)
            {
                monthlyEarnings[i] = Math.Round(monthlyEarnings[i], 2);
            }

            return monthlyEarnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Parsen der Monthly Earnings.");
            return monthlyEarnings;
        }
    }

    private static void ParseDailyEarningsFromJsonArray(JsonElement root, double[] dailyEarnings, int daysInMonth)
    {
        if (root.ValueKind != JsonValueKind.Array) return;

        foreach (var item in root.EnumerateArray())
        {
            if (item.TryGetProperty("from_w3c", out JsonElement dateProp))
            {
                string dateStr = dateProp.GetString() ?? "";

                if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date))
                {
                    int dayIndex = date.Day - 1;
                    if (dayIndex >= 0 && dayIndex < daysInMonth)
                    {
                        dailyEarnings[dayIndex] += RetrieveValueSafe(item);
                    }
                }
            }
        }
    }

    private static void ParseMonthlyEarningsFromJsonArray(JsonElement root, double[] monthlyEarnings)
    {
        if (root.ValueKind != JsonValueKind.Array) return;

        foreach (var item in root.EnumerateArray())
        {
            if (item.TryGetProperty("from_w3c", out JsonElement dateProp))
            {
                string dateStr = dateProp.GetString() ?? "";

                if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date))
                {
                    int monthIndex = date.Month - 1;
                    if (monthIndex >= 0 && monthIndex < 12)
                    {
                        monthlyEarnings[monthIndex] += RetrieveValueSafe(item);
                    }
                }
            }
        }
    }

    private static double RetrieveValueSafe(JsonElement item)
    {
        if (item.TryGetProperty("value", out JsonElement valProp))
        {
            if (valProp.ValueKind == JsonValueKind.Number)
            {
                return valProp.GetDouble();
            }
            else if (valProp.ValueKind == JsonValueKind.String
                      && double.TryParse(valProp.GetString()?.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double dVal))
            {
                return dVal;
            }
        }
        return 0.0;
    }

    private static string RetrieveStringSafe(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement prop)
            && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? "-";
        }
        return "-";
    }
}

