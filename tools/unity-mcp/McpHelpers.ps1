Set-StrictMode -Version Latest

function Get-UnityMcpBaseUrl {
    param([string]$ExplicitBaseUrl)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitBaseUrl)) {
        return $ExplicitBaseUrl.TrimEnd("/")
    }

    $portFile = Join-Path $PSScriptRoot "..\..\ProjectSettings\UnityMcpPort.txt"
    if (Test-Path -LiteralPath $portFile) {
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

function Wait-McpBridgeHealthy {
    param(
        [string]$Root,
        [int]$TimeoutSec = 60,
        [double]$PollSec = 0.5
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastError = $null

    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-McpGetJson -Root $Root -SubPath "/health"
            if ($null -ne $state -and $state.ok) {
                $sw.Stop()
                return @{
                    State = $state
                    ElapsedMs = $sw.ElapsedMilliseconds
                }
            }

            $lastError = "MCP /health returned ok=false."
        }
        catch {
            $lastError = $_.Exception.Message
        }

        Start-Sleep -Seconds $PollSec
    }

    $sw.Stop()
    if ([string]::IsNullOrWhiteSpace($lastError)) {
        $lastError = "Unknown bridge health failure."
    }

    throw "Unity MCP bridge did not become healthy within ${TimeoutSec}s. Last error: $lastError"
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

function Wait-McpPlayModeStopped {
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
            if (-not $state.isPlaying -and -not (Get-McpPlayModeChanging -State $state)) {
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
    throw "Play mode did not stop within ${TimeoutSec}s."
}

function Invoke-McpSceneOpenAndWait {
    param(
        [string]$Root,
        [string]$ScenePath,
        [bool]$SaveCurrentSceneIfDirty = $true,
        [int]$TimeoutSec = 60,
        [double]$PollSec = 0.5
    )

    $response = Invoke-McpJson -Root $Root -SubPath "/scene/open" -Body @{
        scenePath = $ScenePath
        saveCurrentSceneIfDirty = $SaveCurrentSceneIfDirty
    }

    $health = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec
    return [PSCustomObject]@{
        Response = $response
        Health = $health.State
        ElapsedMs = $health.ElapsedMs
    }
}

function Invoke-McpPlayStartAndWaitForBridge {
    param(
        [string]$Root,
        [int]$TimeoutSec = 60,
        [double]$PollSec = 0.5
    )

    $response = Invoke-McpJson -Root $Root -SubPath "/play/start"
    $health = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec
    return [PSCustomObject]@{
        Response = $response
        Health = $health.State
        ElapsedMs = $health.ElapsedMs
    }
}

function Invoke-McpPlayStopAndWait {
    param(
        [string]$Root,
        [int]$TimeoutSec = 90,
        [double]$PollSec = 0.5
    )

    $response = Invoke-McpJson -Root $Root -SubPath "/play/stop"
    $bridge = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec
    $stopped = Wait-McpPlayModeStopped -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec

    return [PSCustomObject]@{
        Response = $response
        Health = $bridge.State
        StoppedState = $stopped.State
        ElapsedMs = $bridge.ElapsedMs + $stopped.ElapsedMs
    }
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
