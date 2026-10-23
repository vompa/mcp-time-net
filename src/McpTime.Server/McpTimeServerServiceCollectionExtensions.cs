using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace McpTime.Server;

public static class McpTimeServerServiceCollectionExtensions
{
    public static IMcpServerBuilder AddMcpTimeServer(this IServiceCollection services, string localTimeZone)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(localTimeZone);

        return services.AddMcpServer(options => options.ServerInfo = McpTimeServerInfo.Create())
            .WithTools([new GetCurrentTimeTool(localTimeZone), new ConvertTimeTool(localTimeZone)]);
    }
}
