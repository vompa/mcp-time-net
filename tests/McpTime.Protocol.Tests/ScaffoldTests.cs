using McpTime.Protocol.Tests.Harness;

namespace McpTime.Protocol.Tests;

public sealed class ScaffoldTests
{
    [Fact]
    public void HostRuntimeConfigIsCopiedNextToTheTests()
        => Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "McpTime.Host.runtimeconfig.json")));

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task EmptyServerAnswersToolsListWithNoToolsAsync(string protocolVersion)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, ct);
        Assert.Equal(protocolVersion, server.Client.NegotiatedProtocolVersion);
        var tools = await server.Client.ListToolsAsync(cancellationToken: server.Token);
        Assert.Empty(tools);
    }

    [Fact]
    public async Task ToolsListSnapshotAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var server = await ServerUnderTest.StartAsync(ProtocolGenerations.Sessionless, ct);
        var tools = await server.Client.ListToolsAsync(cancellationToken: server.Token);
        JsonSnapshot.Match("ToolsList", tools.Select(t => t.ProtocolTool).ToList());
    }
}
