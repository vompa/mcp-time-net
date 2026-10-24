# PRD – mcp-time (rebuild on .NET 10)

> Describes the what, not the how. Terms: `GLOSSARY.md`. Expected values with their source: `parity/`.
> Precedence when the analysis contradicts itself: tests > code > README.

## Purpose

A small local MCP server that gives an AI model two abilities: report the current time in any time zone, and convert a time between time zones. Both are correct across daylight saving time, quarter-hour offsets, and date boundaries.

## Principles

- **P1 Correct above all else.** Offsets, daylight saving flags, and weekdays apply to the actual moment in each zone.
- **P2 Compatible for callers.** Tool names, parameters, result fields, and annotations stay the same, so existing clients and prompts keep working.
- **P3 Strict input, clear errors.** Only exact IANA names, the same on all platforms. Error texts say what is wrong, without internals.
- **P4 Read-only and local.** No network, no files except the time zone database. stdout belongs only to the MCP transport.
- **P5 Testable without a real clock.** "Now" comes from a replaceable time source.

## Users

| Role | Typical tasks |
|---|---|
| AI model (through an MCP client) | "What time is it in Tokyo?", "What is 14:00 Vienna in New York?" |
| Operator | Install the server, override the local time zone if needed |

## Features

### F1 Server & protocol
**Story**: As an MCP client, I want to connect and see the tools with their schemas, whichever protocol generation I speak.
- AC1 Given a client with 2025-11-25, When `initialize`, Then server info `mcp-time` with a version, capability `tools`, no prompts and no resources.
- AC2 Given a client with 2026-07-28, When it makes a sessionless request, Then the same tool list as in AC1.
- AC3 When `tools/list`, Then exactly `get_current_time` and `convert_time`, each with an input schema, an output schema, and the annotations readOnly true, destructive false, idempotent true, openWorld false.
- AC4 Given a call to `no_such_tool`, Then JSON-RPC error `-32602` with the tool name in the message.
- AC5 The parameter descriptions name the local time zone as the default when the user gives none.

### F2 Current time
**Story**: As a model, I want to learn the current time in a time zone, so that I answer time questions correctly.
- AC1 Given "now" = 2024-01-01 12:00:00Z, When `get_current_time{timezone:"UTC"}`, Then `{timezone:"UTC", datetime:"2024-01-01T12:00:00+00:00", day_of_week:"Monday", is_dst:false}` as `structuredContent` and as a JSON text block.
- AC2 Daylight saving time, quarter-hour offsets, and the date boundary as in `parity/business-cases.md` B-NOW.
- AC3 The time zone name comes back as it was requested.
- AC4 If `timezone` is missing or empty, Then `isError` with "Missing required argument: timezone". The local time zone is **not** silently substituted here.
- AC5 Unknown name, wrong letter case, or path, Then `isError`. The text contains "Invalid timezone" and the value.
- Additional, unknown arguments are ignored.

### F3 Convert time
**Story**: As a model, I want to convert a time from one zone to another.
- AC1 Given "now" = 2024-01-01 00:00Z, When `convert_time{source_timezone:"Europe/London", time:"12:00", target_timezone:"Europe/Warsaw"}`, Then target `2024-01-01T13:00:00+01:00`, `time_difference:"+1.0h"`.
- AC2 The reference date is "today" in the source zone (for example, 20:00Z is already Tuesday in Tokyo). With the new optional `date`, that date applies instead (see improvements).
- AC3 Difference format: whole hours with one decimal place, fractions without superfluous zeros (`+5.75h`, `+5.5h`, `-4.75h`, `+0.0h`).
- AC4 Skipped wall-clock time (02:30 New York on 2024-03-10), Then `isError`. The text names the time, zone, date, and "skipped by a clock change".
- AC5 Ambiguous wall-clock time (01:30 New York on 2024-11-03), Then first occurrence (daylight saving time, -04:00), difference to UTC `+4.0h`.
- AC6 `time` in the format `H:M` to `HH:MM` (24 h). Anything else, Then `isError` "Invalid time format. Expected HH:MM [24-hour format]".
- AC7 Empty source or target zone, Then "Missing required argument: source_timezone" or "… target_timezone".

### F4 Start & local time zone
- AC1 `--local-timezone Europe/Vienna` overrides the detected zone, and the parameter descriptions name it.
- AC2 Invalid value, Then the process ends before the transport with exactly one line on stderr: `Error: invalid --local-timezone '<value>': not a known IANA timezone name`, exit code ≠ 0.
- AC3 An empty value counts as not set. If the system zone cannot be determined, `UTC` applies.
- AC4 `--help` describes the server, and unknown options are rejected.

## API (contract)

MCP over stdio, both protocol generations. Tools, parameters, and result fields as in `analysis.md` → Features/Interfaces. Business errors as a tool result with `isError`, unknown tool as JSON-RPC `-32602`. The rebuild writes the description texts itself. They must state the purpose, the formats, and the local time zone default.

## Improvements on purpose

- **Optional `date` (YYYY-MM-DD) on `convert_time`.** Appointments on other days are converted with the correct offset. Without `date`, the previous behavior applies, so this is backward compatible.
- **Own, clear error texts** without Python internals. The information stays the same (parity checks content, not wording).
- **Schema titles chosen deliberately** instead of a by-product of class names.
- **Unknown tool** handled directly by the SDK, without a middleware workaround.
- Further improvements from the target stack research: see `research.md` → Decisions.

## Non-goals

- More tools (calendar, holidays, world clock), localized weekdays, HTTP transport.
