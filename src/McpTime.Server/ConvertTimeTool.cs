using System.Text.Json;
using McpTime.Core;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpTime.Server;

internal sealed class ConvertTimeTool(string localTimeZone, TimeConversion conversion) : McpServerTool
{
    private const string SourceArgument = "source_timezone";
    private const string TimeArgument = "time";
    private const string TargetArgument = "target_timezone";
    private const string DateArgument = "date";

    public override Tool ProtocolTool { get; } = ToolDefinitions.ConvertTime(localTimeZone);

    public override IReadOnlyList<object> Metadata => [];

    public override ValueTask<CallToolResult> InvokeAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(Invoke(request.Params));

    private CallToolResult Invoke(CallToolRequestParams? parameters)
    {
        if (!TryReadZone(parameters, SourceArgument, out var source, out var error)
            || !TryReadTime(parameters, out var time, out error)
            || !TryReadZone(parameters, TargetArgument, out var target, out error)
            || !TryReadDate(parameters, source, out var date, out error))
        {
            return error;
        }

        if (!conversion.TryConvert(source, time, date, target, out var result))
        {
            return ToolErrors.SkippedLocalTime(time, source, date);
        }

        return ToolResults.Success(
            JsonSerializer.SerializeToElement(result, McpTimeJsonContext.Default.TimeConversionResult));
    }

    private static bool TryReadZone(
        CallToolRequestParams? parameters,
        string argument,
        out TimeZoneName zone,
        out CallToolResult error)
    {
        zone = null!;
        if (!ToolArguments.TryGetText(parameters, argument, out var text))
        {
            error = ToolErrors.MissingTimeZone(argument);
            return false;
        }

        if (!TimeZoneName.TryCreate(text, out var created))
        {
            error = ToolErrors.InvalidTimeZone(argument, text);
            return false;
        }

        zone = created;
        error = null!;
        return true;
    }

    private static bool TryReadTime(CallToolRequestParams? parameters, out WallClockTime time, out CallToolResult error)
    {
        time = null!;
        if (!ToolArguments.TryGetText(parameters, TimeArgument, out var text))
        {
            error = ToolErrors.MissingTime();
            return false;
        }

        if (!WallClockTime.TryParse(text, out var parsed))
        {
            error = ToolErrors.InvalidTime(text);
            return false;
        }

        time = parsed;
        error = null!;
        return true;
    }

    private bool TryReadDate(
        CallToolRequestParams? parameters,
        TimeZoneName source,
        out ReferenceDate date,
        out CallToolResult error)
    {
        error = null!;
        if (!ToolArguments.TryGetText(parameters, DateArgument, out var text))
        {
            date = conversion.TodayIn(source);
            return true;
        }

        if (!ReferenceDate.TryParse(text, out var parsed))
        {
            date = null!;
            error = ToolErrors.InvalidDate(text);
            return false;
        }

        date = parsed;
        return true;
    }
}
