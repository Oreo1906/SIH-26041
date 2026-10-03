"""Run backend/admin gates from any working directory; retain command output locally."""

import argparse
from datetime import datetime, timezone
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    # Vite emits Unicode progress markers; Windows terminals may default to cp1252.
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--component", choices=["all", "backend", "admin"], default="all")
    args = parser.parse_args()
    venv_python = ROOT / "backend" / ".venv" / ("Scripts/python.exe" if sys.platform == "win32" else "bin/python")
    python = str(venv_python) if venv_python.exists() else sys.executable
    npm = shutil.which("npm.cmd" if sys.platform == "win32" else "npm")
    output = ROOT / "artifacts" / "verification"
    output.mkdir(parents=True, exist_ok=True)
    steps = []
    if args.component in ("all", "backend"):
        steps.extend([
            ("backend-dependencies", "backend", [python, "-m", "pip", "check"]),
            ("backend-tests", "backend", [python, "-m", "pytest", "-q"]),
            ("backend-wheel", "backend", [python, "-m", "build", "--wheel", "--no-isolation"]),
        ])
    if args.component in ("all", "admin"):
        if not npm:
            print("Node/npm is required. See README.md.", file=sys.stderr)
            return 1
        steps.extend([
            ("admin-tests", "admin-web", [npm, "run", "test"]),
            ("admin-build", "admin-web", [npm, "run", "build"]),
        ])
    summary = [f"Backend/admin verification: {datetime.now(timezone.utc).isoformat()}"]
    failed = False
    for name, folder, command in steps:
        print(f"Running {name}...", flush=True)
        try:
            result = subprocess.run(command, cwd=ROOT / folder, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=300)
            content = result.stdout + result.stderr
            code = result.returncode
        except (OSError, subprocess.TimeoutExpired) as error:
            content, code = str(error), 1
        (output / f"{name}.log").write_text(content, encoding="utf-8")
        print(content, flush=True)
        summary.append(f"{'PASS' if code == 0 else 'FAIL'} {name}: {' '.join(command)}")
        failed = failed or code != 0
    summary.append("Unity/Android evidence is recorded separately in artifacts/unity and STATUS.md.")
    (output / f"summary-{args.component}.txt").write_text("\n".join(summary) + "\n", encoding="utf-8")
    print("\n".join(summary))
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
