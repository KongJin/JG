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

function ConvertTo-TechDebtRelativePath {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$Path
    )

    $root = (Resolve-Path -LiteralPath $RepoRoot).Path.TrimEnd('\', '/').Replace('\', '/')
    $resolved = (Resolve-Path -LiteralPath $Path).Path.Replace('\', '/')
    if ($resolved.StartsWith("$root/", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolved.Substring($root.Length + 1)
    }

    $resolved
}

function Test-TechDebtScanFileIncluded {
    param(
        [Parameter(Mandatory)][string]$RelativePath
    )

    $normalized = $RelativePath.Replace('\', '/')
    if ($normalized -match '(^|/)(Library|Temp|obj|bin|Build|Builds|Logs|UserSettings|\.git|node_modules)/') {
        return $false
    }
    if ($normalized -match '(^|/)tools/rule-harness/tests/') {
        return $false
    }
    if ($normalized -match '(^|/)Assets/Editor/UnityMcp/') {
        return $false
    }

    $extension = [System.IO.Path]::GetExtension($normalized).ToLowerInvariant()
    $extension -in @('.cs', '.asmdef', '.uxml', '.uss', '.json', '.ps1', '.md')
}

function Get-TechDebtHeuristicPatterns {
    @(
        [pscustomobject]@{
            id = 'runtime-resources-load'
            title = 'Runtime Resources.Load dependency'
            severity = 'medium'
            regex = '\bResources\.Load\s*<'
            include = @('Assets/Scripts/')
            message = 'Runtime code is loading assets by string path; prefer injected references, Addressables, or explicit config assets.'
        },
        [pscustomobject]@{
            id = 'runtime-object-lookup'
            title = 'Runtime object lookup API'
            severity = 'medium'
            regex = '\b(GameObject\.Find|FindObjectOfType|FindAnyObjectByType|FindFirstObjectByType|FindObjectsByType)\b'
            include = @('Assets/Scripts/')
            message = 'Runtime object lookup creates hidden scene coupling and is fragile under refactors.'
        },
        [pscustomobject]@{
            id = 'runtime-placeholder'
            title = 'Runtime placeholder or generated stub'
            severity = 'high'
            regex = '(?i)\b(TODO|FIXME|HACK|placeholder|temporary stub|generated composition root placeholder)\b'
            include = @('Assets/Scripts/')
            message = 'Runtime code still contains placeholder or temporary implementation text.'
        },
        [pscustomobject]@{
            id = 'runtime-legacy-path'
            title = 'Runtime legacy compatibility path'
            severity = 'medium'
            regex = '(?i)\blegacy\b'
            include = @('Assets/Scripts/')
            message = 'Runtime code still carries legacy compatibility logic that should be isolated or retired.'
        },
        [pscustomobject]@{
            id = 'blocking-wait'
            title = 'Blocking wait in project code'
            severity = 'medium'
            regex = '\bThread\.Sleep\s*\('
            include = @('Assets/Scripts/', 'Assets/Editor/', 'tools/')
            message = 'Blocking waits make automation and runtime behavior brittle.'
        },
        [pscustomobject]@{
            id = 'empty-catch'
            title = 'Swallowed exception'
            severity = 'medium'
            regex = '\bcatch\s*(\([^)]*\))?\s*\{\s*\}'
            include = @('Assets/Scripts/', 'Assets/Editor/', 'tools/')
            message = 'Empty catch blocks hide failure signals and make recurrence diagnosis harder.'
        },
        [pscustomobject]@{
            id = 'presentation-getcomponent-fallback'
            title = 'Presentation GetComponent fallback'
            severity = 'low'
            regex = '\bGetComponent\s*<'
            include = @('Assets/Scripts/Features/')
            pathRegex = '/Presentation/'
            message = 'Presentation code falls back to component lookup instead of explicit serialized or setup-time wiring.'
        },
        [pscustomobject]@{
            id = 'active-plan-residual'
            title = 'Active plan residual debt marker'
            severity = 'low'
            regex = '(?i)\b(TODO|FIXME|HACK|placeholder|residual)\b'
            include = @('docs/plans/')
            pathRegex = '^docs/plans/progress\.md$'
            message = 'Active progress tracking still describes residual work or placeholders that need owner follow-up.'
        }
    )
}

function Get-TechDebtHeuristicFindings {
    param(
        [Parameter(Mandatory)][string]$RepoRoot
    )

    $scanRoots = @('Assets/Scripts', 'Assets/Editor', 'tools', 'docs/plans')
    $patterns = @(Get-TechDebtHeuristicPatterns)
    $findings = [System.Collections.Generic.List[object]]::new()
    $seen = @{}

    foreach ($scanRoot in $scanRoots) {
        $absoluteRoot = Join-Path $RepoRoot $scanRoot
        if (-not (Test-Path -LiteralPath $absoluteRoot)) {
            continue
        }

        $files = @(Get-ChildItem -LiteralPath $absoluteRoot -Recurse -File -ErrorAction SilentlyContinue)
        foreach ($file in $files) {
            $relativePath = ConvertTo-TechDebtRelativePath -RepoRoot $RepoRoot -Path $file.FullName
            $normalizedPath = $relativePath.Replace('\', '/')
            if (-not (Test-TechDebtScanFileIncluded -RelativePath $normalizedPath)) {
                continue
            }

            $contentLines = @(Get-Content -LiteralPath $file.FullName -ErrorAction SilentlyContinue)
            for ($index = 0; $index -lt $contentLines.Count; $index++) {
                $line = [string]$contentLines[$index]
                foreach ($pattern in $patterns) {
                    $included = $false
                    foreach ($prefix in @($pattern.include)) {
                        if ($normalizedPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                            $included = $true
                            break
                        }
                    }
                    if (-not $included) {
                        continue
                    }

                    if ($pattern.PSObject.Properties.Name -contains 'pathRegex') {
                        if ($normalizedPath -notmatch [string]$pattern.pathRegex) {
                            continue
                        }
                    }

                    if ($line -notmatch [string]$pattern.regex) {
                        continue
                    }

                    $key = "$($pattern.id)|$normalizedPath|$($index + 1)"
                    if ($seen.ContainsKey($key)) {
                        continue
                    }
                    $seen[$key] = $true

                    [void]$findings.Add([pscustomobject]@{
                        findingType = 'tech_debt'
                        severity = [string]$pattern.severity
                        title = [string]$pattern.title
                        evidence = @([pscustomobject]@{
                            path = $normalizedPath
                            line = [int]($index + 1)
                            snippet = $line.Trim()
                        })
                    })
                }
            }
        }
    }

    $severityRank = @{ high = 3; medium = 2; low = 1 }
    @(
        $findings |
            Sort-Object @{ Expression = { $severityRank[[string]$_.severity] }; Descending = $true }, @{ Expression = { [string]$_.evidence[0].path } }, @{ Expression = { [int]$_.evidence[0].line } } |
            Select-Object -First 200
    )
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
    $heuristicFindings = @(Get-TechDebtHeuristicFindings -RepoRoot $RepoRoot)
    $allReviewFindings = @($reviewedFindings + $heuristicFindings)
    $highCount = @($reviewedFindings | Where-Object severity -eq 'high').Count
    $mediumCount = @($reviewedFindings | Where-Object severity -eq 'medium').Count
    $lowCount = @($reviewedFindings | Where-Object severity -eq 'low').Count
    $heuristicHighCount = @($heuristicFindings | Where-Object severity -eq 'high').Count
    $heuristicMediumCount = @($heuristicFindings | Where-Object severity -eq 'medium').Count
    $heuristicLowCount = @($heuristicFindings | Where-Object severity -eq 'low').Count

    $findingScore = [Math]::Min(25, ($highCount * 10) + ($mediumCount * 5) + ($lowCount * 2))
    $heuristicScore = [Math]::Min(40, ($heuristicHighCount * 10) + ($heuristicMediumCount * 5) + ($heuristicLowCount * 2))
    $dependencyScore = if ([string]$snapshot.featureDependencyGate.status -eq 'failed' -or [int]$snapshot.featureDependencyGate.cycleCount -gt 0) { 15 } else { 0 }
    $compileScore = switch ([string]$snapshot.compileGate.status) {
        'failed' { 15; break }
        'blocked' { 8; break }
        'unavailable' { 8; break }
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
        $allReviewFindings |
            ForEach-Object { @($_.evidence) } |
            ForEach-Object { [string]$_.path } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    )
    $maxPathCount = @($evidencePaths | Group-Object | Sort-Object Count -Descending | Select-Object -First 1).Count
    $concentrationScore = [Math]::Min(10, [int]$maxPathCount * 2)
    $severityScore = [Math]::Min(100, $findingScore + $heuristicScore + $dependencyScore + $compileScore + $automationScore + $concentrationScore)

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
        $allReviewFindings | ForEach-Object {
            [pscustomobject]@{
                findingType = [string]$_.findingType
                severity = [string]$_.severity
                title = [string]$_.title
                evidence = @($_.evidence)
            }
        }
    )
    $severityRank = @{ high = 3; medium = 2; low = 1 }
    $refactorTargets = @(
        $allReviewFindings |
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
    $actionItems = @($snapshot.compileGate.actionItems + $snapshot.featureDependencyGate.actionItems)

    $report = [pscustomobject]@{
        runId = [string]$snapshot.runId
        baseCommitSha = [string]$snapshot.baseCommitSha
        generatedAtUtc = [string]$snapshot.generatedAtUtc
        severityScore = [int]$severityScore
        severityBand = Get-TechDebtSeverityBand -Score ([int]$severityScore)
        scoreBreakdown = [pscustomobject]@{
            findings = [int]$findingScore
            heuristicDebt = [int]$heuristicScore
            heuristicFindingCount = [int]@($heuristicFindings).Count
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
        actionItems = @($actionItems)
    }

    $reportPath = Join-Path $OutputDir 'report.json'
    $summaryPath = Join-Path $OutputDir 'summary.md'
    $report | ConvertTo-Json -Depth 50 | Set-Content -Path $reportPath -Encoding UTF8
    Get-TechDebtReviewSummaryLines -Report $report | Set-Content -Path $summaryPath -Encoding UTF8
    $report
}

Export-ModuleMember -Function Invoke-TechDebtReviewHarness
