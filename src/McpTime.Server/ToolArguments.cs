using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace McpTime.Server;

internal static class ToolArguments
{
    /// <summary>
    /// Reads a text argument. Returns false when it is absent, JSON null or empty; a value that is not
    /// a JSON string is returned as its raw JSON text so the caller can report it as invalid.
    /// </summary>
    public static bool TryGetText(CallToolRequestParams? request, string name, [NotNullWhen(true)] out string? value)
    {
        value = null;
        if (request?.Arguments is not { } arguments || !arguments.TryGetValue(name, out var element))
        {
            return false;
        }

        value = element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            _ => element.GetRawText(),
        };
        return !string.IsNullOrEmpty(value);
    }
}
