# Deviation-Log

Urteil: Fehler im Rebuild / Verbesserung mit Absicht (+ PRD-Verweis).

| Fall | Original (laut Analyse) | Rebuild | Urteil |
|---|---|---|---|
| `convert_time` mit Datum | kein `date`, immer „heute“ | optionales `date` (B-NEW-1..3) | Verbesserung mit Absicht, PRD „Verbesserungen“ |
| Fehlertext ungültige Zeitzone | Python-Wortlaut („No time zone found with key '…'“) | eigener Text mit „Invalid timezone“ und Wert | Verbesserung mit Absicht |
| Schema-Titel | aus pydantic-Klassennamen („Is Dst“) | bewusst gewählt | Verbesserung mit Absicht |
| Fehlertext fehlendes Argument (B-ERR-1/2) | „Missing required argument: timezone“ | gleicher Anfang plus Formathinweis „Expected an IANA time zone name such as 'Europe/Vienna'.“ | Verbesserung mit Absicht, PRD „Eigene, klare Fehlertexte“ |
| `is_dst` bei negativer Sommerzeit (Europe/Dublin) | vermutlich `bool(dst())` aus tzdata, nicht durch Tests belegt | tzdb-Savings ≠ 0: Winter `true` (Savings −01:00), Sommer `false`; Test `NegativeSavingsCountAsDaylightSavingTime` | bewusst so (User, 2026-10-24, Plan Slice 02 Annahme 1): folgt der tzdb und dem irischen Recht, widerspricht aber dem Alltagsverständnis |
| Bereich für `date` | – (kein `date` im Original) | nur `0001-01-02` bis `9999-12-29`, sonst „Invalid date“ mit Format und Bereich; `null` oder `""` = heute in der Quellzone | Verbesserung mit Absicht (Plan Slice 03 Annahme 2, Obergrenze per Task-Review): verhindert Überlauf und damit einen generischen SDK-Fehler |
| Offsets mit Sekundenanteil (historische Ortszeit) | – (ohne `date` nicht erreichbar) | voll ausgegeben, z. B. `1850-01-01T12:00:00+01:05:21`; sonst `+HH:MM` | Verbesserung mit Absicht (Plan Slice 03 Annahme 3), wie Python `isoformat`; nicht RFC 3339 |
| Fehlertexte `convert_time` (B-ERR-6, B-EDGE-2, B-NEW-2) | Python-Wortlaut | eigene Texte: „Invalid time format. Expected HH:MM [24-hour format] …“ mit Wert, „Invalid date …“ mit Format und Bereich, übersprungene Ortszeit mit Zeit, Zone, Datum und „skipped by a clock change“, „Missing required argument: time …“ | Verbesserung mit Absicht, PRD „Eigene, klare Fehlertexte“ |
| Mehrere ungültige Argumente | nicht festgelegt | nur der erste Fehler in der Reihenfolge `source_timezone`, `time`, `target_timezone`, `date` | bewusst so (Plan Slice 03), Test `FirstInvalidArgumentWinsAsync` |
| Zeitformat | `H:M` bis `HH:MM` | gleich, aber nur ASCII-Ziffern, keine Leerzeichen, kein abschließender Zeilenumbruch | Verbesserung mit Absicht (Plan Slice 03 Annahme 1) |
| Nicht-String-Wert für `timezone` (z. B. `42`) | im Original durch pydantic abgelehnt, Wortlaut nicht im Vertrag | `isError` „Invalid timezone“ mit dem rohen JSON-Wert; JSON `null` zählt als fehlend | Verbesserung mit Absicht (Plan Slice 02 Annahme 3) |
