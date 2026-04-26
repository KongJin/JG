param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string]$TestFilter = "",
    [string]$UnityPath = "",
    [string]$ResultName = "unity-editmode-tests"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\WorkflowHelpers.ps1"

$preflight = Test-WorkflowUnityCliRunTestsPreflight -RepoRoot $RepoRoot
if (-not $preflight.Allowed) {
    $preflight | ConvertTo-Json -Depth 8
    exit 2
}

$unityExe = Resolve-WorkflowUnityExe -RepoRoot $RepoRoot -UnityPath $UnityPath
$artifactRoot = Join-Path $RepoRoot "artifacts\unity"
if (-not (Test-Path -LiteralPath $artifactRoot)) {
    New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
}

$safeName = if ([string]::IsNullOrWhiteSpace($ResultName)) { "unity-editmode-tests" } else { $ResultName }
$testResults = Join-Path $artifactRoot "$safeName.xml"
$logFile = Join-Path $artifactRoot "$safeName.log"

$arguments = @(
    "-projectPath", $RepoRoot,
    "-batchmode",
    "-nographics",
    "-runTests",
    "-testPlatform", "EditMode",
    "-testResults", $testResults,
    "-logFile", $logFile,
    "-quit"
)

if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $arguments += @("-testFilter", $TestFilter)
}

Write-Host ("RUN Unity EditMode tests: {0}" -f ($(if ([string]::IsNullOrWhiteSpace($TestFilter)) { "all" } else { $TestFilter }))) -ForegroundColor Cyan
Write-Host ("Unity: {0}" -f $unityExe)
Write-Host ("Results: {0}" -f $testResults)
Write-Host ("Log: {0}" -f $logFile)

& $unityExe @arguments
exit $LASTEXITCODE
