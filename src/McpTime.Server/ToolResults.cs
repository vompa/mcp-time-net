using ModelContextProtocol.Protocol;

namespace McpTime.Server;

internal static class ToolResults
{
    public static CallToolResult NotImplemented(string toolName) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = $"Tool '{toolName}' is not implemented yet." }],
    };
}
