namespace eBRestarter.Core.Application.ObjectArchetypes.DTOs.Records;

public sealed record EarningsData(
    double[] HourlyEarnings,
    double[] DailyEarnings,
    double[] MonthlyEarnings,
    double YearlySum,
    double MonthlySum,
    double TodaySum
);