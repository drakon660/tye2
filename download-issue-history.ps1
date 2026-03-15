[CmdletBinding(DefaultParameterSetName = "ByNumber")]
param(
    [Parameter(Mandatory = $true, ParameterSetName = "ByNumber")]
    [string]$Owner,

    [Parameter(Mandatory = $true, ParameterSetName = "ByNumber")]
    [string]$Repo,

    [Parameter(Mandatory = $true, ParameterSetName = "ByNumber")]
    [int]$IssueNumber,

    [Parameter(Mandatory = $true, ParameterSetName = "ByUrl")]
    [string]$IssueUrl,

    [ValidateRange(0, 3600)]
    [int]$RequestDelaySeconds = $(if ($env:GITHUB_TOKEN) { 1 } else { 65 }),

    [string]$OutFile
)

$headers = @{
    Accept                 = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent"           = "github-issue-history-export"
}

if ($env:GITHUB_TOKEN) {
    $headers["Authorization"] = "Bearer $env:GITHUB_TOKEN"
}

$script:RequestCount = 0

function Invoke-GitHubApi {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,

        [string]$Label = "request"
    )

    if ($script:RequestCount -gt 0 -and $RequestDelaySeconds -gt 0) {
        Write-Host "Sleeping $RequestDelaySeconds second(s) before $Label..."
        Start-Sleep -Seconds $RequestDelaySeconds
    }

    $script:RequestCount++
    return Invoke-RestMethod -Uri $Url -Headers $headers -Method Get
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
        $items = Invoke-GitHubApi -Url $pagedUrl -Label "$Label page $page"
        $results += @($items)
        $count = @($items).Count
        Write-Host "Fetched $count $Label from page $page."
        $page++
    } while ($count -eq $perPage)

    return $results
}

function Get-PropValue {
    param(
        $Object,
        [string]$Name
    )

    if ($null -eq $Object) {
        return $null
    }

    $prop = $Object.PSObject.Properties[$Name]
    if ($null -ne $prop) {
        return $prop.Value
    }

    return $null
}

function Convert-TimelineEvent {
    param($EventItem)

    $eventType = Get-PropValue -Object $EventItem -Name "event"
    $actor = Get-PropValue -Object (Get-PropValue -Object $EventItem -Name "actor") -Name "login"
    $createdAt = Get-PropValue -Object $EventItem -Name "created_at"
    $details = $null

    switch ($eventType) {
        "assigned" {
            $details = Get-PropValue -Object (Get-PropValue -Object $EventItem -Name "assignee") -Name "login"
        }
        "unassigned" {
            $details = Get-PropValue -Object (Get-PropValue -Object $EventItem -Name "assignee") -Name "login"
        }
        "labeled" {
            $details = Get-PropValue -Object (Get-PropValue -Object $EventItem -Name "label") -Name "name"
        }
        "unlabeled" {
            $details = Get-PropValue -Object (Get-PropValue -Object $EventItem -Name "label") -Name "name"
        }
        "renamed" {
            $rename = Get-PropValue -Object $EventItem -Name "rename"
            $from = Get-PropValue -Object $rename -Name "from"
            $to = Get-PropValue -Object $rename -Name "to"
            if ($from -or $to) {
                $details = "{0} -> {1}" -f $from, $to
            }
        }
        "milestoned" {
            $details = Get-PropValue -Object (Get-PropValue -Object $EventItem -Name "milestone") -Name "title"
        }
        "demilestoned" {
            $details = Get-PropValue -Object (Get-PropValue -Object $EventItem -Name "milestone") -Name "title"
        }
        "referenced" {
            $source = Get-PropValue -Object $EventItem -Name "source"
            $issue = Get-PropValue -Object $source -Name "issue"
            $sourceNumber = Get-PropValue -Object $issue -Name "number"
            $sourceUrl = Get-PropValue -Object $issue -Name "html_url"
            if ($sourceNumber) {
                $details = "Referenced by issue #$sourceNumber"
                if ($sourceUrl) {
                    $details = "$details ($sourceUrl)"
                }
            }
        }
        "cross-referenced" {
            $source = Get-PropValue -Object $EventItem -Name "source"
            $issue = Get-PropValue -Object $source -Name "issue"
            $sourceNumber = Get-PropValue -Object $issue -Name "number"
            $sourceUrl = Get-PropValue -Object $issue -Name "html_url"
            if ($sourceNumber) {
                $details = "Cross-referenced by issue #$sourceNumber"
                if ($sourceUrl) {
                    $details = "$details ($sourceUrl)"
                }
            }
        }
    }

    return [PSCustomObject]@{
        type       = $eventType
        actor      = $actor
        created_at = $createdAt
        details    = $details
    }
}

function Resolve-IssueReference {
    if ($PSCmdlet.ParameterSetName -eq "ByUrl") {
        $pattern = '^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/issues/(?<number>\d+)(?:[/?#].*)?$'
        $match = [System.Text.RegularExpressions.Regex]::Match($IssueUrl, $pattern)

        if (-not $match.Success) {
            throw "IssueUrl must look like https://github.com/<owner>/<repo>/issues/<number>."
        }

        return [PSCustomObject]@{
            Owner       = $match.Groups["owner"].Value
            Repo        = $match.Groups["repo"].Value
            IssueNumber = [int]$match.Groups["number"].Value
        }
    }

    return [PSCustomObject]@{
        Owner       = $Owner
        Repo        = $Repo
        IssueNumber = $IssueNumber
    }
}

$issueReference = Resolve-IssueReference
$Owner = $issueReference.Owner
$Repo = $issueReference.Repo
$IssueNumber = $issueReference.IssueNumber

if (-not $OutFile) {
    $OutFile = ".\issue-$IssueNumber-history.json"
}

if (-not $env:GITHUB_TOKEN) {
    Write-Warning "GITHUB_TOKEN is not set. Anonymous GitHub API requests are heavily rate-limited; using a default $RequestDelaySeconds-second delay between requests."
}

$issueUrl = "https://api.github.com/repos/$Owner/$Repo/issues/$IssueNumber"
Write-Host "Fetching issue #$IssueNumber..."
$issue = Invoke-GitHubApi -Url $issueUrl -Label "issue #$IssueNumber"

if (Get-PropValue -Object $issue -Name "pull_request") {
    throw "Issue #$IssueNumber is a pull request, not an issue."
}

Write-Host "Fetching comments for issue #$IssueNumber..."
$commentsUrl = "https://api.github.com/repos/$Owner/$Repo/issues/$IssueNumber/comments"
$comments = @(
    Get-AllPages -Url $commentsUrl -Label "comments for issue #$IssueNumber" |
        Where-Object { $null -ne $_ } |
        ForEach-Object {
            [PSCustomObject]@{
                type       = "commented"
                actor      = $_.user.login
                created_at = $_.created_at
                body       = $_.body
            }
        }
)

Write-Host "Fetching timeline for issue #$IssueNumber..."
$timelineUrl = "https://api.github.com/repos/$Owner/$Repo/issues/$IssueNumber/timeline"
$timeline = @(
    Get-AllPages -Url $timelineUrl -Label "timeline events for issue #$IssueNumber" |
        Where-Object {
            $null -ne $_ -and
            $_.event -ne "commented"
        } |
        ForEach-Object { Convert-TimelineEvent -EventItem $_ }
)

$history = @(
    [PSCustomObject]@{
        type       = "opened"
        actor      = $issue.user.login
        created_at = $issue.created_at
        body       = $issue.body
    }

    $comments

    $timeline
) |
    Sort-Object -Property created_at

$output = [PSCustomObject]@{
    number  = $issue.number
    title   = $issue.title
    body    = $issue.body
    state   = $issue.state
    html_url = $issue.html_url
    history = $history
}

$output | ConvertTo-Json -Depth 10 | Set-Content -Path $OutFile -Encoding UTF8
Write-Host "Saved issue history to $OutFile"
