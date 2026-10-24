# Analysis – mcp-time (MCP reference server "time")

> Source: `modelcontextprotocol/servers`, folder `src/time` @ `51b6382`, package version 2026.10.19, license MIT. 310 lines of production code, 4 test files with about 1,900 lines.

## Stack & runtime

- Python ≥ 3.10, MCP Python SDK 2.x (`MCPServer`, high-level API), pydantic for result models, `zoneinfo` + `tzdata` for IANA time zones, `tzlocal` for the local time zone.
- Started from the console (`mcp-server-time`, also `python -m mcp_server_time`), stdio transport. Distributed through `uvx`, `pip`, and Docker.
- Only option: `--local-timezone <IANA name>`.
- Tests: pytest with a frozen clock (freezegun), protocol tests through a real MCP client.

## Domain

- **Time zone**: IANA name such as `Europe/Vienna`. Only the exact key from the time zone database is valid. Lowercase (`europe/warsaw`), paths (`../etc/passwd`, `/etc/localtime`), and unknown names are rejected.
- **Local time zone**: from `--local-timezone`, otherwise detected from the operating system, otherwise `UTC`. An empty override counts as not set. An invalid override ends startup with a one-line error message before the transport opens.
- **Time value (`time`)**: 24-hour format `HH:MM`. Single-digit hours and minutes are accepted (`9:5` = 09:05).
- **Reference date of the conversion**: always "today" in the source time zone. You cannot pass a date.
- **Skipped wall-clock time** (daylight saving jump, for example 02:30 in New York on 2024-03-10): error with time, zone, and date.
- **Ambiguous wall-clock time** (clock set back, for example 01:30 in New York on 2024-11-03): the first occurrence applies, so still daylight saving time.
- **Time difference**: target offset minus source offset at the converted moment, in hours with a sign. Whole hours with one decimal place (`+1.0h`, `+0.0h`, `-5.0h`), fractions without superfluous zeros (`+5.5h`, `+5.75h`, `-4.75h`).
- **Result per moment**: time zone name as requested (not canonicalized), date/time in ISO 8601 to the second with offset, English weekday, daylight saving flag.

## Features

| Tool | Purpose | Input | Result |
|---|---|---|---|
| `get_current_time` | current time in a time zone | `timezone` | `{timezone, datetime, day_of_week, is_dst}` |
| `convert_time` | convert a wall-clock time from one zone to another | `source_timezone`, `time`, `target_timezone` | `{source, target, time_difference}` |

Both tools are read-only: `readOnlyHint` true, `destructiveHint` false, `idempotentHint` true, `openWorldHint` false.
The parameter descriptions name the detected local time zone, so that a model uses it as the default when the user names none.

## Interfaces

- **Server**: name `mcp-time`, version from the package version. No prompts, no resources, no list-changed notifications.
- **Protocol**: both spec generations. 2025-11-25 through the `initialize` handshake (including `ping`), 2026-07-28 sessionless through `server/discover` or a request envelope. The tool list is identical in both.
- **Result shape**: `structuredContent` with the result object plus a text block with the same object as indented JSON. Each tool publishes an `outputSchema`.
- **Errors**:
  - Business errors are tool results with `isError: true`: invalid time zone, wrong time format, empty required field ("Missing required argument: …"), skipped wall-clock time.
  - An unknown tool is a JSON-RPC error `-32602` "Unknown tool: …".
  - Unknown additional arguments are ignored.
- **CLI**: `--help` describes the server, unknown arguments are rejected, `--local-timezone` is checked before startup.

## Non-functional

- Deterministic except for "now". Tests freeze the clock.
- No network access, no file access except the time zone database.
- Platform differences deliberately levelled out: time zone keys are checked equally strictly on all systems (issue #5060, macOS accepted lowercase).
- Error messages at startup show no file paths.

## Existing tests

- `test_server.py`: business logic with a frozen clock: current time in several zones and seasons, conversions with the expected difference, error cases. → Business contract.
- `test_protocol.py`: through a real MCP client. It checks:
  - server info, tool list with input and output schemas
  - result shape
  - time zones across the date boundary and daylight saving time
  - quarter-hour offsets
  - skipped and ambiguous times
  - argument validation
  - behavior of the local time zone

  → Business and interface contract.
- `test_eras.py`: both protocol generations, errors in both, unknown tool and unknown method as JSON-RPC errors, `ping` only in the old generation. → Interface contract.
- `test_entrypoints.py`: CLI options, start over stdio, invalid local time zone. → Interface contract, partly Python-specific (`python -m`).
- **Implementation-bound**:
  - Wording of the Python error messages from `zoneinfo` ("ZoneInfo keys must refer to subdirectories of TZPATH …")
  - Pydantic titles in the schema ("Day Of Week")
  - Python entry points

## Legacy findings

- No date on conversion: "today" in the source zone is always assumed. For appointments on another day, the model therefore converts with the wrong offset whenever a clock change lies in between.
- Error texts contain Python internals (error class wording, `repr` with quotation marks).
- Schema titles from pydantic class names ("Is Dst") are a by-product, not a deliberate contract.
- The middleware for unknown tools works around an SDK shortcoming (python-sdk#3659) and should go once the SDK handles it itself.
- Weekdays in English only, time difference as a string instead of a number.
- Otherwise clean: small files, one responsibility per function, well tested.

## Open questions

See `open-questions.md`.
