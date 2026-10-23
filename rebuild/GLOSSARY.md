# Glossar

| Begriff | Code | Bedeutung |
|---|---|---|
| IANA-Zeitzone | `TimeZoneName` | exakter Schlüssel der Zeitzonen-Datenbank, z. B. `Europe/Vienna` |
| Lokale Zeitzone | `LocalTimeZone` | aus `--local-timezone`, sonst vom System erkannt, sonst `UTC` |
| Zeitpunkt-Ergebnis | `TimeResult` | `timezone`, `datetime` (ISO 8601 mit Offset, Sekunden), `day_of_week` (Englisch), `is_dst` |
| Umrechnungs-Ergebnis | `TimeConversionResult` | `source`, `target` (je `TimeResult`), `time_difference` |
| Zeitdifferenz | `TimeDifference` | Offset Ziel − Offset Quelle zum umgerechneten Zeitpunkt, z. B. `+5.75h` |
| Ortszeit | – | Wanduhrzeit `HH:MM` in einer Zone |
| Bezugstag | `ReferenceDate` | Datum der Umrechnung: `date`, sonst „heute“ in der Quellzone |
| Nicht existierende Ortszeit | – | durch Sommerzeit-Sprung übersprungene Uhrzeit → Fehler |
| Doppeldeutige Ortszeit | – | bei Zeitumstellung doppelt vorkommende Uhrzeit → erstes Vorkommen |
| Zeitquelle | `TimeProvider` | austauschbare Quelle für „jetzt“ (Tests: eingefroren) |
| Protokollgeneration | – | 2025-11-25 (Handshake) oder 2026-07-28 (sitzungslos) |
