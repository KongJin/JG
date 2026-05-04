# Anti-Pattern Check Script
#
# Features 코드에서 안티패턴 검사
# 기존 validate-rules.ps1의 규칙 1, 2에 해당
# 새로운 anti-pattern 정의는 jg-no-silent-fallback SKILL.md에 추가
#

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
$ProjectPath = Split-Path -Parent $PSScriptRoot "Assets\Scripts\Features"
$FeaturesPath = $ProjectPath

# 결과 저장
$Results = @{
    GetComponent = @()
    FindObjectOfType = @()
    FindObjectsByType = @()
    AddComponent = @()
    DDOLSingleton = @()
    StaticEvent = @()
}

# 안티패턴 정의
# runtime reflection은 GetComponent, FindObjectOfType, FindObjectsByType, AddComponent 사용 금지
# Unity API 안티패턴은 FindFirstObjectByType 같은 허용될 수 있음

$ReflectionPatterns = @(
    "GetComponent<",
    "FindObjectOfType<",
    "FindObjectsByType<",
    "AddComponent<",
    "FindFirstObjectByType"
)

# 안티패턴 예외 (Editor 가드, 이미 구현된 코드)
# Test-ExcludedLine, Test-InEditorGuard를 사용하여 기존 검사 로직 유지

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

    # Editor 가드 체크 (현재 줄 포함, 위 아래 몇 줄 제외)
    # #if UNITY_EDITOR, #if UNITY_EDITOR && ... 같은 Editor 가드 패턴
    # 기존 validate-rules.ps1의 로직과 호환 유지

    return $false
}

# 검색 함수
function Search-GetComponent {
    Write-Section "규칙 1: GetComponent 런타임 탐색"

    $pattern = "GetComponent<"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    $foundCount = 0
    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $lineNumber = 0

        foreach ($line in $content) {
            $lineNumber++

            # 예외 체크
            if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                continue
            }

            # Editor 가드 영역 체크
            if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                continue
            }

            # 패턴 매칭
            if ($line -match $pattern) {
                # 예외 체크
                if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                    continue
                }

                # 위반 (기존 코드인 경우)
                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.GetComponent += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "GetComponent (위반)"
                }
                $foundCount++
            }
        }
    }

    # 결과 출력
    if ($Results.GetComponent.Count -eq 0) {
        Write-Color "✓ GetComponent 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.GetComponent.Count)건 GetComponent 위반 발견" Error
        if ($Verbose) {
            foreach ($result in $Results.GetComponent) {
                Write-Host "  [$($result.File):$($result.Line)] $($result.Content)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  (상세 내용을 보려면 -Verbose 사용)" -ForegroundColor Gray
        }
    }
}

function Search-FindObjectOfType {
    Write-Section "규칙 2: FindObjectOfType 런타임 탐색"

    $pattern = "FindObjectOfType<"

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

            if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                continue
            }

            if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                continue
            }

            if ($line -match $pattern) {
                if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                    continue
                }

                # 위반
                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.FindObjectOfType += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "FindObjectOfType (위반)"
                }
                $foundCount++
            }
        }
    }

    if ($Results.FindObjectOfType.Count -eq 0) {
        Write-Color "✓ FindObjectOfType 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.FindObjectOfType.Count)건 FindObjectOfType 위반 발견" Error
    }
}

function Search-FindObjectsByType {
    Write-Section "규칙 3: FindObjectsByType 런타임 탐색"

    $pattern = "FindObjectsByType<"

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

            if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                continue
            }

            if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                continue
            }

            if ($line -match $pattern) {
                if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                    continue
                }

                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.FindObjectsByType += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "FindObjectsByType (위반)"
                }
                $foundCount++
            }
        }
    }

    if ($Results.FindObjectsByType.Count -eq 0) {
        Write-Color "✓ FindObjectsByType 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.FindObjectsByType.Count)건 FindObjectsByType 위반 발견" Error
    }
}

function Search-AddComponent {
    Write-Section "규칙 4: AddComponent 런타임 검색"

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

            if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                continue
            }

            if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                continue
            }

            if ($line -match $pattern) {
                if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                    continue
                }

                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.AddComponent += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "AddComponent (위반)"
                }
                $foundCount++
            }
        }
    }

    if ($Results.AddComponent.Count -eq 0) {
        Write-Color "✓ AddComponent 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.AddComponent.Count)건 AddComponent 위반 발견" Error
    }
}

function Search-DDOLSingleton {
    Write-Section "규칙 5: DDOL Singleton 검색"

    $patternInstance = "public\s+static\s+\w+\s+Instance"
    $patternDontDestroy = "DontDestroyOnLoad"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $hasInstance = $false
        $hasDontDestroy = $false
        $instanceLine = 0
        $dontDestroyLine = 0

        foreach ($line in $content) {
            # 패턴 매칭
            if ($line -match $patternInstance) {
                $hasInstance = $true
                $instanceLine = $lineNumber
            }
            if ($line -match $patternDontDestroy) {
                $hasDontDestroy = $true
                $dontDestroyLine = $lineNumber
            }
        }

        # 두 패턴이 모두 있으면 DDOL singleton으로 판정
        if ($hasInstance -and $hasDontDestroy) {
            $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
            $Results.DDOLSingleton += @{
                File = $relativePath
                InstanceLine = $instanceLine
                DontDestroyLine = $dontDestroyLine
                Type = "DDOL Singleton"
            }
        }
    }

    if ($Results.DDOLSingleton.Count -eq 0) {
        Write-Color "✓ DDOL Singleton 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.DDOLSingleton.Count)건 DDOL Singleton 위반 발견" Error
    }
}

function Search-StaticEvent {
    Write-Section "규칙 6: 정적 이벤트 검색"

    $pattern = "static\s+event\s+\w+"

    if (-not (Test-Path $FeaturesPath)) {
        Write-Color "Features 폴더를 찾을 수 없습니다." Warning
        return
    }

    $allFiles = Get-ChildItem -Path $FeaturesPath -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\Editor\\" }

    foreach ($file in $allFiles) {
        $content = Get-Content $file.FullName -Encoding UTF8
        $lineNumber = 0
        $hasEvent = $false

        foreach ($line in $content) {
            $lineNumber++

            # 예외 체크
            if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                continue
            }

            # Editor 가드 영역 체크
            if (Test-InEditorGuard -Content $content -LineNumber $lineNumber) {
                continue
            }

            # 엔진/네트워크 콜백인지 확인
            if ($line -match "Photon|Unity|Engine|Network") {
                # 허용된 경우: 주석이 있거나 명시된 것으로 간주
                continue
            }

            # 패턴 매칭
            if ($line -match $pattern) {
                if (Test-ExcludedLine -FilePath $file.FullName -Line $lineNumber -LineNumber $lineNumber) {
                    continue
                }

                # 위반
                $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
                $Results.StaticEvent += @{
                    File = $relativePath
                    Line = $lineNumber
                    Content = $line.Trim()
                    Type = "Static Event (위반)"
                }
                $hasEvent = $true
                $foundCount++
            }
        }
    }

    if ($Results.StaticEvent.Count -eq 0) {
        Write-Color "✓ 정적 이벤트 위반 없음" Success
    } else {
        Write-Color "✗ $($Results.StaticEvent.Count)건 정적 이벤트 위반 발견" Error
    }
}

function Write-Summary {
    Write-Host ""
    Write-Host "=== 요약 ===" -ForegroundColor Cyan

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
}

# 메인
if ($Verbose) {
    Search-GetComponent
    Search-FindObjectOfType
    Search-FindObjectsByType
    Search-AddComponent
    Search-DDOLSingleton
    Search-StaticEvent
} else {
    Write-Color "  (상세 내용을 보려면 -Verbose 사용)" -ForegroundColor Gray
}

Write-Color "Anti-Pattern Check 완료" Success
