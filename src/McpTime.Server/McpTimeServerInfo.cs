using System.Reflection;
using ModelContextProtocol.Protocol;

namespace McpTime.Server;

internal static class McpTimeServerInfo
{
    public const string ServerName = "mcp-time";

    public static Implementation Create() => new() { Name = ServerName, Version = ReadVersion() };

    private static string ReadVersion()
    {
        var assembly = typeof(McpTimeServerInfo).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(informational))
        {
            var plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus < 0 ? informational : informational[..plus];
        }

        var version = assembly.GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
