# Slice 02 – Zeitzonen & aktuelle Zeit

**Goal:** get_current_time liefert für jede gültige IANA-Zone die richtige Zeit, lehnt alles andere klar ab.
**PRD reference:** F2
**Depends on:** 01

## Scope
- TimeZoneName: exakte IANA-Schlüssel, gleich streng auf allen Plattformen, Pfade abgelehnt
- TimeResult: ISO 8601 mit Offset, englischer Wochentag, Sommerzeit-Flag
- Ergebnisform: structuredContent plus JSON-Textblock
- Fachfehler als isError mit klarer Information

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "F2".
Implement slice 02 "Zeitzonen & aktuelle Zeit" (this file).
0. If this slice needs a library or pattern research.md does not cover,
   look up its current documentation first and add the findings to rebuild/research.md.
1. First present a plan (files, classes, tests) and wait for approval.
2. Write this slice's acceptance and parity tests as failing tests (RED).
3. Implement the minimum to get green, then refactor. Follow every rule in CLAUDE.md.
4. Check the Definition of Done and report any deviations.
```

## Definition of Done
- [ ] Acceptance criteria green as tests
- [ ] Parity cases green: B-NOW-1..15, B-ERR-1..5, I-RES-1, I-ERR-3, I-ERR-4 (isError false)
- [ ] Build without warnings, architecture tests green
- [ ] No file > 300 lines, no new packages without approval
- [ ] No dead code, no TODOs without an issue

## Non-goals
Umrechnung, CLI
