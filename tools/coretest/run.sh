#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
dotnet build -c Release --nologo -v quiet
dotnet run -c Release --no-build
