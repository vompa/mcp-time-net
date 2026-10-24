using McpTime.Protocol.Tests.Harness;

namespace McpTime.Protocol.Tests;

public sealed class CommandLineTests
{
    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("europe/vienna")]
    [InlineData("W. Europe Standard Time")]
    [InlineData("/etc/localtime")]
    [InlineData(" ")]
    public async Task InvalidLocalTimeZoneEndsBeforeTransportAsync(string value)
    {
        var result = await HostProcess.RunToExitAsync(["--local-timezone", value], TestContext.Current.CancellationToken);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(
            [$"Error: invalid --local-timezone '{value}': not a known IANA timezone name"],
            result.Stderr);
        Assert.Empty(result.Stdout);
    }

    [Fact]
    public async Task ControlCharactersStayOnOneLineAsync()
    {
        var result = await HostProcess.RunToExitAsync(
            ["--local-timezone", "Europe/Vienna\nX"],
            TestContext.Current.CancellationToken);

        Assert.NotEqual(0, result.ExitCode);
        var line = Assert.Single(result.Stderr);
        Assert.Contains("\\u000A", line);
        Assert.Empty(result.Stdout);
    }

    [Fact]
    public async Task HelpDescribesServerAndOptionAsync()
    {
        var result = await HostProcess.RunToExitAsync(["--help"], TestContext.Current.CancellationToken);

        Assert.Equal(0, result.ExitCode);
        var help = string.Join('\n', result.Stdout);
        Assert.Contains("MCP", help);
        Assert.Contains("get_current_time", help);
        Assert.Contains("convert_time", help);
        Assert.Contains("--local-timezone", help);
    }

    [Theory]
    [InlineData("--foo")]
    [InlineData("extra")]
    public async Task UnknownOptionsAndArgumentsAreRejectedAsync(string token)
    {
        var result = await HostProcess.RunToExitAsync([token], TestContext.Current.CancellationToken);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains(token, string.Join('\n', result.Stderr));
    }

    [Fact]
    public async Task RepeatedOptionIsRejectedAsync()
    {
        var result = await HostProcess.RunToExitAsync(
            ["--local-timezone", "UTC", "--local-timezone", "UTC"],
            TestContext.Current.CancellationToken);

        Assert.NotEqual(0, result.ExitCode);
        Assert.NotEmpty(result.Stderr);
    }

    [Fact]
    public async Task ResponseFileTokensAreNotExpandedAsync()
    {
        var result = await HostProcess.RunToExitAsync(
            ["--local-timezone", "@something"],
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ExitCode);
        Assert.Equal(
            ["Error: invalid --local-timezone '@something': not a known IANA timezone name"],
            result.Stderr);
    }
}
