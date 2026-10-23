# Zielstack-Recherche – mcp-time auf .NET 10

## Ziel & Umfang

Neubau des lokalen MCP-Servers „mcp-time“ (stdio, zwei read-only Tools: aktuelle Zeit in einer IANA-Zeitzone; Umrechnung einer Wanduhrzeit zwischen IANA-Zeitzonen inkl. DST, nicht-existenter und mehrdeutiger lokaler Zeiten) von Python (MCP Python SDK 2.x `MCPServer`, `zoneinfo` + `tzdata`, `tzlocal`, pydantic als `structuredContent` mit `outputSchema`, pytest + freezegun) auf C# / .NET 10.

Bereiche: MCP-Server-Core (Tool-Registrierung mit Input-/Output-Schema und Annotations, beide Protokoll-Ären 2025-11-25 via `initialize` und 2026-07-28 sessionless, unbekanntes Tool als JSON-RPC -32602), Zeitzonen (IANA-Lookup mit exakter Key-Validierung, lokale Zeitzone mit Override-Flag und UTC-Fallback, DST-Flags, gebrochene Offsets), Parsing/Formatierung (HH:MM, ISO 8601 mit Offset, englischer Wochentag), Fehler als `isError`-Tool-Results, CLI mit `--local-timezone`-Validierung, Tests mit eingefrorener Uhr, Distribution (heute uvx/pip/Docker).

Pass 1 – BROAD und Pass 2 – DEEP abgeschlossen (DEEP-Abschnitte mit Präfix „DEEP:“). Stand: 2026-10-23.

## Versionen & Support

| Komponente | Stand | Support / Anmerkung | Quelle |
|---|---|---|---|
| .NET 10 | LTS, GA 11.11.2025, aktueller Patch 10.0.13 (13.10.2026) | Support bis 14.11.2028 | https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core |
| .NET 11 | RC2 (13.10.2026, go-live), GA zur .NET Conf 10.–13.11.2026 | ungerade Version → STS (Kadenz-Regel, Seite nennt es nicht explizit) | dto. |
| .NET 8 / 9 | Support-Ende 10.11.2026 | irrelevant für Neubau, aber Hinweis für Multi-Targeting | https://www.elevenforum.com/t/net-8-and-net-9-will-reach-end-of-support-on-november-10-2026.47839/ |
| `ModelContextProtocol` (offizielles C#-SDK) | 2.3.0 (20.10.2026); 2.2.0 (13.08.), 2.1.0 (05.08.), 2.0.0 (28.07.2026); letzte 1.x: 1.4.1 | TFMs: netstandard2.0, net8.0, net9.0, net10.0; Deps: `ModelContextProtocol.Core` (=2.3.0), `Microsoft.Extensions.Hosting.Abstractions` ≥ 10.0.13, `Microsoft.Extensions.Caching.Abstractions` ≥ 10.0.13 | https://www.nuget.org/packages/ModelContextProtocol/absoluteLatest |
| NodaTime | 3.3.6 (22.10.2026), tzdb 2026e | 3.3.x ohne net6.0-Target; tzdb-Patches laufend | https://nodatime.org/versions |
| C# | 14 (Standard in .NET 10) | – | https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14 |
| MCPB manifest | `manifest_version` 0.3 (0.4 für `uv`) | – | https://github.com/modelcontextprotocol/mcpb/blob/main/MANIFEST.md |

## Erkenntnisse nach Thema

### MCP C# SDK 2.x – Protokoll-Ären, Pakete, Breaking Changes

- Das offizielle SDK (gemeinsam von Microsoft und dem MCP-Projekt) implementiert ab v2.0.0 die Spec-Revision **2026-07-28** und bleibt rückwärtskompatibel zu **2025-11-25 und älter**: Ein v2-Server akzeptiert weiterhin den Legacy-Handshake `initialize`, ein v2-Client fällt bei älteren Servern darauf zurück. Clients bevorzugen 2026-07-28; `McpClientOptions.ProtocolVersion` erlaubt Pinning. (https://devblogs.microsoft.com/dotnet/announcing-v20-of-the-official-mcp-csharp-sdk/)
- In 2026-07-28 entfallen `initialize`/`initialized` und `Mcp-Session-Id`; Protokollversion und Capabilities reisen mit jedem Request („discovery-first negotiation“, `server/discover`). (dto.; https://github.com/modelcontextprotocol/csharp-sdk/releases)
- Für stdio beschreibt der Blogpost nicht explizit, wie beide Ären bedient werden – laut Release Notes ist Down-Level-Interop eingebaut; konkretes stdio-Verhalten (z. B. `server/discover` vs. `initialize` auf derselben Pipe) ist **in DEEP zu verifizieren**.
- Pakete: `ModelContextProtocol.Core` (Low-Level), `ModelContextProtocol` (stdio-Server, Hosting/DI, Attribut-Discovery), `ModelContextProtocol.AspNetCore` (HTTP), `ModelContextProtocol.Extensions.Tasks`, `ModelContextProtocol.Extensions.Apps` (experimentell, `MCPEXP003`). Für einen stdio-Server reicht `ModelContextProtocol`. (Blogpost)
- Breaking Changes v2 ggü. 1.x: HTTP standardmäßig stateless (`HttpServerTransportOptions.Stateless = true`); Tasks in eigenes Paket verschoben; Sampling/Roots/Logging deprecated (`MCP9005`), Legacy-SSE `MCP9004`, stateful-only Optionen `MCP9006`; `Tool.inputSchema` beim Deserialisieren Pflicht; strukturierter Content bei nicht-Objekt-Rückgabetypen wird roh ausgegeben (z. B. `structuredContent: 72` statt `{ "result": 72 }`); Output-Schemas dürfen beliebige JSON Schema 2020-12 Dokumente sein (SEP-2106). (https://github.com/modelcontextprotocol/csharp-sdk/releases)
- stdio-relevante Änderungen in 2.0.0: über-verschachtelte stdio-Requests liefern JSON-RPC Parse Error; Umgebungsvariablen werden nicht mehr geloggt. (Release Notes)
- 2.1.0: `subscriptions/listen`-Handler, HTTP-Fallback auf `initialize` wenn `server/discover` scheitert. 2.2.0: `HttpServerSessionMode` (hybrides HTTP für beide Ären auf einem Endpoint). 2.3.0: Release Notes in diesem Lauf nicht gefunden → DEEP. (Release Notes)
- Neue APIs (für dieses Projekt vermutlich irrelevant): MRTR (`InputRequiredException`, `InputRequest.*`), `[McpHeader]`, Caching-Hints. (Blogpost)

### MCP C# SDK – Tool-Deklaration, Annotations, Structured Content, Fehler

- Tools: Klasse mit `[McpServerToolType]`, Methoden mit `[McpServerTool]`, Beschreibungen über `[Description]`; Registrierung via `AddMcpServer()` + Transport (`WithStdioServerTransport()` für stdio) + `WithToolsFromAssembly()`/`WithTools<T>()`. Parameter wie `McpServer`, `RequestContext<CallToolRequestParams>` und DI-Services werden injiziert. (https://devblogs.microsoft.com/dotnet/announcing-v20-of-the-official-mcp-csharp-sdk/)
- `McpServerToolAttribute`-Properties (Doku-Stand v1-API, für v2 in DEEP prüfen): `Name` (Default Methodenname), `Title`, `ReadOnly` (Default false), `Idempotent` (false), `Destructive` (Default **true**), `OpenWorld` (Default **true**), `UseStructuredContent` (false) – füllt `OutputSchema` und `StructuredContent`, `OutputSchemaType` (Schema aus einem anderen Typ als dem Rückgabetyp), `IconSource`, `TaskSupport` (experimentell). Für read-only Zeit-Tools müssen also `ReadOnly = true`, `Destructive = false`, `OpenWorld = false`, `Idempotent = true` explizit gesetzt werden. (https://csharp.sdk.modelcontextprotocol.io/v1/api/ModelContextProtocol.Server.McpServerToolAttribute.html)
- Rückgabe-Mapping: `string` → Text-Block; `ContentBlock`/`IEnumerable<…>`/`AIContent` → Content-Liste; `CallToolResult` → unverändert (volle Kontrolle, z. B. `IsError = true`); sonstige Typen → als JSON serialisierter Text-Block. (dto.)
- Fehler: `McpException` → Fehler-Result mit der Exception-Message an den Client; andere Exceptions → Fehler-Result mit generischer Meldung (kein Leak). Alternativ explizit `CallToolResult { IsError = true }`. Wichtig: DataAnnotations formen nur das Schema, das SDK validiert Eingaben **nicht** zur Laufzeit – Validierung ist Sache des Tools. (dto.)
- Verhalten bei unbekanntem Toolnamen (Python: JSON-RPC -32602) ist in den gelesenen Quellen nicht dokumentiert → DEEP (SDK-Quellcode / Tests prüfen).
- `McpServerToolCreateOptions` als programmatische Alternative (u. a. mehrere Icons). (https://csharp.sdk.modelcontextprotocol.io/api/ModelContextProtocol.Server.McpServerToolCreateOptions.html)

### Zeitzonen in .NET 10 – `TimeZoneInfo`, IANA, ICU

- `TimeZoneInfo` ist immutable und wird über `FindSystemTimeZoneById`, `TryFindSystemTimeZoneById`, `Local`, `Utc` oder `CreateCustomTimeZone` bezogen; `HasIanaId` zeigt, ob die Instanz eine IANA-ID trägt. Members für DST-Randfälle: `IsInvalidTime` (Lücke beim Vorstellen), `IsAmbiguousTime` (Überlappung beim Zurückstellen), `GetAmbiguousTimeOffsets` (beide möglichen Offsets), dazu `IsDaylightSavingTime`, `GetUtcOffset`. (https://learn.microsoft.com/dotnet/api/system.timezoneinfo)
- Allgemein bekanntes, in diesem Lauf nicht in Primärquelle belegtes Verhalten (→ DEEP verifizieren): Seit .NET 6 akzeptiert `FindSystemTimeZoneById` auf allen Plattformen sowohl IANA- als auch Windows-IDs und konvertiert intern; auf Linux wird `Local` aus `TZ` bzw. `/etc/localtime` bestimmt, sonst UTC; `ConvertTime`/`ConvertTimeToUtc` werfen bei ungültiger (`Unspecified`) Zeit `ArgumentException`; `GetAmbiguousTimeOffsets` wirft, wenn die Zeit nicht mehrdeutig ist. Relevant für „exakte Key-Validierung“: Wenn Lookups Windows-IDs oder abweichende Schreibweisen/Aliase akzeptieren, muss die Exaktheit selbst geprüft werden (z. B. gegen `tz.Id` / `GetSystemTimeZones()`).
- `TimeZoneInfo.TryConvertIanaIdToWindowsId` / `TryConvertWindowsIdToIanaId` existieren ab .NET 6, funktionieren aber **nur mit ICU**. ICU fehlt, wenn das OS keine ICU hat (ältere Windows-Versionen ohne App-local ICU), wenn Globalization-Invariant-Mode aktiv ist oder wenn auf Windows NLS-Modus erzwungen wird. App-local ICU ist als Opt-in möglich. (https://learn.microsoft.com/hu-hu/dotnet/api/system.timezoneinfo.tryconvertianaidtowindowsid?view=netcore-2.2)
- Konsequenz für die Zeitzonendaten: Unter Linux/macOS kommt die tz-Datenbank vom OS (`/usr/share/zoneinfo`), unter Windows aus der Registry + ICU-Mapping. Die Ergebnisse können daher je Plattform/Patchstand differieren (Python-Original nutzte gebündeltes `tzdata`). In Containern (z. B. Alpine, chiseled) müssen `tzdata` und ggf. ICU vorhanden sein bzw. ist Invariant-Mode zu beachten → DEEP.
- Historische Grundlage zum Umgang mit ambiguous/invalid Zeitpunkten mit `TimeZoneInfo`: BCL-Team-Blog (archiviert). (https://learn.microsoft.com/en-us/archive/blogs/bclteam/system-timezoneinfo-working-with-ambiguous-and-invalid-points-in-time-josh-free)

### NodaTime als Alternative

- NodaTime (aktuell 3.3.x laut NuGet-Treffer; `NodaTime.Testing` 3.3.0) bringt eine eigene, gebündelte IANA-tzdb (`DateTimeZoneProviders.Tzdb`), damit plattformunabhängig identische Ergebnisse – vergleichbar zum Python-Setup mit `tzdata`. Der Provider bietet `GetZoneOrNull` (Lookup ohne Exception) und eine Liste der IDs; zusätzlich gibt es einen BCL-Provider. (https://nodatime.org/api/NodaTime.DateTimeZoneProviders.html, https://nodatime.org/api/NodaTime.TimeZones.html, https://www.nuget.org/packages/NodaTime.Testing)
- Explizite Resolver für lokale Zeiten (`NodaTime.TimeZones.Resolvers`): `LenientResolver` (mehrdeutig → frühere, übersprungen → um Lückenlänge vorwärts verschoben), `StrictResolver` (wirft `AmbiguousTimeException`/`SkippedTimeException`), Einzelresolver `ReturnEarlier`, `ReturnLater`, `ThrowWhenAmbiguous`, `ReturnEndOfIntervalBefore`, `ReturnStartOfIntervalAfter`, `ReturnForwardShifted`, `ThrowWhenSkipped`, kombinierbar via `CreateMappingResolver`. Zusätzlich `MapLocal` liefert eine `ZoneLocalMapping` mit Count 0/1/2 – direkt nutzbar für „non-existent“/„ambiguous“-Flags. (https://nodatime.org/3.3.x/api/NodaTime.TimeZones.Resolvers.html, https://nodatime.org/3.2.x/api/NodaTime.TimeZones.SkippedTimeResolver.html, https://nodatime.org/3.2.x/api/NodaTime.TimeZones.AmbiguousTimeResolver.html)
- `NodaTime.Testing` liefert `FakeClock`; NodaTime nutzt eigenes `IClock` statt `TimeProvider` – Brücke/Adapter wäre nötig, falls `TimeProvider` als Standard-Abstraktion gewünscht ist (→ DEEP prüfen, ob NodaTime 3.3 `TimeProvider`-Interop hat). (https://www.nuget.org/packages/NodaTime.Testing)
- Abwägung (nicht bewertet, nur gesammelt): `TimeZoneInfo` = keine Abhängigkeit, OS-tzdb, ICU-Abhängigkeit, implizites DST-Handling; NodaTime = zusätzliche Abhängigkeit, gebündelte und reproduzierbare tzdb, explizite Typen (`LocalDateTime`, `ZonedDateTime`, `Instant`, `Offset`) und Resolver. Historischer Kontext: MSDN Magazine „The Working Programmer – Noda Time“. (https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/the-working-programmer-noda-time)

### Testen mit Fake-Clock – `TimeProvider` / `FakeTimeProvider`

- `System.TimeProvider` (seit .NET 8 in der BCL) ist die Standard-Abstraktion für Zeit; `GetUtcNow()`, `GetLocalNow()`, `LocalTimeZone`, `TimeProvider.System`. (https://learn.microsoft.com/zh-tw/dotnet/standard/datetime/timeprovider-overview)
- `FakeTimeProvider` im Paket `Microsoft.Extensions.TimeProvider.Testing` (Namespace `Microsoft.Extensions.Time.Testing`; Doku zeigt 10.x): Konstruktor mit Startzeit, `SetUtcNow`, `AdjustTime`, `Advance`, `SetLocalTimeZone(TimeZoneInfo)`, `AutoAdvanceAmount`. Damit lassen sich sowohl die eingefrorene Uhr (freezegun-Ersatz) als auch die lokale Zeitzone im Test fixieren. (https://learn.microsoft.com/en-us/dotNet/api/microsoft.extensions.time.testing.faketimeprovider?view=net-8.0, Quellcode: https://source.dot.net/Microsoft.Extensions.TimeProvider.Testing/FakeTimeProvider.cs.html)
- Praxisartikel (Blog, sekundär) zur Ablösung von `DateTime.Now` durch `TimeProvider`: https://blog.maartenballiauw.be/posts/2026-07-22-timeprovider-and-the-end-of-untestable-datetime-now/

### Projektvorlage, Struktur & Distribution (dotnet tool / dnx / server.json / Registry)

- Offizielle Vorlage `Microsoft.McpServer.ProjectTemplates` (Preview), `dotnet new mcpserver`; erfordert .NET 10 SDK. Optionen: Framework, Transport `local` (stdio) oder `remote` (HTTP), Native-AOT-Publish, Self-contained-Publish. Standard: self-contained Tool-Paket für die gängigen RIDs. Erzeugt `Program.cs` (Server + Transport), Beispiel-Tool-Klasse und `.mcp/server.json`. (https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)
- Ausführung über `dnx <PackageId>@<version> --yes` (im .NET 10 SDK enthalten) – Gegenstück zu `uvx`. `dotnet pack -c Release` erzeugt ein Tool-Paket plus RID-spezifische Pakete gemäß `<RuntimeIdentifiers>`; alle `.nupkg` müssen gepusht werden. Test-Feed: `https://apiint.nugettest.org/v3/index.json`. NuGet.org zeigt einen „MCP Server“-Tab mit kopierbarer Client-Konfig. (dto.; ursprüngliche Ankündigung: https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/)
- `server.json` folgt dem MCP-Registry-Schema (`https://static.modelcontextprotocol.io/schemas/2025-10-17/server.schema.json`); NuGet nutzt den ersten `packages`-Eintrag mit `registryType: nuget`; `packageArguments` (z. B. `--local-timezone`) und `environmentVariables` deklarierbar. (https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)
- Veröffentlichung in der offiziellen MCP Registry: `name` im Namespace `io.github.<user>/<server>`, README muss `<!-- mcp-name: … -->` enthalten (`PackageReadmeFile`), erst NuGet-Push und Validierung abwarten, dann `mcp-publisher login github` + `mcp-publisher publish .mcp/server.json`. (https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/publish-mcp-registry)
- csproj-Properties `PackAsTool`, `PackageType McpServer`, `ToolCommandName` werden in den gelesenen Seiten nicht erläutert → DEEP (Template-Output bzw. PR https://github.com/dotnet/docs/pull/49301/files).
- Client-Konfiguration lokal: `dotnet run --project …` in `.vscode/mcp.json` bzw. `.mcp.json` (Visual Studio). (Quickstart)

### stdio-Hosting & Logging

- Minimal-Setup laut SDK-Doku: `dotnet add package ModelContextProtocol` + `Microsoft.Extensions.Hosting`; `Host.CreateApplicationBuilder(args)`, `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()`, `RunAsync()`. Konsolen-Logging muss per `AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)` vollständig auf **stderr** umgeleitet werden, weil stdout exklusiv dem JSON-RPC-Transport gehört. (https://csharp.sdk.modelcontextprotocol.io/v1/concepts/getting-started.html)
- Die öffentliche SDK-Doku-Site leitet derzeit auf `/v1/` um; eine v2-spezifische Konzept-Doku wurde in diesem Lauf nicht gefunden → API-Details für 2.3 in DEEP gegen Quellcode/Samples im Repo prüfen (https://github.com/modelcontextprotocol/csharp-sdk).
- Sekundärquellen (Blogs) zu Transports/Quickstart, nur zur Orientierung: https://www.devleader.ca/2026/07/17/mcp-transports-in-c-stdio-vs-streamable-http-vs-legacy-sse

### CLI-Parsing (`--local-timezone`)

- `System.CommandLine` ist seit 2.0 stabil; aktuell 2.0.13 (13.10.2026), Targets net8.0/netstandard2.0; parallel existiert 3.0.0-rc.2 (Prerelease). Alternative: Argumente über `Microsoft.Extensions.Configuration` (Command-Line-Provider des Hosts) – Wahl offen. (https://www.nuget.org/packages/system.commandline, Doku: https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- Hinweis: `Host.CreateApplicationBuilder(args)` interpretiert `args` selbst als Konfigurationsquelle – Wechselwirkung mit eigenem Parser in DEEP prüfen.

### Snapshot-/Approval-Tests

- **Verify** (VerifyTests) ist das verbreitetste Snapshot-Tool in .NET; Adapter für xUnit (v3), NUnit, MSTest, TUnit; geeignet, um JSON von `tools/list` (Schemas, Annotations) und Tool-Results gegen akzeptierte `.verified.*`-Dateien zu prüfen. Verify hat 2026 Sponsoring-Änderungen kommuniziert (Lizenz/Finanzierung in DEEP prüfen) (geklärt 2026-10-23: Sponsoring-Erklärung ab v33 nötig → kein Verify, siehe D14). (https://docsearch.algolia.com/mcp/docs/repo/verifytests/verify, https://duendesoftware.com/blog/20260409-verify-open-source-sponsorship)
- Testframework-Landschaft (xUnit v3 / MSTest / TUnit auf Microsoft.Testing.Platform, `dotnet test` in .NET 10) in diesem Lauf nicht recherchiert → DEEP.

### Container & Globalisierung (Docker-Distribution)

- Größenoptimierte .NET-Images (Alpine, Mariner/Azure Linux distroless, Ubuntu chiseled) enthalten **weder ICU noch tzdata** – nur für Invariant-Mode-Apps geeignet. Ubuntu/Debian-Images haben beides. Chiseled-Varianten: Basis (ohne tzdata/ICU), `-aot` (ohne tzdata/ICU/stdc++), `-extra` (mit tzdata, ICU, stdc++ – für self-contained/single-file mit Globalisierung). `runtime-deps` für self-contained/AOT. (https://learn.microsoft.com/dotnet/core/docker/container-images, https://github.com/dotnet/dotnet-docker/discussions/4821)
- Für einen Zeitzonen-Server ist das zentral: Mit `TimeZoneInfo` braucht das Image tzdata (und für IANA↔Windows-Mapping ICU); mit NodaTime-tzdb ist man unabhängig von OS-tzdata. `InvariantGlobalization=true` betrifft Kultur/ICU, nicht zwingend tzdata (→ DEEP). Seite zeigt .NET-8-Tags; .NET-10-Tags (Ubuntu 24.04 „noble“?) in DEEP verifizieren.

### MCPB (MCP Bundles)

- `.mcpb` = Zip mit lokalem Server + `manifest.json` (früher DXT); CLI `@anthropic-ai/mcpb` zum Init/Packen. Server-Typen `node`, `python`, `uv`, `binary` – ein .NET-Server ginge als `binary` (self-contained/AOT-Executable, pro OS/Arch eigener Build; `.exe` wird unter Windows angehängt). `user_config` (string/number/boolean/directory/file) wird via `${user_config.KEY}` in `mcp_config.args` injiziert – z. B. für `--local-timezone`. Unterstützt u. a. Claude Desktop (macOS/Windows, Ein-Klick-Install). Aktuelle `manifest_version` → DEEP (MANIFEST.md). (https://docsearch.algolia.com/mcp/docs/repo/modelcontextprotocol/mcpb)

### DEEP: MCP C# SDK 2.3 – Verhalten laut Quellcode

Quelle: Quellcode `modelcontextprotocol/csharp-sdk`, `main` @ 8b00f20 (23.10.2026), `src/Directory.Build.props` → `VersionPrefix 2.3.0` (https://github.com/modelcontextprotocol/csharp-sdk/tree/main/src). Die GitHub-Release-Notes zu 2.3.0 waren in diesem Lauf nicht abrufbar (API-Zugriff gesperrt), Aussagen stammen daher direkt aus dem Code.

- **Unbekanntes Tool → JSON-RPC-Fehler -32602, nicht `isError`.** Wird kein registriertes Tool zum Namen gefunden, wirft der Default-Handler `McpProtocolException("Unknown tool: '<name>'", McpErrorCode.InvalidParams)`. `McpProtocolException` wird im Tool-Call-Pfad explizit durchgereicht (nicht in ein Result verpackt) → Protokollfehler wie im Python-Original. `McpErrorCode.cs` dokumentiert -32602 ausdrücklich für „Unknown tool name or invalid protocol-level tool arguments“. (https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerImpl.cs, https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/McpErrorCode.cs)
- **Exception-Mapping im Tool-Aufruf** (`CreateToolCallErrorResult`): durchgereicht werden nur `OperationCanceledException` (bei Abbruch), `McpProtocolException` und `InputRequiredException` (MRTR). Alle anderen Exceptions werden zu `CallToolResult { IsError = true }`; Text bei `McpException`: **`An error occurred invoking '<tool>': <Message>`**, bei sonstigen Exceptions nur `An error occurred invoking '<tool>'.` – d. h. die SDK stellt einen Präfix voran. Für exakte Fehlertext-Parität mit Python muss das Tool selbst ein `CallToolResult` mit `IsError = true` zurückgeben statt zu werfen.
- **Beide Protokoll-Ären auf demselben stdio-Server, ohne Konfiguration.** Unterstützte Versionen (`McpProtocolVersions`): 2026-07-28, 2025-11-25, 2025-06-18, 2025-03-26, 2024-11-05. Die Ära wird pro Verbindung aus dem ersten Request abgeleitet: Requests mit Per-Request-`_meta` (Protokollversion) etablieren die moderne Ära; `server/discover` ohne solche Metadaten → -32602; `initialize` auf einer bereits modernen Sitzung → **-32601 MethodNotFound** („Use server/discover …“); ohne etablierte Ära wird `initialize` klassisch verhandelt. `ping` existiert in 2026-07-28 nicht mehr (SEP-2575). (https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerImpl.cs, https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/McpSessionHandler.cs)
- **Attribut-API in v2 unverändert gültig.** `McpServerToolAttribute` hat weiterhin `Name`, `Title`, `Destructive`, `Idempotent`, `OpenWorld`, `ReadOnly`, `UseStructuredContent`, `OutputSchemaType`, `IconSource`; Defaults im Code: `Destructive = true`, `Idempotent = false`, `OpenWorld = true`, `ReadOnly = false`. Intern nullable Felder → nur explizit gesetzte Werte werden als Annotation-Hint übernommen. Die v1-Doku zu den Attributen trifft damit auf 2.3 zu. (https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerToolAttribute.cs)
- **Output-Schema / Structured Content:** `UseStructuredContent = true` (Attribut oder `McpServerToolCreateOptions`) erzeugt `OutputSchema` über `AIJsonUtilities.CreateJsonSchema` (Microsoft.Extensions.AI, basiert auf `System.Text.Json.Schema.JsonSchemaExporter`) aus dem Rückgabetyp bzw. `OutputSchemaType`; die `[Description]` des Rückgabewerts fließt ins Output-Schema ein. Tool-Namen werden per Regex validiert (ungültiger Name → `ArgumentException` beim Registrieren). (https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/AIFunctionMcpServerTool.cs, https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerToolCreateOptions.cs)
- **AOT/Trimming:** Alle SDK-Pakete (`ModelContextProtocol`, `.Core`, `.AspNetCore`, `.Extensions.*`) setzen `<IsAotCompatible>true</IsAotCompatible>`. Interne Serialisierung nutzt source-generierte `McpJsonUtilities.JsonContext`. Für eigene Tool-Parameter-/Rückgabetypen unter Native AOT ist ein eigener `JsonSerializerContext` nötig (Attribut-Discovery per Reflection `WithToolsFromAssembly` ist trimming-kritisch → Samples prüfen). Samples im Repo: `QuickstartWeatherServer`, `TestServerWithHosting`, `InMemoryTransport`, `FileBasedMcpServer`, `EverythingServer`. (https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples)

### DEEP: `TimeZoneInfo` in .NET 10 – Lookup, Aliase, lokale Zone (laut Doku + Runtime-Quellcode)

- **Lookup ist case-insensitive.** Laut API-Doku muss `id` in der Länge exakt, aber nicht in Groß-/Kleinschreibung übereinstimmen. Im Runtime-Code sind alle Caches (`_systemTimeZones`, `_timeZonesUsingAlternativeIds`) `StringComparer.OrdinalIgnoreCase`; auch `Equals`/`GetHashCode` von `TimeZoneInfo` vergleichen IDs ignore-case. Ab .NET 8 liefert `FindSystemTimeZoneById` gecachte Instanzen. (https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.findsystemtimezonebyid?view=net-10.0, https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs)
- **Unix-Dateilookup ist dagegen case-sensitiv:** Bei Cache-Miss wird `Path.Combine(<tzdir>, id)` gelesen (erlaubte Zeichen nur `[A-Za-z0-9/_-]`, keine gerooteten Pfade). Folge: `"europe/warsaw"` scheitert auf Linux beim ersten Aufruf, kann aber treffen, sobald `Europe/Warsaw` bereits im case-insensitiven Cache liegt (z. B. nach `GetSystemTimeZones()`) – **zustandsabhängiges Verhalten**. Für das Python-Verhalten (nur exakter Key) muss daher zusätzlich `string.Equals(tz.Id, input, StringComparison.Ordinal)` bzw. Abgleich gegen eine eigene ID-Menge geprüft werden. (https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.Unix.NonAndroid.cs)
- **Windows-IDs auf Linux/macOS und IANA-IDs auf Windows** werden über die ICU-Mapping-Tabelle als „alternative IDs“ akzeptiert; die zurückgegebene Instanz trägt dann die **angefragte** ID (z. B. `Id == "W. Europe Standard Time"`, `HasIanaId == false`). Exakte IANA-Validierung muss Windows-IDs also selbst ablehnen (`HasIanaId` prüfen). Auf Windows funktionieren IANA-IDs nur ohne NLS-Modus und ohne Invariant-Mode. (dto.; Learn-Seite FindSystemTimeZoneById)
- **Backward-Links** wie `US/Eastern` sind auf Linux eigene Dateien in `/usr/share/zoneinfo` (sofern das tzdata-Paket sie installiert) und werden daher gefunden; Python `zoneinfo` mit `tzdata` akzeptiert sie ebenfalls → Parität hängt vom OS-Paket ab.
- **Enumeration:** `GetSystemTimeZones()` liest auf Unix die IDs aus `zone.tab` (unter `TZDIR` oder `/usr/share/zoneinfo`) – das sind nur kanonische Zonen mit Länderzuordnung, **nicht** alle gültigen Keys (fehlend z. B. `UTC`-Aliase außer dem eingebauten `UTC`, `Etc/GMT+5`, Backward-Links). Als „Menge gültiger Keys“ ist es daher ungeeignet; Python `zoneinfo.available_timezones()` liefert mehr. (TimeZoneInfo.Unix.cs, dto.)
- **Lokale Zone (tzlocal-Ersatz):** Unix: `TZ`-Umgebungsvariable, sonst `/etc/localtime` (Symlink-Ziel liefert die IANA-ID), sonst UTC. Windows: Registry-Zone mit Windows-ID → per `TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out iana)` in IANA umrechnen (benötigt ICU; mit optionalem Region-Parameter). `TimeZoneInfo.Local.HasIanaId` zeigt, ob Konvertierung nötig ist. (TimeZoneInfo.Unix.NonAndroid.cs; https://learn.microsoft.com/dotnet/api/system.timezoneinfo)
- **Zwei Invariant-Schalter:** (1) `InvariantGlobalization` (`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`) – deaktiviert ICU → kein IANA↔Windows-Mapping, IDs ohne alternative Auflösung; (2) **`System.TimeZoneInfo.Invariant`** Feature-Switch (`DOTNET_SYSTEM_TIMEZONE_INVARIANT`) – dann existiert **nur UTC**, `Local` = UTC, jeder andere Lookup → `TimeZoneNotFoundException`. Beides darf für diesen Server nicht aktiv sein (bzw. nur (1), wenn Windows-ID-Konvertierung verzichtbar ist). (https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs)
- DST-Randfälle: `IsInvalidTime`, `IsAmbiguousTime`, `GetAmbiguousTimeOffsets` (siehe BROAD). `IsDaylightSavingTime` liefert nur `bool`; die DST-Differenz (Lord Howe: 30 min) muss als `GetUtcOffset(t) - BaseUtcOffset` berechnet werden – Achtung: `BaseUtcOffset` ist der *aktuelle* Standard-Offset, bei historischen Änderungen des Standard-Offsets nicht zwingend korrekt. In diesem Lauf nicht empirisch getestet (kein .NET-SDK in der Recherche-Umgebung).

### DEEP: NodaTime 3.3.6 – Version, tzdb, Abwägung für diese App

- **Aktuell 3.3.6 (22.10.2026)**, gebündelte tzdb **2026e**; 3.3.6 aktualisiert zusätzlich das Windows-Mapping auf CLDR v49. Die 3.3.x-Linie liefert tzdb-Updates zeitnah als Patch-Release (3.3.1 → 2026a, 3.3.2 → 2026b, 3.3.3 → 2026c, 3.3.4 → 2026d, 3.3.5 → 2026e). 3.3.0 (03.01.2026) hat das net6.0-Target entfernt. (https://nodatime.org/versions, https://nodatime.org/)
- **`TimeProvider`-Interop** existiert seit 3.2.0 (Extension-Methoden, nur im .NET-8+-Target) – damit lässt sich `FakeTimeProvider` auch mit NodaTime als einheitliche Testuhr nutzen (Details der API in diesem Lauf nicht nachgelesen). (https://nodatime.org/versions)
- **tzdb-Herkunft:** OS-unabhängig, im Assembly eingebettet (vergleichbar zu Python `tzdata`); Aktualität hängt am Paket-Update statt am OS-Patchstand. Die IANA-Releases 2026c–2026e zeigen, dass 2026 mehrere tz-Änderungen erschienen sind (→ OS-tzdata in Containern/Windows kann hinterherhinken). (https://www.iana.org/time-zones/releases/2026e)
- **Für diese App relevante Eigenschaften** (Quellen siehe BROAD): `MapLocal` → `ZoneLocalMapping.Count` 0 (Lücke)/1/2 (Überlappung) liefert die Flags „nonexistent“/„ambiguous“ direkt; `ZonedDateTime.IsDaylightSavingTime()` und `GetZoneInterval(instant).Savings` liefern die DST-Differenz als `Offset` (für Lord Howe +00:30) – das entspricht Pythons `dst()` genauer als `TimeZoneInfo` (nur `bool`). `DateTimeZoneProviders.Tzdb.GetZoneOrNull(id)` für Lookup ohne Exception; `Tzdb.Ids` als Menge gültiger Keys (enthält nach meinem Kenntnisstand auch Aliase wie `US/Eastern`; Groß-/Kleinschreibungs-Verhalten in diesem Lauf **nicht verifiziert**).
- **Gegenüberstellung (gesammelt, Entscheidung in DISTILL):** NodaTime = +1 Abhängigkeit, reproduzierbare tzdb identisch auf allen OS und in jedem Container (auch chiseled/Alpine ohne tzdata), exakte ID-Menge, DST-Savings als Wert, eigener Resolver-Mechanismus; `TimeZoneInfo` = keine Abhängigkeit, aber OS-tzdb, case-insensitive/zustandsabhängiger Lookup, Windows-ID-Aliase, ICU-Pflicht für Mapping, `zone.tab`-Enumeration unvollständig.

### DEEP: Packaging – Tool-Paket, Container, MCPB

- **csproj der offiziellen Vorlage** (dotnet/docs PR 49301, Sample `SampleMcpServer.csproj`): `PackAsTool=true`, `PackageType=McpServer` (aktiviert den „MCP Server“-Tab auf NuGet.org), `RuntimeIdentifiers=win-x64;win-arm64;osx-arm64;linux-x64;linux-arm64;linux-musl-x64`, `SelfContained=true`, `PublishSelfContained=true`, `PublishSingleFile=true`. `ToolCommandName`, `PublishAot` und das Packen von `.mcp/server.json` sind im Diff nicht ausgeschrieben (Datei wird per Code-Embed eingebunden) → beim Scaffolden mit `dotnet new mcpserver` am erzeugten Projekt ablesen. Ergebnis: ein Zeiger-Paket plus je RID ein Paket; `dnx` lädt das passende. (https://github.com/dotnet/docs/pull/49301/files, https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)
- **Self-contained pro RID** bedeutet: Der Server bringt seine Runtime mit, nutzt aber weiterhin **OS-tzdata und OS-ICU** (bei `TimeZoneInfo`). `linux-musl-x64` (Alpine) hat oft weder tzdata noch ICU → bei `TimeZoneInfo` Fehlerquelle, bei NodaTime nur ICU-relevant (Kultur), nicht für Zeitzonen.
- **Container .NET 10:** Default-Tags (`10.0`) sind ab .NET 10 **Ubuntu 24.04 „noble“**; Debian-Images gibt es für .NET 10 nicht mehr. Ubuntu-Vollimages enthalten ICU und tzdata; chiseled-Basis und `-aot` enthalten **weder tzdata noch ICU**, `-chiseled-extra` enthält beides (Schema aus .NET-8-Doku, für noble analog angenommen). Für stdio im Container `docker run -i --rm`. (https://learn.microsoft.com/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu, https://learn.microsoft.com/dotnet/core/docker/container-images)
- **MCPB:** `MANIFEST.md` nennt als aktuelle Spec-Version **`manifest_version: "0.3"`**; Server-Typ `uv` ist neu ab **0.4** (Python-spezifisch). Für .NET ist `server.type: "binary"` passend, mit `compatibility.platforms` (`darwin`, `win32`, `linux`) und `platform_overrides` für plattformspezifische `command`-Pfade – also ein Bundle mit mehreren self-contained/AOT-Binaries oder je Plattform ein Bundle. (https://github.com/modelcontextprotocol/mcpb/blob/main/MANIFEST.md)

### DEEP: Tests – Plattform, Frameworks, Fake-Clock

- **VSTest vs. Microsoft.Testing.Platform (MTP):** MTP ist executable-first (Testprojekt läuft wie eine App, `dotnet run`/`dotnet watch`), unterstützt Native AOT/Trimming. MTP-Support: MSTest, NUnit, **xUnit v3** (v2 nur VSTest), **TUnit (nur MTP)**. Empfehlung: pro Repo nur eine Plattform. (https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview)
- **`dotnet test` in .NET 10:** Default bleibt VSTest; nativer MTP-Modus per `global.json` (Schlüssel `test.runner = "Microsoft.Testing.Platform"` – Schlüsselname nicht direkt in der Primärquelle gesehen, gegen global.json-Referenz prüfen); erfordert .NET 10 SDK und MTP ≥ 1.7; dann müssen **alle** Testprojekte MTP nutzen. Exit-Code 8 bei null Tests. (https://learn.microsoft.com/ar-sa/dotnet/core/tools/dotnet-test-mtp, https://devblogs.microsoft.com/dotnet/dotnet-test-with-mtp/)
- **End-to-End-Tests gegen den MCP-Server:** Das SDK-Repo enthält ein `InMemoryTransport`-Sample und `TestServerWithHosting` – Muster, um mit `McpClient` in-process `tools/list`/`tools/call` zu prüfen, inkl. beider Protokoll-Ären (`McpClientOptions.ProtocolVersion` pinnen). (https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples)
- **Fake-Clock:** `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) inkl. `SetLocalTimeZone` (BROAD); NodaTime hat `TimeProvider`-Interop seit 3.2 – eine einzige Testuhr reicht für beide Varianten.

### DEEP: C# 14 / .NET 10 – für diese App nützlich

- C# 14 ist die Sprachversion von .NET 10. Relevante Features: **Extension members** (`extension(...)`-Blöcke, z. B. Extension-Properties auf `TimeZoneInfo`/`DateTimeOffset`), **`field`-Keyword** (Validierung im Property-Setter ohne Backing-Field), **Null-conditional assignment** (`x?.Prop = …`), `nameof` mit ungebundenen Generics, **File-based apps** (`dotnet run app.cs` mit `#:package`-Direktiven; das SDK-Repo hat dazu ein `FileBasedMcpServer`-Sample – für einen Mini-Server eine mögliche, aber für Packaging/Tests ungewöhnliche Form). (https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14, https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples/FileBasedMcpServer)
- Das SDK-Repo selbst baut mit `LangVersion preview`; als Konsument reicht C# 14.

## Fallstricke & Breaking Changes

**MCP SDK**
- Annotation-Defaults `Destructive = true`, `OpenWorld = true`, `ReadOnly = false`, `Idempotent = false` – für read-only Tools alle vier explizit setzen (Quellcode 2.3 bestätigt).
- Unbekanntes Tool → `McpProtocolException` mit -32602 (Parität zu Python gegeben); `McpProtocolException` aus Tool-Code wird ebenfalls als JSON-RPC-Fehler durchgereicht – für fachliche Fehler also **nicht** `McpProtocolException` werfen.
- `McpException` aus dem Tool → `isError`, aber mit SDK-Präfix `An error occurred invoking '<tool>': …`; andere Exceptions → nur generischer Text. Exakte Fehlertexte nur über selbst gebautes `CallToolResult { IsError = true }`.
- Eingaben werden nicht gegen das Schema validiert; Validierung im Tool.
- v2: `structuredContent` bei primitiven Rückgabetypen ungewrappt; `Tool.inputSchema` Pflicht.
- Ären: `initialize` nach etablierter 2026-07-28-Sitzung → -32601; `server/discover` ohne `_meta`-Version → -32602; `ping` in 2026-07-28 entfernt.
- Reflection-basierte Discovery (`WithToolsFromAssembly`) und eigene Typen ohne `JsonSerializerContext` sind bei Native AOT/Trimming problematisch, obwohl das SDK selbst `IsAotCompatible` ist.
- stdout ist exklusiv für JSON-RPC – Logging auf stderr.

**Zeitzonen**
- `FindSystemTimeZoneById` ist case-insensitive (Doku), auf Linux aber beim ersten Dateizugriff case-sensitiv → Ergebnis hängt vom Cache-Zustand ab. Exaktheit selbst prüfen (Ordinal-Vergleich mit `tz.Id`).
- Windows-IDs werden überall (mit ICU) als Alias akzeptiert, `Id` ist dann die Windows-ID, `HasIanaId == false`.
- `GetSystemTimeZones()` auf Linux = nur `zone.tab`-Einträge → keine vollständige Liste gültiger Keys.
- `DOTNET_SYSTEM_TIMEZONE_INVARIANT` / Feature-Switch `System.TimeZoneInfo.Invariant` → nur UTC existiert; `InvariantGlobalization` → kein IANA↔Windows-Mapping (lokale Zone unter Windows nicht als IANA-ID ermittelbar).
- `IsDaylightSavingTime` nur `bool`; DST-Differenz (Lord Howe 30 min) muss errechnet werden; `BaseUtcOffset` ist der aktuelle, nicht der historische Standard-Offset.
- OS-tzdb-Stand variiert (2026 bereits IANA 2026a–e); NodaTime 3.3.6 bündelt 2026e.

**Packaging/Container**
- .NET-10-Default-Images = Ubuntu 24.04, keine Debian-Images mehr; chiseled/`-aot`/Alpine ohne tzdata und ICU.
- RID `linux-musl-x64` (Alpine) im Template-Default → tzdata/ICU auf Zielsystem nicht garantiert.
- Alle RID-spezifischen `.nupkg` müssen gepusht werden, sonst scheitert `dnx` auf der fehlenden Plattform.
- MCPB `manifest_version` 0.3 (aktuell laut MANIFEST.md) vs. 0.4 (nur für `uv`-Typ nötig).

**Tests**
- MTP-Modus in `global.json` gilt für die ganze Lösung – VSTest-Projekte daneben sind ein Fehler; xUnit nur in v3 auf MTP.

## Entscheidungen für diesen Rebuild

| # | Entscheidung | Befund | Quelle |
|---|---|---|---|
| D1 | `net10.0`, C# 14, kein Multi-Targeting | .NET 10 LTS bis 14.11.2028; .NET 8/9 enden 10.11.2026 | Versionen & Support |
| D2 | MCP über `ModelContextProtocol` 2.3.x mit stdio-Transport im Generic Host | offizielles SDK, bedient beide Protokollgenerationen | SDK-Abschnitt, NuGet |
| D3 | Zeitzonen mit **NodaTime 3.3.x** (gebündelte tzdb), nicht `TimeZoneInfo` | `TimeZoneInfo` ist case-insensitiv (unter Linux abhängig vom Cache), akzeptiert Windows-IDs, kennt keine vollständige Key-Liste und liefert Sommerzeit nur als bool; NodaTime liefert auf allen Plattformen identische Daten und `MapLocal` meldet Lücke/Überlappung direkt | Zeitzonen-Abschnitte, Fallstricke |
| D4 | Exakte Prüfung: Name muss als Ordinal in `DateTimeZoneProviders.Tzdb.Ids` stehen, sonst „Invalid timezone“ | macht P3 auf allen Plattformen gleich; Pfade sind damit automatisch ungültig | NodaTime, Fallstricke |
| D5 | Lokale Zone: `TimeZoneInfo.Local` → IANA-ID (unter Windows per `TryConvertWindowsIdToIanaId`) → in NodaTime prüfen, sonst `UTC`; `--local-timezone` hat Vorrang | Ersatz für `tzlocal` | TimeZoneInfo-Abschnitt |
| D6 | Nicht existierende Ortszeit → eigener Fehler; doppeldeutige → frühere Abbildung (wie Original) über `MapLocal` | `ZoneLocalMapping.Count` 0/1/2 | NodaTime |
| D7 | Fachfehler als selbst gebautes `CallToolResult { IsError = true }` mit eigenem Text – kein `McpException` | `McpException` bekommt ein SDK-Präfix, `McpProtocolException` würde ein JSON-RPC-Fehler | Fallstricke MCP SDK |
| D8 | Unbekanntes Tool: SDK-Verhalten (-32602) nutzen, keine eigene Middleware | SDK 2.3 macht das bereits | Fallstricke MCP SDK |
| D9 | Alle vier Annotations explizit setzen (readOnly, destructive=false, idempotent, openWorld=false) | SDK-Defaults sind destructive/openWorld = true | Fallstricke MCP SDK |
| D10 | Eingaben im Tool selbst prüfen (leere/fehlende Felder, Formate) | SDK validiert nicht gegen das Schema | Fallstricke MCP SDK |
| D11 | Ergebnisobjekte als `structuredContent` mit Ausgabeschema plus JSON-Textblock; Serialisierung mit System.Text.Json und `JsonSerializerContext` | Ergebnisform des Originals; hält Trimming/AOT offen | SDK-Abschnitt |
| D12 | Tools explizit registrieren statt `WithToolsFromAssembly` | Reflection-Discovery ist bei Trimming/AOT problematisch | Fallstricke MCP SDK |
| D13 | „Jetzt“ über `TimeProvider`; Tests mit `FakeTimeProvider`, Brücke zu NodaTime über deren `TimeProvider`-Interop | eine einzige Testuhr für BCL und NodaTime | Tests, NodaTime |
| D14 | Tests mit xUnit v3 auf Microsoft.Testing.Platform (`global.json`), Snapshots von `tools/list` als eigener JSON-Vergleich im Testprojekt | MTP ist Standard in .NET 10. Befund: Verify ≥ 33 (nach 2026-09-01) verlangt eine Sponsoring-/Lizenz-Erklärung im Build (SponsorCheck SC021, Ausnahmen befristet auf 12 Monate); Entscheidung Nutzer 2026-10-23: kein Verify | Tests |
| D15 | CLI mit `System.CommandLine` 2.0.x (stabil) | `--help`, unbekannte Optionen und Prüfung von `--local-timezone` ohne Eigenbau | CLI-Parsing |
| D16 | Logging nur nach stderr; stdout gehört dem Transport | stdio-Regel | stdio-Hosting |
| D17 | Auslieferung als `dotnet tool` (`PackAsTool`, `PackageType McpServer`, `.mcp/server.json`), Start per `dnx`; RID-spezifische Pakete nur, wenn alle gepusht werden | offizieller .NET-Weg, NuGet-MCP-Tab | Packaging |
| D18 | Kein Container im ersten Release; falls später: Ubuntu-basiertes Image, nicht chiseled/Alpine | chiseled/Alpine ohne tzdata/ICU – mit NodaTime unkritisch, aber lokale Zone fehlt | Packaging/Container |
| D19 | Native AOT optional später; Code von Anfang an trim-fähig (D11, D12) | kleiner Server profitiert vom Startverhalten, SDK ist AOT-fähig | SDK-Abschnitt |
| D20 | Optionales `date` (YYYY-MM-DD) mit NodaTime `LocalDatePattern.Iso` strikt parsen | Verbesserung aus PRD; ISO-Format eindeutig | PRD, NodaTime |


### Vom Nutzer bestätigt (23.10.2026)
- Zeitzonen mit NodaTime (D3).
- Auslieferung als `dotnet tool` über `dnx` (D17); Container und AOT später optional (D18, D19).

## Noch nicht abgedeckt

(Stand nach DEEP)
- Release Notes 2.3.0 im Wortlaut (GitHub-API in dieser Sitzung gesperrt; Verhalten stattdessen aus Quellcode `main` mit `VersionPrefix 2.3.0` abgeleitet – Tag-Stand nicht verglichen).
- Exaktes JSON-Schema, das `AIJsonUtilities.CreateJsonSchema` für einen Record mit nullable Properties/`[Description]` erzeugt (Vergleich zum pydantic-`outputSchema` des Originals) – empirisch mit SDK prüfen.
- Verhalten bei fehlenden/falsch typisierten Argumenten (Binding-Fehler: `isError` vs. -32602) im SDK 2.3.
- NodaTime: Case-Sensitivity von `Tzdb.GetZoneOrNull` und ob `Tzdb.Ids` Aliase enthält; API der `TimeProvider`-Extensions.
- Empirischer Test der `TimeZoneInfo`-Randfälle (Lord Howe, Kathmandu, `europe/warsaw`, `US/Eastern`) auf Linux/Windows – keine .NET-Installation in der Recherche-Umgebung.
- `global.json`-Schlüssel für MTP gegen global.json-Referenz verifizieren (geklärt, siehe Scaffold-Befunde / D14); xUnit-v3- vs. MSTest- vs. TUnit-Versionen; Verify-Lizenz/Sponsoring-Status (geklärt, siehe Scaffold-Befunde / D14).
- Template-Output `dotnet new mcpserver` (ToolCommandName, server.json-Packing, AOT-Option); .NET-10-chiseled-Tag-Namen (`10.0-noble-chiseled-extra`) auf MCR verifizieren.
- CLI-Parsing (`System.CommandLine` vs. Host-Konfiguration) und Wechselwirkung mit `Host.CreateApplicationBuilder(args)`.
- Kulturunabhängige Formatierung (Wochentag englisch, ISO 8601) – BCL-Standardwissen, nicht vertieft.

## Scaffold-Befunde (Slice 00)

- xunit.v3 4.0.2 braucht `UseMicrosoftTestingPlatformRunner` + `OutputType Exe`, kein `Microsoft.NET.Test.Sdk`.
- `global.json`-Schlüssel `test.runner = "Microsoft.Testing.Platform"` ist in der Microsoft-Doku (`global-json.md`) bestätigt.
- xunit.v3 hat kein implizites `using Xunit` → `<Using Include="Xunit" />` in `tests/Directory.Build.props`.
- Verify.XunitV3 33.3.4 hängt an `xunit.v3.extensibility.core` 4.0.2, bricht den Build aber mit SponsorCheck SC021 ohne Lizenz-Erklärung ab (32.0.1 bräuchte xunit.v3 3.2.2) → Entscheidung: eigener JSON-Snapshot.
- NetArchTest.eNhancedEdition 1.4.5 (Mono.Cecil 0.11.6) bietet `FailingTypes`, nicht `FailingTypeNames`.
- Eine `ProjectReference` auf eine Exe kopiert `McpTime.Host.dll` und `McpTime.Host.runtimeconfig.json` ins Testausgabeverzeichnis (geprüft).
- Der Trim-Analyzer (`EnableTrimAnalyzer`) meldet nichts für `Host.CreateApplicationBuilder` + `AddConsole` + `AddMcpServer().WithStdioServerTransport()`.
- Ein MCP-Server ohne Tools meldet die Fähigkeit `tools` nicht → `tools/list` ist „nicht verfügbar“; `WithListToolsHandler` mit leerem `ListToolsResult` ist der minimale Platzhalter.
- SDK 2.3.0 verhandelt auf demselben stdio-Server sowohl „2025-11-25“ als auch „2026-07-28“; `McpClient.NegotiatedProtocolVersion` zeigt, welche gilt.

## Quellen

- .NET Support Policy: https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- .NET 8/9 Support-Ende (Forum-Meldung): https://www.elevenforum.com/t/net-8-and-net-9-will-reach-end-of-support-on-november-10-2026.47839/
- NuGet ModelContextProtocol: https://www.nuget.org/packages/ModelContextProtocol/absoluteLatest
- NuGet ModelContextProtocol.Core 2.1.0: https://www.nuget.org/packages/ModelContextProtocol.Core/2.1.0
- Ankündigung MCP C# SDK v2.0: https://devblogs.microsoft.com/dotnet/announcing-v20-of-the-official-mcp-csharp-sdk/
- csharp-sdk Releases: https://github.com/modelcontextprotocol/csharp-sdk/releases
- csharp-sdk Repo: https://github.com/modelcontextprotocol/csharp-sdk
- McpServerToolAttribute (v1-API-Doku): https://csharp.sdk.modelcontextprotocol.io/v1/api/ModelContextProtocol.Server.McpServerToolAttribute.html
- McpServerToolCreateOptions: https://csharp.sdk.modelcontextprotocol.io/api/ModelContextProtocol.Server.McpServerToolCreateOptions.html
- SDK Getting Started: https://csharp.sdk.modelcontextprotocol.io/v1/concepts/getting-started.html
- TimeZoneInfo API: https://learn.microsoft.com/dotnet/api/system.timezoneinfo
- TryConvertIanaIdToWindowsId: https://learn.microsoft.com/hu-hu/dotnet/api/system.timezoneinfo.tryconvertianaidtowindowsid?view=netcore-2.2
- BCL-Blog ambiguous/invalid: https://learn.microsoft.com/en-us/archive/blogs/bclteam/system-timezoneinfo-working-with-ambiguous-and-invalid-points-in-time-josh-free
- NodaTime Resolvers 3.3: https://nodatime.org/3.3.x/api/NodaTime.TimeZones.Resolvers.html
- NodaTime SkippedTimeResolver / AmbiguousTimeResolver: https://nodatime.org/3.2.x/api/NodaTime.TimeZones.SkippedTimeResolver.html, https://nodatime.org/3.2.x/api/NodaTime.TimeZones.AmbiguousTimeResolver.html
- NodaTime DateTimeZoneProviders / TimeZones: https://nodatime.org/api/NodaTime.DateTimeZoneProviders.html, https://nodatime.org/api/NodaTime.TimeZones.html
- NuGet NodaTime.Testing: https://www.nuget.org/packages/NodaTime.Testing
- MSDN Magazine Noda Time: https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/the-working-programmer-noda-time
- TimeProvider Overview: https://learn.microsoft.com/zh-tw/dotnet/standard/datetime/timeprovider-overview
- FakeTimeProvider: https://learn.microsoft.com/en-us/dotNet/api/microsoft.extensions.time.testing.faketimeprovider?view=net-8.0
- FakeTimeProvider Quellcode: https://source.dot.net/Microsoft.Extensions.TimeProvider.Testing/FakeTimeProvider.cs.html
- Blog TimeProvider: https://blog.maartenballiauw.be/posts/2026-07-22-timeprovider-and-the-end-of-untestable-datetime-now/
- Quickstart MCP-Server .NET: https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server
- Publish to MCP Registry: https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/publish-mcp-registry
- NuGet MCP Quickstart (Blog 2025): https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/
- dotnet/docs PR 49301: https://github.com/dotnet/docs/pull/49301/files
- System.CommandLine NuGet: https://www.nuget.org/packages/system.commandline
- System.CommandLine Doku: https://learn.microsoft.com/en-us/dotnet/standard/commandline/
- Verify: https://docsearch.algolia.com/mcp/docs/repo/verifytests/verify
- Verify Sponsoring: https://duendesoftware.com/blog/20260409-verify-open-source-sponsorship
- .NET Container Images: https://learn.microsoft.com/dotnet/core/docker/container-images
- dotnet-docker Discussion 4821: https://github.com/dotnet/dotnet-docker/discussions/4821
- MCPB: https://docsearch.algolia.com/mcp/docs/repo/modelcontextprotocol/mcpb
- Dev Leader Transports (sekundär): https://www.devleader.ca/2026/07/17/mcp-transports-in-c-stdio-vs-streamable-http-vs-legacy-sse
- csharp-sdk Quellcode (main, 2.3.0): https://github.com/modelcontextprotocol/csharp-sdk/tree/main/src
- McpServerImpl.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerImpl.cs
- McpErrorCode.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/McpErrorCode.cs
- McpSessionHandler.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/McpSessionHandler.cs
- McpServerToolAttribute.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/McpServerToolAttribute.cs
- AIFunctionMcpServerTool.cs: https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Server/AIFunctionMcpServerTool.cs
- csharp-sdk Samples: https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples
- FindSystemTimeZoneById (.NET 10): https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.findsystemtimezonebyid?view=net-10.0
- dotnet/runtime TimeZoneInfo.cs: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.cs
- dotnet/runtime TimeZoneInfo.Unix.NonAndroid.cs: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/TimeZoneInfo.Unix.NonAndroid.cs
- NodaTime Startseite: https://nodatime.org/
- NodaTime Versionen: https://nodatime.org/versions
- IANA tzdb 2026e: https://www.iana.org/time-zones/releases/2026e
- C# 14 What's new: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14
- .NET 10 Default-Images Ubuntu: https://learn.microsoft.com/dotnet/core/compatibility/containers/10.0/default-images-use-ubuntu
- MCPB MANIFEST.md: https://github.com/modelcontextprotocol/mcpb/blob/main/MANIFEST.md
- Test-Plattformen Übersicht: https://learn.microsoft.com/en-us/dotnet/core/testing/test-platforms-overview
- dotnet test MTP: https://learn.microsoft.com/ar-sa/dotnet/core/tools/dotnet-test-mtp
- dotnet test mit MTP (Blog): https://devblogs.microsoft.com/dotnet/dotnet-test-with-mtp/
