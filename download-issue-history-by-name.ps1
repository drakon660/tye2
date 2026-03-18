param(
    [Parameter(Mandatory = $true)]
    [string]$Owner,

    [Parameter(Mandatory = $true)]
    [string]$Repo,

    [Parameter(Mandatory = $true)]
    [string]$IssueTitle,

    [ValidateSet("open", "closed", "all")]
    [string]$State = "all",

    [string]$OutDir = ".\issue-history-by-name",

    [switch]$AllowMultiple
)

$historyScript = Join-Path $PSScriptRoot "download-issue-history.ps1"
if (-not (Test-Path -LiteralPath $historyScript)) {
    throw "Required script not found: $historyScript"
}

$headers = @{
    Accept                 = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent"           = "github-issue-history-export"
}

if ($env:GITHUB_TOKEN) {
    $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
}

function Get-PagedUrl {
    param(
        [string]$Url,
        [int]$Page,
        [int]$PerPage
    )

    if ($Url -match "\?") {
        return "{0}&page={1}&per_page={2}" -f $Url, $Page, $PerPage
    }

    return "{0}?page={1}&per_page={2}" -f $Url, $Page, $PerPage
}

function Get-AllPages {
    param(
        [string]$Url,
        [string]$Label = "items"
    )

    $results = @()
    $page = 1
    $perPage = 100

    do {
        $pagedUrl = Get-PagedUrl -Url $Url -Page $page -PerPage $perPage
        Write-Host "Fetching $Label page $page..."
        $response = Invoke-RestMethod -Uri $pagedUrl -Headers $headers -Method Get
        $items = @($response.items)
        $results += $items
        $count = $items.Count
        Write-Host "Fetched $count $Label from page $page."
        $page++
    } while ($count -eq $perPage)

    return $results
}

function Get-IssueSearchQuery {
    $stateFilter = switch ($State.ToLowerInvariant()) {
        "open" { " is:open" }
        "closed" { " is:closed" }
        default { "" }
    }

    return "repo:$Owner/$Repo is:issue in:title `"$IssueTitle`"$stateFilter"
}

function ConvertTo-SafeFileName {
    param([string]$Value)

    $invalidChars = [System.IO.Path]::GetInvalidFileNameChars()
    $safeChars = foreach ($char in $Value.ToCharArray()) {
        if ($invalidChars -contains $char) {
            '-'
        }
        else {
            $char
        }
    }

    $safeValue = (-join $safeChars).Trim()
    $safeValue = $safeValue -replace '\s+', '-'
    $safeValue = $safeValue.Trim('-')

    if ([string]::IsNullOrWhiteSpace($safeValue)) {
        return "issue"
    }

    return $safeValue
}

$issueQuery = Get-IssueSearchQuery
$encodedQuery = [System.Uri]::EscapeDataString($issueQuery)
$searchUrl = "https://api.github.com/search/issues?q=$encodedQuery&sort=created&order=desc"

Write-Host "Searching for issues named '$IssueTitle' in $Owner/$Repo..."
$candidateIssues = @(
    Get-AllPages -Url $searchUrl -Label "issue search results" |
        Where-Object { $_.title -ieq $IssueTitle }
)

if ($candidateIssues.Count -eq 0) {
    throw "No issues found with the exact title '$IssueTitle' in $Owner/$Repo."
}

if ($candidateIssues.Count -gt 1 -and -not $AllowMultiple) {
    $matches = $candidateIssues |
        Sort-Object -Property number |
        ForEach-Object { "#{0} ({1})" -f $_.number, $_.state }

    throw "Multiple issues matched '$IssueTitle': $($matches -join ', '). Re-run with -AllowMultiple to download all matches."
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

$savedFiles = @()
foreach ($issue in $candidateIssues | Sort-Object -Property number) {
    $safeTitle = ConvertTo-SafeFileName -Value $issue.title
    $outFile = Join-Path $OutDir ("issue-{0:D4}-{1}-history.json" -f $issue.number, $safeTitle)

    Write-Host "Downloading history for issue #$($issue.number) '$($issue.title)'..."
    try {
        & $historyScript -Owner $Owner -Repo $Repo -IssueNumber $issue.number -OutFile $outFile
    }
    catch {
        throw "Failed to download history for issue #$($issue.number): $($_.Exception.Message)"
    }

    $savedFiles += $outFile
}

Write-Host "Saved $($savedFiles.Count) issue history file(s) to $OutDir"
