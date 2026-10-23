# Offene Fragen

## Architekturrelevant

> Stand 23.10.2026: Fragen 1 und 3 entschieden (NodaTime, dotnet tool über dnx) – siehe `research.md` D3, D17. Frage 2 als Verbesserung im PRD übernommen.

1. **Zeitzonen-Bibliothek**: das eingebaute `TimeZoneInfo` (IANA-IDs seit .NET 6, ICU) oder NodaTime? `[Annahme]` Entscheidung in `research.md`.
2. **Optionales Datum bei `convert_time`**: Soll der Rebuild ein optionales `date` (YYYY-MM-DD) annehmen, damit Termine an anderen Tagen richtig umgerechnet werden? `[Annahme]` Ja, als bewusste Verbesserung. Ohne `date` gilt weiter „heute“ in der Quellzone, das Verhalten für bestehende Aufrufer bleibt also gleich.
3. **Verteilung**: `dotnet tool` über NuGet/`dnx`, Self-contained-Binaries oder Docker? `[Annahme]` Entscheidung in `research.md`.

## Fachlich

4. **Fehlertexte**: Der Rebuild formuliert eigene, klare Texte ohne Python-Interna. Er nennt aber dieselbe Information: Was ist ungültig, welcher Wert, welches Format wird erwartet. `[Annahme]` Ja (implementierungsgebunden laut Parity).
5. **Zeitdifferenz zusätzlich als Zahl?** Etwa `time_difference_hours: 5.75` neben dem String. `[Annahme]` Nein. Kann später als Verbesserung kommen, ohne den Vertrag zu brechen.
6. **Wochentag lokalisiert?** `[Annahme]` Nein, Englisch bleibt (Vertrag gegenüber Modellen).

## Im Rebuild gestrichen

- Python-Einstiegspunkte (`python -m`), freezegun-spezifische Testmechanik, die Middleware-Krücke für unbekannte Tools (das .NET-SDK erledigt das, siehe `research.md`).
