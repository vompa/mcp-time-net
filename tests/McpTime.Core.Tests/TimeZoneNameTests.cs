namespace McpTime.Core.Tests;

public class TimeZoneNameTests
{
    [Theory]
    [InlineData("UTC")]
    [InlineData("Europe/Warsaw")]
    [InlineData("Etc/GMT+5")]
    [InlineData("US/Eastern")]
    public void ValidNamesAreAccepted(string value)
    {
        Assert.True(TimeZoneName.TryCreate(value, out var name));
        Assert.Equal(value, name.Value);
        Assert.Equal(value, name.ToString());
    }

    [Theory]
    [InlineData("Invalid/Timezone")]
    [InlineData("europe/warsaw")]
    [InlineData("UTC ")]
    [InlineData(" UTC")]
    [InlineData("../etc/passwd")]
    [InlineData("/etc/localtime")]
    [InlineData("")]
    [InlineData("W. Europe Standard Time")]
    public void InvalidNamesAreRejected(string value)
    {
        Assert.False(TimeZoneName.TryCreate(value, out var name));
        Assert.Null(name);
    }
}
