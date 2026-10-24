namespace McpTime.Core;

/// <summary>The same moment in the source and the target zone, plus the offset difference between them.</summary>
public sealed record TimeConversionResult(TimeResult Source, TimeResult Target, string TimeDifference);
