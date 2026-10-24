using McpTime.Core;
using McpTime.Host;

namespace McpTime.Protocol.Tests.Host;

public sealed class LocalTimeZoneTests
{
    private static TimeZoneName Tokyo()
    {
        Assert.True(TimeZoneName.TryCreate("Asia/Tokyo", out var name));
        return name;
    }

    [Fact]
    public void ValidOverrideWins()
    {
        var detectCalled = false;

        var resolved = LocalTimeZone.TryResolve(
            "Europe/Vienna",
            () =>
            {
                detectCalled = true;
                return Tokyo();
            },
            out var zone);

        Assert.True(resolved);
        Assert.Equal("Europe/Vienna", zone?.Value);
        Assert.False(detectCalled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MissingOrEmptyOverrideUsesDetection(string? optionValue)
    {
        var resolved = LocalTimeZone.TryResolve(optionValue, Tokyo, out var zone);

        Assert.True(resolved);
        Assert.Equal("Asia/Tokyo", zone?.Value);
    }

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("europe/vienna")]
    [InlineData("W. Europe Standard Time")]
    [InlineData("/etc/localtime")]
    [InlineData(" ")]
    public void InvalidOverridesAreRejected(string optionValue)
    {
        var resolved = LocalTimeZone.TryResolve(optionValue, Tokyo, out var zone);

        Assert.False(resolved);
        Assert.Null(zone);
    }

    [Theory]
    [InlineData("Europe/Vienna", "AT", "Europe/Vienna")]
    [InlineData("UTC", "US", "UTC")]
    public void DetectKeepsIanaIds(string systemId, string region, string expected)
        => Assert.Equal(expected, LocalTimeZone.Detect(systemId, region).Value);

    [Theory]
    [InlineData("W. Europe Standard Time", "AT", "Europe/Vienna")]
    [InlineData("W. Europe Standard Time", "001", "Europe/Berlin")]
    [InlineData("Tokyo Standard Time", "", "Asia/Tokyo")]
    public void DetectMapsWindowsIds(string systemId, string region, string expected)
        => Assert.Equal(expected, LocalTimeZone.Detect(systemId, region).Value);

    [Theory]
    [InlineData("Nonsense Zone", "AT")]
    [InlineData("", "")]
    [InlineData("europe/vienna", "AT")]
    public void UnmappableSystemZoneFallsBackToUtc(string systemId, string region)
        => Assert.Equal("UTC", LocalTimeZone.Detect(systemId, region).Value);

    [Fact]
    public void DetectFromSystemReturnsAKnownZone()
    {
        var detected = LocalTimeZone.DetectFromSystem();

        Assert.True(TimeZoneName.TryCreate(detected.Value, out _));
    }
}
