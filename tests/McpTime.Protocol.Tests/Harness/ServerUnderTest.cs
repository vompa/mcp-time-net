using ModelContextProtocol.Client;

namespace McpTime.Protocol.Tests.Harness;

internal sealed class ServerUnderTest : IAsyncDisposable
{
    private static readonly TimeSpan s_requestTimeout = TimeSpan.FromSeconds(30);

    private readonly CancellationTokenSource _timeout;
    private readonly List<string> _standardError;

    private ServerUnderTest(McpClient client, CancellationTokenSource timeout, List<string> standardError)
    {
        Client = client;
        _timeout = timeout;
        _standardError = standardError;
    }

    public McpClient Client { get; }

    /// <summary>Cancels 30 seconds after start or when the caller's token is cancelled.</summary>
    public CancellationToken Token => _timeout.Token;

    public IReadOnlyList<string> StandardError
    {
        get
        {
            lock (_standardError)
            {
                return [.. _standardError];
            }
        }
    }

    /// <param name="hostArguments">Command line for the host; the default pins the local zone so output never depends on the machine.</param>
    public static async Task<ServerUnderTest> StartAsync(
        string protocolVersion,
        CancellationToken ct,
        IReadOnlyList<string>? hostArguments = null)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(s_requestTimeout);
        var standardError = new List<string>();
        var hostDllPath = Path.Combine(AppContext.BaseDirectory, "McpTime.Host.dll");
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "mcp-time",
            Command = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            Arguments = [hostDllPath, .. hostArguments ?? ["--local-timezone", "UTC"]],
            StandardErrorLines = line =>
            {
                lock (standardError)
                {
                    standardError.Add(line);
                }
            },
        });
        try
        {
            var client = await McpClient.CreateAsync(
                transport,
                new McpClientOptions { ProtocolVersion = protocolVersion },
                cancellationToken: timeout.Token);
            return new ServerUnderTest(client, timeout, standardError);
        }
        catch
        {
            timeout.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        _timeout.Dispose();
    }
}
