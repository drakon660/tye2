#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$SCRIPT_DIR"
TYE2_BIN_DIR="$REPO_ROOT/artifacts/release/tye2"

MARKER_BEGIN="# >>> tye2 local build path >>>"
MARKER_END="# <<< tye2 local build path <<<"
LEGACY_MARKER_BEGIN="# >>> tye2 dotnet tools path >>>"
LEGACY_MARKER_END="# <<< tye2 dotnet tools path <<<"

rewrite_profile() {
  local profile_file="$1"
  local temp_file

  temp_file="$(mktemp)"

  awk \
    -v begin="$MARKER_BEGIN" \
    -v end="$MARKER_END" \
    -v legacy_begin="$LEGACY_MARKER_BEGIN" \
    -v legacy_end="$LEGACY_MARKER_END" \
    '
      $0 == begin { skip = 1; next }
      $0 == end { skip = 0; next }
      $0 == legacy_begin { skip = 1; next }
      $0 == legacy_end { skip = 0; next }
      skip != 1 { print }
    ' "$profile_file" > "$temp_file"

  mv "$temp_file" "$profile_file"
}

append_snippet_if_missing() {
  local profile_file="$1"

  touch "$profile_file"
  rewrite_profile "$profile_file"

  cat <<EOF >> "$profile_file"

$MARKER_BEGIN
if [ -d "$TYE2_BIN_DIR" ]; then
  case ":\$PATH:" in
    *":$TYE2_BIN_DIR:"*) : ;;
    *) export PATH="$TYE2_BIN_DIR:\$PATH" ;;
  esac
fi
$MARKER_END
EOF
  echo "Updated PATH snippet in $profile_file"
}

append_snippet_if_missing "$HOME/.zprofile"
append_snippet_if_missing "$HOME/.zshrc"
append_snippet_if_missing "$HOME/.bash_profile"

if [[ ! -x "$TYE2_BIN_DIR/tye2" ]]; then
  echo ""
  echo "WARN: tye2 executable not found at $TYE2_BIN_DIR/tye2"
  echo "Build it first with:"
  echo "  ./build/build-release.sh"
fi

echo ""
echo "Done. Open a new terminal or run:"
echo "  source ~/.zprofile"
echo "Then verify with:"
echo "  command -v tye2"
