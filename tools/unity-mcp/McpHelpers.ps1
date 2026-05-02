Set-StrictMode -Version Latest

function Get-UnityMcpProjectRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

function Get-UnityMcpPortFilePath {
    return Join-Path (Get-UnityMcpProjectRoot) "ProjectSettings\UnityMcpPort.txt"
}

function Get-UnityMcpConfiguredPort {
    $portFile = Get-UnityMcpPortFilePath
    if (Test-Path -LiteralPath $portFile) {
        $portText = (Get-Content -Path $portFile -Raw).Trim()
        if ($portText -match '^\d+$') {
            return [int]$portText
        }
    }

    return 0
}

function Get-UnityMcpRootPort {
    param([string]$Root)

    if ([string]::IsNullOrWhiteSpace($Root)) {
        return 0
    }

    try {
        return ([System.Uri]$Root).Port
    }
    catch {
        return 0
    }
}

function Get-UnityMcpNormalizedProjectPath {
    return [System.IO.Path]::GetFullPath((Get-UnityMcpProjectRoot)).
        TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar).
        Replace('\', '/').
        ToLowerInvariant()
}

function Get-UnityMcpStableHash {
    param([string]$Value)

    $hash = [uint32]2166136261
    for ($i = 0; $i -lt $Value.Length; $i++) {
        $hash = [uint32]((([uint64]($hash -bxor [uint32][char]$Value[$i])) * 16777619) % 4294967296)
    }

    return $hash
}

function Get-UnityMcpProjectKey {
    return (Get-UnityMcpStableHash -Value (Get-UnityMcpNormalizedProjectPath)).ToString("x8")
}

function Get-UnityMcpStickyFallbackPorts {
    param([int]$Count = 8)

    $rangeStart = 52000
    $rangeSize = 1000
    $hash = Get-UnityMcpStableHash -Value (Get-UnityMcpNormalizedProjectPath)
    for ($i = 0; $i -lt $Count; $i++) {
        $bucket = [int](($hash + [uint32]$i) % $rangeSize)
        $rangeStart + $bucket
    }
}

function New-UnityMcpRootFromPort {
    param([int]$Port)

    return "http://127.0.0.1:$Port"
}

function Get-UnityMcpCandidateRoots {
    param([string]$Root)

    $ports = New-Object System.Collections.Generic.List[int]
    $rootPort = Get-UnityMcpRootPort -Root $Root
    if ($rootPort -gt 0) {
        $ports.Add($rootPort)
    }

    $configuredPort = Get-UnityMcpConfiguredPort
    if ($configuredPort -gt 0) {
        $ports.Add($configuredPort)
    }

    $ports.Add(51234)
    foreach ($port in Get-UnityMcpStickyFallbackPorts) {
        $ports.Add($port)
    }

    return @(
        $ports |
            Where-Object { $_ -gt 0 -and $_ -le 65535 } |
            Select-Object -Unique |
            ForEach-Object { New-UnityMcpRootFromPort -Port $_ }
    )
}

function Test-UnityMcpHealthMatchesProject {
    param([object]$State)

    if ($null -eq $State -or -not $State.ok) {
        return $false
    }

    if ($null -ne $State.PSObject.Properties["projectKey"] -and -not [string]::IsNullOrWhiteSpace([string]$State.projectKey)) {
        return [string]::Equals([string]$State.projectKey, (Get-UnityMcpProjectKey), [System.StringComparison]::OrdinalIgnoreCase)
    }

    if ($null -ne $State.PSObject.Properties["projectRootPath"] -and -not [string]::IsNullOrWhiteSpace([string]$State.projectRootPath)) {
        $actual = [System.IO.Path]::GetFullPath([string]$State.projectRootPath).
            TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar).
            Replace('\', '/').
            ToLowerInvariant()
        return [string]::Equals($actual, (Get-UnityMcpNormalizedProjectPath), [System.StringComparison]::Ordinal)
    }

    return $false
}

function Test-UnityMcpRootHealthyForProject {
    param(
        [string]$Root,
        [int]$TimeoutSec = 1
    )

    try {
        $state = Invoke-RestMethod -Method Get -Uri "$($Root.TrimEnd('/'))/health" -TimeoutSec $TimeoutSec
        return Test-UnityMcpHealthMatchesProject -State $state
    }
    catch {
        return $false
    }
}

function Get-UnityMcpBaseUrl {
    param([string]$ExplicitBaseUrl)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitBaseUrl)) {
        return $ExplicitBaseUrl.TrimEnd("/")
    }

    $configuredPort = Get-UnityMcpConfiguredPort
    if ($configuredPort -gt 0) {
        return New-UnityMcpRootFromPort -Port $configuredPort
    }

    return New-UnityMcpRootFromPort -Port 51234
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

    if ([string]::IsNullOrWhiteSpace($Root)) {
        foreach ($candidateRoot in Get-UnityMcpCandidateRoots -Root "") {
            if (Test-UnityMcpRootHealthyForProject -Root $candidateRoot) {
                return $candidateRoot
            }
        }

        return Get-UnityMcpBaseUrl -ExplicitBaseUrl ""
    }

    if (-not (Test-UnityMcpRootCanFollowProjectPort -Root $Root)) {
        return $Root
    }

    foreach ($candidateRoot in Get-UnityMcpCandidateRoots -Root $Root) {
        if (Test-UnityMcpRootHealthyForProject -Root $candidateRoot) {
            return $candidateRoot
        }
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
    $refreshedRoot = Get-UnityMcpRefreshedRoot -Root $Root
    if ($refreshedRoot -ne $Root) {
        $Root = $refreshedRoot
    }

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
            $refreshedRoot = Get-UnityMcpRefreshedRoot -Root $Root
            if ($refreshedRoot -ne $Root) {
                $Root = $refreshedRoot
            }
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
    $refreshedRoot = Get-UnityMcpRefreshedRoot -Root $Root
    if ($refreshedRoot -ne $Root) {
        $Root = $refreshedRoot
    }

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
            $refreshedRoot = Get-UnityMcpRefreshedRoot -Root $Root
            if ($refreshedRoot -ne $Root) {
                $Root = $refreshedRoot
            }
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
    $refreshedRoot = Get-UnityMcpRefreshedRoot -Root $currentRoot
    if ($refreshedRoot -ne $currentRoot) {
        $currentRoot = $refreshedRoot
    }

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
    $lastError = $null
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
        catch {
            $lastError = $_.Exception.Message
        }

        Start-Sleep -Seconds $PollSec
    }

    $sw.Stop()
    if ([string]::IsNullOrWhiteSpace($lastError)) {
        $lastError = "No health response met the ready condition."
    }

    throw "Play mode did not stabilize within ${TimeoutSec}s. Last error: $lastError"
}

function Wait-McpPlayModeStopped {
    param(
        [string]$Root,
        [int]$TimeoutSec,
        [double]$PollSec
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastError = $null
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
        catch {
            $lastError = $_.Exception.Message
        }

        Start-Sleep -Seconds $PollSec
    }

    $sw.Stop()
    if ([string]::IsNullOrWhiteSpace($lastError)) {
        $lastError = "No health response met the stopped condition."
    }

    throw "Play mode did not stop within ${TimeoutSec}s. Last error: $lastError"
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
    $lastError = $null
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
        catch {
            $lastError = $_.Exception.Message
        }

        Start-Sleep -Seconds $PollSec
    }

    $sw.Stop()
    if ([string]::IsNullOrWhiteSpace($lastError)) {
        $lastError = "No health response reported the expected active scene."
    }

    throw "Active scene did not become '${SceneName}' within ${TimeoutSec}s. Last error: $lastError"
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

function Write-McpTextFileWithRetry {
    param(
        [string]$PathValue,
        [string]$Content,
        [int]$TimeoutSec = 8,
        [double]$PollSec = 0.25
    )

    $absolutePath = Resolve-McpAbsolutePath -PathValue $PathValue
    Ensure-McpParentDirectory -PathValue $absolutePath

    $directory = Split-Path -Parent $absolutePath
    $fileName = Split-Path -Leaf $absolutePath
    $encoding = [System.Text.UTF8Encoding]::new($false)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $lastError = $null

    while ($true) {
        $tempPath = Join-Path $directory (".{0}.{1}.{2}.tmp" -f $fileName, $PID, [guid]::NewGuid().ToString("N"))
        $backupPath = Join-Path $directory (".{0}.{1}.{2}.bak" -f $fileName, $PID, [guid]::NewGuid().ToString("N"))

        try {
            [System.IO.File]::WriteAllText($tempPath, $Content, $encoding)

            if (Test-Path -LiteralPath $absolutePath) {
                [System.IO.File]::Replace($tempPath, $absolutePath, $backupPath)
            }
            else {
                [System.IO.File]::Move($tempPath, $absolutePath)
            }

            if (Test-Path -LiteralPath $backupPath) {
                Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue
            }

            return [PSCustomObject]@{
                path = $absolutePath
                fallbackUsed = $false
                warning = ""
            }
        }
        catch [System.IO.IOException] {
            $lastError = $_.Exception.Message
        }
        catch [System.UnauthorizedAccessException] {
            $lastError = $_.Exception.Message
        }
        finally {
            if (Test-Path -LiteralPath $tempPath) {
                Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
            }

            if (Test-Path -LiteralPath $backupPath) {
                Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue
            }
        }

        if ((Get-Date) -ge $deadline) {
            $fallbackPath = Join-Path $directory ("{0}.{1}.{2}.json" -f [System.IO.Path]::GetFileNameWithoutExtension($fileName), $PID, [guid]::NewGuid().ToString("N"))
            [System.IO.File]::WriteAllText($fallbackPath, $Content, $encoding)

            return [PSCustomObject]@{
                path = $fallbackPath
                fallbackUsed = $true
                warning = ("Target file remained locked; wrote fallback artifact. target='{0}' error='{1}'" -f $absolutePath, $lastError)
            }
        }

        Start-Sleep -Seconds $PollSec
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

    throw "UGUI helper Invoke-McpSetUiValue is disabled. Use Set-McpUitkElementValue for UIDocument/VisualElement state."
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

    throw "UGUI helper Get-McpUiElementState is disabled. Use Get-McpUitkElementState."
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

    throw "UGUI helper Get-McpUiStateMap is disabled. Use Get-McpUitkElementState."
}

function Get-McpUiTextValue {
    param(
        [string]$Root,
        [string]$Path
    )

    throw "UGUI helper Get-McpUiTextValue is disabled. Use Get-McpUitkElementState."
}

function Get-McpUiButtonInfo {
    param(
        [string]$Root,
        [string]$Path
    )

    throw "UGUI helper Get-McpUiButtonInfo is disabled. Use Get-McpUitkElementState."
}

function Get-McpUiActiveInHierarchy {
    param(
        [string]$Root,
        [string]$Path
    )

    throw "UGUI helper Get-McpUiActiveInHierarchy is disabled. Use Get-McpUitkElementState."
}

function Get-McpPageStateSnapshot {
    param(
        [string]$Root,
        [string]$LobbyRootPath,
        [string]$GarageRootPath
    )

    throw "UGUI page snapshot helper is disabled. Use Get-McpUitkState or explicit UITK element state checks."
}

function Invoke-McpPrepareLobbyPlaySession {
    param(
        [string]$Root,
        [string]$ScenePath = "Assets/Scenes/LobbyScene.unity",
        [string]$LoginLoadingPanelPath = "",
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
        throw "LoginLoadingPanelPath is a disabled UGUI wait hook. Use Wait-McpUitkElement with an explicit UIDocument selector."
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

    throw "UGUI helper Invoke-McpUiInvoke is disabled. Use Invoke-McpUitkElement for VisualElement events or Invoke-McpGameObjectMethod for MonoBehaviour smoke-driver methods."
}

function Invoke-McpGameObjectMethod {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Method,
        [Alias("Args")]
        [object[]]$InvokeArgs
    )

    $body = @{
        path = $Path
        method = $Method
    }

    if ($null -ne $InvokeArgs) {
        $normalizedArgs = @($InvokeArgs | ForEach-Object { if ($null -eq $_) { "" } else { [string]$_ } })
        $body.args = $normalizedArgs
        for ($i = 0; $i -lt $normalizedArgs.Count -and $i -lt 3; $i++) {
            $body["arg$i"] = $normalizedArgs[$i]
        }
    }

    return Invoke-McpJson -Root $Root -SubPath "/gameobject/invoke" -Body $body
}

function Wait-McpComponent {
    param(
        [string]$Root,
        [string]$Path,
        [string]$ComponentType,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    return Invoke-McpJson -Root $Root -SubPath "/wait/for-component" -Body @{
        path = $Path
        componentType = $ComponentType
        timeoutMs = $TimeoutMs
        pollIntervalMs = $PollIntervalMs
    }
}

function Wait-McpUiActive {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    throw "UGUI helper Wait-McpUiActive is disabled. Use Wait-McpUitkElement."
}

function Wait-McpUiInactive {
    param(
        [string]$Root,
        [string]$Path,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    throw "UGUI helper Wait-McpUiInactive is disabled. Use Wait-McpUitkElement or Get-McpUitkElementState."
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

    throw "UGUI helper Wait-McpUiText is disabled. Use Wait-McpUitkElement with expectedText."
}

function Wait-McpUiComponent {
    param(
        [string]$Root,
        [string]$Path,
        [string]$ComponentType,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    throw "UGUI helper Wait-McpUiComponent is disabled. Use Wait-McpComponent for GameObject components."
}

function Get-McpUitkState {
    param(
        [string]$Root,
        [string]$DocumentPath,
        [string]$DocumentName,
        [int]$MaxDepth = 6
    )

    $query = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($DocumentPath)) {
        $query.Add("documentPath=$([uri]::EscapeDataString($DocumentPath))")
    }
    if (-not [string]::IsNullOrWhiteSpace($DocumentName)) {
        $query.Add("documentName=$([uri]::EscapeDataString($DocumentName))")
    }
    if ($MaxDepth -gt 0) {
        $query.Add("maxDepth=$MaxDepth")
    }

    $subPath = "/uitk/state"
    if ($query.Count -gt 0) {
        $subPath = "$subPath?$($query -join '&')"
    }

    return Invoke-McpGetJson -Root $Root -SubPath $subPath
}

function New-McpUitkElementBody {
    param(
        [string]$DocumentPath,
        [string]$DocumentName,
        [string]$ElementName,
        [string]$ElementPath,
        [string]$Method,
        [string]$Value,
        [string]$ExpectedText,
        [bool]$Exact = $false,
        [int]$TimeoutMs = 0,
        [int]$PollIntervalMs = 0
    )

    $body = @{}
    if (-not [string]::IsNullOrWhiteSpace($DocumentPath)) { $body.documentPath = $DocumentPath }
    if (-not [string]::IsNullOrWhiteSpace($DocumentName)) { $body.documentName = $DocumentName }
    if (-not [string]::IsNullOrWhiteSpace($ElementName)) { $body.elementName = $ElementName }
    if (-not [string]::IsNullOrWhiteSpace($ElementPath)) { $body.elementPath = $ElementPath }
    if (-not [string]::IsNullOrWhiteSpace($Method)) { $body.method = $Method }
    if ($PSBoundParameters.ContainsKey("Value")) { $body.value = $Value }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedText)) { $body.expectedText = $ExpectedText }
    if ($Exact) { $body.exact = $true }
    if ($TimeoutMs -gt 0) { $body.timeoutMs = $TimeoutMs }
    if ($PollIntervalMs -gt 0) { $body.pollIntervalMs = $PollIntervalMs }
    return $body
}

function Get-McpUitkElementState {
    param(
        [string]$Root,
        [string]$DocumentPath,
        [string]$DocumentName,
        [string]$ElementName,
        [string]$ElementPath
    )

    $body = New-McpUitkElementBody -DocumentPath $DocumentPath -DocumentName $DocumentName -ElementName $ElementName -ElementPath $ElementPath
    return Invoke-McpJson -Root $Root -SubPath "/uitk/get-state" -Body $body
}

function Set-McpUitkElementValue {
    param(
        [string]$Root,
        [string]$Value,
        [string]$DocumentPath,
        [string]$DocumentName,
        [string]$ElementName,
        [string]$ElementPath
    )

    $body = New-McpUitkElementBody -DocumentPath $DocumentPath -DocumentName $DocumentName -ElementName $ElementName -ElementPath $ElementPath -Value $Value
    return Invoke-McpJson -Root $Root -SubPath "/uitk/set-value" -Body $body
}

function Invoke-McpUitkElement {
    param(
        [string]$Root,
        [string]$DocumentPath,
        [string]$DocumentName,
        [string]$ElementName,
        [string]$ElementPath,
        [string]$Method = "click",
        [string]$Value
    )

    $body = New-McpUitkElementBody -DocumentPath $DocumentPath -DocumentName $DocumentName -ElementName $ElementName -ElementPath $ElementPath -Method $Method -Value $Value
    return Invoke-McpJson -Root $Root -SubPath "/uitk/invoke" -Body $body
}

function Wait-McpUitkElement {
    param(
        [string]$Root,
        [string]$DocumentPath,
        [string]$DocumentName,
        [string]$ElementName,
        [string]$ElementPath,
        [string]$ExpectedText,
        [bool]$Exact = $false,
        [int]$TimeoutMs = 10000,
        [int]$PollIntervalMs = 100
    )

    $body = New-McpUitkElementBody `
        -DocumentPath $DocumentPath `
        -DocumentName $DocumentName `
        -ElementName $ElementName `
        -ElementPath $ElementPath `
        -ExpectedText $ExpectedText `
        -Exact $Exact `
        -TimeoutMs $TimeoutMs `
        -PollIntervalMs $PollIntervalMs

    return Invoke-McpJson -Root $Root -SubPath "/uitk/wait-for-element" -Body $body
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
