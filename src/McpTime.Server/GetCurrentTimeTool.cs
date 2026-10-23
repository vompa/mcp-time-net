using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTime.Server;

internal sealed class GetCurrentTimeTool(string localTimeZone) : McpServerTool
{
    public override Tool ProtocolTool { get; } = ToolDefinitions.GetCurrentTime(localTimeZone);

    public override IReadOnlyList<object> Metadata => [];

    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ToolResults.NotImplemented(ProtocolTool.Name));
}
