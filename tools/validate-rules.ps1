# Unity 코딩 규칙 검증 스크립트
#
# 사용법: .\tools\validate-rules.ps1
#
# 검증하는 규칙:
# - 런타임 탐색 금지 (GetComponent, FindObjectOfType, AddComponent)
# - DDOL singleton (Features/**) 금지
# - 정적 이벤트 사용 확인
#
# 예외:
# - #if UNITY_EDITOR 가드 코드
# - Assets/Editor/** 폴더
# - Assets/FromStore/** 외부 라이브러리

param(
    [switch]$Verbose,
    [switch]$Fix
)

$ErrorActionPreference = "Stop"

# 색상 정의
$Colors = @{
    Error = "Red"
    Warning = "Yellow"
    Success = "Green"
    Info = "Cyan"
}

# 프로젝트 경로
$ProjectPath = Split-Path -Parent $PSScriptRoot
$FeaturesPath = Join-Path $ProjectPath "Assets\Scripts\Features"
$SharedPath = Join-Path $ProjectPath "Assets\Scripts\Shared"

# 결과 저장
$Results = @{
    GetComponent = @()
    FindObjectOfType = @()
    FindObjectsByType = @()
    AddComponent = @()
    DDOLSingleton = @()
    StaticEvent = @()
}

# 도움말 함수
function Write-Color {
    param([string]$Text, [string]$Color = "White")
    Write-Host $Text -ForegroundColor $Colors[$Color] -NoNewline
    Write-Host
}

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
}

# 예외 체크 함수
function Test-ExcludedLine {
    param(
        [string]$FilePath,
        [string]$Line,
        [int]$LineNumber
    )

    # 경로 기반 예외
    if ($FilePath -match "Assets\\Editor\\") { return $true }
    if ($FilePath -match "Assets\\FromStore\\") { return $true }

    # Editor 가드 체크 (현재 줄 포함, 위 아래 몇 줄도 확인)
    # Editor 가드 패턴: #if UNITY_EDITOR, #if UNITY_EDITOR && ... 등
    if ($Line -match "#if\s+UNITY_EDITOR") { return $true }

    return $false
}

# Editor 가드 영역 체크 (파일 전체 스캔)
function Test-InEditorGuard {
    param(
        [string[]]$Content,
        [int]$LineNumber
    )

    # Editor 가드 범위 추적
    $inEditorGuard = $false

    for ($i = 0; $i -lt $Content.Count; $i++) {
        $line = $Content[$i].Trim()

        # #if UNITY_EDITOR 시작
        if ($line -match "#if\s+UNITY_EDITOR") {
            $inEditorGuard = $true
            continue
        }

        # #elif에서도 유지
        if ($inEditorGuard -and $line -match "#elif") {
            continue
        }

        # #endif로 끝
        if ($inEditorGuard -and $line -match "#endif") {
            $inEditorGuard = $false
            continue
        }

        # 현재 줄이 Editor 가드 안에 있는지
        if ($i -eq ($LineNumber - 1) -and $inEditorGuard) {
            return $true
        }
    }

    return $false
}

# 규칙 1: GetComponent 검색
function Search-GetComponent {
    Write-Section "규칙 1: GetComponent 런타임 탐색"

    $pattern = "GetComponent<"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $lineNumber = 0

        foreach ($line in $content) {
            $lineNumber++

            if ($line -match $pattern) {
                # 예외 체크
                if (Test-ExcludedLine -FilePath $file.FullName -Line $line -LineNumber $lineNumber) {
                    continue
                }

                # Editor 가드 영역 체크
                if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                    continue
                }

                # GetComponentInChildren는 허용 (동일 GameObject 내부 helper 탐색)
                if ($line -match "GetComponentInChildren") {
                    # 하지만 의존성 획득 목적이 아닌지 주석 확인 필요
                    if (-not $line -match "//.*helper" -and -not $line -match "//.*allowed") {
                        $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                        $Results.GetComponent += @{
                            File = $relativePath
                            Line = $lineNumber
                            Content = $line.Trim()
                            Type = "GetComponentInChildren (확인 필요)"
                        }
                    }
                    continue
                }

                # 위반
                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.GetComponent += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "GetComponent"
                }
            }
        }
    }

    if ($Results.GetComponent.Count -eq 0) {
        Write-Color "✓ GetComponent 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.GetComponent.Count)건 GetComponent 위반 발견" Error
        if ($Verbose) {
            foreach ($result in $Results.GetComponent) {
                Write-Host "  [$($result.File):$($result.Line)] $($result.Type)" -ForegroundColor Yellow
                Write-Host "    $($result.Content)" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "  (상세 내용을 보려면 -Verbose 사용)" -ForegroundColor Gray
        }
    }
}

# 규칙 2: FindObjectOfType 검색
function Search-FindObjectOfType {
    Write-Section "규칙 2: FindObjectOfType 런타임 탐색"

    $pattern = "FindObjectOfType"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $lineNumber = 0

        foreach ($line in $content) {
            $lineNumber++

            if ($line -match $pattern) {
                # 예외 체크
                if (Test-ExcludedLine -FilePath $file.FullName -Line $line -LineNumber $lineNumber) {
                    continue
                }

                # Editor 가드 영역 체크
                if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                    continue
                }

                # 위반
                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.FindObjectOfType += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "FindObjectOfType"
                }
            }
        }
    }

    if ($Results.FindObjectOfType.Count -eq 0) {
        Write-Color "✓ FindObjectOfType 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.FindObjectOfType.Count)건 FindObjectOfType 위반 발견" Error
        if ($Verbose) {
            foreach ($result in $Results.FindObjectOfType) {
                Write-Host "  [$($result.File):$($result.Line)]" -ForegroundColor Yellow
                Write-Host "    $($result.Content)" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "  (상세 내용을 보려면 -Verbose 사용)" -ForegroundColor Gray
        }
    }
}

# 규칙 3: FindObjectsByType 검색
function Search-FindObjectsByType {
    Write-Section "규칙 3: FindObjectsByType 런타임 탐색"

    $pattern = "FindObjectsByType"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $lineNumber = 0

        foreach ($line in $content) {
            $lineNumber++

            if ($line -match $pattern) {
                # 예외 체크
                if (Test-ExcludedLine -FilePath $file.FullName -Line $line -LineNumber $lineNumber) {
                    continue
                }

                # Editor 가드 영역 체크
                if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                    continue
                }

                # 위반
                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.FindObjectsByType += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "FindObjectsByType"
                }
            }
        }
    }

    if ($Results.FindObjectsByType.Count -eq 0) {
        Write-Color "✓ FindObjectsByType 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.FindObjectsByType.Count)건 FindObjectsByType 위반 발견" Error
        if ($Verbose) {
            foreach ($result in $Results.FindObjectsByType) {
                Write-Host "  [$($result.File):$($result.Line)]" -ForegroundColor Yellow
                Write-Host "    $($result.Content)" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "  (상세 내용을 보려면 -Verbose 사용)" -ForegroundColor Gray
        }
    }
}

# 규칙 4: AddComponent 검색
function Search-AddComponent {
    Write-Section "규칙 4: AddComponent 런타임 추가"

    $pattern = "AddComponent<"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $lineNumber = 0

        foreach ($line in $content) {
            $lineNumber++

            if ($line -match $pattern) {
                # 예외 체크
                if (Test-ExcludedLine -FilePath $file.FullName -Line $line -LineNumber $lineNumber) {
                    continue
                }

                # Editor 가드 영역 체크
                if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                    continue
                }

                # 위반
                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.AddComponent += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "AddComponent"
                }
            }
        }
    }

    if ($Results.AddComponent.Count -eq 0) {
        Write-Color "✓ AddComponent 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.AddComponent.Count)건 AddComponent 위반 발견" Error
        if ($Verbose) {
            foreach ($result in $Results.AddComponent) {
                Write-Host "  [$($result.File):$($result.Line)]" -ForegroundColor Yellow
                Write-Host "    $($result.Content)" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "  (상세 내용을 보려면 -Verbose 사용)" -ForegroundColor Gray
        }
    }
}

# 규칙 5: DDOL Singleton 검색
function Search-DDOLSingleton {
    Write-Section "규칙 5: DDOL Singleton (Features/** 금지)"

    $patternInstance = "public\s+static\s+\w+\s+Instance"
    $patternDontDestroy = "DontDestroyOnLoad"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $lineNumber = 0
        $hasInstance = $false
        $hasDontDestroy = $false
        $instanceLine = 0
        $dontDestroyLine = 0

        foreach ($line in $content) {
            $lineNumber++

            if ($line -match $patternInstance) {
                $hasInstance = $true
                $instanceLine = $lineNumber
            }

            if ($line -match $patternDontDestroy) {
                $hasDontDestroy = $true
                $dontDestroyLine = $lineNumber
            }
        }

        # 두 패턴이 모두 있으면 DDOL singleton으로 간주
        if ($hasInstance -and $hasDontDestroy) {
            $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
            $Results.DDOLSingleton += @{
                File = $relativePath
                InstanceLine = $instanceLine
                DontDestroyLine = $dontDestroyLine
            }
        }
    }

    if ($Results.DDOLSingleton.Count -eq 0) {
        Write-Color "✓ DDOL Singleton 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.DDOLSingleton.Count)건 DDOL Singleton 발견 (Features/**에 금지됨)" Error
        if ($Verbose) {
            foreach ($result in $Results.DDOLSingleton) {
                Write-Host "  [$($result.File)]" -ForegroundColor Yellow
                Write-Host "    Instance: Line $($result.InstanceLine)" -ForegroundColor DarkGray
                Write-Host "    DontDestroyOnLoad: Line $($result.DontDestroyLine)" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "  (상세 내용을 보려면 -Verbose 사용)" -ForegroundColor Gray
        }
    }
}

# 규칙 6: 정적 이벤트 검색
function Search-StaticEvent {
    Write-Section "규칙 6: 정적 이벤트 (gameplay event bus 대체제 사용 금지)"

    $pattern = "static\s+event\s+\w+"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $lineNumber = 0

        foreach ($line in $content) {
            $lineNumber++

            if ($line -match $pattern) {
                # 예외 체크
                if (Test-ExcludedLine -FilePath $file.FullName -Line $line -LineNumber $lineNumber) {
                    continue
                }

                # Editor 가드 영역 체크
                if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                    continue
                }

                # 엔진/네트워크 콜백인지 확인 (예: PhotonEvent, UnityEvent 등)
                if ($line -match "Photon|Unity|Engine|Network") {
                    # 허용 가능한 경우: 주석이 있거나 명시된 콜백
                    $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                    $Results.StaticEvent += @{
                        File = $relativePath
                        Line = $lineNumber
                        Content = $line.Trim()
                        Type = "Static Event (엔진/네트워크 - 확인 필요)"
                    }
                } else {
                    # 위반
                    $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                    $Results.StaticEvent += @{
                        File = $relativePath
                        Line = $lineNumber
                        Content = $line.Trim()
                        Type = "Static Event (gameplay bus 대체제 금지)"
                    }
                }
            }
        }
    }

    if ($Results.StaticEvent.Count -eq 0) {
        Write-Color "✓ 정적 이벤트 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.StaticEvent.Count)건 정적 이벤트 발견" Error
        if ($Verbose) {
            foreach ($result in $Results.StaticEvent) {
                Write-Host "  [$($result.File):$($result.Line)] $($result.Type)" -ForegroundColor Yellow
                Write-Host "    $($result.Content)" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "  (상세 내용을 보려면 -Verbose 사용)" -ForegroundColor Gray
        }
    }
}

# 요약 출력
function Write-Summary {
    Write-Section "요약"

    $totalCount = $Results.GetComponent.Count +
                  $Results.FindObjectOfType.Count +
                  $Results.FindObjectsByType.Count +
                  $Results.AddComponent.Count +
                  $Results.DDOLSingleton.Count +
                  $Results.StaticEvent.Count

    Write-Host "총 위반: $totalCount건" -ForegroundColor $(if ($totalCount -eq 0) { "Green" } else { "Red" })
    Write-Host "  - GetComponent: $($Results.GetComponent.Count)" -ForegroundColor $(if ($Results.GetComponent.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  - FindObjectOfType: $($Results.FindObjectOfType.Count)" -ForegroundColor $(if ($Results.FindObjectOfType.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  - FindObjectsByType: $($Results.FindObjectsByType.Count)" -ForegroundColor $(if ($Results.FindObjectsByType.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  - AddComponent: $($Results.AddComponent.Count)" -ForegroundColor $(if ($Results.AddComponent.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  - DDOL Singleton: $($Results.DDOLSingleton.Count)" -ForegroundColor $(if ($Results.DDOLSingleton.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host "  - Static Event: $($Results.StaticEvent.Count)" -ForegroundColor $(if ($Results.StaticEvent.Count -eq 0) { "Green" } else { "Yellow" })
    Write-Host ""

    if ($totalCount -gt 0) {
        Write-Host "상세 내용을 보려면: .\tools\validate-rules.ps1 -Verbose" -ForegroundColor Cyan
    }
}

# 메인 실행
Write-Color "Unity 코딩 규칙 검증 스크립트" Info
Write-Host "프로젝트: $ProjectPath"
Write-Host ""

# 검색 경로 확인
if (-not (Test-Path $FeaturesPath)) {
    Write-Color "Features 폴더를 찾을 수 없습니다: $FeaturesPath" Error
    exit 1
}

# 규칙 실행
Search-GetComponent
Search-FindObjectOfType
Search-FindObjectsByType
Search-AddComponent
Search-DDOLSingleton
Search-StaticEvent

# 요약
Write-Summary

# 종료 코드 = 위반 수
$exitCode = $Results.GetComponent.Count +
            $Results.FindObjectOfType.Count +
            $Results.FindObjectsByType.Count +
            $Results.AddComponent.Count +
            $Results.DDOLSingleton.Count +
            $Results.StaticEvent.Count

exit $exitCode
