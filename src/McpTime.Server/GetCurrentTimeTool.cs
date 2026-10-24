using System.Text.Json;
using McpTime.Core;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTime.Server;

internal sealed class GetCurrentTimeTool(string localTimeZone, CurrentTime currentTime) : McpServerTool
{
    private const string TimeZoneArgument = "timezone";

    public override Tool ProtocolTool { get; } = ToolDefinitions.GetCurrentTime(localTimeZone);

    public override IReadOnlyList<object> Metadata => [];

    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Invoke(request.Params));

    private CallToolResult Invoke(CallToolRequestParams? parameters)
    {
        if (!ToolArguments.TryGetText(parameters, TimeZoneArgument, out var text))
        {
            return ToolErrors.MissingTimeZone(TimeZoneArgument);
        }

        if (!TimeZoneName.TryCreate(text, out var zone))
        {
            return ToolErrors.InvalidTimeZone(TimeZoneArgument, text);
        }

        return ToolResults.Success(
            JsonSerializer.SerializeToElement(currentTime.In(zone), McpTimeJsonContext.Default.TimeResult));
    }
}
