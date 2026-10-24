# mcp-time-net – MCP server "time" on .NET 10

A small local MCP server (stdio) with two read-only tools: `get_current_time` (current time in an IANA time zone) and `convert_time` (convert a wall-clock time between two time zones, optionally on a specific date).

## Origin

This repo is a rebuild of the MCP reference server **"time"** on .NET 10:

- **Original:** [`modelcontextprotocol/servers` → `src/time`](https://github.com/modelcontextprotocol/servers/tree/51b6382/src/time), at commit `51b6382`, Python, MIT license.
- **Not a port, not a copy:** Only behavior, interfaces, and expected values were taken from the original (`rebuild/parity/`), no code. The rebuild itself never built, started, or read the original – everything it has to meet is in `rebuild/`.

## Test case for the skill `vibe-rebuild`

This repo is a **direct end-to-end test of the skill [`vibe-rebuild`](https://github.com/vompa/vibe-rebuild)** (version 2.2.0) and is linked there as a reference: The skill created the plan in `rebuild/` from the original, and the rebuild was then implemented slice by slice using only the `/rebuild` command that the skill generated. The Git history shows both phases unchanged:

| Commit | Time (CEST) | Phase |
|---|---|---|
| `591296e` | 2026-10-23 22:34 | Plan from `vibe-rebuild` 2.2.0 (28 files, no code) |
| `9979965` | 2026-10-23 23:37 | Slice 00 Scaffold |
| `4a23de6` | 2026-10-24 00:58 | Slice 01 Server core & protocol |
| `92cde94` | 2026-10-24 04:54 | Slice 02 Time zones & current time |
| `9918428` | 2026-10-24 05:28 | Slice 03 Conversion |
| `32bc117` | 2026-10-24 06:13 | Slice 04 Start, local time zone & distribution |
| `dfa7b1c` | 2026-10-24 06:18 | Addendum: architecture test "Server without NodaTime" |

Result: all parity cases from `rebuild/parity/` green, 202 tests (70 test methods, 35 of them theories) in three test projects green (2026-10-24), 853 lines of production code, 17 documented deviations in [`rebuild/parity/deviations.md`](rebuild/parity/deviations.md) – all deliberate (improvement or decision), none an error in the rebuild.

## How it was built with the skill

### Phase 1 – Create the plan (skill `vibe-rebuild`)

A separate session with the skill. Input: the original repo (read-only), target stack .NET 10, full scope. The skill works in these steps; the results are in `rebuild/`:

| Step | What the skill does | Result |
|---|---|---|
| 1. Inputs | asks for the repo, target stack (with concrete options), scope, and whether the original runs locally – in one round | Assumptions marked with `[Assumption]` |
| 2. Inventory | `scripts/inventory.py` counts projects, files, lines, tests, existing docs | `_inventory.md` |
| 3. Analysis | explorer subagents per area, reports as working material, spot check of hard numbers with `grep` | `analysis.md`, `_raw/` |
| 4. Open questions | grouped into architecture-relevant / business / dropped, each with an assumption | `open-questions.md` |
| 5. Target stack research | three passes: BROAD (12 topics), DEEP (source code of the MCP C# SDK, NodaTime, packaging, tests), DISTILL (decisions with sources) | `research.md` with D1–D20, `_raw/research-progress.md` |
| 6. PRD & glossary | behavior with Given/When/Then, deliberate improvements (optional `date`), terms | `PRD.md`, `GLOSSARY.md` |
| 7. Guardrails | target architecture with enforced dependency directions, hard rules, commands | `CLAUDE.md` |
| 8. Slicing | vertical slices by dependency, each with a prompt and Definition of Done; graph rendered | `slices/00–04`, `_slices.json`, `slice-graph*` |
| 9. Parity | parity cases from the original's tests, split into business and interface contract, with exact values | `parity/business-cases.md`, `parity/interface-cases.md`, `parity/README.md` |
| 10. Handover | assessment of rebuild vs. refactoring, checklist, `/rebuild` command | plan README (today this file), `checklist.md`, `.claude/commands/rebuild.md` |

The architecture-relevant questions (time zone library, distribution) were decided before slicing: NodaTime (D3) and `dotnet tool` through `dnx` (D17).

The skill's assessment at the time: The original is small (310 lines), clean, and well tested; a refactoring in the same stack is hardly worth it. The rebuild is worth it for the .NET ecosystem (`dotnet tool` through `dnx`) and brings two improvements: an optional date on conversion, and identical time zone data on every platform through NodaTime.

### Phase 2 – Implement the rebuild (`/rebuild`, a fresh session per slice)

Start Claude Code in the repo folder and type **`/rebuild`**. The command picks the lowest open slice whose dependencies are ticked off and works through it like this (with the superpowers plugin installed):

1. **Read:** the slice file, `CLAUDE.md`, the PRD sections it names, the decisions in `research.md`, the parity cases, and the glossary.
2. **Worktree:** `superpowers:using-git-worktrees` creates `.claude/worktrees/slice-NN` with branch `slice-NN`.
3. **Plan:** `superpowers:writing-plans` writes `rebuild/plans/<date>-slice-NN.md` with tasks, assumptions ("confirm on approval"), and review focus. **Claude stops here** and waits for approval.
4. **Implement:** `superpowers:subagent-driven-development` – one subagent per task, tests first (red), then minimal green; a review subagent after each task and an overall review at the end. Each task commits to `slice-NN` (for review packages and restarts).
5. **Finish:** full test suite, Definition of Done, deviations in `rebuild/parity/deviations.md`, tick in `rebuild/checklist.md`, deviations from the plan at the top of the plan file.
6. **Integrate:** `superpowers:finishing-a-development-branch` – after asking, `git merge --squash` as **one commit per slice** on `main`, then the worktree and branch are removed.
7. Claude ends with three lines ("Slice NN done", "Ready next: …", "Next step: start a fresh session and type /rebuild"). Use a new session for the next slice.

Without superpowers (or with `/rebuild NN direct`), the same runs without a worktree and subagents: branch `slice-NN`, present the plan, red tests, code, one commit.

| Input | What happens |
|---|---|
| `/rebuild` | next open slice whose dependencies are done |
| `/rebuild 03` | exactly slice 03; if a dependency is missing, Claude asks |
| `/rebuild status` | overview: done, in progress, ready to start, blocked |
| `/rebuild 03 direct` | without superpowers, even if it is installed |

After slice 02, slices 03 and 04 were independent and could have run in parallel in two sessions (`/rebuild 03`, `/rebuild 04`); in this test they ran one after the other.

### What the human decided in this test

Approvals and decisions that were not in the plan are recorded in the plan files and in `deviations.md`, among them:

- **No Verify** for snapshots (from v33 a sponsorship declaration is required in the build) → own JSON comparison, D14 adjusted (slice 00).
- **Negative daylight saving time** (Europe/Dublin) follows the tzdb: in winter `is_dst: true` (slice 02).
- **Range for `date`** `0001-01-02` to `9999-12-29`, so that no overflows occur (slice 03, from a task review).
- **Framework-dependent `dotnet tool`** instead of self-contained, localized CLI messages, new `--version` (slice 04).

Gaps that the rebuild had to close itself are recorded as findings in `rebuild/research.md` (one section per slice) and as "Deviations from the plan" at the top of the plan files.

## Build and test

Prerequisites: .NET 10 SDK (`dotnet --version` 10.x); optionally Node.js for the MCP Inspector.

```bash
dotnet build -warnaserror
dotnet test                                  # all tests (Core, protocol, architecture)
dotnet run --project src/McpTime.Host -- --local-timezone Europe/Vienna
dotnet pack src/McpTime.Host -c Release -o artifacts/package      # dotnet tool (D17)
dotnet dnx Vompa.McpTime --source artifacts/package --yes -- --help   # run from the package (`--` needed for --help)
npx @modelcontextprotocol/inspector --cli dotnet run --project src/McpTime.Host --method tools/list
dotnet format --verify-no-changes
```

## Publishing to nuget.org

`.github/workflows/release.yml` publishes `Vompa.McpTime` through Trusted Publishing (nuget.org policy `mcp-time-net-release`, no API key). A tag `vX.Y.Z` triggers it. The version must be identical in `Directory.Build.props` and in `src/McpTime.Host/.mcp/server.json` (both fields), otherwise the workflow fails.

```bash
git tag v0.1.0
git push origin v0.1.0
```

Then in the client: `{ "command": "dotnet", "args": ["dnx", "Vompa.McpTime", "--yes"] }` (on Windows, use `dotnet` with `dnx` as an argument, because only `dnx.cmd` exists).

Test call against the published package (needs Node.js; fetches `Vompa.McpTime` from nuget.org and calls `get_current_time`):

```bash
npx -y @modelcontextprotocol/inspector --cli dotnet dnx Vompa.McpTime --yes --method tools/call --tool-name get_current_time --tool-arg timezone=Europe/Vienna
```

Expected: `isError: false` and, under `structuredContent`, the current time in Vienna with `timezone`, `datetime`, `day_of_week`, and `is_dst`. Text typed into the console is not a test: The server expects JSON-RPC from a client and otherwise reports a parse error on stderr.

## Where things are

| Folder/file | Content |
|---|---|
| `src/`, `tests/` | Server (Host, Server, Core) and tests (Core, protocol, architecture) |
| `CLAUDE.md` | Rules for Claude: architecture, hard rules, commands (loaded automatically) |
| `.claude/commands/rebuild.md` | the `/rebuild` command |
| `rebuild/analysis.md` | Stack, domain, features, interfaces, tests, and legacy findings of the original |
| `rebuild/research.md` | Target stack research, decisions D1–D20 with sources, findings per slice |
| `rebuild/open-questions.md` | open questions with assumptions and decisions |
| `rebuild/PRD.md`, `rebuild/GLOSSARY.md` | desired behavior, improvements, terms |
| `rebuild/slices/` | one file per slice: goal, scope, prompt, Definition of Done |
| `rebuild/plans/` | the detailed plans from `/rebuild`, with deviations from the plan |
| `rebuild/parity/` | parity cases with exact values, deviation log |
| `rebuild/checklist.md` | progress per slice |
| `rebuild/slice-graph*` | dependency graph of the slices |
| `rebuild/_raw/`, `_inventory.md`, `_slices.json` | working material of the skill |

## Slice dependencies

![Slice dependencies](rebuild/slice-graph.png)

## Assumptions compared with the original

- `convert_time` has an optional `date`. Without `date`, the behavior stays as in the original ("today" in the source zone).
- Error texts are newly worded; the information stays the same (field, value, expected format).
- Weekdays stay in English, and the time difference stays a string (`+5.75h`).
