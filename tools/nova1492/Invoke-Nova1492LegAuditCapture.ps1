param(
    [string]$UnityBridgeUrl,
    [string]$SourceRoot = "C:\Program Files (x86)\Nova1492",
    [string]$CatalogPath = "artifacts/nova1492/nova_part_catalog.csv",
    [string]$UnityPath,
    [switch]$RunAudit,
    [int]$TimeoutSec = 600
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$auditManifestPath = Join-Path $repoRoot "artifacts\nova1492\gx_leg_audit_manifest.csv"
$captureRoot = Join-Path $repoRoot "artifacts\nova1492\leg-captures"
$contactSheetPath = Join-Path $captureRoot "gx-leg-contact-sheet.png"
$manualReviewPath = Join-Path $repoRoot "artifacts\nova1492\gx_leg_manual_review.csv"
$captureReportPath = Join-Path $captureRoot "gx-leg-capture-report.md"
$batchLogPath = Join-Path $captureRoot "unity-capture-batch.log"
$menuPath = "Tools/Nova1492/Capture Leg Audit Contact Sheet"

. $PSScriptRoot\..\unity-mcp\McpHelpers.ps1

function Resolve-UnityExecutable {
    param([string]$ExplicitUnityPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitUnityPath)) {
        if (-not (Test-Path -LiteralPath $ExplicitUnityPath)) {
            throw "Unity executable not found: $ExplicitUnityPath"
        }

        return $ExplicitUnityPath
    }

    $projectVersionPath = Join-Path $repoRoot "ProjectSettings\ProjectVersion.txt"
    if (Test-Path -LiteralPath $projectVersionPath) {
        $versionLine = Get-Content -LiteralPath $projectVersionPath | Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } | Select-Object -First 1
        if ($versionLine -match '^m_EditorVersion:\s*(.+)$') {
            $candidate = Join-Path "C:\Program Files\Unity\Hub\Editor" (Join-Path $Matches[1] "Editor\Unity.exe")
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    $latest = Get-ChildItem "C:\Program Files\Unity\Hub\Editor" -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        Select-Object -First 1
    if ($null -ne $latest) {
        $candidate = Join-Path $latest.FullName "Editor\Unity.exe"
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "Unity executable could not be resolved. Pass -UnityPath explicitly."
}

function Invoke-LegAudit {
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
        "UnitParts/Legs"
    )

    Write-Host ("[Nova1492LegAudit] dotnet {0}" -f ($arguments -join " ")) -ForegroundColor Cyan
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "GxObjConverter audit failed with exit code $LASTEXITCODE"
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

function Invoke-CaptureViaMcp {
    $root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
    $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 30
    $root = $health.Root

    Write-Host ("[Nova1492LegAudit] Using Unity MCP bridge: {0}" -f $root) -ForegroundColor Cyan
    Write-Host ("[Nova1492LegAudit] scene={0} playing={1} compiling={2}" -f $health.State.activeScene, $health.State.isPlaying, $health.State.isCompiling) -ForegroundColor DarkGray

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
        throw ("Capture menu failed: {0}" -f ($response | ConvertTo-Json -Compress -Depth 8))
    }

    Wait-CaptureArtifacts
    return [PSCustomObject]@{
        method = "mcp"
        root = $root
        response = $response
    }
}

function Invoke-CaptureViaBatchMode {
    $unity = Resolve-UnityExecutable -ExplicitUnityPath $UnityPath
    New-Item -ItemType Directory -Force -Path $captureRoot | Out-Null

    $arguments = @(
        "-batchmode",
        "-quit",
        "-projectPath",
        $repoRoot.Path,
        "-executeMethod",
        "ProjectSD.EditorTools.Nova1492LegAuditCaptureTool.CaptureLegAuditContactSheet",
        "-logFile",
        $batchLogPath
    )

    Write-Host ("[Nova1492LegAudit] {0} {1}" -f $unity, ($arguments -join " ")) -ForegroundColor Cyan
    $output = @(& $unity @arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $combined = ($output -join "`n") + "`n" + $(if (Test-Path -LiteralPath $batchLogPath) { Get-Content -LiteralPath $batchLogPath -Raw } else { "" })

    if ($exitCode -eq 0) {
        Wait-CaptureArtifacts
        return [PSCustomObject]@{
            method = "batchmode"
            unityPath = $unity
            exitCode = $exitCode
        }
    }

    if ($combined -match "another Unity instance is running|Multiple Unity instances cannot open the same project|project open") {
        return [PSCustomObject]@{
            method = "batchmode-blocked-project-open"
            unityPath = $unity
            exitCode = $exitCode
            message = "Project is already open; use MCP fallback."
        }
    }

    throw "Unity batchmode capture failed with exit code $exitCode. See $batchLogPath"
}

if ($RunAudit -or -not (Test-Path -LiteralPath $auditManifestPath)) {
    Invoke-LegAudit
}

if (-not (Test-Path -LiteralPath $auditManifestPath)) {
    throw "Audit manifest not found after audit step: $auditManifestPath"
}

$captureResult = $null
try {
    $captureResult = Invoke-CaptureViaMcp
}
catch {
    Write-Warning ("MCP capture failed, trying batchmode: {0}" -f $_.Exception.Message)
    $batchResult = Invoke-CaptureViaBatchMode
    if ($batchResult.method -eq "batchmode-blocked-project-open") {
        Write-Warning "Batchmode was blocked because the project is open; retrying MCP after port refresh."
        $captureResult = Invoke-CaptureViaMcp
    }
    else {
        $captureResult = $batchResult
    }
}

[PSCustomObject]@{
    success = $true
    capture = $captureResult
    auditManifestPath = "artifacts/nova1492/gx_leg_audit_manifest.csv"
    contactSheetPath = "artifacts/nova1492/leg-captures/gx-leg-contact-sheet.png"
    manualReviewPath = "artifacts/nova1492/gx_leg_manual_review.csv"
    captureReportPath = "artifacts/nova1492/leg-captures/gx-leg-capture-report.md"
    acceptance = "blocked: manual visual review pending"
} | ConvertTo-Json -Depth 8
