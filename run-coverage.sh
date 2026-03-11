#!/usr/bin/env bash
set -uo pipefail

# Run tests with code coverage and generate HTML report (macOS/Linux).
# Default behavior matches run-coverage.ps1:
# - no flags => unit tests only
# - --e2e => e2e tests only
# - --unit --e2e => both

usage() {
  cat <<'EOF'
Usage: ./run-coverage.sh [options]

Options:
  --report-dir <dir>  Output directory for HTML report (default: coveragereport)
  --open              Open generated report in browser (macOS `open`)
  --unit              Include unit tests
  --e2e               Include e2e tests
  -h, --help          Show this help
EOF
}

REPORT_DIR="coveragereport"
RESULTS_DIR="TestResults"
OPEN_REPORT=false
UNIT=false
E2E=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --report-dir)
      if [[ $# -lt 2 ]]; then
        echo "ERROR: --report-dir requires a value." >&2
        usage
        exit 1
      fi
      REPORT_DIR="$2"
      shift 2
      ;;
    --open)
      OPEN_REPORT=true
      shift
      ;;
    --unit)
      UNIT=true
      shift
      ;;
    --e2e)
      E2E=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "ERROR: Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet CLI not found in PATH." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

run_unit=false
run_e2e=false

if [[ "$UNIT" == true || "$E2E" == false ]]; then
  run_unit=true
fi

if [[ "$E2E" == true ]]; then
  run_e2e=true
fi

test_projects=()
if [[ "$run_unit" == true ]]; then
  test_projects+=("test/Tye2.UnitTests/Tye2.UnitTests.csproj")
fi
if [[ "$run_e2e" == true ]]; then
  test_projects+=("test/Tye2.E2ETests/Tye2.E2ETests.csproj")
fi

echo "Projects: ${test_projects[*]}"

rm -rf "$RESULTS_DIR" "$REPORT_DIR"

project_names=()
project_results=()
any_failed=false

for project in "${test_projects[@]}"; do
  project_name="$(basename "$project" .csproj)"

  echo ""
  echo "========================================"
  echo " Running $project_name"
  echo "========================================"

  if dotnet test "$project" \
      --collect:"XPlat Code Coverage" \
      --results-directory "$RESULTS_DIR/$project_name" \
      -v normal \
      --logger:"console;verbosity=detailed"; then
    project_results+=(0)
  else
    project_results+=($?)
    any_failed=true
  fi

  project_names+=("$project_name")
done

echo ""
echo "========================================"
echo " Test Run Summary"
echo "========================================"
for i in "${!project_names[@]}"; do
  if [[ "${project_results[$i]}" -eq 0 ]]; then
    echo "  ${project_names[$i]}: PASSED"
  else
    echo "  ${project_names[$i]}: FAILED"
  fi
done
echo ""

reports_arg=""
coverage_count=0
while IFS= read -r -d '' coverage_file; do
  coverage_count=$((coverage_count + 1))
  if [[ -z "$reports_arg" ]]; then
    reports_arg="$coverage_file"
  else
    reports_arg="${reports_arg};${coverage_file}"
  fi
done < <(find "$RESULTS_DIR" -type f -name "coverage.cobertura.xml" -print0)

if [[ "$coverage_count" -eq 0 ]]; then
  echo "No coverage files found." >&2
  exit 1
fi

echo "Found $coverage_count coverage file(s)."

reportgenerator_cmd=""
if command -v reportgenerator >/dev/null 2>&1; then
  reportgenerator_cmd="reportgenerator"
elif [[ -x "$HOME/.dotnet/tools/reportgenerator" ]]; then
  reportgenerator_cmd="$HOME/.dotnet/tools/reportgenerator"
else
  echo "Installing reportgenerator..."
  dotnet tool install -g dotnet-reportgenerator-globaltool

  if command -v reportgenerator >/dev/null 2>&1; then
    reportgenerator_cmd="reportgenerator"
  elif [[ -x "$HOME/.dotnet/tools/reportgenerator" ]]; then
    reportgenerator_cmd="$HOME/.dotnet/tools/reportgenerator"
  else
    echo "ERROR: reportgenerator not found after install. Ensure \$HOME/.dotnet/tools is in PATH." >&2
    exit 1
  fi
fi

echo "Generating coverage report..."
"$reportgenerator_cmd" \
  -reports:"$reports_arg" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:Html \
  "-filefilters:-*obj*;-*.Designer.cs"

echo "Report generated at $REPORT_DIR/index.html"

if [[ "$OPEN_REPORT" == true ]]; then
  if command -v open >/dev/null 2>&1; then
    open "$REPORT_DIR/index.html"
  else
    echo "WARN: 'open' command not found; cannot auto-open report." >&2
  fi
fi

if [[ "$any_failed" == true ]]; then
  exit 1
fi
