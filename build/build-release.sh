#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPO_ROOT/src/tye2/tye2.csproj"
OUTPUT="$REPO_ROOT/artifacts/release/tye2"

if [[ ! -f "$PROJECT" ]]; then
  echo "ERROR: Project file not found: $PROJECT" >&2
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet CLI not found in PATH." >&2
  exit 1
fi

cd "$REPO_ROOT"

echo "Restoring solution..."
dotnet restore "tye2.sln"

echo "Publishing tye2 in Release mode..."
dotnet publish "$PROJECT" -c Release -o "$OUTPUT" --nologo

echo "Build completed. Output: $OUTPUT"
