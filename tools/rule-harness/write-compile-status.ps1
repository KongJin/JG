param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$OutputPath,
    [string]$UnityMcpBaseUrl,
    [int]$TimeoutSec = 300,
    [int]$PollIntervalMs = 100,
    [switch]$RuntimeSmokeClean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot 'Temp/RuleHarnessState/compile-status.json'
}

function Get-UnityMcpBaseUrl {
    param([string]$ExplicitBaseUrl)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitBaseUrl)) {
        return $ExplicitBaseUrl.TrimEnd("/")
    }

    $portFile = Join-Path $RepoRoot 'ProjectSettings\UnityMcpPort.txt'
    if (Test-Path -LiteralPath $portFile) {
        $portText = (Get-Content -Path $portFile -Raw).Trim()
        if ($portText -match '^\d+$') {
            return "http://127.0.0.1:$portText"
        }
    }

    return 'http://127.0.0.1:51234'
}

function Invoke-McpJson {
    param(
        [Parameter(Mandatory)]
        [string]$Root,
        [Parameter(Mandatory)]
        [string]$SubPath,
        [object]$Body = $null
    )

    $uri = "$Root$SubPath"
    if ($null -eq $Body) {
        return Invoke-RestMethod -Method Post -Uri $uri
    }

    $json = $Body | ConvertTo-Json -Compress
    return Invoke-RestMethod -Method Post -Uri $uri -ContentType 'application/json' -Body $json
}

function Invoke-McpGetJson {
    param(
        [Parameter(Mandatory)]
        [string]$Root,
        [Parameter(Mandatory)]
        [string]$SubPath
    )

    $uri = "$Root$SubPath"
    return Invoke-RestMethod -Method Get -Uri $uri
}

function Get-McpPlayModeChanging {
    param([object]$State)

    if ($null -ne $State -and $State.PSObject.Properties.Name -contains 'isPlayModeChanging') {
        return [bool]$State.isPlayModeChanging
    }

    if ($null -ne $State -and $State.PSObject.Properties.Name -contains 'isPlayingOrWillChange') {
        return [bool]$State.isPlayingOrWillChange
    }

    return $false
}

function Get-McpRecentErrors {
    param(
        [Parameter(Mandatory)]
        [string]$Root,
        [int]$Limit = 20
    )

    return Invoke-McpGetJson -Root $Root -SubPath "/console/errors?limit=$Limit"
}

function Write-CompileStatusFile {
    param(
        [Parameter(Mandatory)]
        [string]$Status,
        [Parameter(Mandatory)]
        [string]$Summary,
        [string]$Source = 'unity-mcp',
        [bool]$RuntimeSmoke = $false,
        [hashtable]$ExtraFields = @{}
    )

    $parent = Split-Path -Parent $OutputPath
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $payload = [ordered]@{
        status            = $Status
        summary           = $Summary
        source            = $Source
        checkedAtUtc      = (Get-Date).ToUniversalTime().ToString('o')
        runtimeSmokeClean = $RuntimeSmoke
    }

    foreach ($entry in $ExtraFields.GetEnumerator()) {
        $payload[$entry.Key] = $entry.Value
    }

    [pscustomobject]$payload | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputPath -Encoding UTF8
    Write-Host "Rule harness compile status written to $OutputPath ($Status)"
}

function Try-ParseUtcTimestamp {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }

    $parsed = [datetime]::MinValue
    if ([datetime]::TryParse($Value, [ref]$parsed)) {
        return $parsed.ToUniversalTime()
    }

    return $null
}

function Get-OptionalBool {
    param(
        [object]$InputObject,
        [string]$PropertyName,
        [bool]$DefaultValue = $false
    )

    if ($null -ne $InputObject -and $InputObject.PSObject.Properties.Name -contains $PropertyName) {
        return [bool]$InputObject.$PropertyName
    }

    return $DefaultValue
}

function Get-OptionalInt {
    param(
        [object]$InputObject,
        [string]$PropertyName,
        [int]$DefaultValue = 0
    )

    if ($null -ne $InputObject -and $InputObject.PSObject.Properties.Name -contains $PropertyName) {
        return [int]$InputObject.$PropertyName
    }

    return $DefaultValue
}

function Test-IsCompileErrorEntry {
    param(
        [Parameter(Mandatory)]
        [object]$Entry,
        [Parameter(Mandatory)]
        [datetime]$StartedAtUtc
    )

    $entryTimestamp = Try-ParseUtcTimestamp -Value ([string]$Entry.timestampUtc)
    if ($null -ne $entryTimestamp -and $entryTimestamp -lt $StartedAtUtc.AddSeconds(-1)) {
        return $false
    }

    $message = [string]$Entry.message
    $stackTrace = [string]$Entry.stackTrace
    $combined = "$message`n$stackTrace"

    return $combined -match '\berror\s+CS\d+\b' -or
        $combined -match 'Assets[/\\].+\(\d+,\d+\):\s*error\b' -or
        $combined -match 'All compiler errors have to be fixed before you can enter playmode!' -or
        $combined -match 'Compilation failed' -or
        $combined -match 'The type or namespace name .+ could not be found'
}

$mcpRoot = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityMcpBaseUrl

try {
    $health = Invoke-McpGetJson -Root $mcpRoot -SubPath "/health"
}
catch {
    Write-CompileStatusFile `
        -Status 'skipped' `
        -Summary ("Unity MCP health check failed: {0}" -f $_.Exception.Message) `
        -ExtraFields @{ mcpRoot = $mcpRoot }
    return
}

if (-not [bool]$health.ok) {
    Write-CompileStatusFile `
        -Status 'skipped' `
        -Summary 'Unity MCP reported ok=false, so compile-clean could not be verified.' `
        -ExtraFields @{
            mcpRoot = $mcpRoot
            activeScene = [string]$health.activeScene
        }
    return
}

if ([bool]$health.isPlaying -or (Get-McpPlayModeChanging -State $health)) {
    Write-CompileStatusFile `
        -Status 'skipped' `
        -Summary 'Unity is in or transitioning through play mode, so compile-clean verification was skipped.' `
        -ExtraFields @{
            mcpRoot = $mcpRoot
            activeScene = [string]$health.activeScene
            isPlaying = [bool]$health.isPlaying
            isPlayModeChanging = [bool](Get-McpPlayModeChanging -State $health)
        }
    return
}

$startedAtUtc = (Get-Date).ToUniversalTime()
$waitTimedOut = $false
$waitStillCompiling = $false
$waitRequestedCompilation = $false
$waitedMs = 0
$waitBody = @{
    requestFirst   = $true
    cleanBuildCache = $false
    timeoutMs      = $TimeoutSec * 1000
    pollIntervalMs = $PollIntervalMs
}

try {
    $waitResult = Invoke-McpJson -Root $mcpRoot -SubPath "/compile/wait" -Body $waitBody
}
catch {
    Write-CompileStatusFile `
        -Status 'skipped' `
        -Summary ("Unity compile wait failed: {0}" -f $_.Exception.Message) `
        -ExtraFields @{
            mcpRoot = $mcpRoot
            activeScene = [string]$health.activeScene
        }
    return
}

$waitTimedOut = Get-OptionalBool -InputObject $waitResult -PropertyName 'timedOut'
$waitStillCompiling = Get-OptionalBool -InputObject $waitResult -PropertyName 'isCompiling'
$waitRequestedCompilation = Get-OptionalBool -InputObject $waitResult -PropertyName 'requestedCompilation'
$waitedMs = Get-OptionalInt -InputObject $waitResult -PropertyName 'waitedMs'

if ($waitTimedOut -or $waitStillCompiling) {
    Write-CompileStatusFile `
        -Status 'skipped' `
        -Summary 'Unity compile wait did not complete within the allotted time.' `
        -ExtraFields @{
            mcpRoot = $mcpRoot
            activeScene = [string]$health.activeScene
            waitedMs = $waitedMs
            timedOut = $waitTimedOut
            isCompiling = $waitStillCompiling
            requestedCompilation = $waitRequestedCompilation
        }
    return
}

$recentErrors = Get-McpRecentErrors -Root $mcpRoot -Limit 100
$compileErrors = @(
    foreach ($entry in @($recentErrors.items)) {
        if (Test-IsCompileErrorEntry -Entry $entry -StartedAtUtc $startedAtUtc) {
            $entry
        }
    }
)

if ($compileErrors.Count -gt 0) {
    $topMessages = @(
        foreach ($entry in $compileErrors | Select-Object -First 3) {
            [string]$entry.message
        }
    )

    Write-CompileStatusFile `
        -Status 'failed' `
        -Summary ("Unity compile reported {0} compile error(s): {1}" -f $compileErrors.Count, ($topMessages -join ' | ')) `
        -ExtraFields @{
            mcpRoot = $mcpRoot
            activeScene = [string]$health.activeScene
            waitedMs = $waitedMs
            requestedCompilation = $waitRequestedCompilation
            compileErrorCount = $compileErrors.Count
        }
    return
}

$postHealth = Invoke-McpGetJson -Root $mcpRoot -SubPath "/health"
if ([bool]$postHealth.isCompiling) {
    Write-CompileStatusFile `
        -Status 'skipped' `
        -Summary 'Unity still reports isCompiling=true after compile wait returned.' `
        -ExtraFields @{
            mcpRoot = $mcpRoot
            activeScene = [string]$postHealth.activeScene
            waitedMs = $waitedMs
        }
    return
}

Write-CompileStatusFile `
    -Status 'passed' `
    -Summary 'Unity compile succeeded via Unity MCP.' `
    -RuntimeSmoke $RuntimeSmokeClean.IsPresent `
    -ExtraFields @{
        mcpRoot = $mcpRoot
        activeScene = [string]$postHealth.activeScene
        waitedMs = $waitedMs
        requestedCompilation = $waitRequestedCompilation
    }
