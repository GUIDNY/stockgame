#!/usr/bin/env bash
# Type-checks every runtime script under Assets/Scripts against a stub UnityEngine. Not a substitute for Unity.
set -euo pipefail
cd "$(dirname "$0")"
dotnet build -c Release --nologo -v quiet
echo "Unity-facing scripts compiled against the stub API."
