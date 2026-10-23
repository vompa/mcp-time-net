using System.Text.RegularExpressions;
using McpTime.Protocol.Tests.Harness;
using ModelContextProtocol;

namespace McpTime.Protocol.Tests;

public sealed partial class InitializeTests
{
    [Fact]
    public async Task HandshakeReportsServerInfoToolsAndAnswersPingAsync()
    {
        await using var server = await ServerUnderTest.StartAsync(ProtocolGenerations.Handshake, TestContext.Current.CancellationToken);
        var client = server.Client;

        Assert.Equal(ProtocolGenerations.Handshake, client.NegotiatedProtocolVersion);
        var serverInfo = client.ServerInfo;
        Assert.NotNull(serverInfo);
        Assert.Equal("mcp-time", serverInfo.Name);
        Assert.Matches(SemanticVersion(), serverInfo.Version);
        Assert.NotNull(client.ServerCapabilities.Tools);
        Assert.Null(client.ServerCapabilities.Prompts);
        Assert.Null(client.ServerCapabilities.Resources);
        await client.PingAsync(cancellationToken: server.Token);
    }

    [Fact]
    public async Task SessionlessGenerationRejectsPingAsync()
    {
        await using var server = await ServerUnderTest.StartAsync(ProtocolGenerations.Sessionless, TestContext.Current.CancellationToken);
        var client = server.Client;

        Assert.Equal(ProtocolGenerations.Sessionless, client.NegotiatedProtocolVersion);
        Assert.Equal("mcp-time", client.ServerInfo?.Name);
        var exception = await Assert.ThrowsAsync<McpProtocolException>(
            () => client.PingAsync(cancellationToken: server.Token).AsTask());
        Assert.Equal(McpErrorCode.MethodNotFound, exception.ErrorCode);
        Assert.NotEmpty(await client.ListToolsAsync(cancellationToken: server.Token));
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task PromptsAndResourcesAreNotOfferedAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var client = server.Client;

        Assert.Null(client.ServerCapabilities.Prompts);
        Assert.Null(client.ServerCapabilities.Resources);
        await Assert.ThrowsAnyAsync<McpException>(
            () => client.ListPromptsAsync(cancellationToken: server.Token).AsTask());
        await Assert.ThrowsAnyAsync<McpException>(
            () => client.ListResourcesAsync(cancellationToken: server.Token).AsTask());
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex SemanticVersion();
}
