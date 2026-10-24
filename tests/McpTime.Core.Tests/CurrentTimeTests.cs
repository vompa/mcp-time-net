using McpTime.Core.Tests.Harness;

namespace McpTime.Core.Tests;

public class CurrentTimeTests
{
    [Theory]
    [InlineData("B-NOW-1", "2024-01-01 12:00:00", "UTC", "2024-01-01T12:00:00+00:00", "Monday", false)]
    [InlineData("B-NOW-2", "2024-01-01 12:00:00", "Europe/Warsaw", "2024-01-01T13:00:00+01:00", "Monday", false)]
    [InlineData("B-NOW-3", "2024-07-01 12:00:00", "Europe/Warsaw", "2024-07-01T14:00:00+02:00", "Monday", true)]
    [InlineData("B-NOW-4", "2024-01-01 12:00:00", "America/New_York", "2024-01-01T07:00:00-05:00", "Monday", false)]
    [InlineData("B-NOW-5", "2024-01-01 12:00:00", "Australia/Sydney", "2024-01-01T23:00:00+11:00", "Monday", true)]
    [InlineData("B-NOW-6", "2024-07-01 12:00:00", "Australia/Sydney", "2024-07-01T22:00:00+10:00", "Monday", false)]
    [InlineData("B-NOW-7", "2024-03-10 06:59:59", "America/New_York", "2024-03-10T01:59:59-05:00", "Sunday", false)]
    [InlineData("B-NOW-8", "2024-03-10 07:00:00", "America/New_York", "2024-03-10T03:00:00-04:00", "Sunday", true)]
    [InlineData("B-NOW-9", "2024-10-27 00:30:00", "Europe/London", "2024-10-27T01:30:00+01:00", "Sunday", true)]
    [InlineData("B-NOW-10", "2024-10-27 01:30:00", "Europe/London", "2024-10-27T01:30:00+00:00", "Sunday", false)]
    [InlineData("B-NOW-11", "2024-01-01 12:00:00", "Australia/Lord_Howe", "2024-01-01T23:00:00+11:00", "Monday", true)]
    [InlineData("B-NOW-12", "2024-01-01 12:00:00", "Asia/Kathmandu", "2024-01-01T17:45:00+05:45", "Monday", false)]
    [InlineData("B-NOW-13", "2024-01-01 12:00:00", "Pacific/Kiritimati", "2024-01-02T02:00:00+14:00", "Tuesday", false)]
    [InlineData("B-NOW-14", "2024-01-01 05:00:00", "Pacific/Pago_Pago", "2023-12-31T18:00:00-11:00", "Sunday", false)]
    [InlineData("B-NOW-15", "2024-07-01 12:00:00", "Etc/GMT+5", "2024-07-01T07:00:00-05:00", "Monday", false)]
    public void MatchesParityCase(string id, string nowUtc, string zone, string datetime, string dayOfWeek, bool isDst)
    {
        var expected = new TimeResult(zone, datetime, dayOfWeek, isDst);

        var result = CurrentTimeAt(nowUtc, zone);

        Assert.True(expected == result, $"{id}: expected {expected} but was {result}");
    }

    [Fact]
    public void NegativeSavingsCountAsDaylightSavingTime()
    {
        Assert.Equal(
            new TimeResult("Europe/Dublin", "2024-01-01T12:00:00+00:00", "Monday", true),
            CurrentTimeAt("2024-01-01 12:00:00", "Europe/Dublin"));
        Assert.Equal(
            new TimeResult("Europe/Dublin", "2024-07-01T13:00:00+01:00", "Monday", false),
            CurrentTimeAt("2024-07-01 12:00:00", "Europe/Dublin"));
    }

    [Fact]
    public void AliasIsAcceptedAndReturnedAsRequested()
    {
        var result = CurrentTimeAt("2024-01-01 12:00:00", "US/Eastern");

        Assert.Equal("US/Eastern", result.Timezone);
        Assert.Equal("2024-01-01T07:00:00-05:00", result.Datetime);
    }

    private static TimeResult CurrentTimeAt(string nowUtc, string zone)
    {
        Assert.True(TimeZoneName.TryCreate(zone, out var name));
        return new CurrentTime(FrozenClock.At(nowUtc)).In(name);
    }
}
