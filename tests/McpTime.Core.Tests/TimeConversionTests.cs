using McpTime.Core.Tests.Harness;

namespace McpTime.Core.Tests;

public class TimeConversionTests
{
    [Theory]
    [InlineData("B-CONV-1", "2024-01-01 00:00", "Europe/London", "12:00", "Europe/Warsaw", "2024-01-01T12:00:00+00:00", "2024-01-01T13:00:00+01:00", false, "+1.0h")]
    [InlineData("B-CONV-2", "2024-01-01 00:00", "UTC", "12:00", "UTC", "2024-01-01T12:00:00+00:00", "2024-01-01T12:00:00+00:00", false, "+0.0h")]
    [InlineData("B-CONV-3", "2024-01-01 00:00", "UTC", "12:00", "Asia/Kathmandu", "2024-01-01T12:00:00+00:00", "2024-01-01T17:45:00+05:45", false, "+5.75h")]
    [InlineData("B-CONV-4", "2024-01-01 00:00", "UTC", "12:00", "Asia/Kolkata", "2024-01-01T12:00:00+00:00", "2024-01-01T17:30:00+05:30", false, "+5.5h")]
    [InlineData("B-CONV-5", "2024-01-01 00:00", "Asia/Kathmandu", "12:00", "Europe/Warsaw", "2024-01-01T12:00:00+05:45", "2024-01-01T07:15:00+01:00", false, "-4.75h")]
    [InlineData("B-CONV-6", "2024-10-28 00:00", "Europe/Warsaw", "12:00", "America/New_York", "2024-10-28T12:00:00+01:00", "2024-10-28T07:00:00-04:00", true, "-5.0h")]
    [InlineData("B-CONV-7", "2024-01-01 00:00", "Europe/Warsaw", "23:00", "Pacific/Kiritimati", "2024-01-01T23:00:00+01:00", "2024-01-02T12:00:00+14:00", false, "+13.0h")]
    [InlineData("B-CONV-8", "2024-01-01 00:00", "UTC", "05:00", "Pacific/Pago_Pago", "2024-01-01T05:00:00+00:00", "2023-12-31T18:00:00-11:00", false, "-11.0h")]
    [InlineData("B-CONV-9", "2024-01-01 00:00", "UTC", "9:5", "UTC", "2024-01-01T09:05:00+00:00", "2024-01-01T09:05:00+00:00", false, "+0.0h")]
    [InlineData("B-CONV-10", "2024-01-01 00:00", "UTC", "00:00", "UTC", "2024-01-01T00:00:00+00:00", "2024-01-01T00:00:00+00:00", false, "+0.0h")]
    [InlineData("B-CONV-11", "2024-01-01 00:00", "UTC", "23:59", "UTC", "2024-01-01T23:59:00+00:00", "2024-01-01T23:59:00+00:00", false, "+0.0h")]
    public void MatchesParityCase(
        string id,
        string nowUtc,
        string source,
        string time,
        string target,
        string sourceDatetime,
        string targetDatetime,
        bool targetIsDst,
        string timeDifference)
    {
        var conversion = new TimeConversion(FrozenClock.At(nowUtc));
        var sourceZone = Zone(source);

        var converted = conversion.TryConvert(
            sourceZone, Time(time), conversion.TodayIn(sourceZone), Zone(target), out var result);

        Assert.True(converted, id);
        Assert.NotNull(result);
        Assert.Equal(source, result.Source.Timezone);
        Assert.Equal(target, result.Target.Timezone);
        Assert.Equal(sourceDatetime, result.Source.Datetime);
        Assert.Equal(targetDatetime, result.Target.Datetime);
        Assert.Equal(targetIsDst, result.Target.IsDst);
        Assert.Equal(timeDifference, result.TimeDifference);
    }

    [Fact]
    public void TodayIsTakenInTheSourceZone()
    {
        var conversion = new TimeConversion(FrozenClock.At("2024-01-01 20:00:00"));
        var tokyo = Zone("Asia/Tokyo");

        var today = conversion.TodayIn(tokyo);
        Assert.True(conversion.TryConvert(tokyo, Time("12:00"), today, Zone("UTC"), out var result));

        Assert.Equal("2024-01-02", today.ToString());
        Assert.Equal("Tuesday", result.Source.DayOfWeek);
    }

    [Fact]
    public void SkippedLocalTimeIsRejected()
    {
        var conversion = new TimeConversion(FrozenClock.At("2024-03-10 12:00:00"));
        var newYork = Zone("America/New_York");

        var converted = conversion.TryConvert(
            newYork, Time("02:30"), conversion.TodayIn(newYork), Zone("UTC"), out var result);

        Assert.False(converted);
        Assert.Null(result);
    }

    [Fact]
    public void AmbiguousLocalTimeTakesTheFirstOccurrence()
    {
        var conversion = new TimeConversion(FrozenClock.At("2024-11-03 12:00:00"));
        var newYork = Zone("America/New_York");

        Assert.True(conversion.TryConvert(
            newYork, Time("01:30"), conversion.TodayIn(newYork), Zone("UTC"), out var result));

        Assert.Equal(new TimeResult("America/New_York", "2024-11-03T01:30:00-04:00", "Sunday", true), result.Source);
        Assert.Equal("2024-11-03T05:30:00+00:00", result.Target.Datetime);
        Assert.Equal("+4.0h", result.TimeDifference);
    }

    [Fact]
    public void DifferenceUsesOffsetsAtTheConvertedMoment()
    {
        var conversion = new TimeConversion(FrozenClock.At("2024-10-27 00:30:00"));
        var london = Zone("Europe/London");

        Assert.True(conversion.TryConvert(
            london, Time("12:00"), conversion.TodayIn(london), Zone("UTC"), out var result));

        Assert.Equal("2024-10-27T12:00:00+00:00", result.Source.Datetime);
        Assert.False(result.Source.IsDst);
        Assert.Equal("+0.0h", result.TimeDifference);
    }

    [Fact]
    public void ExplicitDateIsUsed()
    {
        var conversion = new TimeConversion(FrozenClock.At("2024-01-01 00:00"));

        Assert.True(conversion.TryConvert(
            Zone("Europe/Vienna"), Time("09:00"), Date("2024-07-15"), Zone("America/New_York"), out var result));

        Assert.Equal("2024-07-15T09:00:00+02:00", result.Source.Datetime);
        Assert.Equal("2024-07-15T03:00:00-04:00", result.Target.Datetime);
        Assert.True(result.Source.IsDst);
        Assert.True(result.Target.IsDst);
        Assert.Equal("-6.0h", result.TimeDifference);
    }

    [Fact]
    public void HistoricalOffsetKeepsSeconds()
    {
        var conversion = new TimeConversion(FrozenClock.At("2024-01-01 00:00"));

        Assert.True(conversion.TryConvert(
            Zone("Europe/Vienna"), Time("12:00"), Date("1850-01-01"), Zone("UTC"), out var result));

        Assert.Equal("1850-01-01T12:00:00+01:05:21", result.Source.Datetime);
        Assert.Equal("1850-01-01T10:54:39+00:00", result.Target.Datetime);
        Assert.Equal("-1.09h", result.TimeDifference);
    }

    [Fact]
    public void LatestDateConvertsAcrossTheWidestOffsetSpread()
    {
        var conversion = new TimeConversion(FrozenClock.At("2024-01-01 00:00"));

        Assert.True(conversion.TryConvert(
            Zone("Etc/GMT+12"), Time("23:59"), Date("9999-12-29"), Zone("Pacific/Kiritimati"), out var result));

        Assert.Equal("9999-12-31T01:59:00+14:00", result.Target.Datetime);
        Assert.Equal("+26.0h", result.TimeDifference);
    }

    [Fact]
    public void SecondsInTheSourceOffsetAreKept()
    {
        var conversion = new TimeConversion(FrozenClock.At("2024-01-01 00:00"));

        Assert.True(conversion.TryConvert(
            Zone("Asia/Kolkata"), Time("12:00"), Date("1900-01-01"), Zone("UTC"), out var result));

        Assert.Equal("1900-01-01T12:00:00+05:21:10", result.Source.Datetime);
        Assert.Equal("1900-01-01T06:38:50+00:00", result.Target.Datetime);
        Assert.Equal("-5.35h", result.TimeDifference);
    }

    private static TimeZoneName Zone(string name)
    {
        Assert.True(TimeZoneName.TryCreate(name, out var zone));
        return zone;
    }

    private static WallClockTime Time(string text)
    {
        Assert.True(WallClockTime.TryParse(text, out var time));
        return time;
    }

    private static ReferenceDate Date(string text)
    {
        Assert.True(ReferenceDate.TryParse(text, out var date));
        return date;
    }
}
