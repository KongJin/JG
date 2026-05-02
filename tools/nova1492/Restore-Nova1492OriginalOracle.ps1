param(
    [switch]$StopProcess
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$artifactRoot = Join-Path $repoRoot "artifacts\nova1492\original-oracle"
$manifestPath = Join-Path $artifactRoot "active-oracle.json"

function Fail($message) {
    Write-Error $message
    exit 1
}

if (!(Test-Path $manifestPath)) {
    Fail "No active original-oracle manifest was found: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$scenePath = [string]$manifest.scenePath
$backupPath = [string]$manifest.backupPath
$pidValue = [int]$manifest.processId

if ($StopProcess -and $pidValue -gt 0) {
    $proc = Get-Process -Id $pidValue -ErrorAction SilentlyContinue
    if ($null -ne $proc) {
        Stop-Process -Id $pidValue -Force
        Write-Host "Stopped Nova1492 PID: $pidValue"
    }
}

$restoreItems = @()
$restoreItems += [ordered]@{
    scenePath = $scenePath
    backupPath = $backupPath
}

if ($manifest.PSObject.Properties.Name -contains "patchedScenes") {
    foreach ($patchedScene in @($manifest.patchedScenes)) {
        $restoreItems += [ordered]@{
            scenePath = [string]$patchedScene.scenePath
            backupPath = [string]$patchedScene.backupPath
        }
    }
}

$seen = @{}
foreach ($item in $restoreItems) {
    $itemScenePath = [string]$item.scenePath
    $itemBackupPath = [string]$item.backupPath

    if ([string]::IsNullOrWhiteSpace($itemScenePath) -or [string]::IsNullOrWhiteSpace($itemBackupPath)) {
        Fail "Restore manifest contains an incomplete scene/backup pair."
    }

    $key = $itemScenePath.ToLowerInvariant()
    if ($seen.ContainsKey($key)) {
        continue
    }
    $seen[$key] = $true

    if (!(Test-Path $itemBackupPath)) {
        Fail "Backup file was not found; refusing to modify the oracle install: $itemBackupPath"
    }

    if (!(Test-Path $itemScenePath)) {
        Fail "Scene file was not found; refusing to delete manifest without restore: $itemScenePath"
    }

    Copy-Item -LiteralPath $itemBackupPath -Destination $itemScenePath -Force
    Write-Host "Restored scene: $itemScenePath"
    Write-Host "Restored from: $itemBackupPath"
}

Remove-Item -LiteralPath $manifestPath -Force

Write-Host "Restored original-oracle patched scenes."
