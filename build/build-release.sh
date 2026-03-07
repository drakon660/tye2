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

if ! command -v dotnet-gitversion >/dev/null 2>&1; then
  echo "ERROR: dotnet-gitversion not found in PATH." >&2
  echo "Install with: dotnet tool install --global GitVersion.Tool" >&2
  exit 1
fi

get_gitversion_var() {
  local name="$1"
  local value

  if ! value="$(dotnet-gitversion /showvariable "$name")"; then
    echo "ERROR: GitVersion failed while resolving $name." >&2
    exit 1
  fi

  if [[ -z "$value" ]]; then
    echo "ERROR: GitVersion returned an empty value for $name." >&2
    exit 1
  fi

  printf '%s' "$value"
}

cd "$REPO_ROOT"

echo "Restoring solution..."
dotnet restore "tye2.sln"

echo "Calculating version with GitVersion..."
ASSEMBLY_SEMVER="$(get_gitversion_var AssemblySemVer)"
ASSEMBLY_FILEVER="$(get_gitversion_var AssemblySemFileVer)"
NUGET_VERSION="$(get_gitversion_var SemVer)"
INFO_VERSION="$(get_gitversion_var InformationalVersion)"

echo "Version: $NUGET_VERSION"
echo "Publishing tye2 in Release mode..."
dotnet publish "$PROJECT" -c Release -o "$OUTPUT" --nologo \
  -p:AssemblyVersion="$ASSEMBLY_SEMVER" \
  -p:FileVersion="$ASSEMBLY_FILEVER" \
  -p:InformationalVersion="$INFO_VERSION" \
  -p:Version="$NUGET_VERSION"

echo "Build completed. Output: $OUTPUT"
