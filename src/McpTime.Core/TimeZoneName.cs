using System.Diagnostics.CodeAnalysis;
using NodaTime;

namespace McpTime.Core;

/// <summary>An IANA time zone name that exists exactly (ordinal, no normalization) in the tzdb.</summary>
public sealed class TimeZoneName
{
    private TimeZoneName(string value, DateTimeZone zone)
    {
        Value = value;
        Zone = zone;
    }

    public string Value { get; }

    internal DateTimeZone Zone { get; }

    public static bool TryCreate(string value, [NotNullWhen(true)] out TimeZoneName? name)
    {
        if (!DateTimeZoneProviders.Tzdb.Ids.Contains(value))
        {
            name = null;
            return false;
        }

        name = new TimeZoneName(value, DateTimeZoneProviders.Tzdb[value]);
        return true;
    }

    public override string ToString() => Value;
}
