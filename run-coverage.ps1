# Run tests with code coverage and generate HTML report
[CmdletBinding()]
param(
    [string]$ReportDir = "coveragereport",
    [switch]$Open,
    [switch]$Unit,
    [switch]$E2E,
    [string]$Class,
    [string]$Method
)

# Handle Ctrl+C gracefully
$script:cancelled = $false
$script:currentProcess = $null

# This can fail in non-interactive hosts where no console handle is attached.
try {
    [Console]::TreatControlCAsInput = $false
}
catch {
}
$null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action {
    $script:cancelled = $true
}

trap {
    $script:cancelled = $true
    if ($script:currentProcess -and -not $script:currentProcess.HasExited) {
        $script:currentProcess.Kill($true)
    }
    break
}

$resultsDir = "TestResults"

# All test projects listed explicitly
$unitProjects = @(
    "test/Tye2.UnitTests/Tye2.UnitTests.csproj",
    "test/Tye2.Extensions.Configuration.Tests/Tye2.Extensions.Configuration.Tests.csproj"
)

$e2eProjects = @(
    "test/Tye2.E2ETests/Tye2.E2ETests.csproj"
)

$testProjects = @()
$runUnit = $Unit -or (-not $E2E)
$runE2E = $E2E

if ($runUnit) { $testProjects += $unitProjects }
if ($runE2E) { $testProjects += $e2eProjects }

Write-Host "Projects: $($testProjects -join ', ')" -ForegroundColor Gray

# Clean previous results
if (Test-Path $resultsDir) {
    Remove-Item $resultsDir -Recurse -Force
}
if (Test-Path $ReportDir) {
    Remove-Item $ReportDir -Recurse -Force
}

# Run each test method individually per project
$totalPassed = 0; $totalFailed = 0; $totalSkipped = 0
$failedTests = @()

foreach ($project in $testProjects) {
    if ($script:cancelled) { break }
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " $projectName — discovering tests" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    # Discover all test methods
    $psiList = New-Object System.Diagnostics.ProcessStartInfo
    $psiList.FileName = "dotnet"
    $psiList.Arguments = "test $project --list-tests -v quiet"
    $psiList.WorkingDirectory = $PSScriptRoot
    $psiList.RedirectStandardOutput = $true
    $psiList.RedirectStandardError = $true
    $psiList.UseShellExecute = $false
    $psiList.CreateNoWindow = $true
    $procList = [System.Diagnostics.Process]::Start($psiList)
    $script:currentProcess = $procList
    $listOutputRaw = $procList.StandardOutput.ReadToEnd()
    $procList.WaitForExit()
    $script:currentProcess = $null
    if ($script:cancelled) { break }
    $listOutput = $listOutputRaw -split "`r?`n"
    $tests = $listOutput | Where-Object {
        $_ -match "^\s{4}\S" -and $_ -notmatch "^Build|^Test run|^The following"
    } | ForEach-Object { $_.Trim() }

    if (-not $tests -or $tests.Count -eq 0) {
        Write-Host "  No tests found." -ForegroundColor Yellow
        continue
    }

    # Deduplicate parameterized tests — keep only the method name (before parentheses)
    $methods = $tests | ForEach-Object { $_ -replace '\(.*$', '' } | Select-Object -Unique

    # Filter by class name if specified (e.g. -Class TyeRunTests)
    if ($Class) {
        $methods = $methods | Where-Object { $_ -match "\.$Class\." -or $_ -match "\.$Class$" -or $_ -like "*$Class*" }
        if (-not $methods -or @($methods).Count -eq 0) {
            Write-Host "  No methods matching '$Class' in this project." -ForegroundColor Yellow
            continue
        }
        $methods = @($methods)
    }

    # Filter by method name if specified (e.g. -Method NginxIngressTest)
    if ($Method) {
        $methods = $methods | Where-Object { $_ -like "*.$Method" -or $_ -eq $Method }
        if (-not $methods -or @($methods).Count -eq 0) {
            Write-Host "  No methods matching '$Method' in this project." -ForegroundColor Yellow
            continue
        }
        $methods = @($methods)
    }

    Write-Host "  Found $(@($methods).Count) method(s) to run" -ForegroundColor Gray
    Write-Host ""

    $projectPassed = 0; $projectFailed = 0

    foreach ($methodName in $methods) {
        if ($script:cancelled) { break }

        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $argList = "test $project --filter `"FullyQualifiedName=$methodName`" --collect:`"XPlat Code Coverage`" --results-directory `"$resultsDir/$projectName`" -v quiet"
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = "dotnet"
        $psi.Arguments = $argList
        $psi.WorkingDirectory = $PSScriptRoot
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.UseShellExecute = $false
        $psi.CreateNoWindow = $true

        $proc = [System.Diagnostics.Process]::Start($psi)
        $script:currentProcess = $proc
        $stdout = $proc.StandardOutput.ReadToEnd()
        $stderr = $proc.StandardError.ReadToEnd()
        $proc.WaitForExit()
        $script:currentProcess = $null
        $exitCode = $proc.ExitCode
        $sw.Stop()

        if ($script:cancelled) { break }

        $duration = $sw.Elapsed.ToString("mm\:ss\.fff")
        $output = ($stdout + "`n" + $stderr) -split "`r?`n"

        if ($exitCode -eq 0) {
            Write-Host "  PASS  $methodName" -ForegroundColor Green -NoNewline
            Write-Host "  ($duration)" -ForegroundColor DarkGray
            $projectPassed++
        } else {
            Write-Host "  FAIL  $methodName" -ForegroundColor Red -NoNewline
            Write-Host "  ($duration)" -ForegroundColor DarkGray
            # Extract error lines from output
            $errorLines = $output | Where-Object { $_ -match "^\s+Expected|^\s+Assert|^\s+at |^\s+But |^\s+Message" } | Select-Object -First 3
            foreach ($line in $errorLines) {
                Write-Host "        $($line.TrimStart())" -ForegroundColor DarkRed
            }
            $failedTests += "$projectName::$methodName"
            $projectFailed++
        }
    }

    Write-Host ""
    Write-Host "  $projectName — " -NoNewline
    Write-Host "Passed: $projectPassed  " -ForegroundColor Green -NoNewline
    if ($projectFailed -gt 0) { Write-Host "Failed: $projectFailed" -ForegroundColor Red } else { Write-Host "" }
    $totalPassed += $projectPassed
    $totalFailed += $projectFailed
}

if ($script:cancelled) {
    Write-Host ""
    Write-Host "  Cancelled by user." -ForegroundColor Yellow
}

# Print combined summary
Write-Host ""
Write-Host "========================================" -ForegroundColor White
Write-Host " Summary" -ForegroundColor White
Write-Host "========================================" -ForegroundColor White
Write-Host "  Total: $($totalPassed + $totalFailed)  " -NoNewline
Write-Host "Passed: $totalPassed  " -ForegroundColor Green -NoNewline
if ($totalFailed -gt 0) { Write-Host "Failed: $totalFailed" -ForegroundColor Red } else { Write-Host "" }

if ($failedTests.Count -gt 0) {
    Write-Host ""
    Write-Host "  Failed tests:" -ForegroundColor Red
    foreach ($ft in $failedTests) {
        Write-Host "    - $ft" -ForegroundColor Red
    }
}
Write-Host ""
$failed = $totalFailed -gt 0

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
