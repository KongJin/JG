Import-Module (Join-Path $PSScriptRoot '..\RuleHarness.psm1') -Force

function Get-TechDebtSeverityBand {
    param([Parameter(Mandatory)][int]$Score)

    if ($Score -ge 80) { return 'critical' }
    if ($Score -ge 50) { return 'high' }
    if ($Score -ge 25) { return 'medium' }
    return 'low'
}

function Get-TechDebtReviewSummaryLines {
    param([Parameter(Mandatory)][object]$Report)

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add('# Tech Debt Review Harness')
    [void]$lines.Add('')
    [void]$lines.Add("- Commit SHA: $($Report.baseCommitSha)")
    [void]$lines.Add("- Severity: $($Report.severityScore)/100 ($($Report.severityBand))")
    [void]$lines.Add("- Review confidence: $($Report.reviewConfidence)")
    [void]$lines.Add("- Scanned scopes: $(@($Report.scannedScopes).Count)/$($Report.totalScopeCount)")
    [void]$lines.Add("- Review items: $(@($Report.reviewItems).Count)")
    [void]$lines.Add("- Refactor targets: $(@($Report.refactorTargets).Count)")
    [void]$lines.Add("- Recommended batches: $(@($Report.recommendedBatches).Count)")
    [void]$lines.Add('')
    [void]$lines.Add('## Score Breakdown')
    foreach ($property in $Report.scoreBreakdown.PSObject.Properties) {
        [void]$lines.Add("- $($property.Name): $($property.Value)")
    }
    [void]$lines.Add('')
    [void]$lines.Add('## Top Refactor Targets')
    foreach ($target in @($Report.refactorTargets | Select-Object -First 10)) {
        [void]$lines.Add("- $($target.path) findings=$($target.findingCount) severity=$($target.highestSeverity)")
    }
    [void]$lines.Add('')
    [void]$lines.Add('## Blockers')
    if (@($Report.blockers).Count -eq 0) {
        [void]$lines.Add('- none')
    }
    else {
        foreach ($blocker in @($Report.blockers)) {
            [void]$lines.Add("- [$($blocker.kind)] $($blocker.summary)")
        }
    }

    @($lines)
}

function Invoke-TechDebtReviewHarness {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$ConfigPath,
        [Parameter(Mandatory)][string]$OutputDir
    )

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    $snapshot = Get-RuleHarnessProjectReviewSnapshot -RepoRoot $RepoRoot -ConfigPath $ConfigPath -AllScopes -ReadOnly
    $reviewedFindings = @(ConvertTo-RuleHarnessReviewedFindings -Findings @($snapshot.findings + $snapshot.featureDependencyGate.findings))
    $highCount = @($reviewedFindings | Where-Object severity -eq 'high').Count
    $mediumCount = @($reviewedFindings | Where-Object severity -eq 'medium').Count
    $lowCount = @($reviewedFindings | Where-Object severity -eq 'low').Count

    $findingScore = [Math]::Min(35, ($highCount * 12) + ($mediumCount * 6) + ($lowCount * 2))
    $dependencyScore = if ([string]$snapshot.featureDependencyGate.status -eq 'failed' -or [int]$snapshot.featureDependencyGate.cycleCount -gt 0) { 25 } else { 0 }
    $compileScore = switch ([string]$snapshot.compileGate.status) {
        'failed' { 20; break }
        'blocked' { 12; break }
        'unavailable' { 12; break }
        default { 0 }
    }
    $automationScore = 0
    if ([string]$snapshot.compileGate.status -in @('failed', 'blocked', 'unavailable')) {
        $automationScore += 5
    }
    if (@($snapshot.featureDependencyGate.actionItems).Count -gt 0) {
        $automationScore += 5
    }
    $automationScore = [Math]::Min(10, $automationScore)

    $evidencePaths = @(
        $reviewedFindings |
            ForEach-Object { @($_.evidence) } |
            ForEach-Object { [string]$_.path } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    $maxPathCount = @($evidencePaths | Group-Object | Sort-Object Count -Descending | Select-Object -First 1).Count
    $concentrationScore = [Math]::Min(10, [int]$maxPathCount * 2)
    $severityScore = [Math]::Min(100, $findingScore + $dependencyScore + $compileScore + $automationScore + $concentrationScore)

    $blockers = [System.Collections.Generic.List[object]]::new()
    if ([string]$snapshot.compileGate.status -in @('failed', 'blocked', 'unavailable')) {
        [void]$blockers.Add([pscustomobject]@{
            kind = 'compile-gate'
            summary = "Compile gate status is $($snapshot.compileGate.status)."
            reasonCode = [string]$snapshot.compileGate.reasonCode
        })
    }
    if ([string]$snapshot.featureDependencyGate.status -eq 'failed') {
        [void]$blockers.Add([pscustomobject]@{
            kind = 'feature-dependency-gate'
            summary = "Feature dependency gate failed with $($snapshot.featureDependencyGate.cycleCount) cycle(s)."
            reasonCode = 'feature-dependency-cycle'
        })
    }

    $reviewConfidence = if (@($snapshot.scannedScopes).Count -lt [int]$snapshot.totalScopeCount) {
        'low'
    }
    elseif ([string]$snapshot.compileGate.status -in @('blocked', 'unavailable')) {
        'medium'
    }
    else {
        'high'
    }

    $reviewItems = @(
        $reviewedFindings | ForEach-Object {
            [pscustomobject]@{
                findingType = [string]$_.findingType
                severity = [string]$_.severity
                ownerDoc = [string]$_.ownerDoc
                title = [string]$_.title
                message = [string]$_.message
                remediationKind = [string]$_.remediationKind
                evidence = @($_.evidence)
            }
        }
    )
    $severityRank = @{ high = 3; medium = 2; low = 1 }
    $refactorTargets = @(
        $reviewedFindings |
            ForEach-Object {
                $finding = $_
                @($finding.evidence) | ForEach-Object {
                    if (-not [string]::IsNullOrWhiteSpace([string]$_.path)) {
                        [pscustomobject]@{ path = [string]$_.path; severity = [string]$finding.severity; title = [string]$finding.title }
                    }
                }
            } |
            Group-Object path |
            Sort-Object Count -Descending |
            ForEach-Object {
                $highest = @($_.Group | Sort-Object { $severityRank[[string]$_.severity] } -Descending | Select-Object -First 1)[0]
                [pscustomobject]@{
                    path = [string]$_.Name
                    findingCount = [int]$_.Count
                    highestSeverity = [string]$highest.severity
                    primaryTitle = [string]$highest.title
                }
            }
    )
    $scopeErrors = @($reviewedFindings | Where-Object { $_.severity -in @('high', 'medium') })
    $recommendedBatches = @(Get-RuleHarnessPlannedBatches -ReviewedFindings $scopeErrors -DocEdits @() -RepoRoot $RepoRoot)

    $report = [pscustomobject]@{
        runId = [string]$snapshot.runId
        baseCommitSha = [string]$snapshot.baseCommitSha
        generatedAtUtc = [string]$snapshot.generatedAtUtc
        severityScore = [int]$severityScore
        severityBand = Get-TechDebtSeverityBand -Score ([int]$severityScore)
        scoreBreakdown = [pscustomobject]@{
            findings = [int]$findingScore
            featureDependency = [int]$dependencyScore
            compileGate = [int]$compileScore
            automationRisk = [int]$automationScore
            concentration = [int]$concentrationScore
        }
        reviewConfidence = $reviewConfidence
        blockers = @($blockers)
        scannedScopes = @($snapshot.scannedScopes)
        totalScopeCount = [int]$snapshot.totalScopeCount
        reviewItems = @($reviewItems)
        refactorTargets = @($refactorTargets)
        recommendedBatches = @($recommendedBatches)
        actionItems = @($snapshot.compileGate.actionItems + $snapshot.featureDependencyGate.actionItems)
    }

    $reportPath = Join-Path $OutputDir 'report.json'
    $summaryPath = Join-Path $OutputDir 'summary.md'
    $report | ConvertTo-Json -Depth 50 | Set-Content -Path $reportPath -Encoding UTF8
    Get-TechDebtReviewSummaryLines -Report $report | Set-Content -Path $summaryPath -Encoding UTF8
    $report
}

Export-ModuleMember -Function Invoke-TechDebtReviewHarness
