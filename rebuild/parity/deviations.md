# Deviation log

Verdict: bug in the rebuild / improvement on purpose (+ PRD reference).

| Case | Original (per the analysis) | Rebuild | Verdict |
|---|---|---|---|
| `convert_time` with a date | no `date`, always "today" | optional `date` (B-NEW-1..3) | improvement on purpose, PRD "Improvements on purpose" |
| Error text for an invalid time zone | Python wording ("No time zone found with key '…'") | own text with "Invalid timezone" and the value | improvement on purpose |
| Schema title | from the pydantic class names ("Is Dst") | chosen deliberately | improvement on purpose |
| Error text for a missing argument (B-ERR-1/2) | "Missing required argument: timezone" | same start plus a format hint "Expected an IANA time zone name such as 'Europe/Vienna'." | improvement on purpose, PRD "Own, clear error texts" |
| `is_dst` for negative daylight saving time (Europe/Dublin) | probably `bool(dst())` from tzdata, not proven by tests | tzdb savings ≠ 0: winter `true` (savings −01:00), summer `false`; test `NegativeSavingsCountAsDaylightSavingTime` | deliberate (user, 2026-10-24, plan slice 02, assumption 1): follows the tzdb and Irish law, but contradicts everyday understanding |
| Range for `date` | – (no `date` in the original) | only `0001-01-02` to `9999-12-29`, otherwise "Invalid date" with format and range; `null` or `""` = today in the source zone | improvement on purpose (plan slice 03, assumption 2, upper limit by task review): prevents an overflow and with it a generic SDK error |
| Offsets with a seconds part (historical local time) | – (not reachable without `date`) | printed in full, e.g. `1850-01-01T12:00:00+01:05:21`; otherwise `+HH:MM` | improvement on purpose (plan slice 03, assumption 3), like Python `isoformat`; not RFC 3339 |
| Error texts of `convert_time` (B-ERR-6, B-EDGE-2, B-NEW-2) | Python wording | own texts: "Invalid time format. Expected HH:MM [24-hour format] …" with the value, "Invalid date …" with format and range, skipped wall-clock time with time, zone, date and "skipped by a clock change", "Missing required argument: time …" | improvement on purpose, PRD "Own, clear error texts" |
| Several invalid arguments | not defined | only the first error, in the order `source_timezone`, `time`, `target_timezone`, `date` | deliberate (plan slice 03), test `FirstInvalidArgumentWinsAsync` |
| Time format | `H:M` to `HH:MM` | same, but ASCII digits only, no spaces, no trailing line break | improvement on purpose (plan slice 03, assumption 1) |
| Command-line messages (I-CLI-2, I-CLI-3) | English messages of the Python parser | `--help` and parse errors from System.CommandLine in the UI language of the system; only the error line for `--local-timezone` is fixed English (PRD F4 AC2) | deliberate (plan slice 04, assumption 4) |
| `--version` | – | prints the version (built into System.CommandLine) | improvement on purpose (plan slice 04, assumption 4) |
| Detection of the local zone on Windows | `tzlocal` | Windows ID via ICU with the region of the system (`W. Europe Standard Time` in AT → `Europe/Vienna`), otherwise without region, otherwise `UTC` | improvement on purpose (plan slice 04, assumption 3) |
| `--local-timezone` with control characters or `@file` | not defined | control characters as `\uXXXX` in the error line; response files off, `@…` is a normal (invalid) value | deliberate (plan slice 04, assumption 6, final review) |
| Distribution | Python package, `uvx mcp-server-time` | framework-dependent `dotnet tool` `Vompa.McpTime` with `.mcp/server.json`, started with `dotnet dnx`; not yet published | decision D17, plan slice 04, assumption 8 |
| Configuration from the working directory | – | no `appsettings*.json`, environment variables only | deliberate (final review, slice 04): clients start the tool in arbitrary folders |
| Non-string value for `timezone` (e.g. `42`) | rejected by pydantic in the original, wording not part of the contract | `isError` "Invalid timezone" with the raw JSON value; JSON `null` counts as missing | improvement on purpose (plan slice 02, assumption 3) |
