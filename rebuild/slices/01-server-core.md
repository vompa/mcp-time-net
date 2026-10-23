# Slice 01 – Server-Kern & Protokoll

**Goal:** Server spricht beide Protokollgenerationen und listet beide Tools mit Schemas und Annotations.
**PRD reference:** F1
**Depends on:** 00

## Scope
- Host mit stdio-Transport, stdout nur für JSON-RPC, Logs nach stderr
- beide Tools registriert mit eigenen Beschreibungen, Ein- und Ausgabeschema, Annotations; Rümpfe vorerst ohne Logik
- unbekanntes Tool als JSON-RPC -32602, unbekannte Argumente ignoriert
- Server-Name und Version, keine Prompts/Ressourcen

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "F1".
Implement slice 01 "Server-Kern & Protokoll" (this file).
0. If this slice needs a library or pattern research.md does not cover,
   look up its current documentation first and add the findings to rebuild/research.md.
1. First present a plan (files, classes, tests) and wait for approval.
2. Write this slice's acceptance and parity tests as failing tests (RED).
3. Implement the minimum to get green, then refactor. Follow every rule in CLAUDE.md.
4. Check the Definition of Done and report any deviations.
```

## Definition of Done
- [ ] Acceptance criteria green as tests
- [ ] Parity cases green: I-INIT-1, I-INIT-2, I-LIST-1..5, I-LIST-7, I-ERR-1, I-ERR-2, I-ERR-4, I-LOG-1
- [ ] Build without warnings, architecture tests green
- [ ] No file > 300 lines, no new packages without approval
- [ ] No dead code, no TODOs without an issue

## Non-goals
Zeitlogik
