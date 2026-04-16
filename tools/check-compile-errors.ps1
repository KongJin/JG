# Unity 컴파일 에러/경고 확인 스크립트
# Editor.log 파일에서 컴파일 에러와 경고를 추출합니다

param(
    [string]$LogPath = "$env:LOCALAPPDATA\Unity\Editor\Editor.log",
    [switch]$IncludeWarnings = $true,
    [switch]$FullLog = $false
)

# 로그 파일 확인
if (-not (Test-Path $LogPath)) {
    Write-Host "Error: Log file not found: $LogPath" -ForegroundColor Red
    exit 1
}

$logContent = Get-Content $LogPath -Raw

# 에러 추출 (Assets\...cs(line,col): error CSxxxx: message)
$errorPattern = "^(Assets[^\(]+\(\d+,\d+\)): error (CS\d+): (.+)$"
$errors = [regex]::Matches($logContent, $errorPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)

# 경고 추출
$warningPattern = "^(Assets[^\(]+\(\d+,\d+\)): warning (CS\d+): (.+)$"
$warnings = [regex]::Matches($logContent, $warningPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)

# 결과 출력
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Unity Compile Errors & Warnings Check" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Log: $LogPath" -ForegroundColor Gray
Write-Host ""

# 에러 출력
$errorCount = $errors.Count
if ($errorCount -gt 0) {
    Write-Host "ERRORS ($errorCount):" -ForegroundColor Red
    Write-Host "----------------------------------------" -ForegroundColor DarkRed

    # 에러별 그룹화 (같은 위치의 에러는 최근 것만 표시)
    $errorGroups = @{}
    for ($i = $errors.Count - 1; $i -ge 0; $i--) {
        $location = $errors[$i].Groups[1].Value
        $code = $errors[$i].Groups[2].Value
        $message = $errors[$i].Groups[3].Value
        $key = "$location|$code"

        if (-not $errorGroups.ContainsKey($key)) {
            $errorGroups[$key] = @{
                Location = $location
                Code = $code
                Message = $message
            }
        }
    }

    foreach ($key in $errorGroups.Keys) {
        $e = $errorGroups[$key]
        Write-Host "  $($e.Location)" -ForegroundColor Yellow
        Write-Host "    [$($e.Code)] $($e.Message)" -ForegroundColor Red
    }
    Write-Host ""
} else {
    Write-Host "ERRORS: 0" -ForegroundColor Green
}

# 경고 출력
if ($IncludeWarnings) {
    $warningCount = $warnings.Count
    if ($warningCount -gt 0) {
        Write-Host "WARNINGS ($warningCount):" -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor DarkYellow

        # 경고별 그룹화
        $warningGroups = @{}
        for ($i = $warnings.Count - 1; $i -ge 0; $i--) {
            $location = $warnings[$i].Groups[1].Value
            $code = $warnings[$i].Groups[2].Value
            $message = $warnings[$i].Groups[3].Value
            $key = "$location|$code"

            if (-not $warningGroups.ContainsKey($key)) {
                $warningGroups[$key] = @{
                    Location = $location
                    Code = $code
                    Message = $message
                }
            }
        }

        # 최대 30개 경고만 표시
        $showCount = [Math]::Min(30, $warningGroups.Count)
        $idx = 0
        foreach ($key in $warningGroups.Keys) {
            if ($idx -ge $showCount) { break }
            $w = $warningGroups[$key]
            Write-Host "  $($w.Location)" -ForegroundColor DarkYellow
            Write-Host "    [$($w.Code)] $($w.Message)" -ForegroundColor DarkGray
            $idx++
        }

        if ($warningGroups.Count -gt 30) {
            Write-Host "  ... and $($warningGroups.Count - 30) more warnings" -ForegroundColor Gray
        }
        Write-Host ""
    } else {
        Write-Host "WARNINGS: 0" -ForegroundColor Green
    }
}

# 요약
Write-Host "========================================" -ForegroundColor Cyan
if ($errorCount -eq 0) {
    Write-Host "RESULT: SUCCESS - No compilation errors" -ForegroundColor Green
} else {
    Write-Host "RESULT: FAILED - $errorCount error(s) found" -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Cyan

# 종료 코드
exit $errorCount
