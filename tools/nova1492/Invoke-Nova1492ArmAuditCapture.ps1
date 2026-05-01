param(
    [string]$UnityBridgeUrl,
    [string]$SourceRoot = "C:\Program Files (x86)\Nova1492",
    [string]$CatalogPath = "artifacts/nova1492/nova_part_catalog.csv",
    [switch]$RunAudit,
    [int]$TimeoutSec = 600
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$auditManifestPath = Join-Path $repoRoot "artifacts\nova1492\gx_arm_audit_manifest.csv"
$captureRoot = Join-Path $repoRoot "artifacts\nova1492\arm-captures"
$contactSheetPath = Join-Path $captureRoot "gx-arm-contact-sheet.png"
$manualReviewPath = Join-Path $repoRoot "artifacts\nova1492\gx_arm_manual_review.csv"
$captureReportPath = Join-Path $captureRoot "gx-arm-capture-report.md"
$menuPath = "Tools/Nova1492/Capture Arm Audit Contact Sheet"

. $PSScriptRoot\..\unity-mcp\McpHelpers.ps1

function Invoke-ArmAudit {
    $converterProject = Join-Path $repoRoot "tools\nova1492\GxObjConverter\GxObjConverter.csproj"
    $arguments = @(
        "run",
        "--project",
        $converterProject,
        "--",
        "--stage",
        "audit",
        "--source-root",
        $SourceRoot,
        "--catalog",
        $CatalogPath,
        "--category",
        "UnitParts/ArmWeapons"
    )

    Write-Host ("[Nova1492ArmAudit] dotnet {0}" -f ($arguments -join " ")) -ForegroundColor Cyan
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "GxObjConverter arm audit failed with exit code $LASTEXITCODE"
    }
}

function Wait-CaptureArtifacts {
    param([int]$TimeoutSeconds = 60)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if ((Test-Path -LiteralPath $contactSheetPath) -and
            (Test-Path -LiteralPath $manualReviewPath) -and
            (Test-Path -LiteralPath $captureReportPath)) {
            return
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Capture artifacts were not produced within ${TimeoutSeconds}s. Expected $contactSheetPath, $manualReviewPath, and $captureReportPath"
}

if ($RunAudit -or -not (Test-Path -LiteralPath $auditManifestPath)) {
    Invoke-ArmAudit
}

if (-not (Test-Path -LiteralPath $auditManifestPath)) {
    throw "Arm audit manifest not found after audit step: $auditManifestPath"
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 30
$root = $health.Root

Write-Host ("[Nova1492ArmAudit] Using Unity MCP bridge: {0}" -f $root) -ForegroundColor Cyan
Write-Host ("[Nova1492ArmAudit] scene={0} playing={1} compiling={2}" -f $health.State.activeScene, $health.State.isPlaying, $health.State.isCompiling) -ForegroundColor DarkGray

if ($health.State.isPlaying) {
    Invoke-McpPlayStopAndWait -Root $root -TimeoutSec 90 | Out-Null
}

$compile = Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs 120000
if (-not (Test-McpResponseSuccess -Response $compile)) {
    throw ("Compile wait failed before capture: {0}" -f ($compile | ConvertTo-Json -Compress -Depth 8))
}

$response = Invoke-McpJsonWithTransientRetry -Root $root -SubPath "/menu/execute" -Body @{
    menuPath = $menuPath
} -TimeoutSec $TimeoutSec -RequestTimeoutSec $TimeoutSec
if (-not (Test-McpResponseSuccess -Response $response)) {
    throw ("Arm capture menu failed: {0}" -f ($response | ConvertTo-Json -Compress -Depth 8))
}

Wait-CaptureArtifacts

[PSCustomObject]@{
    success = $true
    capture = @{
        method = "mcp"
        root = $root
        response = $response
    }
    auditManifestPath = "artifacts/nova1492/gx_arm_audit_manifest.csv"
    contactSheetPath = "artifacts/nova1492/arm-captures/gx-arm-contact-sheet.png"
    manualReviewPath = "artifacts/nova1492/gx_arm_manual_review.csv"
    captureReportPath = "artifacts/nova1492/arm-captures/gx-arm-capture-report.md"
    acceptance = "blocked: manual visual review pending"
} | ConvertTo-Json -Depth 8
