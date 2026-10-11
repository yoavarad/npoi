#!/usr/bin/env python3
"""PR body gate: run `ydk verify pr-body` on gh pr create/edit bodies before they reach CI.

Mirrors .github/workflows/pr-body-check.yml (required check `check-body`). Fails open on
anything unexpected (no stdin, bad JSON, unparsable command, ydk missing or slow, any error).
"""
import json, os, re, shlex, shutil, subprocess, sys, tempfile, threading
from pathlib import Path

HEREDOC_RE = re.compile(r"^\$\(cat <<-?'?(\w+)'?\n(.*)\n\1\s*\)$", re.DOTALL)

# Same fail-open stdin read as guard.py (no POSIX-only readiness check on Windows).
# Decoded as UTF-8 explicitly: the cp1252 default chokes on emoji in PR bodies.
def _read_stdin(timeout=0.5):
    box = {}
    def _reader():
        try:
            box["data"] = sys.stdin.buffer.read().decode("utf-8", errors="replace")
        except Exception:
            box["data"] = ""
    t = threading.Thread(target=_reader, daemon=True)
    t.start()
    t.join(timeout)
    if t.is_alive():
        return None  # No data in time = allow
    return box.get("data", "")


def _skip(reason):
    sys.stderr.write(f"pr-body-gate: body not checked ({reason}).\n")
    sys.exit(0)


def check():
    """Return ydk's failure output, or None when the body passes or is not checked."""
    raw = _read_stdin(0.5)
    if raw is None:
        return None
    try:
        data = json.loads(raw)
    except (json.JSONDecodeError, ValueError):
        return None

    command = (data.get("tool_input") or {}).get("command", "")
    if data.get("tool_name") != "Bash" or not ("gh pr create" in command or "gh pr edit" in command):
        return None

    cwd = data.get("cwd") or "."
    try:
        args = shlex.split(command, posix=True)
    except ValueError:
        return None

    body = body_file = base = None
    has_body_flag = False
    for i, arg in enumerate(args):
        nxt = args[i + 1] if i + 1 < len(args) else None
        if arg in ("--body-file", "-F"):
            has_body_flag, body_file = True, nxt
        elif arg.startswith("--body-file="):
            has_body_flag, body_file = True, arg.split("=", 1)[1]
        elif arg in ("--body", "-b"):
            has_body_flag, body = True, nxt
        elif arg.startswith("--body="):
            has_body_flag, body = True, arg.split("=", 1)[1]
        elif arg in ("--base", "-B"):
            base = nxt
        elif arg.startswith("--base="):
            base = arg.split("=", 1)[1]

    if not has_body_flag:
        return None  # Nothing to check (ydk task done validates its own body)
    if body_file is not None:
        if body_file == "-":
            _skip("body read from stdin")
        body = (Path(cwd) / body_file).read_text(encoding="utf-8")
    if body is None:
        _skip("body flag without a value")
    match = HEREDOC_RE.match(body)
    if match:
        body = match.group(2)
    elif body.startswith("$("):
        _skip("body is a command substitution")

    ydk = shutil.which("ydk")
    if ydk is None:
        _skip("ydk not on PATH")

    fd, tmp = tempfile.mkstemp(suffix=".md")
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="") as f:
            f.write(body)
        try:
            result = subprocess.run(
                [ydk, "verify", "pr-body", "--body-file", tmp, "--base-ref", f"origin/{base or 'main'}"],
                cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120, check=False,
            )
        except subprocess.TimeoutExpired:
            _skip("ydk verify pr-body timed out")
    finally:
        os.unlink(tmp)
    return (result.stdout + result.stderr) if result.returncode != 0 else None


try:
    failure = check()
except Exception as exc:  # Fail open on anything unexpected
    sys.stderr.write(f"pr-body-gate: body not checked ({type(exc).__name__}: {exc}).\n")
    sys.exit(0)

if failure is not None:
    sys.stderr.write("BLOCKED: PR body fails pr-body-check (CI check-body). Fix and retry:\n" + failure)
    sys.exit(2)
sys.exit(0)
