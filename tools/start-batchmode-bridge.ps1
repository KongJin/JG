# Unity MCP Batchmode Bridge Start Script
# Unity CLI에서 브릿지를 실행하여 컴파일 에러를 확인합니다

param(
    [string]$UnityPath,
    [string]$ProjectPath = "C:\Users\SOL\Documents\JG",
    [int]$Port = 51234,
    [switch]$ShowWindow
)

# 기본 Unity 경로
if ([string]::IsNullOrEmpty($UnityPath)) {
    $possiblePaths = @(
        "$env:PROGRAMFILES\Unity\Hub\Editor\*\Editor\Unity.exe",
        "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor\*\Editor\Unity.exe"
    )

    foreach ($pattern in $possiblePaths) {
        $found = Get-ChildItem $pattern -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
        if ($found) {
            $UnityPath = $found.FullName
            Write-Host "Found Unity: $UnityPath" -ForegroundColor Green
            break
        }
    }

    if ([string]::IsNullOrEmpty($UnityPath)) {
        Write-Host "Error: Unity.exe not found. Please specify -UnityPath parameter." -ForegroundColor Red
        exit 1
    }
}

# 프로젝트 경로 확인
if (-not (Test-Path $ProjectPath)) {
    Write-Host "Error: Project path not found: $ProjectPath" -ForegroundColor Red
    exit 1
}

# 로그 파일 경로
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$logPath = Join-Path $ProjectPath "Temp\batchmode-bridge-$timestamp.log"
$projectDir = Split-Path $ProjectPath -Parent

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Unity MCP Batchmode Bridge" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Unity: $UnityPath" -ForegroundColor Gray
Write-Host "Project: $ProjectPath" -ForegroundColor Gray
Write-Host "Log: $logPath" -ForegroundColor Gray
Write-Host "Port: $Port" -ForegroundColor Gray
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Unity CLI 인자
$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-executeMethod", "ProjectSD.EditorTools.UnityMcp.BatchmodeBridge.StartInBatchmode",
    "-logFile", $logPath
)

if (-not $ShowWindow) {
    $unityArgs += "-quit"  # 브릿지가 종료되면 Unity도 종료
}

Write-Host "Starting Unity..." -ForegroundColor Yellow

# Unity 프로세스 시작
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = $UnityPath
$processInfo.Arguments = $unityArgs -join " "
$processInfo.UseShellExecute = $ShowWindow
$processInfo.CreateNoWindow = -not $ShowWindow
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true
$processInfo.WorkingDirectory = $projectDir

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $processInfo
$process.Start() | Out-Null

# 로그 모니터링
Write-Host "Unity started. Monitoring logs..." -ForegroundColor Green
Write-Host "Press Ctrl+C to stop." -ForegroundColor Gray
Write-Host ""

$logReader = {
    param($logFile)
    if (Test-Path $logFile) {
        Get-Content $logFile -Wait -Tail 10 | ForEach-Object {
            if ($_ -match "\[BatchmodeBridge\]") {
                Write-Host $_ -ForegroundColor Cyan
            } elseif ($_ -match "\[Compile Error\]") {
                Write-Host $_ -ForegroundColor Red
            } elseif ($_ -match "\[Compile Warning\]") {
                Write-Host $_ -ForegroundColor Yellow
            } elseif ($_ -match "error CS") {
                Write-Host $_ -ForegroundColor Red
            } elseif ($_ -match "warning CS") {
                Write-Host $_ -ForegroundColor DarkYellow
            }
        }
    }
}

# 별도 작업으로 로그 모니터링 시작
$job = Start-Job -ScriptBlock $logReader -ArgumentList $logPath

# 브릿지가 준비될 때까지 대기
Write-Host "Waiting for bridge to be ready..." -ForegroundColor Yellow
$ready = $false
$maxWait = 60
$waited = 0

while (-not $ready -and $waited -lt $maxWait) {
    try {
        $response = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/health" -TimeoutSec 2 -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $ready = $true
            Write-Host "Bridge is ready at http://127.0.0.1:$Port" -ForegroundColor Green
        }
    } catch {
        # 대기 계속
    }
    Start-Sleep -Seconds 1
    $waited++
}

if (-not $ready) {
    Write-Host "Timeout waiting for bridge. Check log file: $logPath" -ForegroundColor Red
    Stop-Job $job
    Remove-Job $job
    exit 1
}

# 컴파일 상태 확인
Write-Host ""
Write-Host "Checking compilation status..." -ForegroundColor Yellow
$health = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/health" -UseBasicParsing

Write-Host "Bridge Health:" -ForegroundColor Green
Write-Host "  Running: $($health.ok)" -ForegroundColor Gray
Write-Host "  Compiling: $($health.isCompiling)" -ForegroundColor Gray
Write-Host "  Port: $($health.port)" -ForegroundColor Gray

# 최근 로그 확인
Write-Host ""
Write-Host "Recent logs:" -ForegroundColor Yellow
$logs = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/console/logs" -UseBasicParsing
$errorCount = ($logs | Where-Object { $_.type -eq 'Error' }).Count
$warningCount = ($logs | Where-Object { $_.type -eq 'Warning' }).Count

Write-Host "  Errors: $errorCount" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })
Write-Host "  Warnings: $warningCount" -ForegroundColor $(if ($warningCount -gt 0) { "Yellow" } else { "Gray" })

if ($errorCount -gt 0) {
    Write-Host ""
    Write-Host "Errors found:" -ForegroundColor Red
    $logs | Where-Object { $_.type -eq 'Error' } | Select-Object -First 10 | ForEach-Object {
        Write-Host "  $($_.message)" -ForegroundColor Red
    }
}

# 계속 실행 (사용자가 Ctrl+C로 종료할 때까지)
if ($ShowWindow) {
    Write-Host ""
    Write-Host "Bridge is running. Press Ctrl+C to stop..." -ForegroundColor Green
    try {
        while ($process -and !$process.HasExited) {
            Start-Sleep -Seconds 5
        }
    } catch [System.Management.Automation.PipelineStoppedException] {
        Write-Host "`nStopping bridge..." -ForegroundColor Yellow
    }
} else {
    # 자동 모드: 브릿지가 종료되면 종료
    $process.WaitForExit()
}

# 정리
Stop-Job $job
Remove-Job $job

$exitCode = $process.ExitCode
Write-Host ""
Write-Host "Unity exited with code: $exitCode" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Red" })
Write-Host "Log file: $logPath" -ForegroundColor Gray

exit $exitCode
