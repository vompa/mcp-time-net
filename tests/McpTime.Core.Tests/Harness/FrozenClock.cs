using System.Globalization;
using Microsoft.Extensions.Time.Testing;

namespace McpTime.Core.Tests.Harness;

public static class FrozenClock
{
    private static readonly string[] s_formats = ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm"];

    public static FakeTimeProvider At(string utcIso)
    {
        var instant = DateTimeOffset.ParseExact(
            utcIso,
            s_formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        return new FakeTimeProvider(instant);
    }
}
