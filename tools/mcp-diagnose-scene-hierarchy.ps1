# Capture reproducible /scene/hierarchy cases before changing the bridge.

[CmdletBinding()]
param(
    [string]$BaseUrl,
    [string]$ScenePath = "",
    [int]$Depth = 5,
    [int]$ProbeRootCount = 3,
    [string]$ResultJsonPath = "",
    [switch]$WriteJsonToStdout
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "mcp-test-common.ps1")

function Get-DefaultResultJsonPath {
    Join-Path $PSScriptRoot "..\Temp\UnityMcp\scene-hierarchy-diagnose.json"
}

function New-HierarchyCaseResult {
    param(
        [string]$Root,
        [int]$Depth,
        [bool]$IncludeComponents,
        [string]$ProbePath = ""
    )

    $query = "/scene/hierarchy?depth=$Depth&includeComponents=$($IncludeComponents.ToString().ToLowerInvariant())"
    if (-not [string]::IsNullOrWhiteSpace($ProbePath)) {
        $query += "&path=$([System.Uri]::EscapeDataString($ProbePath))"
    }

    $result = [ordered]@{
        request = [ordered]@{
            subPath = $query
            depth = $Depth
            includeComponents = $IncludeComponents
            path = $ProbePath
        }
    }

    try {
        $response = Invoke-McpGetJson -Root $Root -SubPath $query
        $result.ok = $true
        $result.response = $response
        $result.summary = [ordered]@{
            sceneName = $response.sceneName
            nodeCount = @($response.nodes).Count
            nodes = @(
                foreach ($node in @($response.nodes)) {
                    [ordered]@{
                        name = $node.name
                        path = $node.path
                        childCount = $node.childCount
                        childNames = @($node.children | ForEach-Object { $_.name })
                    }
                }
            )
        }
    }
    catch {
        $result.ok = $false
        $result.error = $_.Exception.Message
    }

    return [PSCustomObject]$result
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
$resultPathResolved = if ([string]::IsNullOrWhiteSpace($ResultJsonPath)) { Get-DefaultResultJsonPath } else { $ResultJsonPath }

if (-not [string]::IsNullOrWhiteSpace($ScenePath)) {
    Invoke-McpJson -Root $root -SubPath "/scene/open" -Body @{
        scenePath = $ScenePath
        saveCurrentSceneIfDirty = $true
    } | Out-Null
}

$health = Invoke-McpGetJson -Root $root -SubPath "/health"
$rootFalse = New-HierarchyCaseResult -Root $root -Depth $Depth -IncludeComponents:$false
$rootTrue = New-HierarchyCaseResult -Root $root -Depth $Depth -IncludeComponents:$true

$probePaths = @()
if ($rootFalse.ok) {
    $probePaths = @($rootFalse.response.nodes | Select-Object -First $ProbeRootCount | ForEach-Object { $_.path })
}

$cases = New-Object System.Collections.ArrayList
[void]$cases.Add($rootFalse)
[void]$cases.Add($rootTrue)
foreach ($probePath in $probePaths) {
    [void]$cases.Add((New-HierarchyCaseResult -Root $root -Depth $Depth -IncludeComponents:$false -ProbePath $probePath))
    [void]$cases.Add((New-HierarchyCaseResult -Root $root -Depth $Depth -IncludeComponents:$true -ProbePath $probePath))
}

$payload = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    mcpBaseUrl = $root
    health = $health
    probePaths = $probePaths
    cases = @($cases.ToArray())
}

$dir = Split-Path -Parent $resultPathResolved
if (-not [string]::IsNullOrWhiteSpace($dir) -and -not (Test-Path $dir)) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

$json = $payload | ConvertTo-Json -Depth 12
Set-Content -Path $resultPathResolved -Value $json -Encoding utf8
Write-Host "Hierarchy diagnose JSON: $resultPathResolved" -ForegroundColor DarkCyan
foreach ($case in $payload.cases) {
    $pathLabel = if ([string]::IsNullOrWhiteSpace($case.request.path)) { "<root>" } else { $case.request.path }
    if ($case.ok) {
        Write-Host ("OK depth={0} includeComponents={1} path={2} nodes={3}" -f $case.request.depth, $case.request.includeComponents, $pathLabel, $case.summary.nodeCount) -ForegroundColor Green
    }
    else {
        Write-Host ("FAIL depth={0} includeComponents={1} path={2} error={3}" -f $case.request.depth, $case.request.includeComponents, $pathLabel, $case.error) -ForegroundColor Red
    }
}

if ($WriteJsonToStdout) {
    Write-Output $json
}
