# Slice 00 – Scaffold

**Goal:** A runnable empty solution with all quality gates and a test harness for unit and protocol tests.
**PRD reference:** Principles P1–P5; CLAUDE.md
**Depends on:** –

## Scope
- Solution with the projects from CLAUDE.md, central package management, global.json
- Architecture tests (dependency directions, no MCP SDK in the business logic)
- CI (GitHub Actions): build without warnings, tests, format – Linux, macOS, Windows
- Test harness: FakeTimeProvider for unit tests, MCP client for protocol tests against the built server

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "Principles P1–P5; CLAUDE.md".
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
- [ ] Parity cases green: architecture tests green; the harness starts an empty server and reads the tool list
- [ ] Build without warnings, architecture tests green
- [ ] No file > 300 lines, no new packages without approval
- [ ] No dead code, no TODOs without an issue

## Non-goals
Tools, business logic
