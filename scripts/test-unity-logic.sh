#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
runner="${DOTNET_BIN:-dotnet}"
mkdir -p work/unity-tests
"$runner" run --project unity/Tests/SchulteFlow.LogicTests.csproj --configuration Release --property:BaseIntermediateOutputPath="$(pwd)/work/unity-tests/obj/"
