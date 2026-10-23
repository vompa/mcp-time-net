# CLAUDE.md – mcp-time (Rebuild auf .NET 10)

Kleiner lokaler MCP-Server (stdio) mit zwei rein lesenden Tools: aktuelle Zeit in einer IANA-Zeitzone und Umrechnung einer Ortszeit zwischen Zeitzonen. Neubau des MCP-Referenzservers „time“ auf .NET 10 – keine Kopie, sondern besser gebaut.
Verhalten: `rebuild/PRD.md` · Begriffe: `rebuild/GLOSSARY.md` · Technische Entscheidungen: `rebuild/research.md` (D1–D20) · Ziel: alle Fälle in `rebuild/parity/` grün.

## Architecture

```
src/
  McpTime.Host     → McpTime.Server, McpTime.Core   (Programmstart, System.CommandLine, Generic Host, stdio)
  McpTime.Server   → McpTime.Core                    (MCP-Adapter: Tool-Registrierung, Schemas, Annotations, CallToolResult)
  McpTime.Core     → (nur NodaTime)                  (Fachlogik: TimeZoneName, TimeResult, Umrechnung, Zeitdifferenz)
tests/
  McpTime.Core.Tests       Business-Fälle (rebuild/parity/business-cases.md), FakeTimeProvider
  McpTime.Protocol.Tests   Schnittstellen-Fälle (rebuild/parity/interface-cases.md), MCP-Client gegen den gebauten Server, Verify-Snapshots
  McpTime.ArchitectureTests
```

- Pfeile = erlaubte Abhängigkeiten, sonst keine. `ModelContextProtocol` nur in Host und Server, `System.CommandLine` nur im Host.
- **Erzwungen** durch `McpTime.ArchitectureTests` (NetArchTest): Abhängigkeitsrichtungen, kein MCP-SDK in Core, kein `Console.Out`/`Console.WriteLine` außer im Transport.
- `TreatWarningsAsErrors`, Nullable an, `AnalysisLevel latest-recommended`, Trim-Analyzer an (D19).
- `.slnx`, zentrale Paketverwaltung (`Directory.Packages.props`), `global.json` mit Microsoft Testing Platform (D14).

## Hard rules

- ≤ 300 Zeilen pro Datei, eine Verantwortung pro Klasse.
- **stdout gehört nur dem MCP-Transport**; Logs nach stderr (D16).
- „Jetzt“ nur über `TimeProvider` – nie `DateTime.Now`, `DateTimeOffset.UtcNow` oder `SystemClock` direkt (D13).
- Zeitzonen nur über NodaTime-tzdb; ein Name ist nur gültig, wenn er exakt (ordinal) in `DateTimeZoneProviders.Tzdb.Ids` steht (D3, D4). Kein `TimeZoneInfo` in Core – nur im Host zum Erkennen der lokalen Zone (D5).
- Fachfehler als selbst gebautes `CallToolResult { IsError = true }` mit klarem Text; nie `McpException`/`McpProtocolException` für Fachfehler (D7). Fehlertexte nennen Feld, Wert und erwartetes Format.
- Alle vier Tool-Annotations explizit setzen (D9); Tools explizit registrieren, nicht per Reflection-Scan (D12); JSON nur über `JsonSerializerContext` (D11).
- Tool-Namen, Parameter, Ergebnisfelder bleiben kompatibel zum Original (PRD P2). Beschreibungstexte selbst formulieren: Zweck, Formate, lokale Zone als Standard.
- Keine neuen Pakete ohne Rückfrage. Freigegeben: `ModelContextProtocol` 2.3.x, `NodaTime` 3.3.x, `System.CommandLine` 2.0.x, `Microsoft.Extensions.Hosting`, `xunit.v3`, `Microsoft.Extensions.TimeProvider.Testing`, `Verify.XunitV3`, `NetArchTest.eNhancedEdition`.
- Das Original wird nie gebaut, gestartet oder gelesen. Fehlt etwas im Plan, ist das eine Lücke → nachfragen.

## Forbidden (legacy)

- Python-Fehlerwortlaut, pydantic-Schema-Titel als Vertrag behandeln.
- Middleware-Krücke für unbekannte Tools (SDK erledigt das, D8).
- Plattformabhängige Zeitzonen-Prüfung (Groß-/Kleinschreibung, Windows-IDs, Pfade).

## Commands

```bash
dotnet build -warnaserror
dotnet test                                  # alle Tests (Core, Protokoll, Architektur)
dotnet run --project src/McpTime.Host -- --local-timezone Europe/Vienna
dotnet pack src/McpTime.Host -c Release      # dotnet tool (D17)
npx @modelcontextprotocol/inspector --cli dotnet run --project src/McpTime.Host --method tools/list
dotnet format --verify-no-changes
```

## Way of working

1. Slice-Datei lesen (`rebuild/slices/NN-*.md`), die genannten PRD-Abschnitte und Entscheidungen in `rebuild/research.md`.
2. Plan vorstellen (Dateien, Klassen, Tests), in `rebuild/plans/` ablegen, auf Freigabe warten.
3. Tests der Slice zuerst – sie müssen rot sein.
4. Minimal implementieren bis grün, dann aufräumen.
5. Definition of Done prüfen, Abweichungen in `rebuild/parity/deviations.md`, Slice in `rebuild/checklist.md` abhaken.
6. Ein Commit pro grüner Slice. Unklar = fragen.
