param(
    [string]$BaseUrl,
    [string]$OutputPath = "artifacts/unity/game-flow/game-scene-phase5-preflight.json",
    [switch]$FailOnBlocked
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "McpHelpers.ps1")

function ConvertTo-RepoRelativePath {
    param(
        [string]$RepoRoot,
        [string]$Path
    )

    $resolvedRoot = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\', '/')
    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    if ($resolvedPath.StartsWith($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolvedPath.Substring($resolvedRoot.Length).TrimStart('\', '/').Replace('\', '/')
    }

    return $resolvedPath.Replace('\', '/')
}

function Get-FileSummary {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [PSCustomObject]@{
            exists = $false
            path = $Path.Replace('\', '/')
        }
    }

    $item = Get-Item -LiteralPath $Path
    return [PSCustomObject]@{
        exists = $true
        path = $Path.Replace('\', '/')
        length = $item.Length
        lastWriteTime = $item.LastWriteTime.ToString("o")
    }
}

function Get-JsonSmokeSummary {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [PSCustomObject]@{
            exists = $false
            path = $Path.Replace('\', '/')
        }
    }

    try {
        $json = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
        $evidence = $json.evidence
        return [PSCustomObject]@{
            exists = $true
            path = $Path.Replace('\', '/')
            success = $json.success
            terminalVerdict = $json.terminalVerdict
            resultMode = $json.resultMode
            generatedAt = $json.generatedAt
            newErrorCount = if ($null -ne $evidence -and $null -ne $evidence.PSObject.Properties["newErrorCount"]) { $evidence.newErrorCount } else { $null }
            hasSummonLog = if ($null -ne $evidence -and $null -ne $evidence.PSObject.Properties["hasSummonLog"]) { $evidence.hasSummonLog } else { $null }
            hasUnitKillLog = if ($null -ne $evidence -and $null -ne $evidence.PSObject.Properties["hasUnitKillLog"]) { $evidence.hasUnitKillLog } else { $null }
            hasVictoryResultLog = if ($null -ne $evidence -and $null -ne $evidence.PSObject.Properties["hasVictoryResultLog"]) { $evidence.hasVictoryResultLog } else { $null }
        }
    }
    catch {
        return [PSCustomObject]@{
            exists = $true
            path = $Path.Replace('\', '/')
            parseError = $_.Exception.Message
        }
    }
}

function Get-EditorInstanceSummary {
    param([string]$RepoRoot)

    $path = Join-Path $RepoRoot "Library/EditorInstance.json"
    if (-not (Test-Path -LiteralPath $path)) {
        return [PSCustomObject]@{
            exists = $false
            path = ConvertTo-RepoRelativePath -RepoRoot $RepoRoot -Path $path
        }
    }

    try {
        $json = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
        $processId = if ($null -ne $json.PSObject.Properties["process_id"]) { [int]$json.process_id } else { 0 }
        $alive = $false
        if ($processId -gt 0) {
            $alive = $null -ne (Get-Process -Id $processId -ErrorAction SilentlyContinue)
        }

        return [PSCustomObject]@{
            exists = $true
            path = ConvertTo-RepoRelativePath -RepoRoot $RepoRoot -Path $path
            processId = $processId
            processAlive = $alive
            version = if ($null -ne $json.PSObject.Properties["version"]) { $json.version } else { $null }
            appPath = if ($null -ne $json.PSObject.Properties["app_path"]) { $json.app_path } else { $null }
        }
    }
    catch {
        return [PSCustomObject]@{
            exists = $true
            path = ConvertTo-RepoRelativePath -RepoRoot $RepoRoot -Path $path
            parseError = $_.Exception.Message
        }
    }
}

function Find-TwoClientRunnerCandidates {
    param([string]$RepoRoot)

    $roots = @(
        (Join-Path $RepoRoot "tools"),
        (Join-Path $RepoRoot "Assets/Editor")
    )
    $patterns = @("*.ps1", "*.js", "*.mjs", "*.cs")
    $namePattern = "(TwoClient|2Client|LateJoin|Multiplayer|MultiClient|Phase5)"
    $candidates = New-Object System.Collections.Generic.List[object]

    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }

        foreach ($pattern in $patterns) {
            $items = Get-ChildItem -LiteralPath $root -Recurse -Filter $pattern -File -ErrorAction SilentlyContinue
            foreach ($item in $items) {
                if ($item.FullName -eq $PSCommandPath) {
                    continue
                }

                if ($item.Name -match $namePattern) {
                    $candidates.Add([PSCustomObject]@{
                        path = ConvertTo-RepoRelativePath -RepoRoot $RepoRoot -Path $item.FullName
                        reason = "filename"
                    })
                }
            }
        }
    }

    return $candidates.ToArray()
}

function Get-WebGlBuildSummary {
    param([string]$RepoRoot)

    $buildRoot = Join-Path $RepoRoot "Build/WebGL"
    $required = @(
        "index.html",
        "Build/WebGL.loader.js",
        "Build/WebGL.framework.js",
        "Build/WebGL.data",
        "Build/WebGL.wasm"
    )

    $files = foreach ($relative in $required) {
        Get-FileSummary -Path (Join-Path $buildRoot $relative)
    }

    return [PSCustomObject]@{
        exists = Test-Path -LiteralPath $buildRoot
        path = ConvertTo-RepoRelativePath -RepoRoot $RepoRoot -Path $buildRoot
        files = @($files)
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$resolvedBaseUrl = if ([string]::IsNullOrWhiteSpace($BaseUrl)) { Get-UnityMcpBaseUrl } else { $BaseUrl.TrimEnd("/") }

$healthSummary = $null
try {
    $health = Wait-McpBridgeHealthy -Root $resolvedBaseUrl -TimeoutSec 20
    $resolvedBaseUrl = $health.Root
    $healthSummary = [PSCustomObject]@{
        ok = $health.State.ok
        port = $health.State.port
        isPlaying = $health.State.isPlaying
        isCompiling = $health.State.isCompiling
        activeScene = $health.State.activeScene
        activeScenePath = $health.State.activeScenePath
    }
}
catch {
    $healthSummary = [PSCustomObject]@{
        ok = $false
        error = $_.Exception.Message
    }
}

$runnerCandidates = Find-TwoClientRunnerCandidates -RepoRoot $repoRoot
$singleClientBaselinePath = Join-Path $repoRoot "artifacts/unity/game-flow/game-scene-natural-victory-flow-closeout.json"
$placementContractPath = Join-Path $repoRoot "artifacts/unity/game-flow/game-scene-placement-contract-smoke.json"
$editorInstance = Get-EditorInstanceSummary -RepoRoot $repoRoot
$webGlBuild = Get-WebGlBuildSummary -RepoRoot $repoRoot
$twoClientRunnerAvailable = @($runnerCandidates).Count -gt 0

$blockedReason = if ($twoClientRunnerAvailable) {
    ""
} else {
    "two-client runner unavailable"
}
$preflightReady = $twoClientRunnerAvailable

$result = [PSCustomObject]@{
    success = $preflightReady
    terminalVerdict = if ($twoClientRunnerAvailable) { "ready" } else { "blocked" }
    blockedReason = $blockedReason
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
    evidence = [PSCustomObject]@{
        mcpHealth = $healthSummary
        editorInstance = $editorInstance
        webGlBuild = $webGlBuild
        twoClientRunnerCandidates = @($runnerCandidates)
        singleClientBaseline = Get-JsonSmokeSummary -Path $singleClientBaselinePath
        placementContract = Get-JsonSmokeSummary -Path $placementContractPath
    }
    acceptance = [PSCustomObject]@{
        singleClientBaselineAvailable = Test-Path -LiteralPath $singleClientBaselinePath
        placementContractAvailable = Test-Path -LiteralPath $placementContractPath
        twoClientRunnerAvailable = $twoClientRunnerAvailable
        manualMultiplayerSessionRequired = -not $twoClientRunnerAvailable
        actualMultiplayerAcceptance = if ($twoClientRunnerAvailable) { "not-run" } else { "blocked" }
    }
}

$resolvedOutputPath = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
} else {
    Join-Path $repoRoot $OutputPath
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $resolvedOutputPath -Encoding UTF8
$result | ConvertTo-Json -Depth 12

if ($FailOnBlocked -and -not $twoClientRunnerAvailable) {
    exit 2
}
