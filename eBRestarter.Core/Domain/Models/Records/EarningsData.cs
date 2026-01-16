using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Domain.Models.Records
{
    public record EarningsData(
        double[] HourlyEarnings,
        double[] DailyEarnings,
        double[] MonthlyEarnings,
        double YearlySum,
        double MonthlySum,
        double TodaySum
    );
}
