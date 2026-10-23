# Parity – was erhalten bleiben muss

Das Original ist die Vorlage, nicht die Messlatte. Diese Fälle sichern, dass beim Neubau nichts verloren geht, worauf sich Nutzer und Clients verlassen. Alles andere darf besser werden.

## Drei Arten

| Art | Datei | Im Rebuild |
|---|---|---|
| Business-Vertrag | `business-cases.md` (Zeiten, Offsets, Sommerzeit, Fehlerfälle, neue `date`-Fälle) | strikt prüfen |
| Schnittstellen-Vertrag | `interface-cases.md` (Protokoll, Tool-Liste, Schemas, Ergebnisform, CLI) | für Kompatibilität halten; Änderungen nur mit Absicht, im Deviation-Log |
| Implementierungsgebunden | – | nicht übernehmen: Wortlaut der Python-Fehlermeldungen, pydantic-Schema-Titel, Python-Einstiegspunkte |

## Vom Fall zum Test

- **Business-Fälle**: Unit-Tests gegen die Fachlogik, „jetzt“ über `FakeTimeProvider` eingefroren, eine Zeile der Tabellen = ein Testfall (parametrisiert).
- **Schnittstellen-Fälle**: Protokolltests mit dem MCP-Client des SDK, der den gebauten Server per stdio startet, einmal je Protokollgeneration.
- **Fehlertexte**: Geprüft wird, dass die genannte Information enthalten ist (Feldname, Wert, erwartetes Format), nicht der exakte Satz.
- Das Original wird weder gebaut noch gestartet. Alle Erwartungswerte stehen in diesen Dateien.

## Normalisierung

Nicht nötig für Business-Fälle (eingefrorene Zeit). Bei Protokolltests: Serverversion → `<VERSION>`.

## Deviation-Log

`deviations.md`: Fall | Original (laut Analyse) | Rebuild | Urteil (Fehler im Rebuild / Verbesserung mit Absicht + PRD-Verweis).
