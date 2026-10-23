using ModelContextProtocol.Protocol;

namespace McpTime.Server;

internal static class ToolDefinitions
{
    public static Tool GetCurrentTime(string localTimeZone) => new()
    {
        Name = "get_current_time",
        Description = "Returns the current date and time in an IANA time zone, including the UTC offset, the English weekday and whether daylight saving time is in effect.",
        InputSchema = ToolSchemas.GetCurrentTimeInput(localTimeZone),
        OutputSchema = ToolSchemas.TimeResultOutput(),
        Annotations = ReadOnlyAnnotations(),
    };

    public static Tool ConvertTime(string localTimeZone) => new()
    {
        Name = "convert_time",
        Description = "Converts a wall-clock time from one IANA time zone to another and reports the offset difference in hours. Handles daylight saving time, quarter-hour offsets and date boundaries.",
        InputSchema = ToolSchemas.ConvertTimeInput(localTimeZone),
        OutputSchema = ToolSchemas.TimeConversionResultOutput(),
        Annotations = ReadOnlyAnnotations(),
    };

    public static ToolAnnotations ReadOnlyAnnotations() => new()
    {
        ReadOnlyHint = true,
        DestructiveHint = false,
        IdempotentHint = true,
        OpenWorldHint = false,
    };
}
