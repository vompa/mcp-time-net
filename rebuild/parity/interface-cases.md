# Parity – interface contract (keep compatible)

Each case becomes an acceptance test: An MCP client starts the rebuild over stdio and checks the response against the values here.

| ID | Check | Expected |
|---|---|---|
| I-INIT-1 | `initialize` with 2025-11-25 | Server name `mcp-time`, version set, capability `tools`; `ping` is answered |
| I-INIT-2 | sessionless request with 2026-07-28 | Response in the new generation; `ping` not offered |
| I-LIST-1 | `tools/list` (both generations) | exactly `get_current_time`, `convert_time`, same order, same in both generations |
| I-LIST-2 | Input schema `get_current_time` | Required field `timezone` (string) |
| I-LIST-3 | Input schema `convert_time` | Required fields `source_timezone`, `time`, `target_timezone` (string); optional `date` (string, new) |
| I-LIST-4 | Output schemas | `TimeResult` with required fields `timezone`, `datetime`, `day_of_week` (string), `is_dst` (boolean); `TimeConversionResult` with `source`, `target`, `time_difference` |
| I-LIST-5 | Annotations of both tools | readOnly true, destructive false, idempotent true, openWorld false |
| I-LIST-6 | Parameter descriptions with `--local-timezone Europe/Vienna` | name `Europe/Vienna` as the default |
| I-LIST-7 | `prompts/list`, `resources/list` | empty or not offered |
| I-RES-1 | successful tool call | `structuredContent` = result object; one text block with the same object as JSON; `isError` false |
| I-ERR-1 | `tools/call` with `no_such_tool` | JSON-RPC error `-32602`, message contains `no_such_tool` |
| I-ERR-2 | unknown method | JSON-RPC error (method not found) |
| I-ERR-3 | business error | Tool result with `isError: true`, no JSON-RPC error |
| I-ERR-4 | additional unknown argument | is ignored, the call succeeds |
| I-CLI-1 | `--local-timezone Not/AZone` | Exit ≠ 0, exactly one line on stderr with the value, no stack trace, no path |
| I-CLI-2 | `--help` | describes the server and the option |
| I-CLI-3 | unknown option | Exit ≠ 0 with a hint |
| I-LOG-1 | stdout during operation | JSON-RPC messages only |
