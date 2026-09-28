#!/usr/bin/env python3
"""Verification plugin: run the three NPOI test projects on net10.0."""

import sys
import time
from pathlib import Path


def main() -> None:
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
    from _npoi_dotnet import TEST_FRAMEWORK, TEST_PROJECTS, emit, load_context, require_sdk, run, skip_unless_dotnet_changed

    name = "dotnet-test"
    start = time.time()
    context = load_context()
    root = context["project_root"]

    skip_unless_dotnet_changed(name, context, start)
    dotnet = require_sdk(name, root, start)

    all_passed = True
    sections: list[str] = []
    for project in TEST_PROJECTS:
        passed, output = run([dotnet, "test", project, "-f", TEST_FRAMEWORK, "--nologo"], root, timeout=600)
        all_passed &= passed
        summary = [ln for ln in output.splitlines() if ln.startswith(("Passed!", "Failed!")) or " error " in ln]
        sections.append(f"=== {project} ({'ok' if passed else 'FAILED'}) ===\n" + "\n".join(summary or output.splitlines()[-15:]))
    emit(name, all_passed, "\n".join(sections), start, {"projects": TEST_PROJECTS, "framework": TEST_FRAMEWORK})


if __name__ == "__main__":
    main()
