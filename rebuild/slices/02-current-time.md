# Slice 02 – Time zones & current time

**Goal:** get_current_time returns the correct time for every valid IANA zone and clearly rejects everything else.
**PRD reference:** F2
**Depends on:** 01

## Scope
- TimeZoneName: exact IANA keys, equally strict on all platforms, paths rejected
- TimeResult: ISO 8601 with offset, English weekday, daylight saving flag
- Result shape: structuredContent plus JSON text block
- Business errors as isError with clear information

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "F2".
Implement slice 02 "Time zones & current time" (this file).
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
Conversion, CLI
