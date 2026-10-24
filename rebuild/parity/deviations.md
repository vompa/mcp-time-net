# Deviation-Log

Urteil: Fehler im Rebuild / Verbesserung mit Absicht (+ PRD-Verweis).

| Fall | Original (laut Analyse) | Rebuild | Urteil |
|---|---|---|---|
| `convert_time` mit Datum | kein `date`, immer „heute“ | optionales `date` (B-NEW-1..3) | Verbesserung mit Absicht, PRD „Verbesserungen“ |
| Fehlertext ungültige Zeitzone | Python-Wortlaut („No time zone found with key '…'“) | eigener Text mit „Invalid timezone“ und Wert | Verbesserung mit Absicht |
| Schema-Titel | aus pydantic-Klassennamen („Is Dst“) | bewusst gewählt | Verbesserung mit Absicht |
| Fehlertext fehlendes Argument (B-ERR-1/2) | „Missing required argument: timezone“ | gleicher Anfang plus Formathinweis „Expected an IANA time zone name such as 'Europe/Vienna'.“ | Verbesserung mit Absicht, PRD „Eigene, klare Fehlertexte“ |
| `is_dst` bei negativer Sommerzeit (Europe/Dublin) | vermutlich `bool(dst())` aus tzdata, nicht durch Tests belegt | tzdb-Savings ≠ 0: Winter `true` (Savings −01:00), Sommer `false`; Test `NegativeSavingsCountAsDaylightSavingTime` | bewusst so (User, 2026-10-24, Plan Slice 02 Annahme 1): folgt der tzdb und dem irischen Recht, widerspricht aber dem Alltagsverständnis |
| Nicht-String-Wert für `timezone` (z. B. `42`) | im Original durch pydantic abgelehnt, Wortlaut nicht im Vertrag | `isError` „Invalid timezone“ mit dem rohen JSON-Wert; JSON `null` zählt als fehlend | Verbesserung mit Absicht (Plan Slice 02 Annahme 3) |
