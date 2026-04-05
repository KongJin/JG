# Common helpers for Unity MCP UI smoke tests.

Set-StrictMode -Version Latest

function Get-UnityMcpBaseUrl {
    param([string]$ExplicitBaseUrl)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitBaseUrl)) {
        return $ExplicitBaseUrl.TrimEnd("/")
    }

    $portFile = Join-Path $PSScriptRoot "..\ProjectSettings\UnityMcpPort.txt"
    if (Test-Path $portFile) {
        $portText = (Get-Content -Path $portFile -Raw).Trim()
        if ($portText -match '^\d+$') {
            return "http://127.0.0.1:$portText"
        }
    }

    return "http://127.0.0.1:51234"
}

function Invoke-McpJson {
    param(
        [string]$Root,
        [string]$SubPath,
        [object]$Body = $null
    )

    $uri = "$Root$SubPath"
    if ($null -eq $Body) {
        return Invoke-RestMethod -Method Post -Uri $uri
    }

    $json = $Body | ConvertTo-Json -Compress
    return Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" -Body $json
}

function Invoke-McpGetJson {
    param(
        [string]$Root,
        [string]$SubPath
    )

    $uri = "$Root$SubPath"
    return Invoke-RestMethod -Method Get -Uri $uri
}

function Get-McpPlayModeChanging {
    param([object]$State)

    if ($null -ne $State -and $null -ne $State.PSObject.Properties["isPlayModeChanging"]) {
        return [bool]$State.isPlayModeChanging
    }

    if ($null -ne $State -and $null -ne $State.PSObject.Properties["isPlayingOrWillChange"]) {
        return [bool]$State.isPlayingOrWillChange
    }

    return $false
}

function Wait-McpPlayModeReady {
    param(
        [string]$Root,
        [int]$TimeoutSec,
        [double]$PollSec
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-McpGetJson -Root $Root -SubPath "/health"
            if ($state.isPlaying -and -not (Get-McpPlayModeChanging -State $state) -and -not $state.isCompiling) {
                $sw.Stop()
                return @{
                    State = $state
                    ElapsedMs = $sw.ElapsedMilliseconds
                }
            }
        }
        catch { }

        Start-Sleep -Seconds $PollSec
    }

    $sw.Stop()
    throw "Play mode did not stabilize within ${TimeoutSec}s."
}

function Wait-McpSceneActive {
    param(
        [string]$Root,
        [string]$SceneName,
        [int]$TimeoutSec,
        [double]$PollSec
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-McpGetJson -Root $Root -SubPath "/health"
            if ($state.activeScene -eq $SceneName) {
                $sw.Stop()
                return @{
                    State = $state
                    ElapsedMs = $sw.ElapsedMilliseconds
                }
            }
        }
        catch { }

        Start-Sleep -Seconds $PollSec
    }

    $sw.Stop()
    throw "Active scene did not become '${SceneName}' within ${TimeoutSec}s."
}

function Get-McpUiPathSpec {
    $spec = [ordered]@{}
    $spec["Lobby.CreateRoom"] = [PSCustomObject]@{
        Path = "/UIRoot/Canvas/lobby/RoomListView/Header/CreateRoomButton"
        FallbackName = "CreateRoomButton"
    }
    $spec["Lobby.Ready"] = [PSCustomObject]@{
        Path = "/UIRoot/Canvas/lobby/RoomDetailPanel/ReadyButton"
        FallbackName = "ReadyButton"
    }
    $spec["Lobby.StartGame"] = [PSCustomObject]@{
        Path = "/UIRoot/Canvas/lobby/RoomDetailPanel/StartGameButton"
        FallbackName = "StartGameButton"
    }
    $spec["Shared.ErrorModalDismiss"] = [PSCustomObject]@{
        Path = "/UIRoot/Canvas/ErrorModalRoot/Panel/DismissButton"
        FallbackName = "DismissButton"
    }
    $spec["Game.StartSkillButton0"] = [PSCustomObject]@{
        Path = "/UIRoot/StartSkillSelectionCanvas/Panel/ButtonGrid/SkillButton0"
        FallbackName = "SkillButton0"
    }
    $spec["Game.StartSkillButton1"] = [PSCustomObject]@{
        Path = "/UIRoot/StartSkillSelectionCanvas/Panel/ButtonGrid/SkillButton1"
        FallbackName = "SkillButton1"
    }
    $spec["Game.StartSkillConfirm"] = [PSCustomObject]@{
        Path = "/UIRoot/StartSkillSelectionCanvas/Panel/ConfirmButton"
        FallbackName = "ConfirmButton"
    }

    return $spec
}

function Get-McpUiPath {
    param([string]$Key)

    $spec = Get-McpUiPathSpec
    if (-not $spec.Contains($Key)) {
        throw "Unknown MCP UI key: $Key"
    }

    return $spec[$Key]
}

function Find-McpUiPathFallback {
    param(
        [string]$Root,
        [string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($Name)) {
        return $null
    }

    $result = Invoke-McpJson -Root $Root -SubPath "/gameobject/find" -Body @{
        name = $Name
        lightweight = $true
    }

    if ($result.found) {
        return $result.path
    }

    return $null
}

function Invoke-McpButton {
    param(
        [string]$Root,
        [string]$Path,
        [string]$FallbackName = "",
        [string]$Label = ""
    )

    $resolvedPath = $Path
    try {
        Invoke-McpJson -Root $Root -SubPath "/ui/button/invoke" -Body @{ path = $resolvedPath } | Out-Null
        return [PSCustomObject]@{
            path = $resolvedPath
            fallbackUsed = $false
            label = $Label
        }
    }
    catch {
        $initialMessage = $_.Exception.Message
        if ([string]::IsNullOrWhiteSpace($FallbackName)) {
            throw "Failed to invoke UI button '$Label' at '$Path'. $initialMessage"
        }

        $fallbackPath = Find-McpUiPathFallback -Root $Root -Name $FallbackName
        if ([string]::IsNullOrWhiteSpace($fallbackPath)) {
            throw "Failed to invoke UI button '$Label' at '$Path'. Fallback name '$FallbackName' not found. $initialMessage"
        }

        Invoke-McpJson -Root $Root -SubPath "/ui/button/invoke" -Body @{ path = $fallbackPath } | Out-Null
        return [PSCustomObject]@{
            path = $fallbackPath
            fallbackUsed = $true
            label = $Label
        }
    }
}

function Get-McpRecentLogs {
    param(
        [string]$Root,
        [int]$Limit = 80
    )

    return Invoke-McpGetJson -Root $Root -SubPath "/console/logs?limit=$Limit"
}

function Get-McpRecentErrors {
    param(
        [string]$Root,
        [int]$Limit = 20
    )

    return Invoke-McpGetJson -Root $Root -SubPath "/console/errors?limit=$Limit"
}

function Write-McpRecentConsole {
    param(
        [string]$Root,
        [string]$Label,
        [int]$LogLimit = 80,
        [int]$ErrorLimit = 20
    )

    Write-Host ""
    Write-Host "=== Console: $Label ===" -ForegroundColor Magenta

    try {
        $logs = Get-McpRecentLogs -Root $Root -Limit $LogLimit
        Write-Host "logs=$($logs.count)" -ForegroundColor Gray
        foreach ($item in $logs.items) {
            Write-Host ("[{0}] {1}" -f $item.type, $item.message)
        }
    }
    catch {
        Write-Host ("console/logs failed: {0}" -f $_.Exception.Message) -ForegroundColor Red
    }

    try {
        $errors = Get-McpRecentErrors -Root $Root -Limit $ErrorLimit
        Write-Host "errors=$($errors.count)" -ForegroundColor Gray
        foreach ($item in $errors.items) {
            Write-Host ("[ERROR] {0}" -f $item.message) -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host ("console/errors failed: {0}" -f $_.Exception.Message) -ForegroundColor Red
    }
}
