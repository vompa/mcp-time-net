using System.Text.Json;
using McpTime.Host;
using McpTime.Protocol.Tests.Harness;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpTime.Protocol.Tests;

public sealed class ToolListTests
{
    private static readonly string[] s_expectedNames = ["get_current_time", "convert_time"];

    private static readonly string[] s_conversionEndpoints = ["source", "target"];

    private static readonly string[] s_stringResultFields = ["timezone", "datetime", "day_of_week"];

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task ListsExactlyBothToolsInOrderAsync(string protocolVersion)
    {
        var tools = await ListToolsAsync(protocolVersion);
        Assert.Equal(s_expectedNames, tools.Select(t => t.Name));
    }

    [Fact]
    public async Task ToolListIsIdenticalInBothGenerationsAsync()
    {
        var handshake = Serialize(await ListToolsAsync(ProtocolGenerations.Handshake));
        var sessionless = Serialize(await ListToolsAsync(ProtocolGenerations.Sessionless));
        Assert.Equal(handshake, sessionless);
    }

    [Fact]
    public async Task GetCurrentTimeRequiresTimezoneStringAsync()
    {
        var schema = (await FindToolAsync("get_current_time")).InputSchema;
        Assert.Equal(["timezone"], RequiredOf(schema));
        Assert.Equal("string", TypeOf(schema, "timezone"));
    }

    [Fact]
    public async Task ConvertTimeRequiresThreeStringsAndOffersDateAsync()
    {
        var schema = (await FindToolAsync("convert_time")).InputSchema;
        Assert.Equal(["source_timezone", "time", "target_timezone"], RequiredOf(schema));
        Assert.All(RequiredOf(schema), name => Assert.Equal("string", TypeOf(schema, name!)));
        Assert.Equal("string", TypeOf(schema, "date"));
    }

    [Fact]
    public async Task OutputSchemasNameTheResultFieldsAsync()
    {
        var timeResult = (await FindToolAsync("get_current_time")).OutputSchema!.Value;
        Assert.Equal(["timezone", "datetime", "day_of_week", "is_dst"], RequiredOf(timeResult));
        Assert.Equal("boolean", TypeOf(timeResult, "is_dst"));
        Assert.All(s_stringResultFields, name => Assert.Equal("string", TypeOf(timeResult, name)));

        var conversion = (await FindToolAsync("convert_time")).OutputSchema!.Value;
        Assert.Equal(["source", "target", "time_difference"], RequiredOf(conversion));
        Assert.Equal("string", TypeOf(conversion, "time_difference"));
        Assert.All(s_conversionEndpoints, endpoint =>
        {
            Assert.Equal("object", TypeOf(conversion, endpoint));
            var endpointSchema = conversion.GetProperty("properties").GetProperty(endpoint);
            Assert.Equal(["timezone", "datetime", "day_of_week", "is_dst"], RequiredOf(endpointSchema));
        });
    }

    [Theory]
    [InlineData("get_current_time")]
    [InlineData("convert_time")]
    public async Task ToolIsReadOnlyIdempotentClosedWorldAsync(string toolName)
    {
        var annotations = (await FindToolAsync(toolName)).Annotations;
        Assert.NotNull(annotations);
        Assert.True(annotations.ReadOnlyHint);
        Assert.False(annotations.DestructiveHint);
        Assert.True(annotations.IdempotentHint);
        Assert.False(annotations.OpenWorldHint);
    }

    [Fact]
    public async Task ToolsListSnapshotAsync()
    {
        var tools = await ListToolsAsync(ProtocolGenerations.Sessionless);
        JsonSnapshot.Match("ToolsList", tools.Select(t => t.ProtocolTool).ToList());
    }

    [Fact]
    public async Task DescriptionsNameTheOverriddenLocalTimeZoneAsync()
    {
        var tools = await ListToolsAsync(
            ProtocolGenerations.Sessionless,
            ["--local-timezone", "Europe/Vienna"]);

        Assert.Contains("'Europe/Vienna'", PropertyDescription(tools, "get_current_time", "timezone"));
        Assert.Contains("'Europe/Vienna'", PropertyDescription(tools, "convert_time", "source_timezone"));
        Assert.Contains("'Europe/Vienna'", PropertyDescription(tools, "convert_time", "target_timezone"));
    }

    [Fact]
    public async Task DescriptionsNameTheDetectedZoneWithoutOptionAsync()
    {
        var detected = LocalTimeZone.DetectFromSystem().Value;

        var tools = await ListToolsAsync(ProtocolGenerations.Sessionless, []);

        Assert.Contains($"'{detected}'", PropertyDescription(tools, "get_current_time", "timezone"));
    }

    private static string? PropertyDescription(IList<McpClientTool> tools, string tool, string property)
        => tools.Single(t => t.Name == tool).ProtocolTool.InputSchema
            .GetProperty("properties").GetProperty(property).GetProperty("description").GetString();

    private static async Task<IList<McpClientTool>> ListToolsAsync(
        string protocolVersion,
        IReadOnlyList<string>? hostArguments = null)
    {
        await using var server = await ServerUnderTest.StartAsync(
            protocolVersion,
            TestContext.Current.CancellationToken,
            hostArguments);
        return await server.Client.ListToolsAsync(cancellationToken: server.Token);
    }

    private static async Task<Tool> FindToolAsync(string name)
    {
        var tools = await ListToolsAsync(ProtocolGenerations.Sessionless);
        return tools.Single(t => t.Name == name).ProtocolTool;
    }

    private static string Serialize(IEnumerable<McpClientTool> tools)
        => JsonSerializer.Serialize(tools.Select(t => t.ProtocolTool).ToList(), McpJsonUtilities.DefaultOptions);

    private static string?[] RequiredOf(JsonElement schema)
        => schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToArray();

    private static string? TypeOf(JsonElement schema, string property)
        => schema.GetProperty("properties").GetProperty(property).GetProperty("type").GetString();
}
