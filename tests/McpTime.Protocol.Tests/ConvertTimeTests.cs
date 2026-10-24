using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using McpTime.Protocol.Tests.Harness;
using ModelContextProtocol.Protocol;

namespace McpTime.Protocol.Tests;

public sealed class ConvertTimeTests
{
    private const string ToolName = "convert_time";

    public static TheoryData<string, string, string[]> BusinessErrors
    {
        get
        {
            var data = new TheoryData<string, string, string[]>();
            foreach (var generation in new[] { ProtocolGenerations.Handshake, ProtocolGenerations.Sessionless })
            {
                foreach (var time in new[] { "25:00", "12:00 PM" })
                {
                    data.Add(generation, Call(time: time), ["Invalid time format. Expected HH:MM [24-hour format]", time]);
                }

                data.Add(generation, Call(source: ""), ["Missing required argument: source_timezone"]);
                data.Add(generation, Call(target: ""), ["Missing required argument: target_timezone"]);
                data.Add(generation, Call(source: "Invalid/Zone"), ["Invalid timezone", "Invalid/Zone", "source_timezone"]);
                data.Add(generation, Call(target: "Invalid/Zone"), ["Invalid timezone", "Invalid/Zone", "target_timezone"]);
                foreach (var date in new[] { "2024-02-30", "15.07.2024" })
                {
                    data.Add(generation, Call(date: date), ["YYYY-MM-DD", date]);
                }

                data.Add(
                    generation,
                    Call(source: "America/New_York", time: "02:30", target: "UTC", date: "2024-03-10"),
                    ["02:30", "America/New_York", "2024-03-10", "skipped by a clock change"]);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task SuccessfulConversionReturnsStructuredContentAndJsonTextAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);

        var result = await CallAsync(server, Call(source: "Europe/London", time: "12:00", target: "Europe/Warsaw", date: "2024-01-01"));

        Assert.NotEqual(true, result.IsError);
        var expected = JsonNode.Parse("""
            {
              "source": { "timezone": "Europe/London", "datetime": "2024-01-01T12:00:00+00:00", "day_of_week": "Monday", "is_dst": false },
              "target": { "timezone": "Europe/Warsaw", "datetime": "2024-01-01T13:00:00+01:00", "day_of_week": "Monday", "is_dst": false },
              "time_difference": "+1.0h"
            }
            """);
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(result.StructuredContent!.Value.GetRawText())));
        var block = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(block.Text)));
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task WithoutDateUsesTodayInTheSourceZoneAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var before = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await CallAsync(server, Call(source: "UTC", time: "12:00", target: "UTC", date: null));

        var after = DateOnly.FromDateTime(DateTime.UtcNow);
        Assert.NotEqual(true, result.IsError);
        var datetime = result.StructuredContent!.Value.GetProperty("source").GetProperty("datetime").GetString()!;
        var date = DateOnly.ParseExact(datetime[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.InRange(date, before, after);
    }

    [Theory]
    [MemberData(nameof(BusinessErrors))]
    public async Task BusinessErrorsAreToolResultsAsync(string protocolVersion, string arguments, string[] expectedParts)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);

        var result = await CallAsync(server, arguments);

        AssertBusinessError(result, expectedParts);
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task ExtremeDateIsABusinessErrorAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);

        var result = await CallAsync(server, Call(source: "UTC", time: "23:59", target: "Pacific/Kiritimati", date: "9999-12-31"));

        AssertBusinessError(result, ["Invalid date"]);
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task FirstInvalidArgumentWinsAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);

        var result = await CallAsync(server, Call(source: "Invalid/Zone", time: "25:00", target: "Invalid/Other", date: "nope"));

        AssertBusinessError(result, ["source_timezone"]);
        Assert.DoesNotContain("time format", ((TextContentBlock)result.Content[0]).Text);
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task MissingTimeIsReportedAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var arguments = new Dictionary<string, object?>
        {
            ["source_timezone"] = "UTC",
            ["target_timezone"] = "UTC",
        };

        var result = await server.Client.CallToolAsync(ToolName, arguments, cancellationToken: server.Token);

        AssertBusinessError(result, ["Missing required argument: time"]);
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task UnknownArgumentIsIgnoredAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var arguments = new Dictionary<string, object?>
        {
            ["source_timezone"] = "UTC",
            ["time"] = "12:00",
            ["target_timezone"] = "UTC",
            ["date"] = "2024-01-01",
            ["unexpected"] = "x",
        };

        var result = await server.Client.CallToolAsync(ToolName, arguments, cancellationToken: server.Token);

        Assert.NotEqual(true, result.IsError);
    }

    private static string Call(
        string source = "UTC",
        string time = "12:00",
        string target = "UTC",
        string? date = "2024-01-01")
    {
        var arguments = new JsonObject
        {
            ["source_timezone"] = source,
            ["time"] = time,
            ["target_timezone"] = target,
        };
        if (date is not null)
        {
            arguments["date"] = date;
        }

        return arguments.ToJsonString();
    }

    private static ValueTask<CallToolResult> CallAsync(ServerUnderTest server, string arguments)
    {
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(arguments)!
            .ToDictionary(p => p.Key, p => (object?)p.Value);
        return server.Client.CallToolAsync(ToolName, parsed, cancellationToken: server.Token);
    }

    private static void AssertBusinessError(CallToolResult result, string[] expectedParts)
    {
        Assert.True(result.IsError);
        Assert.Null(result.StructuredContent);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        foreach (var part in expectedParts)
        {
            Assert.Contains(part, text);
        }
    }
}
