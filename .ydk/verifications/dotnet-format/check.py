#!/usr/bin/env python3
"""Verification plugin: dotnet format --verify-no-changes on .cs files ADDED by this branch.

NPOI is a permanent fork that keeps merging from upstream nissl-lab/npoi, whose existing
files carry years of style drift (final newlines, import order, whitespace). Reformatting
every touched file would turn small fixes into whole-file diffs and merge conflicts, so
only brand-new files must satisfy .editorconfig; edits to existing files keep upstream style.
"""

import sys
import time
from pathlib import Path


def main() -> None:
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
    from _npoi_dotnet import SOLUTION, added_files, emit, load_context, require_sdk, run, skip_unless_dotnet_changed

    name = "dotnet-format"
    start = time.time()
    context = load_context()
    root = context["project_root"]

    skip_unless_dotnet_changed(name, context, start)
    files = added_files(context)
    if files is None:
        emit(name, False, "Could not determine changed files (no base branch found); refusing to format the whole repo.", start)
    cs_files = [f for f in files if f.lower().endswith(".cs") and (Path(root) / f).is_file()]
    if not cs_files:
        emit(name, True, "No newly added .cs files -- skipped (existing files keep upstream style)", start)

    dotnet = require_sdk(name, root, start)
    passed, output = run([dotnet, "format", SOLUTION, "--verify-no-changes", "--include", *cs_files], root, timeout=840)
    emit(name, passed, output, start, {"target": SOLUTION, "files": cs_files})


if __name__ == "__main__":
    main()
