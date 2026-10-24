using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using McpTime.Core;

namespace McpTime.Host;

/// <summary>Resolves the local time zone: option value, else system zone, else UTC (PRD F4, D4, D5).</summary>
internal static class LocalTimeZone
{
    private const string Fallback = "UTC";

    public static bool TryResolve(
        string? optionValue,
        Func<TimeZoneName> detect,
        [NotNullWhen(true)] out TimeZoneName? zone)
    {
        if (string.IsNullOrEmpty(optionValue))
        {
            zone = detect();
            return true;
        }

        return TimeZoneName.TryCreate(optionValue, out zone);
    }

    public static TimeZoneName Detect(string systemId, string region)
    {
        if (TimeZoneName.TryCreate(systemId, out var direct))
        {
            return direct;
        }

        if (TryConvertWindowsId(systemId, region, out var iana) && TimeZoneName.TryCreate(iana, out var mapped))
        {
            return mapped;
        }

        return TimeZoneName.TryCreate(Fallback, out var utc)
            ? utc
            : throw new InvalidOperationException($"The fallback zone '{Fallback}' is missing from tzdb.");
    }

    public static TimeZoneName DetectFromSystem()
        => Detect(TimeZoneInfo.Local.Id, RegionInfo.CurrentRegion.TwoLetterISORegionName);

    private static bool TryConvertWindowsId(string windowsId, string region, [NotNullWhen(true)] out string? iana)
    {
        if (region.Length > 0 && TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsId, region, out iana))
        {
            return true;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(windowsId, out iana);
    }
}
