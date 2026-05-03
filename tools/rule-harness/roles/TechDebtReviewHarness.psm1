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
    [void]$lines.Add("- Cleanup candidates: $(@($Report.cleanupCandidates).Count)")
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
    [void]$lines.Add('## Top Cleanup Candidates')
    foreach ($candidate in @($Report.cleanupCandidates | Select-Object -First 10)) {
        [void]$lines.Add("- [$($candidate.kind)] $($candidate.path) autoApply=$($candidate.autoApply) reason=$($candidate.reason)")
    }
    if (@($Report.cleanupCandidates).Count -eq 0) {
        [void]$lines.Add('- none')
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

function Get-TechDebtTypeNames {
    param([Parameter(Mandatory)][string]$Text)

    @(
        [regex]::Matches($Text, '\b(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)') |
            ForEach-Object { [string]$_.Groups[1].Value } |
            Sort-Object -Unique
    )
}

function Get-TechDebtMetaGuid {
    param([Parameter(Mandatory)][string]$FilePath)

    $metaPath = "$FilePath.meta"
    if (-not (Test-Path -LiteralPath $metaPath)) {
        return ''
    }

    $match = Select-String -LiteralPath $metaPath -Pattern '^guid:\s*([a-fA-F0-9]+)\s*$' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $match) {
        return ''
    }

    [string]$match.Matches[0].Groups[1].Value
}

function Get-TechDebtExternalReferenceMatches {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string[]]$Tokens,
        [Parameter(Mandatory)][string[]]$ExcludedPaths
    )

    $excluded = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($path in @($ExcludedPaths)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$path)) {
            [void]$excluded.Add(([string]$path).Replace('\', '/'))
        }
    }

    $referenceMatches = [System.Collections.Generic.List[object]]::new()
    $rg = Get-Command rg -ErrorAction SilentlyContinue
    if ($null -ne $rg) {
        Push-Location $RepoRoot
        try {
            foreach ($token in @($Tokens | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)) {
                $output = @(& $rg.Source --fixed-strings --line-number --glob '!Library/**' --glob '!Temp/**' --glob '!obj/**' --glob '!bin/**' --glob '!Build/**' --glob '!Builds/**' --glob '!Logs/**' --glob '!UserSettings/**' --glob '!.git/**' --glob '!node_modules/**' -- ([string]$token) . 2>$null)
                foreach ($line in $output) {
                    if ([string]$line -notmatch '^\./?(?<path>.*?):(?<line>\d+):') {
                        continue
                    }

                    $relativePath = ([string]$Matches['path']).TrimStart('.', '/', '\').Replace('\', '/')
                    if ($excluded.Contains($relativePath)) {
                        continue
                    }

                    [void]$referenceMatches.Add([pscustomobject]@{
                        token = [string]$token
                        path = $relativePath
                        line = [int]$Matches['line']
                    })
                }
            }
        }
        finally {
            Pop-Location
        }

        return @($referenceMatches)
    }

    $root = (Resolve-Path -LiteralPath $RepoRoot).Path.TrimEnd('\', '/')
    $files = @(
        Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object {
                $relative = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
                $relative -notmatch '(^|/)(Library|Temp|obj|bin|Build|Builds|Logs|UserSettings|\.git|node_modules)/' -and
                -not $excluded.Contains($relative)
            }
    )

    foreach ($token in @($Tokens | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Sort-Object -Unique)) {
        foreach ($file in $files) {
            $hit = Select-String -LiteralPath $file.FullName -SimpleMatch -Pattern ([string]$token) -List -ErrorAction SilentlyContinue
            if ($null -eq $hit) {
                continue
            }

            [void]$referenceMatches.Add([pscustomobject]@{
                token = [string]$token
                path = $file.FullName.Substring($root.Length + 1).Replace('\', '/')
                line = [int]$hit.LineNumber
            })
        }
    }

    @($referenceMatches)
}

function Get-TechDebtSimplificationSnapshot {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $scriptPath = Join-Path $RepoRoot 'tools/workflow/Find-SimplificationCandidates.ps1'
    if (-not (Test-Path -LiteralPath $scriptPath)) {
        return $null
    }

    try {
        $json = & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath -RepoRoot $RepoRoot -AsJson 2>$null
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace(($json -join "`n"))) {
            return $null
        }

        return (($json -join "`n") | ConvertFrom-Json)
    }
    catch {
        return $null
    }
}

function Get-TechDebtDeleteUnusedCandidates {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $scriptsRoot = Join-Path $RepoRoot 'Assets/Scripts'
    if (-not (Test-Path -LiteralPath $scriptsRoot)) {
        return @()
    }

    $candidates = [System.Collections.Generic.List[object]]::new()
    $files = @(Get-ChildItem -LiteralPath $scriptsRoot -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue)
    foreach ($file in $files) {
        $relativePath = ConvertTo-TechDebtRelativePath -RepoRoot $RepoRoot -Path $file.FullName
        $normalizedPath = $relativePath.Replace('\', '/')
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -match '\b(MonoBehaviour|ScriptableObject|EditorWindow)\b') {
            continue
        }

        $typeNames = @(Get-TechDebtTypeNames -Text $content)
        if ($typeNames.Count -eq 0) {
            continue
        }

        $primaryName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $suffixMatch = $primaryName -match '(Helper|Factory|Config|Resolver|Applier|Writer|Evaluator)$'
        $allTypesMatchFile = @($typeNames | Where-Object { $_ -ne $primaryName }).Count -eq 0
        if (-not ($suffixMatch -and $allTypesMatchFile)) {
            continue
        }

        $metaRelativePath = "$normalizedPath.meta"
        $targetFiles = @($normalizedPath)
        $metaPath = "$($file.FullName).meta"
        if (Test-Path -LiteralPath $metaPath) {
            $targetFiles += $metaRelativePath
        }

        $guid = Get-TechDebtMetaGuid -FilePath $file.FullName
        $tokens = @($typeNames)
        if (-not [string]::IsNullOrWhiteSpace($guid)) {
            $tokens += $guid
        }

        $externalMatches = @(Get-TechDebtExternalReferenceMatches -RepoRoot $RepoRoot -Tokens $tokens -ExcludedPaths $targetFiles)
        if ($externalMatches.Count -gt 0) {
            continue
        }

        [void]$candidates.Add([pscustomobject]@{
            kind = 'delete_unused'
            path = $normalizedPath
            targetFiles = @($targetFiles)
            referenceTokens = @($tokens | Sort-Object -Unique)
            autoApply = $true
            confidence = 'high'
            reason = 'No external type or Unity GUID references were found.'
            evidence = @([pscustomobject]@{ path = $normalizedPath; line = $null; snippet = ($typeNames -join ', ') })
        })
    }

    @($candidates)
}

function Get-TechDebtSimplifyInlineCandidates {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $scriptsRoot = Join-Path $RepoRoot 'Assets/Scripts'
    if (-not (Test-Path -LiteralPath $scriptsRoot)) {
        return @()
    }

    $candidates = [System.Collections.Generic.List[object]]::new()
    $files = @(Get-ChildItem -LiteralPath $scriptsRoot -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue)
    foreach ($file in $files) {
        $relativePath = (ConvertTo-TechDebtRelativePath -RepoRoot $RepoRoot -Path $file.FullName).Replace('\', '/')
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -match '\b(MonoBehaviour|ScriptableObject|EditorWindow)\b') {
            continue
        }

        $typeNames = @(Get-TechDebtTypeNames -Text $content)
        if ($typeNames.Count -ne 1) {
            continue
        }

        $className = [string]$typeNames[0]
        if ($className -notmatch '(Factory|Helper)$') {
            continue
        }

        $methodMatch = [regex]::Match($content, '(?s)\bpublic\s+static\s+(?<return>[A-Za-z0-9_<>,.\s]+)\s+(?<method>[A-Za-z_][A-Za-z0-9_]*)\s*\(\s*\)\s*\{\s*return\s+(?<expr>[^;]+);\s*\}')
        if (-not $methodMatch.Success) {
            continue
        }

        $inlineExpression = [string]$methodMatch.Groups['expr'].Value.Trim()
        if ($inlineExpression -match '^[A-Za-z_][A-Za-z0-9_]*\s*\(') {
            continue
        }

        $callToken = "$className.$($methodMatch.Groups['method'].Value)()"
        $matches = @(Get-TechDebtExternalReferenceMatches -RepoRoot $RepoRoot -Tokens @($callToken) -ExcludedPaths @($relativePath, "$relativePath.meta"))
        if ($matches.Count -ne 1) {
            continue
        }

        [void]$candidates.Add([pscustomobject]@{
            kind = 'simplify_inline'
            path = $relativePath
            callerPath = [string]$matches[0].path
            targetFiles = @($relativePath, "$relativePath.meta", [string]$matches[0].path)
            referenceTokens = @($className, $callToken, (Get-TechDebtMetaGuid -FilePath $file.FullName) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
            className = $className
            methodName = [string]$methodMatch.Groups['method'].Value
            callToken = $callToken
            inlineExpression = $inlineExpression
            autoApply = $true
            confidence = 'high'
            reason = 'Single no-arg static factory/helper call can be inlined into its only caller.'
            evidence = @([pscustomobject]@{ path = $relativePath; line = $null; snippet = $callToken })
        })
    }

    @($candidates)
}

function Get-TechDebtMoveOwnerCandidates {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [object]$SimplificationSnapshot
    )

    $candidates = [System.Collections.Generic.List[object]]::new()
    if ($null -ne $SimplificationSnapshot) {
        foreach ($item in @($SimplificationSnapshot.rootRuntimeVisualHelpers)) {
            [void]$candidates.Add([pscustomobject]@{
                kind = 'move_owner'
                path = [string]$item.File
                targetFiles = @([string]$item.File)
                autoApply = $false
                confidence = 'medium'
                reason = 'Feature-root runtime visual code should move behind a presentation/owner seam.'
                evidence = @([pscustomobject]@{ path = [string]$item.File; line = $null; snippet = [string]$item.Signal })
            })
        }
    }

    $featureRootFiles = @(
        Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'Assets/Scripts/Features') -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue |
            Where-Object {
                $relative = (ConvertTo-TechDebtRelativePath -RepoRoot $RepoRoot -Path $_.FullName).Replace('\', '/')
                $relative -match '^Assets/Scripts/Features/[^/]+/[^/]+\.cs$'
            }
    )
    foreach ($file in $featureRootFiles) {
        $relativePath = (ConvertTo-TechDebtRelativePath -RepoRoot $RepoRoot -Path $file.FullName).Replace('\', '/')
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -match '\b(LineRenderer|Canvas|RectTransform|Material|Shader\.Find|new\s+GameObject|View|Preview)\b') {
            [void]$candidates.Add([pscustomobject]@{
                kind = 'move_owner'
                path = $relativePath
                targetFiles = @($relativePath)
                autoApply = $false
                confidence = 'medium'
                reason = 'Feature-root runtime visual code should move behind a presentation/owner seam.'
                evidence = @([pscustomobject]@{ path = $relativePath; line = $null; snippet = 'feature-root runtime visual code' })
            })
        }
    }

    $presentationFiles = @(
        Get-ChildItem -LiteralPath (Join-Path $RepoRoot 'Assets/Scripts/Features') -Recurse -File -Filter '*.cs' -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName.Replace('\', '/') -match '/Presentation/' }
    )
    foreach ($file in $presentationFiles) {
        $relativePath = (ConvertTo-TechDebtRelativePath -RepoRoot $RepoRoot -Path $file.FullName).Replace('\', '/')
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($content -match '\b(Register.*Input|Scroll|Renderer|Cache|PreviewTexture|Pointer|Drag)\b' -and $relativePath -match '(Adapter|Controller)\.cs$') {
            [void]$candidates.Add([pscustomobject]@{
                kind = 'move_owner'
                path = $relativePath
                targetFiles = @($relativePath)
                autoApply = $false
                confidence = 'medium'
                reason = 'Presentation adapter/controller appears to own separable input, render, or cache responsibility.'
                evidence = @([pscustomobject]@{ path = $relativePath; line = $null; snippet = 'input/render/cache responsibility signal' })
            })
        }
    }

    @($candidates | Sort-Object kind, path -Unique)
}

function Get-TechDebtAggressiveCleanupCandidates {
    param([Parameter(Mandatory)][string]$RepoRoot)

    $snapshot = Get-TechDebtSimplificationSnapshot -RepoRoot $RepoRoot
    @(
        @(Get-TechDebtDeleteUnusedCandidates -RepoRoot $RepoRoot) +
        @(Get-TechDebtSimplifyInlineCandidates -RepoRoot $RepoRoot) +
        @(Get-TechDebtMoveOwnerCandidates -RepoRoot $RepoRoot -SimplificationSnapshot $snapshot)
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
    $cleanupCandidates = @(Get-TechDebtAggressiveCleanupCandidates -RepoRoot $RepoRoot)
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
            cleanupCandidateCount = [int]@($cleanupCandidates).Count
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
        cleanupCandidates = @($cleanupCandidates)
        recommendedBatches = @($recommendedBatches)
        actionItems = @(
            @($actionItems) +
            @($cleanupCandidates | Where-Object { -not [bool]$_.autoApply } | ForEach-Object {
                [pscustomobject]@{
                    kind = 'manual-cleanup-review'
                    severity = 'medium'
                    summary = "Review cleanup move candidate: $([string]$_.path)"
                    details = [string]$_.reason
                    relatedPaths = @($_.targetFiles)
                }
            })
        )
    }

    $reportPath = Join-Path $OutputDir 'report.json'
    $summaryPath = Join-Path $OutputDir 'summary.md'
    $report | ConvertTo-Json -Depth 50 | Set-Content -Path $reportPath -Encoding UTF8
    Get-TechDebtReviewSummaryLines -Report $report | Set-Content -Path $summaryPath -Encoding UTF8
    $report
}

Export-ModuleMember -Function Invoke-TechDebtReviewHarness
