using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace McpTime.Server;

internal static class ToolResults
{
    public static CallToolResult Success(JsonElement structured) => new()
    {
        IsError = false,
        StructuredContent = structured,
        Content = [new TextContentBlock { Text = structured.GetRawText() }],
    };

    public static CallToolResult Error(string message) => new()
    {
        IsError = true,
        Content = [new TextContentBlock { Text = message }],
    };
}
