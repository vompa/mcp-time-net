# Slice 04 – Start, lokale Zeitzone & Verteilung

**Goal:** Server startet mit erkannter oder überschriebener lokaler Zeitzone und ist installierbar wie in research.md entschieden.
**PRD reference:** F4, F1 AC5
**Depends on:** 02

## Scope
- --local-timezone mit Prüfung vor dem Transport, eine Zeile auf stderr bei Fehler
- Systemzone erkennen, Fallback UTC; lokale Zone in den Parameterbeschreibungen
- --help, unbekannte Optionen
- Paketierung und Auslieferung laut research.md, Smoke-Test in der CI

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "F4, F1 AC5".
Implement slice 04 "Start, lokale Zeitzone & Verteilung" (this file).
0. If this slice needs a library or pattern research.md does not cover,
   look up its current documentation first and add the findings to rebuild/research.md.
1. First present a plan (files, classes, tests) and wait for approval.
2. Write this slice's acceptance and parity tests as failing tests (RED).
3. Implement the minimum to get green, then refactor. Follow every rule in CLAUDE.md.
4. Check the Definition of Done and report any deviations.
```

## Definition of Done
- [ ] Acceptance criteria green as tests
- [ ] Parity cases green: I-LIST-6, I-CLI-1..3
- [ ] Build without warnings, architecture tests green
- [ ] No file > 300 lines, no new packages without approval
- [ ] No dead code, no TODOs without an issue

## Non-goals
HTTP-Transport
