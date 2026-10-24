# Target stack research – mcp-time on .NET 10

## Goal & scope

Rebuild of the local MCP server "mcp-time" (stdio, two read-only tools: current time in an IANA time zone; conversion of a wall-clock time between IANA time zones, including DST, skipped and ambiguous local times) from Python (MCP Python SDK 2.x `MCPServer`, `zoneinfo` + `tzdata`, `tzlocal`, pydantic as `structuredContent` with `outputSchema`, pytest + freezegun) to C# / .NET 10.

Areas: MCP server core (tool registration with input/output schema and annotations, both protocol eras 2025-11-25 via `initialize` and 2026-07-28 sessionless, unknown tool as JSON-RPC -32602), time zones (IANA lookup with exact key validation, local time zone with override flag and UTC fallback, DST flags, fractional offsets), parsing/formatting (HH:MM, ISO 8601 with offset, English weekday), errors as `isError` tool results, CLI with `--local-timezone` validation, tests with a frozen clock, distribution (today uvx/pip/Docker).

Pass 1 – BROAD and pass 2 – DEEP done (DEEP sections carry the prefix "DEEP:"). As of: 2026-10-23.

## Versions & support

| Component | Status | Support / note | Source |
|---|---|---|---|
| .NET 10 | LTS, GA 11.11.2025, current patch 10.0.13 (13.10.2026) | supported until 14.11.2028 | https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core |
| .NET 11 | RC2 (13.10.2026, go-live), GA at .NET Conf 10.–13.11.2026 | odd version → STS (cadence rule, the page does not say so explicitly) | same |
| .NET 8 / 9 | end of support 10.11.2026 | irrelevant for the rebuild, but a note for multi-targeting | https://www.elevenforum.com/t/net-8-and-net-9-will-reach-end-of-support-on-november-10-2026.47839/ |
| `ModelContextProtocol` (official C# SDK) | 2.3.0 (20.10.2026); 2.2.0 (13.08.), 2.1.0 (05.08.), 2.0.0 (28.07.2026); last 1.x: 1.4.1 | TFMs: netstandard2.0, net8.0, net9.0, net10.0; deps: `ModelContextProtocol.Core` (=2.3.0), `Microsoft.Extensions.Hosting.Abstractions` ≥ 10.0.13, `Microsoft.Extensions.Caching.Abstractions` ≥ 10.0.13 | https://www.nuget.org/packages/ModelContextProtocol/absoluteLatest |
| NodaTime | 3.3.6 (22.10.2026), tzdb 2026e | 3.3.x has no net6.0 target; tzdb patches ongoing | https://nodatime.org/versions |
| C# | 14 (default in .NET 10) | – | https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14 |
| MCPB manifest | `manifest_version` 0.3 (0.4 for `uv`) | – | https://github.com/modelcontextprotocol/mcpb/blob/main/MANIFEST.md |

## Findings by topic

### MCP C# SDK 2.x – protocol eras, packages, breaking changes

- From v2.0.0 the official SDK (built jointly by Microsoft and the MCP project) implements spec revision **2026-07-28** and stays backward compatible with **2025-11-25 and older**: A v2 server still accepts the legacy `initialize` handshake, and a v2 client falls back to it for older servers. Clients prefer 2026-07-28; `McpClientOptions.ProtocolVersion` allows pinning. (https://devblogs.microsoft.com/dotnet/announcing-v20-of-the-official-mcp-csharp-sdk/)
- 2026-07-28 drops `initialize`/`initialized` and `Mcp-Session-Id`; protocol version and capabilities travel with every request ("discovery-first negotiation", `server/discover`). (same; https://github.com/modelcontextprotocol/csharp-sdk/releases)
- For stdio, the blog post does not say explicitly how both eras are served – according to the release notes, down-level interop is built in; the concrete stdio behavior (for example `server/discover` vs. `initialize` on the same pipe) is **to be verified in DEEP**.
- Packages: `ModelContextProtocol.Core` (low level), `ModelContextProtocol` (stdio server, hosting/DI, attribute discovery), `ModelContextProtocol.AspNetCore` (HTTP), `ModelContextProtocol.Extensions.Tasks`, `ModelContextProtocol.Extensions.Apps` (experimental, `MCPEXP003`). `ModelContextProtocol` is enough for a stdio server. (Blog post)
- Breaking changes in v2 vs. 1.x: HTTP is stateless by default (`HttpServerTransportOptions.Stateless = true`); tasks moved to their own package; sampling/roots/logging deprecated (`MCP9005`), legacy SSE `MCP9004`, stateful-only options `MCP9006`; `Tool.inputSchema` is required on deserialization; structured content for non-object return types is emitted raw (for example `structuredContent: 72` instead of `{ "result": 72 }`); output schemas may be any JSON Schema 2020-12 document (SEP-2106). (https://github.com/modelcontextprotocol/csharp-sdk/releases)
- Changes in 2.0.0 that matter for stdio: over-nested stdio requests return a JSON-RPC parse error; environment variables are no longer logged. (Release notes)
- 2.1.0: `subscriptions/listen` handler, HTTP fallback to `initialize` when `server/discover` fails. 2.2.0: `HttpServerSessionMode` (hybrid HTTP for both eras on one endpoint). 2.3.0: release notes not found in this run → DEEP. (Release notes)
- New APIs (probably irrelevant for this project): MRTR (`InputRequiredException`, `InputRequest.*`), `[McpHeader]`, caching hints. (Blog post)

### MCP C# SDK – tool declaration, annotations, structured content, errors

- Tools: class with `[McpServerToolType]`, methods with `[McpServerTool]`, descriptions via `[Description]`; registration via `AddMcpServer()` + transport (`WithStdioServerTransport()` for stdio) + `WithToolsFromAssembly()`/`WithTools<T>()`. Parameters such as `McpServer`, `RequestContext<CallToolRequestParams>` and DI services are injected. (https://devblogs.microsoft.com/dotnet/announcing-v20-of-the-official-mcp-csharp-sdk/)
- `McpServerToolAttribute` properties (docs reflect the v1 API, check for v2 in DEEP): `Name` (default method name), `Title`, `ReadOnly` (default false), `Idempotent` (false), `Destructive` (default **true**), `OpenWorld` (default **true**), `UseStructuredContent` (false) – fills `OutputSchema` and `StructuredContent`, `OutputSchemaType` (schema from a type other than the return type), `IconSource`, `TaskSupport` (experimental). For read-only time tools, `ReadOnly = true`, `Destructive = false`, `OpenWorld = false` and `Idempotent = true` must therefore be set explicitly. (https://csharp.sdk.modelcontextprotocol.io/v1/api/ModelContextProtocol.Server.McpServerToolAttribute.html)
- Return mapping: `string` → text block; `ContentBlock`/`IEnumerable<…>`/`AIContent` → content list; `CallToolResult` → unchanged (full control, for example `IsError = true`); other types → text block serialized as JSON. (same)
- Errors: `McpException` → error result with the exception message for the client; other exceptions → error result with a generic message (no leak). Alternatively, return `CallToolResult { IsError = true }` explicitly. Important: DataAnnotations only shape the schema; the SDK does **not** validate inputs at runtime – validation is the tool's job. (same)
- Behavior for an unknown tool name (Python: JSON-RPC -32602) is not documented in the sources read → DEEP (check SDK source code / tests).
- `McpServerToolCreateOptions` as a programmatic alternative (among other things, several icons). (https://csharp.sdk.modelcontextprotocol.io/api/ModelContextProtocol.Server.McpServerToolCreateOptions.html)

### Time zones in .NET 10 – `TimeZoneInfo`, IANA, ICU

- `TimeZoneInfo` is immutable and is obtained via `FindSystemTimeZoneById`, `TryFindSystemTimeZoneById`, `Local`, `Utc` or `CreateCustomTimeZone`; `HasIanaId` shows whether the instance carries an IANA ID. Members for DST edge cases: `IsInvalidTime` (gap when clocks move forward), `IsAmbiguousTime` (overlap when clocks move back), `GetAmbiguousTimeOffsets` (both possible offsets), plus `IsDaylightSavingTime`, `GetUtcOffset`. (https://learn.microsoft.com/dotnet/api/system.timezoneinfo)
- Generally known behavior that this run did not confirm in a primary source (→ verify in DEEP): Since .NET 6, `FindSystemTimeZoneById` accepts both IANA and Windows IDs on all platforms and converts internally; on Linux, `Local` comes from `TZ` or `/etc/localtime`, otherwise UTC; `ConvertTime`/`ConvertTimeToUtc` throw `ArgumentException` for an invalid (`Unspecified`) time; `GetAmbiguousTimeOffsets` throws when the time is not ambiguous. Relevant for "exact key validation": If lookups accept Windows IDs or different spellings/aliases, exactness has to be checked separately (for example against `tz.Id` / `GetSystemTimeZones()`).
- `TimeZoneInfo.TryConvertIanaIdToWindowsId` / `TryConvertWindowsIdToIanaId` exist since .NET 6 but work **only with ICU**. ICU is missing when the OS has no ICU (older Windows versions without app-local ICU), when globalization invariant mode is active, or when NLS mode is forced on Windows. App-local ICU is possible as an opt-in. (https://learn.microsoft.com/hu-hu/dotnet/api/system.timezoneinfo.tryconvertianaidtowindowsid?view=netcore-2.2)
- Consequence for the time zone data: On Linux/macOS the tz database comes from the OS (`/usr/share/zoneinfo`); on Windows it comes from the registry + ICU mapping. Results can therefore differ by platform and patch level (the Python original used the bundled `tzdata`). In containers (for example Alpine, chiseled), `tzdata` and possibly ICU must be present, and invariant mode needs attention → DEEP.
- Historical basis for handling ambiguous/invalid points in time with `TimeZoneInfo`: BCL team blog (archived). (https://learn.microsoft.com/en-us/archive/blogs/bclteam/system-timezoneinfo-working-with-ambiguous-and-invalid-points-in-time-josh-free)

### NodaTime as an alternative

- NodaTime (currently 3.3.x according to NuGet hits; `NodaTime.Testing` 3.3.0) ships its own bundled IANA tzdb (`DateTimeZoneProviders.Tzdb`), so results are identical on every platform – comparable to the Python setup with `tzdata`. The provider offers `GetZoneOrNull` (lookup without an exception) and a list of IDs; there is also a BCL provider. (https://nodatime.org/api/NodaTime.DateTimeZoneProviders.html, https://nodatime.org/api/NodaTime.TimeZones.html, https://www.nuget.org/packages/NodaTime.Testing)
- Explicit resolvers for local times (`NodaTime.TimeZones.Resolvers`): `LenientResolver` (ambiguous → earlier, skipped → shifted forward by the gap length), `StrictResolver` (throws `AmbiguousTimeException`/`SkippedTimeException`), single resolvers `ReturnEarlier`, `ReturnLater`, `ThrowWhenAmbiguous`, `ReturnEndOfIntervalBefore`, `ReturnStartOfIntervalAfter`, `ReturnForwardShifted`, `ThrowWhenSkipped`, combinable via `CreateMappingResolver`. In addition, `MapLocal` returns a `ZoneLocalMapping` with count 0/1/2 – directly usable for "non-existent"/"ambiguous" flags. (https://nodatime.org/3.3.x/api/NodaTime.TimeZones.Resolvers.html, https://nodatime.org/3.2.x/api/NodaTime.TimeZones.SkippedTimeResolver.html, https://nodatime.org/3.2.x/api/NodaTime.TimeZones.AmbiguousTimeResolver.html)
- `NodaTime.Testing` provides `FakeClock`; NodaTime uses its own `IClock` instead of `TimeProvider` – a bridge/adapter would be needed if `TimeProvider` is wanted as the standard abstraction (→ check in DEEP whether NodaTime 3.3 has `TimeProvider` interop). (https://www.nuget.org/packages/NodaTime.Testing)
- Trade-off (collected, not rated): `TimeZoneInfo` = no dependency, OS tzdb, ICU dependency, implicit DST handling; NodaTime = additional dependency, bundled and reproducible tzdb, explicit types (`LocalDateTime`, `ZonedDateTime`, `Instant`, `Offset`) and resolvers. Historical context: MSDN Magazine "The Working Programmer – Noda Time". (https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/the-working-programmer-noda-time)

### Testing with a fake clock – `TimeProvider` / `FakeTimeProvider`

- `System.TimeProvider` (in the BCL since .NET 8) is the standard abstraction for time; `GetUtcNow()`, `GetLocalNow()`, `LocalTimeZone`, `TimeProvider.System`. (https://learn.microsoft.com/zh-tw/dotnet/standard/datetime/timeprovider-overview)
- `FakeTimeProvider` in the package `Microsoft.Extensions.TimeProvider.Testing` (namespace `Microsoft.Extensions.Time.Testing`; the docs show 10.x): constructor with start time, `SetUtcNow`, `AdjustTime`, `Advance`, `SetLocalTimeZone(TimeZoneInfo)`, `AutoAdvanceAmount`. This fixes both the frozen clock (freezegun replacement) and the local time zone in tests. (https://learn.microsoft.com/en-us/dotNet/api/microsoft.extensions.time.testing.faketimeprovider?view=net-8.0, source code: https://source.dot.net/Microsoft.Extensions.TimeProvider.Testing/FakeTimeProvider.cs.html)
- Practical article (blog, secondary) on replacing `DateTime.Now` with `TimeProvider`: https://blog.maartenballiauw.be/posts/2026-07-22-timeprovider-and-the-end-of-untestable-datetime-now/

### Project template, structure & distribution (dotnet tool / dnx / server.json / registry)

- Official template `Microsoft.McpServer.ProjectTemplates` (preview), `dotnet new mcpserver`; requires the .NET 10 SDK. Options: framework, transport `local` (stdio) or `remote` (HTTP), native AOT publish, self-contained publish. Default: self-contained tool package for the common RIDs. Generates `Program.cs` (server + transport), a sample tool class and `.mcp/server.json`. (https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)
- Run with `dnx <PackageId>@<version> --yes` (included in the .NET 10 SDK) – the counterpart to `uvx`. `dotnet pack -c Release` creates a tool package plus RID-specific packages according to `<RuntimeIdentifiers>`; all `.nupkg` files must be pushed. Test feed: `https://apiint.nugettest.org/v3/index.json`. NuGet.org shows an "MCP Server" tab with a copyable client config. (same; original announcement: https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/)
- `server.json` follows the MCP registry schema (`https://static.modelcontextprotocol.io/schemas/2025-10-17/server.schema.json`); NuGet uses the first `packages` entry with `registryType: nuget`; `packageArguments` (for example `--local-timezone`) and `environmentVariables` can be declared. (https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)
- Publishing to the official MCP registry: `name` in the namespace `io.github.<user>/<server>`, the README must contain `<!-- mcp-name: … -->` (`PackageReadmeFile`), first wait for the NuGet push and validation, then `mcp-publisher login github` + `mcp-publisher publish .mcp/server.json`. (https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/publish-mcp-registry)
- The csproj properties `PackAsTool`, `PackageType McpServer`, `ToolCommandName` are not explained on the pages read → DEEP (template output or PR https://github.com/dotnet/docs/pull/49301/files).
- Local client configuration: `dotnet run --project …` in `.vscode/mcp.json` or `.mcp.json` (Visual Studio). (Quickstart)

### stdio hosting & logging

- Minimal setup according to the SDK docs: `dotnet add package ModelContextProtocol` + `Microsoft.Extensions.Hosting`; `Host.CreateApplicationBuilder(args)`, `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`, `RunAsync()`. Console logging must be redirected fully to **stderr** with `AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)`, because stdout belongs exclusively to the JSON-RPC transport. (https://csharp.sdk.modelcontextprotocol.io/v1/concepts/getting-started.html)
- The public SDK docs site currently redirects to `/v1/`; no v2-specific concept docs were found in this run → check API details for 2.3 in DEEP against source code/samples in the repo (https://github.com/modelcontextprotocol/csharp-sdk).
- Secondary sources (blogs) on transports/quickstart, for orientation only: https://www.devleader.ca/2026/07/17/mcp-transports-in-c-stdio-vs-streamable-http-vs-legacy-sse

### CLI parsing (`--local-timezone`)

- `System.CommandLine` has been stable since 2.0; current 2.0.13 (13.10.2026), targets net8.0/netstandard2.0; 3.0.0-rc.2 (prerelease) exists in parallel. Alternative: arguments via `Microsoft.Extensions.Configuration` (the host's command-line provider) – choice open. (https://www.nuget.org/packages/system.commandline, docs: https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- Note: `Host.CreateApplicationBuilder(args)` itself interprets `args` as a configuration source – check the interaction with a custom parser in DEEP.

### Snapshot/approval tests

- **Verify** (VerifyTests) is the most widespread snapshot tool in .NET; adapters for xUnit (v3), NUnit, MSTest, TUnit; suitable for checking the JSON of `tools/list` (schemas, annotations) and tool results against accepted `.verified.*` files. Verify announced sponsorship changes in 2026 (check license/funding in DEEP) (clarified 2026-10-23: a sponsorship declaration is required from v33 → no Verify, see D14). (https://docsearch.algolia.com/mcp/docs/repo/verifytests/verify, https://duendesoftware.com/blog/20260409-verify-open-source-sponsorship)
- The test framework landscape (xUnit v3 / MSTest / TUnit on Microsoft.Testing.Platform, `dotnet test` in .NET 10) was not researched in this run → DEEP.

### Containers & globalization (Docker distribution)

- Size-optimized .NET images (Alpine, Mariner/Azure Linux distroless, Ubuntu chiseled) contain **neither ICU nor tzdata** – suitable only for invariant-mode apps. Ubuntu/Debian images have both. Chiseled variants: base (without tzdata/ICU), `-aot` (without tzdata/ICU/stdc++), `-extra` (with tzdata, ICU, stdc++ – for self-contained/single-file with globalization). `runtime-deps` for self-contained/AOT. (https://learn.microsoft.com/dotnet/core/docker/container-images, https://github.com/dotnet/dotnet-docker/discussions/4821)
- This is central for a time zone server: With `TimeZoneInfo`, the image needs tzdata (and ICU for the IANA↔Windows mapping); with the NodaTime tzdb, the server does not depend on the OS tzdata. `InvariantGlobalization=true` affects culture/ICU, not necessarily tzdata (→ DEEP). The page shows .NET 8 tags; verify .NET 10 tags (Ubuntu 24.04 "noble"?) in DEEP.

### MCPB (MCP Bundles)

- `.mcpb` = zip with a local server + `manifest.json` (formerly DXT); CLI `@anthropic-ai/mcpb` for init/pack. Server types `node`, `python`, `uv`, `binary` – a .NET server would work as `binary` (self-contained/AOT executable, separate build per OS/architecture; `.exe` is appended on Windows). `user_config` (string/number/boolean/directory/file) is injected into `mcp_config.args` via `${user_config.KEY}` – for example for `--local-timezone`. Supported by Claude Desktop (macOS/Windows, one-click install), among others. Current `manifest_version` → DEEP (MANIFEST.md). (https://docsearch.algolia.com/mcp/docs/repo/modelcontextprotocol/mcpb)

### DEEP: MCP C# SDK 2.3 – behavior according to the source code

Source: source code `modelcontextprotocol/csharp-sdk`, `main` @ 8b00f20 (23.10.2026), `src/Directory.Build.props` → `VersionPrefix 2.3.0` (https://github.com/modelcontextprotocol/csharp-sdk/tree/main/src). The GitHub release notes for 2.3.0 could not be retrieved in this run (API access blocked), so the statements come directly from the code.

- **Unknown tool → JSON-RPC error -32602, not `isError`.** If no registered tool matches the name, the default handler throws `McpProtocolException("Unknown tool: '<name>'", McpErrorCode.InvalidParams)`. `McpProtocolException` is passed through explicitly in the tool call path (not wrapped in a result) → protocol error as in the Python original. `McpErrorCode.cs` documents -32602 explicitly for "Unknown tool name or invalid protocol-level tool arguments". (https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerImpl.cs, https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/McpErrorCode.cs)
- **Exception mapping in the tool call** (`CreateToolCallErrorResult`): Only `OperationCanceledException` (on cancellation), `McpProtocolException` and `InputRequiredException` (MRTR) are passed through. All other exceptions become `CallToolResult { IsError = true }`; text for `McpException`: **`An error occurred invoking '<tool>': <Message>`**, for other exceptions only `An error occurred invoking '<tool>'.` – that is, the SDK prepends a prefix. For exact error text parity with Python, the tool itself must return a `CallToolResult` with `IsError = true` instead of throwing.
- **Both protocol eras on the same stdio server, without configuration.** Supported versions (`McpProtocolVersions`): 2026-07-28, 2025-11-25, 2025-06-18, 2025-03-26, 2024-11-05. The era is derived per connection from the first request: Requests with per-request `_meta` (protocol version) establish the modern era; `server/discover` without such metadata → -32602; `initialize` on an already modern session → **-32601 MethodNotFound** ("Use server/discover …"); with no established era, `initialize` is negotiated the classic way. `ping` no longer exists in 2026-07-28 (SEP-2575). (https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerImpl.cs, https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/McpSessionHandler.cs)
- **Attribute API still valid in v2.** `McpServerToolAttribute` still has `Name`, `Title`, `Destructive`, `Idempotent`, `OpenWorld`, `ReadOnly`, `UseStructuredContent`, `OutputSchemaType`, `IconSource`; defaults in the code: `Destructive = true`, `Idempotent = false`, `OpenWorld = true`, `ReadOnly = false`. The fields are nullable internally → only explicitly set values are taken over as annotation hints. The v1 docs on the attributes therefore apply to 2.3. (https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerToolAttribute.cs)
- **Output schema / structured content:** `UseStructuredContent = true` (attribute or `McpServerToolCreateOptions`) generates `OutputSchema` via `AIJsonUtilities.CreateJsonSchema` (Microsoft.Extensions.AI, based on `System.Text.Json.Schema.JsonSchemaExporter`) from the return type or `OutputSchemaType`; the `[Description]` of the return value flows into the output schema. Tool names are validated with a regex (invalid name → `ArgumentException` on registration). (https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/AIFunctionMcpServerTool.cs, https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerToolCreateOptions.cs)
- **AOT/trimming:** All SDK packages (`ModelContextProtocol`, `.Core`, `.AspNetCore`, `.Extensions.*`) set `<IsAotCompatible>true</IsAotCompatible>`. Internal serialization uses the source-generated `McpJsonUtilities.JsonContext`. Custom tool parameter and return types under native AOT need their own `JsonSerializerContext` (attribute discovery by reflection with `WithToolsFromAssembly` is critical for trimming → check the samples). Samples in the repo: `QuickstartWeatherServer`, `TestServerWithHosting`, `InMemoryTransport`, `FileBasedMcpServer`, `EverythingServer`. (https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples)

### DEEP: `TimeZoneInfo` in .NET 10 – lookup, aliases, local zone (per docs + runtime source code)

- **Lookup is case-insensitive.** According to the API docs, `id` must match exactly in length but not in case. In the runtime code, all caches (`_systemTimeZones`, `_timeZonesUsingAlternativeIds`) use `StringComparer.OrdinalIgnoreCase`; `Equals`/`GetHashCode` of `TimeZoneInfo` also compare IDs ignoring case. Since .NET 8, `FindSystemTimeZoneById` returns cached instances. (https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.findsystemtimezonebyid?view=net-10.0, https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs)
- **The Unix file lookup, in contrast, is case-sensitive:** On a cache miss, `Path.Combine(<tzdir>, id)` is read (allowed characters only `[A-Za-z0-9/_-]`, no rooted paths). Consequence: `"europe/warsaw"` fails on Linux on the first call, but can hit as soon as `Europe/Warsaw` is already in the case-insensitive cache (for example after `GetSystemTimeZones()`) – **state-dependent behavior**. To match the Python behavior (exact key only), `string.Equals(tz.Id, input, StringComparison.Ordinal)` or a comparison against a custom ID set must therefore be checked as well. (https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.Unix.NonAndroid.cs)
- **Windows IDs on Linux/macOS and IANA IDs on Windows** are accepted as "alternative IDs" via the ICU mapping table; the returned instance then carries the **requested** ID (for example `Id == "W. Europe Standard Time"`, `HasIanaId == false`). Exact IANA validation must therefore reject Windows IDs itself (check `HasIanaId`). On Windows, IANA IDs work only without NLS mode and without invariant mode. (same; Learn page FindSystemTimeZoneById)
- **Backward links** such as `US/Eastern` are separate files in `/usr/share/zoneinfo` on Linux (if the tzdata package installs them) and are therefore found; Python `zoneinfo` with `tzdata` accepts them too → parity depends on the OS package.
- **Enumeration:** On Unix, `GetSystemTimeZones()` reads the IDs from `zone.tab` (under `TZDIR` or `/usr/share/zoneinfo`) – these are only canonical zones with a country assignment, **not** all valid keys (missing, for example, `UTC` aliases other than the built-in `UTC`, `Etc/GMT+5`, backward links). It is therefore unsuitable as a "set of valid keys"; Python `zoneinfo.available_timezones()` returns more. (TimeZoneInfo.Unix.cs, same)
- **Local zone (tzlocal replacement):** Unix: `TZ` environment variable, otherwise `/etc/localtime` (the symlink target gives the IANA ID), otherwise UTC. Windows: registry zone with a Windows ID → convert to IANA with `TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out iana)` (needs ICU; with an optional region parameter). `TimeZoneInfo.Local.HasIanaId` shows whether a conversion is needed. (TimeZoneInfo.Unix.NonAndroid.cs; https://learn.microsoft.com/dotnet/api/system.timezoneinfo)
- **Two invariant switches:** (1) `InvariantGlobalization` (`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`) – disables ICU → no IANA↔Windows mapping, IDs without alternative resolution; (2) the **`System.TimeZoneInfo.Invariant`** feature switch (`DOTNET_SYSTEM_TIMEZONE_INVARIANT`) – then **only UTC** exists, `Local` = UTC, every other lookup → `TimeZoneNotFoundException`. Neither may be active for this server (or only (1), if the Windows ID conversion can be dropped). (https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs)
- DST edge cases: `IsInvalidTime`, `IsAmbiguousTime`, `GetAmbiguousTimeOffsets` (see BROAD). `IsDaylightSavingTime` returns only `bool`; the DST difference (Lord Howe: 30 min) has to be computed as `GetUtcOffset(t) - BaseUtcOffset` – note: `BaseUtcOffset` is the *current* standard offset and is not necessarily correct after historical changes of the standard offset. Not tested empirically in this run (no .NET SDK in the research environment).

### DEEP: NodaTime 3.3.6 – version, tzdb, trade-off for this app

- **Current 3.3.6 (22.10.2026)**, bundled tzdb **2026e**; 3.3.6 also updates the Windows mapping to CLDR v49. The 3.3.x line ships tzdb updates promptly as patch releases (3.3.1 → 2026a, 3.3.2 → 2026b, 3.3.3 → 2026c, 3.3.4 → 2026d, 3.3.5 → 2026e). 3.3.0 (03.01.2026) removed the net6.0 target. (https://nodatime.org/versions, https://nodatime.org/)
- **`TimeProvider` interop** exists since 3.2.0 (extension methods, .NET 8+ target only) – so `FakeTimeProvider` can be used with NodaTime as a single test clock (API details not read in this run). (https://nodatime.org/versions)
- **tzdb origin:** OS-independent, embedded in the assembly (comparable to Python `tzdata`); freshness depends on the package update rather than the OS patch level. The IANA releases 2026c–2026e show that several tz changes appeared in 2026 (→ OS tzdata in containers/Windows can lag behind). (https://www.iana.org/time-zones/releases/2026e)
- **Properties relevant for this app** (sources see BROAD): `MapLocal` → `ZoneLocalMapping.Count` 0 (gap)/1/2 (overlap) delivers the flags "nonexistent"/"ambiguous" directly; `ZonedDateTime.IsDaylightSavingTime()` and `GetZoneInterval(instant).Savings` deliver the DST difference as an `Offset` (+00:30 for Lord Howe) – this matches Python's `dst()` more closely than `TimeZoneInfo` (only `bool`). `DateTimeZoneProviders.Tzdb.GetZoneOrNull(id)` for lookup without an exception; `Tzdb.Ids` as the set of valid keys (to my knowledge it also contains aliases such as `US/Eastern`; case-sensitivity behavior **not verified** in this run).
- **Comparison (collected, decision in DISTILL):** NodaTime = +1 dependency, reproducible tzdb identical on all OSes and in every container (even chiseled/Alpine without tzdata), exact ID set, DST savings as a value, its own resolver mechanism; `TimeZoneInfo` = no dependency, but OS tzdb, case-insensitive/state-dependent lookup, Windows ID aliases, ICU required for the mapping, incomplete `zone.tab` enumeration.

### DEEP: Packaging – tool package, container, MCPB

- **csproj of the official template** (dotnet/docs PR 49301, sample `SampleMcpServer.csproj`): `PackAsTool=true`, `PackageType=McpServer` (enables the "MCP Server" tab on NuGet.org), `RuntimeIdentifiers=win-x64;win-arm64;osx-arm64;linux-x64;linux-arm64;linux-musl-x64`, `SelfContained=true`, `PublishSelfContained=true`, `PublishSingleFile=true`. `ToolCommandName`, `PublishAot` and packing `.mcp/server.json` are not spelled out in the diff (the file is included by code embed) → read them from the generated project when scaffolding with `dotnet new mcpserver`. Result: one pointer package plus one package per RID; `dnx` downloads the matching one. (https://github.com/dotnet/docs/pull/49301/files, https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)
- **Self-contained per RID** means: The server brings its own runtime but still uses **OS tzdata and OS ICU** (with `TimeZoneInfo`). `linux-musl-x64` (Alpine) often has neither tzdata nor ICU → a source of errors with `TimeZoneInfo`, only ICU-relevant (culture) with NodaTime, not for time zones.
- **Container .NET 10:** Default tags (`10.0`) are **Ubuntu 24.04 "noble"** from .NET 10; there are no Debian images for .NET 10 any more. Full Ubuntu images contain ICU and tzdata; chiseled base and `-aot` contain **neither tzdata nor ICU**, `-chiseled-extra` contains both (scheme from the .NET 8 docs, assumed analogous for noble). For stdio in a container, use `docker run -i --rm`. (https://learn.microsoft.com/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu, https://learn.microsoft.com/dotnet/core/docker/container-images)
- **MCPB:** `MANIFEST.md` names **`manifest_version: "0.3"`** as the current spec version; server type `uv` is new from **0.4** (Python-specific). For .NET, `server.type: "binary"` fits, with `compatibility.platforms` (`darwin`, `win32`, `linux`) and `platform_overrides` for platform-specific `command` paths – so one bundle with several self-contained/AOT binaries, or one bundle per platform. (https://github.com/modelcontextprotocol/mcpb/blob/main/MANIFEST.md)

### DEEP: Tests – platform, frameworks, fake clock

- **VSTest vs. Microsoft.Testing.Platform (MTP):** MTP is executable-first (the test project runs like an app, `dotnet run`/`dotnet watch`) and supports native AOT/trimming. MTP support: MSTest, NUnit, **xUnit v3** (v2 only VSTest), **TUnit (MTP only)**. Recommendation: only one platform per repo. (https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview)
- **`dotnet test` in .NET 10:** The default stays VSTest; native MTP mode via `global.json` (key `test.runner = "Microsoft.Testing.Platform"` – key name not seen directly in the primary source, check against the global.json reference); requires the .NET 10 SDK and MTP ≥ 1.7; then **all** test projects must use MTP. Exit code 8 for zero tests. (https://learn.microsoft.com/ar-sa/dotnet/core/tools/dotnet-test-mtp, https://devblogs.microsoft.com/dotnet/dotnet-test-with-mtp/)
- **End-to-end tests against the MCP server:** The SDK repo contains an `InMemoryTransport` sample and `TestServerWithHosting` – patterns for checking `tools/list`/`tools/call` in-process with `McpClient`, including both protocol eras (pin `McpClientOptions.ProtocolVersion`). (https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples)
- **Fake clock:** `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) including `SetLocalTimeZone` (BROAD); NodaTime has `TimeProvider` interop since 3.2 – a single test clock is enough for both variants.

### DEEP: C# 14 / .NET 10 – useful for this app

- C# 14 is the language version of .NET 10. Relevant features: **extension members** (`extension(...)` blocks, for example extension properties on `TimeZoneInfo`/`DateTimeOffset`), **`field` keyword** (validation in a property setter without a backing field), **null-conditional assignment** (`x?.Prop = …`), `nameof` with unbound generics, **file-based apps** (`dotnet run app.cs` with `#:package` directives; the SDK repo has a `FileBasedMcpServer` sample for this – a possible form for a mini server, but unusual for packaging/tests). (https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14, https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples/FileBasedMcpServer)
- The SDK repo itself builds with `LangVersion preview`; C# 14 is enough for a consumer.

## Pitfalls & breaking changes

**MCP SDK**
- Annotation defaults `Destructive = true`, `OpenWorld = true`, `ReadOnly = false`, `Idempotent = false` – set all four explicitly for read-only tools (confirmed by the 2.3 source code).
- Unknown tool → `McpProtocolException` with -32602 (parity with Python given); `McpProtocolException` from tool code is also passed through as a JSON-RPC error – so do **not** throw `McpProtocolException` for business errors.
- `McpException` from the tool → `isError`, but with the SDK prefix `An error occurred invoking '<tool>': …`; other exceptions → generic text only. Exact error texts only via a self-built `CallToolResult { IsError = true }`.
- Inputs are not validated against the schema; validate in the tool.
- v2: `structuredContent` is unwrapped for primitive return types; `Tool.inputSchema` is required.
- Eras: `initialize` after an established 2026-07-28 session → -32601; `server/discover` without a `_meta` version → -32602; `ping` removed in 2026-07-28.
- Reflection-based discovery (`WithToolsFromAssembly`) and custom types without a `JsonSerializerContext` are problematic with native AOT/trimming, even though the SDK itself is `IsAotCompatible`.
- stdout is exclusively for JSON-RPC – log to stderr.

**Time zones**
- `FindSystemTimeZoneById` is case-insensitive (docs), but case-sensitive on Linux at the first file access → the result depends on the cache state. Check exactness yourself (ordinal comparison with `tz.Id`).
- Windows IDs are accepted as aliases everywhere (with ICU); `Id` is then the Windows ID, `HasIanaId == false`.
- `GetSystemTimeZones()` on Linux = only `zone.tab` entries → not a complete list of valid keys.
- `DOTNET_SYSTEM_TIMEZONE_INVARIANT` / feature switch `System.TimeZoneInfo.Invariant` → only UTC exists; `InvariantGlobalization` → no IANA↔Windows mapping (the local zone cannot be determined as an IANA ID on Windows).
- `IsDaylightSavingTime` is `bool` only; the DST difference (Lord Howe 30 min) has to be computed; `BaseUtcOffset` is the current, not the historical, standard offset.
- The OS tzdb level varies (IANA 2026a–e already in 2026); NodaTime 3.3.6 bundles 2026e.

**Packaging/container**
- .NET 10 default images = Ubuntu 24.04, no Debian images any more; chiseled/`-aot`/Alpine without tzdata and ICU.
- RID `linux-musl-x64` (Alpine) in the template default → tzdata/ICU on the target system not guaranteed.
- All RID-specific `.nupkg` files must be pushed, otherwise `dnx` fails on the missing platform.
- MCPB `manifest_version` 0.3 (current according to MANIFEST.md) vs. 0.4 (needed only for the `uv` type).

**Tests**
- MTP mode in `global.json` applies to the whole solution – VSTest projects next to it are an error; xUnit only in v3 on MTP.

## Decisions for this rebuild

| # | Decision | Finding | Source |
|---|---|---|---|
| D1 | `net10.0`, C# 14, no multi-targeting | .NET 10 LTS until 14.11.2028; .NET 8/9 end 10.11.2026 | Versions & support |
| D2 | MCP via `ModelContextProtocol` 2.3.x with the stdio transport in the generic host | official SDK, serves both protocol generations | SDK section, NuGet |
| D3 | Time zones with **NodaTime 3.3.x** (bundled tzdb), not `TimeZoneInfo` | `TimeZoneInfo` is case-insensitive (on Linux depending on the cache), accepts Windows IDs, has no complete key list and returns daylight saving time only as bool; NodaTime delivers identical data on all platforms and `MapLocal` reports gap/overlap directly | Time zone sections, pitfalls |
| D4 | Exact check: the name must be in `DateTimeZoneProviders.Tzdb.Ids` as an ordinal match, otherwise "Invalid timezone" | makes P3 identical on all platforms; paths are thereby automatically invalid | NodaTime, pitfalls |
| D5 | Local zone: `TimeZoneInfo.Local` → IANA ID (on Windows via `TryConvertWindowsIdToIanaId`) → check in NodaTime, otherwise `UTC`; `--local-timezone` takes precedence | replacement for `tzlocal` | TimeZoneInfo section |
| D6 | Skipped wall-clock time → own error; ambiguous → earlier mapping (as in the original) via `MapLocal` | `ZoneLocalMapping.Count` 0/1/2 | NodaTime |
| D7 | Business errors as a self-built `CallToolResult { IsError = true }` with own text – no `McpException` | `McpException` gets an SDK prefix, `McpProtocolException` would become a JSON-RPC error | Pitfalls MCP SDK |
| D8 | Unknown tool: use the SDK behavior (-32602), no own middleware | SDK 2.3 already does this | Pitfalls MCP SDK |
| D9 | Set all four annotations explicitly (readOnly, destructive=false, idempotent, openWorld=false) | SDK defaults are destructive/openWorld = true | Pitfalls MCP SDK |
| D10 | Check inputs in the tool itself (empty/missing fields, formats) | the SDK does not validate against the schema | Pitfalls MCP SDK |
| D11 | Result objects as `structuredContent` with an output schema plus a JSON text block; serialization with System.Text.Json and `JsonSerializerContext` | result shape of the original; keeps trimming/AOT open | SDK section |
| D12 | Register tools explicitly instead of `WithToolsFromAssembly` | reflection discovery is problematic with trimming/AOT | Pitfalls MCP SDK |
| D13 | "Now" via `TimeProvider`; tests with `FakeTimeProvider`, bridge to NodaTime via its `TimeProvider` interop | a single test clock for BCL and NodaTime | Tests, NodaTime |
| D14 | Tests with xUnit v3 on Microsoft.Testing.Platform (`global.json`), snapshots of `tools/list` as a custom JSON comparison in the test project | MTP is the standard in .NET 10. Finding: Verify ≥ 33 (after 2026-09-01) requires a sponsorship/license declaration in the build (SponsorCheck SC021, exemptions limited to 12 months); decision by the user 2026-10-23: no Verify | Tests |
| D15 | CLI with `System.CommandLine` 2.0.x (stable) | `--help`, unknown options and the check of `--local-timezone` without a custom build | CLI parsing |
| D16 | Log to stderr only; stdout belongs to the transport | stdio rule | stdio hosting |
| D17 | Distribution as a `dotnet tool` (`PackAsTool`, `PackageType McpServer`, `.mcp/server.json`), started with `dnx`; RID-specific packages only if all are pushed | official .NET way, NuGet MCP tab | Packaging |
| D18 | No container in the first release; if added later: Ubuntu-based image, not chiseled/Alpine | chiseled/Alpine without tzdata/ICU – uncritical with NodaTime, but the local zone is missing | Packaging/container |
| D19 | Native AOT optional later; trim-ready code from the start (D11, D12) | a small server benefits from the startup behavior, the SDK is AOT-ready | SDK section |
| D20 | Parse the optional `date` (YYYY-MM-DD) strictly with NodaTime `LocalDatePattern.Iso` | improvement from the PRD; the ISO format is unambiguous | PRD, NodaTime |


### Confirmed by the user (23.10.2026)
- Time zones with NodaTime (D3).
- Distribution as a `dotnet tool` via `dnx` (D17); container and AOT optional later (D18, D19).

## Not yet covered

(As of after DEEP)
- Release notes for 2.3.0 verbatim (GitHub API blocked in this session; behavior derived from the source code on `main` with `VersionPrefix 2.3.0` instead – tag state not compared).
- The exact JSON schema that `AIJsonUtilities.CreateJsonSchema` produces for a record with nullable properties/`[Description]` (comparison with the pydantic `outputSchema` of the original) – check empirically with the SDK.
- Behavior for missing or wrongly typed arguments (binding errors: `isError` vs. -32602) in SDK 2.3. – moot because of the custom `McpServerTool` classes: the tools receive the arguments raw and check them themselves (D10).
- ~~NodaTime: case sensitivity of `Tzdb.GetZoneOrNull` and whether `Tzdb.Ids` contains aliases; API of the `TimeProvider` extensions.~~ Clarified, see slice 02 findings.
- Empirical test of the `TimeZoneInfo` edge cases (Lord Howe, Kathmandu, `europe/warsaw`, `US/Eastern`) on Linux/Windows – no .NET installation in the research environment.
- Verify the `global.json` key for MTP against the global.json reference (clarified, see Scaffold findings / D14); xUnit v3 vs. MSTest vs. TUnit versions; Verify license/sponsorship status (clarified, see Scaffold findings / D14).
- ~~Template output of `dotnet new mcpserver` (ToolCommandName, server.json packing).~~ Clarified, see slice 04 findings. Open: AOT option; verify the .NET 10 chiseled tag names (`10.0-noble-chiseled-extra`) on MCR.
- ~~CLI parsing (`System.CommandLine` vs. host configuration) and interaction with `Host.CreateApplicationBuilder(args)`.~~ Clarified, see slice 04 findings.
- Culture-independent formatting (English weekday, ISO 8601) – standard BCL knowledge, not examined in depth.

## Scaffold findings (slice 00)

- xunit.v3 4.0.2 needs `UseMicrosoftTestingPlatformRunner` + `OutputType Exe`, no `Microsoft.NET.Test.Sdk`.
- The `global.json` key `test.runner = "Microsoft.Testing.Platform"` is confirmed in the Microsoft docs (`global-json.md`).
- xunit.v3 has no implicit `using Xunit` → `<Using Include="Xunit" />` in `tests/Directory.Build.props`.
- Verify.XunitV3 33.3.4 depends on `xunit.v3.extensibility.core` 4.0.2 but breaks the build with SponsorCheck SC021 without a license declaration (32.0.1 would need xunit.v3 3.2.2) → decision: custom JSON snapshot.
- NetArchTest.eNhancedEdition 1.4.5 (Mono.Cecil 0.11.6) offers `FailingTypes`, not `FailingTypeNames`.
- A `ProjectReference` to an exe copies `McpTime.Host.dll` and `McpTime.Host.runtimeconfig.json` into the test output directory (checked).
- The trim analyzer (`EnableTrimAnalyzer`) reports nothing for `Host.CreateApplicationBuilder` + `AddConsole` + `AddMcpServer().WithStdioServerTransport()`.
- An MCP server without tools does not report the `tools` capability → `tools/list` is "not available"; `WithListToolsHandler` with an empty `ListToolsResult` is the minimal placeholder (replaced by the registered tools in slice 01).
- SDK 2.3.0 negotiates both "2025-11-25" and "2026-07-28" on the same stdio server; `McpClient.NegotiatedProtocolVersion` shows which one applies.

## Slice 01 findings (SDK 2.3.0, source code main)

- `McpServerTool` can be derived from: `protected` constructor; abstract members are `Tool ProtocolTool`, `IReadOnlyList<object> Metadata` and `ValueTask<CallToolResult> InvokeAsync(RequestContext<CallToolRequestParams>, CancellationToken)`.
- `WithTools(IEnumerable<McpServerTool>)` registers ready-made instances without reflection.
- `ToolAnnotations` has four `bool?` hints (`readOnlyHint`, `destructiveHint`, `idempotentHint`, `openWorldHint`).
- `Tool.InputSchema` is `JsonElement`, `Tool.OutputSchema` is `JsonElement?`.
- `AddMcpServer(Action<McpServerOptions>)` sets `ServerInfo` (`Implementation { Name, Version }`, both `required`).
- `McpProtocolException : McpException` carries an `ErrorCode`.
- In 2026-07-28, `ValidateRequestMethodBoundary` rejects `ping` with -32601, and `initialize` on a modern session likewise.
- Raw requests can be sent in a test via `McpSession.SendRequestAsync(JsonRpcRequest)`.
- stdin EOF ends the stdio transport immediately: If requests are sent and stdin is closed right after, the handlers still run (stderr shows `"get_current_time" completed`), but no response arrives on stdout, exit 0. An `echo … | dotnet run` test therefore shows nothing; raw tests must wait for the response before they close stdin (this is what `RawStdioServer` does).

## Slice 02 findings (NodaTime 3.3.6, tzdb 2026e, probe)

- `DateTimeZoneProviders.Tzdb.Ids` contains 597 IDs including aliases (`US/Eastern`, `GMT`, `Etc/UTC`, `Etc/GMT+5`, `UTC`); `Contains` compares ordinally.
- `europe/warsaw`, `../etc/passwd`, `/etc/localtime` and the empty string are not included. `GetZoneOrNull` is equally strict.
- Australia/Lord_Howe has savings +00:30 at 2024-01-01T12:00Z (`IsDaylightSavingTime()` true).
- Europe/Dublin has negative daylight saving time in the tzdb: 2024-01-01T12:00Z savings −01:00 (`is_dst` true), 2024-07-01T12:00Z savings 0 (`false`).
- The pattern `uuuu'-'MM'-'dd'T'HH':'mm':'sso<+HH:mm>` prints `+00:00` for UTC (not `Z`).
- `NodaTime.Extensions.TimeProviderExtensions` exists; `Instant.FromDateTimeOffset(timeProvider.GetUtcNow())` is used instead.

## Slice 03 findings (NodaTime 3.3.6, tzdb 2026e, probe)

- `LocalDatePattern.Iso` (`uuuu'-'MM'-'dd`) rejects `2024-02-30`, `15.07.2024`, `+2024-01-01`, `2024-1-5`, ` 2024-01-01` and full-width digits (for example ２０２４), **but accepts** `0000-01-01` (year 0 = 1 BC) and `-0001-01-01` → additional range `0001-01-02` to `9999-12-29`.
- `(new LocalDate(9999,12,31) + new LocalTime(23,59))` in UTC, then `.ToInstant().InZone(Pacific/Kiritimati)` throws `ArgumentOutOfRangeException` ("daysSinceEpoch") → the range ends on 9999-12-29: In year 9999 the offsets range from −12 h to +14 h (spread 26 h). Pago_Pago 9999-12-30 23:59 → Kiritimati would be at 10000-01-01T00:59 and also throws; 9999-12-29 23:59 from `Etc/GMT+12` to Kiritimati gives `9999-12-31T01:59:00+14:00` (+26.0h).
- Lower limit `0001-01-02`: no overflow possible (NodaTime reaches down to year −9998). In year 1, LMT offsets from `−15:56:08` (Asia/Manila) to `+15:13:42` (America/Metlakatla) apply, a spread of about 31 h; the result can therefore fall in year 0000 (for example America/Adak 0001-01-02 00:00 → `0000-12-31T19:50:30-15:56:08`), valid as an astronomical ISO 8601 year. Chosen as the first day after 0001-01-01 so that `0000`/negative input stays excluded. Final review: all 597 × 597 zone pairs on both limit days at 00:00 and 23:59 without an exception (1,425,636 cases).
- `America/New_York`: 2024-03-10 02:30 → `MapLocal` count 0 (skipped); 2024-11-03 01:30 → count 2, `First()` = `2024-11-03T01:30:00-04:00`.
- Historical local time: Europe/Vienna 1850-01-01 12:00 has offset `+01:05:21` (3921 s), Asia/Kolkata 1900-01-01 12:00 `+05:21:10`. The pattern `o<+HH:mm>` truncates to `+01:05`; `o<+HH:mm:ss>` prints `+01:05:21` → seconds only if the offset has any.
- .NET `\d` also matches Arabic-Indic digits (`١٢`) → `[0-9]`; `$` matches before a trailing `\n` → `\z`.
- Rounding of the time difference: −18 s gives −0.005 h and, with `AwayFromZero`, −0.01 h (not `+0.0h`); the test for "never `-0.0h`" therefore uses −17 s.

## Slice 04 findings (System.CommandLine 2.0.13, template mcpserver, probe)

- System.CommandLine 2.0.13 (`new RootCommand(desc) { option }`, `root.SetAction(...)`, `root.Parse(args).Invoke()`): Parse errors print a localized message to stderr and the help to stdout, exit 1; `--help` exit 0; `--version` prints the assembly version.
- `--local-timezone ""` gives the empty string value; `--local-timezone=` is a parse error "missing argument"; a repeated option is a parse error.
- The host does not get the command line: it belongs to System.CommandLine. Since the final review, the host is built with `Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { DisableDefaults = true })` from environment variables only (for example `Logging__LogLevel__Default`), see below.
- `TimeZoneInfo.TryConvertWindowsIdToIanaId` maps `W. Europe Standard Time` with region `AT` to `Europe/Vienna`, and with region `001` or `""` to `Europe/Berlin`. Under `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` (region then `IV`) it returns `false` without an exception → detection falls back to UTC.
- The template `dotnet new mcpserver` (2026, MCP SDK 2.1.0 in the template) creates a csproj with `PackAsTool`, `PackageType McpServer`, `RuntimeIdentifiers` `win-x64;win-arm64;osx-arm64;linux-x64;linux-arm64;linux-musl-x64`, `SelfContained`, `PublishSingleFile`, `PackageReadmeFile` and packs `.mcp/server.json` to `/.mcp/` (no `ToolCommandName`).
- `server.json`: schema `https://static.modelcontextprotocol.io/schemas/2025-10-17/server.schema.json`, `packages[].registryType` `nuget`, `transport.type` `stdio`, plus `packageArguments` and `environmentVariables`.
- Packaging (task 3, SDK 10.0.402): `dotnet pack src/McpTime.Host -c Release -o artifacts/package` creates `Vompa.McpTime.0.1.0.nupkg` (framework-dependent, 1.88 MB). Contents: `.mcp/server.json`, `README.md`, `tools/net10.0/any/` with `McpTime.Host.dll`, `McpTime.Host.runtimeconfig.json` (RollForward Major), `DotnetToolSettings.xml` and all dependencies including the System.CommandLine language folders; no `runtimes/` except the Windows EventLog of the hosting package.
- dnx call for a local folder: `dotnet dnx Vompa.McpTime --source artifacts/package --yes -- --help` (exit 0, help of the tool). `--source` replaces all feeds. `dnx` alone is not on the PATH in Git Bash → `dotnet dnx`.
- Arguments: Tool options that dnx does not know pass through even without `--` (`dnx Vompa.McpTime --source … --yes --local-timezone Not/AZone` → error line, exit 1; the order does not matter). `--help` without `--`, in contrast, is consumed by dnx itself and shows its help → always put `--` before `--help` (and `--version`), and use `--` consistently in the smoke test and the client configuration.
- Cache: dnx unpacks to `~/.nuget/packages/vompa.mcptime/0.1.0`. A newly packed package with the **same version** is not unpacked again (probe: different `InformationalVersion`, `--version` kept showing the old state) → raise the version locally or remove the folder `vompa.mcptime/0.1.0` from the cache; not a problem on fresh CI runners.
- `server.json` checked against the schema `2025-10-17` (Python `jsonschema` not installed → own stdlib script that evaluates `$ref`, `allOf`, `anyOf`, `type`, `required`, `minLength`, `maxLength`, `pattern`, `enum`, `const` and `not`, 25 checks for `name`, `description`, `version`, `repository`, `packages[]`, `transport`, `packageArguments[]`; not a full validator, formats such as `uri` stay unchecked). Finding: `description` had 108 characters with `maxLength: 100` → shortened to 90; no violations afterwards.
- Final review: `Host.CreateApplicationBuilder()` loads `appsettings*.json` including a file watcher from the working directory in which the MCP client starts the tool (a broken file = crash before the transport) → `HostApplicationBuilderSettings { DisableDefaults = true }`, only environment variables as configuration, logging from the `Logging` section (`Logging__LogLevel__Default` still works); the console lifetime (ends on stdin EOF, exit 0) is preserved, nothing to add. Response files (`@file`) of System.CommandLine are off via `ParserConfiguration.ResponseFileTokenReplacer = null`. On Windows, `dnx` exists only as `dnx.cmd`; clients without a shell (Claude Desktop, Node spawn) do not start it → client configuration with `"command": "dotnet"` and `"args": ["dnx", ...]`.


## Sources

- .NET Support Policy: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- .NET 8/9 end of support (forum post): https://www.elevenforum.com/t/net-8-and-net-9-will-reach-end-of-support-on-november-10-2026.47839/
- NuGet ModelContextProtocol: https://www.nuget.org/packages/ModelContextProtocol/absoluteLatest
- NuGet ModelContextProtocol.Core 2.1.0: https://www.nuget.org/packages/ModelContextProtocol.Core/2.1.0
- Announcement MCP C# SDK v2.0: https://devblogs.microsoft.com/dotnet/announcing-v20-of-the-official-mcp-csharp-sdk/
- csharp-sdk releases: https://github.com/modelcontextprotocol/csharp-sdk/releases
- csharp-sdk repo: https://github.com/modelcontextprotocol/csharp-sdk
- McpServerToolAttribute (v1 API docs): https://csharp.sdk.modelcontextprotocol.io/v1/api/ModelContextProtocol.Server.McpServerToolAttribute.html
- McpServerToolCreateOptions: https://csharp.sdk.modelcontextprotocol.io/api/ModelContextProtocol.Server.McpServerToolCreateOptions.html
- SDK Getting Started: https://csharp.sdk.modelcontextprotocol.io/v1/concepts/getting-started.html
- TimeZoneInfo API: https://learn.microsoft.com/dotnet/api/system.timezoneinfo
- TryConvertIanaIdToWindowsId: https://learn.microsoft.com/hu-hu/dotnet/api/system.timezoneinfo.tryconvertianaidtowindowsid?view=netcore-2.2
- BCL blog ambiguous/invalid: https://learn.microsoft.com/en-us/archive/blogs/bclteam/system-timezoneinfo-working-with-ambiguous-and-invalid-points-in-time-josh-free
- NodaTime Resolvers 3.3: https://nodatime.org/3.3.x/api/NodaTime.TimeZones.Resolvers.html
- NodaTime SkippedTimeResolver / AmbiguousTimeResolver: https://nodatime.org/3.2.x/api/NodaTime.TimeZones.SkippedTimeResolver.html, https://nodatime.org/3.2.x/api/NodaTime.TimeZones.AmbiguousTimeResolver.html
- NodaTime DateTimeZoneProviders / TimeZones: https://nodatime.org/api/NodaTime.DateTimeZoneProviders.html, https://nodatime.org/api/NodaTime.TimeZones.html
- NuGet NodaTime.Testing: https://www.nuget.org/packages/NodaTime.Testing
- MSDN Magazine Noda Time: https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/the-working-programmer-noda-time
- TimeProvider overview: https://learn.microsoft.com/zh-tw/dotnet/standard/datetime/timeprovider-overview
- FakeTimeProvider: https://learn.microsoft.com/en-us/dotNet/api/microsoft.extensions.time.testing.faketimeprovider?view=net-8.0
- FakeTimeProvider source code: https://source.dot.net/Microsoft.Extensions.TimeProvider.Testing/FakeTimeProvider.cs.html
- Blog TimeProvider: https://blog.maartenballiauw.be/posts/2026-07-22-timeprovider-and-the-end-of-untestable-datetime-now/
- Quickstart MCP server .NET: https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server
- Publish to MCP registry: https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/publish-mcp-registry
- NuGet MCP quickstart (blog 2025): https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/
- dotnet/docs PR 49301: https://github.com/dotnet/docs/pull/49301/files
- System.CommandLine NuGet: https://www.nuget.org/packages/system.commandline
- System.CommandLine docs: https://learn.microsoft.com/en-us/dotnet/standard/commandline/
- Verify: https://docsearch.algolia.com/mcp/docs/repo/verifytests/verify
- Verify sponsorship: https://duendesoftware.com/blog/20260409-verify-open-source-sponsorship
- .NET container images: https://learn.microsoft.com/dotnet/core/docker/container-images
- dotnet-docker discussion 4821: https://github.com/dotnet/dotnet-docker/discussions/4821
- MCPB: https://docsearch.algolia.com/mcp/docs/repo/modelcontextprotocol/mcpb
- Dev Leader transports (secondary): https://www.devleader.ca/2026/07/17/mcp-transports-in-c-stdio-vs-streamable-http-vs-legacy-sse
- csharp-sdk source code (main, 2.3.0): https://github.com/modelcontextprotocol/csharp-sdk/tree/main/src
- McpServerImpl.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerImpl.cs
- McpErrorCode.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/McpErrorCode.cs
- McpSessionHandler.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/McpSessionHandler.cs
- McpServerToolAttribute.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerToolAttribute.cs
- AIFunctionMcpServerTool.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/AIFunctionMcpServerTool.cs
- csharp-sdk samples: https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples
- FindSystemTimeZoneById (.NET 10): https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.findsystemtimezonebyid?view=net-10.0
- dotnet/runtime TimeZoneInfo.cs: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs
- dotnet/runtime TimeZoneInfo.Unix.NonAndroid.cs: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.Unix.NonAndroid.cs
- NodaTime home page: https://nodatime.org/
- NodaTime versions: https://nodatime.org/versions
- IANA tzdb 2026e: https://www.iana.org/time-zones/releases/2026e
- C# 14 What's new: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14
- .NET 10 default images Ubuntu: https://learn.microsoft.com/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu
- MCPB MANIFEST.md: https://github.com/modelcontextprotocol/mcpb/blob/main/MANIFEST.md
- Test platforms overview: https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview
- dotnet test MTP: https://learn.microsoft.com/ar-sa/dotnet/core/tools/dotnet-test-mtp
- dotnet test with MTP (blog): https://devblogs.microsoft.com/dotnet/dotnet-test-with-mtp/
