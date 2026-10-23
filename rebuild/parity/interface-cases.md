# Parity – Schnittstellen-Vertrag (für Kompatibilität halten)

Jeder Fall wird ein Abnahmetest: Ein MCP-Client startet den Rebuild per stdio und prüft die Antwort gegen die Werte hier.

| ID | Prüfung | Erwartet |
|---|---|---|
| I-INIT-1 | `initialize` mit 2025-11-25 | Server-Name `mcp-time`, Version gesetzt, Fähigkeit `tools`; `ping` wird beantwortet |
| I-INIT-2 | sitzungslose Anfrage mit 2026-07-28 | Antwort in der neuen Generation; `ping` nicht angeboten |
| I-LIST-1 | `tools/list` (beide Generationen) | genau `get_current_time`, `convert_time`, gleiche Reihenfolge, gleich in beiden Generationen |
| I-LIST-2 | Eingabeschema `get_current_time` | Pflichtfeld `timezone` (string) |
| I-LIST-3 | Eingabeschema `convert_time` | Pflichtfelder `source_timezone`, `time`, `target_timezone` (string); optional `date` (string, neu) |
| I-LIST-4 | Ausgabeschemas | `TimeResult` mit Pflichtfeldern `timezone`, `datetime`, `day_of_week` (string), `is_dst` (boolean); `TimeConversionResult` mit `source`, `target`, `time_difference` |
| I-LIST-5 | Annotations beider Tools | readOnly true, destructive false, idempotent true, openWorld false |
| I-LIST-6 | Parameterbeschreibungen mit `--local-timezone Europe/Vienna` | nennen `Europe/Vienna` als Standard |
| I-LIST-7 | `prompts/list`, `resources/list` | leer bzw. nicht angeboten |
| I-RES-1 | erfolgreicher Tool-Aufruf | `structuredContent` = Ergebnisobjekt; ein Textblock mit demselben Objekt als JSON; `isError` false |
| I-ERR-1 | `tools/call` mit `no_such_tool` | JSON-RPC-Fehler `-32602`, Meldung enthält `no_such_tool` |
| I-ERR-2 | unbekannte Methode | JSON-RPC-Fehler (Methode nicht gefunden) |
| I-ERR-3 | Fachfehler | Tool-Ergebnis mit `isError: true`, kein JSON-RPC-Fehler |
| I-ERR-4 | zusätzliches unbekanntes Argument | wird ignoriert, Aufruf gelingt |
| I-CLI-1 | `--local-timezone Not/AZone` | Exit ≠ 0, genau eine Zeile auf stderr mit dem Wert, kein Stacktrace, kein Pfad |
| I-CLI-2 | `--help` | beschreibt den Server und die Option |
| I-CLI-3 | unbekannte Option | Exit ≠ 0 mit Hinweis |
| I-LOG-1 | stdout während des Betriebs | ausschließlich JSON-RPC-Nachrichten |
