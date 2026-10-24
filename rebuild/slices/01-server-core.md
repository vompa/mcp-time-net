# Slice 01 – Server core & protocol

**Goal:** The server speaks both protocol generations and lists both tools with schemas and annotations.
**PRD reference:** F1
**Depends on:** 00

## Scope
- Host with stdio transport, stdout only for JSON-RPC, logs to stderr
- Both tools registered with own descriptions, input and output schema, annotations; bodies without logic for now
- Unknown tool as JSON-RPC -32602, unknown arguments ignored
- Server name and version, no prompts/resources

## Prompt
```
Read CLAUDE.md, rebuild/GLOSSARY.md, rebuild/research.md and the rebuild/PRD.md sections "F1".
Implement slice 01 "Server core & protocol" (this file).
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
Time logic
