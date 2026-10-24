using McpTime.Core;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace McpTime.Server;

public static class McpTimeServerServiceCollectionExtensions
{
    public static IMcpServerBuilder AddMcpTimeServer(
        this IServiceCollection services,
        string localTimeZone,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(localTimeZone);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var currentTime = new CurrentTime(timeProvider);
        return services.AddMcpServer(options => options.ServerInfo = McpTimeServerInfo.Create())
            .WithTools([new GetCurrentTimeTool(localTimeZone, currentTime), new ConvertTimeTool(localTimeZone, new TimeConversion(timeProvider))]);
    }
}
