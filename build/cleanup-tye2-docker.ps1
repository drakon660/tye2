param(
    [string]$ContextDirectory,
    [switch]$IncludeVolumes,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info([string]$message) {
    Write-Host "[cleanup] $message"
}

function Get-ContextLabel([string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) {
        return $null
    }

    $full = [System.IO.Path]::GetFullPath($path)
    return $full.ToLowerInvariant().Replace('\\', '/')
}

function Remove-Resources([string]$kind, [string[]]$ids, [string[]]$removeArgsPrefix) {
    if ($ids.Count -eq 0) {
        Write-Info "No $kind found."
        return
    }

    Write-Info "Found $($ids.Count) $kind."
    foreach ($id in $ids) {
        if ($DryRun) {
            Write-Info ("DRY RUN: docker {0} {1}" -f ($removeArgsPrefix -join ' '), $id)
            continue
        }

        & docker @removeArgsPrefix $id
        if ($LASTEXITCODE -eq 0) {
            Write-Info "Removed ${kind}: $id"
        }
        else {
            Write-Info "Failed to remove ${kind}: $id"
        }
    }
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'docker CLI is not available in PATH.'
}

$labelFilter = @('--filter', 'label=tye2.managed=true')
$contextLabel = Get-ContextLabel $ContextDirectory
if ($null -ne $contextLabel) {
    $labelFilter += @('--filter', "label=tye2.context=$contextLabel")
    Write-Info "Using context filter: $contextLabel"
}

Write-Info 'Scanning Tye2 containers...'
$containerIds = & docker ps -aq @labelFilter
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to list Docker containers.'
}
$containerIds = @($containerIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

if ($containerIds.Count -gt 0 -and -not $DryRun) {
    Write-Info "Stopping $($containerIds.Count) containers..."
    & docker stop $containerIds | Out-Null
}
Remove-Resources -kind 'container(s)' -ids $containerIds -removeArgsPrefix @('rm', '-f')

Write-Info 'Scanning Tye2 networks...'
$networkIds = & docker network ls -q @labelFilter
if ($LASTEXITCODE -ne 0) {
    throw 'Failed to list Docker networks.'
}
$networkIds = @($networkIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
Remove-Resources -kind 'network(s)' -ids $networkIds -removeArgsPrefix @('network', 'rm')

if ($IncludeVolumes) {
    Write-Info 'Scanning Tye2 volumes...'
    $volumeIds = & docker volume ls -q @labelFilter
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to list Docker volumes.'
    }

    $volumeIds = @($volumeIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    Remove-Resources -kind 'volume(s)' -ids $volumeIds -removeArgsPrefix @('volume', 'rm')
}
else {
    Write-Info 'Skipping volumes. Use -IncludeVolumes to remove labeled volumes.'
}

Write-Info 'Done.'

