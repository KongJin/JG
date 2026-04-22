param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/LobbyScene.unity",
    [string]$ResultPath = "artifacts/unity/lobby-ui-workflow-result.json",
    [int]$TimeoutSec = 120
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

Assert-McpSceneAssetExistsForWorkflow -ScenePath $ScenePath -WorkflowName "Invoke-CodexLobbyUiWorkflowGate.ps1"

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$resultAbsolutePath = Resolve-McpAbsolutePath -PathValue $ResultPath

$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec
if ($health.State.isPlaying) {
    Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
}

$compile = Invoke-McpCompileRequestAndWait -Root $root -CleanBuildCache -TimeoutMs ($TimeoutSec * 1000)
$compileSuccess = Test-McpResponseSuccess -Response $compile.Wait
$layoutOwnership = Get-McpPresentationLayoutOwnership -Root $root
if (-not $layoutOwnership.success) {
    $violationPreview = @(
        @($layoutOwnership.violations) |
            Select-Object -First 3 |
            ForEach-Object { "{0}:{1} [{2}]" -f $_.path, $_.line, $_.rule }
    ) -join "; "

    throw ("Presentation layout ownership validator failed. violationCount={0}. {1}" -f @($layoutOwnership.violations).Count, $violationPreview)
}

$contract = Get-McpLobbyContract -Root $root
if (-not $contract.success) {
    throw ("Lobby scene contract failed. Missing sentinels={0}, missing references={1}" -f @($contract.missingSentinels).Count, @($contract.missingReferences).Count)
}

if ($contract.scenePath -ne $ScenePath) {
    throw ("Lobby contract returned unexpected scenePath: {0}" -f $contract.scenePath)
}

$report = [PSCustomObject]@{
    success = $true
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
    scenePath = $ScenePath
    resultPath = $resultAbsolutePath
    compile = [PSCustomObject]@{
        success = $compileSuccess
        healthAfterWait = $compile.HealthAfterWait
    }
    presentationLayoutOwnership = [PSCustomObject]@{
        success = [bool]$layoutOwnership.success
        reportPath = $layoutOwnership.reportPath
        violationCount = @($layoutOwnership.violations).Count
    }
    contract = [PSCustomObject]@{
        success = [bool]$contract.success
        sceneSaved = [bool]$contract.sceneSaved
        missingSentinelCount = @($contract.missingSentinels).Count
        missingReferenceCount = @($contract.missingReferences).Count
    }
}

Ensure-McpParentDirectory -PathValue $resultAbsolutePath
($report | ConvertTo-Json -Depth 8) | Set-Content -Path $resultAbsolutePath -Encoding UTF8
$report | ConvertTo-Json -Depth 8
