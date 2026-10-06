#!/usr/bin/env python3
"""Verification plugin: run the three NPOI test projects on net10.0 in one no-build invocation."""

import sys
import time
from pathlib import Path


def main() -> None:
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
    from _npoi_dotnet import DEV_TFM_ARG, SOLUTION, TEST_FRAMEWORK, emit, load_context, require_sdk, run, skip_unless_dotnet_changed

    name = "dotnet-test"
    start = time.time()
    context = load_context()
    root = context["project_root"]

    skip_unless_dotnet_changed(name, context, start)
    dotnet = require_sdk(name, root, start)

    # One invocation over the slnf: MSBuild runs the three test projects in parallel.
    # --no-build/--no-restore rely on dotnet-build (runs first) with the same NpoiDevTfm.
    passed, output = run([dotnet, "test", SOLUTION, DEV_TFM_ARG, "--no-build", "--no-restore", "--nologo"], root, timeout=600)
    summary = [ln for ln in output.splitlines() if ln.startswith(("Passed!", "Failed!")) or " error " in ln]
    text = chr(10).join(summary or output.splitlines()[-15:])
    emit(name, passed, text, start, {"solution": SOLUTION, "framework": TEST_FRAMEWORK})


if __name__ == "__main__":
    main()
