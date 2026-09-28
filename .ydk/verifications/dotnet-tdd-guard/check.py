#!/usr/bin/env python3
"""Verification plugin: production C# changes must come with a test change.

The stock guard wants `<Stem>Tests.cs` inside a `Tests`/`*.Tests` folder. NPOI's tests
live under testcases/ and are named `Test<Stem>.cs`, so the stock rule can never pass.
Here: if the branch touches production code (main/, ooxml/, openxml4Net/), it must also
touch a .cs file under testcases/. OpenXmlFormats/ is data-only schema mapping and exempt.
"""

import sys
import time
from pathlib import Path

PRODUCTION_DIRS = ("main/", "ooxml/", "openxml4Net/")
TEST_DIR = "testcases/"


def main() -> None:
    sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
    from _npoi_dotnet import changed_files, emit, load_context

    name = "dotnet-tdd-guard"
    start = time.time()
    context = load_context()

    staged = context.get("config", {}).get("staged_files", []) or []
    files = {f.replace("\\", "/") for f in staged} | {f.replace("\\", "/") for f in (changed_files(context) or [])}

    production = sorted(f for f in files if f.endswith(".cs") and f.startswith(PRODUCTION_DIRS))
    tests = sorted(f for f in files if f.endswith(".cs") and f.startswith(TEST_DIR))

    if not production:
        emit(name, True, "No production .cs changes -- nothing to check", start)
    if tests:
        emit(name, True, f"{len(production)} production file(s) changed alongside {len(tests)} test file(s)", start)
    listing = "\n".join(f"  {f}" for f in production[:20])
    emit(name, False, f"Production code changed but no .cs file under {TEST_DIR} did:\n{listing}", start, {"production": production})


if __name__ == "__main__":
    main()
