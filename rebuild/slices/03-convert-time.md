# Slice 03 – Umrechnung

**Goal:** convert_time rechnet Ortszeiten korrekt über Sommerzeit, Viertelstunden-Offsets und Datumsgrenzen um – mit optionalem Datum.
**PRD reference:** F3, Verbesserungen
**Depends on:** 02

## Scope
- Zeitformat H:M bis HH:MM, Bezugstag heute in der Quellzone oder optionales date
- nicht existierende Ortszeit als Fehler, doppeldeutige als erstes Vorkommen
- Zeitdifferenz zum umgerechneten Zeitpunkt im Format +1.0h / +5.75h
- Eingabeschema um optionales date erweitern

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "F3, Verbesserungen".
Implement slice 03 "Umrechnung" (this file).
0. If this slice needs a library or pattern research.md does not cover,
   look up its current documentation first and add the findings to rebuild/research.md.
1. First present a plan (files, classes, tests) and wait for approval.
2. Write this slice's acceptance and parity tests as failing tests (RED).
3. Implement the minimum to get green, then refactor. Follow every rule in CLAUDE.md.
4. Check the Definition of Done and report any deviations.
```

## Definition of Done
- [ ] Acceptance criteria green as tests
- [ ] Parity cases green: B-CONV-1..11, B-EDGE-1..4, B-ERR-6..8, B-NEW-1..3, I-LIST-3
- [ ] Build without warnings, architecture tests green
- [ ] No file > 300 lines, no new packages without approval
- [ ] No dead code, no TODOs without an issue

## Non-goals
mehrere Zielzonen auf einmal
