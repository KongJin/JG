param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$ScriptsRoot,
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ScriptsRoot)) {
    $ScriptsRoot = Join-Path $RepoRoot 'Assets\Scripts'
}

$coreSourcePath = Join-Path $RepoRoot 'Assets\Editor\LayerDependencyValidator.cs'
if (-not (Test-Path -LiteralPath $coreSourcePath)) {
    return [pscustomobject]@{
        outputPath = $null
        featureCount = 0
        edgeCount = 0
        hasCycles = $false
        layerViolationCount = 0
        status = 'unsupported'
        reason = 'feature-dependency-analyzer-core-missing'
    }
}

$typeDefinition = Get-Content -Path $coreSourcePath -Raw
$typePattern = 'public\s+static\s+class\s+LayerDependencyAnalyzer'
if ($typeDefinition -notmatch $typePattern) {
    return [pscustomobject]@{
        outputPath = $null
        featureCount = 0
        edgeCount = 0
        hasCycles = $false
        layerViolationCount = 0
        status = 'unsupported'
        reason = 'feature-dependency-analyzer-type-missing'
    }
}

$typeDefinition = [regex]::Replace(
    $typeDefinition,
    '(?ms)^#if !LAYER_VALIDATION_NO_UNITY\r?\nusing UnityEditor;\r?\nusing UnityEngine;\r?\n#endif\r?\n',
    ''
)
$typeDefinition = [regex]::Replace(
    $typeDefinition,
    '(?s)#if !LAYER_VALIDATION_NO_UNITY\s*namespace Editor\b.*?#endif\s*',
    ''
)
Add-Type -TypeDefinition $typeDefinition -Language CSharp

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $relativeOutput = [ProjectSD.LayerValidation.LayerDependencyAnalyzer]::DependencyReportRelativePath
    $OutputPath = Join-Path $RepoRoot $relativeOutput
}

function Convert-FeatureDependencyEvidence {
    param([ProjectSD.LayerValidation.FeatureDependencyEvidence]$Evidence)

    [pscustomobject]@{
        path = $Evidence.path
        line = $Evidence.line
    }
}

function Convert-FeatureDependencyEdge {
    param([ProjectSD.LayerValidation.FeatureDependencyEdge]$Edge)

    [pscustomobject]@{
        from = $Edge.from
        to = $Edge.to
        evidence = @($Edge.evidence | ForEach-Object { Convert-FeatureDependencyEvidence $_ })
    }
}

function Convert-FeatureDependencyCycle {
    param([ProjectSD.LayerValidation.FeatureDependencyCycle]$Cycle)

    [pscustomobject]@{
        features = @($Cycle.features)
        evidence = @($Cycle.evidence | ForEach-Object { Convert-FeatureDependencyEvidence $_ })
        edges = @($Cycle.edges | ForEach-Object { Convert-FeatureDependencyEdge $_ })
        preferredBreakCandidates = @($Cycle.preferredBreakCandidates | ForEach-Object {
            [pscustomobject]@{
                recipe = $_.recipe
                from = $_.from
                to = $_.to
                reason = $_.reason
                evidence = @($_.evidence | ForEach-Object { Convert-FeatureDependencyEvidence $_ })
            }
        })
    }
}

$analysis = [ProjectSD.LayerValidation.LayerDependencyAnalyzer]::Analyze($ScriptsRoot)
$report = $analysis.report

$jsonObject = [pscustomobject]@{
    generatedAtUtc = $report.generatedAtUtc
    featureCount = $report.featureCount
    edgeCount = $report.edgeCount
    hasCycles = $report.hasCycles
    edges = @($report.edges | ForEach-Object { Convert-FeatureDependencyEdge $_ })
    cycles = @($report.cycles | ForEach-Object { Convert-FeatureDependencyCycle $_ })
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$jsonObject | ConvertTo-Json -Depth 8 | Set-Content -Path $OutputPath -Encoding UTF8

[pscustomobject]@{
    outputPath = $OutputPath
    featureCount = $report.featureCount
    edgeCount = $report.edgeCount
    hasCycles = $report.hasCycles
    layerViolationCount = @($analysis.layerViolations).Count
    status = 'passed'
    reason = $null
}
