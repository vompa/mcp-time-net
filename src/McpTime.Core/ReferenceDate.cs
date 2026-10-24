using System.Diagnostics.CodeAnalysis;
using NodaTime;
using NodaTime.Text;

namespace McpTime.Core;

/// <summary>The calendar date a conversion refers to, strictly <c>YYYY-MM-DD</c> within a range every zone can represent.</summary>
public sealed class ReferenceDate
{
    private static readonly LocalDate s_earliest = new(1, 1, 2);
    private static readonly LocalDate s_latest = new(9999, 12, 29);

    private ReferenceDate(LocalDate value) => Value = value;

    internal LocalDate Value { get; }

    public static bool TryParse(string text, [NotNullWhen(true)] out ReferenceDate? date)
    {
        date = null;
        var parsed = LocalDatePattern.Iso.Parse(text);
        if (!parsed.Success || parsed.Value < s_earliest || parsed.Value > s_latest)
        {
            return false;
        }

        date = new ReferenceDate(parsed.Value);
        return true;
    }

    internal static ReferenceDate From(LocalDate value) => new(value);

    public override string ToString() => LocalDatePattern.Iso.Format(Value);
}
