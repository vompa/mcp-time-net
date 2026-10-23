using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTime.Server;

public static class McpTimeServerServiceCollectionExtensions
{
    // Placeholder: the empty handler makes the server advertise tools until Slice 01 registers them.
    public static IMcpServerBuilder AddMcpTimeServer(this IServiceCollection services)
        => services.AddMcpServer()
            .WithListToolsHandler((_, _) => ValueTask.FromResult(new ListToolsResult()));
}
