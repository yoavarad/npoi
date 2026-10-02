#!/usr/bin/env python3
"""Verification plugin: dotnet format --verify-no-changes on every .cs file CHANGED by this branch.

The whole repo was reformatted to .editorconfig in one commit (task #46, listed in
.git-blame-ignore-revs), so every added or modified .cs file must stay formatted.
"""

import sys
import time
from pathlib import Path


def main() -> None:
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
    from _npoi_dotnet import SOLUTION, emit, load_context, owning_project, require_sdk, run, slnf_projects, skip_unless_dotnet_changed

    name = "dotnet-format"
    start = time.time()
    context = load_context()
    root = context["project_root"]

    files = skip_unless_dotnet_changed(name, context, start)
    if files is None:
        emit(name, False, "Could not determine changed files (no base branch found); refusing to format the whole repo.", start)
    cs_files = [f for f in files if f.lower().endswith(".cs") and (Path(root) / f).is_file()]
    rules_changed = any(Path(f).name.lower() == ".editorconfig" for f in files)
    if not cs_files and not rules_changed:
        emit(name, True, "No changed .cs files -- skipped", start)

    dotnet = require_sdk(name, root, start)
    # A rule change can affect any file, and past ~200 paths the --include list risks
    # Windows' 32K command-line limit, so check the whole solution in both cases.
    whole = rules_changed or len(cs_files) > 200
    # --no-restore: dotnet-build (runs first) already restored. Scope to projects owning changed files.
    projects = {owning_project(root, f) for f in cs_files}
    # Owners outside the slnf (benchmarks, _build) were never restored: fall back to the slnf.
    if whole or not projects <= slnf_projects(root):
        runs = [([SOLUTION], [])]
    else:
        runs = [([p], ["--include", *[f for f in cs_files if owning_project(root, f) == p]]) for p in sorted(projects)]
    # Do NOT set NpoiDevTfm here: with one TFM the #if-gated code is analysed differently and
    # reports false WHITESPACE errors (e.g. HSSFCell.cs). Unrestored TFMs only log warnings.
    passed, output = True, ""
    for target, include in runs:
        ok, out = run([dotnet, "format", *target, "--no-restore", "--verify-no-changes", *include], root, timeout=840)
        passed &= ok
        output += out
    emit(name, passed, output, start, {"targets": [r[0][0] for r in runs], "files": "all" if whole else cs_files})


if __name__ == "__main__":
    main()
