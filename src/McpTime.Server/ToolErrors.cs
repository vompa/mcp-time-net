using McpTime.Core;
using ModelContextProtocol.Protocol;

namespace McpTime.Server;

internal static class ToolErrors
{
    public static CallToolResult MissingTimeZone(string argument)
        => Missing(argument, "an IANA time zone name such as 'Europe/Vienna'");

    public static CallToolResult MissingTime()
        => Missing("time", "a 24-hour time in HH:MM format such as '14:30'");

    public static CallToolResult InvalidTimeZone(string argument, string value) => ToolResults.Error(
        $"Invalid timezone '{value}' in argument '{argument}'. Expected an exact IANA time zone name such as 'Europe/Vienna'; names are case-sensitive.");

    public static CallToolResult InvalidTime(string value) => ToolResults.Error(
        $"Invalid time format. Expected HH:MM [24-hour format] such as '14:30'; got '{value}' in argument 'time'.");

    public static CallToolResult InvalidDate(string value) => ToolResults.Error(
        $"Invalid date '{value}' in argument 'date'. Expected a calendar date in YYYY-MM-DD format between 0001-01-02 and 9999-12-29, such as '2024-03-10'.");

    public static CallToolResult SkippedLocalTime(WallClockTime time, TimeZoneName zone, ReferenceDate date) => ToolResults.Error(
        $"The time '{time}' does not exist in '{zone}' on {date} because it is skipped by a clock change. Use a time before or after the change.");

    private static CallToolResult Missing(string argument, string expected)
        => ToolResults.Error($"Missing required argument: {argument}. Expected {expected}.");
}
