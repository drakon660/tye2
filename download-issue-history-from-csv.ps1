param(
    [string]$CsvPath = ".\tye-issues.csv",
    [string]$OutDir = ".\issue-history-json",
    [ValidateRange(0, 3600)]
    [int]$RequestDelaySeconds = $(if ($env:GITHUB_TOKEN) { 1 } else { 65 })
)

$historyScript = Join-Path $PSScriptRoot "download-issue-history.ps1"
if (-not (Test-Path -LiteralPath $historyScript)) {
    throw "Required script not found: $historyScript"
}

if (-not (Test-Path -LiteralPath $CsvPath)) {
    throw "CSV file not found: $CsvPath"
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

$rows = Import-Csv -LiteralPath $CsvPath
$savedCount = 0

foreach ($row in $rows) {
    $properties = @($row.PSObject.Properties)
    if ($properties.Count -eq 0) {
        continue
    }

    $issueIdValue = $properties[0].Value
    if ([string]::IsNullOrWhiteSpace($issueIdValue)) {
        continue
    }

    $issueId = 0
    if (-not [int]::TryParse($issueIdValue, [ref]$issueId)) {
        Write-Warning "Skipping row with invalid issue id '$issueIdValue'."
        continue
    }

    $issueUrl = "https://github.com/dotnet/tye/issues/$issueId"
    $outFile = Join-Path $OutDir "$issueId.json"

    Write-Host "Downloading history for issue #$issueId..."
    & $historyScript -IssueUrl $issueUrl -OutFile $outFile -RequestDelaySeconds $RequestDelaySeconds

    $savedCount++
}

Write-Host "Saved $savedCount issue history file(s) to $OutDir"
