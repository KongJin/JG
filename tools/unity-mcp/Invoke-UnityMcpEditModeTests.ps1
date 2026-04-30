param(
    [string[]]$GroupName = @(),
    [string[]]$TestName = @(),
    [string[]]$AssemblyName = @(),
    [string[]]$CategoryName = @(),
    [string]$OutputPath = "artifacts/unity/editmode-direct-tests.xml",
    [int]$TimeoutMs = 120000,
    [string]$BaseUrl = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 60
$root = $health.Root

if ($health.State.isCompiling) {
    throw "Unity is compiling. Wait for compile to settle before running EditMode tests."
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

$result | ConvertTo-Json -Depth 8

if (-not $result.success) {
    exit 2
}
