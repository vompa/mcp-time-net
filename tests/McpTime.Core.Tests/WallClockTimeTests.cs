namespace McpTime.Core.Tests;

public class WallClockTimeTests
{
    [Theory]
    [InlineData("9:5", "09:05")]
    [InlineData("00:00", "00:00")]
    [InlineData("23:59", "23:59")]
    [InlineData("7:30", "07:30")]
    public void ValidTimesAreNormalized(string text, string expected)
    {
        Assert.True(WallClockTime.TryParse(text, out var time));
        Assert.Equal(expected, time.ToString());
    }

    [Theory]
    [InlineData("24:00")]
    [InlineData("12:60")]
    [InlineData("12:00:00")]
    [InlineData("12:00 PM")]
    [InlineData("1200")]
    [InlineData("12.00")]
    [InlineData("009:05")]
    [InlineData(" 12:00")]
    [InlineData("12:00 ")]
    [InlineData("12:00\n")]
    [InlineData("١٢:٠٠")]
    [InlineData("")]
    public void InvalidTimesAreRejected(string text)
    {
        Assert.False(WallClockTime.TryParse(text, out var time));
        Assert.Null(time);
    }
}
