# Open questions

## Architecture-relevant

> As of 2026-10-23: questions 1 and 3 decided (NodaTime, dotnet tool through dnx) – see `research.md` D3, D17. Question 2 adopted in the PRD as an improvement.

1. **Time zone library**: the built-in `TimeZoneInfo` (IANA IDs since .NET 6, ICU) or NodaTime? `[Assumption]` Decision in `research.md`.
2. **Optional date on `convert_time`**: Should the rebuild accept an optional `date` (YYYY-MM-DD), so that appointments on other days are converted correctly? `[Assumption]` Yes, as a deliberate improvement. Without `date`, "today" in the source zone still applies, so the behavior for existing callers stays the same.
3. **Distribution**: `dotnet tool` through NuGet/`dnx`, self-contained binaries, or Docker? `[Assumption]` Decision in `research.md`.

## Business

4. **Error texts**: The rebuild writes its own clear texts without Python internals. It still gives the same information: what is invalid, which value, which format is expected. `[Assumption]` Yes (implementation-bound according to parity).
5. **Time difference also as a number?** For example `time_difference_hours: 5.75` next to the string. `[Assumption]` No. Can come later as an improvement without breaking the contract.
6. **Localized weekday?** `[Assumption]` No, English stays (contract with models).

## Dropped in the rebuild

- Python entry points (`python -m`), freezegun-specific test mechanics, the middleware workaround for unknown tools (the .NET SDK handles it, see `research.md`).
