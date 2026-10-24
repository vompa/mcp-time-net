namespace McpTime.Core.Tests;

public class ReferenceDateTests
{
    [Theory]
    [InlineData("2024-07-15")]
    [InlineData("2024-02-29")]
    [InlineData("0001-01-02")]
    [InlineData("9999-12-29")]
    public void ValidDatesAreAccepted(string text)
    {
        Assert.True(ReferenceDate.TryParse(text, out var date));
        Assert.Equal(text, date.ToString());
    }

    [Theory]
    [InlineData("2024-02-30")]
    [InlineData("15.07.2024")]
    [InlineData("2024-1-5")]
    [InlineData(" 2024-01-01")]
    [InlineData("+2024-01-01")]
    [InlineData("")]
    public void InvalidDatesAreRejected(string text)
    {
        Assert.False(ReferenceDate.TryParse(text, out var date));
        Assert.Null(date);
    }

    [Theory]
    [InlineData("0000-01-01")]
    [InlineData("-0001-01-01")]
    [InlineData("0001-01-01")]
    [InlineData("9999-12-30")]
    [InlineData("9999-12-31")]
    public void OutOfRangeDatesAreRejected(string text)
    {
        Assert.False(ReferenceDate.TryParse(text, out var date));
        Assert.Null(date);
    }
}
