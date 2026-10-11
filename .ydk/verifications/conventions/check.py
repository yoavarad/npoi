#!/usr/bin/env python3
"""Verification plugin: local mirror of .github/workflows/conventions.yml.

branch-name: branch must match BRANCH_RE (detached HEAD is skipped).
commit-messages: every commit since the merge-base must pass `cz check` (commitizen 4.19
conventional_commits). cz rejects `release:` and `task:` although ydk's commit-msg hook
accepts them. Upstream-sync branches are skipped, as in CI.
"""

import re
import subprocess
import sys
import time
from pathlib import Path

BRANCH_RE = re.compile(r"^(feat|fix|docs|chore|refactor|test|ci|perf|release|task)/[a-z0-9-]+$")
BRANCH_RULE = (
    "must match type/description (types: feat|fix|docs|chore|refactor|test|ci|perf|release|task; "
    "lowercase letters, digits, hyphens)"
)
COMMIT_RE = re.compile(r"^(build|bump|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\(\S+\))?!?:\s[^\n\r]+")
CZ_ALLOWED_PREFIXES = ("Merge", "Revert", "Pull request", "fixup!", "squash!", "amend!")
BASE_CANDIDATES = ("origin/main", "main")
SKIP_COMMITS_PREFIX = "chore/upstream-sync-"


def _git(root: str, *args: str) -> str | None:
    result = subprocess.run(
        ["git", *args], cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace", check=False
    )
    return result.stdout if result.returncode == 0 else None


def _base(root: str) -> str | None:
    """Newest merge-base of HEAD with origin/main and main (a stale origin/main must not
    drag already-merged main commits into the range)."""
    bases = [b.strip() for b in (_git(root, "merge-base", "HEAD", c) for c in BASE_CANDIDATES) if b and b.strip()]
    if not bases:
        return None
    newest = bases[0]
    for other in bases[1:]:
        if _git(root, "merge-base", "--is-ancestor", newest, other) is not None:
            newest = other
    return newest


def main() -> None:
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
    from _npoi_dotnet import emit, load_context

    name = "conventions"
    start = time.time()
    root = load_context()["project_root"]
    problems: list[str] = []
    notes: list[str] = []

    branch = (_git(root, "rev-parse", "--abbrev-ref", "HEAD") or "HEAD").strip()
    if branch == "HEAD":
        notes.append("detached HEAD, branch check skipped")
    elif BRANCH_RE.match(branch):
        notes.append(f"branch '{branch}' ok")
    else:
        problems.append(f"Branch '{branch}' {BRANCH_RULE}.")

    if branch.startswith(SKIP_COMMITS_PREFIX):
        notes.append("upstream-sync branch, commit check skipped")
    else:
        base = _base(root)
        if base is None:
            notes.append("no base found, skipped commit check")
        else:
            lines = [l for l in (_git(root, "log", "--format=%H %s", f"{base}..HEAD") or "").splitlines() if l.strip()]
            bad = []
            for line in lines:
                sha, _, subject = line.partition(" ")
                if not (subject.startswith(CZ_ALLOWED_PREFIXES) or COMMIT_RE.match(subject)):
                    bad.append(f"  {sha[:9]} {subject}")
            if bad:
                problems.append(
                    "Commit subjects fail `cz check` (conventional_commits; note `release:`/`task:` are rejected):\n"
                    + "\n".join(bad)
                    + "\nFix with `git rebase -i <base>` (reword) or `git commit --amend` for the last commit."
                )
            else:
                notes.append(f"{len(lines)} commit(s) ok")

    if problems:
        emit(name, False, "\n".join(problems), start)
    emit(name, True, "; ".join(notes), start)


if __name__ == "__main__":
    main()
