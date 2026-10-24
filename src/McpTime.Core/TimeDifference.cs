using System.Globalization;
using NodaTime;

namespace McpTime.Core;

/// <summary>Formats the offset difference between two zones as signed hours such as <c>+5.75h</c>.</summary>
internal static class TimeDifference
{
    private const decimal SecondsPerHour = 3600m;

    internal static string Format(Offset source, Offset target)
    {
        var seconds = target.Seconds - source.Seconds;
        var hours = Math.Round(seconds / SecondsPerHour, 2, MidpointRounding.AwayFromZero);
        var number = hours == Math.Truncate(hours)
            ? Math.Abs(hours).ToString("0.0", CultureInfo.InvariantCulture)
            : Math.Abs(hours).ToString("0.##", CultureInfo.InvariantCulture);
        return (hours < 0 ? "-" : "+") + number + "h";
    }
}
