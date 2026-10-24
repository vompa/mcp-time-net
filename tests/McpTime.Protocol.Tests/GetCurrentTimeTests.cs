using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using McpTime.Protocol.Tests.Harness;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace McpTime.Protocol.Tests;

public sealed partial class GetCurrentTimeTests
{
    private static readonly TimeSpan s_clockTolerance = TimeSpan.FromMinutes(2);

    public static TheoryData<string, string, string[]> BusinessErrors
    {
        get
        {
            var data = new TheoryData<string, string, string[]>();
            foreach (var generation in new[] { ProtocolGenerations.Handshake, ProtocolGenerations.Sessionless })
            {
                data.Add(generation, """{"timezone":""}""", ["Missing required argument: timezone"]);
                data.Add(generation, "{}", ["Missing required argument: timezone"]);
                foreach (var name in new[] { "Invalid/Timezone", "europe/warsaw", "../etc/passwd", "/etc/localtime" })
                {
                    data.Add(generation, $$"""{"timezone":"{{name}}"}""", ["Invalid timezone", name, "timezone"]);
                }
            }

            return data;
        }
    }

    public static TheoryData<string, object?, string> NonStringValues
    {
        get
        {
            var data = new TheoryData<string, object?, string>();
            foreach (var generation in new[] { ProtocolGenerations.Handshake, ProtocolGenerations.Sessionless })
            {
                data.Add(generation, 42, "Invalid timezone");
                data.Add(generation, true, "Invalid timezone");
                data.Add(generation, new Dictionary<string, object?>(), "Invalid timezone");
                data.Add(generation, null, "Missing required argument: timezone");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task SuccessfulCallReturnsStructuredContentAndJsonTextAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);

        var result = await CallAsync(server, "UTC");

        Assert.NotEqual(true, result.IsError);
        var structured = Assert.IsType<JsonObject>(JsonNode.Parse(result.StructuredContent!.Value.GetRawText()));
        Assert.Equal("UTC", structured["timezone"]!.GetValue<string>());
        var datetime = structured["datetime"]!.GetValue<string>();
        Assert.Matches(UtcDateTimePattern(), datetime);
        var moment = DateTimeOffset.Parse(datetime, CultureInfo.InvariantCulture);
        Assert.InRange(moment, DateTimeOffset.UtcNow - s_clockTolerance, DateTimeOffset.UtcNow + s_clockTolerance);
        Assert.Equal(moment.DayOfWeek.ToString(), structured["day_of_week"]!.GetValue<string>());
        Assert.False(structured["is_dst"]!.GetValue<bool>());
        var block = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        Assert.True(JsonNode.DeepEquals(structured, JsonNode.Parse(block.Text)));
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task StructuredContentMatchesTheOutputSchemaAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var tools = await server.Client.ListToolsAsync(cancellationToken: server.Token);
        var schema = tools.Single(t => t.Name == "get_current_time").ProtocolTool.OutputSchema!.Value;
        var required = schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).Order().ToArray();

        var result = await CallAsync(server, "Europe/Vienna");

        var structured = result.StructuredContent!.Value;
        Assert.Equal(required, structured.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.True(structured.GetProperty("is_dst").ValueKind is JsonValueKind.True or JsonValueKind.False);
    }

    [Theory]
    [MemberData(nameof(BusinessErrors))]
    public async Task BusinessErrorsAreToolResultsAsync(string protocolVersion, string arguments, string[] expectedParts)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments)!
            .ToDictionary(p => p.Key, p => (object?)p.Value);

        var result = await server.Client.CallToolAsync("get_current_time", parsed, cancellationToken: server.Token);

        Assert.True(result.IsError);
        Assert.Null(result.StructuredContent);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        foreach (var part in expectedParts)
        {
            Assert.Contains(part, text);
        }
    }

    [Theory]
    [MemberData(nameof(NonStringValues))]
    public async Task NonStringTimezoneIsABusinessErrorAsync(string protocolVersion, object? value, string expectedText)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var arguments = new Dictionary<string, object?> { ["timezone"] = value };

        var result = await server.Client.CallToolAsync("get_current_time", arguments, cancellationToken: server.Token);

        Assert.True(result.IsError);
        Assert.Contains(expectedText, Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text);
        Assert.Equal(2, (await server.Client.ListToolsAsync(cancellationToken: server.Token)).Count);
    }

    private static ValueTask<CallToolResult> CallAsync(ServerUnderTest server, string timezone)
        => server.Client.CallToolAsync(
            "get_current_time",
            new Dictionary<string, object?> { ["timezone"] = timezone },
            cancellationToken: server.Token);

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\+00:00$")]
    private static partial Regex UtcDateTimePattern();
}
