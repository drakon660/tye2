# Run tests with code coverage and generate HTML report
param(
    [string]$ReportDir = "coveragereport",
    [switch]$Open,
    # -Unit: unit tests, -E2E: e2e tests, both: all, neither: unit only (default)
    [switch]$Unit,
    [switch]$E2E
)

$resultsDir = "TestResults"

# Determine which projects to run
$testProjects = @()
$runUnit = $Unit -or (-not $E2E)
$runE2E = $E2E

if ($runUnit) { $testProjects += "test/Tye2.UnitTests/Tye2.UnitTests.csproj" }
if ($runE2E)  { $testProjects += "test/Tye2.E2ETests/Tye2.E2ETests.csproj" }

Write-Host "Projects: $($testProjects -join ', ')" -ForegroundColor Gray

# Clean previous results
if (Test-Path $resultsDir) {
    Remove-Item $resultsDir -Recurse -Force
}
if (Test-Path $ReportDir) {
    Remove-Item $ReportDir -Recurse -Force
}

# Run each test project with coverage
$results = @{}
foreach ($project in $testProjects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " Running $projectName" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    dotnet test $project --collect:"XPlat Code Coverage" --results-directory "$resultsDir/$projectName" -v normal --logger:"console;verbosity=detailed"
    $results[$projectName] = $LASTEXITCODE -eq 0
}

# Print combined summary
Write-Host ""
Write-Host "========================================" -ForegroundColor White
Write-Host " Test Run Summary" -ForegroundColor White
Write-Host "========================================" -ForegroundColor White
$failed = $false
foreach ($entry in $results.GetEnumerator()) {
    if ($entry.Value) {
        Write-Host "  $($entry.Key): PASSED" -ForegroundColor Green
    } else {
        Write-Host "  $($entry.Key): FAILED" -ForegroundColor Red
        $failed = $true
    }
}
Write-Host ""

# Find all coverage files
$coverageFiles = Get-ChildItem -Path $resultsDir -Filter "coverage.cobertura.xml" -Recurse
if ($coverageFiles.Count -eq 0) {
    Write-Host "No coverage files found." -ForegroundColor Red
    exit 1
}

$reportsArg = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
Write-Host "Found $($coverageFiles.Count) coverage file(s)." -ForegroundColor Gray

# Check for reportgenerator
if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    Write-Host "Installing reportgenerator..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# Generate merged report
Write-Host "Generating coverage report..." -ForegroundColor Cyan
reportgenerator `
    -reports:"$reportsArg" `
    -targetdir:$ReportDir `
    -reporttypes:Html `
    "-filefilters:-*obj*;-*.Designer.cs"

Write-Host "Report generated at $ReportDir/index.html" -ForegroundColor Green

# Open in browser if requested
if ($Open) {
    Start-Process "$ReportDir/index.html"
}
