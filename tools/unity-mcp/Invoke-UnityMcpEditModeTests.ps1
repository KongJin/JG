param(
    [string[]]$GroupName = @(),
    [string[]]$TestName = @(),
    [string[]]$AssemblyName = @(),
    [string[]]$CategoryName = @(),
    [string]$OutputPath = "artifacts/unity/editmode-direct-tests.xml",
    [int]$TimeoutMs = 120000,
    [string]$BaseUrl = "",
    [string]$Owner = "unity-mcp-editmode-tests",
    [int]$LockTimeoutSec = 0,
    [switch]$PreservePlayMode
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

$lock = $null
try {
    $lock = Enter-McpExclusiveOperation `
        -Name "unity-mcp-editmode-tests" `
        -Owner $Owner `
        -LockPath "Temp/UnityMcp/editmode-tests.lock" `
        -TimeoutSec $LockTimeoutSec

    $root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
    $root = $health.Root
    $stoppedPlayModeBeforeRun = $false
    $playStop = $null
    $compile = $null

    if ($health.State.isPlaying -or (Get-McpPlayModeChanging -State $health.State)) {
        if ($PreservePlayMode) {
            throw "Unity is in or entering Play Mode. EditMode tests require Edit Mode; rerun without -PreservePlayMode to stop Play Mode automatically."
        }

        Write-Host "Unity is in Play Mode; stopping before EditMode tests." -ForegroundColor Yellow
        $playStop = Invoke-McpPlayStopAndWait -Root $root -TimeoutSec 90 -PollSec 0.5
        $stoppedPlayModeBeforeRun = $true
        $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
        $root = $health.Root
    }

    if ($health.State.isCompiling) {
        Write-Host "Unity is compiling; waiting before EditMode tests." -ForegroundColor Yellow
        $compile = Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs $TimeoutMs
        $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
        $root = $health.Root
    }

    if ($health.State.isCompiling) {
        throw "Unity is still compiling after waiting. EditMode tests were not started."
    }

    $body = @{
        groupNames = @($GroupName)
        testNames = @($TestName)
        assemblyNames = @($AssemblyName)
        categoryNames = @($CategoryName)
        outputPath = $OutputPath
        timeoutMs = $TimeoutMs
        runSynchronously = $true
    }

    $result = Invoke-McpJsonWithTransientRetry `
        -Root $root `
        -SubPath "/tests/editmode/run" `
        -Body $body `
        -TimeoutSec ([Math]::Ceiling($TimeoutMs / 1000.0) + 30) `
        -RequestTimeoutSec ([Math]::Ceiling($TimeoutMs / 1000.0) + 15)

    $result | Add-Member -NotePropertyName stoppedPlayModeBeforeRun -NotePropertyValue $stoppedPlayModeBeforeRun -Force
    $result | Add-Member -NotePropertyName playStop -NotePropertyValue $playStop -Force
    $result | Add-Member -NotePropertyName compileWait -NotePropertyValue $compile -Force
    $result | ConvertTo-Json -Depth 10

    if (-not $result.success) {
        exit 2
    }
}
finally {
    Exit-McpExclusiveOperation -Lock $lock
}
