# Slice 00 – Scaffold

**Goal:** Lauffähige leere Solution mit allen Quality Gates und einem Test-Harness für Unit- und Protokolltests.
**PRD reference:** Prinzipien P1–P5; CLAUDE.md
**Depends on:** –

## Scope
- Solution mit den Projekten aus CLAUDE.md, zentrale Paketverwaltung, global.json
- Architekturtests (Abhängigkeitsrichtungen, kein MCP-SDK in der Fachlogik)
- CI (GitHub Actions): Build ohne Warnungen, Tests, Format – Linux, macOS, Windows
- Test-Harness: FakeTimeProvider für Unit-Tests, MCP-Client für Protokolltests gegen den gebauten Server

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "Prinzipien P1–P5; CLAUDE.md".
Implement slice 00 "Scaffold" (this file).
0. If this slice needs a library or pattern research.md does not cover,
   look up its current documentation first and add the findings to rebuild/research.md.
1. First present a plan (files, classes, tests) and wait for approval.
2. Write this slice's acceptance and parity tests as failing tests (RED).
3. Implement the minimum to get green, then refactor. Follow every rule in CLAUDE.md.
4. Check the Definition of Done and report any deviations.
```

## Definition of Done
- [ ] Acceptance criteria green as tests
- [ ] Parity cases green: Architekturtests grün; Harness startet einen leeren Server und liest die Tool-Liste
- [ ] Build without warnings, architecture tests green
- [ ] No file > 300 lines, no new packages without approval
- [ ] No dead code, no TODOs without an issue

## Non-goals
Tools, Fachlogik
