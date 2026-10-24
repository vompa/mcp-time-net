using NodaTime;

namespace McpTime.Core;

/// <summary>Answers "what time is it now in this zone", using only the injected clock.</summary>
public sealed class CurrentTime(TimeProvider timeProvider)
{
    public TimeResult In(TimeZoneName zone) =>
        TimeResult.At(zone, Instant.FromDateTimeOffset(timeProvider.GetUtcNow()));
}
