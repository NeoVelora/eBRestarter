using System.Globalization;

namespace eBRestarter.Core.Application.BehavioralComponents.Extensions;

public static class Int64Extensions
{
    private static readonly string[] _sizeSuffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

    public static string ToSizeSuffix(this long value, int decimalPlaces = 1, IFormatProvider? formatProvider = null)
    {
        formatProvider ??= CultureInfo.CurrentCulture;

        if (value < 0) { return "-" + ToSizeSuffix(-value, decimalPlaces, formatProvider); }
        if (value == 0) { return "0 B"; }

        int mag = (int)Math.Log(value, 1024);

        if (mag >= _sizeSuffixes.Length)
        {
            mag = _sizeSuffixes.Length - 1;
        }

        if (mag == 0)
        {
            return $"{value} {_sizeSuffixes[0]}";
        }

        decimal adjustedSize = (decimal)(value / Math.Pow(1024, mag));

        string numberFormat = $"n{decimalPlaces}";

        return $"{adjustedSize.ToString(numberFormat, formatProvider)} {_sizeSuffixes[mag]}";
    }
}