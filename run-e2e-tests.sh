#!/bin/bash
# Run E2E tests separately per test class to avoid resource contention.
# Usage: ./run-e2e-tests.sh [class-name]
# Examples:
#   ./run-e2e-tests.sh                    # run all classes sequentially
#   ./run-e2e-tests.sh ProcessRunnerE2E   # run only ProcessRunnerE2ETests

set -e

PROJECT="test/Tye2.E2ETests"

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
SKIPPED=0
FAILURES=()

for CLASS in "${TEST_CLASSES[@]}"; do
    # If a filter argument was provided, skip non-matching classes
    if [[ -n "$1" && "$CLASS" != *"$1"* ]]; then
        continue
    fi

    echo "========================================="
    echo "Running: $CLASS"
    echo "========================================="

    if dotnet test "$PROJECT" --filter "FullyQualifiedName~$CLASS" --no-build --no-restore 2>&1 | tee /dev/stderr | grep -q "Passed!"; then
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
