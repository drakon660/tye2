param(
    [Parameter(Mandatory = $true)]
    [string]$Owner,

    [Parameter(Mandatory = $true)]
    [string]$Repo,

    [string]$OutDir = ".\issues-chat-pages"
)

if (-not $env:GITHUB_TOKEN) {
    throw "Set GITHUB_TOKEN first. Example: `$env:GITHUB_TOKEN='ghp_...'"
}

$headers = @{
    Accept                 = "application/vnd.github+json"
    Authorization          = "Bearer $env:GITHUB_TOKEN"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent"           = "github-issues-chat-export"
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
        $items = Invoke-RestMethod -Uri $pagedUrl -Headers $headers -Method Get
        $results += @($items)
        $count = @($items).Count
        Write-Host "Fetched $count $Label from page $page."
        $page++
    } while ($count -eq $perPage)

    return $results
}

function Get-IssueSearchQuery {
    return "repo:$Owner/$Repo is:issue is:open"
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

$issueQuery = Get-IssueSearchQuery
$page = 1
$perPage = 100
$totalIssues = 0
$savedFiles = 0

do {
    $encodedQuery = [System.Uri]::EscapeDataString($issueQuery)
    $pagedIssuesUrl = "https://api.github.com/search/issues?q=$encodedQuery&sort=created&order=desc&page=$page&per_page=$perPage"
    Write-Host "Fetching issues page $page..."
    $searchResponse = Invoke-RestMethod -Uri $pagedIssuesUrl -Headers $headers -Method Get
    $issues = @($searchResponse.items)
    $count = $issues.Count
    Write-Host "Fetched $count issues from page $page."
    Write-Host "Processing $($issues.Count) issues from page $page..."

    if ($count -gt 0) {
        $output = @(
            foreach ($issue in $issues) {
                [PSCustomObject]@{
                    number = $issue.number
                    title  = $issue.title
                    body   = $issue.body
                }
            }
        )

        $outFile = Join-Path $OutDir ("issues-page-{0:D4}.json" -f $page)
        $output | ConvertTo-Json -Depth 10 | Set-Content -Path $outFile -Encoding UTF8
        Write-Host "Saved $($output.Count) issues to $outFile"

        $totalIssues += $output.Count
        $savedFiles++
    }

    $page++
} while ($count -eq $perPage)

Write-Host "Saved $totalIssues issues across $savedFiles files in $OutDir"
