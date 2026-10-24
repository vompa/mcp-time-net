using NodaTime;

namespace McpTime.Core.Tests;

public class TimeDifferenceTests
{
    [Theory]
    [InlineData(0, 3600, "+1.0h")]
    [InlineData(0, 0, "+0.0h")]
    [InlineData(0, 20700, "+5.75h")]
    [InlineData(0, 19800, "+5.5h")]
    [InlineData(20700, 3600, "-4.75h")]
    [InlineData(0, -17, "+0.0h")]
    [InlineData(3921, 0, "-1.09h")]
    public void FormatsHours(int sourceSeconds, int targetSeconds, string expected)
    {
        var result = TimeDifference.Format(
            Offset.FromSeconds(sourceSeconds),
            Offset.FromSeconds(targetSeconds));

        Assert.Equal(expected, result);
    }
}
