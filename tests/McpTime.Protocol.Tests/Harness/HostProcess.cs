using System.Diagnostics;
using System.Text;

namespace McpTime.Protocol.Tests.Harness;

/// <summary>Runs the built host with arguments until it exits, for checks that end before the transport.</summary>
internal static class HostProcess
{
    private static readonly TimeSpan s_exitTimeout = TimeSpan.FromSeconds(30);

    public static async Task<(int ExitCode, IReadOnlyList<string> Stdout, IReadOnlyList<string> Stderr)> RunToExitAsync(
        IReadOnlyList<string> arguments,
        CancellationToken ct,
        string? workingDirectory = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(s_exitTimeout);
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? string.Empty,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
        };
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "McpTime.Host.dll"));
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["Logging__LogLevel__Default"] = "Debug";

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        process.StandardInput.Close();
        var stdout = ReadAllLinesAsync(process.StandardOutput, timeout.Token);
        var stderr = ReadAllLinesAsync(process.StandardError, timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return (process.ExitCode, await stdout, await stderr);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"The host did not exit within {s_exitTimeout.TotalSeconds:0} s.");
        }
    }

    private static async Task<IReadOnlyList<string>> ReadAllLinesAsync(StreamReader reader, CancellationToken ct)
    {
        var lines = new List<string>();
        // The token alone may not abort a read already blocked on the pipe; WaitAsync abandons it on timeout.
        while (await reader.ReadLineAsync(ct).AsTask().WaitAsync(ct) is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }
}
