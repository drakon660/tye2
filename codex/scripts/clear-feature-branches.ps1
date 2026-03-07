[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Remote = "origin",
    [string[]]$Patterns = @("feature/*", "hotfix/*", "bugfix/*"),
    [string[]]$ProtectedBranches = @("main", "master", "develop", "dev"),
    [switch]$SkipFetch,
    [switch]$SkipLocal,
    [switch]$SkipRemote,
    [switch]$Force,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Get-BranchMatches {
    param(
        [string[]]$BranchNames,
        [string[]]$GlobPatterns
    )

    $matched = New-Object System.Collections.Generic.List[string]

    foreach ($name in $BranchNames) {
        foreach ($pattern in $GlobPatterns) {
            if ($name -like $pattern) {
                $matched.Add($name)
                break
            }
        }
    }

    return $matched | Sort-Object -Unique
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "git is required but was not found in PATH."
}

if (-not $SkipFetch) {
    Write-Host "Fetching/pruning remote refs from '$Remote'..."
    git fetch $Remote --prune | Out-Host
}

$currentBranch = (git branch --show-current).Trim()
if ([string]::IsNullOrWhiteSpace($currentBranch)) {
    throw "Unable to determine current branch."
}

$protected = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::OrdinalIgnoreCase)
foreach ($b in $ProtectedBranches) {
    [void]$protected.Add($b)
}
[void]$protected.Add($currentBranch)

$localBranches = git for-each-ref --format="%(refname:short)" refs/heads
$remoteBranchRefs = git for-each-ref --format="%(refname:short)" "refs/remotes/$Remote"
$remotePrefix = "$Remote/"
$remoteBranches = $remoteBranchRefs |
    Where-Object { $_ -and $_.StartsWith($remotePrefix) -and $_ -ne "$Remote/HEAD" } |
    ForEach-Object { $_.Substring($remotePrefix.Length) }

$targetLocal = Get-BranchMatches -BranchNames $localBranches -GlobPatterns $Patterns |
    Where-Object { -not $protected.Contains($_) }
$targetRemote = Get-BranchMatches -BranchNames $remoteBranches -GlobPatterns $Patterns |
    Where-Object { -not $protected.Contains($_) }

if (-not $targetLocal -and -not $targetRemote) {
    Write-Host "No matching feature branches found."
    exit 0
}

Write-Host "Current branch: $currentBranch"
Write-Host "Patterns: $($Patterns -join ', ')"

if (-not $SkipLocal) {
    if ($targetLocal) {
        Write-Host "Local branches to delete ($($targetLocal.Count)):"
        $targetLocal | ForEach-Object { Write-Host "  - $_" }
    }
    else {
        Write-Host "No local branches matched."
    }
}

if (-not $SkipRemote) {
    if ($targetRemote) {
        Write-Host "Remote branches to delete on '$Remote' ($($targetRemote.Count)):"
        $targetRemote | ForEach-Object { Write-Host "  - $_" }
    }
    else {
        Write-Host "No remote branches matched."
    }
}

if ($DryRun) {
    Write-Host "Dry run enabled, no branches were deleted."
    exit 0
}

if (-not $SkipLocal) {
    foreach ($branch in $targetLocal) {
        $args = if ($Force) { @("branch", "-D", $branch) } else { @("branch", "-d", $branch) }

        if ($PSCmdlet.ShouldProcess("local branch '$branch'", "Delete")) {
            git @args | Out-Host
        }
    }
}

if (-not $SkipRemote) {
    foreach ($branch in $targetRemote) {
        if ($PSCmdlet.ShouldProcess("remote branch '$Remote/$branch'", "Delete")) {
            git push $Remote --delete $branch | Out-Host
        }
    }
}

Write-Host "Feature branch cleanup complete."


