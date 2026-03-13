# Run E2E tests separately per test class to avoid resource contention.
# Usage:
#   .\run-e2e-tests.ps1                    # run all classes sequentially
#   .\run-e2e-tests.ps1 ProcessRunnerE2E   # run only ProcessRunnerE2ETests

param(
    [string]$Filter = ""
)

$Project = "test/Tye2.E2ETests"

$TestClasses = @(
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
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Building E2E test project..."            -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
dotnet build $Project --no-restore
Write-Host ""

$Passed = 0
$Failed = 0
$Failures = @()

foreach ($Class in $TestClasses) {
    # If a filter was provided, skip non-matching classes
    if ($Filter -and $Class -notlike "*$Filter*") {
        continue
    }

    Write-Host "=========================================" -ForegroundColor Cyan
    Write-Host "Running: $Class"                          -ForegroundColor Cyan
    Write-Host "=========================================" -ForegroundColor Cyan

    dotnet test $Project --filter "FullyQualifiedName~$Class" --no-build --no-restore

    if ($LASTEXITCODE -eq 0) {
        $Passed++
        Write-Host ">>> ${Class}: PASSED" -ForegroundColor Green
    } else {
        $Failed++
        $Failures += $Class
        Write-Host ">>> ${Class}: FAILED" -ForegroundColor Red
    }

    Write-Host ""
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "SUMMARY"                                   -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Passed: $Passed" -ForegroundColor Green
Write-Host "Failed: $Failed" -ForegroundColor $(if ($Failed -gt 0) { "Red" } else { "Green" })

if ($Failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Failed classes:" -ForegroundColor Red
    foreach ($F in $Failures) {
        Write-Host "  - $F" -ForegroundColor Red
    }
    exit 1
}

Write-Host ""
Write-Host "All test classes passed!" -ForegroundColor Green
