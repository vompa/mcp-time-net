# CLAUDE.md – mcp-time (rebuild on .NET 10)

A small local MCP server (stdio) with two read-only tools: the current time in an IANA time zone, and conversion of a wall-clock time between time zones. A rebuild of the MCP reference server "time" on .NET 10 – not a copy, but built better.
Behavior: `rebuild/PRD.md` · Terms: `rebuild/GLOSSARY.md` · Technical decisions: `rebuild/research.md` (D1–D20) · Goal: all cases in `rebuild/parity/` green.

## Architecture

```
src/
  McpTime.Host     → McpTime.Server, McpTime.Core   (program start, System.CommandLine, Generic Host, stdio)
  McpTime.Server   → McpTime.Core                    (MCP adapter: tool registration, schemas, annotations, CallToolResult)
  McpTime.Core     → (NodaTime only)                 (business logic: TimeZoneName, TimeResult, conversion, time difference)
tests/
  McpTime.Core.Tests       business cases (rebuild/parity/business-cases.md), FakeTimeProvider
  McpTime.Protocol.Tests   interface cases (rebuild/parity/interface-cases.md), MCP client against the built server, JSON snapshots (own comparison, D14)
  McpTime.ArchitectureTests
```

- Arrows = allowed dependencies, nothing else. `ModelContextProtocol` only in Host and Server, `System.CommandLine` only in Host.
- **Enforced** by `McpTime.ArchitectureTests` (NetArchTest): dependency directions, no MCP SDK in Core, no NodaTime in Server, no `Console.Out`/`Console.WriteLine` except in the transport.
- `TreatWarningsAsErrors`, nullable on, `AnalysisLevel latest-recommended`, trim analyzer on (D19).
- `.slnx`, central package management (`Directory.Packages.props`), `global.json` with Microsoft Testing Platform (D14).

## Hard rules

- ≤ 300 lines per file, one responsibility per class.
- **stdout belongs only to the MCP transport**; logs to stderr (D16).
- "Now" only through `TimeProvider` – never `DateTime.Now`, `DateTimeOffset.UtcNow`, or `SystemClock` directly (D13).
- Time zones only through the NodaTime tzdb; a name is valid only if it is exactly (ordinal) in `DateTimeZoneProviders.Tzdb.Ids` (D3, D4). No `TimeZoneInfo` in Core – only in the Host to detect the local zone (D5).
- Business errors as a self-built `CallToolResult { IsError = true }` with a clear text; never `McpException`/`McpProtocolException` for business errors (D7). Error texts name the field, the value, and the expected format.
- Set all four tool annotations explicitly (D9); register tools explicitly, not by reflection scan (D12); JSON only through a `JsonSerializerContext` (D11).
- Tool names, parameters, and result fields stay compatible with the original (PRD P2). Write the description texts yourself: purpose, formats, local zone as the default.
- No new packages without asking. Approved: `ModelContextProtocol` 2.3.x, `NodaTime` 3.3.x, `System.CommandLine` 2.0.x, `Microsoft.Extensions.Hosting`, `xunit.v3`, `Microsoft.Extensions.TimeProvider.Testing`, `NetArchTest.eNhancedEdition`. No Verify (sponsorship declaration from v33, see `research.md` D14).
- The original is never built, started, or read. If something is missing in the plan, that is a gap → ask.

## Forbidden (legacy)

- Treating the Python error wording or pydantic schema titles as a contract.
- Middleware workaround for unknown tools (the SDK handles it, D8).
- Platform-dependent time zone checks (letter case, Windows IDs, paths).

## Commands

```bash
dotnet build -warnaserror
dotnet test                                  # all tests (Core, protocol, architecture)
dotnet run --project src/McpTime.Host -- --local-timezone Europe/Vienna
dotnet pack src/McpTime.Host -c Release -o artifacts/package      # dotnet tool (D17)
dotnet dnx Vompa.McpTime --source artifacts/package --yes -- --help   # run from the package (`--` needed for --help)
npx @modelcontextprotocol/inspector --cli dotnet run --project src/McpTime.Host --method tools/list
dotnet format --verify-no-changes
```

## Way of working

1. Read the slice file (`rebuild/slices/NN-*.md`), the PRD sections it names, and the decisions in `rebuild/research.md`.
2. Present a plan (files, classes, tests), save it in `rebuild/plans/`, wait for approval.
3. Write the slice's tests first – they must be red.
4. Implement the minimum until green, then clean up.
5. Check the Definition of Done, record deviations in `rebuild/parity/deviations.md`, tick the slice in `rebuild/checklist.md`.
6. One commit per green slice. Unclear = ask.
