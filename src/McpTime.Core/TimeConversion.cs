using System.Diagnostics.CodeAnalysis;
using NodaTime;

namespace McpTime.Core;

/// <summary>Converts a wall-clock time on a reference date from one zone to another.</summary>
public sealed class TimeConversion(TimeProvider timeProvider)
{
    /// <summary>The current calendar date in the given zone, used when the caller gives no date.</summary>
    public ReferenceDate TodayIn(TimeZoneName zone) =>
        ReferenceDate.From(Instant.FromDateTimeOffset(timeProvider.GetUtcNow()).InZone(zone.Zone).Date);

    /// <summary>
    /// Returns false when the local time is skipped by a clock change in the source zone.
    /// An ambiguous local time resolves to its first occurrence.
    /// </summary>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "One service object offers both operations; callers hold a single instance.")]
    public bool TryConvert(
        TimeZoneName source,
        WallClockTime time,
        ReferenceDate date,
        TimeZoneName target,
        [NotNullWhen(true)] out TimeConversionResult? result)
    {
        result = null;
        var mapping = source.Zone.MapLocal(date.Value + time.Value);
        if (mapping.Count == 0)
        {
            return false;
        }

        var instant = mapping.First().ToInstant();
        var sourceResult = TimeResult.At(source, instant);
        var targetResult = TimeResult.At(target, instant);
        var difference = TimeDifference.Format(
            source.Zone.GetUtcOffset(instant),
            target.Zone.GetUtcOffset(instant));

        result = new TimeConversionResult(sourceResult, targetResult, difference);
        return true;
    }
}
