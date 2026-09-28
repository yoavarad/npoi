#!/usr/bin/env python3
"""Verification plugin: build solution/NPOI.Core.Test.sln (all libraries, tests, benchmarks)."""

import sys
import time
from pathlib import Path


def main() -> None:
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
    from _npoi_dotnet import SOLUTION, emit, load_context, require_sdk, run, skip_unless_dotnet_changed

    name = "dotnet-build"
    start = time.time()
    context = load_context()
    root = context["project_root"]

    skip_unless_dotnet_changed(name, context, start)
    dotnet = require_sdk(name, root, start)

    passed, output = run([dotnet, "build", SOLUTION, "--nologo", "-v", "q"], root, timeout=840)
    emit(name, passed, output, start, {"target": SOLUTION})


if __name__ == "__main__":
    main()
