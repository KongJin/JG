param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\WorkflowHelpers.ps1"

$result = Test-WorkflowUnityCliRunTestsPreflight -RepoRoot $RepoRoot
$result | ConvertTo-Json -Depth 8

if (-not $result.Allowed) {
    exit 2
}
