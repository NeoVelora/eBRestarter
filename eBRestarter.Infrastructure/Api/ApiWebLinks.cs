using System.Globalization;

namespace eBRestarter.Infrastructure.Api;

public static class ApiWebLinks
{
    public const string IpLink = "https://www.ebesucher.de/api/ip.json/data";

    public const string EarningsThisMonth = "https://www.ebesucher.de/api/visitor_exchange.json/account/earnings/";

    public static string HourlyEarnings
    {
        get
        {
            var dateStr = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return $"https://www.ebesucher.de/api/visitor_exchange.json/account/earnings_hourly/{dateStr}?timezone=Europe%2FBerlin";
        }
    }


}

