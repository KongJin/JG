param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string[]]$ChangedFile = @(),
    [ValidateSet("", "A", "B", "C", "Phase5", "Any")]
    [string]$Agent = "",
    [switch]$StagedOnly,
    [switch]$PlanOnly,
    [switch]$SkipCompile,
    [switch]$SkipRulesLint,
    [switch]$SkipAssetHygiene,
    [switch]$SkipUnityUiPolicy,
    [switch]$SkipArtifactScope
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\WorkflowHelpers.ps1"

$changedFiles = @(if (@($ChangedFile).Count -gt 0) {
    @(
        foreach ($file in @($ChangedFile)) {
            foreach ($part in ([string]$file -split ",")) {
                if ([string]::IsNullOrWhiteSpace($part)) {
                    continue
                }

                $part.Trim() -replace "\\", "/"
            }
        }
    ) | Sort-Object -Unique
}
else {
    @(Get-WorkflowChangedFiles -RepoRoot $RepoRoot -StagedOnly:$StagedOnly)
})

$hasCs = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("\.cs$")).Count -gt 0
$hasDocs = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("^docs/", "^AGENTS\.md$", "^\.codex/skills/", "^tools/.+README\.md$")).Count -gt 0
$hasUnityAssets = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("^Assets/.*\.(meta|prefab|unity|asset|mat|controller|anim)$")).Count -gt 0
$hasUnityUiSurface = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("^Assets/UI/", "^Assets/Scripts/Shared/Ui/", "^Assets/Prefabs/")).Count -gt 0
$hasGeneratedArtifact = @(Get-WorkflowPathsMatching -Paths $changedFiles -Patterns @("^artifacts/(unity|rules)/.*\.json$")).Count -gt 0

$planned = New-Object System.Collections.Generic.List[string]
if ($hasCs -and -not $SkipCompile) { $planned.Add("compile") }
if (($hasDocs -or $hasGeneratedArtifact) -and -not $SkipRulesLint) { $planned.Add("rules:lint") }
if ($hasUnityAssets -and -not $SkipAssetHygiene) { $planned.Add("unity:asset-hygiene") }
if ($hasUnityUiSurface -and -not $SkipUnityUiPolicy) { $planned.Add("unity-ui-policy") }
if ($hasGeneratedArtifact -and -not $SkipArtifactScope) { $planned.Add("generated-artifact-scope") }
if ($planned.Count -eq 0 -and -not $SkipRulesLint) { $planned.Add("git-diff-check") }

Write-WorkflowSection "Closeout Pack"
Write-Host ("changedFiles={0}" -f $changedFiles.Count)
if (-not [string]::IsNullOrWhiteSpace($Agent)) {
    Write-Host ("agent={0}" -f $Agent)
}
Write-Host ("planned={0}" -f ($(if ($planned.Count -gt 0) { $planned -join ", " } else { "none" })))

foreach ($path in @($changedFiles | Select-Object -First 40)) {
    Write-Host ("  {0}" -f $path)
}
if ($changedFiles.Count -gt 40) {
    Write-Host ("  ... {0} more" -f ($changedFiles.Count - 40))
}

if ($PlanOnly) {
    return
}

$results = New-Object System.Collections.Generic.List[object]

foreach ($step in $planned) {
    switch ($step) {
        "compile" {
            $results.Add((Invoke-WorkflowCommand -RepoRoot $RepoRoot -Label "compile" -FileName "powershell" -Arguments @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", (Join-Path $RepoRoot "tools\check-compile-errors.ps1"))))
        }
        "rules:lint" {
            $results.Add((Invoke-WorkflowCommand -RepoRoot $RepoRoot -Label "rules:lint" -FileName "npm" -Arguments @("run", "--silent", "rules:lint")))
        }
        "unity:asset-hygiene" {
            $results.Add((Invoke-WorkflowCommand -RepoRoot $RepoRoot -Label "unity:asset-hygiene" -FileName "npm" -Arguments @("run", "--silent", "unity:asset-hygiene")))
        }
        "unity-ui-policy" {
            $policyArguments = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", (Join-Path $RepoRoot "tools\unity-mcp\Invoke-UnityUiAuthoringWorkflowPolicy.ps1"), "-ChangedFile") + $changedFiles
            if (-not [string]::IsNullOrWhiteSpace($Agent)) {
                $policyArguments += @("-Agent", $Agent)
            }

            $results.Add((Invoke-WorkflowCommand -RepoRoot $RepoRoot -Label "unity-ui-policy" -FileName "powershell" -Arguments $policyArguments))
        }
        "generated-artifact-scope" {
            $expectedPatterns = @(
                foreach ($file in $changedFiles) {
                    "^$([regex]::Escape($file))$"
                }
            )
            $scriptPath = Join-Path $RepoRoot "tools\workflow\Test-GeneratedArtifactScope.ps1"
            $arguments = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $scriptPath, "-ExpectedPattern") + $expectedPatterns
            $results.Add((Invoke-WorkflowCommand -RepoRoot $RepoRoot -Label "generated-artifact-scope" -FileName "powershell" -Arguments $arguments))
        }
        "git-diff-check" {
            $results.Add((Invoke-WorkflowCommand -RepoRoot $RepoRoot -Label "git diff --check" -FileName "git" -Arguments @("-C", $RepoRoot, "diff", "--check")))
        }
    }
}

Write-WorkflowSection "Results"
$failed = $false
foreach ($result in $results) {
    $status = if ($result.Passed) { "PASS" } else { "FAIL" }
    Write-Host ("{0}: {1}" -f $status, $result.Label)
    if (-not [string]::IsNullOrWhiteSpace($result.Summary)) {
        Write-Host $result.Summary
    }
    if (-not $result.Passed) {
        $failed = $true
    }
}

if ($failed) {
    exit 1
}
