# Project Conventions -- npoi (yoavarad fork)

Permanent fork of nissl-lab/npoi. Serves ole-extractor now (only `main/NPOI.Core.csproj`: HSSF, HPSF, POIFS); broader NPOI improvement later.

## Read first

- [AGENTS.md](AGENTS.md) -- codebase overview, directory map, code style, build/test commands.
- [docs/project-rules.md](docs/project-rules.md) -- project rules and known gotchas (SDK, formatting, upstream sync, verification plugins, graphify). Follow it; do not duplicate it here.

## YDK workflow

Follows the YDK lifecycle (01 brainstorming, 01.5 ignition, 02 task management, 03 execution, 04 learning). Work is tracked as `ydk task` items (`.ydk/`); each task runs in its own worktree under `.ydk/worktrees/` and ends with `ydk task done <id>`. Commits are conventional (`type(scope): desc`).

## Code exploration: use graphify

`graphify-out/` holds the code knowledge graph. For codebase questions run `graphify query "<question>"` first; use `graphify path "<A>" "<B>"` and `graphify explain "<concept>"` for relationships. Fall back to Read/Grep only if the graph lacks the answer. After code changes run `graphify update .`. Details: the Knowledge Graph section of [docs/project-rules.md](docs/project-rules.md).
