param(
    [string]$UnityBridgeUrl,
    [string]$ResultPath = "artifacts/unity/unity-ui-authoring-workflow-policy.json",
    [ValidateSet("", "A", "B", "C", "Phase5", "Any")]
    [string]$Agent = "",
    [string[]]$ChangedFile = @(),
    [int]$TimeoutSec = 120,
    [switch]$AllowCapabilityExpansion
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

function Get-GitCommandLines {
    param(
        [string]$RepoRoot,
        [string[]]$Arguments
    )

    $argumentList = @("-C", $RepoRoot) + @($Arguments)
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = "git"
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.Arguments = ($argumentList | ForEach-Object {
        if ($_ -match '\s') {
            '"' + ($_ -replace '"', '\"') + '"'
        }
        else {
            $_
        }
    }) -join ' '

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    [void]$process.Start()
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    if ($process.ExitCode -ne 0) {
        $detail = if ([string]::IsNullOrWhiteSpace($stderr)) { $stdout } else { $stderr }
        throw ("git {0} failed in {1}. {2}" -f ($Arguments -join " "), $RepoRoot, $detail.Trim())
    }

    return @(
        $stdout -split "`r?`n" |
            ForEach-Object { [string]$_ } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_.Trim() } |
            Sort-Object -Unique
    )
}

function Test-AnyPathMatch {
    param(
        [string[]]$Paths,
        [string[]]$Patterns
    )

    foreach ($path in @($Paths)) {
        foreach ($pattern in @($Patterns)) {
            if ($path -match $pattern) {
                return $true
            }
        }
    }

    return $false
}

function Get-PathsMatching {
    param(
        [string[]]$Paths,
        [string[]]$Patterns
    )

    return @(
        foreach ($path in @($Paths)) {
            foreach ($pattern in @($Patterns)) {
                if ($path -match $pattern) {
                    $path
                    break
                }
            }
        }
    ) | Sort-Object -Unique
}

function Get-LatestExistingWriteTimeUtc {
    param(
        [string]$RepoRoot,
        [string[]]$Paths
    )

    $latest = $null
    foreach ($path in @($Paths)) {
        $absolutePath = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $path))
        if (-not (Test-Path -LiteralPath $absolutePath)) {
            continue
        }

        $candidate = (Get-Item -LiteralPath $absolutePath).LastWriteTimeUtc
        if ($null -eq $latest -or $candidate -gt $latest) {
            $latest = $candidate
        }
    }

    return $latest
}

function Get-PrefabManagementSummary {
    param(
        [string]$RepoRoot
    )

    $inventoryPath = Join-Path $RepoRoot "artifacts\unity\prefab-management-inventory.json"
    $approvalPath = Join-Path $RepoRoot "artifacts\unity\prefab-management-approved-new-prefabs.json"
    $inventory = $null
    $approval = $null

    if (Test-Path -LiteralPath $inventoryPath) {
        try {
            $json = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json
            $inventory = [PSCustomObject]@{
                path = "artifacts/unity/prefab-management-inventory.json"
                generatedAt = [string]$json.generatedAt
                totalPrefabs = [int]$json.summary.totalPrefabs
                generatedPreviewPrefabs = [int]$json.summary.generatedPreviewPrefabs
                resourcesPrefabs = [int]$json.summary.resourcesPrefabs
                duplicateCandidateGroups = [int]$json.summary.duplicateCandidateGroups
                approvedNewPrefabTargets = [int]$json.summary.approvedNewPrefabTargets
            }
        }
        catch {
            $inventory = [PSCustomObject]@{
                path = "artifacts/unity/prefab-management-inventory.json"
                error = $_.Exception.Message
            }
        }
    }

    if (Test-Path -LiteralPath $approvalPath) {
        try {
            $json = Get-Content -LiteralPath $approvalPath -Raw | ConvertFrom-Json
            $approval = [PSCustomObject]@{
                path = "artifacts/unity/prefab-management-approved-new-prefabs.json"
                generatedAt = [string]$json.generatedAt
                prefabCount = @($json.prefabs).Count
                prefabPaths = @($json.prefabs | ForEach-Object { [string]$_.assetPath })
            }
        }
        catch {
            $approval = [PSCustomObject]@{
                path = "artifacts/unity/prefab-management-approved-new-prefabs.json"
                error = $_.Exception.Message
            }
        }
    }

    return [PSCustomObject]@{
        inventory = $inventory
        approvalManifest = $approval
    }
}

function New-EvidenceRecord {
    param(
        [string]$Name,
        [string]$Path,
        [string]$Message
    )

    return [PSCustomObject]@{
        name = $Name
        path = $Path
        message = $Message
    }
}

function Test-EvidenceFreshness {
    param(
        [string]$RepoRoot,
        [string]$EvidencePath,
        [string[]]$SourcePaths,
        [string]$EvidenceName,
        [string]$MissingMessage,
        [string]$StaleMessage
    )

    $absoluteEvidencePath = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $EvidencePath))
    if (-not (Test-Path -LiteralPath $absoluteEvidencePath)) {
        return [PSCustomObject]@{
            status = "missing"
            record = New-EvidenceRecord -Name $EvidenceName -Path $EvidencePath -Message $MissingMessage
        }
    }

    $latestSourceWriteUtc = Get-LatestExistingWriteTimeUtc -RepoRoot $RepoRoot -Paths $SourcePaths
    if ($null -eq $latestSourceWriteUtc) {
        return [PSCustomObject]@{
            status = "fresh"
            record = $null
        }
    }

    $evidenceWriteUtc = (Get-Item -LiteralPath $absoluteEvidencePath).LastWriteTimeUtc
    if ($evidenceWriteUtc -lt $latestSourceWriteUtc) {
        return [PSCustomObject]@{
            status = "stale"
            record = [PSCustomObject]@{
                name = $EvidenceName
                path = $EvidencePath
                message = $StaleMessage
                evidenceLastWriteUtc = $evidenceWriteUtc.ToString("o")
                latestSourceWriteUtc = $latestSourceWriteUtc.ToString("o")
            }
        }
    }

    return [PSCustomObject]@{
        status = "fresh"
        record = $null
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$defaultResultPath = "artifacts/unity/unity-ui-authoring-workflow-policy.json"
if (-not [string]::IsNullOrWhiteSpace($Agent) -and $Agent -ne "Any" -and $ResultPath -eq $defaultResultPath) {
    $ResultPath = "artifacts/unity/unity-ui-authoring-workflow-policy-$($Agent.ToLowerInvariant()).json"
}

$resultAbsolutePath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ResultPath))
$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$lobbySceneExists = Test-Path -LiteralPath ([System.IO.Path]::GetFullPath((Join-Path $repoRoot "Assets/Scenes/LobbyScene.unity")))

$changedTracked = Get-GitCommandLines -RepoRoot $repoRoot -Arguments @("diff", "--name-only", "HEAD")
$changedUntracked = Get-GitCommandLines -RepoRoot $repoRoot -Arguments @("ls-files", "--others", "--exclude-standard")
$allChangedFiles = @($changedTracked + $changedUntracked | Sort-Object -Unique)
$changedFiles = if (@($ChangedFile).Count -gt 0) {
    @(
        foreach ($path in @($ChangedFile)) {
            if ([string]::IsNullOrWhiteSpace($path)) {
                continue
            }

            ([string]$path).Replace("\", "/").Trim()
        }
    ) | Sort-Object -Unique
}
else {
    $allChangedFiles
}

$addedTracked = Get-GitCommandLines -RepoRoot $repoRoot -Arguments @("diff", "--cached", "--name-only", "--diff-filter=A", "HEAD")
$allAddedFiles = @($addedTracked + $changedUntracked | Sort-Object -Unique)
$addedFiles = if (@($ChangedFile).Count -gt 0) {
    @($changedFiles | Where-Object { $allAddedFiles -contains $_ })
}
else {
    $allAddedFiles
}

$scenePrefabPatterns = @(
    '^Assets/Scenes/.+\.unity$',
    '^Assets/.+\.prefab$'
)
$lobbyPatterns = @(
    '^Assets/Scenes/LobbyScene\.unity$',
    '^Assets/Scripts/Features/Garage/',
    '^Assets/Scripts/Features/Lobby/'
)
$gameScenePatterns = @(
    '^Assets/Scenes/BattleScene\.unity$',
    '^Assets/Resources/BattleEntity\.prefab$',
    '^Assets/Scripts/Features/(Player|Summon|Wave|Core|Battle|Combat|Placement)/'
)
$unityUiRelevantPatterns = @(
    '^Assets/Scenes/.+\.unity$',
    '^Assets/.+\.prefab$',
    '^Assets/UI/.+\.(uxml|uss|asset)$'
)
$uitkCandidatePatterns = @(
    '^Assets/UI/UIToolkit/.+\.(uxml|uss|asset)$'
)
$uitkCandidateEvidencePatterns = @(
    '^artifacts/unity/.+\.(md|json|png)$'
)
$uitkCandidatePreviewEvidencePatterns = @(
    '^artifacts/unity/.+\.png$'
)
$uitkCandidateReportEvidencePatterns = @(
    '^artifacts/unity/.+\.(md|json)$'
)
$stitchSurfaceOnboardingEvidencePatterns = @(
    '^artifacts/unity/set-[a-e]-.+\.(json|png)$',
    '^artifacts/unity/(garage|lobby|account|common|battle|result)-.+\.(json|png)$',
    '^Assets/UI/UIToolkit/.+\.(uxml|uss|asset)$'
)
$stitchSurfaceOnboardingExclusionPatterns = @(
    '^artifacts/unity/prefab-management-'
)
$stitchCapabilityExpansionPatterns = @(
    '^tools/stitch-unity/',
    '^tools/unity-mcp/',
    '^Assets/Editor/UnityMcp/',
    '^Assets/Editor/SceneTools/.+Stitch.+\.cs$'
)

$scenePrefabFiles = Get-PathsMatching -Paths $changedFiles -Patterns $scenePrefabPatterns
$lobbyFiles = Get-PathsMatching -Paths $changedFiles -Patterns $lobbyPatterns
$gameSceneFiles = Get-PathsMatching -Paths $changedFiles -Patterns $gameScenePatterns
$unityUiRelevantFiles = Get-PathsMatching -Paths $changedFiles -Patterns $unityUiRelevantPatterns
$uitkCandidateFiles = Get-PathsMatching -Paths $changedFiles -Patterns $uitkCandidatePatterns
$uitkCandidateEvidenceFiles = Get-PathsMatching -Paths $changedFiles -Patterns $uitkCandidateEvidencePatterns
$uitkCandidatePreviewEvidenceFiles = Get-PathsMatching -Paths $changedFiles -Patterns $uitkCandidatePreviewEvidencePatterns
$uitkCandidateReportEvidenceFiles = Get-PathsMatching -Paths $changedFiles -Patterns $uitkCandidateReportEvidencePatterns
$stitchSurfaceOnboardingFiles = @(
    Get-PathsMatching -Paths $changedFiles -Patterns $stitchSurfaceOnboardingEvidencePatterns |
        Where-Object {
            -not (Test-AnyPathMatch -Paths @($_) -Patterns $stitchSurfaceOnboardingExclusionPatterns)
        }
)
$stitchCapabilityExpansionFiles = Get-PathsMatching -Paths $changedFiles -Patterns $stitchCapabilityExpansionPatterns
$newPrefabFiles = Get-PathsMatching -Paths $addedFiles -Patterns @('^Assets/.+\.prefab$')
$declaredNewPrefabTargets = @()
$prefabManagementSummary = Get-PrefabManagementSummary -RepoRoot $repoRoot

$hasScenePrefab = @($scenePrefabFiles).Count -gt 0
$hasLobby = @($lobbyFiles).Count -gt 0
$hasGameScene = @($gameSceneFiles).Count -gt 0
$hasUitkCandidate = @($uitkCandidateFiles).Count -gt 0

$route = "no-unity-ui-workflow"
if ($hasLobby -and $hasGameScene) {
    $route = "mixed"
}
elseif ($hasLobby) {
    $route = if ($lobbySceneExists) { "lobby-ui" } else { "uitk-candidate" }
}
elseif ($hasGameScene) {
    $route = "game-scene-ui"
}
elseif ($hasScenePrefab) {
    $route = "scene/prefab authoring"
}
elseif ($hasUitkCandidate) {
    $route = "uitk-candidate"
}

$requiredEvidence = @()
$missingEvidence = @()
$staleEvidence = @()
$policyViolations = @()
$compileSummary = $null
$capabilityExpansionGuard = [PSCustomObject]@{
    allowCapabilityExpansion = [bool]$AllowCapabilityExpansion
    surfaceOnboardingFiles = @($stitchSurfaceOnboardingFiles)
    capabilityExpansionFiles = @($stitchCapabilityExpansionFiles)
}

if (@($stitchSurfaceOnboardingFiles).Count -gt 0 -and @($stitchCapabilityExpansionFiles).Count -gt 0 -and -not $AllowCapabilityExpansion) {
    $policyViolations += [PSCustomObject]@{
        code = "stitch-onboarding-mixed-with-capability-expansion"
        message = "Stitch screen onboarding evidence is mixed with common Stitch/Unity MCP capability or policy edits. Stop and split the work: zero-touch onboarding must not mutate shared logic; capability expansion must be declared and validated as a separate lane."
    }
}

if ($route -ne "no-unity-ui-workflow") {
    $requiredEvidence += [PSCustomObject]@{
        name = "compile-reload"
        path = $null
        message = "Compile and script reload must settle before Unity UI workflow evidence is trusted."
    }

    try {
        $health = Wait-McpBridgeHealthy -Root $root -TimeoutSec $TimeoutSec
        if ($health.State.isPlaying) {
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        }

        $compile = Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs ($TimeoutSec * 1000)
        $compileSuccess = Test-McpResponseSuccess -Response $compile.Wait
        $compileSummary = [PSCustomObject]@{
            success = $compileSuccess
            request = $compile.Request
            wait = $compile.Wait
            healthAfterWait = $compile.HealthAfterWait
        }

        if (-not $compileSuccess) {
            $policyViolations += [PSCustomObject]@{
                code = "compile-reload-failed"
                message = "Compile or reload did not settle cleanly. Fix compile errors and rerun the workflow before acceptance."
            }
        }
    }
    catch {
        $compileSummary = [PSCustomObject]@{
            success = $false
            error = $_.Exception.Message
        }
        $policyViolations += [PSCustomObject]@{
            code = "compile-reload-failed"
            message = ("Compile or reload could not be verified. Fix the MCP/editor state and rerun the workflow. Details: {0}" -f $_.Exception.Message)
        }
    }

    if (@($newPrefabFiles).Count -gt 0) {
        foreach ($path in @($newPrefabFiles)) {
            if ($declaredNewPrefabTargets -contains $path) {
                continue
            }

            $policyViolations += [PSCustomObject]@{
                code = "new-prefab-blocked"
                message = ("New prefab detected. UI prefab creation is blocked by default; author the change in an existing scene/prefab unless the task explicitly requires a new prefab. path={0}" -f $path)
            }
        }
    }

    if ($hasLobby -and -not $lobbySceneExists) {
        $requiredEvidence += [PSCustomObject]@{
            name = "uitk-candidate"
            path = $null
            message = "Lobby/Garage changes need a UI Toolkit candidate surface before fresh scene evidence can be trusted."
        }
    }

    if ($route -eq "uitk-candidate") {
        $requiredEvidence += [PSCustomObject]@{
            name = "preview-capture-report"
            path = $null
            message = "UI Toolkit candidate surfaces require a fresh preview capture/report before pilot success or runtime acceptance can be claimed."
        }

        if (@($uitkCandidatePreviewEvidenceFiles).Count -eq 0) {
            $missingEvidence += New-EvidenceRecord `
                -Name "preview-capture" `
                -Path "artifacts/unity/*.png" `
                -Message "No preview capture PNG was included with the UI Toolkit surface change. Leave the pass blocked or add a fresh preview capture before claiming pilot success."
        }

        if (@($uitkCandidateReportEvidenceFiles).Count -eq 0) {
            $missingEvidence += New-EvidenceRecord `
                -Name "preview-capture-report" `
                -Path "artifacts/unity/*.md|*.json" `
                -Message "No preview report or summary artifact was included with the UI Toolkit surface change. Leave the pass blocked or add a fresh report before claiming pilot success."
        }
    }
}

foreach ($record in @($missingEvidence)) {
    $policyViolations += [PSCustomObject]@{
        code = "missing-evidence"
        message = $record.message
    }
}

foreach ($record in @($staleEvidence)) {
    $policyViolations += [PSCustomObject]@{
        code = "stale-evidence"
        message = $record.message
    }
}

$report = [PSCustomObject]@{
    success = (@($policyViolations).Count -eq 0)
    terminalVerdict = if (@($policyViolations).Count -eq 0) { "" } else { "blocked" }
    blockedReason = if (@($policyViolations).Count -eq 0) { "" } else { [string](@($policyViolations | ForEach-Object { [string]$_.message }) -join " | ") }
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
    agent = $Agent
    route = $route
    changedFiles = @($changedFiles)
    allDirtyFileCount = @($allChangedFiles).Count
    unityUiRelevantFiles = @($unityUiRelevantFiles)
    uitkCandidateFiles = @($uitkCandidateFiles)
    uitkCandidateEvidenceFiles = @($uitkCandidateEvidenceFiles)
    uitkCandidatePreviewEvidenceFiles = @($uitkCandidatePreviewEvidenceFiles)
    uitkCandidateReportEvidenceFiles = @($uitkCandidateReportEvidenceFiles)
    requiredEvidence = @($requiredEvidence)
    missingEvidence = @($missingEvidence)
    staleEvidence = @($staleEvidence)
    policyViolations = @($policyViolations)
    compile = $compileSummary
    capabilityExpansionGuard = $capabilityExpansionGuard
    prefabManagement = $prefabManagementSummary
    declaredNewPrefabTargets = $declaredNewPrefabTargets
    resultPath = $resultAbsolutePath
}

Ensure-McpParentDirectory -PathValue $resultAbsolutePath
($report | ConvertTo-Json -Depth 8) | Set-Content -Path $resultAbsolutePath -Encoding UTF8
$report | ConvertTo-Json -Depth 8
