# Slice 03 – Conversion

**Goal:** convert_time converts wall-clock times correctly across daylight saving time, quarter-hour offsets, and date boundaries – with an optional date.
**PRD reference:** F3, Improvements
**Depends on:** 02

## Scope
- Time format H:M to HH:MM, reference date today in the source zone or optional date
- Skipped wall-clock time as an error, ambiguous wall-clock time as the first occurrence
- Time difference at the converted moment in the format +1.0h / +5.75h
- Extend the input schema with an optional date

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "F3, Improvements".
Implement slice 03 "Conversion" (this file).
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
Several target zones at once
