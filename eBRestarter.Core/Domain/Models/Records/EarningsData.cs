using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Domain.Models.Records
{
    public record EarningsData(
        double[] HourlyEarnings,
        double MonthlySum,
        double TodaySum
    );
}
