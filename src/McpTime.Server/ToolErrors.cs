using ModelContextProtocol.Protocol;

namespace McpTime.Server;

internal static class ToolErrors
{
    public static CallToolResult MissingArgument(string argument) => ToolResults.Error(
        $"Missing required argument: {argument}. Expected an IANA time zone name such as 'Europe/Vienna'.");

    public static CallToolResult InvalidTimeZone(string argument, string value) => ToolResults.Error(
        $"Invalid timezone '{value}' in argument '{argument}'. Expected an exact IANA time zone name such as 'Europe/Vienna'; names are case-sensitive.");
}
