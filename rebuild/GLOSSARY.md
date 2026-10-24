# Glossary

| Term | Code | Meaning |
|---|---|---|
| IANA time zone | `TimeZoneName` | exact key of the time zone database, e.g. `Europe/Vienna` |
| Local time zone | `LocalTimeZone` | from `--local-timezone`, else detected from the system, else `UTC` |
| Time result | `TimeResult` | `timezone`, `datetime` (ISO 8601 with offset, seconds), `day_of_week` (English), `is_dst` |
| Conversion result | `TimeConversionResult` | `source`, `target` (each a `TimeResult`), `time_difference` |
| Time difference | `TimeDifference` | target offset − source offset at the converted moment, e.g. `+5.75h` |
| Wall-clock time | `WallClockTime` | clock time `HH:MM` in a zone |
| Reference date | `ReferenceDate` | date of the conversion: `date`, else "today" in the source zone |
| Skipped wall-clock time | – | clock time skipped by a daylight saving jump → error |
| Ambiguous wall-clock time | – | clock time that occurs twice at a clock change → first occurrence |
| Time source | `TimeProvider` | replaceable source for "now" (tests: frozen) |
| Protocol generation | – | 2025-11-25 (handshake) or 2026-07-28 (sessionless) |
