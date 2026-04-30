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

function Test-UnityMcpRootCanFollowProjectPort {
    param([string]$Root)

    if ([string]::IsNullOrWhiteSpace($Root)) {
        return $false
    }

    return $Root -match '^https?://(127\.0\.0\.1|localhost):\d+/?$'
}

function Get-UnityMcpRefreshedRoot {
    param([string]$Root)

    if (-not (Test-UnityMcpRootCanFollowProjectPort -Root $Root)) {
        return $Root
    }

    $freshRoot = Get-UnityMcpBaseUrl -ExplicitBaseUrl ""
    if ([string]::IsNullOrWhiteSpace($freshRoot)) {
        return $Root
    }

    return $freshRoot
}

function Invoke-McpJson {
    param(
        [string]$Root,
        [string]$SubPath,
        [object]$Body = $null,
        [int]$TimeoutSec = 0
    )

    $uri = "$Root$SubPath"
    $request = @{
        Method = "Post"
        Uri = $uri
    }
    if ($TimeoutSec -gt 0) {
        $request.TimeoutSec = $TimeoutSec
    }

    if ($null -eq $Body) {
        return Invoke-RestMethod @request
    }

    $json = $Body | ConvertTo-Json -Compress
    $request.ContentType = "application/json"
    $request.Body = [System.Text.Encoding]::UTF8.GetBytes($json)
    return Invoke-RestMethod @request
}

function Invoke-McpGetJson {
    param(
        [string]$Root,
        [string]$SubPath,
        [int]$TimeoutSec = 0
    )

    $uri = "$Root$SubPath"
    $request = @{
        Method = "Get"
        Uri = $uri
    }
    if ($TimeoutSec -gt 0) {
        $request.TimeoutSec = $TimeoutSec
    }

    return Invoke-RestMethod @request
}

function Invoke-McpJsonWithTransientRetry {
    param(
        [string]$Root,
        [string]$SubPath,
        [object]$Body = $null,
        [int]$TimeoutSec = 60,
        [int]$RequestTimeoutSec = 15,
        [double]$PollSec = 0.5
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastError = $null

    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-McpJson -Root $Root -SubPath $SubPath -Body $Body -TimeoutSec $RequestTimeoutSec
            if ($null -eq $response -or ($response -is [string] -and [string]::IsNullOrWhiteSpace($response))) {
                $lastError = "MCP POST ${SubPath} returned an empty response."
                Start-Sleep -Seconds $PollSec
                continue
            }

            return $response
        }
        catch {
            if (-not (Test-McpTransientConnectionFailure -Exception $_.Exception)) {
                throw
            }

            $lastError = $_.Exception.Message
            Start-Sleep -Seconds $PollSec
        }
    }

    if ([string]::IsNullOrWhiteSpace($lastError)) {
        $lastError = "Unknown transient POST failure."
    }

    throw "Unity MCP POST ${SubPath} did not recover within ${TimeoutSec}s. Last error: $lastError"
}

function Invoke-McpGetJsonWithTransientRetry {
    param(
        [string]$Root,
        [string]$SubPath,
        [int]$TimeoutSec = 60,
        [int]$RequestTimeoutSec = 15,
        [double]$PollSec = 0.5
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastError = $null

    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-McpGetJson -Root $Root -SubPath $SubPath -TimeoutSec $RequestTimeoutSec
            if ($null -eq $response -or ($response -is [string] -and [string]::IsNullOrWhiteSpace($response))) {
                $lastError = "MCP GET ${SubPath} returned an empty response."
                Start-Sleep -Seconds $PollSec
                continue
            }

            return $response
        }
        catch {
            if (-not (Test-McpTransientConnectionFailure -Exception $_.Exception)) {
                throw
            }

            $lastError = $_.Exception.Message
            Start-Sleep -Seconds $PollSec
        }
    }

    if ([string]::IsNullOrWhiteSpace($lastError)) {
        $lastError = "Unknown transient GET failure."
    }

    throw "Unity MCP GET ${SubPath} did not recover within ${TimeoutSec}s. Last error: $lastError"
}

function Test-McpTransientConnectionFailure {
    param([System.Exception]$Exception)

    if ($null -eq $Exception) {
        return $false
    }

    $message = $Exception.Message
    return $message -match "connect|connection|actively refused|forcibly closed|reset by peer|No connection could be made|timed out|timeout"
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
    $currentRoot = $Root

    while ((Get-Date) -lt $deadline) {
        try {
            $state = Invoke-McpGetJson -Root $currentRoot -SubPath "/health" -TimeoutSec 10
            if ($null -ne $state -and $state.ok) {
                $sw.Stop()
                return @{
                    State = $state
                    Root = $currentRoot
                    ElapsedMs = $sw.ElapsedMilliseconds
                }
            }

            $lastError = "MCP /health returned ok=false."
        }
        catch {
            $lastError = $_.Exception.Message
            $refreshedRoot = Get-UnityMcpRefreshedRoot -Root $currentRoot
            if ($refreshedRoot -ne $currentRoot) {
                $currentRoot = $refreshedRoot
            }
        }

        Start-Sleep -Seconds $PollSec
    }

    $sw.Stop()
    if ([string]::IsNullOrWhiteSpace($lastError)) {
        $lastError = "Unknown bridge health failure."
    }

    throw "Unity MCP bridge did not become healthy within ${TimeoutSec}s. Last error: $lastError"
}

function Invoke-McpTransitionWait {
    param(
        [string]$Root,
        [string]$SubPath,
        [int]$TimeoutSec = 60,
        [double]$PollSec = 0.5
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastError = $null

    while ((Get-Date) -lt $deadline) {
        try {
            return Invoke-McpJson -Root $Root -SubPath $SubPath
        }
        catch {
            if (-not (Test-McpTransientConnectionFailure -Exception $_.Exception)) {
                throw
            }

            $lastError = $_.Exception.Message
            Start-Sleep -Seconds $PollSec
        }
    }

    if ([string]::IsNullOrWhiteSpace($lastError)) {
        $lastError = "Unknown transition wait failure."
    }

    throw "Unity MCP transition wait failed for ${SubPath} within ${TimeoutSec}s. Last error: $lastError"
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

function Invoke-McpCompileRequestAndWait {
    param(
        [string]$Root,
        [switch]$CleanBuildCache,
        [int]$TimeoutMs = 120000,
        [int]$PollIntervalMs = 250
    )

    $currentRoot = $Root

    $request = Invoke-McpJsonWithTransientRetry -Root $currentRoot -SubPath "/compile/request" -Body @{
        cleanBuildCache = [bool]$CleanBuildCache
    } -TimeoutSec ([Math]::Ceiling($TimeoutMs / 1000.0))

    $bridgeHealth = Wait-McpBridgeHealthy -Root $currentRoot -TimeoutSec ([Math]::Ceiling($TimeoutMs / 1000.0))
    if ($null -ne $bridgeHealth.Root) {
        $currentRoot = [string]$bridgeHealth.Root
    }

    $wait = Invoke-McpJsonWithTransientRetry -Root $currentRoot -SubPath "/compile/wait" -Body @{
        timeoutMs = $TimeoutMs
        pollIntervalMs = $PollIntervalMs
        requestFirst = $false
        cleanBuildCache = $false
    } -TimeoutSec ([Math]::Ceiling($TimeoutMs / 1000.0))

    $postCompileHealth = Wait-McpBridgeHealthy -Root $currentRoot -TimeoutSec ([Math]::Ceiling($TimeoutMs / 1000.0))

    return [PSCustomObject]@{
        Request = $request
        Wait = $wait
        HealthAfterWait = $postCompileHealth.State
    }
}

function Invoke-McpPlayStartAndWaitForBridge {
    param(
        [string]$Root,
        [int]$TimeoutSec = 60,
        [double]$PollSec = 0.5
    )

    $response = $null
    try {
        $response = Invoke-McpJson -Root $Root -SubPath "/play/start"
    }
    catch {
        if ($_.Exception.Message -notmatch "connect") {
            throw
        }
    }

    $health = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec
    $wait = Invoke-McpTransitionWait -Root $Root -SubPath "/play/wait-for-play" -TimeoutSec $TimeoutSec -PollSec $PollSec
    $ready = Wait-McpPlayModeReady -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec
    return [PSCustomObject]@{
        Response = $response
        Wait = $wait
        Health = $health.State
        ReadyState = $ready.State
        ElapsedMs = $health.ElapsedMs + $ready.ElapsedMs
    }
}

function Invoke-McpPlayStopAndWait {
    param(
        [string]$Root,
        [int]$TimeoutSec = 90,
        [double]$PollSec = 0.5
    )

    $response = $null
    try {
        $response = Invoke-McpJson -Root $Root -SubPath "/play/stop"
    }
    catch {
        if ($_.Exception.Message -notmatch "connect") {
            throw
        }
    }

    $bridge = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec
    $wait = Invoke-McpTransitionWait -Root $Root -SubPath "/play/wait-for-stop" -TimeoutSec $TimeoutSec -PollSec $PollSec
    $stopped = Wait-McpPlayModeStopped -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec

    return [PSCustomObject]@{
        Response = $response
        Wait = $wait
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

function Assert-McpNoOpenSceneDiskWrite {
    param(
        [string]$Root,
        [string[]]$AssetPaths,
        [int]$TimeoutSec = 30
    )

    $normalizedTargets = @(
        @($AssetPaths) |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_.Replace('\', '/') }
    )

    if (@($normalizedTargets).Count -eq 0) {
        return
    }

    $health = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec
    $activeScenePath = ""

    if ($null -ne $health.State.PSObject.Properties["activeScenePath"] -and -not [string]::IsNullOrWhiteSpace([string]$health.State.activeScenePath)) {
        $activeScenePath = ([string]$health.State.activeScenePath).Replace('\', '/')
    }

    if ([string]::IsNullOrWhiteSpace($activeScenePath)) {
        return
    }

    foreach ($target in $normalizedTargets) {
        if ($target.EndsWith(".unity", [System.StringComparison]::OrdinalIgnoreCase) -and
            [string]::Equals($target, $activeScenePath, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw ("Open scene on-disk write blocked by SSOT policy. Target='{0}', activeScene='{1}'. Use MCP scene repair, switch scenes first, or close Unity before touching the file on disk." -f $target, $activeScenePath)
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

function Convert-McpConsoleItemsToGroupedList {
    param(
        [object[]]$Items
    )

    $groups = @{}

    foreach ($item in @($Items)) {
        if ($null -eq $item) {
            continue
        }

        $type = if ($null -ne $item.PSObject.Properties["type"]) { [string]$item.type } else { "Error" }
        $message = if ($null -ne $item.PSObject.Properties["message"]) { [string]$item.message } else { [string]$item }
        $timestampUtc = if ($null -ne $item.PSObject.Properties["timestampUtc"]) { [string]$item.timestampUtc } else { $null }
        $stackTrace = if ($null -ne $item.PSObject.Properties["stackTrace"]) { [string]$item.stackTrace } else { $null }
        $key = "{0}`n{1}" -f $type, $message

        if (-not $groups.ContainsKey($key)) {
            $groups[$key] = [ordered]@{
                type = $type
                message = $message
                count = 0
                latestTimestampUtc = $timestampUtc
                stackTrace = $stackTrace
            }
        }

        $groups[$key].count++

        if (-not [string]::IsNullOrWhiteSpace($timestampUtc)) {
            $groups[$key].latestTimestampUtc = $timestampUtc
        }

        if ([string]::IsNullOrWhiteSpace($groups[$key].stackTrace) -and -not [string]::IsNullOrWhiteSpace($stackTrace)) {
            $groups[$key].stackTrace = $stackTrace
        }
    }

    return @(
        $groups.Values |
            Sort-Object @{ Expression = "count"; Descending = $true }, @{ Expression = "latestTimestampUtc"; Descending = $true } |
            ForEach-Object {
                [PSCustomObject]@{
                    type = $_.type
                    message = $_.message
                    count = $_.count
                    latestTimestampUtc = $_.latestTimestampUtc
                    stackTrace = $_.stackTrace
                }
            }
    )
}

function Get-McpConsoleSummary {
    param(
        [string]$Root,
        [int]$LogLimit = 80,
        [int]$ErrorLimit = 20
    )

    $logs = Get-McpRecentLogs -Root $Root -Limit $LogLimit
    $errors = Get-McpRecentErrors -Root $Root -Limit $ErrorLimit

    $groupedLogs = Convert-McpConsoleItemsToGroupedList -Items $logs.items
    $groupedErrors = Convert-McpConsoleItemsToGroupedList -Items $errors.items

    $warningGroups = @($groupedLogs | Where-Object { $_.type -eq "Warning" })
    $errorGroups = @($groupedErrors)

    return [PSCustomObject]@{
        warningCount = @($warningGroups).Count
        errorCount = @($errorGroups).Count
        warnings = $warningGroups
        errors = $errorGroups
    }
}

function Resolve-McpAbsolutePath {
    param([string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $PathValue))
}

function Assert-McpSceneAssetExistsForWorkflow {
    param(
        [string]$ScenePath,
        [string]$WorkflowName,
        [string]$FallbackRoute = "UI Toolkit candidate surface"
    )

    if ([string]::IsNullOrWhiteSpace($ScenePath)) {
        throw "$WorkflowName requires a concrete scene path."
    }

    $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
    $absoluteScenePath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ScenePath))
    if (Test-Path -LiteralPath $absoluteScenePath) {
        return
    }

    throw ("{0} is a historical scene workflow and cannot run because '{1}' does not exist. Use the {2} route before generating fresh scene evidence." -f $WorkflowName, $ScenePath, $FallbackRoute)
}

function Ensure-McpParentDirectory {
    param([string]$PathValue)

    $directory = Split-Path -Parent $PathValue
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
}

function Enter-McpLockFile {
    param(
        [string]$Name,
        [string]$Owner = "unknown",
        [string]$LockPath,
        [string]$HeldMessagePrefix = "MCP exclusive operation lock",
        [int]$TimeoutSec = 0,
        [double]$PollSec = 0.5,
        [int]$StaleAfterMinutes = 90
    )

    $absolutePath = Resolve-McpAbsolutePath -PathValue $LockPath
    Ensure-McpParentDirectory -PathValue $absolutePath

    $token = [guid]::NewGuid().ToString("N")
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $payload = [ordered]@{
        name = $Name
        owner = $Owner
        token = $token
        pid = $PID
        startedAt = (Get-Date).ToString("o")
    }

    while ($true) {
        try {
            $stream = [System.IO.File]::Open($absolutePath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                $writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::UTF8)
                try {
                    $writer.Write(($payload | ConvertTo-Json -Depth 4))
                    $writer.Flush()
                }
                finally {
                    $writer.Dispose()
                }
            }
            finally {
                $stream.Dispose()
            }

            return [PSCustomObject]@{
                Name = $Name
                Owner = $Owner
                Token = $token
                Path = $LockPath
                AbsolutePath = $absolutePath
            }
        }
        catch [System.IO.IOException] {
            $existingText = ""
            if (Test-Path -LiteralPath $absolutePath) {
                $lockItem = Get-Item -LiteralPath $absolutePath
                if ($StaleAfterMinutes -gt 0 -and $lockItem.LastWriteTimeUtc -lt (Get-Date).ToUniversalTime().AddMinutes(-1 * $StaleAfterMinutes)) {
                    Remove-Item -LiteralPath $absolutePath -Force
                    continue
                }

                try {
                    $existingText = (Get-Content -LiteralPath $absolutePath -Raw -ErrorAction Stop).Trim()
                }
                catch {
                    $existingText = "unable to read lock: $($_.Exception.Message)"
                }

                try {
                    $existingJson = $existingText | ConvertFrom-Json
                    if ($null -ne $existingJson.PSObject.Properties["pid"]) {
                        $existingPid = [int]$existingJson.pid
                        if ($existingPid -gt 0 -and $null -eq (Get-Process -Id $existingPid -ErrorAction SilentlyContinue)) {
                            Remove-Item -LiteralPath $absolutePath -Force
                            continue
                        }
                    }
                }
                catch {
                    # Keep unreadable or non-JSON locks conservative; age-based cleanup below still applies.
                }
            }

            if ($TimeoutSec -le 0 -or (Get-Date) -ge $deadline) {
                throw ("{0} is held. lock='{1}' requested='{2}' owner='{3}' existing='{4}'" -f $HeldMessagePrefix, $LockPath, $Name, $Owner, $existingText)
            }

            Start-Sleep -Seconds $PollSec
        }
    }
}

function Exit-McpLockFile {
    param([object]$Lock)

    if ($null -eq $Lock -or [string]::IsNullOrWhiteSpace([string]$Lock.AbsolutePath)) {
        return
    }

    if (-not (Test-Path -LiteralPath $Lock.AbsolutePath)) {
        return
    }

    try {
        $json = Get-Content -LiteralPath $Lock.AbsolutePath -Raw -ErrorAction Stop | ConvertFrom-Json
        if ($null -ne $json.PSObject.Properties["token"] -and [string]$json.token -ne [string]$Lock.Token) {
            return
        }
    }
    catch {
        return
    }

    Remove-Item -LiteralPath $Lock.AbsolutePath -Force
}

function Enter-McpExclusiveOperation {
    param(
        [string]$Name,
        [string]$Owner = "unknown",
        [string]$LockPath = "Temp/UnityMcp/runtime-operation.lock",
        [int]$TimeoutSec = 0,
        [double]$PollSec = 0.5,
        [int]$StaleAfterMinutes = 90,
        [string]$UnityResourceLockPath = "Temp/UnityMcp/unity-resource.lock"
    )

    $unityResourceLock = $null
    $operationLock = $null

    try {
        if ($LockPath -ne $UnityResourceLockPath) {
            $unityResourceLock = Enter-McpLockFile `
                -Name $Name `
                -Owner $Owner `
                -LockPath $UnityResourceLockPath `
                -HeldMessagePrefix "Unity resource lock" `
                -TimeoutSec $TimeoutSec `
                -PollSec $PollSec `
                -StaleAfterMinutes $StaleAfterMinutes
        }

        $operationLock = Enter-McpLockFile `
            -Name $Name `
            -Owner $Owner `
            -LockPath $LockPath `
            -HeldMessagePrefix "MCP exclusive operation lock" `
            -TimeoutSec $TimeoutSec `
            -PollSec $PollSec `
            -StaleAfterMinutes $StaleAfterMinutes

        return [PSCustomObject]@{
            Name = $operationLock.Name
            Owner = $operationLock.Owner
            Token = $operationLock.Token
            Path = $operationLock.Path
            AbsolutePath = $operationLock.AbsolutePath
            UnityResourceLock = $unityResourceLock
        }
    }
    catch {
        Exit-McpLockFile -Lock $operationLock
        Exit-McpLockFile -Lock $unityResourceLock
        throw
    }
}

function Exit-McpExclusiveOperation {
    param([object]$Lock)

    if ($null -eq $Lock) {
        return
    }

    $unityResourceLock = $null
    if ($null -ne $Lock.PSObject.Properties["UnityResourceLock"]) {
        $unityResourceLock = $Lock.UnityResourceLock
    }

    Exit-McpLockFile -Lock $Lock
    Exit-McpLockFile -Lock $unityResourceLock
}

function Test-McpResponseSuccess {
    param([object]$Response)

    if ($null -eq $Response) {
        return $false
    }

    if ($Response -is [string]) {
        return $false
    }

    if ($null -ne $Response.PSObject.Properties["success"]) {
        return [bool]$Response.success
    }

    if ($null -ne $Response.PSObject.Properties["ok"]) {
        return [bool]$Response.ok
    }

    return $true
}

function Wait-McpCondition {
    param(
        [scriptblock]$Condition,
        [string]$Description,
        [int]$TimeoutSec = 20,
        [int]$PollMs = 250
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) {
            return
        }

        Start-Sleep -Milliseconds $PollMs
    }

    throw "Timed out waiting for $Description."
}

function Invoke-McpSetUiValue {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Value
    )

    Invoke-RestMethod `
        -Method Post `
        -Uri "$Root/ui/set-value" `
        -ContentType "application/json" `
        -Body (@{
            path = $Path
            value = $Value
        } | ConvertTo-Json -Compress) | Out-Null
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

function Get-McpUiElementState {
    param(
        [string]$Root,
        [string]$Path
    )

    return Invoke-McpJson -Root $Root -SubPath "/ui/get-state" -Body @{
        path = $Path
    }
}

function Convert-McpUiStateEntriesToMap {
    param([object]$Response)

    $map = @{}
    foreach ($entry in @($Response.state)) {
        if ($entry -match '^\[(.*?),\s?(.*)\]$') {
            $map[$matches[1]] = $matches[2]
        }
    }

    return $map
}

function Get-McpUiStateMap {
    param(
        [string]$Root,
        [string]$Path
    )

    return Convert-McpUiStateEntriesToMap (Get-McpUiElementState -Root $Root -Path $Path)
}

function Get-McpUiTextValue {
    param(
        [string]$Root,
        [string]$Path
    )

    $state = Get-McpUiStateMap -Root $Root -Path $Path
    return [string]$state["text"]
}

function Get-McpUiButtonInfo {
    param(
        [string]$Root,
        [string]$Path
    )

    $state = Get-McpUiStateMap -Root $Root -Path $Path

    return [PSCustomObject]@{
        path = [string]$state["path"]
        activeInHierarchy = ([string]$state["activeInHierarchy"]) -eq "True"
        interactable = ([string]$state["interactable"]) -eq "True"
    }
}

function Get-McpUiActiveInHierarchy {
    param(
        [string]$Root,
        [string]$Path
    )

    $state = Get-McpUiStateMap -Root $Root -Path $Path
    return ([string]$state["activeInHierarchy"]) -eq "True"
}

function Get-McpPageStateSnapshot {
    param(
        [string]$Root,
        [string]$LobbyRootPath,
        [string]$GarageRootPath
    )

    return [PSCustomObject]@{
        lobbyActive = Get-McpUiActiveInHierarchy -Root $Root -Path $LobbyRootPath
        garageActive = Get-McpUiActiveInHierarchy -Root $Root -Path $GarageRootPath
    }
}

function Invoke-McpPrepareLobbyPlaySession {
    param(
        [string]$Root,
        [string]$ScenePath = "Assets/Scenes/LobbyScene.unity",
        [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
        [int]$TimeoutSec = 90,
        [double]$PollSec = 0.5
    )

    Assert-McpSceneAssetExistsForWorkflow -ScenePath $ScenePath -WorkflowName "Invoke-McpPrepareLobbyPlaySession"

    $sceneName = [System.IO.Path]::GetFileNameWithoutExtension($ScenePath)
    $health = Wait-McpBridgeHealthy -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec
    $stoppedPreExistingPlay = $false

    if ($health.State.isPlaying) {
        Invoke-McpPlayStopAndWait -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec | Out-Null
        $stoppedPreExistingPlay = $true
    }

    if ($health.State.activeScenePath -ne $ScenePath) {
        Invoke-McpSceneOpenAndWait -Root $Root -ScenePath $ScenePath -TimeoutSec $TimeoutSec -PollSec $PollSec | Out-Null
        Wait-McpSceneActive -Root $Root -SceneName $sceneName -TimeoutSec $TimeoutSec -PollSec $PollSec | Out-Null
    }

    $play = Invoke-McpPlayStartAndWaitForBridge -Root $Root -TimeoutSec $TimeoutSec -PollSec $PollSec
    $loadingPanelWait = $null

    if (-not [string]::IsNullOrWhiteSpace($LoginLoadingPanelPath)) {
        try {
            $loadingPanelWait = Wait-McpUiInactive -Root $Root -Path $LoginLoadingPanelPath -TimeoutMs ($TimeoutSec * 1000)
        }
        catch {
            $loadingPanelWait = [PSCustomObject]@{
                ok = $false
                message = $_.Exception.Message
            }
        }
    }

    return [PSCustomObject]@{
        stoppedPreExistingPlay = $stoppedPreExistingPlay
        play = $play
        loadingPanelWait = $loadingPanelWait
    }
}

function Wait-McpPhotonLobbyReady {
    param(
        [string]$Root,
        [int]$TimeoutSec = 90,
        [int]$LogLimit = 120
    )

    Wait-McpCondition `
        -Description "Photon lobby join log" `
        -TimeoutSec $TimeoutSec `
        -Condition {
            $logs = Get-McpRecentLogs -Root $Root -Limit $LogLimit
            foreach ($item in @($logs.items)) {
                if ($item.message -like "*Joined lobby. Ready for matchmaking.*") {
                    return $true
                }
            }

            return $false
        }
}

function Invoke-McpUiInvoke {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Method = "click",
        [string]$CustomMethod,
        [Alias("Args")]
        [object[]]$InvokeArgs
    )

    $body = @{
        path = $Path
        method = $Method
    }

    if (-not [string]::IsNullOrWhiteSpace($CustomMethod)) {
        $body.customMethod = $CustomMethod
    }

    if ($null -ne $InvokeArgs) {
        $normalizedArgs = @($InvokeArgs | ForEach-Object { if ($null -eq $_) { "" } else { [string]$_ } })
        $body.args = $normalizedArgs
        for ($i = 0; $i -lt $normalizedArgs.Count -and $i -lt 3; $i++) {
            $body["arg$i"] = $normalizedArgs[$i]
        }
    }

    return Invoke-McpJson -Root $Root -SubPath "/ui/invoke" -Body $body
}

function Wait-McpUiActive {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    return Invoke-McpJson -Root $Root -SubPath "/ui/wait-for-active" -Body @{
        path = $Path
        timeoutMs = $TimeoutMs
        pollIntervalMs = $PollIntervalMs
    }
}

function Wait-McpUiInactive {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    return Invoke-McpJson -Root $Root -SubPath "/ui/wait-for-inactive" -Body @{
        path = $Path
        timeoutMs = $TimeoutMs
        pollIntervalMs = $PollIntervalMs
    }
}

function Wait-McpUiText {
    param(
        [string]$Root,
        [string]$ExpectedText,
        [string]$Path,
        [bool]$Exact = $false,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    return Invoke-McpJson -Root $Root -SubPath "/ui/wait-for-text" -Body @{
        path = $Path
        expectedText = $ExpectedText
        exact = $Exact
        timeoutMs = $TimeoutMs
        pollIntervalMs = $PollIntervalMs
    }
}

function Wait-McpUiComponent {
    param(
        [string]$Root,
        [string]$Path,
        [string]$ComponentType,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    return Invoke-McpJson -Root $Root -SubPath "/ui/wait-for-component" -Body @{
        path = $Path
        componentType = $ComponentType
        timeoutMs = $TimeoutMs
        pollIntervalMs = $PollIntervalMs
    }
}

function Invoke-McpScreenshotCapture {
    param(
        [string]$Root,
        [string]$OutputPath,
        [int]$SuperSize = 1,
        [switch]$Overwrite
    )

    $body = @{
        superSize = $SuperSize
        overwrite = [bool]$Overwrite
    }

    if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
        $body.outputPath = $OutputPath
    }

    return Invoke-McpJson -Root $Root -SubPath "/screenshot/capture" -Body $body
}
