# Analyse – mcp-time (MCP-Referenzserver „time“)

> Quelle: `modelcontextprotocol/servers`, Ordner `src/time` @ `51b6382`, Paketversion 2026.10.19, Lizenz MIT. 310 Zeilen Produktivcode, 4 Testdateien mit rund 1.900 Zeilen.

## Stack & runtime

- Python ≥ 3.10, MCP Python SDK 2.x (`MCPServer`, high-level API), pydantic für Ergebnis-Modelle, `zoneinfo` + `tzdata` für IANA-Zeitzonen, `tzlocal` für die lokale Zeitzone.
- Start über die Konsole (`mcp-server-time`, auch `python -m mcp_server_time`), Transport stdio. Verteilung über `uvx`, `pip` und Docker.
- Einzige Option: `--local-timezone <IANA-Name>`.
- Tests: pytest mit eingefrorener Uhr (freezegun), Protokolltests über einen echten MCP-Client.

## Domain

- **Zeitzone**: IANA-Name wie `Europe/Vienna`. Gültig ist nur der exakte Schlüssel aus der Zeitzonen-Datenbank. Kleinschreibung (`europe/warsaw`), Pfade (`../etc/passwd`, `/etc/localtime`) und unbekannte Namen werden abgelehnt.
- **Lokale Zeitzone**: aus `--local-timezone`, sonst vom Betriebssystem erkannt, sonst `UTC`. Ein leerer Override zählt als nicht gesetzt. Ein ungültiger Override beendet den Start mit einer einzeiligen Fehlermeldung, bevor der Transport öffnet.
- **Zeitangabe (`time`)**: 24-Stunden-Format `HH:MM`. Einstellige Stunden und Minuten werden akzeptiert (`9:5` = 09:05).
- **Bezugstag der Umrechnung**: immer „heute“ in der Quell-Zeitzone. Ein Datum lässt sich nicht angeben.
- **Nicht existierende Ortszeit** (Sommerzeit-Sprung, z. B. 02:30 in New York am 10.03.2024): Fehler mit Zeit, Zone und Datum.
- **Doppeldeutige Ortszeit** (Zeitumstellung zurück, z. B. 01:30 in New York am 03.11.2024): Es gilt das erste Vorkommen, also noch Sommerzeit.
- **Zeitdifferenz**: Offset Ziel minus Offset Quelle zum umgerechneten Zeitpunkt, in Stunden mit Vorzeichen. Ganze Stunden mit einer Nachkommastelle (`+1.0h`, `+0.0h`, `-5.0h`), Bruchteile ohne überflüssige Nullen (`+5.5h`, `+5.75h`, `-4.75h`).
- **Ergebnis je Zeitpunkt**: Zeitzonenname wie angefragt (nicht kanonisiert), Datum/Uhrzeit ISO 8601 auf Sekunden mit Offset, englischer Wochentag, Sommerzeit-Flag.

## Features

| Tool | Zweck | Eingabe | Ergebnis |
|---|---|---|---|
| `get_current_time` | aktuelle Zeit in einer Zeitzone | `timezone` | `{timezone, datetime, day_of_week, is_dst}` |
| `convert_time` | Ortszeit von einer Zone in eine andere umrechnen | `source_timezone`, `time`, `target_timezone` | `{source, target, time_difference}` |

Beide Tools sind rein lesend: `readOnlyHint` true, `destructiveHint` false, `idempotentHint` true, `openWorldHint` false.
Die Parameterbeschreibungen nennen die erkannte lokale Zeitzone, damit ein Modell sie als Standard verwendet, wenn der Nutzer keine nennt.

## Interfaces

- **Server**: Name `mcp-time`, Version aus der Paketversion. Keine Prompts, keine Ressourcen, keine List-Change-Benachrichtigungen.
- **Protokoll**: beide Spec-Generationen. 2025-11-25 per `initialize`-Handshake (inklusive `ping`), 2026-07-28 sitzungslos per `server/discover` oder Request-Envelope. Die Tool-Liste ist in beiden identisch.
- **Ergebnisform**: `structuredContent` mit dem Ergebnisobjekt plus ein Textblock mit demselben Objekt als eingerücktes JSON. Jedes Tool veröffentlicht ein `outputSchema`.
- **Fehler**:
  - Fachfehler sind Tool-Ergebnisse mit `isError: true`: ungültige Zeitzone, falsches Zeitformat, leeres Pflichtfeld („Missing required argument: …“), nicht existierende Ortszeit.
  - Ein unbekanntes Tool ist ein JSON-RPC-Fehler `-32602` „Unknown tool: …“.
  - Unbekannte zusätzliche Argumente werden ignoriert.
- **CLI**: `--help` beschreibt den Server, unbekannte Argumente werden abgelehnt, `--local-timezone` wird vor dem Start geprüft.

## Non-functional

- Deterministisch bis auf „jetzt“. Tests frieren die Uhr ein.
- Keine Netzwerkzugriffe, keine Dateizugriffe außer der Zeitzonen-Datenbank.
- Plattformunterschiede bewusst eingeebnet: Zeitzonen-Schlüssel werden auf allen Systemen gleich streng geprüft (Issue #5060, macOS akzeptierte Kleinschreibung).
- Fehlermeldungen beim Start zeigen keine Dateipfade.

## Existing tests

- `test_server.py`: Fachlogik mit eingefrorener Uhr: aktuelle Zeit in mehreren Zonen und Jahreszeiten, Umrechnungen mit erwarteter Differenz, Fehlerfälle. → Business-Vertrag.
- `test_protocol.py`: über einen echten MCP-Client. Geprüft werden:
  - Server-Info, Tool-Liste mit Ein- und Ausgabeschemas
  - Ergebnisform
  - Zeitzonen über Datumsgrenze und Sommerzeit
  - Viertelstunden-Offsets
  - nicht existierende und doppeldeutige Zeiten
  - Argumentprüfung
  - Verhalten der lokalen Zeitzone

  → Business- und Schnittstellen-Vertrag.
- `test_eras.py`: beide Protokollgenerationen, Fehler in beiden, unbekanntes Tool und unbekannte Methode als JSON-RPC-Fehler, `ping` nur in der alten Generation. → Schnittstellen-Vertrag.
- `test_entrypoints.py`: CLI-Optionen, Start über stdio, ungültige lokale Zeitzone. → Schnittstellen-Vertrag, teilweise Python-spezifisch (`python -m`).
- **Implementierungsgebunden**:
  - Wortlaut der Python-Fehlermeldungen von `zoneinfo` („ZoneInfo keys must refer to subdirectories of TZPATH …“)
  - Pydantic-Titel im Schema („Day Of Week“)
  - Python-Einstiegspunkte

## Legacy findings

- Kein Datum bei der Umrechnung: Es wird immer „heute“ in der Quellzone angenommen. Termine an einem anderen Tag rechnet das Modell daher mit dem falschen Offset um, sobald eine Zeitumstellung dazwischenliegt.
- Fehlertexte enthalten Python-Interna (Fehlerklassen-Wortlaut, `repr` mit Anführungszeichen).
- Schema-Titel aus pydantic-Klassennamen („Is Dst“) sind ein Nebenprodukt, kein bewusster Vertrag.
- Die Middleware für unbekannte Tools umgeht einen SDK-Mangel (python-sdk#3659) und soll entfallen, sobald das SDK es selbst kann.
- Wochentage nur auf Englisch, Zeitdifferenz als String statt als Zahl.
- Sonst sauber: kleine Dateien, eine Verantwortung je Funktion, gut getestet.

## Open questions

Siehe `open-questions.md`.
