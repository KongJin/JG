<#
.SYNOPSIS
    plans/*.md 상태를 스캔하고 progress.md와 비교하여 불일치를 보고합니다.
#>

$plansDir = "C:\Users\SOL\Documents\JG\docs\plans"
$progressFile = Join-Path $plansDir "progress.md"

# 상태별로 그룹화
$activePlans = @()
$referencePlans = @()
$historicalPlans = @()

# 각 plan 파일 읽기
$planFiles = Get-ChildItem -Path $plansDir -Filter "*.md" | Where-Object { $_.Name -ne "progress.md" }

foreach ($file in $planFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8

    if ($content -match '>\s*상태:\s*(\w+)') {
        $status = $matches[1].Trim()

        if ($status -eq "active") {
            $activePlans += $file.Name
        } elseif ($status -eq "reference") {
            $referencePlans += $file.Name
        } elseif ($status -eq "historical") {
            $historicalPlans += $file.Name
        }
    }
}

Write-Host "`n=== Plan Status Summary ===" -ForegroundColor Cyan

Write-Host "`nActive Plans ($($activePlans.Count)):" -ForegroundColor Green
foreach ($plan in $activePlans) {
    Write-Host "  - $plan"
}

Write-Host "`nReference Plans ($($referencePlans.Count)):" -ForegroundColor Yellow
foreach ($plan in $referencePlans) {
    Write-Host "  - $plan"
}

Write-Host "`nHistorical Plans ($($historicalPlans.Count)):" -ForegroundColor Gray
foreach ($plan in $historicalPlans) {
    Write-Host "  - $plan"
}

# Budget 체크
Write-Host "`n=== Budget Check ===" -ForegroundColor Cyan
Write-Host "Active plans: $($activePlans.Count) / Budget: 5" -ForegroundColor $(if ($activePlans.Count -le 5) { "Green" } else { "Red" })
if ($activePlans.Count -gt 5) {
    Write-Host "  WARNING: Active plan budget exceeded!" -ForegroundColor Red
}

# progress.md 분석
Write-Host "`n=== Progress.md Analysis ===" -ForegroundColor Cyan

if (Test-Path $progressFile) {
    $progressContent = Get-Content $progressFile -Raw -Encoding UTF8

    # progress.md에서 참조하는 owner docs 추출
    $referencedInProgress = @()

    # 현재 포커스 테이블에서 owner doc 링크 추출
    if ($progressContent -match 'game_scene_flow_validation_closeout_plan\.md') {
        $referencedInProgress += "game_scene_flow_validation_closeout_plan.md"
    }
    if ($progressContent -match 'audio_sfx_mcp_pipeline_plan\.md') {
        $referencedInProgress += "audio_sfx_mcp_pipeline_plan.md"
    }
    if ($progressContent -match 'webgl-audio-closeout\.md') {
        $referencedInProgress += "webgl-audio-closeout.md"
    }
    if ($progressContent -match 'nova1492-content-residual-plan\.md') {
        $referencedInProgress += "nova1492-content-residual-plan.md"
    }

    Write-Host "Progress.md에서 참조하는 owner plans: $($referencedInProgress.Count)" -ForegroundColor White
    foreach ($ref in $referencedInProgress) {
        Write-Host "  - $ref"
    }

    # 불일치 체크
    $missingInProgress = $activePlans | Where-Object { $_ -notin $referencedInProgress }
    $staleInProgress = $referencedInProgress | Where-Object { $_ -notin $activePlans }

    if ($missingInProgress.Count -gt 0) {
        Write-Host "`n  Active이지만 progress.md에서 누락된 plans:" -ForegroundColor Yellow
        foreach ($plan in $missingInProgress) {
            Write-Host "    - $plan"
        }
    }

    if ($staleInProgress.Count -gt 0) {
        Write-Host "`n  progress.md에 있지만 active가 아닌 plans:" -ForegroundColor Yellow
        foreach ($plan in $staleInProgress) {
            $actualStatus = if ($plan -in $referencePlans) { "reference" } elseif ($plan -in $historicalPlans) { "historical" } else { "unknown" }
            Write-Host "    - $plan (actual: $actualStatus)"
        }
    }

    if ($missingInProgress.Count -eq 0 -and $staleInProgress.Count -eq 0) {
        Write-Host "`n  Progress.md와 현재 상태가 일치합니다." -ForegroundColor Green
    }
} else {
    Write-Warning "Progress.md not found: $progressFile"
}

Write-Host "`n"
