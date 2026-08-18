using System;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Utilities;

public static class HumanReadableTimeUtility
{
    private const int MinimumThreshold = 1;
    private const string ZeroSecondsText = "0s";

    public static string Format(int seconds)
    {
        if (seconds <= 0)
        {
            return ZeroSecondsText;
        }

        TimeSpan time = TimeSpan.FromSeconds(seconds);
        int totalHours = (int)time.TotalHours;

        if (totalHours >= MinimumThreshold)
        {
            return $"{totalHours}h: {time.Minutes}m: {time.Seconds}s";
        }

        if (time.TotalMinutes >= MinimumThreshold)
        {
            return $"{time.Minutes}m: {time.Seconds}s";
        }

        return $"{time.Seconds}s";
    }
}
