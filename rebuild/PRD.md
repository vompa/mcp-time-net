# PRD – mcp-time (Rebuild auf .NET 10)

> Beschreibt das Was, nicht das Wie. Begriffe: `GLOSSARY.md`. Erwartete Werte mit Quelle: `parity/`.
> Rangfolge bei Widersprüchen in der Analyse: Tests > Code > README.

## Zweck

Ein kleiner lokaler MCP-Server, der einem KI-Modell zwei Fähigkeiten gibt: die aktuelle Uhrzeit in einer beliebigen Zeitzone nennen und eine Uhrzeit zwischen Zeitzonen umrechnen, korrekt über Sommerzeit, Viertelstunden-Offsets und Datumsgrenzen hinweg.

## Prinzipien

- **P1 Fachlich korrekt vor allem anderen.** Offsets, Sommerzeit-Flags und Wochentage gelten für den tatsächlichen Zeitpunkt in der jeweiligen Zone.
- **P2 Kompatibel für Aufrufer.** Tool-Namen, Parameter, Ergebnisfelder und Annotations bleiben gleich, damit bestehende Clients und Prompts weiter funktionieren.
- **P3 Strenge Eingaben, klare Fehler.** Nur exakte IANA-Namen, auf allen Plattformen gleich. Fehlertexte nennen, was falsch ist, ohne Interna.
- **P4 Rein lesend und lokal.** Kein Netzwerk, keine Dateien außer der Zeitzonen-Datenbank. stdout gehört nur dem MCP-Transport.
- **P5 Testbar ohne echte Uhr.** „Jetzt“ kommt aus einer austauschbaren Zeitquelle.

## Nutzer

| Rolle | Typische Aufgaben |
|---|---|
| KI-Modell (über einen MCP-Client) | „Wie spät ist es in Tokio?“, „Was ist 14:00 Wien in New York?“ |
| Betreiber | Server installieren, lokale Zeitzone bei Bedarf überschreiben |

## Features

### F1 Server & Protokoll
**Story**: Als MCP-Client will ich mich verbinden und die Tools mit Schemas sehen, egal welche Protokollgeneration ich spreche.
- AC1 Given ein Client mit 2025-11-25, When `initialize`, Then Server-Info `mcp-time` mit Version, Fähigkeit `tools`, keine Prompts und Ressourcen.
- AC2 Given ein Client mit 2026-07-28, When er sitzungslos anfragt, Then dieselbe Tool-Liste wie in AC1.
- AC3 When `tools/list`, Then genau `get_current_time` und `convert_time`, jeweils mit Eingabe- und Ausgabeschema und den Annotations readOnly true, destructive false, idempotent true, openWorld false.
- AC4 Given ein Aufruf von `no_such_tool`, Then JSON-RPC-Fehler `-32602` mit dem Toolnamen in der Meldung.
- AC5 Die Parameterbeschreibungen nennen die lokale Zeitzone als Standard, wenn der Nutzer keine angibt.

### F2 Aktuelle Zeit
**Story**: Als Modell will ich die aktuelle Zeit in einer Zeitzone erfahren, damit ich Zeitfragen richtig beantworte.
- AC1 Given „jetzt“ = 2024-01-01 12:00:00Z, When `get_current_time{timezone:"UTC"}`, Then `{timezone:"UTC", datetime:"2024-01-01T12:00:00+00:00", day_of_week:"Monday", is_dst:false}` als `structuredContent` und als JSON-Textblock.
- AC2 Sommerzeit, Viertelstunden-Offsets und Datumsgrenze laut `parity/business-cases.md` B-NOW.
- AC3 Der Zeitzonenname kommt so zurück, wie er angefragt wurde.
- AC4 Fehlt `timezone` oder ist es leer, Then `isError` mit „Missing required argument: timezone“. Die lokale Zeitzone wird hier **nicht** stillschweigend eingesetzt.
- AC5 Unbekannter Name, falsche Groß-/Kleinschreibung oder Pfad, Then `isError`. Der Text nennt „Invalid timezone“ und den Wert.
- Zusätzliche, unbekannte Argumente werden ignoriert.

### F3 Zeit umrechnen
**Story**: Als Modell will ich eine Uhrzeit von einer Zone in eine andere umrechnen.
- AC1 Given „jetzt“ = 2024-01-01 00:00Z, When `convert_time{source_timezone:"Europe/London", time:"12:00", target_timezone:"Europe/Warsaw"}`, Then Ziel `2024-01-01T13:00:00+01:00`, `time_difference:"+1.0h"`.
- AC2 Bezugstag ist „heute“ in der Quellzone (z. B. 20:00Z ist in Tokio schon Dienstag). Mit dem neuen optionalen `date` gilt stattdessen dieses Datum (siehe Verbesserungen).
- AC3 Differenzformat: ganze Stunden mit einer Nachkommastelle, Bruchteile ohne überflüssige Nullen (`+5.75h`, `+5.5h`, `-4.75h`, `+0.0h`).
- AC4 Nicht existierende Ortszeit (02:30 New York am 2024-03-10), Then `isError`. Der Text nennt Zeit, Zone, Datum und „skipped by a clock change“.
- AC5 Doppeldeutige Ortszeit (01:30 New York am 2024-11-03), Then erstes Vorkommen (Sommerzeit, -04:00), Differenz zu UTC `+4.0h`.
- AC6 `time` im Format `H:M` bis `HH:MM` (24 h). Alles andere, Then `isError` „Invalid time format. Expected HH:MM [24-hour format]“.
- AC7 Leere Quell- oder Zielzone, Then „Missing required argument: source_timezone“ bzw. „… target_timezone“.

### F4 Start & lokale Zeitzone
- AC1 `--local-timezone Europe/Vienna` überschreibt die erkannte Zone, die Parameterbeschreibungen nennen sie.
- AC2 Ungültiger Wert, Then der Prozess endet vor dem Transport mit genau einer Zeile auf stderr: `Error: invalid --local-timezone '<wert>': not a known IANA timezone name`, Exit-Code ≠ 0.
- AC3 Leerer Wert zählt als nicht gesetzt. Ist die Systemzone nicht ermittelbar, gilt `UTC`.
- AC4 `--help` beschreibt den Server, unbekannte Optionen werden abgelehnt.

## API (Vertrag)

MCP über stdio, beide Protokollgenerationen. Tools, Parameter und Ergebnisfelder wie in `analysis.md` → Features/Interfaces. Fachfehler als Tool-Ergebnis mit `isError`, unbekanntes Tool als JSON-RPC `-32602`. Beschreibungstexte formuliert der Rebuild selbst. Sie müssen Zweck, Formate und den Standard der lokalen Zeitzone nennen.

## Verbesserungen mit Absicht

- **Optionales `date` (YYYY-MM-DD) bei `convert_time`.** Termine an anderen Tagen werden mit dem richtigen Offset umgerechnet. Ohne `date` gilt das bisherige Verhalten, das ist also rückwärtskompatibel.
- **Eigene, klare Fehlertexte** ohne Python-Interna. Die Information bleibt gleich (Parity prüft Inhalt, nicht Wortlaut).
- **Schema-Titel bewusst gewählt** statt Nebenprodukt aus Klassennamen.
- **Unbekanntes Tool** direkt über das SDK, ohne Middleware-Krücke.
- Weitere Verbesserungen aus der Zielstack-Recherche: siehe `research.md` → Entscheidungen.

## Non-goals

- Weitere Tools (Kalender, Feiertage, Weltzeituhr), lokalisierte Wochentage, HTTP-Transport.
