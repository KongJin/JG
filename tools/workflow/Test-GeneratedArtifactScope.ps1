param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path,
    [string[]]$ArtifactPath = @("artifacts/rules/issue-recurrence-closeout.json"),
    [string[]]$ExpectedPattern = @(),
    [switch]$ExplicitArtifactPathOnly,
    [switch]$StagedOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\WorkflowHelpers.ps1"

function Get-ChangedFilesFromJson {
    param([object]$Node)

    $files = New-Object System.Collections.Generic.List[string]

    function Visit {
        param([object]$Value)

        if ($null -eq $Value) {
            return
        }

        if ($Value -is [System.Array]) {
            foreach ($item in $Value) {
                Visit -Value $item
            }
            return
        }

        if ($Value -is [pscustomobject]) {
            foreach ($property in $Value.PSObject.Properties) {
                if ($property.Name -eq "changedFiles" -and $property.Value -is [System.Array]) {
                    foreach ($item in $property.Value) {
                        if (-not [string]::IsNullOrWhiteSpace([string]$item)) {
                            $files.Add(([string]$item -replace "\\", "/"))
                        }
                    }
                    continue
                }

                Visit -Value $property.Value
            }
        }
    }

    Visit -Value $Node
    return @($files | Sort-Object -Unique)
}

$currentChangedFiles = @(Get-WorkflowChangedFiles -RepoRoot $RepoRoot -StagedOnly:$StagedOnly)
$normalizedArtifactPaths = @(
    foreach ($artifact in @($ArtifactPath)) {
        if ([string]::IsNullOrWhiteSpace($artifact)) {
            continue
        }

        foreach ($part in ([string]$artifact -split ",")) {
            if (-not [string]::IsNullOrWhiteSpace($part)) {
                $part.Trim() -replace "\\", "/"
            }
        }
    }
)
$artifactPathsToCheck = if ($ExplicitArtifactPathOnly) {
    @($normalizedArtifactPaths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object -Unique)
}
else {
    @(
        @($normalizedArtifactPaths) +
        @(Get-WorkflowPathsMatching -Paths $currentChangedFiles -Patterns @("^artifacts/(unity|rules)/.*\.json$")) |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )
}
$normalizedExpectedPatterns = @(
    foreach ($pattern in @($ExpectedPattern)) {
        if ([string]::IsNullOrWhiteSpace($pattern)) {
            continue
        }

        foreach ($part in ([string]$pattern -split ",")) {
            if (-not [string]::IsNullOrWhiteSpace($part)) {
                $part.Trim()
            }
        }
    }
)
$results = @()
$hasIssue = $false

foreach ($artifact in @($artifactPathsToCheck)) {
    $absolutePath = Join-Path $RepoRoot $artifact
    if (-not (Test-Path -LiteralPath $absolutePath)) {
        $results += [PSCustomObject]@{
            Artifact = $artifact
            Exists = $false
            ChangedFiles = 0
            OffScopeFiles = @()
            Verdict = "missing"
        }
        $hasIssue = $true
        continue
    }

    $json = Get-Content -LiteralPath $absolutePath -Raw | ConvertFrom-Json
    $artifactChangedFiles = @(Get-ChangedFilesFromJson -Node $json)
    $patterns = @($normalizedExpectedPatterns)

    if ($patterns.Count -eq 0) {
        $patterns = @(
            foreach ($file in $currentChangedFiles) {
                "^$([regex]::Escape($file))$"
            }
        )
    }

    $offScope = if ($patterns.Count -gt 0) {
        @(Get-WorkflowPathsOutside -Paths $artifactChangedFiles -Patterns $patterns)
    }
    else {
        @($artifactChangedFiles)
    }

    $offScope = @($offScope)
    $verdict = if ($artifactChangedFiles.Count -eq 0) {
        "no-changed-files-field"
    }
    elseif ($offScope.Count -gt 0) {
        "broad-dirty-worktree"
    }
    else {
        "scoped"
    }

    if ($verdict -ne "scoped" -and $verdict -ne "no-changed-files-field") {
        $hasIssue = $true
    }

    $results += [PSCustomObject]@{
        Artifact = $artifact
        Exists = $true
        ChangedFiles = $artifactChangedFiles.Count
        OffScopeFiles = $offScope
        Verdict = $verdict
    }
}

Write-WorkflowSection "Generated Artifact Scope"
foreach ($result in $results) {
    Write-Host ("{0}: {1} changedFiles={2}" -f $result.Artifact, $result.Verdict, $result.ChangedFiles)
    $offScopeFiles = @($result.OffScopeFiles)
    foreach ($path in @($offScopeFiles | Select-Object -First 20)) {
        Write-Host ("  off-scope: {0}" -f $path) -ForegroundColor Yellow
    }
    if ($offScopeFiles.Count -gt 20) {
        Write-Host ("  ... {0} more" -f ($offScopeFiles.Count - 20)) -ForegroundColor Yellow
    }
}

if ($hasIssue) {
    exit 1
}
