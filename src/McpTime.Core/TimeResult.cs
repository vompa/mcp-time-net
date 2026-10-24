using NodaTime;
using NodaTime.Text;

namespace McpTime.Core;

/// <summary>A moment as seen in one time zone.</summary>
public sealed record TimeResult(string Timezone, string Datetime, string DayOfWeek, bool IsDst)
{
    private static readonly OffsetDateTimePattern s_pattern =
        OffsetDateTimePattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd'T'HH':'mm':'sso<+HH:mm>");

    internal static TimeResult At(TimeZoneName zone, Instant instant)
    {
        var local = instant.InZone(zone.Zone);
        return new TimeResult(
            zone.Value,
            s_pattern.Format(local.ToOffsetDateTime()),
            local.DayOfWeek.ToString(),
            local.IsDaylightSavingTime());
    }
}
