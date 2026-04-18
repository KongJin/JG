param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/CodexLobbyScene.unity",
    [string]$ResultPath = "artifacts/unity/codex-lobby-ui-workflow-result.json",
    [int]$TimeoutSec = 120
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

function Resolve-AbsolutePath {
    param([string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $PathValue))
}

function Ensure-ParentDirectory {
    param([string]$PathValue)

    $directory = Split-Path -Parent $PathValue
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$resultAbsolutePath = Resolve-AbsolutePath -PathValue $ResultPath

$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec
if ($health.State.isPlaying) {
    Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
}

$compile = Invoke-McpCompileRequestAndWait -Root $root -CleanBuildCache -TimeoutMs ($TimeoutSec * 1000)
$rebuild = Invoke-McpCodexLobbyVerifiedRebuild -Root $root

if (-not $rebuild.success) {
    throw ("Verified CodexLobby rebuild failed. Missing sentinels={0}, missing references={1}" -f @($rebuild.missingSentinels).Count, @($rebuild.missingReferences).Count)
}

if ($rebuild.scenePath -ne $ScenePath) {
    throw ("Verified rebuild returned unexpected scenePath: {0}" -f $rebuild.scenePath)
}

$contract = Get-McpCodexLobbyContract -Root $root
if (-not $contract.success) {
    throw ("CodexLobby scene contract failed after rebuild. Missing sentinels={0}, missing references={1}" -f @($contract.missingSentinels).Count, @($contract.missingReferences).Count)
}

$pageSwitchJson = powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "Invoke-LobbyGaragePageSwitchSmoke.ps1") -UnityBridgeUrl $root
if ([string]::IsNullOrWhiteSpace(($pageSwitchJson | Out-String))) {
    throw "Lobby/Garage page-switch smoke returned no JSON output."
}

$pageSwitch = $pageSwitchJson | ConvertFrom-Json
if ($null -eq $pageSwitch -or -not $pageSwitch.success) {
    throw "Lobby/Garage page-switch smoke did not report success."
}

$report = [PSCustomObject]@{
    success = $true
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
    root = $root
    scenePath = $ScenePath
    resultPath = $resultAbsolutePath
    compile = $compile
    rebuild = $rebuild
    contract = $contract
    pageSwitchSmoke = $pageSwitch
}

Ensure-ParentDirectory -PathValue $resultAbsolutePath
($report | ConvertTo-Json -Depth 8) | Set-Content -Path $resultAbsolutePath -Encoding UTF8
$report | ConvertTo-Json -Depth 8
