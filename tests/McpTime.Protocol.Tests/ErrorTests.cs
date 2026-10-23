using McpTime.Protocol.Tests.Harness;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace McpTime.Protocol.Tests;

public sealed class ErrorTests
{
    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task UnknownToolIsAnInvalidParamsErrorAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<McpProtocolException>(
            () => server.Client.CallToolAsync("no_such_tool", cancellationToken: server.Token).AsTask());

        Assert.Equal(McpErrorCode.InvalidParams, exception.ErrorCode);
        Assert.Contains("no_such_tool", exception.Message);
        Assert.NotEmpty(await server.Client.ListToolsAsync(cancellationToken: server.Token));
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task UnknownMethodIsAMethodNotFoundErrorAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var request = new JsonRpcRequest { Method = "no/such_method", Id = new RequestId("i-err-2") };

        var exception = await Assert.ThrowsAsync<McpProtocolException>(
            () => server.Client.SendRequestAsync(request, server.Token));

        Assert.Equal(McpErrorCode.MethodNotFound, exception.ErrorCode);
        Assert.NotEmpty(await server.Client.ListToolsAsync(cancellationToken: server.Token));
    }

    [Theory]
    [MemberData(nameof(ProtocolGenerations.All), MemberType = typeof(ProtocolGenerations))]
    public async Task UnknownArgumentIsIgnoredAtProtocolLevelAsync(string protocolVersion)
    {
        await using var server = await ServerUnderTest.StartAsync(protocolVersion, TestContext.Current.CancellationToken);
        var plain = new Dictionary<string, object?> { ["timezone"] = "UTC" };
        var withUnknown = new Dictionary<string, object?> { ["timezone"] = "UTC", ["unexpected"] = "x" };

        var expected = await server.Client.CallToolAsync("get_current_time", plain, cancellationToken: server.Token);
        var actual = await server.Client.CallToolAsync("get_current_time", withUnknown, cancellationToken: server.Token);

        Assert.Equal(expected.IsError, actual.IsError);
        Assert.Equal(FirstText(expected), FirstText(actual));
    }

    private static string? FirstText(CallToolResult result)
        => Assert.IsType<TextContentBlock>(result.Content[0]).Text;

    [Fact]
    public async Task CallWithoutArgumentsDoesNotCrashTheServerAsync()
    {
        await using var server = await ServerUnderTest.StartAsync(ProtocolGenerations.Handshake, TestContext.Current.CancellationToken);

        var result = await server.Client.CallToolAsync("get_current_time", cancellationToken: server.Token);
        var tools = await server.Client.ListToolsAsync(cancellationToken: server.Token);

        Assert.True(result.IsError);
        Assert.Equal(2, tools.Count);
    }
}
