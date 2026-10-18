#!/usr/bin/env python3
"""Check CI activation inputs without printing credential contents."""
import os
import sys

missing = [name for name in ("UNITY_EMAIL", "UNITY_PASSWORD") if not os.environ.get(name)]
if not (os.environ.get("UNITY_SERIAL") or os.environ.get("UNITY_LICENSE")):
    missing.append("UNITY_SERIAL or UNITY_LICENSE")
if missing:
    message = "Unity activation is not configured: " + ", ".join(missing)
    print("::error title=Unity license setup required::" + message)
    print("Add the applicable Unity activation credentials in Repository Settings > Secrets and variables > Actions.")
    print("Use a license valid for this Unity version and CI environment; credentials alone do not establish entitlement.")
    print("Do not paste credentials into issues, logs, or source files.")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write("### Unity build is blocked before activation\n\n" + message + "\n\n")
            handle.write("No Unity APK was built or published. Configure the repository's Unity Actions secrets, then rerun failed jobs.\n")
    sys.exit(1)
print("Unity activation inputs are present. The Editor will verify their validity during activation.")
