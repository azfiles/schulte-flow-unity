#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${UNITY_EDITOR:?Set UNITY_EDITOR to a licensed Unity 6000.0 LTS editor with Android Build Support}"
mkdir -p work/unity-build unity/Builds/Android
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$(pwd)/unity" -buildTarget Android \
  -executeMethod SchulteFlow.Editor.UnityBuild.Android -logFile "$(pwd)/work/unity-build/editor.log"
if ! grep -q 'SCHULTE_BUILD_PASS:' work/unity-build/editor.log; then
  echo 'Unity did not report a successful build; inspect work/unity-build/editor.log.' >&2
  exit 1
fi
