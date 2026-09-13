#!/usr/bin/env bash
# Builds and runs the simulation harness. Excludes any script that depends on UnityEngine/UnityEditor.
set -euo pipefail
cd "$(dirname "$0")"
{
  echo '<Project><ItemGroup>'
  (grep -rL --include='*.cs' -E '^using UnityEngine|^using UnityEditor' ../../Assets/Scripts || true) | sort | while read -r f; do
    echo "  <CoreFiles Include=\"$f\" />"
  done
  echo '</ItemGroup></Project>'
} > corefiles.props
dotnet build -c Release --nologo -v quiet
dotnet run -c Release --no-build -- "$@"
