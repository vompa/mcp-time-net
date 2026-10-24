# mcp-time-net – MCP-Server „time“ auf .NET 10

Kleiner lokaler MCP-Server (stdio) mit zwei rein lesenden Tools: `get_current_time` (aktuelle Zeit in einer IANA-Zeitzone) und `convert_time` (Ortszeit zwischen zwei Zeitzonen umrechnen, optional an einem bestimmten Datum).

## Herkunft

Dieses Repo ist ein Neubau des MCP-Referenzservers **„time“** auf .NET 10:

- **Original:** [`modelcontextprotocol/servers` → `src/time`](https://github.com/modelcontextprotocol/servers/tree/51b6382/src/time), Stand Commit `51b6382`, Python, MIT-Lizenz.
- **Kein Port, keine Kopie:** Aus dem Original wurden nur Verhalten, Schnittstellen und erwartete Werte übernommen (`rebuild/parity/`), kein Code. Der Rebuild selbst hat das Original nie gebaut, gestartet oder gelesen – alles, was er treffen muss, steht in `rebuild/`.

## Testfall für den Skill `vibe-rebuild`

Dieses Repo ist ein **direkter End-to-End-Test des Skills [`vibe-rebuild`](https://github.com/vompa/vibe-rebuild)** (Version 2.2.0) und dort als Referenz verlinkt: Der Skill hat aus dem Original den Plan in `rebuild/` erzeugt, und der Rebuild wurde danach ausschließlich mit dem vom Skill erzeugten Befehl `/rebuild` Slice für Slice umgesetzt. Die Git-Historie zeigt beide Phasen unverändert:

| Commit | Zeit (MESZ) | Phase |
|---|---|---|
| `591296e` | 23.10.2026 22:34 | Plan aus `vibe-rebuild` 2.2.0 (28 Dateien, kein Code) |
| `9979965` | 23.10.2026 23:37 | Slice 00 Scaffold |
| `4a23de6` | 24.10.2026 00:58 | Slice 01 Server-Kern & Protokoll |
| `92cde94` | 24.10.2026 04:54 | Slice 02 Zeitzonen & aktuelle Zeit |
| `9918428` | 24.10.2026 05:28 | Slice 03 Umrechnung |
| `32bc117` | 24.10.2026 06:13 | Slice 04 Start, lokale Zeitzone & Verteilung |
| `dfa7b1c` | 24.10.2026 06:18 | Nachtrag: Architekturtest „Server ohne NodaTime“ |

Ergebnis: alle Vergleichsfälle aus `rebuild/parity/` grün, 202 Tests (70 Testmethoden, davon 35 Theories) in drei Testprojekten grün (24.10.2026), 853 Zeilen Produktivcode, 17 dokumentierte Abweichungen in [`rebuild/parity/deviations.md`](rebuild/parity/deviations.md) – alle bewusst (Verbesserung oder Entscheidung), keine als Fehler im Rebuild.

## Ablauf mit dem Skill

### Phase 1 – Plan erzeugen (Skill `vibe-rebuild`)

Eine eigene Sitzung mit dem Skill, Eingabe: Original-Repo (nur lesend), Zielstack .NET 10, ganzer Umfang. Der Skill arbeitet in diesen Schritten; die Ergebnisse liegen in `rebuild/`:

| Schritt | Was der Skill tut | Ergebnis |
|---|---|---|
| 1. Eingaben | fragt Repo, Zielstack (mit konkreten Optionen), Umfang und ob das Original lokal läuft – in einer Runde | Annahmen mit `[Annahme]` markiert |
| 2. Inventar | `scripts/inventory.py` zählt Projekte, Dateien, Zeilen, Tests, vorhandene Doku | `_inventory.md` |
| 3. Analyse | Explorer-Subagenten je Bereich, Berichte als Arbeitsmaterial, Stichprobe harter Zahlen per `grep` | `analysis.md`, `_raw/` |
| 4. Offene Fragen | gruppiert nach architekturrelevant / fachlich / gestrichen, je mit Annahme | `open-questions.md` |
| 5. Recherche Zielstack | drei Durchgänge: BROAD (12 Themen), DEEP (Quellcode des MCP-C#-SDK, NodaTime, Packaging, Tests), DISTILL (Entscheidungen mit Quellen) | `research.md` mit D1–D20, `_raw/research-progress.md` |
| 6. PRD & Glossar | Verhalten mit Given/When/Then, bewusste Verbesserungen (optionales `date`), Begriffe | `PRD.md`, `GLOSSARY.md` |
| 7. Leitplanken | Zielarchitektur mit erzwungenen Abhängigkeitsrichtungen, harte Regeln, Befehle | `CLAUDE.md` |
| 8. Slicing | vertikale Slices nach Abhängigkeit, je mit Prompt und Definition of Done; Graph gerendert | `slices/00–04`, `_slices.json`, `slice-graph*` |
| 9. Parity | Vergleichsfälle aus den Tests des Originals, getrennt nach Business- und Schnittstellenvertrag, mit exakten Werten | `parity/business-cases.md`, `parity/interface-cases.md`, `parity/README.md` |
| 10. Übergabe | Einschätzung Neubau vs. Refactoring, Checkliste, Befehl `/rebuild` | Plan-README (heute diese Datei), `checklist.md`, `.claude/commands/rebuild.md` |

Die architekturrelevanten Fragen (Zeitzonen-Bibliothek, Verteilung) wurden vor dem Slicing entschieden: NodaTime (D3) und `dotnet tool` über `dnx` (D17).

Einschätzung des Skills damals: Das Original ist klein (310 Zeilen), sauber und gut getestet; ein Refactoring im selben Stack lohnt kaum. Der Neubau lohnt sich für das .NET-Ökosystem (`dotnet tool` über `dnx`) und bringt zwei Verbesserungen: optionales Datum bei der Umrechnung und plattformunabhängig gleiche Zeitzonendaten durch NodaTime.

### Phase 2 – Rebuild umsetzen (`/rebuild`, je Slice eine frische Sitzung)

Claude Code im Repo-Ordner starten und **`/rebuild`** eintippen. Der Befehl wählt die niedrigste offene Slice, deren Abhängigkeiten abgehakt sind, und arbeitet sie so ab (mit installiertem Plugin superpowers):

1. **Lesen:** Slice-Datei, `CLAUDE.md`, die genannten PRD-Abschnitte, Entscheidungen in `research.md`, Vergleichsfälle und Glossar.
2. **Worktree:** `superpowers:using-git-worktrees` legt `.claude/worktrees/slice-NN` mit Branch `slice-NN` an.
3. **Plan:** `superpowers:writing-plans` schreibt `rebuild/plans/<Datum>-slice-NN.md` mit Tasks, Annahmen („bei Freigabe bestätigen“) und Review-Fokus. **Hier stoppt Claude** und wartet auf die Freigabe.
4. **Umsetzen:** `superpowers:subagent-driven-development` – ein Subagent je Task, Tests zuerst (rot), dann minimal grün; nach jedem Task ein Review-Subagent, am Ende ein Gesamtreview. Jeder Task committet auf `slice-NN` (für Review-Pakete und Wiederanlauf).
5. **Abschluss:** volle Testsuite, Definition of Done, Abweichungen in `rebuild/parity/deviations.md`, Haken in `rebuild/checklist.md`, Abweichungen vom Plan oben in der Plan-Datei.
6. **Integration:** `superpowers:finishing-a-development-branch` – nach Rückfrage `git merge --squash` als **ein Commit pro Slice** auf `main`, danach Worktree und Branch weg.
7. Claude endet mit drei Zeilen („Slice NN done“, „Ready next: …“, „Next step: start a fresh session and type /rebuild“). Für die nächste Slice eine neue Sitzung.

Ohne superpowers (oder mit `/rebuild NN direct`) läuft dasselbe ohne Worktree und Subagenten: Branch `slice-NN`, Plan vorlegen, rote Tests, Code, ein Commit.

| Eingabe | Was passiert |
|---|---|
| `/rebuild` | nächste offene Slice, deren Abhängigkeiten erledigt sind |
| `/rebuild 03` | genau Slice 03; fehlt eine Abhängigkeit, fragt Claude nach |
| `/rebuild status` | Übersicht: erledigt, in Arbeit, startbereit, blockiert |
| `/rebuild 03 direct` | ohne superpowers, auch wenn es installiert ist |

Nach Slice 02 waren 03 und 04 unabhängig und hätten parallel in zwei Sitzungen laufen können (`/rebuild 03`, `/rebuild 04`); in diesem Test liefen sie nacheinander.

### Was der Mensch in diesem Test entschieden hat

Freigaben und Entscheidungen, die nicht im Plan standen, sind in den Plan-Dateien und in `deviations.md` festgehalten, unter anderem:

- **Kein Verify** für Snapshots (ab v33 Sponsoring-Erklärung im Build nötig) → eigener JSON-Vergleich, D14 angepasst (Slice 00).
- **Negative Sommerzeit** (Europe/Dublin) folgt der tzdb: Winter `is_dst: true` (Slice 02).
- **Bereich für `date`** `0001-01-02` bis `9999-12-29`, damit keine Überläufe entstehen (Slice 03, aus einem Task-Review).
- **Framework-abhängiges `dotnet tool`** statt self-contained, CLI-Meldungen lokalisiert, `--version` neu (Slice 04).

Lücken, die der Rebuild selbst schließen musste, stehen als Befunde in `rebuild/research.md` (Abschnitte je Slice) und als „Abweichungen vom Plan“ oben in den Plan-Dateien.

## Bauen und testen

Voraussetzungen: .NET 10 SDK (`dotnet --version` 10.x); optional Node.js für den MCP Inspector.

```bash
dotnet build -warnaserror
dotnet test                                  # alle Tests (Core, Protokoll, Architektur)
dotnet run --project src/McpTime.Host -- --local-timezone Europe/Vienna
dotnet pack src/McpTime.Host -c Release -o artifacts/package      # dotnet tool (D17)
dotnet dnx Vompa.McpTime --source artifacts/package --yes -- --help   # aus dem Paket starten (`--` nötig für --help)
npx @modelcontextprotocol/inspector --cli dotnet run --project src/McpTime.Host --method tools/list
dotnet format --verify-no-changes
```

## Veröffentlichen auf nuget.org

`.github/workflows/release.yml` veröffentlicht `Vompa.McpTime` per Trusted Publishing (nuget.org-Policy `mcp-time-net-release`, ohne API-Key). Auslöser ist ein Tag `vX.Y.Z`; die Version muss in `Directory.Build.props` und in `src/McpTime.Host/.mcp/server.json` (beide Felder) gleich sein, sonst bricht der Workflow ab.

```bash
git tag v0.1.0
git push origin v0.1.0
```

Danach im Client: `{ "command": "dotnet", "args": ["dnx", "Vompa.McpTime", "--yes"] }` (unter Windows `dotnet` mit `dnx` als Argument, weil es nur `dnx.cmd` gibt).

Testaufruf gegen das veröffentlichte Paket (braucht Node.js; holt `Vompa.McpTime` von nuget.org und ruft `get_current_time` auf):

```bash
npx -y @modelcontextprotocol/inspector --cli dotnet dnx Vompa.McpTime --yes --method tools/call --tool-name get_current_time --tool-arg timezone=Europe/Vienna
```

Erwartet: `isError: false` und unter `structuredContent` die aktuelle Zeit in Wien mit `timezone`, `datetime`, `day_of_week` und `is_dst`. Eingetippter Text in der Konsole ist kein Test: Der Server erwartet JSON-RPC von einem Client und meldet sonst einen Parse-Fehler auf stderr.

## Wo steht was

| Ordner/Datei | Inhalt |
|---|---|
| `src/`, `tests/` | Server (Host, Server, Core) und Tests (Core, Protokoll, Architektur) |
| `CLAUDE.md` | Regeln für Claude: Architektur, harte Regeln, Befehle (wird automatisch geladen) |
| `.claude/commands/rebuild.md` | der Befehl `/rebuild` |
| `rebuild/analysis.md` | Stack, Domain, Features, Schnittstellen, Tests, Legacy-Befunde des Originals |
| `rebuild/research.md` | Zielstack-Recherche, Entscheidungen D1–D20 mit Quellen, Befunde je Slice |
| `rebuild/open-questions.md` | offene Fragen mit Annahmen und Entscheidungen |
| `rebuild/PRD.md`, `rebuild/GLOSSARY.md` | gewünschtes Verhalten, Verbesserungen, Begriffe |
| `rebuild/slices/` | eine Datei je Slice: Ziel, Umfang, Prompt, Definition of Done |
| `rebuild/plans/` | die Detailpläne aus `/rebuild`, mit Abweichungen vom Plan |
| `rebuild/parity/` | Vergleichsfälle mit exakten Werten, Abweichungsprotokoll |
| `rebuild/checklist.md` | Fortschritt je Slice |
| `rebuild/slice-graph*` | Abhängigkeitsgraph der Slices |
| `rebuild/_raw/`, `_inventory.md`, `_slices.json` | Arbeitsmaterial des Skills |

## Slice-Abhängigkeiten

![Slice-Abhängigkeiten](rebuild/slice-graph.png)

## Annahmen gegenüber dem Original

- `convert_time` hat ein optionales `date`. Ohne `date` bleibt das Verhalten wie beim Original („heute“ in der Quellzone).
- Fehlertexte sind neu formuliert, die Information bleibt gleich (Feld, Wert, erwartetes Format).
- Wochentage bleiben englisch, die Zeitdifferenz bleibt ein String (`+5.75h`).
