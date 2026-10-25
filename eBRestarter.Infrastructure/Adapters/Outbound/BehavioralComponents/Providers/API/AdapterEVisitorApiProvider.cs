using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using eBRestarter.Core.Application.ObjectArchetypes.DTOs.System;
using eBRestarter.Core.Application.Ports.Outbound.Network;
using eBRestarter.Core.Application.Ports.Outbound.Config;

using eBRestarter.Infrastructure.BehavioralComponents.Clients.Http;
using eBRestarter.Infrastructure.BehavioralComponents.Utilities;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;
using eBRestarter.Infrastructure.ObjectArchetypes.Models;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Infrastructure.Adapters.Outbound.BehavioralComponents.Providers.API;

public sealed partial class AdapterEVisitorApiProvider(
    IOutboundPortEVisitorConfigRepository configPort,
    ILogger<AdapterEVisitorApiProvider> logger,
    IRestClient restClientPort,
    TimeProvider? timeProvider = null) : IOutboundPortEVisitorApiProvider
{
    private const int HoursInDayCount = 24;
    private const int MonthsInYearCount = 12;

    private readonly IOutboundPortEVisitorConfigRepository _configPort = configPort ?? throw new ArgumentNullException(nameof(configPort));
    private readonly ILogger<AdapterEVisitorApiProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IRestClient _restClientPort = restClientPort ?? throw new ArgumentNullException(nameof(restClientPort));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;


    public async Task<EarningsData?> RetrieveEarningsAsync()
    {
        var config = _configPort.LoadConfig();

        if (string.IsNullOrWhiteSpace(config.Settings.ApiUsername)
            || string.IsNullOrWhiteSpace(config.Settings.ApiKey))
        {
            return null;
        }

        var username = config.Settings.ApiUsername;
        var apiKey = config.Settings.ApiKey;
        var now = _timeProvider.GetLocalNow().DateTime;

        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local);

        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);

        try
        {
            var tHourly = RetrieveHourlyEarningsRawAsync(username, apiKey);

            var tMonthlyDetails = RetrieveDailyEarningsRawAsync(username, apiKey, startOfMonth, now);

            var tYearlyDetails = RetrieveMonthlyEarningsRawAsync(username, apiKey, startOfYear, now);

            await Task.WhenAll(tHourly, tMonthlyDetails, tYearlyDetails).ConfigureAwait(false);
            var hourlyArray = await tHourly.ConfigureAwait(false);
            var dailyArray = await tMonthlyDetails.ConfigureAwait(false);
            var monthlyArray = await tYearlyDetails.ConfigureAwait(false);

            var monthlySum = dailyArray.Sum();
            var yearlySum = monthlyArray.Sum();
            var todaySum = hourlyArray.Sum();

            return new EarningsData(hourlyArray, dailyArray, monthlyArray, yearlySum, monthlySum, todaySum);
        }
        catch (Exception exception)
        {
            LogEarningsRetrievalFailed(_logger, exception);
            return null;
        }
    }

    public async Task<IpInfoData?> RetrieveIpInfoAsync()
    {
        var request = new ApiRequest
        {
            Url = ApiWebLinks.IpLink,
            Method = ObjectArchetypes.Enums.HttpMethod.GET
        };

        var response = await _restClientPort.ExecuteGetAsync(request).ConfigureAwait(false);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
        {
            return null;
        }

        try
        {
            return EVisitorApiResponseUtility.ParseIpInfo(response.Content, _logger);
        }
        catch (Exception exception)
        {
            LogResponseParsingFailed(_logger, exception, "IP data");
            return null;
        }
    }

    private async Task<double[]> RetrieveDailyEarningsRawAsync(
        string username,
        string apiKey,
        DateTime start,
        DateTime end)
    {
        var unixStart = ((DateTimeOffset)start).ToUnixTimeSeconds();
        var unixEnd = ((DateTimeOffset)end).ToUnixTimeSeconds();

        string url = $"{ApiWebLinks.EarningsThisMonth}{unixStart}-{unixEnd}";

        var request = new ApiRequest
        {
            Url = url,
            Method = ObjectArchetypes.Enums.HttpMethod.GET,
            Username = username,
            Password = apiKey
        };

        var daysInMonth = DateTime.DaysInMonth(start.Year, start.Month);

        var response = await _restClientPort.ExecuteGetAsync(request).ConfigureAwait(false);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
        {
            return new double[daysInMonth];
        }

        try
        {
            return EVisitorApiResponseUtility.ParseDailyEarnings(response.Content, daysInMonth, _logger);
        }
        catch (Exception exception)
        {
            LogResponseParsingFailed(_logger, exception, "daily earnings");
            return new double[daysInMonth];
        }
    }

    private async Task<double[]> RetrieveHourlyEarningsRawAsync(string username, string apiKey)
    {
        var request = new ApiRequest
        {
            Url = ApiWebLinks.GetHourlyEarnings(_timeProvider),
            Method = ObjectArchetypes.Enums.HttpMethod.GET,
            Username = username,
            Password = apiKey
        };

        var response = await _restClientPort.ExecuteGetAsync(request).ConfigureAwait(false);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
        {
            return new double[HoursInDayCount];
        }

        try
        {
            return EVisitorApiResponseUtility.ParseHourlyEarnings(response.Content, _logger);
        }
        catch (Exception exception)
        {
            LogResponseParsingFailed(_logger, exception, "hourly earnings");
            return new double[HoursInDayCount];
        }
    }

    private async Task<double[]> RetrieveMonthlyEarningsRawAsync(
        string username,
        string apiKey,
        DateTime start,
        DateTime end)
    {
        var unixStart = ((DateTimeOffset)start).ToUnixTimeSeconds();
        var unixEnd = ((DateTimeOffset)end).ToUnixTimeSeconds();

        string url = $"{ApiWebLinks.EarningsThisMonth}{unixStart}-{unixEnd}";

        var request = new ApiRequest
        {
            Url = url,
            Method = ObjectArchetypes.Enums.HttpMethod.GET,
            Username = username,
            Password = apiKey
        };

        var response = await _restClientPort.ExecuteGetAsync(request).ConfigureAwait(false);

        if (!response.IsSuccess || string.IsNullOrEmpty(response.Content))
        {
            return new double[MonthsInYearCount];
        }

        try
        {
            return EVisitorApiResponseUtility.ParseMonthlyEarnings(response.Content, _logger);
        }
        catch (Exception exception)
        {
            LogResponseParsingFailed(_logger, exception, "monthly earnings");
            return new double[MonthsInYearCount];
        }
    }


    [LoggerMessage(
        EventId = LogEventIds.Api.EarningsRetrievalFailed,
        Level = LogLevel.Error,
        Message = "Error retrieving earnings (overall process).")]
    private static partial void LogEarningsRetrievalFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = LogEventIds.Api.ApiResponseParsingFailed,
        Level = LogLevel.Error,
        Message = "Error parsing {DataType}.")]
    private static partial void LogResponseParsingFailed(ILogger logger, Exception exception, string dataType);
}
