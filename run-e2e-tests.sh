#!/usr/bin/env bash
# Run E2E tests separately per test class to avoid resource contention.
# Usage: ./run-e2e-tests.sh [class-name]
# Examples:
#   ./run-e2e-tests.sh                    # run all classes sequentially
#   ./run-e2e-tests.sh ProcessRunnerE2E   # run only ProcessRunnerE2ETests

set -euo pipefail

PROJECT="test/Tye2.E2ETests"
FILTER="${1:-}"

TEST_CLASSES=(
    "ApplicationFactoryTests"
    "ApplicationTests"
    "HealthCheckTests"
    "ProcessRunnerE2ETests"
    "ProcessUtilTests"
    "ReplicaStoppingTests"
    "TyeGenerateTests"
    "TyeInitTests"
    "TyePurgeTests"
    "TyeRunTests"
)

# Build once before running tests
echo "========================================="
echo "Building E2E test project..."
echo "========================================="
dotnet build "$PROJECT" --no-restore
echo ""

PASSED=0
FAILED=0
FAILURES=()

for CLASS in "${TEST_CLASSES[@]}"; do
    # If a filter argument was provided, skip non-matching classes
    if [[ -n "$FILTER" && "$CLASS" != *"$FILTER"* ]]; then
        continue
    fi

    echo "========================================="
    echo "Running: $CLASS"
    echo "========================================="

    if dotnet test "$PROJECT" --filter "FullyQualifiedName~$CLASS" --no-build --no-restore; then
        PASSED=$((PASSED + 1))
        echo ">>> $CLASS: PASSED"
    else
        FAILED=$((FAILED + 1))
        FAILURES+=("$CLASS")
        echo ">>> $CLASS: FAILED"
    fi

    echo ""
done

echo "========================================="
echo "SUMMARY"
echo "========================================="
echo "Passed: $PASSED"
echo "Failed: $FAILED"

if [[ ${#FAILURES[@]} -gt 0 ]]; then
    echo ""
    echo "Failed classes:"
    for F in "${FAILURES[@]}"; do
        echo "  - $F"
    done
    exit 1
fi

echo ""
echo "All test classes passed!"
