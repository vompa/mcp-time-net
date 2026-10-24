using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using NodaTime;

namespace McpTime.Core;

/// <summary>A wall-clock time of day in <c>H:M</c> or <c>HH:MM</c> form (24-hour, ASCII digits only).</summary>
public sealed partial class WallClockTime
{
    private WallClockTime(LocalTime value) => Value = value;

    internal LocalTime Value { get; }

    public static bool TryParse(string text, [NotNullWhen(true)] out WallClockTime? time)
    {
        time = null;
        var match = Shape().Match(text);
        if (!match.Success)
        {
            return false;
        }

        var hour = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var minute = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        if (hour > 23 || minute > 59)
        {
            return false;
        }

        time = new WallClockTime(new LocalTime(hour, minute));
        return true;
    }

    public override string ToString() => Value.ToString("HH':'mm", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"\A([0-9]{1,2}):([0-9]{1,2})\z")]
    private static partial Regex Shape();
}
