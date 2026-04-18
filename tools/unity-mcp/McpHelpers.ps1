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

function Test-McpTransientConnectionFailure {
    param([System.Exception]$Exception)

    if ($null -eq $Exception) {
        return $false
    }

    $message = $Exception.Message
    return $message -match "connect|connection|actively refused|forcibly closed|reset by peer|No connection could be made"
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

function Test-McpMessageMatchesPattern {
    param(
        [string]$Message,
        [string[]]$Patterns
    )

    foreach ($pattern in @($Patterns)) {
        if (-not [string]::IsNullOrWhiteSpace($pattern) -and $Message -like $pattern) {
            return $true
        }
    }

    return $false
}

function Convert-McpConsoleItemsToGroupedList {
    param(
        [object[]]$Items,
        [string[]]$BenignMessagePatterns = @()
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
                benign = Test-McpMessageMatchesPattern -Message $message -Patterns $BenignMessagePatterns
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
                    benign = $_.benign
                }
            }
    )
}

function Get-McpConsoleSummary {
    param(
        [string]$Root,
        [int]$LogLimit = 80,
        [int]$ErrorLimit = 20,
        [string[]]$BenignMessagePatterns = @(
            "[[]Firestore[]] Document not found*",
            "PUN is in development mode*"
        )
    )

    $logs = Get-McpRecentLogs -Root $Root -Limit $LogLimit
    $errors = Get-McpRecentErrors -Root $Root -Limit $ErrorLimit

    $groupedLogs = Convert-McpConsoleItemsToGroupedList -Items $logs.items -BenignMessagePatterns $BenignMessagePatterns
    $groupedErrors = Convert-McpConsoleItemsToGroupedList -Items $errors.items -BenignMessagePatterns $BenignMessagePatterns

    $warningGroups = @($groupedLogs | Where-Object { $_.type -eq "Warning" -and -not $_.benign })
    $infoGroups = @($groupedLogs | Where-Object { $_.type -ne "Warning" -and $_.type -ne "Error" -and -not $_.benign })
    $benignGroups = @($groupedLogs + $groupedErrors | Where-Object { $_.benign })
    $errorGroups = @($groupedErrors | Where-Object { -not $_.benign })

    return [PSCustomObject]@{
        rawLogCount = $logs.count
        rawErrorCount = $errors.count
        uniqueLogCount = @($groupedLogs).Count
        uniqueErrorCount = @($groupedErrors).Count
        warningCount = @($warningGroups).Count
        errorCount = @($errorGroups).Count
        benignCount = @($benignGroups).Count
        warnings = $warningGroups
        errors = $errorGroups
        info = $infoGroups
        benign = $benignGroups
    }
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

function Get-McpUiState {
    param([string]$Root)
    return Invoke-McpGetJson -Root $Root -SubPath "/ui/state"
}

function Convert-McpUiNodesToFlatList {
    param(
        [object[]]$Nodes,
        [System.Collections.Generic.List[object]]$Result
    )

    foreach ($node in @($Nodes)) {
        if ($null -eq $node) {
            continue
        }

        $components = @()
        if ($null -ne $node.PSObject.Properties["components"]) {
            if ($node.components -is [string]) {
                $components = @(
                    ([string]$node.components).Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries) |
                        Select-Object -Unique
                )
            }
            else {
                $components = @($node.components | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
            }
        }

        $Result.Add([PSCustomObject]@{
            path = [string]$node.path
            name = [string]$node.name
            activeInHierarchy = [bool]$node.activeInHierarchy
            childCount = if ($null -ne $node.PSObject.Properties["childCount"]) { [int]$node.childCount } else { 0 }
            components = $components
        }) | Out-Null

        if ($null -ne $node.PSObject.Properties["children"]) {
            Convert-McpUiNodesToFlatList -Nodes $node.children -Result $Result
        }
    }
}

function Get-McpUiStateSummary {
    param(
        [string]$Root,
        [string[]]$PathPrefixes = @("/Canvas"),
        [string[]]$ComponentTypes = @(),
        [switch]$IncludeInactive,
        [int]$MaxItems = 80
    )

    $uiState = Get-McpUiState -Root $Root
    if ($null -eq $uiState -or $null -eq $uiState.PSObject.Properties["canvases"]) {
        return [PSCustomObject]([ordered]@{
            sceneName = if ($null -ne $uiState -and $null -ne $uiState.PSObject.Properties["sceneName"]) { [string]$uiState.sceneName } else { $null }
            isPlaying = if ($null -ne $uiState -and $null -ne $uiState.PSObject.Properties["isPlaying"]) { [bool]$uiState.isPlaying } else { $false }
            timestampUtc = if ($null -ne $uiState -and $null -ne $uiState.PSObject.Properties["timestampUtc"]) { [string]$uiState.timestampUtc } else { $null }
            totalCanvasCount = 0
            totalNodeCount = 0
            matchedNodeCount = 0
            truncated = $false
            pathPrefixes = @($PathPrefixes)
            activeRoots = @()
            nodes = @()
        })
    }

    $flatNodes = New-Object 'System.Collections.Generic.List[object]'

    foreach ($canvas in @($uiState.canvases)) {
        Convert-McpUiNodesToFlatList -Nodes @($canvas) -Result $flatNodes
    }

    $filteredNodes = @(
        $flatNodes |
            Where-Object {
                $pathMatch = $false
                foreach ($prefix in @($PathPrefixes)) {
                    if ([string]::IsNullOrWhiteSpace($prefix) -or $_.path.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                        $pathMatch = $true
                        break
                    }
                }

                if (-not $pathMatch) {
                    return $false
                }

                if (-not $IncludeInactive.IsPresent -and -not $_.activeInHierarchy) {
                    return $false
                }

                if (@($ComponentTypes).Count -eq 0) {
                    return $true
                }

                foreach ($component in @($ComponentTypes)) {
                    if ($_.components -contains $component) {
                        return $true
                    }
                }

                return $false
            }
    )

    $visibleRoots = @(
        $flatNodes |
            Where-Object { $_.activeInHierarchy -and $_.path -in $PathPrefixes } |
            ForEach-Object { $_.path }
    )

    $nodeSummaries = @(
        $filteredNodes |
            Select-Object -First $MaxItems |
            ForEach-Object {
                [PSCustomObject]([ordered]@{
                    path = $_.path
                    name = $_.name
                    activeInHierarchy = $_.activeInHierarchy
                    childCount = $_.childCount
                    components = @($_.components)
                })
            }
    )

    $sceneNameValue = [string]$uiState.sceneName
    $isPlayingValue = [bool]$uiState.isPlaying
    $timestampValue = [string]$uiState.timestampUtc
    $totalCanvasCountValue = @($uiState.canvases).Count
    $totalNodeCountValue = $flatNodes.Count
    $matchedNodeCountValue = @($filteredNodes).Count
    $truncatedValue = @($filteredNodes).Count -gt $MaxItems
    $pathPrefixesValue = @($PathPrefixes)
    $activeRootsValue = @($visibleRoots)
    $nodesValue = @($nodeSummaries)

    $summary = [ordered]@{
        sceneName = $sceneNameValue
        isPlaying = $isPlayingValue
        timestampUtc = $timestampValue
        totalCanvasCount = $totalCanvasCountValue
        totalNodeCount = $totalNodeCountValue
        matchedNodeCount = $matchedNodeCountValue
        truncated = $truncatedValue
        pathPrefixes = $pathPrefixesValue
        activeRoots = $activeRootsValue
        nodes = $nodesValue
    }

    return [PSCustomObject]$summary
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

function Invoke-McpUiInvoke {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Method = "click",
        [string]$CustomMethod,
        [object[]]$Args
    )

    $body = @{
        path = $Path
        method = $Method
    }

    if (-not [string]::IsNullOrWhiteSpace($CustomMethod)) {
        $body.customMethod = $CustomMethod
    }

    if ($null -ne $Args) {
        $body.args = $Args
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
