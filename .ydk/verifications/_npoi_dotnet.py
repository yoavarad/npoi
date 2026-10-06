"""Shared helpers for the npoi dotnet verification plugins.

Lives beside the plugin folders (ydk only treats folders holding manifest.yaml +
check.py as plugins), so each check.py can `from _npoi_dotnet import ...`.

Why these exist instead of ydk's stock dotnet-* plugins: the stock ones build the
alphabetically-first .sln (benchmarks/NPOI.Benchmarks), use 2-minute timeouts, and
ydk's own `changed_files` is derived from a hardcoded `git diff main...HEAD`, which
is empty on a master-based repo. See docs/project-rules.md.
"""

import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

# Slim solution filter: 4 libraries + 3 test projects (no Benchmarks, _build, Pack).
SOLUTION = "solution/NPOI.Dev.slnf"
TEST_PROJECTS = [
    "testcases/main/NPOI.TestCases.Core.csproj",
    "testcases/ooxml/NPOI.OOXML.TestCases.Core.csproj",
    "testcases/openxml4net/NPOI.OOXML4Net.TestCases.Core.csproj",
]
TEST_FRAMEWORK = "net10.0"
# Single-TFM dev switch (Directory.Build.targets). Build and test must pass the same value.
DEV_TFM_ARG = f"-p:NpoiDevTfm={TEST_FRAMEWORK}"

_DOTNET_SUFFIXES = (".cs", ".csproj", ".sln", ".props", ".targets")
_DOTNET_NAMES = {"global.json", ".editorconfig", "nuget.config"}
_BASE_CANDIDATES = ("origin/main", "origin/master", "main", "master")
_MAX_OUTPUT_CHARS = 8000


def _git(root: str, *args: str) -> str | None:
    result = subprocess.run(
        ["git", *args], cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace", check=False
    )
    return result.stdout if result.returncode == 0 else None


def _derive_changed(root: str, diff_filter: str | None) -> list[str] | None:
    files: set[str] = set()
    for candidate in _BASE_CANDIDATES:
        base = _git(root, "merge-base", "HEAD", candidate)
        if base and base.strip():
            args = ["diff", "--name-only"] + ([f"--diff-filter={diff_filter}"] if diff_filter else []) + [base.strip()]
            files.update((_git(root, *args) or "").split())  # committed + uncommitted, tracked
            break
    else:
        return None
    files.update((_git(root, "ls-files", "--others", "--exclude-standard") or "").split())
    return sorted(files)


def changed_files(context: dict) -> list[str] | None:
    """Files changed on this branch (plus uncommitted work), or None if unknown.

    Trusts ydk's `changed_files` when it is a list; otherwise derives it from the
    merge-base with the first existing base branch. None means "run everything".
    """
    provided = context.get("changed_files")
    if isinstance(provided, list) and provided:
        return provided
    return _derive_changed(context["project_root"], None)


def added_files(context: dict) -> list[str] | None:
    """Files newly added on this branch (ydk's changed_files carries no add/modify status)."""
    return _derive_changed(context["project_root"], "A")


def owning_project(root: str, rel_file: str) -> str | None:
    """Nearest ancestor .csproj of a repo-relative file, as a repo-relative posix path."""
    base = Path(root)
    for parent in Path(rel_file).parents:
        found = sorted((base / parent).glob("*.csproj")) if (base / parent).is_dir() else []
        if found:
            return found[0].relative_to(base).as_posix()
    return None


def slnf_projects(root: str) -> set[str]:
    """Repo-relative posix paths of the projects listed in SOLUTION (the slnf)."""
    data = json.loads((Path(root) / SOLUTION).read_text(encoding="utf-8"))
    base = (Path(root) / SOLUTION).parent
    return {(base / p.replace("\\", "/")).resolve().relative_to(Path(root).resolve()).as_posix() for p in data["solution"]["projects"]}


def is_dotnet_relevant(files: list[str]) -> bool:
    return any(f.lower().endswith(_DOTNET_SUFFIXES) or Path(f).name.lower() in _DOTNET_NAMES for f in files)


def load_context() -> dict:
    return json.loads(sys.stdin.read())


def emit(name: str, passed: bool, output: str, start: float, detail: dict | None = None) -> None:
    text = output.strip()
    if len(text) > _MAX_OUTPUT_CHARS:
        text = "... (truncated)\n" + text[-_MAX_OUTPUT_CHARS:]
    json.dump(
        {
            "name": name,
            "passed": passed,
            "output": text,
            "duration_seconds": round(time.time() - start, 1),
            "detail": detail,
        },
        sys.stdout,
    )
    sys.exit(0 if passed else 1)


def skip_unless_dotnet_changed(name: str, context: dict, start: float) -> list[str] | None:
    """Emit a passing 'skipped' result if the branch touches no dotnet inputs.

    Returns the changed-file list (None when it could not be determined).
    """
    files = changed_files(context)
    if files is not None and not is_dotnet_relevant(files):
        emit(name, True, "No .cs/.csproj/.sln/.props/.targets/global.json changes -- skipped", start)
    return files


def require_sdk(name: str, root: str, start: float) -> str:
    """Return the dotnet executable, or fail loudly (a silent pass hid the missing SDK 10)."""
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        emit(name, False, "dotnet not found on PATH. Install the .NET 10 SDK (see global.json).", start)
    probe = subprocess.run([dotnet, "--version"], cwd=root, capture_output=True, text=True, check=False)
    if probe.returncode != 0:
        emit(name, False, f"No usable .NET SDK for global.json:\n{probe.stdout}{probe.stderr}", start)
    return dotnet


def run(cmd: list[str], root: str, timeout: int, env: dict | None = None) -> tuple[bool, str]:
    try:
        result = subprocess.run(
            cmd, cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=timeout, check=False, env=env
        )
    except subprocess.TimeoutExpired:
        return False, f"Timed out after {timeout}s: {' '.join(cmd[:3])} ..."
    output = result.stdout + ("\n" + result.stderr if result.stderr.strip() else "")
    return result.returncode == 0, output
