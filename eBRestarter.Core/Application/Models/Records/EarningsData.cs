namespace eBRestarter.Core.Application.Models.Records;

public record EarningsData(
    double[] HourlyEarnings,
    double[] DailyEarnings,
    double[] MonthlyEarnings,
    double YearlySum,
    double MonthlySum,
    double TodaySum
);
