# Unity MCP: /scene/hierarchy 응답 안정성 진단 스크립트
# 목적: 같은 씬, 같은 depth, includeComponents 설정에 따라 자식 반환이 어떻게 달라지는지 기록
# 출력: Temp/UnityMcp/hierarchy-diag-results.json
# 실패 시 exit code 1 (누락 감지 시)

[CmdletBinding()]
param(
    [string]$BaseUrl,
    [string]$TargetScene = "Assets/Scenes/GameScene.unity",
    [string]$RootPath = "/UIRoot",
    [int]$MaxDepth = 12,
    [int]$PlayModeReadyTimeoutSec = 120,
    [double]$PlayModePollSec = 0.5
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "unity-mcp\McpHelpers.ps1")

$script:Results = @()
$script:IssuesFound = $false

function Get-DefaultResultPath {
    Join-Path $PSScriptRoot "..\Temp\UnityMcp\hierarchy-diag-results.json"
}

function Test-HierarchyResponse {
    param(
        [string]$Root,
        [string]$Path,
        [int]$Depth,
        [bool]$IncludeComponents
    )

    $icParam = if ($IncludeComponents) { "true" } else { "false" }
    $query = "/scene/hierarchy?depth=${Depth}&path=${Path}&includeComponents=${icParam}"
    $label = "depth=${Depth}, includeComponents=${icParam}, path=${Path}"

    Write-Host "Testing: $label" -ForegroundColor Cyan

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $resp = Invoke-McpGetJson -Root $Root -SubPath $query
        $sw.Stop()

        $nodeCount = if ($null -ne $resp.nodes) { $resp.nodes.Count } else { 0 }
        $hasChildren = $nodeCount -gt 0

        $result = [ordered]@{
            label = $label
            depth = $Depth
            path = $Path
            includeComponents = $IncludeComponents
            ok = $true
            nodeCount = $nodeCount
            hasChildren = $hasChildren
            responseTimeMs = $sw.ElapsedMilliseconds
            errorMessage = $null
        }

        # 자식 노드가 있어야 하는데 없는 경우 이슈로 기록
        if ($Path -eq $RootPath -and $Depth -ge 4 -and -not $hasChildren) {
            Write-Host "  ISSUE: No children returned for root path at depth=$Depth" -ForegroundColor Yellow
            $result.issue = "unexpected_empty_children"
            $script:IssuesFound = $true
        }
        else {
            Write-Host "  OK: $nodeCount nodes, ${sw.ElapsedMilliseconds}ms" -ForegroundColor Green
        }

        return $result
    }
    catch {
        $sw.Stop()
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red

        return [ordered]@{
            label = $label
            depth = $Depth
            path = $Path
            includeComponents = $IncludeComponents
            ok = $false
            nodeCount = 0
            hasChildren = $false
            responseTimeMs = $sw.ElapsedMilliseconds
            errorMessage = $_.Exception.Message
            issue = "request_failed"
        }
    }
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$resultPath = Get-DefaultResultPath

try {
    Write-Host "=== MCP Hierarchy Diagnostic ===" -ForegroundColor Magenta
    Write-Host "MCP: $root" -ForegroundColor Cyan
    Write-Host "Target scene: $TargetScene" -ForegroundColor Cyan
    Write-Host "Root path: $RootPath" -ForegroundColor Cyan

    # Pre-check: MCP health
    $h = Invoke-McpGetJson -Root $root -SubPath "/health"
    if (-not $h.ok) { throw "MCP /health not ok." }
    Write-Host "MCP bridge: OK (scene=$($h.activeScene))" -ForegroundColor Green

    # Enter play mode if not already
    if (-not $h.isPlaying) {
        Write-Host "Opening scene and starting play mode..." -ForegroundColor Yellow
        Invoke-McpSceneOpenAndWait -Root $root -ScenePath $TargetScene -SaveCurrentSceneIfDirty $true -TimeoutSec 60 -PollSec $PlayModePollSec | Out-Null
        Invoke-McpPlayStartAndWaitForBridge -Root $root -TimeoutSec $PlayModeReadyTimeoutSec -PollSec $PlayModePollSec | Out-Null

        Write-Host "Waiting for play mode ready..." -ForegroundColor Yellow
        Wait-McpPlayModeReady -Root $root -TimeoutSec $PlayModeReadyTimeoutSec -PollSec $PlayModePollSec | Out-Null
    }
    else {
        Write-Host "Already in play mode." -ForegroundColor Gray
    }

    # Wait for target scene
    Write-Host "Waiting for target scene active..." -ForegroundColor Yellow
    Wait-McpSceneActive -Root $root -SceneName ($TargetScene -replace 'Assets/Scenes/|\.unity') -TimeoutSec 60 -PollSec $PlayModePollSec | Out-Null

    Start-Sleep -Seconds 2
    Write-Host ""
    Write-Host "=== Running hierarchy tests ===" -ForegroundColor Magenta
    Write-Host ""

    # Test matrix: depth x includeComponents for root path
    $depths = @(2, 4, 6, 8, 10, 12)
    $includeFlags = @($true, $false)

    foreach ($depth in $depths) {
        foreach ($ic in $includeFlags) {
            $r = Test-HierarchyResponse -Root $root -Path $RootPath -Depth $depth -IncludeComponents $ic
            $script:Results += $r
            Start-Sleep -Milliseconds 200
        }
    }

    # Additional: test without path parameter (full scene)
    Write-Host ""
    Write-Host "=== Full scene hierarchy tests ===" -ForegroundColor Magenta
    Write-Host ""

    foreach ($depth in @(4, 8)) {
        foreach ($ic in $includeFlags) {
            $icParam = if ($ic) { "true" } else { "false" }
            $query = "/scene/hierarchy?depth=${depth}&includeComponents=${icParam}"
            $label = "full_scene, depth=${depth}, includeComponents=${ic}"

            Write-Host "Testing: $label" -ForegroundColor Cyan
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                $resp = Invoke-McpGetJson -Root $root -SubPath $query
                $sw.Stop()
                $nodeCount = if ($null -ne $resp.nodes) { $resp.nodes.Count } else { 0 }
                Write-Host "  OK: $nodeCount root nodes, ${sw.ElapsedMilliseconds}ms" -ForegroundColor Green
                $script:Results += [ordered]@{
                    label = $label
                    ok = $true
                    nodeCount = $nodeCount
                    responseTimeMs = $sw.ElapsedMilliseconds
                    errorMessage = $null
                    issue = $null
                }
            }
            catch {
                $sw.Stop()
                Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
                $script:Results += [ordered]@{
                    label = $label
                    ok = $false
                    nodeCount = 0
                    responseTimeMs = $sw.ElapsedMilliseconds
                    errorMessage = $_.Exception.Message
                    issue = "request_failed"
                }
                $script:IssuesFound = $true
            }
            Start-Sleep -Milliseconds 200
        }
    }

    # Summary
    Write-Host ""
    Write-Host "=== Summary ===" -ForegroundColor Magenta
    $okCount = ($script:Results | Where-Object { $_.ok }).Count
    $failCount = ($script:Results | Where-Object { -not $_.ok }).Count
    $issueCount = ($script:Results | Where-Object { $_.issue }).Count

    Write-Host "Total tests: $($script:Results.Count)" -ForegroundColor Cyan
    Write-Host "Passed: $okCount" -ForegroundColor Green
    Write-Host "Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
    Write-Host "Issues detected: $issueCount" -ForegroundColor $(if ($issueCount -gt 0) { "Yellow" } else { "Green" })

    # Write results JSON
    $dir = Split-Path -Parent $resultPath
    if (-not [string]::IsNullOrEmpty($dir) -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    $payload = [ordered]@{
        schemaVersion = 1
        finishedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        mcpBaseUrl = $root
        targetScene = $TargetScene
        rootPath = $RootPath
        totalTests = $script:Results.Count
        passed = $okCount
        failed = $failCount
        issuesDetected = $issueCount
        allPassed = (-not $script:IssuesFound) -and ($failCount -eq 0)
        results = @($script:Results)
    }

    $json = $payload | ConvertTo-Json -Depth 5
    Set-Content -Path $resultPath -Value $json -Encoding utf8
    Write-Host ""
    Write-Host "Results written to: $resultPath" -ForegroundColor Cyan

    if ($script:IssuesFound -or $failCount -gt 0) {
        Write-Host ""
        Write-Host "DIAGNOSTIC: Issues found. Review results JSON for details." -ForegroundColor Yellow
        Write-Host "This data should be used to fix /scene/hierarchy bridge in a follow-up task." -ForegroundColor Yellow
        exit 1
    }
    else {
        Write-Host ""
        Write-Host "DIAGNOSTIC: All tests passed. No issues detected." -ForegroundColor Green
    }
}
catch {
    Write-Host "FATAL: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
