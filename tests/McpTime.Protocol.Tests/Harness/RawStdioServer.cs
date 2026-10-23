using System.Diagnostics;
using System.Text;

namespace McpTime.Protocol.Tests.Harness;

/// <summary>Starts the built host as a plain process so a test sees the raw stdout bytes.</summary>
internal sealed class RawStdioServer : IAsyncDisposable
{
    private static readonly TimeSpan s_exitTimeout = TimeSpan.FromSeconds(30);

    private readonly Process _process;
    private readonly CancellationTokenSource _timeout;
    private readonly Task _standardErrorReader;
    private readonly List<string> _standardError;

    private RawStdioServer(
        Process process,
        CancellationTokenSource timeout,
        Task standardErrorReader,
        List<string> standardError)
    {
        _process = process;
        _timeout = timeout;
        _standardErrorReader = standardErrorReader;
        _standardError = standardError;
    }

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

    public static Task<RawStdioServer> StartAsync(CancellationToken ct)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(s_exitTimeout);
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
        };
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "McpTime.Host.dll"));
        startInfo.Environment["Logging__LogLevel__Default"] = "Debug";

        var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch
        {
            process.Dispose();
            timeout.Dispose();
            throw;
        }

        var standardError = new List<string>();
        var reader = Task.Run(() => CollectStandardErrorAsync(process, standardError), CancellationToken.None);
        return Task.FromResult(new RawStdioServer(process, timeout, reader, standardError));
    }

    public async Task SendLineAsync(string json)
    {
        await _process.StandardInput.WriteLineAsync(json.AsMemory(), _timeout.Token);
        await _process.StandardInput.FlushAsync(_timeout.Token);
    }

    public async Task<string> ReadStdoutLineAsync()
    {
        // The token alone may not abort a read already blocked on the pipe; WaitAsync abandons it on timeout.
        var line = await _process.StandardOutput.ReadLineAsync(_timeout.Token).AsTask().WaitAsync(_timeout.Token);
        return line ?? throw new EndOfStreamException("The server closed stdout before the expected line arrived.");
    }

    /// <summary>Closes stdin, then drains stdout to EOF while waiting for the process to exit.</summary>
    public async Task<(int ExitCode, IReadOnlyList<string> RemainingStdout)> CloseStdinAndWaitForExitAsync()
    {
        _process.StandardInput.Close();
        var remainingStdout = new List<string>();
        var drain = Task.Run(() => DrainStandardOutputAsync(remainingStdout), CancellationToken.None);
        try
        {
            await _process.WaitForExitAsync(_timeout.Token);
            await drain.WaitAsync(_timeout.Token);
        }
        catch (OperationCanceledException)
        {
            _process.Kill(entireProcessTree: true);
            Assert.Fail($"The server did not exit within {s_exitTimeout.TotalSeconds:0} s after stdin was closed.");
        }

        await _standardErrorReader.WaitAsync(_timeout.Token);
        return (_process.ExitCode, remainingStdout);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync(CancellationToken.None);
        }

        await _standardErrorReader;
        _process.Dispose();
        _timeout.Dispose();
    }

    private async Task DrainStandardOutputAsync(List<string> lines)
    {
        string? line;
        while ((line = await _process.StandardOutput.ReadLineAsync()) is not null)
        {
            lines.Add(line);
        }
    }

    private static async Task CollectStandardErrorAsync(Process process, List<string> lines)
    {
        string? line;
        while ((line = await process.StandardError.ReadLineAsync()) is not null)
        {
            lock (lines)
            {
                lines.Add(line);
            }
        }
    }
}
