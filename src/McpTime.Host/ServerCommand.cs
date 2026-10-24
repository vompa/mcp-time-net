using System.CommandLine;
using System.Globalization;
using System.Text;

namespace McpTime.Host;

/// <summary>Command line of the server: validates the local time zone before the transport starts (PRD F4).</summary>
internal static class ServerCommand
{
    private const string Description =
        "MCP server (stdio) with two read-only tools: get_current_time returns the current time in an IANA "
        + "time zone, convert_time converts a local time between two time zones.";

    private const string OptionDescription =
        "IANA time zone name used as the default for the tools, for example Europe/Vienna. "
        + "Default: the system time zone, otherwise UTC.";

    public static RootCommand Create()
    {
        var localTimeZone = new Option<string?>("--local-timezone") { Description = OptionDescription };
        var root = new RootCommand(Description) { localTimeZone };
        root.SetAction((parseResult, ct) => RunAsync(parseResult.GetValue(localTimeZone), ct));
        return root;
    }

    private static async Task<int> RunAsync(string? value, CancellationToken ct)
    {
        if (!LocalTimeZone.TryResolve(value, LocalTimeZone.DetectFromSystem, out var zone))
        {
            await Console.Error.WriteLineAsync(
                $"Error: invalid --local-timezone '{Escape(value)}': not a known IANA timezone name");
            return 1;
        }

        await McpTimeHost.RunAsync(zone, ct);
        return 0;
    }

    /// <summary>Keeps the error on one line by writing control characters as \uXXXX.</summary>
    private static string Escape(string? value)
    {
        var escaped = new StringBuilder();
        foreach (var character in value ?? string.Empty)
        {
            if (char.IsControl(character))
            {
                escaped.Append(CultureInfo.InvariantCulture, $"\\u{(int)character:X4}");
            }
            else
            {
                escaped.Append(character);
            }
        }

        return escaped.ToString();
    }
}
