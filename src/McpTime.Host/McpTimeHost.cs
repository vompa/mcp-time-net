using McpTime.Core;
using McpTime.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McpTime.Host;

/// <summary>Runs the MCP server on stdio; stdout belongs to the transport, logs go to stderr (D16).</summary>
internal static class McpTimeHost
{
    public static Task RunAsync(TimeZoneName localTimeZone, CancellationToken ct)
    {
        // Not CreateApplicationBuilder(): its defaults read appsettings*.json (with a file watcher) from the
        // directory the MCP client happens to start the tool in. Only environment variables configure the host;
        // the command line belongs to System.CommandLine.
        var builder = Microsoft.Extensions.Hosting.Host.CreateEmptyApplicationBuilder(
            new HostApplicationBuilderSettings { DisableDefaults = true });
        builder.Configuration.AddEnvironmentVariables();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddMcpTimeServer(localTimeZone.Value, TimeProvider.System).WithStdioServerTransport();
        return builder.Build().RunAsync(ct);
    }
}
