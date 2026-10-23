using System.Text.Json;
using McpTime.Protocol.Tests.Harness;

namespace McpTime.Protocol.Tests;

public sealed class StdoutDisciplineTests
{
    private const string InitializeRequest =
        """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"raw-test","version":"1"}}}""";

    private const string InitializedNotification =
        """{"jsonrpc":"2.0","method":"notifications/initialized"}""";

    private const string ToolsListRequest =
        """{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}""";

    [Fact]
    public async Task StdoutCarriesOnlyJsonRpcMessagesAsync()
    {
        await using var server = await RawStdioServer.StartAsync(TestContext.Current.CancellationToken);

        await server.SendLineAsync(InitializeRequest);
        await server.SendLineAsync(InitializedNotification);
        await server.SendLineAsync(ToolsListRequest);
        await ReadUntilResponseAsync(server, expectedId: 2);

        var (exitCode, remainingStdout) = await server.CloseStdinAndWaitForExitAsync();

        Assert.All(remainingStdout, AssertIsJsonRpc);
        Assert.Equal(0, exitCode);
        Assert.NotEmpty(server.StandardError);
    }

    private static async Task<List<string>> ReadUntilResponseAsync(RawStdioServer server, int expectedId)
    {
        var lines = new List<string>();
        try
        {
            while (true)
            {
                var line = await server.ReadStdoutLineAsync();
                lines.Add(line);
                if (IsResponseWithId(line, expectedId))
                {
                    return lines;
                }
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or EndOfStreamException)
        {
            Assert.Fail(
                $"No response with id {expectedId} arrived on stdout ({exception.GetType().Name}). "
                + $"stdout lines: [{string.Join(" | ", lines)}]; stderr lines: [{string.Join(" | ", server.StandardError)}]");
            throw;
        }
    }

    private static bool IsResponseWithId(string line, int expectedId)
    {
        using var document = ParseJsonRpcMessage(line);
        return document.RootElement.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.Number
            && id.GetInt32() == expectedId
            && !document.RootElement.TryGetProperty("method", out _);
    }

    private static void AssertIsJsonRpc(string line)
    {
        using var document = ParseJsonRpcMessage(line);
    }

    private static JsonDocument ParseJsonRpcMessage(string line)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            Assert.Fail($"stdout line is not a JSON-RPC message: {line}");
            throw;
        }

        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("jsonrpc", out var version)
            || version.ValueKind != JsonValueKind.String
            || version.GetString() != "2.0")
        {
            document.Dispose();
            Assert.Fail($"stdout line is not a JSON-RPC message: {line}");
        }

        return document;
    }
}
