param(
    [string]$UnityBridgeUrl,
    [string]$ResultPath = "artifacts/unity/unity-ui-authoring-workflow-policy.json",
    [int]$TimeoutSec = 120
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
$resultAbsolutePath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $ResultPath))
$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl

$changedTracked = Get-GitCommandLines -RepoRoot $repoRoot -Arguments @("diff", "--name-only", "HEAD")
$changedUntracked = Get-GitCommandLines -RepoRoot $repoRoot -Arguments @("ls-files", "--others", "--exclude-standard")
$changedFiles = @($changedTracked + $changedUntracked | Sort-Object -Unique)

$addedTracked = Get-GitCommandLines -RepoRoot $repoRoot -Arguments @("diff", "--cached", "--name-only", "--diff-filter=A", "HEAD")
$addedFiles = @($addedTracked + $changedUntracked | Sort-Object -Unique)

$scenePrefabPatterns = @(
    '^Assets/Scenes/.+\.unity$',
    '^Assets/.+\.prefab$'
)
$presentationCodePatterns = @(
    '^Assets/Scripts/Features/.+/Presentation/.+\.cs$'
)
$codexLobbyPatterns = @(
    '^Assets/Scenes/CodexLobbyScene\.unity$',
    '^Assets/Editor/SceneTools/CodexLobbySceneContract\.cs$',
    '^Assets/Scripts/Features/Garage/',
    '^Assets/Scripts/Features/Lobby/'
)
$gameScenePatterns = @(
    '^Assets/Scenes/GameScene\.unity$',
    '^Assets/Resources/BattleEntity\.prefab$',
    '^Assets/Scripts/Features/(Player|Summon|Wave|Core|Battle|Combat|Placement)/'
)
$unityUiRelevantPatterns = @(
    '^Assets/Scenes/.+\.unity$',
    '^Assets/.+\.prefab$',
    '^Assets/Scripts/Features/.+/Presentation/.+\.cs$'
)

$scenePrefabFiles = Get-PathsMatching -Paths $changedFiles -Patterns $scenePrefabPatterns
$presentationFiles = Get-PathsMatching -Paths $changedFiles -Patterns $presentationCodePatterns
$codexLobbyFiles = Get-PathsMatching -Paths $changedFiles -Patterns $codexLobbyPatterns
$gameSceneFiles = Get-PathsMatching -Paths $changedFiles -Patterns $gameScenePatterns
$unityUiRelevantFiles = Get-PathsMatching -Paths $changedFiles -Patterns $unityUiRelevantPatterns
$newPrefabFiles = Get-PathsMatching -Paths $addedFiles -Patterns @('^Assets/.+\.prefab$')

$hasScenePrefab = @($scenePrefabFiles).Count -gt 0
$hasPresentationCode = @($presentationFiles).Count -gt 0
$hasCodexLobby = @($codexLobbyFiles).Count -gt 0
$hasGameScene = @($gameSceneFiles).Count -gt 0

$route = "no-unity-ui-workflow"
if ($hasCodexLobby -and $hasGameScene) {
    $route = "mixed"
}
elseif ($hasCodexLobby) {
    $route = "codex-lobby-ui"
}
elseif ($hasGameScene) {
    $route = "game-scene-ui"
}
elseif ($hasScenePrefab -and $hasPresentationCode) {
    $route = "mixed"
}
elseif ($hasScenePrefab) {
    $route = "scene/prefab authoring"
}
elseif ($hasPresentationCode) {
    $route = "presentation-code"
}

$requiredEvidence = @()
$missingEvidence = @()
$staleEvidence = @()
$policyViolations = @()
$compileSummary = $null
$layoutOwnershipSummary = $null

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
        $compileSummary = [PSCustomObject]@{
            success = [bool]$compile.Wait.ok
            request = $compile.Request
            wait = $compile.Wait
        }

        if (-not $compile.Wait.ok) {
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

    $needsLayoutValidator = $hasPresentationCode -or $route -eq "game-scene-ui" -or ($route -eq "mixed" -and $hasGameScene)
    if ($needsLayoutValidator) {
        $requiredEvidence += [PSCustomObject]@{
            name = "presentation-layout-ownership"
            path = "Temp/PresentationLayoutOwnershipValidator/presentation-layout-ownership.json"
            message = "Presentation code must not author geometry or materials. Move the authoring to scene/prefab or runtime non-presentation layer."
        }

        try {
            $layoutOwnership = Get-McpPresentationLayoutOwnership -Root $root
            $layoutOwnershipSummary = [PSCustomObject]@{
                success = [bool]$layoutOwnership.success
                reportPath = $layoutOwnership.reportPath
                violationCount = @($layoutOwnership.violations).Count
            }

            if (-not $layoutOwnership.success) {
                $policyViolations += [PSCustomObject]@{
                    code = "presentation-layout-ownership-failed"
                    message = "Presentation code must not author geometry or materials. Move the authoring to scene/prefab or runtime non-presentation layer."
                }
            }
        }
        catch {
            $layoutOwnershipSummary = [PSCustomObject]@{
                success = $false
                error = $_.Exception.Message
            }
            $policyViolations += [PSCustomObject]@{
                code = "presentation-layout-ownership-failed"
                message = ("Presentation layout ownership could not be verified. Fix the validator route and rerun the workflow. Details: {0}" -f $_.Exception.Message)
            }
        }
    }

    if (@($newPrefabFiles).Count -gt 0) {
        foreach ($path in @($newPrefabFiles)) {
            $policyViolations += [PSCustomObject]@{
                code = "new-prefab-blocked"
                message = ("New prefab detected. UI prefab creation is blocked by default; author the change in an existing scene/prefab unless the task explicitly requires a new prefab. path={0}" -f $path)
            }
        }
    }

    if ($hasCodexLobby) {
        $requiredEvidence += [PSCustomObject]@{
            name = "codex-lobby-workflow-gate"
            path = "artifacts/unity/codex-lobby-ui-workflow-result.json"
            message = "CodexLobby UI changes require a fresh workflow gate result newer than the latest modified source file."
        }
        $requiredEvidence += [PSCustomObject]@{
            name = "codex-lobby-canonical-smoke"
            path = "artifacts/unity/lobby-garage-page-switch-result.json"
            message = "CodexLobby UI changes require a fresh canonical smoke result newer than the latest modified source file."
        }

        $workflowGateCheck = Test-EvidenceFreshness `
            -RepoRoot $repoRoot `
            -EvidencePath "artifacts/unity/codex-lobby-ui-workflow-result.json" `
            -SourcePaths $codexLobbyFiles `
            -EvidenceName "codex-lobby-workflow-gate" `
            -MissingMessage "CodexLobby UI changes require a fresh workflow gate result newer than the latest modified source file." `
            -StaleMessage "CodexLobby UI changes require a fresh workflow gate result newer than the latest modified source file."
        if ($workflowGateCheck.status -eq "missing") {
            $missingEvidence += $workflowGateCheck.record
        }
        elseif ($workflowGateCheck.status -eq "stale") {
            $staleEvidence += $workflowGateCheck.record
        }

        $canonicalSmokeCheck = Test-EvidenceFreshness `
            -RepoRoot $repoRoot `
            -EvidencePath "artifacts/unity/lobby-garage-page-switch-result.json" `
            -SourcePaths $codexLobbyFiles `
            -EvidenceName "codex-lobby-canonical-smoke" `
            -MissingMessage "CodexLobby UI changes require a fresh canonical smoke result newer than the latest modified source file." `
            -StaleMessage "CodexLobby UI changes require a fresh canonical smoke result newer than the latest modified source file."
        if ($canonicalSmokeCheck.status -eq "missing") {
            $missingEvidence += $canonicalSmokeCheck.record
        }
        elseif ($canonicalSmokeCheck.status -eq "stale") {
            $staleEvidence += $canonicalSmokeCheck.record
        }
    }

    if ($hasGameScene) {
        $gameSceneEvidence = @(
            [PSCustomObject]@{
                name = "game-scene-summon-smoke"
                path = "artifacts/unity/game-scene-summon-smoke-result.json"
            }
            [PSCustomObject]@{
                name = "game-scene-placement-wave-smoke"
                path = "artifacts/unity/game-scene-placement-wave-result.json"
            }
        )

        foreach ($evidence in $gameSceneEvidence) {
            $absoluteEvidencePath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $evidence.path))
            if (-not (Test-Path -LiteralPath $absoluteEvidencePath)) {
                continue
            }

            $requiredEvidence += [PSCustomObject]@{
                name = $evidence.name
                path = $evidence.path
                message = "GameScene UI changes require existing smoke evidence to stay fresh when the artifact is already present."
            }

            $smokeCheck = Test-EvidenceFreshness `
                -RepoRoot $repoRoot `
                -EvidencePath $evidence.path `
                -SourcePaths $gameSceneFiles `
                -EvidenceName $evidence.name `
                -MissingMessage "GameScene UI changes require a fresh smoke artifact when that artifact is already part of the repo evidence set." `
                -StaleMessage "GameScene UI changes require existing smoke evidence to be newer than the latest modified source file."
            if ($smokeCheck.status -eq "missing") {
                $missingEvidence += $smokeCheck.record
            }
            elseif ($smokeCheck.status -eq "stale") {
                $staleEvidence += $smokeCheck.record
            }
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
    generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
    route = $route
    changedFiles = $changedFiles
    unityUiRelevantFiles = $unityUiRelevantFiles
    requiredEvidence = $requiredEvidence
    missingEvidence = $missingEvidence
    staleEvidence = $staleEvidence
    policyViolations = $policyViolations
    compile = $compileSummary
    presentationLayoutOwnership = $layoutOwnershipSummary
    resultPath = $resultAbsolutePath
}

Ensure-McpParentDirectory -PathValue $resultAbsolutePath
($report | ConvertTo-Json -Depth 8) | Set-Content -Path $resultAbsolutePath -Encoding UTF8
$report | ConvertTo-Json -Depth 8
