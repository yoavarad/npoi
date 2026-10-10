# Project Rules

Conventions, preferences, and domain knowledge.

## Product Context

`yoavarad/npoi` is a permanent fork of [nissl-lab/npoi](https://github.com/nissl-lab/npoi) (a C# port of Apache POI).

- **Current goal:** serve [ole-extractor](https://github.com/yoavarad/ole-extractor), which consumes this repo as the submodule `third_party/npoi` and references only `main/NPOI.Core.csproj` (HSSF `.xls` bodies, HPSF/POIFS legacy metadata).
- **Later goal:** improve this repo broadly. Changes are not constrained by upstream acceptance.

## Known Gotchas

- **ole-extractor may only pin commits reachable from `main` or a tag**, never a feature branch -- a deleted branch breaks every fresh clone of ole-extractor.
- **Building needs the .NET 10 SDK** (`global.json` pins 10.0.100, `rollForward: latestFeature`). `Directory.Build.props` multi-targets `net472;netstandard2.0;netstandard2.1;net8.0;net10.0` and sets `TreatWarningsAsErrors`; with only SDK 8/9 installed, builds fail with `NETSDK1045`. The Desktop Runtime alone is not enough -- install the SDK.
- **Timings on SDK 10.0.401 (this machine, 2026-09-28):** cold `dotnet build solution/NPOI.Core.Test.sln` ~1m40s; `dotnet test --no-build` of the whole solution (net472 + net10.0) ~2m; per project on net10.0: NPOI.TestCases ~41s, OOXML.TestCases ~1m, OpenXml4Net.TestCases ~3s. Size verification plugin timeouts from these.
- **`Microsoft.SourceLink.GitHub` must be >= 10.0.303** (pinned 10.0.401 in `Directory.Packages.props`). 8.0.0 drags in `Microsoft.Build.Tasks.Git` 8.0.0, which has advisory GHSA-23fw-v26w-5fgq (CVE-2026-62900, no 8.x fix); with `TreatWarningsAsErrors` the NU1902 audit warning fails restore for every project.
- **One OOXML test is flaky when both TFMs run in parallel** (`dotnet test` of the whole solution failed 1 test on net472 and net10.0; rerunning net10.0 alone passed 1864/1864). Not yet identified -- suspect shared temp files.
- **Verification plugins are project-local** (`.ydk/verifications/`, shared helper `_npoi_dotnet.py`), overriding ydk's stock `dotnet-*` ones, which built the alphabetically-first `.sln` (`benchmarks/NPOI.Benchmarks`). Pre-push runs, sequentially: `dotnet-build` (`solution/NPOI.Dev.slnf` -- 4 libraries + 3 test projects, no Benchmarks/_build/Pack -- with `-p:NpoiDevTfm=net10.0`; the only step that restores), `dotnet-format` (`--no-restore`, scoped to the projects owning changed `.cs` files; whole slnf if `.editorconfig` changed or >200 files), `dotnet-test` (one `dotnet test` of the slnf with `--no-build --no-restore` and the same `NpoiDevTfm`, so the three test projects run in parallel). All skip when the branch touches no `.cs/.csproj/.sln/.props/.targets/global.json/.editorconfig`, and fail loudly (not skip) if the SDK is missing. `dotnet-quality` is disabled (it only repeated build + format). Run time: ~5 minutes for code changes before task #89; after it the measured cold run is recorded in PR #89 (build ~4m cold restore, warm much less; test ~2.5m, down from ~3 sequential runs).
- **`dotnet-format` checks every `.cs` file the branch adds or modifies.** The whole repo was reformatted to `.editorconfig` in one commit (task #46), so any changed file must stay formatted: CRLF line endings and **no** final newline (one exception, see the overrides below). Run `dotnet format whitespace` and `dotnet format style` on `solution/NPOI.Core.Test.sln --include <files>` before pushing. When `.editorconfig` changes, or above 200 changed `.cs` files (Windows command-line length), the plugin checks the whole solution instead.
- **`git blame` skips the reformat commit** listed in `.git-blame-ignore-revs`. Enable it once per clone: `git config blame.ignoreRevsFile .git-blame-ignore-revs` (GitHub's blame view uses the file automatically). Merge the PR that adds a listed commit with a merge commit, not squash/rebase, or the listed hash will not exist on `main`. After merging, check `git cat-file -t <hash>` on `main` and fix the entry if it fails.
- **`.editorconfig`'s `charset = utf-8bom` is not a valid value** (the valid one is `utf-8-bom`), so `dotnet format` writes plain UTF-8 and drops the BOM from files it rewrites. Mixed BOM/no-BOM files are expected; the formatter does not flag either.
- **Two `.editorconfig` overrides keep `dotnet format --verify-no-changes` clean:** CA2022 is off under `testcases/` (its fix rewrites `Stream.Read` to `ReadExactly`, which changes what the tests check and does not exist on net472), and `benchmarks/NPOI.Benchmarks/Program.cs` keeps its final newline (on a top-level-statement file the whitespace and final-newline formatters otherwise fight). Never run `dotnet format analyzers` fixes blindly: they are code changes, not formatting. The plugin's plain `dotnet format --verify-no-changes` still checks analyzers, so if it reports one, fix that code by hand (as a reviewed code change) or, if the fix would change behaviour, turn the rule off for that path in `.editorconfig` with a comment explaining why.
- **Branch and commit conventions are enforced in CI** (`.github/workflows/conventions.yml`): branch must match `^(feat|fix|docs|chore|refactor|test|ci|perf|release|task)/[a-z0-9-]+$` (ydk's `task/NN-...` passes), and `cz check` validates every PR commit. Upstream-sync PRs must use a branch starting `chore/upstream-sync-` -- the commit check skips those (nissl-lab commits are not conventional). Squash-merge titles on main ("Task 39: ...") are not checked.
- **Upstream sync method (nissl-lab/npoi):** no `upstream` remote is configured yet; add it with `git remote add upstream https://github.com/nissl-lab/npoi.git`. Because of the reformat, a plain merge conflicts on almost every file upstream touched. To sync: (1) `git fetch upstream`; (2) branch from the upstream commit (`git switch -c upstream-formatted upstream/master`; upstream's default branch is still `master`), copy the fork's `.editorconfig` over upstream's (`git checkout main -- .editorconfig`), run `dotnet format whitespace` then `dotnet format style` on `solution/NPOI.Core.Test.sln` (not plain `dotnet format`, which also applies analyzer code fixes), and commit that as a formatting-only commit; (3) merge `upstream-formatted` into a branch off `main`. Conflicts then show only real code changes on both sides; if nearly every file still conflicts, the two formatter runs disagreed (for example a different SDK version), so stop and compare them first. Resolve anything left by hand; do not use `-X ours/theirs`, which silently drops one side's code changes. Build and run the full suite before merging, and when ole-extractor re-pins `third_party/npoi` afterwards, verify its build and tests too.
- **`dotnet-tdd-guard` (pre-commit) uses NPOI's layout:** a branch that changes production `.cs` under `main/`, `ooxml/` or `openxml4Net/` must also change a `.cs` under `testcases/`. `OpenXmlFormats/` is exempt (data-only). The stock guard's `<Stem>Tests.cs`-in-a-`Tests`-folder rule can never match NPOI.
- **ydk quirks the plugins work around:** ydk derives `changed_files` with a hardcoded `git diff main...HEAD` (empty while the default branch was `master`; now `main`, so this may no longer bite), so plugins compute changed files themselves from the merge-base; and ydk's verification result cache hashes only `*.py` files, so a cached pass can be stale for C# changes -- use `ydk verify run --no-cache` when in doubt (task #33).
- **`.ydk/proofs/` is gitignored** (decision, task #27): proofs are per-run `ydk task done` output, machine-local and noisy; the PR body and CI are the durable record. (ole-extractor commits them; this fork does not.)
- **Guard hook uses a repo-relative command** (`python .claude/hooks/guard.py` in `.claude/settings.json`): `guard.py` is stdlib-only, so any `python` on PATH works from the repo root or any worktree. No absolute user paths in tracked settings.
- **`.ydk/state.json` stage is `03`** (implementation; the stage-gate only blocks source edits in stages 01/02).

## Knowledge Graph (graphify)

- `graphify-out/` holds the code graph. Query it with `graphify query "<question>"`, `graphify path "A" "B"`, `graphify explain "X"`.
- **Refresh after code changes:** run `graphify update .` from the repo root (AST-only, no API cost, ~4 min cold). Scope is set by `.graphifyignore` (excludes `bin/`, `obj/`, `testcases/`, `benchmarks/`, `scratchpad/`, `.ydk/`).
- **`graph.json` (~68MB), `GRAPH_REPORT.md`, `graph.html`, `manifest.json` are committed on purpose** so exploration works on a fresh clone. `graphify-out/cache/`, dated snapshot dirs and `graphify-out/*.graphify_*` are gitignored. Expect large diffs when re-committing the graph; do it only occasionally.
- `.claude/settings.json` has graphify `PreToolUse` hooks (`hook-guard search` / `read --strict`) that steer searches to the graph.

## Branch Protection (main)

- Applied once via the API (task #97); not stored in the repo. Required status checks (exact check-run names, all run on every PR, no path filters): `ubuntu-latest`, `windows-latest` (CI.yml), `format (windows-latest)` (Format.yml), `branch-name`, `commit-messages` (conventions.yml), `check-body` (pr-body-check.yml).
- Settings: `strict: false` (branch need not be up to date; avoids forced branch updates + CI reruns after every merge), `enforce_admins: false` (owner can bypass in emergencies), force-push and deletion blocked, no required reviews (solo maintainer). Merge commits stay allowed (needed for `.git-blame-ignore-revs` reformat commits); do not change repo merge-method settings.
- Command: `gh api -X PUT repos/yoavarad/npoi/branches/main/protection --input protection.json`, body:

```json
{
  "required_status_checks": {
    "strict": false,
    "contexts": ["ubuntu-latest", "windows-latest", "format (windows-latest)", "branch-name", "commit-messages", "check-body"]
  },
  "enforce_admins": false,
  "required_pull_request_reviews": null,
  "restrictions": null,
  "allow_force_pushes": false,
  "allow_deletions": false
}
```

- Change only this flag: `gh api -X PATCH repos/yoavarad/npoi/branches/main/protection/required_status_checks -F strict=false`.
- Verify: `gh api repos/yoavarad/npoi/branches/main/protection`.
- **Every required check must run on `synchronize`:** with `strict: true`, each "Update branch" creates a new head commit. A required workflow that only triggers on `opened`/`edited` (as `pr-body-check.yml` once did) never reports on that commit and the PR stays BLOCKED.
- **Renaming a workflow job or matrix leg:** the old name becomes a required check that never reports, blocking every PR. Get the new name from `gh pr checks <n>`, then re-run the PUT above with the updated `contexts` (the PUT replaces the full list) before or right after merging the rename.

## Benchmarks

- **Run from the csproj, not the folder:** `benchmarks/NPOI.Benchmarks` holds both a `.csproj` and a `.sln`; a bare `dotnet build`/`dotnet run` there picks the `.sln`, which maps the library projects to Debug, and BenchmarkDotNet then aborts on "non-optimized" dependencies. Use `dotnet build benchmarks/NPOI.Benchmarks/NPOI.Benchmarks.csproj -c Release` then `dotnet run --project benchmarks/NPOI.Benchmarks/NPOI.Benchmarks.csproj -c Release --no-build -- --filter '*ReadFromBytesBenchmark*' --job short`. Delete `benchmarks/NPOI.Benchmarks/bin` first if an earlier sln build left Debug DLLs there.
- **Read-path baseline:** `ReadFromBytesBenchmark` (POIFS open/walk, HPSF, HSSF open, cell iteration, pictures, embedded objects; MemoryDiagnoser) has committed results in [docs/benchmarks/read-path-baseline.md](benchmarks/read-path-baseline.md). Re-run and compare allocated bytes and GC counts when changing the read path.
