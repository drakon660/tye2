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

cd "$REPO_ROOT"

echo "Restoring solution..."
dotnet restore "tye2.sln"

echo "Calculating version with GitVersion..."
ASSEMBLY_SEMVER="$(dotnet-gitversion /showvariable AssemblySemVer)"
ASSEMBLY_FILEVER="$(dotnet-gitversion /showvariable AssemblySemFileVer)"
NUGET_VERSION="$(dotnet-gitversion /showvariable SemVer)"
INFO_VERSION="$(dotnet-gitversion /showvariable InformationalVersion)"

if [[ -z "$ASSEMBLY_SEMVER" || -z "$ASSEMBLY_FILEVER" || -z "$NUGET_VERSION" || -z "$INFO_VERSION" ]]; then
  echo "ERROR: GitVersion did not return expected values." >&2
  exit 1
fi

echo "Version: $NUGET_VERSION"
echo "Publishing tye2 in Release mode..."
dotnet publish "$PROJECT" -c Release -o "$OUTPUT" --nologo \
  -p:AssemblyVersion="$ASSEMBLY_SEMVER" \
  -p:FileVersion="$ASSEMBLY_FILEVER" \
  -p:InformationalVersion="$INFO_VERSION" \
  -p:Version="$NUGET_VERSION"

echo "Build completed. Output: $OUTPUT"

