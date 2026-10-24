using NodaTime;
using NodaTime.Text;

namespace McpTime.Core;

/// <summary>A moment as seen in one time zone.</summary>
public sealed record TimeResult(string Timezone, string Datetime, string DayOfWeek, bool IsDst)
{
    private static readonly OffsetDateTimePattern s_minutePattern =
        OffsetDateTimePattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd'T'HH':'mm':'sso<+HH:mm>");

    private static readonly OffsetDateTimePattern s_secondPattern =
        OffsetDateTimePattern.CreateWithInvariantCulture("uuuu'-'MM'-'dd'T'HH':'mm':'sso<+HH:mm:ss>");

    internal static TimeResult At(TimeZoneName zone, Instant instant)
    {
        var local = instant.InZone(zone.Zone);
        var moment = local.ToOffsetDateTime();
        var pattern = moment.Offset.Seconds % 60 == 0 ? s_minutePattern : s_secondPattern;
        return new TimeResult(
            zone.Value,
            pattern.Format(moment),
            local.DayOfWeek.ToString(),
            local.IsDaylightSavingTime());
    }
}
