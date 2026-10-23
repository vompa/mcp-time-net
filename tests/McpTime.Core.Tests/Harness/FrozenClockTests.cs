using System.Globalization;

namespace McpTime.Core.Tests.Harness;

public class FrozenClockTests
{
    [Theory]
    [InlineData("2024-01-01 12:00:00", "2024-01-01T12:00:00+00:00")]
    [InlineData("2024-01-01 00:00", "2024-01-01T00:00:00+00:00")]
    [InlineData("2024-03-10 06:59:59", "2024-03-10T06:59:59+00:00")]
    public void AtReturnsTheGivenUtcInstant(string now, string expected)
    {
        TimeProvider clock = FrozenClock.At(now);
        Assert.Equal(expected, clock.GetUtcNow().ToString("yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture));
        Assert.Equal(TimeSpan.Zero, clock.GetUtcNow().Offset);
    }
}
