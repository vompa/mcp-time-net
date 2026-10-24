using McpTime.Protocol.Tests.Harness;

namespace McpTime.Protocol.Tests;

public sealed class StartupIsolationTests
{
    [Fact]
    public async Task MalformedAppSettingsInWorkingDirectoryAreIgnoredAsync()
    {
        var directory = Path.Combine(Path.GetTempPath(), "mcp-time-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(directory, "appsettings.json"),
                "{ not json",
                TestContext.Current.CancellationToken);

            var result = await HostProcess.RunToExitAsync(
                ["--local-timezone", "UTC"],
                TestContext.Current.CancellationToken,
                directory);

            Assert.Equal(0, result.ExitCode);
            Assert.DoesNotContain(result.Stderr, line => line.Contains("appsettings", StringComparison.Ordinal));
            Assert.Empty(result.Stdout);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
