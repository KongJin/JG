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

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$resultAbsolutePath = Resolve-McpAbsolutePath -PathValue $ResultPath

$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec
if ($health.State.isPlaying) {
    Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
}

$compile = Invoke-McpCompileRequestAndWait -Root $root -CleanBuildCache -TimeoutMs ($TimeoutSec * 1000)
$contract = Get-McpCodexLobbyContract -Root $root
if (-not $contract.success) {
    throw ("CodexLobby scene contract failed. Missing sentinels={0}, missing references={1}" -f @($contract.missingSentinels).Count, @($contract.missingReferences).Count)
}

if ($contract.scenePath -ne $ScenePath) {
    throw ("CodexLobby contract returned unexpected scenePath: {0}" -f $contract.scenePath)
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
    scenePath = $ScenePath
    resultPath = $resultAbsolutePath
    compile = [PSCustomObject]@{
        success = [bool]$compile.Wait.ok
    }
    contract = [PSCustomObject]@{
        success = [bool]$contract.success
        sceneSaved = [bool]$contract.sceneSaved
        missingSentinelCount = @($contract.missingSentinels).Count
        missingReferenceCount = @($contract.missingReferences).Count
    }
    pageSwitchSmoke = [PSCustomObject]@{
        success = [bool]$pageSwitch.success
        resultPath = $pageSwitch.resultPath
        warningCount = $pageSwitch.warningCount
        errorCount = $pageSwitch.errorCount
    }
}

Ensure-McpParentDirectory -PathValue $resultAbsolutePath
($report | ConvertTo-Json -Depth 8) | Set-Content -Path $resultAbsolutePath -Encoding UTF8
$report | ConvertTo-Json -Depth 8
