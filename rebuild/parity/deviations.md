# Deviation-Log

Urteil: Fehler im Rebuild / Verbesserung mit Absicht (+ PRD-Verweis).

| Fall | Original (laut Analyse) | Rebuild | Urteil |
|---|---|---|---|
| `convert_time` mit Datum | kein `date`, immer „heute“ | optionales `date` (B-NEW-1..3) | Verbesserung mit Absicht, PRD „Verbesserungen“ |
| Fehlertext ungültige Zeitzone | Python-Wortlaut („No time zone found with key '…'“) | eigener Text mit „Invalid timezone“ und Wert | Verbesserung mit Absicht |
| Schema-Titel | aus pydantic-Klassennamen („Is Dst“) | bewusst gewählt | Verbesserung mit Absicht |
