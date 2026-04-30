Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RuleHarnessConfig {
    param(
        [Parameter(Mandatory)]
        [string]$ConfigPath
    )

    Get-Content -Path $ConfigPath -Raw | ConvertFrom-Json
}

function ConvertTo-RuleHarnessRelativePath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$Path
    )

    $repoFull = [System.IO.Path]::GetFullPath($RepoRoot)
    $full = [System.IO.Path]::GetFullPath($Path)
    $repoUri = [Uri]("$repoFull\")
    $fileUri = [Uri]$full
    ([Uri]::UnescapeDataString($repoUri.MakeRelativeUri($fileUri).ToString())).Replace('\', '/')
}

function Test-RuleHarnessWildcardMatch {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [Parameter(Mandatory)]
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if ([WildcardPattern]::new($pattern.Replace('\', '/'), 'IgnoreCase').IsMatch($Path)) {
            return $true
        }
    }

    return $false
}

function Get-RuleHarnessMarkdownTargets {
    param(
        [Parameter(Mandatory)]
        [string]$Content
    )

    $targets = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($match in [regex]::Matches($Content, '\[[^\]]+\]\((?<target>[^)]+)\)')) {
        [void]$targets.Add($match.Groups['target'].Value.Trim())
    }

    foreach ($match in [regex]::Matches($Content, '`(?<target>/?[^`\r\n]+?\.md(?:#[^`\r\n]+)?)`')) {
        [void]$targets.Add($match.Groups['target'].Value.Trim())
    }

    @($targets)
}

function Resolve-RuleHarnessTargetPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$SourcePath,
        [Parameter(Mandatory)]
        [string]$Target
    )

    $clean = $Target.Trim()
    if ([string]::IsNullOrWhiteSpace($clean) -or
        $clean -match '^(https?:|mailto:|#)' -or
        $clean.Contains('<') -or
        $clean.Contains('*')) {
        return $null
    }

    if ($clean.Contains('#')) {
        $clean = $clean.Split('#')[0]
    }

    if ([string]::IsNullOrWhiteSpace($clean)) {
        return $null
    }

    $resolved = if ($clean.StartsWith('/')) {
        Join-Path $RepoRoot $clean.TrimStart('/')
    }
    else {
        Join-Path (Split-Path -Parent (Join-Path $RepoRoot $SourcePath)) $clean
    }

    $full = [System.IO.Path]::GetFullPath($resolved)
    $repoFull = [System.IO.Path]::GetFullPath($RepoRoot)
    if (-not $full.StartsWith($repoFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }

    [pscustomobject]@{
        FullPath     = $full
        RelativePath = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $full
        Exists       = Test-Path -LiteralPath $full
    }
}

function New-RuleHarnessFinding {
    param(
        [Parameter(Mandatory)]
        [string]$FindingType,
        [Parameter(Mandatory)]
        [string]$Severity,
        [Parameter(Mandatory)]
        [string]$OwnerDoc,
        [Parameter(Mandatory)]
        [string]$Title,
        [Parameter(Mandatory)]
        [string]$Message,
        [Parameter(Mandatory)]
        [object[]]$Evidence,
        [string]$Confidence = 'high',
        [string]$Source = 'static',
        [string]$RemediationKind = 'report_only',
        [string]$Rationale = '',
        [object]$ProposedDocEdit = $null
    )

    [pscustomobject]@{
        findingType     = $FindingType
        severity        = $Severity
        ownerDoc        = $OwnerDoc
        title           = $Title
        message         = $Message
        confidence      = $Confidence
        source          = $Source
        remediationKind = $RemediationKind
        rationale       = $Rationale
        evidence        = $Evidence
        proposedDocEdit = $ProposedDocEdit
    }
}

function New-RuleHarnessStageResult {
    param(
        [Parameter(Mandatory)]
        [string]$Stage,
        [Parameter(Mandatory)]
        [string]$Status,
        [Parameter(Mandatory)]
        [bool]$Attempted,
        [Parameter(Mandatory)]
        [string]$Summary,
        $Details = $null
    )

    [pscustomobject]@{
        stage     = $Stage
        status    = $Status
        attempted = $Attempted
        summary   = $Summary
        details   = $Details
    }
}

function New-RuleHarnessActionItem {
    param(
        [Parameter(Mandatory)]
        [string]$Kind,
        [Parameter(Mandatory)]
        [string]$Severity,
        [Parameter(Mandatory)]
        [string]$Summary,
        [Parameter(Mandatory)]
        [string]$Details,
        [string[]]$RelatedPaths = @()
    )

    [pscustomobject]@{
        kind         = $Kind
        severity     = $Severity
        summary      = $Summary
        details      = $Details
        relatedPaths = @($RelatedPaths | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -Unique)
    }
}

function Get-RuleHarnessFeedbackPathHints {
    param(
        [string]$ReportPathHint,
        [string]$SummaryPathHint,
        [string]$LogPathHint
    )

    $parts = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($ReportPathHint)) {
        [void]$parts.Add("report: $ReportPathHint")
    }
    if (-not [string]::IsNullOrWhiteSpace($SummaryPathHint)) {
        [void]$parts.Add("summary: $SummaryPathHint")
    }
    if (-not [string]::IsNullOrWhiteSpace($LogPathHint)) {
        [void]$parts.Add("log: $LogPathHint")
    }

    if ($parts.Count -eq 0) {
        return $null
    }

    "Inspect " + ($parts -join ', ')
}

function Get-RuleHarnessCombinedPaths {
    param(
        [string[]]$Primary = @(),
        [string[]]$Secondary = @()
    )

    @(@($Primary) + @($Secondary) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -Unique)
}

function Merge-RuleHarnessActionItems {
    param(
        [object[]]$Items
    )

    $merged = [System.Collections.Generic.List[object]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($item in @($Items)) {
        if ($null -eq $item) {
            continue
        }

        $key = '{0}|{1}|{2}|{3}' -f [string]$item.kind, [string]$item.severity, [string]$item.summary, [string]$item.details
        if ($seen.Add($key)) {
            [void]$merged.Add($item)
        }
    }

    @($merged)
}

function Get-RuleHarnessActionItemsForFindings {
    param(
        [AllowEmptyCollection()]
        [object[]]$Findings,
        [string]$ReportPathHint,
        [string]$SummaryPathHint,
        [string]$LogPathHint
    )

    $pathHint = Get-RuleHarnessFeedbackPathHints -ReportPathHint $ReportPathHint -SummaryPathHint $SummaryPathHint -LogPathHint $LogPathHint
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($finding in @($Findings)) {
        $relatedPaths = [System.Collections.Generic.List[string]]::new()
        if (-not [string]::IsNullOrWhiteSpace([string]$finding.ownerDoc)) {
            [void]$relatedPaths.Add([string]$finding.ownerDoc)
        }
        foreach ($evidence in @($finding.evidence)) {
            if ($null -ne $evidence -and -not [string]::IsNullOrWhiteSpace([string]$evidence.path)) {
                [void]$relatedPaths.Add([string]$evidence.path)
            }
        }

        if ([string]$finding.source -eq 'harness') {
            $details = [string]$finding.message
            if ($pathHint) {
                $details = "$details. $pathHint."
            }

            $kind = if ([string]$finding.title -match 'agent review failed') { 'check-llm-connectivity' } else { 'check-harness-stage' }
            [void]$items.Add((New-RuleHarnessActionItem `
                -Kind $kind `
                -Severity ([string]$finding.severity) `
                -Summary ([string]$finding.title) `
                -Details $details `
                -RelatedPaths @($relatedPaths)))
            continue
        }

        if ([string]$finding.title -eq 'Hardcoded MCP UI smoke reintroduced') {
            $details = [string]$finding.message
            if ($pathHint) {
                $details = "$details. $pathHint."
            }

            [void]$items.Add((New-RuleHarnessActionItem `
                -Kind 'remove-hardcoded-mcp-ui-smoke' `
                -Severity ([string]$finding.severity) `
                -Summary 'Remove hardcoded MCP UI smoke from automation' `
                -Details $details `
                -RelatedPaths @($relatedPaths)))
            continue
        }

        if ([string]$finding.remediationKind -eq 'rule_fix') {
            $details = [string]$finding.message
            if ($pathHint) {
                $details = "$details. $pathHint."
            }

            [void]$items.Add((New-RuleHarnessActionItem `
                -Kind 'update-owner-doc' `
                -Severity ([string]$finding.severity) `
                -Summary ("Update {0}" -f [string]$finding.ownerDoc) `
                -Details $details `
                -RelatedPaths @($relatedPaths)))
        }
    }

    Merge-RuleHarnessActionItems -Items @($items)
}

function Get-RuleHarnessActionItemForSkippedBatch {
    param(
        [Parameter(Mandatory)]
        [object]$SkippedBatch,
        [object]$PlannedBatch,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $reasonCode = [string]$SkippedBatch.reasonCode
    $targetFiles = if ($null -ne $PlannedBatch -and $PlannedBatch.PSObject.Properties.Name -contains 'targetFiles') {
        @($PlannedBatch.targetFiles)
    }
    else {
        @()
    }

    switch ($reasonCode) {
        'rules-scope-mutation-violation' {
            return New-RuleHarnessActionItem `
                -Kind 'rules-scope-mutation-violation' `
                -Severity 'high' `
                -Summary ("Rules-only scope blocked batch {0}" -f [string]$SkippedBatch.id) `
                -Details ([string]$SkippedBatch.reason) `
                -RelatedPaths (Get-RuleHarnessCombinedPaths -Primary $targetFiles -Secondary @($SkippedBatch.targets))
        }
        'manual-validation-required' {
            return New-RuleHarnessActionItem `
                -Kind 'manual-validation-required' `
                -Severity 'high' `
                -Summary ("Manual validation required for batch {0}" -f [string]$SkippedBatch.id) `
                -Details ([string]$SkippedBatch.reason) `
                -RelatedPaths $targetFiles
        }
        'dirty-target-files' {
            $targetsText = if ($SkippedBatch.PSObject.Properties.Name -contains 'targets' -and @($SkippedBatch.targets).Count -gt 0) {
                @($SkippedBatch.targets) -join ', '
            }
            else {
                @($targetFiles) -join ', '
            }
            return New-RuleHarnessActionItem `
                -Kind 'clean-target-files' `
                -Severity 'medium' `
                -Summary ("Clean target files before rerunning batch {0}" -f [string]$SkippedBatch.id) `
                -Details ("The batch was suppressed because target files are already dirty: {0}." -f $targetsText) `
                -RelatedPaths (Get-RuleHarnessCombinedPaths -Primary $targetFiles -Secondary @($SkippedBatch.targets))
        }
        'ownership-preflight-rejected' {
            return New-RuleHarnessActionItem `
                -Kind 'fix-ownership-scope' `
                -Severity 'high' `
                -Summary ("Fix ownership mismatch for batch {0}" -f [string]$SkippedBatch.id) `
                -Details ([string]$SkippedBatch.reason) `
                -RelatedPaths $targetFiles
        }
        'risk-threshold-exceeded' {
            return New-RuleHarnessActionItem `
                -Kind 'review-risk-threshold' `
                -Severity 'medium' `
                -Summary ("Review risk threshold for batch {0}" -f [string]$SkippedBatch.id) `
                -Details ([string]$SkippedBatch.reason) `
                -RelatedPaths $targetFiles
        }
        'max-attempts-reached' {
            return New-RuleHarnessActionItem `
                -Kind 'inspect-repeated-failure' `
                -Severity 'medium' `
                -Summary ("Inspect repeated failures for batch {0}" -f [string]$SkippedBatch.id) `
                -Details ("This batch already hit the retry limit on the current commit. Check validation failures or history before retrying.") `
                -RelatedPaths $targetFiles
        }
        default {
            return $null
        }
    }
}

function Get-RuleHarnessActionItemForUnplannedFinding {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    $family = Get-RuleHarnessFindingFamily -Finding $Finding
    $primaryPath = Get-RuleHarnessFindingPrimaryEvidencePath -Finding $Finding
    $snippet = $null
    foreach ($evidence in @($Finding.evidence)) {
        if ($null -ne $evidence -and -not [string]::IsNullOrWhiteSpace([string]$evidence.snippet)) {
            $snippet = [string]$evidence.snippet
            break
        }
    }

    $details = [string]$Finding.message
    switch ($family) {
        'code_violation/application_unity_api' {
            if ($snippet -match 'Debug\s*\.\s*Log') {
                $details = "Rule harness detected an Application-layer Unity logging dependency but does not yet have a safe auto-fix recipe for direct Debug.Log calls. Refactor this flow manually or add a logging/event recipe."
            }
            elseif ($snippet -match 'UnityEngine\.') {
                $details = "Rule harness detected an Application-layer fully-qualified Unity API reference but could not derive a safe rewrite from the current recipe set. Check constructor injection/call-site wiring or add a narrower recipe."
            }
            elseif ($snippet -match '\bMonoBehaviour\b|\bGameObject\b|\bSprite\b|\bAudioClip\b|\bColor\b') {
                $details = "Rule harness detected an Application-layer Unity object dependency but does not yet know how to move this type behind setup injection or a port automatically."
            }
            else {
                $details = "Rule harness detected an Application-layer Unity dependency but no safe auto-fix recipe matched this pattern."
            }
        }
        'code_violation/domain_framework_api' {
            $details = "Rule harness detected a Domain-layer framework dependency but no safe auto-fix recipe matched this pattern."
        }
        'missing_rule/feature_bootstrap_root' {
            $details = "Rule harness expected to scaffold a feature root setup/bootstrap file, but no safe target path was derived from the finding evidence."
        }
        default {
            $details = "Rule harness could not build a safe auto-fix batch for this finding from the current recipe set."
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($primaryPath)) {
        $details = "{0} Primary target: {1}." -f $details.TrimEnd('.'), $primaryPath
    }

    $relatedPaths = Get-RuleHarnessCombinedPaths `
        -Primary @([string]$Finding.ownerDoc) `
        -Secondary @(@($Finding.evidence | ForEach-Object { [string]$_.path }))

    New-RuleHarnessActionItem `
        -Kind 'expand-auto-fix-coverage' `
        -Severity ([string]$Finding.severity) `
        -Summary ("Auto-fix recipe missing for {0}" -f [string]$Finding.title) `
        -Details $details `
        -RelatedPaths $relatedPaths
}

function Get-RuleHarnessFeatureDirectories {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $featureRoot = Join-Path $RepoRoot 'Assets/Scripts/Features'
    if (-not (Test-Path -LiteralPath $featureRoot)) {
        return @()
    }

    @(Get-ChildItem -LiteralPath $featureRoot -Directory | Sort-Object Name)
}

function Get-RuleHarnessScopeDocs {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $docs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    foreach ($pattern in $Config.ssot.scopeDocs) {
        $expanded = Join-Path $RepoRoot $pattern
        foreach ($file in Get-ChildItem -Path $expanded -File -ErrorAction SilentlyContinue) {
            [void]$docs.Add((ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $file.FullName))
        }
    }

    foreach ($doc in Get-RuleHarnessClaudeReferencedDocs -RepoRoot $RepoRoot) {
        [void]$docs.Add($doc)
    }

    @($docs | Sort-Object)
}

function Get-RuleHarnessClaudeReferencedDocs {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $claude = Join-Path $RepoRoot 'AGENTS.md'
    if (-not (Test-Path -LiteralPath $claude)) {
        return @()
    }

    $docs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($target in Get-RuleHarnessMarkdownTargets -Content (Get-Content -Path $claude -Raw)) {
        $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath 'AGENTS.md' -Target $target
        if ($null -ne $resolved) {
            [void]$docs.Add($resolved.RelativePath)
        }
    }

    @($docs | Sort-Object)
}

function Get-RuleHarnessPreferredClaudeDoc {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string[]]$Keywords
    )

    $claude = Join-Path $RepoRoot 'AGENTS.md'
    $referencedDocs = @(Get-RuleHarnessClaudeReferencedDocs -RepoRoot $RepoRoot)
    if (-not (Test-Path -LiteralPath $claude)) {
        return 'AGENTS.md'
    }

    $keywordPattern = if (@($Keywords).Count -gt 0) {
        ($Keywords | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | ForEach-Object { [regex]::Escape([string]$_) }) -join '|'
    }
    else {
        $null
    }

    foreach ($line in Get-Content -Path $claude) {
        if (-not [string]::IsNullOrWhiteSpace($keywordPattern) -and $line -notmatch $keywordPattern) {
            continue
        }

        $lineTargets = @(Get-RuleHarnessMarkdownTargets -Content $line)
        $preferredTargets = if (-not [string]::IsNullOrWhiteSpace($keywordPattern)) {
            @($lineTargets | Where-Object { [string]$_ -match $keywordPattern })
        }
        else {
            @()
        }
        $candidateTargets = if (@($preferredTargets).Count -gt 0) {
            @($preferredTargets) + @($lineTargets | Where-Object { $preferredTargets -notcontains $_ })
        }
        else {
            @($lineTargets)
        }

        foreach ($target in $candidateTargets) {
            $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath 'AGENTS.md' -Target $target
            if ($null -ne $resolved -and $resolved.Exists) {
                return $resolved.RelativePath
            }
        }
    }

    foreach ($doc in @($referencedDocs)) {
        foreach ($keyword in @($Keywords)) {
            if ($doc -match ([regex]::Escape([string]$keyword))) {
                return $doc
            }
        }
    }

    return 'AGENTS.md'
}

function Get-RuleHarnessArchitectureOwnerDoc {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    Get-RuleHarnessPreferredClaudeDoc `
        -RepoRoot $RepoRoot `
        -Keywords @('architecture', '피처 경계', '레이어', '포트', '네이밍', 'folders', 'dependencies', 'folder')
}

function Get-RuleHarnessGovernanceOwnerDoc {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    Get-RuleHarnessPreferredClaudeDoc `
        -RepoRoot $RepoRoot `
        -Keywords @('work_principles', '문서 소유권', 'SSOT', '운영 원칙', 'ownership', 'governance')
}

function Get-RuleHarnessAntiPatternsOwnerDoc {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    Get-RuleHarnessPreferredClaudeDoc `
        -RepoRoot $RepoRoot `
        -Keywords @('anti_patterns', '금지 패턴', '하지 말아야', 'anti pattern', '예외 판단')
}

function Get-RuleHarnessEventRulesOwnerDoc {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    Get-RuleHarnessPreferredClaudeDoc `
        -RepoRoot $RepoRoot `
        -Keywords @('event_rules', '이벤트 체인', '이벤트 vs 직접 호출', 'EventBus', '직접 호출')
}

function Get-RuleHarnessValidationGatesOwnerDoc {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    Get-RuleHarnessPreferredClaudeDoc `
        -RepoRoot $RepoRoot `
        -Keywords @('validation_gates', 'clean', 'compile-clean', 'runtime-smoke-clean', '검증 게이트')
}

function Test-RuleHarnessGlobalRuleDoc {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$RelativePath
    )

    $normalized = $RelativePath.Replace('\', '/')
    if ($normalized -eq 'AGENTS.md') {
        return $true
    }

    $normalized -in @(Get-RuleHarnessClaudeReferencedDocs -RepoRoot $RepoRoot)
}

function Get-RuleHarnessScriptFiles {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $root = Join-Path $RepoRoot 'Assets/Scripts'
    if (-not (Test-Path -LiteralPath $root)) {
        return @()
    }

    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.cs') {
        $relative = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $file.FullName
        if (-not (Test-RuleHarnessWildcardMatch -Path $relative -Patterns $Config.scan.exclude)) {
            [void]$files.Add($file)
        }
    }

    @($files)
}

function Get-RuleHarnessOwningRuleDocForScript {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$RelativeScriptPath
    )

    if ($RelativeScriptPath -like 'Assets/Scripts/*') {
        return (Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot)
    }

    if ($RelativeScriptPath -like 'Assets/Editor/UnityMcp/*') {
        return (Get-RuleHarnessPreferredClaudeDoc -RepoRoot $RepoRoot -Keywords @('unity_mcp', 'Unity MCP', 'MCP', 'editor automation'))
    }

    return $null
}

function Get-RuleHarnessDocumentedCustomPropertyKeys {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$OwnerDoc
    )

    $docFull = Join-Path $RepoRoot $OwnerDoc
    if (-not (Test-Path -LiteralPath $docFull)) {
        return @()
    }

    $keys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($match in [regex]::Matches((Get-Content -Path $docFull -Raw), '`(?<key>[A-Za-z][A-Za-z0-9_]*)`')) {
        [void]$keys.Add($match.Groups['key'].Value)
    }

    @($keys)
}

function Get-RuleHarnessCustomPropertyKeysFromFile {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath
    )

    $lines = Get-Content -Path $FilePath
    if (-not ($lines -match 'SetCustomProperties')) {
        return @()
    }

    $keys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch 'SetCustomProperties') {
            continue
        }

        $start = [Math]::Max(0, $i - 20)
        $end = [Math]::Min($lines.Count - 1, $i + 5)
        $window = ($lines[$start..$end] -join "`n")
        foreach ($match in [regex]::Matches($window, '"(?<key>[A-Za-z][A-Za-z0-9_]*)"')) {
            [void]$keys.Add($match.Groups['key'].Value)
        }
    }

    @($keys)
}

function Get-RuleHarnessFirstMatchLine {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string[]]$Lines,
        [Parameter(Mandatory)]
        [string]$Pattern
    )

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match $Pattern) {
            return ($i + 1)
        }
    }

    return $null
}

function Get-RuleHarnessLineSnippet {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string[]]$Lines,
        [int]$Line
    )

    if ($null -eq $Line) {
        return $Lines[0]
    }

    $index = [Math]::Max(0, $Line - 1)
    return $Lines[$index]
}

function Test-RuleHarnessCommentOnlyLine {
    param(
        [AllowEmptyString()]
        [string]$Line
    )

    [string]$Line -match '^\s*(//|///|/\*|\*|\*/)'
}

function Find-RuleHarnessPatternEvidence {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string[]]$Lines,
        [Parameter(Mandatory)]
        [string[]]$Patterns
    )

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = [string]$Lines[$i]
        if (Test-RuleHarnessCommentOnlyLine -Line $line) {
            continue
        }

        foreach ($pattern in @($Patterns)) {
            if ($line -match $pattern) {
                return [pscustomobject]@{
                    line    = ($i + 1)
                    snippet = $line
                    pattern = $pattern
                }
            }
        }
    }

    return $null
}

function Get-RuleHarnessScriptTextWithoutComments {
    param(
        [AllowEmptyString()]
        [string[]]$Lines
    )

    $content = [string]::Join("`n", @($Lines))
    $withoutBlockComments = [regex]::Replace($content, '(?s)/\*.*?\*/', '')
    [regex]::Replace($withoutBlockComments, '(?m)//.*$', '')
}

function Get-RuleHarnessCodeWithoutCommentsOrStrings {
    param(
        [AllowEmptyString()]
        [string]$Content
    )

    $contentWithoutComments = Get-RuleHarnessScriptTextWithoutComments -Lines @($Content -split "\r?\n")
    $withoutDoubleQuotedStrings = [regex]::Replace($contentWithoutComments, '"(?:[^"\\]|\\.)*"', '""')
    [regex]::Replace($withoutDoubleQuotedStrings, "'(?:[^'\\]|\\.)*'", "''")
}

function Get-RuleHarnessUsingNamespaces {
    param(
        [AllowEmptyString()]
        [string[]]$Lines
    )

    $namespaces = [System.Collections.Generic.List[string]]::new()
    foreach ($line in @($Lines)) {
        $match = [regex]::Match([string]$line, '^\s*using\s+(?:static\s+)?(?:\w+\s*=\s*)?(?<ns>[\w.]+)\s*;')
        if ($match.Success) {
            [void]$namespaces.Add([string]$match.Groups['ns'].Value)
        }
    }

    @($namespaces | Select-Object -Unique)
}

function Test-RuleHarnessHasUsingNamespace {
    param(
        [AllowEmptyString()]
        [string[]]$Lines,
        [Parameter(Mandatory)]
        [string]$Namespace
    )

    foreach ($line in @($Lines)) {
        if ([string]$line -match ("^\s*using\s+(?:static\s+)?(?:\w+\s*=\s*)?{0}\s*;" -f [regex]::Escape($Namespace))) {
            return $true
        }
    }

    return $false
}

function Get-RuleHarnessLayerFromRelativePath {
    param(
        [string]$RelativePath
    )

    $normalized = [string]$RelativePath
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $null
    }

    $normalized = $normalized.Replace('\', '/')
    if ($normalized -match '^Assets/Scripts/Shared/') {
        return 'Shared'
    }
    if ($normalized -match '^Assets/Scripts/Features/[^/]+/[^/]+(?:Setup|Bootstrap)\.cs$') {
        return 'Bootstrap'
    }
    if ($normalized -match '/Infrastructure/') {
        return 'Infrastructure'
    }
    if ($normalized -match '/Application/') {
        return 'Application'
    }
    if ($normalized -match '/Domain/') {
        return 'Domain'
    }

    return $null
}

function Get-RuleHarnessLayerViolationForUsing {
    param(
        [string]$CurrentLayer,
        [string]$Namespace
    )

    if ([string]::IsNullOrWhiteSpace($CurrentLayer) -or [string]::IsNullOrWhiteSpace($Namespace)) {
        return $null
    }

    switch ($CurrentLayer) {
        'Domain' {
            if ($Namespace -match '^Features\.\w+\.Application(\.|$)') { return 'Domain -> Application forbidden' }
            if ($Namespace -match '^Features\.\w+\.Infrastructure(\.|$)') { return 'Domain -> Infrastructure forbidden' }
            if ($Namespace -match '^Features\.\w+\.Bootstrap(\.|$)') { return 'Domain -> Bootstrap forbidden' }
        }
        'Application' {
            if ($Namespace -match '^Features\.\w+\.Infrastructure(\.|$)') { return 'Application -> Infrastructure forbidden' }
            if ($Namespace -match '^Features\.\w+\.Bootstrap(\.|$)') { return 'Application -> Bootstrap forbidden' }
        }
        'Infrastructure' {
            if ($Namespace -match '^Features\.\w+\.Bootstrap(\.|$)') { return 'Infrastructure -> Bootstrap forbidden' }
        }
        'Shared' {
            if ($Namespace -match '^Features\.') { return 'Shared -> Features forbidden' }
        }
    }

    return $null
}

function Find-RuleHarnessShortTypeShadowingEvidence {
    param(
        [AllowEmptyString()]
        [string[]]$Lines,
        [Parameter(Mandatory)]
        [string]$ShortTypeName
    )

    $pattern = "(?<![\w\.])$([regex]::Escape($ShortTypeName))(?:\s*\[\]|\s+[A-Za-z_][A-Za-z0-9_]*|\s*\()"
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = [string]$Lines[$i]
        if (Test-RuleHarnessCommentOnlyLine -Line $line) {
            continue
        }
        $lineForMatch = [regex]::Replace($line, '"(?:[^"\\]|\\.)*"', '""')
        $lineForMatch = [regex]::Replace($lineForMatch, "'(?:[^'\\]|\\.)*'", "''")
        if ($line -match '^\s*(using|namespace)\b') {
            continue
        }
        if ($lineForMatch -match ("^\s*(?:public|internal|private|protected)?\s*(?:sealed\s+)?(?:partial\s+)?(?:class|struct|interface|enum)\s+{0}\b" -f [regex]::Escape($ShortTypeName))) {
            continue
        }
        if ($lineForMatch -match ("Features\.[A-Za-z0-9_\.]*\.{0}\b" -f [regex]::Escape($ShortTypeName))) {
            continue
        }

        if ($lineForMatch -match $pattern) {
            return [pscustomobject]@{
                line    = ($i + 1)
                snippet = $line
                pattern = $pattern
            }
        }
    }

    return $null
}

function Get-RuleHarnessFindingKey {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    '{0}|{1}|{2}' -f $Finding.findingType, $Finding.ownerDoc, $Finding.title
}

function Get-RuleHarnessCurrentBranch {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $branch = Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', '--abbrev-ref', 'HEAD')
    if ($LASTEXITCODE -ne 0) {
        return $null
    }

    ($branch | Select-Object -First 1).Trim()
}

function Get-RuleHarnessCompileStatusPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $relativePath = if ($Config.PSObject.Properties.Name -contains 'state' -and
        $Config.state.PSObject.Properties.Name -contains 'compileStatusPath' -and
        -not [string]::IsNullOrWhiteSpace([string]$Config.state.compileStatusPath)) {
        [string]$Config.state.compileStatusPath
    }
    else {
        'Temp/RuleHarnessState/compile-status.json'
    }

    [pscustomobject]@{
        relativePath = $relativePath
        fullPath     = Join-Path $RepoRoot $relativePath
    }
}

function Get-RuleHarnessFeatureDependencyReportPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $relativePath = if ($Config.PSObject.Properties.Name -contains 'state' -and
        $Config.state.PSObject.Properties.Name -contains 'featureDependencyReportPath' -and
        -not [string]::IsNullOrWhiteSpace([string]$Config.state.featureDependencyReportPath)) {
        [string]$Config.state.featureDependencyReportPath
    }
    else {
        'Temp/LayerDependencyValidator/feature-dependencies.json'
    }

    [pscustomobject]@{
        relativePath = $relativePath
        fullPath     = Join-Path $RepoRoot $relativePath
    }
}

function ConvertTo-RuleHarnessFeatureDependencyEvidence {
    param(
        [Parameter(Mandatory)]
        [object]$Evidence,
        [string]$Snippet
    )

    [pscustomobject]@{
        path    = [string]$Evidence.path
        line    = if ($Evidence.PSObject.Properties.Name -contains 'line') { [int]$Evidence.line } else { $null }
        snippet = $Snippet
    }
}

function Get-RuleHarnessFeatureDependencyCycleFindings {
    param(
        [Parameter(Mandatory)]
        [object[]]$Cycles,
        [Parameter(Mandatory)]
        [string]$OwnerDoc
    )

    $findings = [System.Collections.Generic.List[object]]::new()
    foreach ($cycle in @($Cycles)) {
        $features = @($cycle.features | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
        if ($features.Count -eq 0) {
            continue
        }

        $cyclePath = if ($features.Count -gt 1) {
            (@($features) + @($features[0])) -join ' -> '
        }
        else {
            "$($features[0]) -> $($features[0])"
        }
        $snippet = "Cycle: $cyclePath"
        $evidence = @($cycle.evidence | ForEach-Object { ConvertTo-RuleHarnessFeatureDependencyEvidence -Evidence $_ -Snippet $snippet })
        if ($evidence.Count -eq 0) {
            $evidence = @([pscustomobject]@{
                path    = $null
                line    = $null
                snippet = $snippet
            })
        }

        [void]$findings.Add((New-RuleHarnessFinding `
            -FindingType 'code_violation' `
            -Severity 'high' `
            -OwnerDoc $OwnerDoc `
            -Title 'Feature dependency cycle' `
            -Message ("Feature dependency cycle detected: {0}." -f $cyclePath) `
            -Evidence $evidence `
            -Confidence 'high' `
            -Source 'static' `
            -RemediationKind 'report_only' `
            -Rationale 'Feature dependency graph must remain acyclic even when new cross-feature edges are allowed.'))
    }

    @($findings)
}

function Get-RuleHarnessCompileGateStatus {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $statusPath = Get-RuleHarnessCompileStatusPath -RepoRoot $RepoRoot -Config $Config
    if (-not (Test-Path -LiteralPath $statusPath.fullPath)) {
        return [pscustomobject]@{
            compileVerified = $false
            cleanLevel      = 'static-clean only'
            compileGateStatus = 'missing'
            compileGateReasonCode = 'compile-status-missing'
            statusPath      = [string]$statusPath.relativePath
            failed          = $false
            stageResult     = New-RuleHarnessStageResult `
                -Stage 'compile_gate' `
                -Status 'skipped' `
                -Attempted $false `
                -Summary 'Compile-clean was not verified by the runnerless harness. Treat this run as static-clean only.' `
                -Details ([pscustomobject]@{
                    compileVerified = $false
                    cleanLevel = 'static-clean only'
                    statusPath = [string]$statusPath.relativePath
                })
            actionItems     = @(
                New-RuleHarnessActionItem `
                    -Kind 'verify-unity-compile' `
                    -Severity 'medium' `
                    -Summary 'Verify Unity compile before calling this clean' `
                    -Details 'Runnerless harness could not confirm compile-clean. Re-open Unity or refresh the compile status file before reporting clean.' `
                    -RelatedPaths @([string]$statusPath.relativePath)
            )
        }
    }

    try {
        $rawStatus = Get-Content -Path $statusPath.fullPath -Raw | ConvertFrom-Json
    }
    catch {
        return [pscustomobject]@{
            compileVerified = $false
            cleanLevel      = 'static-clean only'
            compileGateStatus = 'invalid'
            compileGateReasonCode = 'compile-status-invalid'
            statusPath      = [string]$statusPath.relativePath
            failed          = $true
            stageResult     = New-RuleHarnessStageResult `
                -Stage 'compile_gate' `
                -Status 'failed' `
                -Attempted $true `
                -Summary 'Compile status file exists but could not be parsed.' `
                -Details ([pscustomobject]@{
                    compileVerified = $false
                    cleanLevel = 'static-clean only'
                    statusPath = [string]$statusPath.relativePath
                })
            actionItems     = @(
                New-RuleHarnessActionItem `
                    -Kind 'repair-compile-status' `
                    -Severity 'high' `
                    -Summary 'Repair the compile status handoff file' `
                    -Details ("Rule harness could not parse compile status at {0}. Recreate the file after a fresh Unity compile." -f [string]$statusPath.relativePath) `
                    -RelatedPaths @([string]$statusPath.relativePath)
            )
        }
    }

    $status = [string]$rawStatus.status
    $reasonCode = if ($rawStatus.PSObject.Properties.Name -contains 'reasonCode') { [string]$rawStatus.reasonCode } else { $null }
    $checkedAtUtc = if ($rawStatus.PSObject.Properties.Name -contains 'checkedAtUtc') { [string]$rawStatus.checkedAtUtc } else { $null }
    $source = if ($rawStatus.PSObject.Properties.Name -contains 'source') { [string]$rawStatus.source } else { 'unknown' }
    $runtimeSmokeClean = $rawStatus.PSObject.Properties.Name -contains 'runtimeSmokeClean' -and [bool]$rawStatus.runtimeSmokeClean
    $summary = if ($rawStatus.PSObject.Properties.Name -contains 'summary' -and -not [string]::IsNullOrWhiteSpace([string]$rawStatus.summary)) {
        [string]$rawStatus.summary
    }
    else {
        "Compile status source=$source checkedAt=$checkedAtUtc"
    }

    switch ($status) {
        'passed' {
            $cleanLevel = if ($runtimeSmokeClean) { 'compile-clean + runtime-smoke-clean' } else { 'compile-clean' }
            return [pscustomobject]@{
                compileVerified = $true
                cleanLevel      = $cleanLevel
                compileGateStatus = 'passed'
                compileGateReasonCode = $reasonCode
                statusPath      = [string]$statusPath.relativePath
                failed          = $false
                stageResult     = New-RuleHarnessStageResult `
                    -Stage 'compile_gate' `
                    -Status 'passed' `
                    -Attempted $true `
                    -Summary ("Compile verification passed. Level={0}." -f $cleanLevel) `
                    -Details ([pscustomobject]@{
                        compileVerified = $true
                        cleanLevel = $cleanLevel
                        checkedAtUtc = $checkedAtUtc
                        source = $source
                        statusPath = [string]$statusPath.relativePath
                    })
                actionItems     = @()
            }
        }
        'failed' {
            return [pscustomobject]@{
                compileVerified = $false
                cleanLevel      = 'static-clean only'
                compileGateStatus = 'failed'
                compileGateReasonCode = $reasonCode
                statusPath      = [string]$statusPath.relativePath
                failed          = $true
                stageResult     = New-RuleHarnessStageResult `
                    -Stage 'compile_gate' `
                    -Status 'failed' `
                    -Attempted $true `
                    -Summary 'Compile verification failed.' `
                    -Details ([pscustomobject]@{
                        compileVerified = $false
                        cleanLevel = 'static-clean only'
                        checkedAtUtc = $checkedAtUtc
                        source = $source
                        statusPath = [string]$statusPath.relativePath
                    })
                actionItems     = @(
                    New-RuleHarnessActionItem `
                        -Kind 'fix-compile-errors' `
                        -Severity 'high' `
                        -Summary 'Fix Unity compile errors before rerunning rule harness' `
                        -Details $summary `
                        -RelatedPaths @([string]$statusPath.relativePath)
                )
            }
        }
        'unavailable' {
            return [pscustomobject]@{
                compileVerified = $false
                cleanLevel      = 'static-clean only'
                compileGateStatus = 'unavailable'
                compileGateReasonCode = $reasonCode
                statusPath      = [string]$statusPath.relativePath
                failed          = $false
                stageResult     = New-RuleHarnessStageResult `
                    -Stage 'compile_gate' `
                    -Status 'skipped' `
                    -Attempted $true `
                    -Summary 'Compile verification was unavailable because Unity MCP could not be reached or did not report healthy status.' `
                    -Details ([pscustomobject]@{
                        compileVerified = $false
                        cleanLevel = 'static-clean only'
                        checkedAtUtc = $checkedAtUtc
                        source = $source
                        reasonCode = $reasonCode
                        statusPath = [string]$statusPath.relativePath
                    })
                actionItems     = @(
                    New-RuleHarnessActionItem `
                        -Kind 'restore-unity-mcp' `
                        -Severity 'medium' `
                        -Summary 'Restore Unity MCP connectivity before trusting compile status' `
                        -Details $summary `
                        -RelatedPaths @([string]$statusPath.relativePath)
                )
            }
        }
        'blocked' {
            return [pscustomobject]@{
                compileVerified = $false
                cleanLevel      = 'static-clean only'
                compileGateStatus = 'blocked'
                compileGateReasonCode = $reasonCode
                statusPath      = [string]$statusPath.relativePath
                failed          = $false
                stageResult     = New-RuleHarnessStageResult `
                    -Stage 'compile_gate' `
                    -Status 'skipped' `
                    -Attempted $true `
                    -Summary 'Compile verification was blocked by Unity state, so this run remains static-clean only.' `
                    -Details ([pscustomobject]@{
                        compileVerified = $false
                        cleanLevel = 'static-clean only'
                        checkedAtUtc = $checkedAtUtc
                        source = $source
                        reasonCode = $reasonCode
                        statusPath = [string]$statusPath.relativePath
                    })
                actionItems     = @(
                    New-RuleHarnessActionItem `
                        -Kind 'retry-compile-verification' `
                        -Severity 'medium' `
                        -Summary 'Retry compile verification when Unity is idle' `
                        -Details $summary `
                        -RelatedPaths @([string]$statusPath.relativePath)
                )
            }
        }
        default {
            return [pscustomobject]@{
                compileVerified = $false
                cleanLevel      = 'static-clean only'
                compileGateStatus = if ([string]::IsNullOrWhiteSpace($status)) { 'unknown' } else { $status }
                compileGateReasonCode = $reasonCode
                statusPath      = [string]$statusPath.relativePath
                failed          = $false
                stageResult     = New-RuleHarnessStageResult `
                    -Stage 'compile_gate' `
                    -Status 'skipped' `
                    -Attempted $true `
                    -Summary 'Compile status file did not declare a passed/failed result. Treat this run as static-clean only.' `
                    -Details ([pscustomobject]@{
                        compileVerified = $false
                        cleanLevel = 'static-clean only'
                        checkedAtUtc = $checkedAtUtc
                        source = $source
                        statusPath = [string]$statusPath.relativePath
                    })
                actionItems     = @(
                    New-RuleHarnessActionItem `
                        -Kind 'verify-unity-compile' `
                        -Severity 'medium' `
                        -Summary 'Verify Unity compile before calling this clean' `
                        -Details $summary `
                        -RelatedPaths @([string]$statusPath.relativePath)
                )
            }
        }
    }
}

function Get-RuleHarnessFeatureDependencyGateStatus {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $reportPath = Get-RuleHarnessFeatureDependencyReportPath -RepoRoot $RepoRoot -Config $Config
    $sourceRelativePath = 'Assets/Editor/LayerDependencyValidator.cs'
    $sourceFullPath = Join-Path $RepoRoot $sourceRelativePath
    $architectureOwnerDoc = Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot

    if (-not (Test-Path -LiteralPath $sourceFullPath) -and -not (Test-Path -LiteralPath $reportPath.fullPath)) {
        return [pscustomobject]@{
            featureDependencyGateStatus = 'unsupported'
            featureDependencyCycleCount = 0
            reportPath                  = [string]$reportPath.relativePath
            failed                      = $false
            findings                    = @()
            stageResult                 = New-RuleHarnessStageResult `
                -Stage 'feature_dependency_gate' `
                -Status 'skipped' `
                -Attempted $false `
                -Summary 'Feature dependency graph validation is not configured for this repository snapshot.' `
                -Details ([pscustomobject]@{
                    sourcePath = $sourceRelativePath
                    reportPath = [string]$reportPath.relativePath
                })
            actionItems                 = @()
        }
    }

    if (-not (Test-Path -LiteralPath $reportPath.fullPath)) {
        return [pscustomobject]@{
            featureDependencyGateStatus = 'missing'
            featureDependencyCycleCount = 0
            reportPath                  = [string]$reportPath.relativePath
            failed                      = $true
            findings                    = @()
            stageResult                 = New-RuleHarnessStageResult `
                -Stage 'feature_dependency_gate' `
                -Status 'failed' `
                -Attempted $true `
                -Summary 'Feature dependency report is missing.' `
                -Details ([pscustomobject]@{
                    sourcePath = $sourceRelativePath
                    reportPath = [string]$reportPath.relativePath
                })
            actionItems                 = @(
                New-RuleHarnessActionItem `
                    -Kind 'refresh-feature-dependency-report' `
                    -Severity 'high' `
                    -Summary 'Refresh the feature dependency graph artifact' `
                    -Details ("Rule harness expected feature dependency report at {0}. Re-run write-feature-dependency-report.ps1 or inspect LayerDependencyValidator." -f [string]$reportPath.relativePath) `
                    -RelatedPaths @([string]$sourceRelativePath, [string]$reportPath.relativePath)
            )
        }
    }

    try {
        $rawReport = Get-Content -Path $reportPath.fullPath -Raw | ConvertFrom-Json
    }
    catch {
        return [pscustomobject]@{
            featureDependencyGateStatus = 'invalid'
            featureDependencyCycleCount = 0
            reportPath                  = [string]$reportPath.relativePath
            failed                      = $true
            findings                    = @()
            stageResult                 = New-RuleHarnessStageResult `
                -Stage 'feature_dependency_gate' `
                -Status 'failed' `
                -Attempted $true `
                -Summary 'Feature dependency report exists but could not be parsed.' `
                -Details ([pscustomobject]@{
                    sourcePath = $sourceRelativePath
                    reportPath = [string]$reportPath.relativePath
                })
            actionItems                 = @(
                New-RuleHarnessActionItem `
                    -Kind 'repair-feature-dependency-report' `
                    -Severity 'high' `
                    -Summary 'Repair the feature dependency graph artifact' `
                    -Details ("Rule harness could not parse feature dependency report at {0}. Recreate the file after checking LayerDependencyValidator." -f [string]$reportPath.relativePath) `
                    -RelatedPaths @([string]$sourceRelativePath, [string]$reportPath.relativePath)
            )
        }
    }

    $hasCycles = $rawReport.PSObject.Properties.Name -contains 'hasCycles' -and [bool]$rawReport.hasCycles
    $featureCount = if ($rawReport.PSObject.Properties.Name -contains 'featureCount') { [int]$rawReport.featureCount } else { 0 }
    $edgeCount = if ($rawReport.PSObject.Properties.Name -contains 'edgeCount') { [int]$rawReport.edgeCount } else { 0 }
    $generatedAtUtc = if ($rawReport.PSObject.Properties.Name -contains 'generatedAtUtc') { [string]$rawReport.generatedAtUtc } else { $null }
    $cycles = @($rawReport.cycles)

    if ($hasCycles) {
        $cycleFindings = @(Get-RuleHarnessFeatureDependencyCycleFindings -Cycles $cycles -OwnerDoc $architectureOwnerDoc)
        $relatedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        [void]$relatedPaths.Add([string]$sourceRelativePath)
        [void]$relatedPaths.Add([string]$reportPath.relativePath)
        foreach ($cycleFinding in @($cycleFindings)) {
            foreach ($evidence in @($cycleFinding.evidence)) {
                if ($null -ne $evidence -and -not [string]::IsNullOrWhiteSpace([string]$evidence.path)) {
                    [void]$relatedPaths.Add([string]$evidence.path)
                }
            }
        }

        return [pscustomobject]@{
            featureDependencyGateStatus = 'failed'
            featureDependencyCycleCount = @($cycleFindings).Count
            reportPath                  = [string]$reportPath.relativePath
            failed                      = $true
            findings                    = @($cycleFindings)
            stageResult                 = New-RuleHarnessStageResult `
                -Stage 'feature_dependency_gate' `
                -Status 'failed' `
                -Attempted $true `
                -Summary ("Feature dependency graph contains {0} cycle(s)." -f @($cycleFindings).Count) `
                -Details ([pscustomobject]@{
                    generatedAtUtc = $generatedAtUtc
                    reportPath = [string]$reportPath.relativePath
                    featureCount = $featureCount
                    edgeCount = $edgeCount
                    cycleCount = @($cycleFindings).Count
                })
            actionItems                 = @(
                New-RuleHarnessActionItem `
                    -Kind 'break-feature-dependency-cycle' `
                    -Severity 'high' `
                    -Summary 'Break feature dependency cycles before rerunning rule harness' `
                    -Details ("LayerDependencyValidator reported {0} cycle(s). Keep cross-feature dependencies acyclic." -f @($cycleFindings).Count) `
                    -RelatedPaths @($relatedPaths)
            )
        }
    }

    return [pscustomobject]@{
        featureDependencyGateStatus = 'passed'
        featureDependencyCycleCount = 0
        reportPath                  = [string]$reportPath.relativePath
        failed                      = $false
        findings                    = @()
        stageResult                 = New-RuleHarnessStageResult `
            -Stage 'feature_dependency_gate' `
            -Status 'passed' `
            -Attempted $true `
            -Summary ("Feature dependency graph is acyclic. Features={0} Edges={1}." -f $featureCount, $edgeCount) `
            -Details ([pscustomobject]@{
                generatedAtUtc = $generatedAtUtc
                reportPath = [string]$reportPath.relativePath
                featureCount = $featureCount
                edgeCount = $edgeCount
                cycleCount = 0
            })
        actionItems                 = @()
    }
}

function Get-RuleHarnessDocumentContent {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$RelativePath
    )

    if ([string]::IsNullOrWhiteSpace($RelativePath)) {
        return $null
    }

    $fullPath = Join-Path $RepoRoot $RelativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        return $null
    }

    Get-Content -Path $fullPath -Raw
}

function Get-RuleHarnessCycleRepairPolicySnapshot {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $claudePath = 'AGENTS.md'
    $architectureDoc = Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot
    $antiPatternsDoc = Get-RuleHarnessAntiPatternsOwnerDoc -RepoRoot $RepoRoot
    $eventRulesDoc = Get-RuleHarnessEventRulesOwnerDoc -RepoRoot $RepoRoot
    $validationDoc = Get-RuleHarnessValidationGatesOwnerDoc -RepoRoot $RepoRoot

    $claudeText = Get-RuleHarnessDocumentContent -RepoRoot $RepoRoot -RelativePath $claudePath
    $architectureText = Get-RuleHarnessDocumentContent -RepoRoot $RepoRoot -RelativePath $architectureDoc
    $antiPatternsText = Get-RuleHarnessDocumentContent -RepoRoot $RepoRoot -RelativePath $antiPatternsDoc
    $eventRulesText = Get-RuleHarnessDocumentContent -RepoRoot $RepoRoot -RelativePath $eventRulesDoc
    $validationText = Get-RuleHarnessDocumentContent -RepoRoot $RepoRoot -RelativePath $validationDoc

    $consumerOwnedPortsPreferred = -not [string]::IsNullOrWhiteSpace($architectureText) -and
        $architectureText -match 'consumer-owned' -and
        $architectureText -match 'Application/Ports'
    $cleanRequiresCompile = -not [string]::IsNullOrWhiteSpace($validationText) -and
        $validationText -match 'static-clean \+ compile-clean'

    $sourceOrder = [System.Collections.Generic.List[string]]::new()
    foreach ($source in @($claudePath, $architectureDoc, $antiPatternsDoc, $eventRulesDoc, $validationDoc)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$source) -and -not $sourceOrder.Contains([string]$source)) {
            [void]$sourceOrder.Add([string]$source)
        }
    }

    [pscustomobject]@{
        sourceOrder = @($sourceOrder)
        sources = [pscustomobject]@{
            claude = $claudePath
            architecture = $architectureDoc
            antiPatterns = $antiPatternsDoc
            eventRules = $eventRulesDoc
            validationGates = $validationDoc
        }
        constraints = [pscustomobject]@{
            dagPortExceptionDocumented = -not [string]::IsNullOrWhiteSpace($architectureText) -and $architectureText -match 'Application/Ports'
            analyticsObserverExceptionDocumented = -not [string]::IsNullOrWhiteSpace($architectureText) -and $architectureText -match 'analytics/reporting'
            consumerOwnedPortsPreferred = $consumerOwnedPortsPreferred
            sharedOwnershipDocumented = -not [string]::IsNullOrWhiteSpace($architectureText) -and $architectureText -match 'Shared'
            eventCycleRequiresDirectCallReplacement = -not [string]::IsNullOrWhiteSpace($eventRulesText) -and $eventRulesText -match '직접 호출' -and $eventRulesText -match '순환'
            antiPhantomSharedContract = -not [string]::IsNullOrWhiteSpace($antiPatternsText) -and $antiPatternsText -match 'Phantom shared 계약'
            antiConcreteInterfaceDrift = -not [string]::IsNullOrWhiteSpace($antiPatternsText) -and $antiPatternsText -match 'Concrete/interface drift'
            antiEventContractDrift = -not [string]::IsNullOrWhiteSpace($antiPatternsText) -and $antiPatternsText -match '이벤트 계약 drift'
            cleanRequiresCompile = $cleanRequiresCompile
        }
        preferredDirections = @(
            if ($consumerOwnedPortsPreferred) { 'port_inversion' }
        )
        summary = if ($consumerOwnedPortsPreferred -and $cleanRequiresCompile) {
            'Docs prefer consumer-owned Application/Ports seams and require compile-clean before declaring clean.'
        }
        elseif ($consumerOwnedPortsPreferred) {
            'Docs prefer consumer-owned Application/Ports seams, but compile-clean guidance was not detected.'
        }
        else {
            'Repair policy snapshot did not confirm consumer-owned Application/Ports guidance.'
        }
    }
}

function Get-RuleHarnessFeatureDependencyRefreshScriptPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $relativePath = 'tools/rule-harness/write-feature-dependency-report.ps1'
    $fullPath = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        return $null
    }

    [pscustomobject]@{
        relativePath = $relativePath
        fullPath = $fullPath
    }
}

function Test-RuleHarnessFeatureDependencyAnalyzerAvailability {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $coreSourcePath = Join-Path $RepoRoot 'Assets/Editor/LayerDependencyValidator.cs'
    if (-not (Test-Path -LiteralPath $coreSourcePath)) {
        return [pscustomobject]@{
            available = $false
            reason = 'feature-dependency-analyzer-core-missing'
            path = $coreSourcePath
        }
    }

    $content = Get-Content -Path $coreSourcePath -Raw
    if ($content -notmatch 'public\s+static\s+class\s+LayerDependencyAnalyzer') {
        return [pscustomobject]@{
            available = $false
            reason = 'feature-dependency-analyzer-type-missing'
            path = $coreSourcePath
        }
    }

    [pscustomobject]@{
        available = $true
        reason = $null
        path = $coreSourcePath
    }
}

function Invoke-RuleHarnessFeatureDependencyReportRefresh {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $scriptPath = Get-RuleHarnessFeatureDependencyRefreshScriptPath -RepoRoot $RepoRoot
    if ($null -eq $scriptPath) {
        return [pscustomobject]@{
            attempted = $false
            succeeded = $false
            scriptPath = $null
            output = $null
            error = 'feature-dependency-refresh-script-missing'
        }
    }

    $analyzerAvailability = Test-RuleHarnessFeatureDependencyAnalyzerAvailability -RepoRoot $RepoRoot
    if (-not [bool]$analyzerAvailability.available) {
        return [pscustomobject]@{
            attempted = $false
            succeeded = $false
            scriptPath = [string]$scriptPath.relativePath
            output = @()
            error = [string]$analyzerAvailability.reason
        }
    }

    try {
        $output = @(& powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath.fullPath -RepoRoot $RepoRoot)
        return [pscustomobject]@{
            attempted = $true
            succeeded = ($LASTEXITCODE -eq 0)
            scriptPath = [string]$scriptPath.relativePath
            output = @($output)
            error = if ($LASTEXITCODE -eq 0) { $null } else { "exit-code-$LASTEXITCODE" }
        }
    }
    catch {
        return [pscustomobject]@{
            attempted = $true
            succeeded = $false
            scriptPath = [string]$scriptPath.relativePath
            output = @()
            error = $_.Exception.Message
        }
    }
}

function Get-RuleHarnessCompileRefreshScriptPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $relativePath = 'tools/rule-harness/write-compile-status.ps1'
    $fullPath = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        return $null
    }

    [pscustomobject]@{
        relativePath = $relativePath
        fullPath = $fullPath
    }
}

function Invoke-RuleHarnessCompileStatusRefresh {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $scriptPath = Get-RuleHarnessCompileRefreshScriptPath -RepoRoot $RepoRoot
    if ($null -eq $scriptPath) {
        return [pscustomobject]@{
            attempted = $false
            succeeded = $false
            scriptPath = $null
            output = $null
            error = 'compile-refresh-script-missing'
        }
    }

    try {
        $output = @(& powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath.fullPath -RepoRoot $RepoRoot)
        return [pscustomobject]@{
            attempted = $true
            succeeded = ($LASTEXITCODE -eq 0)
            scriptPath = [string]$scriptPath.relativePath
            output = @($output)
            error = if ($LASTEXITCODE -eq 0) { $null } else { "exit-code-$LASTEXITCODE" }
        }
    }
    catch {
        return [pscustomobject]@{
            attempted = $true
            succeeded = $false
            scriptPath = [string]$scriptPath.relativePath
            output = @()
            error = $_.Exception.Message
        }
    }
}

function Get-RuleHarnessFeatureDependencyReportObject {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $pathInfo = Get-RuleHarnessFeatureDependencyReportPath -RepoRoot $RepoRoot -Config $Config
    if (-not (Test-Path -LiteralPath $pathInfo.fullPath)) {
        return $null
    }

    Get-Content -Path $pathInfo.fullPath -Raw | ConvertFrom-Json
}

function Get-RuleHarnessFeatureDependencyCyclePath {
    param(
        [Parameter(Mandatory)]
        [object]$Cycle
    )

    $features = @($Cycle.features | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($features.Count -eq 0) {
        return '<unknown-cycle>'
    }

    if ($features.Count -eq 1) {
        return "$($features[0]) -> $($features[0])"
    }

    (@($features) + @($features[0])) -join ' -> '
}

function Get-RuleHarnessFeatureDependencyCycleSignature {
    param(
        [Parameter(Mandatory)]
        [object]$Cycle
    )

    (@($Cycle.features | ForEach-Object { [string]$_ } | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) -join '->'
}

function Get-RuleHarnessFeatureTypeDefinitions {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$FeatureName
    )

    $featureRoot = Join-Path $RepoRoot "Assets/Scripts/Features/$FeatureName"
    if (-not (Test-Path -LiteralPath $featureRoot)) {
        return @()
    }

    $definitions = [System.Collections.Generic.List[object]]::new()
    foreach ($file in Get-ChildItem -LiteralPath $featureRoot -Recurse -File -Filter '*.cs') {
        $content = Get-Content -Path $file.FullName -Raw
        $namespaceMatch = [regex]::Match($content, '(?m)^\s*namespace\s+(?<namespace>[A-Za-z0-9_.]+)')
        $namespaceName = if ($namespaceMatch.Success) { [string]$namespaceMatch.Groups['namespace'].Value } else { $null }
        foreach ($match in [regex]::Matches($content, '(?m)^\s*public\s+(?:sealed\s+|abstract\s+|partial\s+)?class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')) {
            [void]$definitions.Add([pscustomobject]@{
                name = [string]$match.Groups['name'].Value
                namespace = $namespaceName
                path = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $file.FullName
                content = $content
            })
        }
    }

    @($definitions)
}

function Test-RuleHarnessPortMemberTypeSupported {
    param(
        [AllowEmptyString()]
        [string]$TypeName
    )

    if ([string]::IsNullOrWhiteSpace($TypeName)) {
        return $false
    }

    $trimmed = $TypeName.Trim()
    if ($trimmed -match 'UnityEngine|MonoBehaviour|GameObject|Transform|Sprite|AudioClip|Color') {
        return $false
    }

    if ($trimmed -match '^Features\.') {
        return $false
    }

    if ($trimmed -match '\b(?:List|Dictionary|HashSet|IEnumerable|Func|Action)<') {
        return $false
    }

    return $trimmed -match '^(?:void|bool|int|float|double|decimal|long|short|byte|string|object|Guid|DateTime|TimeSpan|Vector2Int|Vector3Int|[A-Z][A-Za-z0-9_]*(?:\?|\[\])?)$'
}

function Get-RuleHarnessPublicMembersForType {
    param(
        [Parameter(Mandatory)]
        [string]$TypeContent,
        [Parameter(Mandatory)]
        [string]$TypeName,
        [Parameter(Mandatory)]
        [string[]]$RequestedMembers
    )

    $members = [System.Collections.Generic.List[object]]::new()
    foreach ($memberName in @($RequestedMembers | Sort-Object -Unique)) {
        $methodMatch = [regex]::Match(
            $TypeContent,
            ("(?m)^\s*public\s+(?<return>[A-Za-z0-9_<>,.\?\[\]]+)\s+(?<name>{0})\s*\((?<params>[^)]*)\)" -f [regex]::Escape($memberName))
        )
        if ($methodMatch.Success -and [string]$methodMatch.Groups['name'].Value -ne $TypeName) {
            $returnType = [string]$methodMatch.Groups['return'].Value
            if (-not (Test-RuleHarnessPortMemberTypeSupported -TypeName $returnType)) {
                return @()
            }

            $parameterList = @()
            foreach ($rawParam in @(([string]$methodMatch.Groups['params'].Value -split ',') | ForEach-Object { $_.Trim() } | Where-Object { $_ })) {
                if ($rawParam -match '^(?<type>[A-Za-z0-9_<>,.\?\[\]]+)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)$') {
                    $parameterType = [string]$Matches['type']
                    if (-not (Test-RuleHarnessPortMemberTypeSupported -TypeName $parameterType)) {
                        return @()
                    }

                    $parameterList += [pscustomobject]@{
                        type = $parameterType
                        name = [string]$Matches['name']
                    }
                }
                else {
                    return @()
                }
            }

            [void]$members.Add([pscustomobject]@{
                kind = 'method'
                name = $memberName
                returnType = $returnType
                parameters = @($parameterList)
            })
            continue
        }

        $propertyMatch = [regex]::Match(
            $TypeContent,
            ("(?m)^\s*public\s+(?<type>[A-Za-z0-9_<>,.\?\[\]]+)\s+(?<name>{0})\s*\{{\s*get;" -f [regex]::Escape($memberName))
        )
        if ($propertyMatch.Success) {
            $propertyType = [string]$propertyMatch.Groups['type'].Value
            if (-not (Test-RuleHarnessPortMemberTypeSupported -TypeName $propertyType)) {
                return @()
            }

            [void]$members.Add([pscustomobject]@{
                kind = 'property'
                name = $memberName
                returnType = $propertyType
                parameters = @()
            })
            continue
        }

        return @()
    }

    @($members)
}

function Get-RuleHarnessPortInversionPlan {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Cycle,
        [Parameter(Mandatory)]
        [object]$Edge,
        [Parameter(Mandatory)]
        [object]$PolicySnapshot
    )

    if (-not [bool]$PolicySnapshot.constraints.consumerOwnedPortsPreferred) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @('consumer-owned-application-ports-not-confirmed')
            blockedByCodeAmbiguity = @()
            blockedBySafety = @()
            reason = 'Owner docs did not confirm consumer-owned Application/Ports as the preferred seam.'
        }
    }

    $evidence = @($Edge.evidence | Where-Object { $null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_.path) })
    if ($evidence.Count -eq 0) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('missing-edge-evidence')
            blockedBySafety = @()
            reason = 'Cycle edge had no usable evidence path.'
        }
    }

    $consumerFeature = [string]$Edge.from
    $providerFeature = [string]$Edge.to
    $consumerPath = [string]$evidence[0].path
    $consumerFullPath = Join-Path $RepoRoot $consumerPath
    if (-not (Test-Path -LiteralPath $consumerFullPath)) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('consumer-file-missing')
            blockedBySafety = @()
            reason = "Consumer evidence path is missing: $consumerPath"
        }
    }

    $consumerContent = Get-Content -Path $consumerFullPath -Raw
    if ($consumerPath -match '/(?:Application/Ports/|Infrastructure/|Domain/)?(?:[^/]+Setup|[^/]+Bootstrap)\.cs$') {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('composition-root-edge')
            blockedBySafety = @()
            reason = 'V1 port inversion does not rewrite composition-root-only evidence.'
        }
    }

    $providerDefinitions = @(Get-RuleHarnessFeatureTypeDefinitions -RepoRoot $RepoRoot -FeatureName $providerFeature)
    $providerTypeMatches = @(
        $providerDefinitions |
            Where-Object {
                $consumerContent -match ("\b{0}\b" -f [regex]::Escape([string]$_.name))
            } |
            Select-Object -Property * -Unique
    )
    if ($providerTypeMatches.Count -ne 1) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('provider-type-not-unique')
            blockedBySafety = @()
            reason = "Expected exactly one provider concrete type in $consumerPath but found $($providerTypeMatches.Count)."
        }
    }

    $providerType = $providerTypeMatches[0]
    $providerTypeName = [string]$providerType.name
    if ($consumerContent -match ("\bnew\s+{0}\s*\(" -f [regex]::Escape($providerTypeName)) -or
        $consumerContent -match ("\b{0}\s*\." -f [regex]::Escape($providerTypeName))) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('consumer-uses-construction-or-static-api')
            blockedBySafety = @()
            reason = "Consumer file $consumerPath constructs or statically references $providerTypeName."
        }
    }

    $fieldMatches = @([regex]::Matches($consumerContent, ("(?m)^\s*(?:private|protected|public|internal)\s+(?:readonly\s+)?{0}\s+(?<name>_[A-Za-z][A-Za-z0-9_]*)\s*;" -f [regex]::Escape($providerTypeName))) | ForEach-Object {
        [string]$_.Groups['name'].Value
    } | Select-Object -Unique)
    if ($fieldMatches.Count -ne 1) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('consumer-dependency-field-not-unique')
            blockedBySafety = @()
            reason = "Expected exactly one injected dependency field typed as $providerTypeName in $consumerPath."
        }
    }

    $dependencyField = [string]$fieldMatches[0]
    $usedMembers = @([regex]::Matches($consumerContent, ("\b{0}\.(?<member>[A-Za-z_][A-Za-z0-9_]*)\b" -f [regex]::Escape($dependencyField))) | ForEach-Object {
        [string]$_.Groups['member'].Value
    } | Sort-Object -Unique)
    if ($usedMembers.Count -eq 0) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('no-member-usage-detected')
            blockedBySafety = @()
            reason = "No provider member usage was detected through $dependencyField in $consumerPath."
        }
    }

    $publicMembers = @(Get-RuleHarnessPublicMembersForType -TypeContent ([string]$providerType.content) -TypeName $providerTypeName -RequestedMembers $usedMembers)
    if ($publicMembers.Count -ne $usedMembers.Count) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('member-signature-ambiguous')
            blockedBySafety = @('member-signature-unsupported')
            reason = "Provider member signatures for $providerTypeName could not be promoted to an Application/Ports interface safely."
        }
    }

    $consumerClassMatch = [regex]::Match($consumerContent, '(?m)^\s*public\s+(?:sealed\s+|partial\s+)?class\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)')
    if (-not $consumerClassMatch.Success) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('consumer-class-not-found')
            blockedBySafety = @()
            reason = "Consumer class declaration could not be located in $consumerPath."
        }
    }

    $consumerClassName = [string]$consumerClassMatch.Groups['name'].Value
    $setupCandidates = @(
        Get-ChildItem -LiteralPath (Join-Path $RepoRoot "Assets/Scripts/Features/$consumerFeature") -Recurse -File -Filter '*.cs' |
            Where-Object { $_.Name -match '(Setup|Bootstrap)\.cs$' }
    )
    $setupMatches = [System.Collections.Generic.List[object]]::new()
    foreach ($setupFile in @($setupCandidates)) {
        $setupContent = Get-Content -Path $setupFile.FullName -Raw
        if ($setupContent -notmatch ("\bnew\s+{0}\s*\(" -f [regex]::Escape($consumerClassName))) {
            continue
        }

        if ($setupContent -notmatch ("\bnew\s+{0}\s*\(" -f [regex]::Escape($providerTypeName))) {
            continue
        }

        [void]$setupMatches.Add([pscustomobject]@{
            path = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $setupFile.FullName
            content = $setupContent
        })
    }

    if ($setupMatches.Count -ne 1) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('wiring-site-not-unique')
            blockedBySafety = @()
            reason = "Expected exactly one setup/bootstrap wiring site for $consumerClassName and $providerTypeName."
        }
    }

    $consumerNamespaceMatch = [regex]::Match($consumerContent, '(?m)^\s*namespace\s+(?<namespace>[A-Za-z0-9_.]+)')
    $consumerNamespace = if ($consumerNamespaceMatch.Success) { [string]$consumerNamespaceMatch.Groups['namespace'].Value } else { "Features.$consumerFeature.Application" }
    $portsNamespace = "Features.$consumerFeature.Application.Ports"
    $interfaceName = "I{0}Port" -f $providerTypeName
    $adapterName = "{0}PortAdapter" -f $providerTypeName
    $interfacePath = "Assets/Scripts/Features/$consumerFeature/Application/Ports/$interfaceName.cs"
    $adapterPath = "Assets/Scripts/Features/$providerFeature/Infrastructure/$adapterName.cs"
    $setupPath = [string]$setupMatches[0].path
    $setupContent = [string]$setupMatches[0].content

    $interfaceMembersText = @(
        foreach ($member in @($publicMembers)) {
            if ([string]$member.kind -eq 'property') {
                "        {0} {1} {{ get; }}" -f [string]$member.returnType, [string]$member.name
            }
            else {
                $parameterText = @($member.parameters | ForEach-Object { "{0} {1}" -f [string]$_.type, [string]$_.name }) -join ', '
                "        {0} {1}({2});" -f [string]$member.returnType, [string]$member.name, $parameterText
            }
        }
    ) -join "`r`n"

    $interfaceContent = @"
namespace $portsNamespace
{
    public interface $interfaceName
    {
$interfaceMembersText
    }
}
"@

    $adapterMembersText = @(
        foreach ($member in @($publicMembers)) {
            if ([string]$member.kind -eq 'property') {
                "        public {0} {1} => _inner.{1};" -f [string]$member.returnType, [string]$member.name
            }
            else {
                $parameterText = @($member.parameters | ForEach-Object { "{0} {1}" -f [string]$_.type, [string]$_.name }) -join ', '
                $argumentText = @($member.parameters | ForEach-Object { [string]$_.name }) -join ', '
                "        public {0} {1}({2}) => _inner.{1}({3});" -f [string]$member.returnType, [string]$member.name, $parameterText, $argumentText
            }
        }
    ) -join "`r`n"

    $providerNamespace = if ([string]::IsNullOrWhiteSpace([string]$providerType.namespace)) { "Features.$providerFeature.Infrastructure" } else { [string]$providerType.namespace }
    $adapterContent = @"
using $portsNamespace;
using $providerNamespace;

namespace Features.$providerFeature.Infrastructure
{
    public sealed class $adapterName : $interfaceName
    {
        private readonly $providerTypeName _inner;

        public $adapterName($providerTypeName inner)
        {
            _inner = inner;
        }

$adapterMembersText
    }
}
"@

    $updatedConsumerContent = Add-RuleHarnessUsingDirective -Content $consumerContent -UsingDirective "using $portsNamespace;"
    $updatedConsumerContent = [regex]::Replace($updatedConsumerContent, ("\b{0}\b" -f [regex]::Escape($providerTypeName)), $interfaceName)
    if (-not [string]::IsNullOrWhiteSpace([string]$providerType.namespace)) {
        $updatedConsumerContent = [regex]::Replace(
            $updatedConsumerContent,
            ("(?m)^[ \t]*using\s+{0}\s*;\r?\n" -f [regex]::Escape([string]$providerType.namespace)),
            ''
        )
    }
    if ($updatedConsumerContent -eq $consumerContent) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('consumer-rewrite-noop')
            blockedBySafety = @()
            reason = "Consumer rewrite for $consumerPath did not change the file."
        }
    }

    $updatedSetupContent = Add-RuleHarnessUsingDirective -Content $setupContent -UsingDirective "using Features.$providerFeature.Infrastructure;"
    $updatedSetupContent = [regex]::Replace(
        $updatedSetupContent,
        ("new\s+{0}\s*\((?<args>[^()]*)\)" -f [regex]::Escape($providerTypeName)),
        ('new {0}(new {1}(${{args}}))' -f $adapterName, $providerTypeName),
        1
    )
    if ($updatedSetupContent -eq $setupContent) {
        return [pscustomobject]@{
            supported = $false
            blockedByDocRules = @()
            blockedByCodeAmbiguity = @('wiring-rewrite-noop')
            blockedBySafety = @()
            reason = "Setup/bootstrap wiring rewrite for $setupPath did not change the file."
        }
    }

    [pscustomobject]@{
        supported = $true
        recipe = 'port_inversion'
        cycleSignature = Get-RuleHarnessFeatureDependencyCycleSignature -Cycle $Cycle
        cyclePath = Get-RuleHarnessFeatureDependencyCyclePath -Cycle $Cycle
        consumerFeature = $consumerFeature
        providerFeature = $providerFeature
        interfaceName = $interfaceName
        adapterName = $adapterName
        targetEdge = [pscustomobject]@{
            from = $consumerFeature
            to = $providerFeature
            evidencePath = $consumerPath
        }
        targetFiles = @($consumerPath, $interfacePath, $adapterPath, $setupPath)
        operations = @(
            [pscustomobject]@{ type = 'write_file'; targetPath = $consumerPath; content = $updatedConsumerContent },
            [pscustomobject]@{ type = 'write_file'; targetPath = $interfacePath; content = $interfaceContent },
            [pscustomobject]@{ type = 'write_file'; targetPath = $adapterPath; content = $adapterContent },
            [pscustomobject]@{ type = 'write_file'; targetPath = $setupPath; content = $updatedSetupContent }
        )
        memberNames = @($usedMembers)
        blockedByDocRules = @()
        blockedByCodeAmbiguity = @()
        blockedBySafety = @()
        reason = "Invert $consumerFeature -> $providerFeature through consumer-owned Application/Ports."
    }
}

function Get-RuleHarnessManagedCycleRepairDocEdits {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$TargetDoc,
        [Parameter(Mandatory)]
        [string]$Signature,
        [Parameter(Mandatory)]
        [string]$Recipe,
        [Parameter(Mandatory)]
        [string]$Rationale
    )

    $fullPath = Join-Path $RepoRoot $TargetDoc
    if (-not (Test-Path -LiteralPath $fullPath)) {
        return @()
    }

    $currentText = Get-Content -Path $fullPath -Raw
    $noteLine = "* Rule harness learned cycle pattern `$Signature`: prefer $Recipe. $Rationale"
    if ($currentText.Contains($noteLine)) {
        return @()
    }

    $heading = '## Rule Harness Learned Notes'
    $updatedText = if ($currentText.Contains($heading)) {
        $currentText.Replace($heading, "$heading`r`n$noteLine")
    }
    else {
        $currentText.TrimEnd() + "`r`n`r`n$heading`r`n$noteLine`r`n"
    }

    @([pscustomobject]@{
        targetPath = $TargetDoc
        searchText = $currentText
        replaceText = $updatedText
    })
}

function Get-RuleHarnessRecurringFailureDocEdits {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PromotionCandidates
    )

    $edits = [System.Collections.Generic.List[object]]::new()

    foreach ($candidate in @($PromotionCandidates)) {
        $targetDoc = [string]$candidate.targetDoc
        $signature = [string]$candidate.signature
        $rationale = [string]$candidate.rationale
        $preferredStrategy = if ($candidate.PSObject.Properties.Name -contains 'preferredStrategy') {
            [string]$candidate.preferredStrategy
        }
        else {
            'N/A'
        }

        $fullPath = Join-Path $RepoRoot $targetDoc
        if (-not (Test-Path -LiteralPath $fullPath)) {
            continue
        }

        $currentText = Get-Content -Path $fullPath -Raw

        $failureType = if ($signature -like '*harness-stage-failed*') {
            'Harness stage failure'
        }
        elseif ($signature -like '*static-scan-failed*') {
            'Static scan failure'
        }
        elseif ($signature -like '*feature-cycle-*') {
            'Feature dependency cycle'
        }
        else {
            'Recurring failure'
        }

        $noteHeader = "## Rule Harness Learned Rules"
        $noteLine = "* **$failureType** (`$Signature`): $preferredStrategy. $rationale"

        if ($currentText.Contains($noteLine)) {
            continue
        }

        $updatedText = if ($currentText.Contains($noteHeader)) {
            $currentText.Replace($noteHeader, "$noteHeader`r`n$noteLine")
        }
        else {
            $currentText.TrimEnd() + "`r`n`r`n$noteHeader`r`n`r`n$noteLine`r`n"
        }

        [void]$edits.Add([pscustomobject]@{
            targetPath = $targetDoc
            searchText = $currentText
            replaceText = $updatedText
        })
    }

    @($edits)
}

function Invoke-RuleHarnessRecurringFailurePromotion {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PromotionCandidates,
        [switch]$DryRun
    )

    $stageResults = [System.Collections.Generic.List[object]]::new()
    $actionItems = [System.Collections.Generic.List[object]]::new()
    $docCommits = [System.Collections.Generic.List[object]]::new()
    $memoryUpdates = [System.Collections.Generic.List[object]]::new()
    $learningTrace = [System.Collections.Generic.List[object]]::new()

    if ($PromotionCandidates.Count -eq 0) {
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'recurring_failure_promotion' `
            -Status 'skipped' `
            -Attempted $true `
            -Summary 'No promotion candidates to process.' `
            -Details ([pscustomobject]@{
                candidateCount = 0
            })))

        return [pscustomobject]@{
            status          = 'skipped'
            attempted       = $true
            failed          = $false
            stageResults    = @($stageResults)
            actionItems     = @($actionItems)
            docCommits      = @($docCommits)
            memoryUpdates   = @($memoryUpdates)
            learningTrace   = @($learningTrace)
            promotedCount   = 0
        }
    }

    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'recurring_failure_promotion' `
        -Status 'in_progress' `
        -Attempted $true `
        -Summary ("Processing {0} promotion candidate(s)." -f $PromotionCandidates.Count) `
        -Details ([pscustomobject]@{
            candidateCount = $PromotionCandidates.Count
        })))

    $docEdits = @(Get-RuleHarnessRecurringFailureDocEdits -RepoRoot $RepoRoot -PromotionCandidates @($PromotionCandidates))

    if ($docEdits.Count -eq 0) {
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'recurring_failure_promotion' `
            -Status 'skipped' `
            -Attempted $true `
            -Summary 'All candidates already have corresponding rules in target docs.' `
            -Details ([pscustomobject]@{
                candidateCount = $PromotionCandidates.Count
                editCount = 0
            })))

        return [pscustomobject]@{
            status          = 'skipped'
            attempted       = $true
            failed          = $false
            stageResults    = @($stageResults)
            actionItems     = @($actionItems)
            docCommits      = @($docCommits)
            memoryUpdates   = @($memoryUpdates)
            learningTrace   = @($learningTrace)
            promotedCount   = 0
        }
    }

    $docApply = Invoke-RuleHarnessDocEdits -Edits @($docEdits) -RepoRoot $RepoRoot -Config $Config -DryRun:$DryRun
    $appliedEdits = @($docApply.edits | Where-Object { $_.status -eq 'applied' })
    $skippedEdits = @($docApply.edits | Where-Object { $_.status -ne 'applied' })

    foreach ($appliedEdit in @($appliedEdits)) {
        $candidate = $PromotionCandidates | Where-Object { $_.targetDoc -eq [string]$appliedEdit.targetPath } | Select-Object -First 1
        if ($null -ne $candidate) {
            [void]$learningTrace.Add([pscustomobject]@{
                batchId             = 'recurring_failure_promotion'
                signature           = [string]$candidate.signature
                promotedTo          = [string]$candidate.targetDoc
                promotionRationale  = [string]$candidate.rationale
            })
        }
    }

    if ($appliedEdits.Count -gt 0) {
        $appliedDocPaths = @($appliedEdits | ForEach-Object { [string]$_.targetPath } | Select-Object -Unique)
        if (-not $DryRun) {
            $docBatch = [pscustomobject]@{ id = 'recurring-failure-promotion' }
            $docCommit = Invoke-RuleHarnessCommit -RepoRoot $RepoRoot -Config $Config -TargetFiles $appliedDocPaths -AppliedBatches @($docBatch)
            [void]$docCommits.Add($docCommit)

            $memoryStore = Read-RuleHarnessMemoryStore -RepoRoot $RepoRoot -Config $Config
            foreach ($candidate in @($PromotionCandidates)) {
                $matchedEdit = $appliedEdits | Where-Object { $_.targetPath -eq [string]$candidate.targetDoc } | Select-Object -First 1
                if ($null -ne $matchedEdit) {
                    $existing = Find-RuleHarnessMemoryEntry -MemoryStore $memoryStore -Signature ([string]$candidate.signature) -ScopePath ([string]$candidate.scopePath)
                    if ($null -ne $existing) {
                        $existing.status = 'promoted'
                        $existing.promotedAt = (Get-Date).ToUniversalTime().ToString('o')
                        $existing.promotedTo = [string]$candidate.targetDoc
                        $memoryStore.dirty = $true
                    }
                }
            }
            if ($memoryStore.dirty) {
                Save-RuleHarnessMemoryStore -MemoryStore $memoryStore
            }
        }
        else {
            [void]$actionItems.Add((New-RuleHarnessActionItem `
                -Kind 'doc_proposal' `
                -Severity 'high' `
                -Summary ("Apply {0} learned rule(s) to owner docs" -f $appliedEdits.Count) `
                -Details ("Dry run detected {0} edit(s) to apply. Re-run without -DryRun to commit changes." -f $appliedEdits.Count) `
                -RelatedPaths @($appliedDocPaths)))
        }
    }

    if ($skippedEdits.Count -gt 0) {
        [void]$actionItems.Add((New-RuleHarnessActionItem `
            -Kind 'info' `
            -Severity 'low' `
            -Summary ("{0} edit(s) were skipped due to conflicts or no changes." -f $skippedEdits.Count) `
            -Details ("Skipped edits: {0}" -f [string](($skippedEdits | ForEach-Object { [string]$_.targetPath }) -join ', '))))
    }

    $finalStatus = if ($appliedEdits.Count -gt 0) { 'passed' } elseif ($docEdits.Count -eq 0) { 'skipped' } else { 'failed' }

    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'recurring_failure_promotion' `
        -Status $finalStatus `
        -Attempted $true `
        -Summary ("Processed {0} candidate(s), applied {1} edit(s)." -f $PromotionCandidates.Count, $appliedEdits.Count) `
        -Details ([pscustomobject]@{
            candidateCount = $PromotionCandidates.Count
            editCount = $docEdits.Count
            appliedCount = $appliedEdits.Count
            skippedCount = $skippedEdits.Count
            appliedDocPaths = @($appliedEdits | ForEach-Object { [string]$_.targetPath } | Select-Object -Unique)
        })))

    [pscustomobject]@{
        status          = $finalStatus
        attempted       = $true
        failed          = ($finalStatus -eq 'failed')
        stageResults    = @($stageResults)
        actionItems     = @($actionItems)
        docCommits      = @($docCommits)
        memoryUpdates   = @($memoryUpdates)
        learningTrace   = @($learningTrace)
        promotedCount   = $appliedEdits.Count
    }
}

function New-RuleHarnessFeatureDependencyRepairResult {
    param(
        [Parameter(Mandatory)]
        [string]$Status,
        [Parameter(Mandatory)]
        [bool]$Attempted,
        [Parameter(Mandatory)]
        [bool]$Failed,
        [Parameter(Mandatory)]
        [int]$AttemptCount,
        [Parameter(Mandatory)]
        [int]$UnsupportedCycleCount,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Summaries,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$StageResults,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$ActionItems,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$ValidationResults,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$MemoryHits,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$MemoryUpdates,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PromotionCandidates,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$LearningTrace,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$UnsupportedFindings,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$CodeCommits,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$DocCommits,
        [object]$PolicySnapshot = $null
    )

    [pscustomobject]@{
        status = $Status
        attempted = $Attempted
        failed = $Failed
        attemptCount = [int]$AttemptCount
        unsupportedCycleCount = [int]$UnsupportedCycleCount
        summaries = @($Summaries)
        stageResults = @($StageResults)
        actionItems = @(Merge-RuleHarnessActionItems -Items @($ActionItems))
        validationResults = @($ValidationResults)
        memoryHits = @($MemoryHits)
        memoryUpdates = @($MemoryUpdates)
        promotionCandidates = @($PromotionCandidates)
        learningTrace = @($LearningTrace)
        unsupportedFindings = @($UnsupportedFindings)
        codeCommits = @($CodeCommits)
        docCommits = @($DocCommits)
        policySnapshot = $PolicySnapshot
    }
}

function Invoke-RuleHarnessFeatureDependencyRepair {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [switch]$DryRun
    )

    $stageResults = [System.Collections.Generic.List[object]]::new()
    $actionItems = [System.Collections.Generic.List[object]]::new()
    $validationResults = [System.Collections.Generic.List[object]]::new()
    $memoryHits = [System.Collections.Generic.List[object]]::new()
    $memoryUpdates = [System.Collections.Generic.List[object]]::new()
    $promotionCandidates = [System.Collections.Generic.List[object]]::new()
    $learningTrace = [System.Collections.Generic.List[object]]::new()
    $summaries = [System.Collections.Generic.List[object]]::new()
    $unsupportedFindings = [System.Collections.Generic.List[object]]::new()
    $codeCommits = [System.Collections.Generic.List[object]]::new()
    $docCommits = [System.Collections.Generic.List[object]]::new()

    $policySnapshot = Get-RuleHarnessCycleRepairPolicySnapshot -RepoRoot $RepoRoot
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'feature_dependency_repair_policy' `
        -Status 'passed' `
        -Attempted $true `
        -Summary $policySnapshot.summary `
        -Details $policySnapshot))

    $initialGate = Get-RuleHarnessFeatureDependencyGateStatus -RepoRoot $RepoRoot -Config $Config
    if ([string]$initialGate.featureDependencyGateStatus -eq 'unsupported') {
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'feature_dependency_repair' `
            -Status 'skipped' `
            -Attempted $false `
            -Summary 'Feature dependency repair skipped because this repository snapshot does not expose LayerDependencyValidator artifacts.' `
            -Details ([pscustomobject]@{
                gateStatus = [string]$initialGate.featureDependencyGateStatus
                reportPath = [string]$initialGate.reportPath
            })))
        return (New-RuleHarnessFeatureDependencyRepairResult `
            -Status 'skipped' `
            -Attempted $false `
            -Failed $false `
            -AttemptCount 0 `
            -UnsupportedCycleCount 0 `
            -Summaries @($summaries) `
            -StageResults @($stageResults) `
            -ActionItems @($actionItems) `
            -ValidationResults @($validationResults) `
            -MemoryHits @($memoryHits) `
            -MemoryUpdates @($memoryUpdates) `
            -PromotionCandidates @($promotionCandidates) `
            -LearningTrace @($learningTrace) `
            -UnsupportedFindings @($unsupportedFindings) `
            -CodeCommits @($codeCommits) `
            -DocCommits @($docCommits) `
            -PolicySnapshot $policySnapshot)
    }

    $memoryStore = Read-RuleHarnessMemoryStore -RepoRoot $RepoRoot -Config $Config
    $learningSettings = Get-RuleHarnessLearningSettings -Config $Config
    $maxRepairAttempts = if ($Config.mutation.PSObject.Properties.Name -contains 'maxBatchesPerRun') {
        [Math]::Max(1, [int]$Config.mutation.maxBatchesPerRun)
    }
    else {
        1
    }

    $attemptCount = 0
    $architectureOwnerDoc = Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot

    for ($attemptIndex = 1; $attemptIndex -le $maxRepairAttempts; $attemptIndex++) {
        $report = Get-RuleHarnessFeatureDependencyReportObject -RepoRoot $RepoRoot -Config $Config
        if ($null -eq $report) {
            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'feature_dependency_repair' `
                -Status 'failed' `
                -Attempted $true `
                -Summary 'Feature dependency repair could not load the dependency report.' `
                -Details ([pscustomobject]@{ reportPath = (Get-RuleHarnessFeatureDependencyReportPath -RepoRoot $RepoRoot -Config $Config).relativePath })))
            return (New-RuleHarnessFeatureDependencyRepairResult `
                -Status 'failed' `
                -Attempted $true `
                -Failed $true `
                -AttemptCount $attemptCount `
                -UnsupportedCycleCount 0 `
                -Summaries @($summaries) `
                -StageResults @($stageResults) `
                -ActionItems @($actionItems) `
                -ValidationResults @($validationResults) `
                -MemoryHits @($memoryHits) `
                -MemoryUpdates @($memoryUpdates) `
                -PromotionCandidates @($promotionCandidates) `
                -LearningTrace @($learningTrace) `
                -UnsupportedFindings @($unsupportedFindings) `
                -CodeCommits @($codeCommits) `
                -DocCommits @($docCommits) `
                -PolicySnapshot $policySnapshot)
        }

        $cycles = @($report.cycles)
        if ($cycles.Count -eq 0) {
            $status = if ($attemptCount -gt 0) { 'passed' } else { 'skipped' }
            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'feature_dependency_repair' `
                -Status $(if ($status -eq 'passed') { 'passed' } else { 'skipped' }) `
                -Attempted ($attemptCount -gt 0) `
                -Summary $(if ($attemptCount -gt 0) { "Feature dependency repair resolved cycle(s) in $attemptCount attempt(s)." } else { 'Feature dependency graph is already acyclic; no repair ran.' }) `
                -Details ([pscustomobject]@{
                    attemptCount = $attemptCount
                    unsupportedCycleCount = 0
                })))
            if (-not $DryRun -and $memoryStore.dirty) {
                Save-RuleHarnessMemoryStore -MemoryStore $memoryStore
            }
            return (New-RuleHarnessFeatureDependencyRepairResult `
                -Status $status `
                -Attempted ($attemptCount -gt 0) `
                -Failed $false `
                -AttemptCount $attemptCount `
                -UnsupportedCycleCount 0 `
                -Summaries @($summaries) `
                -StageResults @($stageResults) `
                -ActionItems @($actionItems) `
                -ValidationResults @($validationResults) `
                -MemoryHits @($memoryHits) `
                -MemoryUpdates @($memoryUpdates) `
                -PromotionCandidates @($promotionCandidates) `
                -LearningTrace @($learningTrace) `
                -UnsupportedFindings @($unsupportedFindings) `
                -CodeCommits @($codeCommits) `
                -DocCommits @($docCommits) `
                -PolicySnapshot $policySnapshot)
        }

        $supportedPlans = [System.Collections.Generic.List[object]]::new()
        $unsupportedCycles = [System.Collections.Generic.List[object]]::new()
        foreach ($cycle in @($cycles)) {
            $candidateEdges = @()
            if ($cycle.PSObject.Properties.Name -contains 'edges' -and $null -ne $cycle.edges) {
                $candidateEdges = @($cycle.edges)
            }

            if ($candidateEdges.Count -eq 0 -and $cycle.PSObject.Properties.Name -contains 'preferredBreakCandidates') {
                foreach ($candidate in @($cycle.preferredBreakCandidates)) {
                    $matchingEdge = @($cycle.edges | Where-Object { [string]$_.from -eq [string]$candidate.from -and [string]$_.to -eq [string]$candidate.to } | Select-Object -First 1)
                    if ($matchingEdge.Count -gt 0) {
                        $candidateEdges += $matchingEdge
                    }
                }
            }

            $attempts = [System.Collections.Generic.List[object]]::new()
            $chosenPlan = $null
            foreach ($edge in @($candidateEdges)) {
                $plan = Get-RuleHarnessPortInversionPlan -RepoRoot $RepoRoot -Cycle $cycle -Edge $edge -PolicySnapshot $policySnapshot
                [void]$attempts.Add($plan)
                if ($plan.supported) {
                    $chosenPlan = $plan
                    break
                }
            }

            if ($null -eq $chosenPlan) {
                $primaryFailure = if ($attempts.Count -gt 0) { $attempts[0] } else { [pscustomobject]@{ reason = 'No candidate edge was available for planning.'; blockedByDocRules = @(); blockedByCodeAmbiguity = @('no-candidate-edge'); blockedBySafety = @() } }
                $cyclePath = Get-RuleHarnessFeatureDependencyCyclePath -Cycle $cycle
                $finding = New-RuleHarnessFinding `
                    -FindingType 'code_violation' `
                    -Severity 'high' `
                    -OwnerDoc $architectureOwnerDoc `
                    -Title 'Feature dependency cycle repair unsupported' `
                    -Message ("Automatic cycle repair is not yet safe for {0}: {1}" -f $cyclePath, [string]$primaryFailure.reason) `
                    -Evidence @($cycle.evidence) `
                    -Confidence 'high' `
                    -Source 'static' `
                    -RemediationKind 'report_only' `
                    -Rationale 'Cycle repair follows AGENTS.md -> owner doc -> code. When the seam is ambiguous or doc constraints do not allow the recipe, the run fails after collecting all unsupported cycles.'
                Set-RuleHarnessObjectProperty -Object $finding -Name 'blockedByDocRules' -Value @($primaryFailure.blockedByDocRules)
                Set-RuleHarnessObjectProperty -Object $finding -Name 'blockedByCodeAmbiguity' -Value @($primaryFailure.blockedByCodeAmbiguity)
                Set-RuleHarnessObjectProperty -Object $finding -Name 'blockedBySafety' -Value @($primaryFailure.blockedBySafety)
                [void]$unsupportedFindings.Add($finding)
                [void]$unsupportedCycles.Add([pscustomobject]@{
                    cycle = $cycle
                    failure = $primaryFailure
                })
                continue
            }

            [void]$supportedPlans.Add($chosenPlan)
        }

        if ($unsupportedCycles.Count -gt 0) {
            foreach ($entry in @($unsupportedCycles)) {
                $cycle = $entry.cycle
                $failure = $entry.failure
                $signature = "feature-cycle-unsupported|" + (Get-RuleHarnessFeatureDependencyCycleSignature -Cycle $cycle)
                $scopePath = if ($cycle.features.Count -gt 0) { "Assets/Scripts/Features/$([string]$cycle.features[0])" } else { 'Assets/Scripts/Features' }
                $headSha = ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim()
                $memoryEntry = Update-RuleHarnessMemoryStoreEntry `
                    -MemoryStore $memoryStore `
                    -Signature $signature `
                    -ScopeType 'feature' `
                    -ScopePath $scopePath `
                    -Symptoms ([string]$failure.reason) `
                    -PreferredRepairStrategy 'Add a narrower structural repair recipe or strengthen owner-doc guidance before retrying.' `
                    -ValidationHints @('tools/rule-harness/write-feature-dependency-report.ps1', 'tools/rule-harness/tests/Run-RuleHarnessTests.ps1') `
                    -Confidence 'high' `
                    -CommitSha $headSha `
                    -PromotionTarget $architectureOwnerDoc `
                    -Status 'observed'
                Set-RuleHarnessObjectProperty -Object $memoryEntry -Name 'selectedRecipe' -Value $null
                Set-RuleHarnessObjectProperty -Object $memoryEntry -Name 'blockedByDocRules' -Value @($failure.blockedByDocRules)
                Set-RuleHarnessObjectProperty -Object $memoryEntry -Name 'blockedByCodeAmbiguity' -Value @($failure.blockedByCodeAmbiguity)
                [void]$memoryUpdates.Add([pscustomobject]@{
                    signature = [string]$memoryEntry.signature
                    scopePath = [string]$memoryEntry.scopePath
                    hitCount = [int]$memoryEntry.hitCount
                    distinctCommitCount = [int]$memoryEntry.distinctCommitCount
                    blockedByDocRules = @($failure.blockedByDocRules)
                    blockedByCodeAmbiguity = @($failure.blockedByCodeAmbiguity)
                })
                $promotionCandidate = Get-RuleHarnessPromotionCandidate -Entry $memoryEntry -Rationale 'Recurring unsupported feature dependency cycle repair' -Config $Config
                if ($null -ne $promotionCandidate) {
                    [void]$promotionCandidates.Add($promotionCandidate)
                }
            }

            if (-not $DryRun -and $memoryStore.dirty) {
                Save-RuleHarnessMemoryStore -MemoryStore $memoryStore
            }

            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'feature_dependency_repair' `
                -Status 'failed' `
                -Attempted $true `
                -Summary ("Feature dependency repair stopped because {0} cycle(s) were unsupported." -f $unsupportedCycles.Count) `
                -Details ([pscustomobject]@{
                    unsupportedCycleCount = $unsupportedCycles.Count
                    attemptCount = $attemptCount
                })))
            [void]$actionItems.Add((New-RuleHarnessActionItem `
                -Kind 'expand-cycle-repair-recipe' `
                -Severity 'high' `
                -Summary 'Extend automatic cycle repair coverage' `
                -Details ("Rule harness collected {0} unsupported cycle(s) after applying AGENTS.md -> owner doc -> code policy ordering." -f $unsupportedCycles.Count) `
                -RelatedPaths @($architectureOwnerDoc, (Get-RuleHarnessFeatureDependencyReportPath -RepoRoot $RepoRoot -Config $Config).relativePath)))

            return (New-RuleHarnessFeatureDependencyRepairResult `
                -Status 'failed' `
                -Attempted $true `
                -Failed $true `
                -AttemptCount $attemptCount `
                -UnsupportedCycleCount $unsupportedCycles.Count `
                -Summaries @($summaries) `
                -StageResults @($stageResults) `
                -ActionItems @($actionItems) `
                -ValidationResults @($validationResults) `
                -MemoryHits @($memoryHits) `
                -MemoryUpdates @($memoryUpdates) `
                -PromotionCandidates @($promotionCandidates) `
                -LearningTrace @($learningTrace) `
                -UnsupportedFindings @($unsupportedFindings) `
                -CodeCommits @($codeCommits) `
                -DocCommits @($docCommits) `
                -PolicySnapshot $policySnapshot)
        }

        $selectedPlan = $supportedPlans[0]
        [void]$summaries.Add([pscustomobject]@{
            cyclePath = [string]$selectedPlan.cyclePath
            recipe = [string]$selectedPlan.recipe
            targetEdge = $selectedPlan.targetEdge
            status = if ($DryRun) { 'planned' } else { 'applying' }
        })
        if ($DryRun) {
            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'feature_dependency_repair' `
                -Status 'skipped' `
                -Attempted $true `
                -Summary ("Dry-run planned feature dependency repair for {0}." -f [string]$selectedPlan.cyclePath) `
                -Details $selectedPlan))
            return (New-RuleHarnessFeatureDependencyRepairResult `
                -Status 'planned' `
                -Attempted $true `
                -Failed $false `
                -AttemptCount 1 `
                -UnsupportedCycleCount 0 `
                -Summaries @($summaries) `
                -StageResults @($stageResults) `
                -ActionItems @($actionItems) `
                -ValidationResults @($validationResults) `
                -MemoryHits @($memoryHits) `
                -MemoryUpdates @($memoryUpdates) `
                -PromotionCandidates @($promotionCandidates) `
                -LearningTrace @($learningTrace) `
                -UnsupportedFindings @($unsupportedFindings) `
                -CodeCommits @($codeCommits) `
                -DocCommits @($docCommits) `
                -PolicySnapshot $policySnapshot)
        }

        $attemptCount++
        $batch = [pscustomobject]@{
            id = "feature-cycle-repair-$attemptIndex"
            kind = 'code_fix'
            targetFiles = @($selectedPlan.targetFiles)
            operations = @($selectedPlan.operations)
        }
        $snapshots = Get-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -TargetFiles @($batch.targetFiles)
        $beforeCycleCount = @($cycles).Count
        try {
            $applyResult = Invoke-RuleHarnessBatchOperations -Batch $batch -RepoRoot $RepoRoot -Config $Config
            $refreshResult = Invoke-RuleHarnessFeatureDependencyReportRefresh -RepoRoot $RepoRoot
            [void]$validationResults.Add([pscustomobject]@{
                batchId = $batch.id
                validation = 'feature_dependency_refresh'
                status = if ($refreshResult.succeeded) { 'passed' } else { 'failed' }
                source = if ($null -eq $refreshResult.scriptPath) { 'none' } else { [string]$refreshResult.scriptPath }
                details = if ($refreshResult.succeeded) { 'Feature dependency report refreshed.' } else { [string]$refreshResult.error }
            })

            $compileRefresh = Invoke-RuleHarnessCompileStatusRefresh -RepoRoot $RepoRoot
            [void]$validationResults.Add([pscustomobject]@{
                batchId = $batch.id
                validation = 'compile_refresh'
                status = if ($compileRefresh.succeeded -or -not $compileRefresh.attempted) { 'passed' } else { 'failed' }
                source = if ($null -eq $compileRefresh.scriptPath) { 'none' } else { [string]$compileRefresh.scriptPath }
                details = if ($compileRefresh.succeeded) { 'Compile status refreshed.' } elseif (-not $compileRefresh.attempted) { 'Compile refresh script not present; using existing handoff file.' } else { [string]$compileRefresh.error }
            })

            $afterGate = Get-RuleHarnessFeatureDependencyGateStatus -RepoRoot $RepoRoot -Config $Config
            $afterCompile = Get-RuleHarnessCompileGateStatus -RepoRoot $RepoRoot -Config $Config
            $afterCycleCount = [int]$afterGate.featureDependencyCycleCount
            $cycleReduced = $afterCycleCount -lt $beforeCycleCount
            $compilePassed = [string]$afterCompile.compileGateStatus -eq 'passed'

            [void]$validationResults.Add([pscustomobject]@{
                batchId = $batch.id
                validation = 'feature_dependency_gate'
                status = if ($cycleReduced) { 'passed' } else { 'failed' }
                source = [string]$afterGate.reportPath
                details = "Before=$beforeCycleCount After=$afterCycleCount"
            })
            [void]$validationResults.Add([pscustomobject]@{
                batchId = $batch.id
                validation = 'compile_gate'
                status = if ($compilePassed) { 'passed' } else { 'failed' }
                source = [string]$afterCompile.statusPath
                details = "Status=$([string]$afterCompile.compileGateStatus) CleanLevel=$([string]$afterCompile.cleanLevel)"
            })

            if (-not $cycleReduced -or -not $compilePassed) {
                Restore-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -Snapshots $snapshots
                [void]$stageResults.Add((New-RuleHarnessStageResult `
                    -Stage 'feature_dependency_repair' `
                    -Status 'failed' `
                    -Attempted $true `
                    -Summary ("Feature dependency repair rollbacked {0} because validation failed." -f [string]$selectedPlan.cyclePath) `
                    -Details ([pscustomobject]@{
                        cycleReduced = $cycleReduced
                        compileGateStatus = [string]$afterCompile.compileGateStatus
                        beforeCycleCount = $beforeCycleCount
                        afterCycleCount = $afterCycleCount
                    })))
                [void]$actionItems.Add((New-RuleHarnessActionItem `
                    -Kind 'repair-cycle-recipe-validation' `
                    -Severity 'high' `
                    -Summary 'Repair recipe did not survive DAG/compile validation' `
                    -Details ("Recipe {0} for {1} was rolled back because cycleReduced={2} compileGateStatus={3}." -f [string]$selectedPlan.recipe, [string]$selectedPlan.cyclePath, $cycleReduced, [string]$afterCompile.compileGateStatus) `
                    -RelatedPaths @($selectedPlan.targetFiles)))
                return (New-RuleHarnessFeatureDependencyRepairResult `
                    -Status 'failed' `
                    -Attempted $true `
                    -Failed $true `
                    -AttemptCount $attemptCount `
                    -UnsupportedCycleCount 0 `
                    -Summaries @($summaries) `
                    -StageResults @($stageResults) `
                    -ActionItems @($actionItems) `
                    -ValidationResults @($validationResults) `
                    -MemoryHits @($memoryHits) `
                    -MemoryUpdates @($memoryUpdates) `
                    -PromotionCandidates @($promotionCandidates) `
                    -LearningTrace @($learningTrace) `
                    -UnsupportedFindings @($unsupportedFindings) `
                    -CodeCommits @($codeCommits) `
                    -DocCommits @($docCommits) `
                    -PolicySnapshot $policySnapshot)
            }

            $commitResult = Invoke-RuleHarnessCommit -RepoRoot $RepoRoot -Config $Config -TargetFiles @($applyResult.touchedPaths) -AppliedBatches @($batch)
            [void]$codeCommits.Add($commitResult)
            $signature = "feature-cycle-repair|" + [string]$selectedPlan.cycleSignature
            $memoryEntry = Update-RuleHarnessMemoryStoreEntry `
                -MemoryStore $memoryStore `
                -Signature $signature `
                -ScopeType 'feature' `
                -ScopePath "Assets/Scripts/Features/$([string]$selectedPlan.consumerFeature)" `
                -Symptoms ("Repaired {0} via {1}" -f [string]$selectedPlan.cyclePath, [string]$selectedPlan.recipe) `
                -PreferredRepairStrategy 'Prefer consumer-owned Application/Ports seams when a cross-feature concrete dependency forms a cycle.' `
                -ValidationHints @('tools/rule-harness/write-feature-dependency-report.ps1', 'tools/rule-harness/write-compile-status.ps1') `
                -Confidence 'high' `
                -CommitSha ([string]$commitResult.sha) `
                -PromotionTarget $architectureOwnerDoc `
                -Status 'resolved'
            Set-RuleHarnessObjectProperty -Object $memoryEntry -Name 'selectedRecipe' -Value ([string]$selectedPlan.recipe)
            Set-RuleHarnessObjectProperty -Object $memoryEntry -Name 'blockedByDocRules' -Value @()
            Set-RuleHarnessObjectProperty -Object $memoryEntry -Name 'blockedByCodeAmbiguity' -Value @()
            [void]$memoryUpdates.Add([pscustomobject]@{
                signature = [string]$memoryEntry.signature
                scopePath = [string]$memoryEntry.scopePath
                hitCount = [int]$memoryEntry.hitCount
                distinctCommitCount = [int]$memoryEntry.distinctCommitCount
                selectedRecipe = [string]$selectedPlan.recipe
            })
            [void]$learningTrace.Add([pscustomobject]@{
                batchId = $batch.id
                attempt = $attemptIndex
                normalizedFailureSignature = $null
                memoryEntriesUsed = @()
                repairDelta = [string]$selectedPlan.recipe
                verificationResult = 'passed'
            })

            $promotionCandidate = Get-RuleHarnessPromotionCandidate -Entry $memoryEntry -Rationale 'Recurring successful feature dependency cycle repair' -Config $Config
            if ($null -ne $promotionCandidate) {
                [void]$promotionCandidates.Add($promotionCandidate)
                $docEdits = @(Get-RuleHarnessManagedCycleRepairDocEdits -RepoRoot $RepoRoot -TargetDoc ([string]$promotionCandidate.targetDoc) -Signature ([string]$promotionCandidate.signature) -Recipe 'consumer-owned Application/Ports port inversion' -Rationale 'This note was promoted from repeated automatic cycle repair results.')
                if ($docEdits.Count -gt 0) {
                    $docApply = Invoke-RuleHarnessDocEdits -Edits @($docEdits) -RepoRoot $RepoRoot -Config $Config
                    $appliedDocPaths = @($docApply.edits | Where-Object status -eq 'applied' | ForEach-Object { [string]$_.targetPath } | Select-Object -Unique)
                    if ($appliedDocPaths.Count -gt 0) {
                        $docBatch = [pscustomobject]@{ id = "feature-cycle-doc-$attemptIndex" }
                        $docCommit = Invoke-RuleHarnessCommit -RepoRoot $RepoRoot -Config $Config -TargetFiles $appliedDocPaths -AppliedBatches @($docBatch)
                        [void]$docCommits.Add($docCommit)
                    }
                }
            }

            [void]$summaries.Add([pscustomobject]@{
                cyclePath = [string]$selectedPlan.cyclePath
                recipe = [string]$selectedPlan.recipe
                targetEdge = $selectedPlan.targetEdge
                status = 'committed'
                commitSha = [string]$commitResult.sha
                remainingCycles = $afterCycleCount
            })
        }
        catch {
            Restore-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -Snapshots $snapshots
            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'feature_dependency_repair' `
                -Status 'failed' `
                -Attempted $true `
                -Summary ("Feature dependency repair failed while applying {0}." -f [string]$selectedPlan.cyclePath) `
                -Details ([pscustomobject]@{ message = $_.Exception.Message })))
            return (New-RuleHarnessFeatureDependencyRepairResult `
                -Status 'failed' `
                -Attempted $true `
                -Failed $true `
                -AttemptCount $attemptCount `
                -UnsupportedCycleCount 0 `
                -Summaries @($summaries) `
                -StageResults @($stageResults) `
                -ActionItems @($actionItems) `
                -ValidationResults @($validationResults) `
                -MemoryHits @($memoryHits) `
                -MemoryUpdates @($memoryUpdates) `
                -PromotionCandidates @($promotionCandidates) `
                -LearningTrace @($learningTrace) `
                -UnsupportedFindings @($unsupportedFindings) `
                -CodeCommits @($codeCommits) `
                -DocCommits @($docCommits) `
                -PolicySnapshot $policySnapshot)
        }
    }

    if (-not $DryRun -and $memoryStore.dirty) {
        Save-RuleHarnessMemoryStore -MemoryStore $memoryStore
    }

    $finalReport = Get-RuleHarnessFeatureDependencyReportObject -RepoRoot $RepoRoot -Config $Config
    $finalCycles = if ($null -eq $finalReport) { 0 } else { @($finalReport.cycles).Count }
    $finalCompile = Get-RuleHarnessCompileGateStatus -RepoRoot $RepoRoot -Config $Config
    $finalStatus = if ($finalCycles -eq 0 -and [string]$finalCompile.compileGateStatus -eq 'passed') { 'passed' } else { 'failed' }
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'feature_dependency_repair' `
        -Status $(if ($finalStatus -eq 'passed') { 'passed' } else { 'failed' }) `
        -Attempted ($attemptCount -gt 0) `
        -Summary ("Feature dependency repair finished with status={0}. RemainingCycles={1} CompileGate={2}" -f $finalStatus, $finalCycles, [string]$finalCompile.compileGateStatus) `
        -Details ([pscustomobject]@{
            attemptCount = $attemptCount
            remainingCycles = $finalCycles
            compileGateStatus = [string]$finalCompile.compileGateStatus
        })))

    return (New-RuleHarnessFeatureDependencyRepairResult `
        -Status $finalStatus `
        -Attempted ($attemptCount -gt 0) `
        -Failed ($finalStatus -ne 'passed') `
        -AttemptCount $attemptCount `
        -UnsupportedCycleCount 0 `
        -Summaries @($summaries) `
        -StageResults @($stageResults) `
        -ActionItems @($actionItems) `
        -ValidationResults @($validationResults) `
        -MemoryHits @($memoryHits) `
        -MemoryUpdates @($memoryUpdates) `
        -PromotionCandidates @($promotionCandidates) `
        -LearningTrace @($learningTrace) `
        -UnsupportedFindings @($unsupportedFindings) `
        -CodeCommits @($codeCommits) `
        -DocCommits @($docCommits) `
        -PolicySnapshot $policySnapshot)
}

function Invoke-RuleHarnessGit {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    $previousPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        @(& git -C $RepoRoot @Arguments 2>$null)
    }
    finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Set-RuleHarnessObjectProperty {
    param(
        [Parameter(Mandatory)]
        [object]$Object,
        [Parameter(Mandatory)]
        [string]$Name,
        $Value
    )

    if ($Object.PSObject.Properties.Name -contains $Name) {
        $Object.$Name = $Value
    }
    else {
        $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
    }
}

function Get-RuleHarnessHistoryStatePath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $relativePath = if ($Config.history.PSObject.Properties.Name -contains 'statePath') {
        [string]$Config.history.statePath
    }
    else {
        'Temp/RuleHarnessState/history.json'
    }

    $fullPath = Join-Path $RepoRoot $relativePath
    [pscustomobject]@{
        relativePath = $relativePath.Replace('\', '/')
        fullPath     = [System.IO.Path]::GetFullPath($fullPath)
    }
}

function Read-RuleHarnessHistoryState {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $pathInfo = Get-RuleHarnessHistoryStatePath -RepoRoot $RepoRoot -Config $Config
    $entries = @{}
    $loadedEntryCount = 0
    $gcRemovedCount = 0
    $schemaVersion = 1

    if (Test-Path -LiteralPath $pathInfo.fullPath) {
        try {
            $raw = Get-Content -Path $pathInfo.fullPath -Raw | ConvertFrom-Json
            if ($raw.PSObject.Properties.Name -contains 'schemaVersion') {
                $schemaVersion = [int]$raw.schemaVersion
            }

            if ($raw.PSObject.Properties.Name -contains 'entries' -and $null -ne $raw.entries) {
                foreach ($property in $raw.entries.PSObject.Properties) {
                    $entries[$property.Name] = $property.Value
                    $loadedEntryCount++
                }
            }
        }
        catch {
            $entries = @{}
            $loadedEntryCount = 0
            $gcRemovedCount = 0
        }
    }

    $gcDays = if ($Config.history.PSObject.Properties.Name -contains 'gcDays') {
        [int]$Config.history.gcDays
    }
    else {
        14
    }

    $cutoff = [DateTimeOffset]::UtcNow.AddDays(-$gcDays)
    foreach ($key in @($entries.Keys)) {
        $entry = $entries[$key]
        $lastSeen = $null
        if ($null -ne $entry -and $entry.PSObject.Properties.Name -contains 'lastSeenUtc') {
            try {
                $lastSeen = [DateTimeOffset]::Parse([string]$entry.lastSeenUtc)
            }
            catch {
                $lastSeen = $null
            }
        }

        if ($null -ne $lastSeen -and $lastSeen -lt $cutoff) {
            $entries.Remove($key)
            $gcRemovedCount++
        }
    }

    [pscustomobject]@{
        schemaVersion    = $schemaVersion
        path             = $pathInfo.fullPath
        relativePath     = $pathInfo.relativePath
        entries          = $entries
        loadedEntryCount = $loadedEntryCount
        gcRemovedCount   = $gcRemovedCount
        touchedKeys      = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    }
}

function Save-RuleHarnessHistoryState {
    param(
        [Parameter(Mandatory)]
        [object]$HistoryState
    )

    $parent = Split-Path -Parent $HistoryState.path
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $orderedEntries = [ordered]@{}
    foreach ($key in @($HistoryState.entries.Keys | Sort-Object)) {
        $orderedEntries[$key] = $HistoryState.entries[$key]
    }

    $payload = [ordered]@{
        schemaVersion = 1
        entries       = $orderedEntries
    }

    Set-Content -Path $HistoryState.path -Value ($payload | ConvertTo-Json -Depth 20) -Encoding UTF8
}

function Get-RuleHarnessFeatureScanStatePath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $relativePath = if ($Config.PSObject.Properties.Name -contains 'state' -and $Config.state.PSObject.Properties.Name -contains 'featureScanStatePath') {
        [string]$Config.state.featureScanStatePath
    }
    else {
        'Temp/RuleHarnessState/feature-scan-state.json'
    }

    [pscustomobject]@{
        relativePath = $relativePath.Replace('\', '/')
        fullPath     = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $relativePath))
    }
}

function Read-RuleHarnessFeatureScanState {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $pathInfo = Get-RuleHarnessFeatureScanStatePath -RepoRoot $RepoRoot -Config $Config
    $entries = @{}
    if (Test-Path -LiteralPath $pathInfo.fullPath) {
        try {
            $raw = Get-Content -Path $pathInfo.fullPath -Raw | ConvertFrom-Json
            foreach ($entry in @($raw.entries)) {
                if ($null -eq $entry -or [string]::IsNullOrWhiteSpace([string]$entry.scopeId)) {
                    continue
                }

                $entries[[string]$entry.scopeId] = $entry
            }
        }
        catch {
            $entries = @{}
        }
    }

    [pscustomobject]@{
        path         = $pathInfo.fullPath
        relativePath = $pathInfo.relativePath
        entries      = $entries
    }
}

function Save-RuleHarnessFeatureScanState {
    param(
        [Parameter(Mandatory)]
        [object]$FeatureScanState
    )

    $parent = Split-Path -Parent $FeatureScanState.path
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $payload = [pscustomobject]@{
        schemaVersion = 1
        entries       = @($FeatureScanState.entries.Values | Sort-Object {
            $entry = $_
            if ($null -eq $entry -or [string]::IsNullOrWhiteSpace([string]$entry.lastCheckedAtUtc)) {
                return '0000-00-00T00:00:00.0000000+00:00'
            }

            [string]$entry.lastCheckedAtUtc
        }, scopeId)
    }
    $payload | ConvertTo-Json -Depth 20 | Set-Content -Path $FeatureScanState.path -Encoding UTF8
}

function Set-RuleHarnessFeatureScanEntry {
    param(
        [Parameter(Mandatory)]
        [object]$FeatureScanState,
        [Parameter(Mandatory)]
        [string]$ScopeId,
        [Parameter(Mandatory)]
        [string]$LastResult,
        [string]$LastFindingSeverity,
        [string]$LastRunId,
        [string]$LastCommitSha,
        [string]$LastStoppedReason
    )

    $FeatureScanState.entries[$ScopeId] = [pscustomobject]@{
        scopeId             = $ScopeId
        lastCheckedAtUtc    = [DateTimeOffset]::UtcNow.ToString('o')
        lastResult          = $LastResult
        lastFindingSeverity = if ([string]::IsNullOrWhiteSpace($LastFindingSeverity)) { $null } else { $LastFindingSeverity }
        lastRunId           = if ([string]::IsNullOrWhiteSpace($LastRunId)) { $null } else { $LastRunId }
        lastCommitSha       = if ([string]::IsNullOrWhiteSpace($LastCommitSha)) { $null } else { $LastCommitSha }
        lastStoppedReason   = if ([string]::IsNullOrWhiteSpace($LastStoppedReason)) { $null } else { $LastStoppedReason }
    }
}

function Get-RuleHarnessOrderedFeatureScopes {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $featureScanState = Read-RuleHarnessFeatureScanState -RepoRoot $RepoRoot -Config $Config
    $scopes = [System.Collections.Generic.List[object]]::new()
    foreach ($feature in @(Get-RuleHarnessFeatureDirectories -RepoRoot $RepoRoot)) {
        $scopeId = [string]$feature.Name
        $stateEntry = if ($featureScanState.entries.ContainsKey($scopeId)) { $featureScanState.entries[$scopeId] } else { $null }
        [void]$scopes.Add([pscustomobject]@{
            scopeId          = $scopeId
            featurePath      = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $feature.FullName
            lastCheckedAtUtc = if ($null -ne $stateEntry -and $stateEntry.PSObject.Properties.Name -contains 'lastCheckedAtUtc') { [string]$stateEntry.lastCheckedAtUtc } else { $null }
        })
    }

    @($scopes | Sort-Object `
        @{ Expression = { if ([string]::IsNullOrWhiteSpace([string]$_.lastCheckedAtUtc)) { 0 } else { 1 } } }, `
        @{ Expression = { if ([string]::IsNullOrWhiteSpace([string]$_.lastCheckedAtUtc)) { '0000-00-00T00:00:00.0000000+00:00' } else { [string]$_.lastCheckedAtUtc } } }, `
        @{ Expression = { [string]$_.scopeId } })
}

function Get-RuleHarnessDocProposalBacklogPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $relativePath = if ($Config.PSObject.Properties.Name -contains 'state' -and $Config.state.PSObject.Properties.Name -contains 'docProposalBacklogPath') {
        [string]$Config.state.docProposalBacklogPath
    }
    else {
        'Temp/RuleHarnessState/doc-proposals.json'
    }

    [pscustomobject]@{
        relativePath = $relativePath.Replace('\', '/')
        fullPath     = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $relativePath))
    }
}

function Read-RuleHarnessDocProposalBacklog {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $pathInfo = Get-RuleHarnessDocProposalBacklogPath -RepoRoot $RepoRoot -Config $Config
    $entries = [System.Collections.Generic.List[object]]::new()
    if (Test-Path -LiteralPath $pathInfo.fullPath) {
        try {
            $raw = Get-Content -Path $pathInfo.fullPath -Raw | ConvertFrom-Json
            $mergedEntries = @{}
            foreach ($rawEntry in @($raw.entries)) {
                $entry = ConvertTo-RuleHarnessDocProposalBacklogEntry -Entry $rawEntry
                if ($null -eq $entry) {
                    continue
                }

                if ($mergedEntries.ContainsKey([string]$entry.signature)) {
                    $mergedEntries[[string]$entry.signature] = Merge-RuleHarnessDocProposalBacklogEntry `
                        -ExistingEntry $mergedEntries[[string]$entry.signature] `
                        -IncomingEntry $entry
                    continue
                }

                $mergedEntries[[string]$entry.signature] = $entry
            }

            foreach ($entry in @($mergedEntries.Values)) {
                [void]$entries.Add($entry)
            }
        }
        catch {
            $entries = [System.Collections.Generic.List[object]]::new()
        }
    }

    [pscustomobject]@{
        path         = $pathInfo.fullPath
        relativePath = $pathInfo.relativePath
        entries      = $entries
        dirty        = $false
    }
}

function Save-RuleHarnessDocProposalBacklog {
    param(
        [Parameter(Mandatory)]
        [object]$Backlog
    )

    $parent = Split-Path -Parent $Backlog.path
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $payload = [pscustomobject]@{
        schemaVersion = 1
        entries       = @($Backlog.entries | Sort-Object signature, targetDoc, primaryEvidencePath)
    }
    $payload | ConvertTo-Json -Depth 30 | Set-Content -Path $Backlog.path -Encoding UTF8
    $Backlog.dirty = $false
}

function Get-RuleHarnessFindingSeverityRank {
    param(
        [string]$Severity
    )

    switch ([string]$Severity) {
        'high' { return 3 }
        'medium' { return 2 }
        'low' { return 1 }
        default { return 0 }
    }
}

function Get-RuleHarnessHighestSeverity {
    param(
        [AllowEmptyCollection()]
        [object[]]$Findings
    )

    $highest = $null
    $rank = -1
    foreach ($finding in @($Findings)) {
        $currentRank = Get-RuleHarnessFindingSeverityRank -Severity ([string]$finding.severity)
        if ($currentRank -gt $rank) {
            $rank = $currentRank
            $highest = [string]$finding.severity
        }
    }

    $highest
}

function Get-RuleHarnessRelatedPathsForFinding {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    $paths = [System.Collections.Generic.List[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace([string]$Finding.ownerDoc)) {
        [void]$paths.Add([string]$Finding.ownerDoc)
    }
    foreach ($evidence in @($Finding.evidence)) {
        if ($null -ne $evidence -and -not [string]::IsNullOrWhiteSpace([string]$evidence.path)) {
            [void]$paths.Add([string]$evidence.path)
        }
    }

    @($paths | Select-Object -Unique)
}

function Test-RuleHarnessFindingMatchesScope {
    param(
        [Parameter(Mandatory)]
        [object]$Finding,
        [Parameter(Mandatory)]
        [string]$ScopeId
    )

    $scopePrefix = "Assets/Scripts/Features/$ScopeId/"
    foreach ($evidence in @($Finding.evidence)) {
        $path = [string]$evidence.path
        if ($path -like "$scopePrefix*" -or $path -eq "Assets/Scripts/Features/$ScopeId") {
            return $true
        }
    }

    if ([string]$Finding.message -match ("Feature '{0}'" -f [regex]::Escape($ScopeId))) {
        return $true
    }

    return $false
}

function ConvertTo-RuleHarnessEvidenceObjects {
    param(
        [AllowEmptyCollection()]
        [object[]]$Evidence
    )

    @($Evidence | ForEach-Object {
        if ($null -eq $_) {
            return
        }

        [pscustomobject]@{
            path    = if ([string]::IsNullOrWhiteSpace([string]$_.path)) { $null } else { ([string]$_.path).Replace('\', '/') }
            line    = if ($null -eq $_.line) { $null } else { [int]$_.line }
            snippet = [string]$_.snippet
        }
    })
}

function Get-RuleHarnessFindingPrimaryEvidencePath {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    foreach ($evidence in @(ConvertTo-RuleHarnessEvidenceObjects -Evidence @($Finding.evidence))) {
        if (-not [string]::IsNullOrWhiteSpace([string]$evidence.path)) {
            return [string]$evidence.path
        }
    }

    return $null
}

function Get-RuleHarnessFindingFamily {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    $findingType = [string]$Finding.findingType
    $title = [string]$Finding.title
    if ([string]::IsNullOrWhiteSpace($title) -and $Finding.PSObject.Properties.Name -contains 'summary') {
        $title = [string]$Finding.summary
    }

    $message = [string]$Finding.message
    if ([string]::IsNullOrWhiteSpace($message) -and $Finding.PSObject.Properties.Name -contains 'details') {
        $message = [string]$Finding.details
    }

    $primaryPath = [string](Get-RuleHarnessFindingPrimaryEvidencePath -Finding $Finding)
    switch ($findingType) {
        'code_violation' {
            if ($title -match 'Domain' -or $message -match 'Domain layer' -or $primaryPath -match '/Domain/') {
                return 'code_violation/domain_framework_api'
            }

            if ($title -match 'Application' -or $message -match 'Application layer' -or $primaryPath -match '/Application/') {
                return 'code_violation/application_unity_api'
            }

            return 'code_violation/generic'
        }
        'missing_rule' {
            if ($title -match 'bootstrap|setup' -or $message -match 'bootstrap|setup') {
                return 'missing_rule/feature_bootstrap_root'
            }

            return 'missing_rule/generic'
        }
        'broken_reference' {
            if ($title -match 'reference|document' -or $message -match 'reference|document|SSOT') {
                return 'broken_reference/markdown_target'
            }

            return 'broken_reference/generic'
        }
        'doc_drift' {
            return 'doc_drift/generic'
        }
        default {
            if ([string]::IsNullOrWhiteSpace($findingType)) {
                return 'unknown/generic'
            }

            return ("{0}/generic" -f $findingType)
        }
    }
}

function Get-RuleHarnessFindingSignature {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    $ownerDoc = [string]$Finding.ownerDoc
    if ($Finding.PSObject.Properties.Name -contains 'targetDoc' -and [string]::IsNullOrWhiteSpace($ownerDoc)) {
        $ownerDoc = [string]$Finding.targetDoc
    }

    $ownerDoc = if ([string]::IsNullOrWhiteSpace($ownerDoc)) { '<no-owner-doc>' } else { $ownerDoc.Replace('\', '/').Trim() }
    $family = Get-RuleHarnessFindingFamily -Finding $Finding
    $primaryPath = Get-RuleHarnessFindingPrimaryEvidencePath -Finding $Finding
    $primaryPath = if ([string]::IsNullOrWhiteSpace($primaryPath)) { '<no-evidence-path>' } else { $primaryPath.Trim() }
    ('{0}|{1}|{2}' -f $ownerDoc, $family, $primaryPath).ToLowerInvariant()
}

function Get-RuleHarnessProposalStatus {
    param(
        [Parameter(Mandatory)]
        [object]$Entry
    )

    $status = if ($Entry.PSObject.Properties.Name -contains 'status') { [string]$Entry.status } else { $null }
    if ([string]::IsNullOrWhiteSpace($status)) {
        return 'active'
    }

    $status
}

function ConvertTo-RuleHarnessDocProposalBacklogEntry {
    param(
        [Parameter(Mandatory)]
        [object]$Entry
    )

    $targetDoc = [string]$Entry.targetDoc
    if ([string]::IsNullOrWhiteSpace($targetDoc)) {
        return $null
    }

    $summary = [string]$Entry.summary
    if ([string]::IsNullOrWhiteSpace($summary) -and $Entry.PSObject.Properties.Name -contains 'normalizedSummary') {
        $summary = [string]$Entry.normalizedSummary
    }
    if ([string]::IsNullOrWhiteSpace($summary)) {
        $summary = "Review rule for $targetDoc"
    }
    $details = if ($Entry.PSObject.Properties.Name -contains 'details') { [string]$Entry.details } else { [string]$Entry.message }
    $evidence = ConvertTo-RuleHarnessEvidenceObjects -Evidence @($Entry.evidence)
    $activeEvidence = if ($Entry.PSObject.Properties.Name -contains 'activeEvidence' -and $null -ne $Entry.activeEvidence) {
        ConvertTo-RuleHarnessEvidenceObjects -Evidence @($Entry.activeEvidence)
    }
    else {
        @($evidence | Select-Object -First 3)
    }

    $findingLike = [pscustomobject]@{
        findingType = if ($Entry.PSObject.Properties.Name -contains 'findingType') { [string]$Entry.findingType } else { $null }
        ownerDoc    = $targetDoc
        targetDoc   = $targetDoc
        title       = $summary
        summary     = $summary
        message     = $details
        details     = $details
        evidence    = $evidence
    }

    $status = Get-RuleHarnessProposalStatus -Entry $Entry
    $findingFamily = if ($Entry.PSObject.Properties.Name -contains 'findingFamily' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.findingFamily)) {
        [string]$Entry.findingFamily
    }
    else {
        Get-RuleHarnessFindingFamily -Finding $findingLike
    }
    $primaryEvidencePath = if ($Entry.PSObject.Properties.Name -contains 'primaryEvidencePath' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.primaryEvidencePath)) {
        ([string]$Entry.primaryEvidencePath).Replace('\', '/')
    }
    else {
        Get-RuleHarnessFindingPrimaryEvidencePath -Finding $findingLike
    }
    $signature = if ($Entry.PSObject.Properties.Name -contains 'signature' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.signature)) {
        [string]$Entry.signature
    }
    else {
        Get-RuleHarnessFindingSignature -Finding $findingLike
    }

    $scopeId = if ($Entry.PSObject.Properties.Name -contains 'scopeId') { [string]$Entry.scopeId } else { $null }
    $activeScopeId = if ($Entry.PSObject.Properties.Name -contains 'activeScopeId' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.activeScopeId)) {
        [string]$Entry.activeScopeId
    }
    else {
        $scopeId
    }
    $firstSeenUtc = if ($Entry.PSObject.Properties.Name -contains 'firstSeenUtc' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.firstSeenUtc)) {
        [string]$Entry.firstSeenUtc
    }
    elseif ($Entry.PSObject.Properties.Name -contains 'lastSeenUtc' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.lastSeenUtc)) {
        [string]$Entry.lastSeenUtc
    }
    else {
        [DateTimeOffset]::UtcNow.ToString('o')
    }
    $lastActiveSeenUtc = if ($Entry.PSObject.Properties.Name -contains 'lastActiveSeenUtc' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.lastActiveSeenUtc)) {
        [string]$Entry.lastActiveSeenUtc
    }
    elseif ($Entry.PSObject.Properties.Name -contains 'lastSeenUtc' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.lastSeenUtc)) {
        [string]$Entry.lastSeenUtc
    }
    else {
        $firstSeenUtc
    }

    [pscustomobject]@{
        targetDoc          = $targetDoc
        findingFamily      = $findingFamily
        primaryEvidencePath = $primaryEvidencePath
        signature          = $signature
        status             = $status
        firstSeenUtc       = $firstSeenUtc
        lastActiveSeenUtc  = $lastActiveSeenUtc
        resolvedAtUtc      = if ($Entry.PSObject.Properties.Name -contains 'resolvedAtUtc' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.resolvedAtUtc)) { [string]$Entry.resolvedAtUtc } else { $null }
        activeScopeId      = if ($status -eq 'active' -and -not [string]::IsNullOrWhiteSpace($activeScopeId)) { $activeScopeId } else { $null }
        activeEvidence     = if ($status -eq 'active') { @($activeEvidence | Select-Object -First 3) } else { @() }
        scopeId            = $scopeId
        normalizedSummary  = if ($Entry.PSObject.Properties.Name -contains 'normalizedSummary' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.normalizedSummary)) { [string]$Entry.normalizedSummary } else { $summary.Trim().ToLowerInvariant() }
        summary            = $summary
        details            = $details
        suggestion         = [string]$Entry.suggestion
        hitCount           = if ($Entry.PSObject.Properties.Name -contains 'hitCount') { [int]$Entry.hitCount } else { 1 }
        lastSeenUtc        = if ($Entry.PSObject.Properties.Name -contains 'lastSeenUtc' -and -not [string]::IsNullOrWhiteSpace([string]$Entry.lastSeenUtc)) { [string]$Entry.lastSeenUtc } else { $lastActiveSeenUtc }
        lastRunId          = if ($Entry.PSObject.Properties.Name -contains 'lastRunId') { [string]$Entry.lastRunId } else { $null }
        severity           = if ($Entry.PSObject.Properties.Name -contains 'severity') { [string]$Entry.severity } else { $null }
        relatedPaths       = @($Entry.relatedPaths | ForEach-Object { [string]$_ } | Select-Object -Unique)
        evidence           = @($evidence | Select-Object -First 3)
    }
}

function Merge-RuleHarnessDocProposalBacklogEntry {
    param(
        [Parameter(Mandatory)]
        [object]$ExistingEntry,
        [Parameter(Mandatory)]
        [object]$IncomingEntry
    )

    $existingActive = ([string]$ExistingEntry.status -eq 'active')
    $incomingActive = ([string]$IncomingEntry.status -eq 'active')
    $winner = $ExistingEntry
    if ([string]$IncomingEntry.lastSeenUtc -gt [string]$ExistingEntry.lastSeenUtc) {
        $winner = $IncomingEntry
    }
    $firstSeenUtc = @(
        [string]$ExistingEntry.firstSeenUtc
        [string]$IncomingEntry.firstSeenUtc
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object | Select-Object -First 1
    $lastActiveSeenUtc = @(
        [string]$ExistingEntry.lastActiveSeenUtc
        [string]$IncomingEntry.lastActiveSeenUtc
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object | Select-Object -Last 1
    $resolvedAtUtc = if ($existingActive -or $incomingActive) {
        $null
    }
    else {
        @(
            [string]$ExistingEntry.resolvedAtUtc
            [string]$IncomingEntry.resolvedAtUtc
        ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object | Select-Object -Last 1
    }
    $lastSeenUtc = @(
        [string]$ExistingEntry.lastSeenUtc
        [string]$IncomingEntry.lastSeenUtc
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Sort-Object | Select-Object -Last 1

    [pscustomobject]@{
        targetDoc           = [string]$winner.targetDoc
        findingFamily       = [string]$winner.findingFamily
        primaryEvidencePath = [string]$winner.primaryEvidencePath
        signature           = [string]$winner.signature
        status              = if ($existingActive -or $incomingActive) { 'active' } else { 'resolved' }
        firstSeenUtc        = $firstSeenUtc
        lastActiveSeenUtc   = $lastActiveSeenUtc
        resolvedAtUtc       = $resolvedAtUtc
        activeScopeId       = if ($incomingActive -and -not [string]::IsNullOrWhiteSpace([string]$IncomingEntry.activeScopeId)) { [string]$IncomingEntry.activeScopeId } elseif ($existingActive) { [string]$ExistingEntry.activeScopeId } else { $null }
        activeEvidence      = if ($incomingActive -and @($IncomingEntry.activeEvidence).Count -gt 0) { @($IncomingEntry.activeEvidence) } elseif ($existingActive) { @($ExistingEntry.activeEvidence) } else { @() }
        scopeId             = if (-not [string]::IsNullOrWhiteSpace([string]$winner.scopeId)) { [string]$winner.scopeId } else { [string]$ExistingEntry.scopeId }
        normalizedSummary   = [string]$winner.normalizedSummary
        summary             = [string]$winner.summary
        details             = [string]$winner.details
        suggestion          = [string]$winner.suggestion
        hitCount            = [int]$ExistingEntry.hitCount + [int]$IncomingEntry.hitCount
        lastSeenUtc         = $lastSeenUtc
        lastRunId           = if ([string]$IncomingEntry.lastSeenUtc -gt [string]$ExistingEntry.lastSeenUtc) { [string]$IncomingEntry.lastRunId } else { [string]$ExistingEntry.lastRunId }
        severity            = [string]$winner.severity
        relatedPaths        = @(@($ExistingEntry.relatedPaths) + @($IncomingEntry.relatedPaths) | Select-Object -Unique)
        evidence            = if (@($winner.evidence).Count -gt 0) { @($winner.evidence) } else { @($ExistingEntry.evidence) }
    }
}

function ConvertTo-RuleHarnessDocProposal {
    param(
        [Parameter(Mandatory)]
        [object]$Finding,
        [Parameter(Mandatory)]
        [string]$ScopeId
    )

    $summary = if ([string]::IsNullOrWhiteSpace([string]$Finding.title)) {
        "Review rule for $([string]$Finding.ownerDoc)"
    }
    else {
        [string]$Finding.title
    }

    [pscustomobject]@{
        findingFamily      = Get-RuleHarnessFindingFamily -Finding $Finding
        primaryEvidencePath = Get-RuleHarnessFindingPrimaryEvidencePath -Finding $Finding
        signature          = Get-RuleHarnessFindingSignature -Finding $Finding
        scopeId            = $ScopeId
        targetDoc          = [string]$Finding.ownerDoc
        severity           = [string]$Finding.severity
        summary            = $summary
        normalizedSummary  = ($summary.Trim().ToLowerInvariant())
        details            = [string]$Finding.message
        suggestion         = ("- Consider adding or tightening a rule in `{0}` for `{1}`." -f [string]$Finding.ownerDoc, $summary)
        relatedPaths       = @(Get-RuleHarnessRelatedPathsForFinding -Finding $Finding)
        evidence           = @($Finding.evidence | ForEach-Object {
            [pscustomobject]@{
                path    = [string]$_.path
                line    = if ($null -eq $_.line) { $null } else { [int]$_.line }
                snippet = [string]$_.snippet
            }
        })
    }
}

function Update-RuleHarnessDocProposalBacklog {
    param(
        [Parameter(Mandatory)]
        [object]$Backlog,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Proposals,
        [Parameter(Mandatory)]
        [string]$RunId
    )

    $createdCount = 0
    $updatedCount = 0
    $reactivatedCount = 0

    foreach ($proposal in @($Proposals)) {
        if ([string]::IsNullOrWhiteSpace([string]$proposal.targetDoc) -or [string]::IsNullOrWhiteSpace([string]$proposal.signature)) {
            continue
        }

        $existing = $null
        foreach ($entry in @($Backlog.entries)) {
            if ([string]$entry.signature -eq [string]$proposal.signature) {
                $existing = $entry
                break
            }
        }

        $timestamp = [DateTimeOffset]::UtcNow.ToString('o')
        if ($null -eq $existing) {
            [void]$Backlog.entries.Add([pscustomobject]@{
                targetDoc           = [string]$proposal.targetDoc
                findingFamily       = [string]$proposal.findingFamily
                primaryEvidencePath = [string]$proposal.primaryEvidencePath
                signature           = [string]$proposal.signature
                status              = 'active'
                firstSeenUtc        = $timestamp
                lastActiveSeenUtc   = $timestamp
                resolvedAtUtc       = $null
                activeScopeId       = [string]$proposal.scopeId
                activeEvidence      = @($proposal.evidence | Select-Object -First 3)
                scopeId             = [string]$proposal.scopeId
                normalizedSummary   = [string]$proposal.normalizedSummary
                summary             = [string]$proposal.summary
                details             = [string]$proposal.details
                suggestion          = [string]$proposal.suggestion
                hitCount            = 1
                lastSeenUtc         = $timestamp
                lastRunId           = $RunId
                severity            = [string]$proposal.severity
                relatedPaths        = @($proposal.relatedPaths)
                evidence            = @($proposal.evidence | Select-Object -First 3)
            })
            $Backlog.dirty = $true
            $createdCount++
            continue
        }

        $wasResolved = ([string](Get-RuleHarnessProposalStatus -Entry $existing) -eq 'resolved')
        $existing.findingFamily = [string]$proposal.findingFamily
        $existing.primaryEvidencePath = [string]$proposal.primaryEvidencePath
        $existing.signature = [string]$proposal.signature
        $existing.status = 'active'
        $existing.firstSeenUtc = if ([string]::IsNullOrWhiteSpace([string]$existing.firstSeenUtc)) { $timestamp } else { [string]$existing.firstSeenUtc }
        $existing.lastActiveSeenUtc = $timestamp
        $existing.resolvedAtUtc = $null
        $existing.activeScopeId = [string]$proposal.scopeId
        $existing.activeEvidence = @($proposal.evidence | Select-Object -First 3)
        $existing.scopeId = [string]$proposal.scopeId
        $existing.normalizedSummary = [string]$proposal.normalizedSummary
        $existing.summary = [string]$proposal.summary
        $existing.details = [string]$proposal.details
        $existing.suggestion = [string]$proposal.suggestion
        $existing.hitCount = [int]$existing.hitCount + 1
        $existing.lastSeenUtc = $timestamp
        $existing.lastRunId = $RunId
        $existing.severity = [string]$proposal.severity
        $existing.relatedPaths = @($proposal.relatedPaths)
        $existing.evidence = @($proposal.evidence | Select-Object -First 3)
        $Backlog.dirty = $true
        if ($wasResolved) {
            $reactivatedCount++
        }
        else {
            $updatedCount++
        }
    }

    [pscustomobject]@{
        createdCount     = $createdCount
        updatedCount     = $updatedCount
        reactivatedCount = $reactivatedCount
    }
}

function Resolve-RuleHarnessDocProposalBacklogForScope {
    param(
        [Parameter(Mandatory)]
        [object]$Backlog,
        [Parameter(Mandatory)]
        [string]$ScopeId,
        [AllowEmptyCollection()]
        [object[]]$CurrentFindings,
        [Parameter(Mandatory)]
        [string]$RunId
    )

    $activeSignatures = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($finding in @($CurrentFindings)) {
        if ([string]::IsNullOrWhiteSpace([string]$finding.ownerDoc)) {
            continue
        }

        [void]$activeSignatures.Add((Get-RuleHarnessFindingSignature -Finding $finding))
    }

    $resolvedCount = 0
    $resolvedSignatures = [System.Collections.Generic.List[string]]::new()
    $timestamp = [DateTimeOffset]::UtcNow.ToString('o')
    foreach ($entry in @($Backlog.entries)) {
        $entryScopeId = if ($entry.PSObject.Properties.Name -contains 'activeScopeId' -and -not [string]::IsNullOrWhiteSpace([string]$entry.activeScopeId)) {
            [string]$entry.activeScopeId
        }
        else {
            [string]$entry.scopeId
        }
        if ([string](Get-RuleHarnessProposalStatus -Entry $entry) -ne 'active' -or $entryScopeId -ne $ScopeId) {
            continue
        }

        if ($activeSignatures.Contains([string]$entry.signature)) {
            continue
        }

        $entry.status = 'resolved'
        $entry.resolvedAtUtc = $timestamp
        $entry.activeScopeId = $null
        $entry.activeEvidence = @()
        $entry.lastRunId = $RunId
        $Backlog.dirty = $true
        $resolvedCount++
        [void]$resolvedSignatures.Add([string]$entry.signature)
    }

    [pscustomobject]@{
        resolvedCount      = $resolvedCount
        resolvedSignatures = @($resolvedSignatures)
    }
}

function Write-RuleHarnessDocProposalFile {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Proposals,
        [Parameter(Mandatory)]
        [string]$Path
    )

    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add('# Rule Harness Doc Proposals')
    [void]$lines.Add('')
    if (@($Proposals).Count -eq 0) {
        [void]$lines.Add('No high/medium doc proposals were generated in this run.')
    }
    else {
        foreach ($proposal in @($Proposals)) {
            [void]$lines.Add(('## {0} -> `{1}`' -f [string]$proposal.summary, [string]$proposal.targetDoc))
            [void]$lines.Add('')
            [void]$lines.Add([string]$proposal.suggestion)
            [void]$lines.Add('')
            [void]$lines.Add([string]$proposal.details)
            if (@($proposal.relatedPaths).Count -gt 0) {
                [void]$lines.Add('')
                [void]$lines.Add(("Related: {0}" -f (@($proposal.relatedPaths) -join ', ')))
            }
            [void]$lines.Add('')
        }
    }

    Set-Content -Path $Path -Value $lines -Encoding UTF8
}

function Get-RuleHarnessHistoryKey {
    param(
        [string]$Branch,
        [string]$CommitSha,
        [Parameter(Mandatory)]
        [string]$Fingerprint
    )

    '{0}|{1}|{2}' -f $(if ([string]::IsNullOrWhiteSpace($Branch)) { '<no-branch>' } else { $Branch }), $CommitSha, $Fingerprint
}

function Get-RuleHarnessHistoryEntry {
    param(
        [Parameter(Mandatory)]
        [object]$HistoryState,
        [string]$Branch,
        [string]$CommitSha,
        [Parameter(Mandatory)]
        [string]$Fingerprint
    )

    $key = Get-RuleHarnessHistoryKey -Branch $Branch -CommitSha $CommitSha -Fingerprint $Fingerprint
    if ($HistoryState.entries.ContainsKey($key)) {
        return $HistoryState.entries[$key]
    }

    return $null
}

function Set-RuleHarnessHistoryEntry {
    param(
        [Parameter(Mandatory)]
        [object]$HistoryState,
        [string]$Branch,
        [string]$CommitSha,
        [Parameter(Mandatory)]
        [string]$Fingerprint,
        [Parameter(Mandatory)]
        [string]$Status,
        [Parameter(Mandatory)]
        [string]$Reason
    )

    $key = Get-RuleHarnessHistoryKey -Branch $Branch -CommitSha $CommitSha -Fingerprint $Fingerprint
    $now = [DateTimeOffset]::UtcNow.ToString('o')
    $previous = if ($HistoryState.entries.ContainsKey($key)) { $HistoryState.entries[$key] } else { $null }
    $firstSeenUtc = if ($null -ne $previous -and $previous.PSObject.Properties.Name -contains 'firstSeenUtc') {
        [string]$previous.firstSeenUtc
    }
    else {
        $now
    }

    $attemptCount = if ($null -ne $previous -and $previous.PSObject.Properties.Name -contains 'attemptCount') {
        [int]$previous.attemptCount + 1
    }
    else {
        1
    }

    $entry = [pscustomobject]@{
        branch       = $Branch
        commitSha    = $CommitSha
        fingerprint  = $Fingerprint
        attemptCount = $attemptCount
        lastStatus   = $Status
        lastReason   = $Reason
        firstSeenUtc = $firstSeenUtc
        lastSeenUtc  = $now
    }

    $HistoryState.entries[$key] = $entry
    [void]$HistoryState.touchedKeys.Add($key)
    $entry
}

function Get-RuleHarnessBatchFingerprint {
    param(
        [Parameter(Mandatory)]
        [object]$Batch
    )

    $stablePayload = [ordered]@{
        kind                     = [string]$Batch.kind
        targetFiles              = @($Batch.targetFiles | Sort-Object)
        expectedFindingsResolved = @($Batch.expectedFindingsResolved | Sort-Object)
        operationTypes           = @($Batch.operations | ForEach-Object { [string]$_.type } | Sort-Object -Unique)
        reason                   = [string]$Batch.reason
    }

    $json = $stablePayload | ConvertTo-Json -Depth 20 -Compress
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($json)
    $hashBytes = [System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)
    -join ($hashBytes | ForEach-Object { $_.ToString('x2') })
}

function Get-RuleHarnessMemoryStorePath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $relativePath = if ($Config.PSObject.Properties.Name -contains 'learning' -and $Config.learning.PSObject.Properties.Name -contains 'memoryPath') {
        [string]$Config.learning.memoryPath
    }
    else {
        'tools/rule-harness/memory/advisory-memory.json'
    }

    [pscustomobject]@{
        relativePath = $relativePath.Replace('\', '/')
        fullPath     = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $relativePath))
    }
}

function Read-RuleHarnessMemoryStore {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $pathInfo = Get-RuleHarnessMemoryStorePath -RepoRoot $RepoRoot -Config $Config
    if (-not (Test-Path -LiteralPath $pathInfo.fullPath)) {
        return [pscustomobject]@{
            path          = $pathInfo.fullPath
            relativePath  = $pathInfo.relativePath
            schemaVersion = 1
            entries       = [System.Collections.Generic.List[object]]::new()
            dirty         = $false
        }
    }

    $raw = Get-Content -Path $pathInfo.fullPath -Raw | ConvertFrom-Json
    $entries = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($raw.entries)) {
        [void]$entries.Add($entry)
    }

    [pscustomobject]@{
        path          = $pathInfo.fullPath
        relativePath  = $pathInfo.relativePath
        schemaVersion = if ($raw.PSObject.Properties.Name -contains 'schemaVersion') { [int]$raw.schemaVersion } else { 1 }
        entries       = $entries
        dirty         = $false
    }
}

function Save-RuleHarnessMemoryStore {
    param(
        [Parameter(Mandatory)]
        [object]$MemoryStore
    )

    $parent = Split-Path -Parent $MemoryStore.path
    if (-not (Test-Path -LiteralPath $parent)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }

    $payload = [pscustomobject]@{
        schemaVersion = $MemoryStore.schemaVersion
        entries       = @($MemoryStore.entries | Sort-Object scopePath, signature)
    }
    $payload | ConvertTo-Json -Depth 50 | Set-Content -Path $MemoryStore.path -Encoding UTF8
    $MemoryStore.dirty = $false
}

function Get-RuleHarnessScopeInfo {
    param(
        [string]$RepoRoot,
        [string]$OwnerDoc,
        [string[]]$TargetFiles = @()
    )

    $scopePath = if (-not [string]::IsNullOrWhiteSpace($OwnerDoc)) { [string]$OwnerDoc } elseif (@($TargetFiles).Count -gt 0) { [string]$TargetFiles[0] } else { 'AGENTS.md' }
    $scopeType = 'global'
    $promotionTarget = $scopePath
    $architectureDoc = if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) { Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot } else { $null }
    $governanceDoc = if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) { Get-RuleHarnessGovernanceOwnerDoc -RepoRoot $RepoRoot } else { $null }
    $unityMcpDoc = if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) { Get-RuleHarnessPreferredClaudeDoc -RepoRoot $RepoRoot -Keywords @('unity_mcp', 'Unity MCP', 'MCP', 'editor automation') } else { $null }

    if ($scopePath -like 'Assets/Scripts/Features/*' -or $scopePath -like 'Assets/Scripts/Shared/*') {
        $scopeType = 'global'
        $promotionTarget = if ([string]::IsNullOrWhiteSpace($architectureDoc)) { 'AGENTS.md' } else { $architectureDoc }
    }
    elseif ($scopePath -like 'Assets/Editor/UnityMcp/*') {
        $scopeType = 'global'
        $promotionTarget = if ([string]::IsNullOrWhiteSpace($unityMcpDoc)) { 'AGENTS.md' } else { $unityMcpDoc }
    }
    elseif (-not [string]::IsNullOrWhiteSpace($RepoRoot) -and (Test-RuleHarnessGlobalRuleDoc -RepoRoot $RepoRoot -RelativePath $scopePath)) {
        $scopeType = 'global'
        if ($scopePath -eq $architectureDoc -or $scopePath -eq 'AGENTS.md') {
            if ([string]::IsNullOrWhiteSpace($governanceDoc) -or $governanceDoc -eq 'AGENTS.md' -or $governanceDoc -eq $scopePath) {
                $promotionTarget = if ([string]::IsNullOrWhiteSpace($architectureDoc)) { 'AGENTS.md' } else { $architectureDoc }
            }
            else {
                $promotionTarget = $governanceDoc
            }
        }
        else {
            $promotionTarget = $scopePath
        }
    }
    elseif ($scopePath -eq 'AGENTS.md') {
        $scopeType = 'global'
        $promotionTarget = if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) { Get-RuleHarnessGovernanceOwnerDoc -RepoRoot $RepoRoot } else { 'AGENTS.md' }
    }
    elseif ($scopePath -like 'tools/rule-harness/*') {
        $scopeType = 'harness'
        $promotionTarget = 'tools/rule-harness/README.md'
    }
    elseif (@($TargetFiles | Where-Object { $_ -like 'tools/rule-harness/*' }).Count -gt 0) {
        $scopeType = 'harness'
        $promotionTarget = 'tools/rule-harness/README.md'
    }

    [pscustomobject]@{
        scopeType       = $scopeType
        scopePath       = $scopePath
        promotionTarget = $promotionTarget
        scopeGuard      = if (Test-RuleHarnessRulesOnlyScope -ScopePath $scopePath -TargetFiles @($TargetFiles)) { 'rules-only' } else { 'default' }
    }
}

function Test-RuleHarnessRulesOnlyPath {
    param(
        [string]$RelativePath
    )

    if ([string]::IsNullOrWhiteSpace($RelativePath)) {
        return $false
    }

    $normalized = ([string]$RelativePath).Replace('\', '/').Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $false
    }

    return $normalized -eq 'AGENTS.md' -or
        $normalized -like 'docs/*' -or
        $normalized -like '.codex/skills/jg-*/*' -or
        $normalized -like '.githooks/*' -or
        $normalized -like 'tools/docs-lint/*' -or
        $normalized -like 'tools/rule-harness/*'
}

function Test-RuleHarnessRulesOnlyScope {
    param(
        [string]$ScopePath,
        [string[]]$TargetFiles = @()
    )

    $normalizedTargets = @($TargetFiles | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($normalizedTargets.Count -gt 0) {
        foreach ($targetPath in @($normalizedTargets)) {
            if (-not (Test-RuleHarnessRulesOnlyPath -RelativePath ([string]$targetPath))) {
                return $false
            }
        }

        return $true
    }

    return (Test-RuleHarnessRulesOnlyPath -RelativePath $ScopePath)
}

function Get-RuleHarnessRulesOnlyViolationTargets {
    param(
        [string[]]$TargetFiles = @()
    )

    @(
        @($TargetFiles) |
            Where-Object { -not (Test-RuleHarnessRulesOnlyPath -RelativePath ([string]$_)) } |
            ForEach-Object { ([string]$_).Replace('\', '/') } |
            Sort-Object -Unique
    )
}

function Get-RuleHarnessRulesOnlyRecurrenceCloseoutStatus {
    param(
        [string]$RepoRoot,
        [object]$ScopeInfo,
        [string[]]$TargetFiles = @()
    )

    $stageName = 'rules_closeout'
    $artifactRelativePath = 'artifacts/rules/issue-recurrence-closeout.json'
    $normalizedTargets = @(
        @($TargetFiles) |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            ForEach-Object { ([string]$_).Replace('\', '/') } |
            Where-Object { $_ -ne $artifactRelativePath -and (Test-RuleHarnessRulesOnlyPath -RelativePath $_) } |
            Sort-Object -Unique
    )

    if ([string]::IsNullOrWhiteSpace($RepoRoot) -or $null -eq $ScopeInfo -or [string]$ScopeInfo.scopeGuard -ne 'rules-only' -or $normalizedTargets.Count -eq 0) {
        return [pscustomobject]@{
            failed = $false
            stageResult = New-RuleHarnessStageResult `
                -Stage $stageName `
                -Status 'skipped' `
                -Attempted $false `
                -Summary 'Rules-only recurrence closeout gate was not applicable.' `
                -Details ([pscustomobject]@{
                    artifactPath = $artifactRelativePath
                    targetFiles = @($normalizedTargets)
                })
            actionItems = @()
        }
    }

    $errors = [System.Collections.Generic.List[string]]::new()
    $artifactAbsolutePath = Join-Path $RepoRoot ($artifactRelativePath -replace '/', '\')
    $payload = $null

    if (-not (Test-Path -LiteralPath $artifactAbsolutePath)) {
        [void]$errors.Add(("Missing closeout artifact `{0}`." -f $artifactRelativePath))
    }
    else {
        try {
            $payload = Get-Content -LiteralPath $artifactAbsolutePath -Raw -Encoding UTF8 | ConvertFrom-Json -Depth 20
        }
        catch {
            [void]$errors.Add(("Failed to parse `{0}` as JSON. {1}" -f $artifactRelativePath, $_.Exception.Message))
        }
    }

    if ($null -ne $payload) {
        if ($payload.PSObject.Properties.Name -notcontains 'scope' -or [string]$payload.scope -ne 'rules-only') {
            [void]$errors.Add('Closeout artifact `scope` must be `rules-only`.')
        }

        if ($payload.PSObject.Properties.Name -notcontains 'issueDetected' -or $payload.issueDetected -isnot [bool]) {
            [void]$errors.Add('Closeout artifact `issueDetected` must be a boolean.')
        }

        foreach ($field in @('declaredLane', 'observedMutationClass', 'acceptanceEvidenceClass', 'verification', 'blockedReason')) {
            if ($payload.PSObject.Properties.Name -notcontains $field -or $payload.$field -isnot [string]) {
                [void]$errors.Add(("Closeout artifact `{0}` must be a string." -f $field))
            }
        }

        if ($payload.PSObject.Properties.Name -notcontains 'escalationRequired' -or $payload.escalationRequired -isnot [bool]) {
            [void]$errors.Add('Closeout artifact `escalationRequired` must be a boolean.')
        }

        if ($payload.PSObject.Properties.Name -notcontains 'verification' -or [string]::IsNullOrWhiteSpace([string]$payload.verification)) {
            [void]$errors.Add('Closeout artifact `verification` must not be empty.')
        }

        if ($payload.issueDetected -eq $true) {
            foreach ($field in @('declaredLane', 'observedMutationClass', 'acceptanceEvidenceClass', 'rootCause', 'prevention', 'verification')) {
                if ($payload.PSObject.Properties.Name -notcontains $field -or [string]::IsNullOrWhiteSpace([string]$payload.$field)) {
                    [void]$errors.Add(("Closeout artifact `{0}` must not be empty when `issueDetected = true`." -f $field))
                }
            }
        }

        if ($payload.PSObject.Properties.Name -contains 'escalationRequired' -and $payload.escalationRequired -eq $true -and [string]::IsNullOrWhiteSpace([string]$payload.blockedReason)) {
            [void]$errors.Add('Closeout artifact `blockedReason` must not be empty when `escalationRequired = true`.')
        }

        if ($payload.PSObject.Properties.Name -notcontains 'changedPaths') {
            [void]$errors.Add('Closeout artifact `changedPaths` must be an array of strings.')
        }
        else {
            $changedPaths = @(
                @($payload.changedPaths) |
                    ForEach-Object { ([string]$_).Replace('\', '/') } |
                    Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
                    Sort-Object -Unique
            )

            foreach ($targetFile in @($normalizedTargets)) {
                if ($changedPaths -notcontains $targetFile) {
                    [void]$errors.Add(("Closeout artifact `changedPaths` must include `{0}`." -f $targetFile))
                }
            }
        }
    }

    $failed = $errors.Count -gt 0
    return [pscustomobject]@{
        failed = $failed
        stageResult = New-RuleHarnessStageResult `
            -Stage $stageName `
            -Status $(if ($failed) { 'failed' } else { 'passed' }) `
            -Attempted $true `
            -Summary $(if ($failed) { 'Rules-only recurrence closeout gate failed.' } else { 'Rules-only recurrence closeout gate passed.' }) `
            -Details ([pscustomobject]@{
                artifactPath = $artifactRelativePath
                targetFiles = @($normalizedTargets)
                errorCount = $errors.Count
                errors = @($errors)
            })
        actionItems = @(
            if ($failed) {
                $relatedPaths = @($artifactRelativePath) + @($normalizedTargets)
                New-RuleHarnessActionItem `
                    -Kind 'rules-only-recurrence-closeout' `
                    -Severity 'high' `
                    -Summary 'Update rules-only recurrence closeout artifact' `
                    -Details ([string](@($errors) -join ' ')) `
                    -RelatedPaths $relatedPaths
            }
        )
    }
}

function Get-RuleHarnessLearningSettings {
    param(
        [Parameter(Mandatory)]
        [object]$Config
    )

    $learningConfig = if ($Config.PSObject.Properties.Name -contains 'learning') { $Config.learning } else { [pscustomobject]@{} }
    [pscustomobject]@{
        maxSameRunAttempts      = if ($learningConfig.PSObject.Properties.Name -contains 'maxSameRunAttempts') { [int]$learningConfig.maxSameRunAttempts } else { 2 }
        promotionMinHits        = if ($learningConfig.PSObject.Properties.Name -contains 'promotionMinHits') { [int]$learningConfig.promotionMinHits } else { 3 }
        promotionMinDistinctCommits = if ($learningConfig.PSObject.Properties.Name -contains 'promotionMinDistinctCommits') { [int]$learningConfig.promotionMinDistinctCommits } else { 2 }
    }
}

function Get-RuleHarnessFeatureTestAssets {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$FeatureNames
    )

    $assets = [System.Collections.Generic.List[string]]::new()
    foreach ($featureName in @($FeatureNames | Sort-Object -Unique)) {
        $featureTestRoot = Join-Path $RepoRoot "Tests/$featureName"
        if (-not (Test-Path -LiteralPath $featureTestRoot)) {
            continue
        }

        foreach ($file in Get-ChildItem -LiteralPath $featureTestRoot -Recurse -File -ErrorAction SilentlyContinue) {
            [void]$assets.Add((ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $file.FullName))
        }
    }

    @($assets | Sort-Object -Unique)
}

function Get-RuleHarnessBatchFeatureNames {
    param(
        [Parameter(Mandatory)]
        [object]$Batch
    )

    $featureNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($featureName in @($Batch.featureNames)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$featureName)) {
            [void]$featureNames.Add([string]$featureName)
        }
    }

    foreach ($targetPath in @($Batch.targetFiles)) {
        $normalized = [string]$targetPath
        if ($normalized -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
            [void]$featureNames.Add([string]$Matches['feature'])
        }
    }

    @($featureNames | Sort-Object)
}

function Test-RuleHarnessSensitiveValidationScope {
    param(
        [Parameter(Mandatory)]
        [string[]]$TargetFiles
    )

    foreach ($targetPath in @($TargetFiles)) {
        $normalized = ([string]$targetPath).Replace('\', '/')
        if ($normalized -like 'Assets/Editor/UnityMcp/*' -or
            $normalized -like 'Assets/Scenes/*.unity' -or
            $normalized -like 'Assets/**/*.prefab') {
            return $true
        }
    }

    return $false
}

function Get-RuleHarnessInferredStructuralChecks {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $checks = [System.Collections.Generic.List[object]]::new()
    if ($Batch.kind -ne 'rule_fix') {
        [void]$checks.Add([pscustomobject]@{
            name     = 'target_files_exist'
            source   = 'inferred'
            runnable = $true
            details  = 'Ensure all mutation target files still exist after apply.'
        })
        [void]$checks.Add([pscustomobject]@{
            name     = 'expected_findings_resolved'
            source   = 'inferred'
            runnable = $true
            details  = 'Static scan must resolve the expected findings without introducing new high-severity findings.'
        })
        [void]$checks.Add([pscustomobject]@{
            name     = 'no_new_high_severity_findings'
            source   = 'inferred'
            runnable = $true
            details  = 'Static scan must not introduce new high-severity findings.'
        })
        [void]$checks.Add([pscustomobject]@{
            name     = 'feature_root_contract_present'
            source   = 'inferred'
            runnable = $true
            details  = 'Touched feature scopes must keep a root-level *Setup.cs or *Bootstrap.cs contract.'
        })
        [void]$checks.Add([pscustomobject]@{
            name     = 'owner_doc_references_resolve'
            source   = 'inferred'
            runnable = $true
            details  = 'AGENTS.md and current global owner docs must keep valid markdown references.'
        })

        $featureNames = @(Get-RuleHarnessBatchFeatureNames -Batch $Batch)
        $featureTestAssets = @(Get-RuleHarnessFeatureTestAssets -RepoRoot $RepoRoot -FeatureNames $featureNames)
        if ($featureTestAssets.Count -gt 0) {
            [void]$checks.Add([pscustomobject]@{
                name     = 'feature_test_assets_present'
                source   = 'feature_test_assets'
                runnable = $false
                details  = "Feature test assets detected: $($featureTestAssets.Count)"
            })
        }

        if (Test-RuleHarnessSensitiveValidationScope -TargetFiles @($Batch.targetFiles)) {
            [void]$checks.Add([pscustomobject]@{
                name     = 'unity_or_scene_scope_detected'
                source   = 'inferred'
                runnable = $false
                details  = 'UnityMcp, scene, or prefab scope detected; runtime validation is not built into this phase.'
            })
        }
    }

    @($checks)
}

function Test-RuleHarnessFeatureRootContracts {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$FeatureNames
    )

    $missing = [System.Collections.Generic.List[string]]::new()
    foreach ($featureName in @($FeatureNames | Sort-Object -Unique)) {
        $featureRoot = Join-Path $RepoRoot "Assets/Scripts/Features/$featureName"
        if (-not (Test-Path -LiteralPath $featureRoot)) {
            [void]$missing.Add($featureName)
            continue
        }

        $hasContract = @(
            Get-ChildItem -LiteralPath $featureRoot -File -Filter '*Setup.cs' -ErrorAction SilentlyContinue
            Get-ChildItem -LiteralPath $featureRoot -File -Filter '*Bootstrap.cs' -ErrorAction SilentlyContinue
        ).Count -gt 0
        if (-not $hasContract) {
            [void]$missing.Add($featureName)
        }
    }

    [pscustomobject]@{
        passed  = ($missing.Count -eq 0)
        missing = @($missing)
    }
}

function Test-RuleHarnessOwnerDocReferences {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $broken = [System.Collections.Generic.List[string]]::new()
    foreach ($doc in Get-RuleHarnessScopeDocs -RepoRoot $RepoRoot -Config $Config) {
        $docFull = Join-Path $RepoRoot $doc
        if (-not (Test-Path -LiteralPath $docFull)) {
            [void]$broken.Add($doc)
            continue
        }

        foreach ($target in Get-RuleHarnessMarkdownTargets -Content (Get-Content -Path $docFull -Raw)) {
            $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath $doc -Target $target
            if ($null -ne $resolved -and -not $resolved.Exists) {
                [void]$broken.Add("$doc -> $target")
            }
        }
    }

    [pscustomobject]@{
        passed = ($broken.Count -eq 0)
        broken = @($broken | Select-Object -Unique)
    }
}

function Get-RuleHarnessDiscoveredValidationPlan {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $scopeInfo = Get-RuleHarnessScopeInfo -RepoRoot $RepoRoot -OwnerDoc ([string](@($Batch.ownerDocs)[0])) -TargetFiles @($Batch.targetFiles)
    if ($Batch.kind -eq 'rule_fix') {
        return [pscustomobject]@{
            batchId            = $Batch.id
            scopeType          = $scopeInfo.scopeType
            scopePath          = $scopeInfo.scopePath
            source             = 'rule-only'
            confidence         = 'high'
            runnable           = $false
            featureTestAssets  = @()
            inferredChecks     = @()
            checks             = @()
            status             = 'rule_only'
            reasonCode         = 'rule-only'
        }
    }

    $featureNames = @(Get-RuleHarnessBatchFeatureNames -Batch $Batch)
    $featureTestAssets = @(Get-RuleHarnessFeatureTestAssets -RepoRoot $RepoRoot -FeatureNames $featureNames)
    $inferredChecks = @(Get-RuleHarnessInferredStructuralChecks -Batch $Batch -RepoRoot $RepoRoot)
    $source = if (@($featureTestAssets).Count -gt 0) {
        'feature_test_assets'
    }
    else {
        'inferred'
    }
    $hasSensitiveScope = Test-RuleHarnessSensitiveValidationScope -TargetFiles @($Batch.targetFiles)
    $hasCrossFeatureScope = $featureNames.Count -gt 1
    $confidence = if (@($featureTestAssets).Count -gt 0 -and -not $hasSensitiveScope -and -not $hasCrossFeatureScope) {
        'medium'
    }
    else {
        'low'
    }

    $checks = [System.Collections.Generic.List[object]]::new()
    foreach ($check in $inferredChecks) {
        [void]$checks.Add($check)
    }

    [pscustomobject]@{
        batchId            = $Batch.id
        scopeType          = $scopeInfo.scopeType
        scopePath          = $scopeInfo.scopePath
        source             = $source
        confidence         = $confidence
        runnable           = (@($checks | Where-Object { $_.runnable }).Count -gt 0)
        featureTestAssets  = @($featureTestAssets)
        inferredChecks     = @($inferredChecks)
        checks             = @($checks)
        status             = 'ok'
        reasonCode         = $null
    }
}

function Get-RuleHarnessFailureSignature {
    param(
        [Parameter(Mandatory)]
        [string]$FailureReason,
        [string]$PrimarySource,
        [string]$OwnerDoc,
        [string]$BatchKind,
        [string]$Message
    )

    $normalizedMessage = if ([string]::IsNullOrWhiteSpace($Message)) {
        ''
    }
    else {
        ([regex]::Replace([string]$Message, '\d+', '#')).Trim()
    }

    '{0}|{1}|{2}|{3}|{4}' -f $FailureReason, $PrimarySource, $OwnerDoc, $BatchKind, $normalizedMessage
}

function Find-RuleHarnessMemoryEntry {
    param(
        [Parameter(Mandatory)]
        [object]$MemoryStore,
        [Parameter(Mandatory)]
        [string]$Signature,
        [Parameter(Mandatory)]
        [string]$ScopePath
    )

    $matches = @($MemoryStore.entries | Where-Object {
        [string]$_.signature -eq $Signature -and [string]$_.scopePath -eq $ScopePath
    } | Select-Object -First 1)
    if ($matches.Count -eq 0) {
        return $null
    }

    $matches[0]
}

function Update-RuleHarnessMemoryStoreEntry {
    param(
        [Parameter(Mandatory)]
        [object]$MemoryStore,
        [Parameter(Mandatory)]
        [string]$Signature,
        [Parameter(Mandatory)]
        [string]$ScopeType,
        [Parameter(Mandatory)]
        [string]$ScopePath,
        [Parameter(Mandatory)]
        [string]$Symptoms,
        [Parameter(Mandatory)]
        [string]$PreferredRepairStrategy,
        [string[]]$ValidationHints = @(),
        [Parameter(Mandatory)]
        [string]$Confidence,
        [Parameter(Mandatory)]
        [string]$CommitSha,
        [Parameter(Mandatory)]
        [string]$PromotionTarget,
        [Parameter(Mandatory)]
        [string]$Status
    )

    $existing = Find-RuleHarnessMemoryEntry -MemoryStore $MemoryStore -Signature $Signature -ScopePath $ScopePath
    $now = (Get-Date).ToUniversalTime().ToString('o')
    if ($null -eq $existing) {
        $existing = [pscustomobject]@{
            signature               = $Signature
            scopeType               = $ScopeType
            scopePath               = $ScopePath
            symptoms                = $Symptoms
            preferredRepairStrategy = $PreferredRepairStrategy
            validationHints         = @($ValidationHints | Select-Object -Unique)
            confidence              = $Confidence
            hitCount                = 1
            distinctCommitCount     = 1
            commitShas              = @($CommitSha)
            lastSeen                = $now
            promotionTarget         = $PromotionTarget
            status                  = $Status
        }
        [void]$MemoryStore.entries.Add($existing)
    }
    else {
        $existing.symptoms = $Symptoms
        $existing.preferredRepairStrategy = $PreferredRepairStrategy
        $existing.validationHints = @(@($existing.validationHints) + @($ValidationHints) | Select-Object -Unique)
        $existing.confidence = $Confidence
        $existing.hitCount = [int]$existing.hitCount + 1
        $commitShas = @(@($existing.commitShas) + @($CommitSha) | Select-Object -Unique)
        $existing.commitShas = $commitShas
        $existing.distinctCommitCount = @($commitShas).Count
        $existing.lastSeen = $now
        $existing.promotionTarget = $PromotionTarget
        $existing.status = $Status
    }

    $MemoryStore.dirty = $true
    $existing
}

function Get-RuleHarnessPromotionCandidate {
    param(
        [Parameter(Mandatory)]
        [object]$Entry,
        [Parameter(Mandatory)]
        [string]$Rationale,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $settings = Get-RuleHarnessLearningSettings -Config $Config
    if ([int]$Entry.hitCount -lt $settings.promotionMinHits -or [int]$Entry.distinctCommitCount -lt $settings.promotionMinDistinctCommits) {
        return $null
    }

    [pscustomobject]@{
        signature              = [string]$Entry.signature
        scopeType              = [string]$Entry.scopeType
        scopePath              = [string]$Entry.scopePath
        targetDoc              = [string]$Entry.promotionTarget
        rationale              = $Rationale
        hitCount               = [int]$Entry.hitCount
        distinctCommitCount    = [int]$Entry.distinctCommitCount
        suggestedRuleTextScope = if ([string]$Entry.scopeType -eq 'feature') { 'owner-doc-amendment' } elseif ([string]$Entry.scopeType -eq 'harness') { 'harness-guidance-amendment' } else { 'global-rule-amendment' }
        status                 = [string]$Entry.status
    }
}

function Get-RuleHarnessRiskLabel {
    param(
        [int]$Score
    )

    if ($Score -le 25) {
        return 'low'
    }
    if ($Score -le 50) {
        return 'medium'
    }

    'high'
}

function Get-RuleHarnessBatchRiskAssessment {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [string]$MutationMode
    )

    if (-not $Config.risk.enabled) {
        return [pscustomobject]@{
            score     = 0
            label     = 'low'
            threshold = 0
            allowed   = $true
        }
    }

    $scores = $Config.risk.scores
    $score = 0
    if ($Batch.kind -eq 'mixed_fix') {
        $score = [int]$scores.mixedRuleCodeBatch
    }
    elseif ($Batch.kind -eq 'rule_fix') {
        $sourceFindingTypes = @($Batch.sourceFindingTypes)
        $isBrokenRefBatch = $sourceFindingTypes.Count -gt 0 -and @($sourceFindingTypes | Where-Object { $_ -notin @('broken_reference', 'doc_drift') }).Count -eq 0
        $score = if ($isBrokenRefBatch) { [int]$scores.brokenReferenceDocFix } else { [int]$scores.featureReadmeContract }
    }
    elseif ($Batch.kind -eq 'code_fix') {
        $allTargetsMissing = @($Batch.targetFiles | Where-Object { Test-Path -LiteralPath (Join-Path $RepoRoot $_) }).Count -eq 0
        $isScaffoldBatch = $allTargetsMissing -and @($Batch.targetFiles | Where-Object { $_ -match '(Setup|Bootstrap)\.cs$' }).Count -eq @($Batch.targetFiles).Count
        $isPlaceholderCleanupBatch = -not $allTargetsMissing -and
            @($Batch.targetFiles).Count -gt 0 -and
            @($Batch.targetFiles | Where-Object { $_ -match '(Setup|Bootstrap)\.cs$' }).Count -eq @($Batch.targetFiles).Count -and
            @($Batch.sourceFindingTypes | Where-Object { $_ -ne 'tech_debt' }).Count -eq 0 -and
            [string]$Batch.reason -match 'placeholder'
        if ($isScaffoldBatch) {
            $score = [int]$scores.featureRootScaffold
        }
        elseif ($isPlaceholderCleanupBatch -and $scores.PSObject.Properties.Name -contains 'featureRootPlaceholderCleanup') {
            $score = [int]$scores.featureRootPlaceholderCleanup
        }
        else {
            $score = [int]$scores.existingCodeFileEdit
        }
    }

    if (@($Batch.targetFiles | Where-Object { Test-RuleHarnessGlobalRuleDoc -RepoRoot $RepoRoot -RelativePath ([string]$_) }).Count -gt 0) {
        $score += [int]$scores.agentDocPenalty
    }

    $threshold = 0
    if ($Config.risk.maxAutoApplyScoreByMode.PSObject.Properties.Name -contains $MutationMode) {
        $threshold = [int]$Config.risk.maxAutoApplyScoreByMode.$MutationMode
    }

    [pscustomobject]@{
        score     = $score
        label     = Get-RuleHarnessRiskLabel -Score $score
        threshold = $threshold
        allowed   = ($score -le $threshold)
    }
}

function Get-RuleHarnessBatchOwnershipAssessment {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    if (-not $Config.ownership.enabled) {
        return [pscustomobject]@{
            status = 'disabled'
            reason = $null
        }
    }

    $ownerDocs = @($Batch.ownerDocs | Sort-Object -Unique)
    if ($ownerDocs.Count -eq 0) {
        return [pscustomobject]@{
            status = 'rejected'
            reason = 'Batch has no owner docs.'
        }
    }

    $architectureOwnerDoc = Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot

    foreach ($targetPath in @($Batch.targetFiles)) {
        $normalized = $targetPath.Replace('\', '/')
        if (Test-RuleHarnessWildcardMatch -Path $normalized -Patterns $Config.docs.denylist) {
            return [pscustomobject]@{
                status = 'rejected'
                reason = 'Target path is denied for mutation.'
            }
        }

        if ($normalized -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
            $allowedOwnerDocs = @($architectureOwnerDoc) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -Unique
            if (@($ownerDocs | Where-Object { $_ -notin $allowedOwnerDocs }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = "Feature-owned path '$normalized' requires the current global architecture rule doc."
                }
            }
            continue
        }

        if ($normalized -like 'Assets/Scripts/Shared/*') {
            $allowedOwnerDocs = @($architectureOwnerDoc) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -Unique
            if (@($ownerDocs | Where-Object { $_ -notin $allowedOwnerDocs }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = "Shared path '$normalized' only allows the current global architecture rule doc."
                }
            }
            continue
        }

        if ($normalized -like 'Assets/Editor/UnityMcp/*') {
            $unityMcpOwnerDoc = Get-RuleHarnessPreferredClaudeDoc -RepoRoot $RepoRoot -Keywords @('unity_mcp', 'Unity MCP', 'MCP', 'editor automation')
            if (@($ownerDocs | Where-Object { $_ -ne $unityMcpOwnerDoc }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = "UnityMcp path '$normalized' requires the current Unity MCP rule doc referenced from AGENTS.md."
                }
            }
            continue
        }

        if ($normalized -eq 'AGENTS.md') {
            if ($Batch.kind -ne 'rule_fix' -or @($Batch.sourceFindingTypes | Where-Object { $_ -notin @('broken_reference', 'doc_drift') }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = 'AGENTS.md only allows rule-fix batches backed by broken_reference or doc_drift findings.'
                }
            }
            continue
        }

        if (Test-RuleHarnessGlobalRuleDoc -RepoRoot $RepoRoot -RelativePath $normalized) {
            if ($Batch.kind -ne 'rule_fix' -or @($Batch.sourceFindingTypes | Where-Object { $_ -notin @('broken_reference', 'doc_drift') }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = "Global rule docs only allow rule-fix batches backed by broken_reference or doc_drift findings."
                }
            }
            continue
        }
    }

    [pscustomobject]@{
        status = 'accepted'
        reason = $null
    }
}

function Get-RuleHarnessStaticFindings {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [string]$ScopeId
    )

    $findings = [System.Collections.Generic.List[object]]::new()
    $architectureOwnerDoc = Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot

    $featureDirectories = if ([string]::IsNullOrWhiteSpace($ScopeId)) {
        @(Get-RuleHarnessFeatureDirectories -RepoRoot $RepoRoot)
    }
    else {
        @(Get-RuleHarnessFeatureDirectories -RepoRoot $RepoRoot | Where-Object Name -eq $ScopeId)
    }

    $scriptFiles = @(Get-RuleHarnessScriptFiles -RepoRoot $RepoRoot -Config $Config)
    if (-not [string]::IsNullOrWhiteSpace($ScopeId)) {
        $scopePrefix = "Assets/Scripts/Features/$ScopeId/"
        $scriptFiles = @($scriptFiles | Where-Object {
            (ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $_.FullName) -like "$scopePrefix*"
        })
    }

    $docCandidates = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

    if ([string]::IsNullOrWhiteSpace($ScopeId)) {
        foreach ($doc in Get-RuleHarnessScopeDocs -RepoRoot $RepoRoot -Config $Config) {
            $docFull = Join-Path $RepoRoot $doc
            if (-not (Test-Path -LiteralPath $docFull)) {
                [void]$findings.Add((New-RuleHarnessFinding `
                    -FindingType 'broken_reference' `
                    -Severity $Config.severityPolicy.missingSsotReference `
                    -OwnerDoc 'AGENTS.md' `
                    -Title 'Missing SSOT document' `
                    -Message "SSOT scope references a document that does not exist: $doc" `
                    -Evidence @([pscustomobject]@{ path = 'AGENTS.md'; line = $null; snippet = $doc }) `
                    -RemediationKind 'rule_fix' `
                    -Rationale 'The missing path lives in the SSOT scope and should be corrected in documentation.'))
                continue
            }

            foreach ($target in Get-RuleHarnessMarkdownTargets -Content (Get-Content -Path $docFull -Raw)) {
                $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath $doc -Target $target
                if ($null -eq $resolved) {
                    continue
                }

                if (-not $resolved.Exists) {
                    [void]$findings.Add((New-RuleHarnessFinding `
                        -FindingType 'broken_reference' `
                        -Severity $Config.severityPolicy.brokenReference `
                        -OwnerDoc $doc `
                        -Title 'Broken markdown reference' `
                        -Message "Document reference does not resolve to an existing file: $target" `
                        -Evidence @([pscustomobject]@{ path = $doc; line = $null; snippet = $target }) `
                        -Confidence 'high' `
                        -RemediationKind 'rule_fix' `
                        -Rationale 'The repository path is stale and should be synchronized in the owning document.'))
                }
            }
        }
    }

    foreach ($feature in $featureDirectories) {
        $relative = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $feature.FullName
        $rootBootstrap = Get-ChildItem -LiteralPath $feature.FullName -File |
            Where-Object { $_.Extension -eq '.cs' -and ($_.Name -like '*Setup.cs' -or $_.Name -like '*Bootstrap.cs') } |
            Select-Object -First 1

        if ($null -eq $rootBootstrap) {
            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $Config.severityPolicy.missingFeatureBootstrap `
                -OwnerDoc $architectureOwnerDoc `
                -Title 'Missing feature bootstrap root' `
                -Message "Feature '$($feature.Name)' has no root-level *Setup.cs or *Bootstrap.cs file." `
                -Evidence @([pscustomobject]@{ path = $relative; line = $null; snippet = 'Expected root-level Setup/Bootstrap file' }) `
                -RemediationKind 'code_fix' `
                -Rationale 'The architecture contract requires a root setup/bootstrap artifact.'))
            [void]$docCandidates.Add($architectureOwnerDoc)
        }
    }

    foreach ($script in $scriptFiles) {
        $relative = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $script.FullName
        $lines = Get-Content -Path $script.FullName
        $layer = Get-RuleHarnessLayerFromRelativePath -RelativePath $relative
        $usingNamespaces = @(Get-RuleHarnessUsingNamespaces -Lines $lines)
        $contentWithoutComments = Get-RuleHarnessScriptTextWithoutComments -Lines $lines
        $codeWithoutCommentsOrStrings = Get-RuleHarnessCodeWithoutCommentsOrStrings -Content $contentWithoutComments
        $featureName = Get-RuleHarnessFeatureNameFromPath -RelativePath $relative

        foreach ($usingNamespace in @($usingNamespaces)) {
            $layerViolation = Get-RuleHarnessLayerViolationForUsing -CurrentLayer $layer -Namespace ([string]$usingNamespace)
            if ($null -eq $layerViolation) {
                continue
            }

            $evidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns @(
                ("^\s*using\s+(?:static\s+)?(?:\w+\s*=\s*)?{0}\s*;" -f [regex]::Escape([string]$usingNamespace))
            )
            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'code_violation' `
                -Severity 'high' `
                -OwnerDoc $architectureOwnerDoc `
                -Title 'Layer dependency violation' `
                -Message ("{0} file '{1}' imports forbidden namespace '{2}'." -f $layer, $relative, [string]$usingNamespace) `
                -Evidence @([pscustomobject]@{ path = $relative; line = if ($null -ne $evidence) { [int]$evidence.line } else { $null }; snippet = if ($null -ne $evidence) { [string]$evidence.snippet } else { [string]$usingNamespace } }) `
                -Confidence 'high' `
                -RemediationKind 'report_only' `
                -Rationale ("Compile-clean hazard: {0}." -f $layerViolation)))
            [void]$docCandidates.Add($architectureOwnerDoc)
            break
        }

        if ($relative -match '/Domain/') {
            $evidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns @(
                'using\s+UnityEngine\s*;',
                'using\s+Photon(?:\.[A-Za-z0-9_]+)*\s*;',
                '\bUnityEngine\.',
                '\bPhoton\.'
            )
            if ($null -ne $evidence) {
                [void]$findings.Add((New-RuleHarnessFinding `
                    -FindingType 'code_violation' `
                    -Severity $Config.severityPolicy.unityInDomain `
                    -OwnerDoc $architectureOwnerDoc `
                    -Title 'Framework API used in Domain' `
                    -Message "Domain layer file '$relative' references Unity or Photon APIs." `
                    -Evidence @([pscustomobject]@{ path = $relative; line = [int]$evidence.line; snippet = [string]$evidence.snippet }) `
                    -Confidence 'high' `
                    -RemediationKind 'code_fix' `
                    -Rationale 'The code is violating a stable architecture rule and should be refactored.'))
                [void]$docCandidates.Add($architectureOwnerDoc)
            }
        }

        if ($relative -match '/Application/') {
            $evidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns @(
                'using\s+UnityEngine\s*;',
                'using\s+Photon(?:\.[A-Za-z0-9_]+)*\s*;',
                '\bUnityEngine\.',
                '\bPhoton\.',
                '\bMonoBehaviour\b',
                '\bGameObject\b',
                '\bSprite\b',
                '\bAudioClip\b',
                '\bColor\b',
                '\bDebug\s*\.\s*Log(?:Warning|Error)?\b'
            )
            if ($null -ne $evidence) {
                [void]$findings.Add((New-RuleHarnessFinding `
                    -FindingType 'code_violation' `
                    -Severity $Config.severityPolicy.unityInApplication `
                    -OwnerDoc $architectureOwnerDoc `
                    -Title 'Unity API used in Application' `
                    -Message "Application layer file '$relative' appears to reference Unity or Photon API types." `
                    -Evidence @([pscustomobject]@{ path = $relative; line = [int]$evidence.line; snippet = [string]$evidence.snippet }) `
                    -Confidence 'high' `
                    -RemediationKind 'code_fix' `
                    -Rationale 'The code is violating a stable architecture rule and should be refactored.'))
                [void]$docCandidates.Add($architectureOwnerDoc)
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($featureName)) {
            $sameNameTypePath = Join-Path $RepoRoot ("Assets/Scripts/Features/{0}/Domain/{0}.cs" -f $featureName)
            if ((Test-Path -LiteralPath $sameNameTypePath) -and $relative -notlike "Assets/Scripts/Features/$featureName/Domain/$featureName.cs") {
                $shadowEvidence = Find-RuleHarnessShortTypeShadowingEvidence -Lines $lines -ShortTypeName $featureName
                if ($null -ne $shadowEvidence) {
                    [void]$findings.Add((New-RuleHarnessFinding `
                        -FindingType 'code_violation' `
                        -Severity 'high' `
                        -OwnerDoc $architectureOwnerDoc `
                        -Title 'Feature short-type shadowing' `
                        -Message ("File '{0}' uses bare short type '{1}' inside feature namespace 'Features.{1}'. Use an alias or fully-qualified name." -f $relative, $featureName) `
                        -Evidence @([pscustomobject]@{ path = $relative; line = [int]$shadowEvidence.line; snippet = [string]$shadowEvidence.snippet }) `
                        -Confidence 'high' `
                        -RemediationKind 'code_fix' `
                        -Rationale 'Compile-clean hazard: same-name feature namespaces and types must not rely on bare identifiers.'))
                    [void]$docCandidates.Add($architectureOwnerDoc)
                }
            }
        }

        $phantomContractEvidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns @('\bIEventBus\b')
        if ($null -ne $phantomContractEvidence) {
            $phantomReplacement = Get-RuleHarnessPhantomEventBusReplacement -CodeText $codeWithoutCommentsOrStrings
            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'code_violation' `
                -Severity 'high' `
                -OwnerDoc $architectureOwnerDoc `
                -Title 'Phantom shared contract name' `
                -Message ("File '{0}' references 'IEventBus', but Shared.EventBus does not define that contract. Use the real EventBus contracts instead." -f $relative) `
                -Evidence @([pscustomobject]@{ path = $relative; line = [int]$phantomContractEvidence.line; snippet = [string]$phantomContractEvidence.snippet }) `
                -Confidence 'high' `
                -RemediationKind $(if ([string]::IsNullOrWhiteSpace($phantomReplacement)) { 'report_only' } else { 'code_fix' }) `
                -Rationale 'Compile-clean hazard: phantom shared contract names drift away from the actual Shared declarations.'))
            [void]$docCandidates.Add($architectureOwnerDoc)
        }

        $knownImportChecks = @(
            [pscustomobject]@{
                symbolLabel = 'Func<>'
                pattern = '\bFunc\s*<'
                requiredNamespace = 'System'
                requiredUsing = 'using System;'
                fullyQualifiedPattern = '(?:global::)?System\.Func\s*<'
            },
            [pscustomobject]@{
                symbolLabel = 'GarageRoster'
                pattern = '(?<!\.)\bGarageRoster\b'
                requiredNamespace = 'Features.Garage.Domain'
                requiredUsing = 'using Features.Garage.Domain;'
                fullyQualifiedPattern = '(?:Features\.)?Garage\.Domain\.GarageRoster\b'
            },
            [pscustomobject]@{
                symbolLabel = 'StatusNetworkAdapter'
                pattern = '(?<!\.)\bStatusNetworkAdapter\b'
                requiredNamespace = 'Features.Status.Infrastructure'
                requiredUsing = 'using Features.Status.Infrastructure;'
                fullyQualifiedPattern = 'Features\.Status\.Infrastructure\.StatusNetworkAdapter\b'
            },
            [pscustomobject]@{
                symbolLabel = 'SceneLoaderAdapter'
                pattern = '(?<!\.)\bSceneLoaderAdapter\b'
                requiredNamespace = 'Features.Lobby.Infrastructure'
                requiredUsing = 'using Features.Lobby.Infrastructure;'
                fullyQualifiedPattern = 'Features\.Lobby\.Infrastructure\.SceneLoaderAdapter\b'
            },
            [pscustomobject]@{
                symbolLabel = 'Required'
                pattern = '\[(?:[^\]]*,\s*)*Required(?:Attribute)?(?:\s*,|\])'
                requiredNamespace = 'Shared.Attributes'
                requiredUsing = 'using Shared.Attributes;'
                fullyQualifiedPattern = 'Shared\.Attributes\.Required\b'
            }
        )
        foreach ($importCheck in $knownImportChecks) {
            if ($codeWithoutCommentsOrStrings -notmatch [string]$importCheck.pattern) {
                continue
            }
            if ($codeWithoutCommentsOrStrings -match [string]$importCheck.fullyQualifiedPattern) {
                continue
            }
            if ($codeWithoutCommentsOrStrings -match ("namespace\s+{0}\b" -f [regex]::Escape([string]$importCheck.requiredNamespace))) {
                continue
            }
            if (Test-RuleHarnessHasUsingNamespace -Lines $lines -Namespace ([string]$importCheck.requiredNamespace)) {
                continue
            }

            $importEvidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns @([string]$importCheck.pattern)
            if ($null -eq $importEvidence) {
                continue
            }

            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'code_violation' `
                -Severity 'high' `
                -OwnerDoc $architectureOwnerDoc `
                -Title 'Missing import after symbol move' `
                -Message ("File '{0}' uses '{1}' without importing '{2}'. Add '{3}' or use the fully-qualified name." -f $relative, [string]$importCheck.symbolLabel, [string]$importCheck.requiredNamespace, [string]$importCheck.requiredUsing) `
                -Evidence @([pscustomobject]@{ path = $relative; line = [int]$importEvidence.line; snippet = [string]$importEvidence.snippet }) `
                -Confidence 'high' `
                -RemediationKind 'code_fix' `
                -Rationale 'Compile-clean hazard: moved symbol imports must stay explicit after refactors.'))
            [void]$docCandidates.Add($architectureOwnerDoc)
        }

        $eventDriftRules = @(
            [pscustomobject]@{ eventToken = 'GameEndEvent'; staleMember = 'IsLocalPlayerDead'; replacement = $null },
            [pscustomobject]@{ eventToken = 'DamageAppliedEvent'; staleMember = 'RemainingHp'; replacement = 'RemainingHealth' }
        )
        foreach ($eventDriftRule in $eventDriftRules) {
            if ($codeWithoutCommentsOrStrings -notmatch ("\b{0}\b" -f [regex]::Escape([string]$eventDriftRule.eventToken))) {
                continue
            }
            if ($codeWithoutCommentsOrStrings -notmatch ("\b{0}\b" -f [regex]::Escape([string]$eventDriftRule.staleMember))) {
                continue
            }

            $driftEvidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns @(
                ("\b{0}\b" -f [regex]::Escape([string]$eventDriftRule.staleMember))
            )
            if ($null -eq $driftEvidence) {
                continue
            }

            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'code_violation' `
                -Severity 'high' `
                -OwnerDoc $architectureOwnerDoc `
                -Title 'Event contract drift' `
                -Message ("File '{0}' assumes stale event member '{1}' on '{2}'. Producer, bridge, and consumer contracts drifted out of sync." -f $relative, [string]$eventDriftRule.staleMember, [string]$eventDriftRule.eventToken) `
                -Evidence @([pscustomobject]@{ path = $relative; line = [int]$driftEvidence.line; snippet = [string]$driftEvidence.snippet }) `
                -Confidence 'high' `
                -RemediationKind $(if ([string]::IsNullOrWhiteSpace([string]$eventDriftRule.replacement)) { 'report_only' } else { 'code_fix' }) `
                -Rationale 'Compile-clean hazard: event payload changes must update producers, bridges, and consumers together.'))
            [void]$docCandidates.Add($architectureOwnerDoc)
        }

        if ($codeWithoutCommentsOrStrings -match '\bIUnitEnergyPort\b' -and $codeWithoutCommentsOrStrings -match '\bnew\s+EnergyAdapter\s*\(') {
            $energyBridgeEvidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns @('\bnew\s+EnergyAdapter\s*\(')
            if ($null -ne $energyBridgeEvidence) {
                [void]$findings.Add((New-RuleHarnessFinding `
                    -FindingType 'code_violation' `
                    -Severity 'high' `
                    -OwnerDoc $architectureOwnerDoc `
                    -Title 'Concrete/interface drift' `
                    -Message ("File '{0}' mixes IUnitEnergyPort with direct EnergyAdapter construction. Use the dedicated bridge adapter instead of the concrete Player adapter." -f $relative) `
                    -Evidence @([pscustomobject]@{ path = $relative; line = [int]$energyBridgeEvidence.line; snippet = [string]$energyBridgeEvidence.snippet }) `
                    -Confidence 'high' `
                    -RemediationKind 'report_only' `
                    -Rationale 'Compile-clean hazard: cross-feature adapters must satisfy the declared interface contract, not the concrete source type.'))
                [void]$docCandidates.Add($architectureOwnerDoc)
            }
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ScopeId)) {
        foreach ($doc in @($docCandidates | Sort-Object)) {
            $docFull = Join-Path $RepoRoot $doc
            if (-not (Test-Path -LiteralPath $docFull)) {
                [void]$findings.Add((New-RuleHarnessFinding `
                    -FindingType 'broken_reference' `
                    -Severity $Config.severityPolicy.missingSsotReference `
                    -OwnerDoc 'AGENTS.md' `
                    -Title 'Missing owner document' `
                    -Message "Current feature findings point at an owner doc that does not exist: $doc" `
                    -Evidence @([pscustomobject]@{ path = 'AGENTS.md'; line = $null; snippet = $doc }) `
                    -RemediationKind 'rule_fix' `
                    -Rationale 'The owner doc path referenced from the current SSOT entrypoint is missing.'))
                continue
            }

            foreach ($target in Get-RuleHarnessMarkdownTargets -Content (Get-Content -Path $docFull -Raw)) {
                $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath $doc -Target $target
                if ($null -eq $resolved) {
                    continue
                }

                if (-not $resolved.Exists) {
                    [void]$findings.Add((New-RuleHarnessFinding `
                        -FindingType 'broken_reference' `
                        -Severity $Config.severityPolicy.brokenReference `
                        -OwnerDoc $doc `
                        -Title 'Broken markdown reference' `
                        -Message "Document reference does not resolve to an existing file: $target" `
                        -Evidence @([pscustomobject]@{ path = $doc; line = $null; snippet = $target }) `
                        -Confidence 'high' `
                        -RemediationKind 'rule_fix' `
                        -Rationale 'The repository path is stale and should be synchronized in the owning document.'))
                }
            }
        }
    }

    @($findings)
}

function Get-RuleHarnessHardcodedMcpUiSmokeFiles {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $searchRoots = @(
        'tools',
        '.github/workflows'
    )
    $excludeRelativePaths = @(
        'tools/mcp-test-compile.ps1',
        'tools/mcp-diagnose-scene-hierarchy.ps1',
        'tools/mcp-hierarchy-diag.ps1',
        'tools/rule-harness/RuleHarness.psm1'
    )
    $excludePrefixes = @(
        'tools/unity-mcp/',
        'tools/rule-harness/tests/'
    )
    $allowedExtensions = @('.ps1', '.psm1', '.psd1', '.json', '.yml', '.yaml')

    $files = [System.Collections.Generic.List[object]]::new()
    foreach ($root in $searchRoots) {
        $fullRoot = Join-Path $RepoRoot $root
        if (-not (Test-Path -LiteralPath $fullRoot)) {
            continue
        }

        foreach ($file in @(Get-ChildItem -LiteralPath $fullRoot -Recurse -File)) {
            if ($allowedExtensions -notcontains [string]$file.Extension) {
                continue
            }

            $relative = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $file.FullName
            if ($excludeRelativePaths -contains $relative) {
                continue
            }

            $skip = $false
            foreach ($prefix in $excludePrefixes) {
                if ($relative.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
                    $skip = $true
                    break
                }
            }

            if (-not $skip) {
                [void]$files.Add([pscustomobject]@{
                    relativePath = $relative
                    fullPath = $file.FullName
                })
            }
        }
    }

    @($files)
}

function Get-RuleHarnessHardcodedMcpUiSmokeFindings {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $severity = if ($Config.severityPolicy.PSObject.Properties.Name -contains 'hardcodedMcpUiSmoke') {
        [string]$Config.severityPolicy.hardcodedMcpUiSmoke
    }
    else {
        'high'
    }

    $routeChecks = @(
        [pscustomobject]@{ label = '/ui/button/invoke'; patterns = @('/ui/button/invoke') },
        [pscustomobject]@{ label = '/input/click'; patterns = @('/input/click') },
        [pscustomobject]@{ label = '/input/drag'; patterns = @('/input/drag') },
        [pscustomobject]@{ label = '/input/key'; patterns = @('/input/key') },
        [pscustomobject]@{ label = '/input/text'; patterns = @('/input/text') },
        [pscustomobject]@{ label = 'Get-McpUiPath'; patterns = @('\bGet-McpUiPath\b') },
        [pscustomobject]@{ label = 'Invoke-McpButton'; patterns = @('\bInvoke-McpButton\b') }
    )
    $sceneLiteralPatterns = @(
        'Assets/Scenes/LobbyScene\.unity',
        'Assets/Scenes/GameScene\.unity',
        '(?<![A-Za-z0-9_])LobbyScene(?![A-Za-z0-9_])',
        '(?<![A-Za-z0-9_])GameScene(?![A-Za-z0-9_])',
        '/UIRoot',
        '/Canvas/.+Button'
    )
    $smokeContextPatterns = @(
        '/scene/open',
        '/play/start',
        'Wait-McpSceneActive',
        'Wait-McpPlayModeReady',
        'Invoke-McpSceneOpenAndWait',
        'Invoke-McpPlayStartAndWaitForBridge'
    )

    $findings = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @(Get-RuleHarnessHardcodedMcpUiSmokeFiles -RepoRoot $RepoRoot)) {
        $lines = Get-Content -Path $file.fullPath
        $content = Get-Content -Path $file.fullPath -Raw
        $matchedLabel = $null
        $evidence = $null

        foreach ($check in $routeChecks) {
            $evidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns @($check.patterns)
            if ($null -ne $evidence) {
                $matchedLabel = [string]$check.label
                break
            }
        }

        if ($null -eq $evidence) {
            $hasSmokeContext = $false
            foreach ($contextPattern in $smokeContextPatterns) {
                if ($content -match $contextPattern) {
                    $hasSmokeContext = $true
                    break
                }
            }

            if ($hasSmokeContext) {
                $evidence = Find-RuleHarnessPatternEvidence -Lines $lines -Patterns $sceneLiteralPatterns
                if ($null -ne $evidence) {
                    $matchedLabel = 'scene/UI flow literal'
                }
            }
        }

        if ($null -eq $evidence) {
            continue
        }

        [void]$findings.Add((New-RuleHarnessFinding `
            -FindingType 'code_violation' `
            -Severity $severity `
            -OwnerDoc 'tools/rule-harness/README.md' `
            -Title 'Hardcoded MCP UI smoke reintroduced' `
            -Message ("Automation file '{0}' reintroduces hardcoded MCP UI smoke via '{1}'. Keep rule-harness on generic compile/status checks and move runtime verification to docs/playtest manual checklists or one-off diagnostics." -f [string]$file.relativePath, [string]$matchedLabel) `
            -Evidence @([pscustomobject]@{ path = [string]$file.relativePath; line = [int]$evidence.line; snippet = [string]$evidence.snippet }) `
            -Confidence 'high' `
            -RemediationKind 'report_only' `
            -Rationale 'Automatic scene/UI flow smoke scripts are out of policy for the harness and should not re-enter automation.'))
    }

    @($findings)
}

function Invoke-RuleHarnessChatCompletion {
    param(
        [Parameter(Mandatory)]
        [string]$Model,
        [Parameter(Mandatory)]
        [string]$ApiBaseUrl,
        [Parameter(Mandatory)]
        [string]$SystemPrompt,
        [Parameter(Mandatory)]
        [string]$UserPrompt,
        [Parameter(Mandatory)]
        [string]$ApiKey,
        [int]$TimeoutSec = 120
    )

    $body = @{
        model           = $Model
        temperature     = 0
        response_format = @{ type = 'json_object' }
        messages        = @(
            @{ role = 'system'; content = $SystemPrompt },
            @{ role = 'user'; content = $UserPrompt }
        )
    }

    $endpoint = '{0}/chat/completions' -f $ApiBaseUrl.TrimEnd('/')
    Write-Host "Rule harness chat completion request started. Model: $Model TimeoutSec: $TimeoutSec Endpoint: $endpoint"
    try {
        $response = Invoke-RestMethod `
            -Uri $endpoint `
            -Method Post `
            -Headers @{
                Authorization = "Bearer $ApiKey"
                'Content-Type' = 'application/json'
            } `
            -Body ($body | ConvertTo-Json -Depth 50) `
            -TimeoutSec $TimeoutSec
    }
    catch {
        $responseBody = $null
        if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                $reader.Close()
            }
            catch {
                $responseBody = $null
            }
        }

        if (-not [string]::IsNullOrWhiteSpace($responseBody)) {
            throw "HTTP request failed. Status=$($_.Exception.Response.StatusCode.value__) Body=$responseBody"
        }

        throw
    }
    Write-Host "Rule harness chat completion request finished. Model: $Model"

    $response.choices[0].message.content
}

function ConvertTo-RuleHarnessReviewedFindings {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Findings
    )

    if ($null -eq $Findings -or @($Findings).Count -eq 0) {
        return @()
    }

    $normalized = [System.Collections.Generic.List[object]]::new()
    foreach ($finding in $Findings) {
        $kind = if ($finding.PSObject.Properties.Name -contains 'remediationKind' -and -not [string]::IsNullOrWhiteSpace([string]$finding.remediationKind)) {
            [string]$finding.remediationKind
        }
        elseif ($finding.findingType -eq 'code_violation') {
            'code_fix'
        }
        elseif ($finding.findingType -in @('broken_reference', 'doc_drift')) {
            'rule_fix'
        }
        elseif ($finding.title -eq 'Missing feature bootstrap root') {
            'code_fix'
        }
        elseif ($finding.findingType -eq 'missing_rule') {
            'rule_fix'
        }
        else {
            'report_only'
        }

        $normalizedKind = if ($kind -in @('code_fix', 'rule_fix', 'mixed_fix', 'report_only')) {
            $kind
        }
        else {
            'report_only'
        }

        $normalizedFinding = [ordered]@{}
        foreach ($property in $finding.PSObject.Properties) {
            $normalizedFinding[$property.Name] = $property.Value
        }

        $normalizedFinding['remediationKind'] = $normalizedKind
        if (-not $normalizedFinding.Contains('rationale')) {
            $normalizedFinding['rationale'] = ''
        }
        if (-not $normalizedFinding.Contains('source')) {
            $normalizedFinding['source'] = 'agent_review'
        }

        [void]$normalized.Add([pscustomobject]$normalizedFinding)
    }

    @($normalized)
}

function ConvertTo-RuleHarnessSerializableFinding {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    [pscustomobject]@{
        findingType     = [string]$Finding.findingType
        severity        = [string]$Finding.severity
        ownerDoc        = [string]$Finding.ownerDoc
        title           = [string]$Finding.title
        message         = [string]$Finding.message
        confidence      = [string]$Finding.confidence
        source          = [string]$Finding.source
        remediationKind = [string]$Finding.remediationKind
        rationale       = [string]$Finding.rationale
        evidence        = @($Finding.evidence | ForEach-Object {
            [pscustomobject]@{
                path    = [string]$_.path
                line    = if ($null -eq $_.line) { $null } else { [int]$_.line }
                snippet = [string]$_.snippet
            }
        })
        proposedDocEdit = $null
    }
}

function Get-RuleHarnessMutationState {
    param(
        [Parameter(Mandatory)]
        [object]$Config,
        [string]$MutationMode,
        [switch]$EnableMutation,
        [switch]$DisableMutation
    )

    $configMode = if ($Config.mutation.PSObject.Properties.Name -contains 'mode') {
        [string]$Config.mutation.mode
    }
    else {
        'report_only'
    }

    $mode = if ([string]::IsNullOrWhiteSpace($MutationMode)) { $configMode } else { $MutationMode }
    if ($mode -notin @('report_only', 'doc_only', 'code_and_rules')) {
        $mode = 'report_only'
    }

    $enabled = if ($DisableMutation) {
        $false
    }
    elseif ($EnableMutation) {
        $true
    }
    elseif ($Config.mutation.PSObject.Properties.Name -contains 'enabled') {
        [bool]$Config.mutation.enabled
    }
    else {
        $false
    }

    if (-not $enabled) {
        $mode = 'report_only'
    }

    [pscustomobject]@{
        enabled = $enabled
        mode    = $mode
    }
}

function Invoke-RuleHarnessAgentReview {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$StaticFindings,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [string]$ApiKey,
        [string]$ApiBaseUrl,
        [string]$Model,
        [int]$TimeoutSec = 120,
        [string]$ReviewJsonPath
    )

    if (@($StaticFindings).Count -eq 0) {
        return @()
    }

    if ($ReviewJsonPath) {
        return @((ConvertTo-RuleHarnessReviewedFindings -Findings @((Get-Content -Path $ReviewJsonPath -Raw | ConvertFrom-Json).findings)))
    }

    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        return @(ConvertTo-RuleHarnessReviewedFindings -Findings $StaticFindings)
    }

    $systemPrompt = Get-Content -Path (Join-Path $RepoRoot 'tools/rule-harness/prompts/review.md') -Raw
    $payload = @{
        repository     = Split-Path -Leaf $RepoRoot
        staticFindings = @($StaticFindings | Select-Object -First $Config.llm.maxFindingsForReview)
    } | ConvertTo-Json -Depth 50

    Write-Host "Rule harness diagnose stage started. Findings sent: $(@($StaticFindings | Select-Object -First $Config.llm.maxFindingsForReview).Count)"
    $raw = Invoke-RuleHarnessChatCompletion -Model $Model -ApiBaseUrl $ApiBaseUrl -SystemPrompt $systemPrompt -UserPrompt $payload -ApiKey $ApiKey -TimeoutSec $TimeoutSec
    Write-Host 'Rule harness diagnose stage finished.'
    @(ConvertTo-RuleHarnessReviewedFindings -Findings @((($raw | ConvertFrom-Json).findings)))
}

function Invoke-RuleHarnessDocSync {
    param(
        [Parameter(Mandatory)]
        [object[]]$Findings,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [string]$ApiKey,
        [Parameter(Mandatory)]
        [string]$ApiBaseUrl,
        [Parameter(Mandatory)]
        [string]$Model,
        [int]$TimeoutSec = 120
    )

    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        return @()
    }

    $systemPrompt = Get-Content -Path (Join-Path $RepoRoot 'tools/rule-harness/prompts/doc-sync.md') -Raw
    $edits = [System.Collections.Generic.List[object]]::new()
    $docGroups = @($Findings | Group-Object ownerDoc)
    $maxTargetDocChars = if ($Config.llm.PSObject.Properties.Name -contains 'maxTargetDocCharsForSync') {
        [int]$Config.llm.maxTargetDocCharsForSync
    }
    else {
        20000
    }

    Write-Host "Rule harness doc sync stage started. Owner docs: $($docGroups.Count)"
    foreach ($group in $docGroups) {
        $docStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $fullPath = Join-Path $RepoRoot $group.Name
        if (-not (Test-Path -LiteralPath $fullPath)) {
            Write-Host "Rule harness doc sync skipped missing target: $($group.Name)"
            continue
        }

        Write-Host "Rule harness doc sync preparing payload for $($group.Name). Findings: $($group.Count)"
        $currentText = Get-Content -Path $fullPath -Raw
        if ($currentText.Length -gt $maxTargetDocChars) {
            Write-Host "Rule harness doc sync skipped target due to size. Path: $($group.Name) TextChars: $($currentText.Length) Limit: $maxTargetDocChars"
            continue
        }
        Write-Host "Rule harness doc sync loaded target for $($group.Name). TextChars: $($currentText.Length)"
        $serializableFindings = @($group.Group | ForEach-Object { ConvertTo-RuleHarnessSerializableFinding -Finding $_ })
        $targetPathJson = ConvertTo-Json -InputObject ([string]$group.Name) -Compress
        $currentTextJson = ConvertTo-Json -InputObject ([string]$currentText) -Compress
        $findingsJson = ConvertTo-Json -InputObject $serializableFindings -Depth 20 -Compress
        $payload = ('{{"targetPath":{0},"currentText":{1},"findings":{2}}}' -f $targetPathJson, $currentTextJson, $findingsJson)
        Write-Host "Rule harness doc sync payload prepared for $($group.Name). PayloadChars: $($payload.Length) PrepMs: $($docStopwatch.ElapsedMilliseconds)"

        Write-Host "Rule harness doc sync request started for $($group.Name). Findings: $($group.Count)"
        $raw = Invoke-RuleHarnessChatCompletion -Model $Model -ApiBaseUrl $ApiBaseUrl -SystemPrompt $systemPrompt -UserPrompt $payload -ApiKey $ApiKey -TimeoutSec $TimeoutSec
        $parsed = $raw | ConvertFrom-Json
        $parsedEdits = @($parsed.edits)
        Write-Host "Rule harness doc sync request finished for $($group.Name). ProposedEdits: $($parsedEdits.Count) ElapsedMs: $($docStopwatch.ElapsedMilliseconds)"
        foreach ($edit in $parsedEdits) {
            [void]$edits.Add($edit)
        }
        $docStopwatch.Stop()
    }
    Write-Host "Rule harness doc sync stage finished. Proposed edits: $($edits.Count)"

    @($edits)
}

function Test-RuleHarnessDocAllowed {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$RelativePath,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $normalized = $RelativePath.Replace('\', '/')
    if (Test-RuleHarnessWildcardMatch -Path $normalized -Patterns $Config.docs.denylist) {
        return $false
    }

    if (Test-RuleHarnessWildcardMatch -Path $normalized -Patterns $Config.docs.allowlist) {
        return $true
    }

    $normalized -in @(Get-RuleHarnessClaudeReferencedDocs -RepoRoot $RepoRoot)
}

function Invoke-RuleHarnessDocEdits {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Edits,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [switch]$DryRun
    )

    $results = [System.Collections.Generic.List[object]]::new()
    $touched = $false

    if ($Edits.Count -eq 0) {
        return [pscustomobject]@{
            touched = $false
            edits   = @()
        }
    }

    foreach ($edit in $Edits) {
        $targetPath = [string]$edit.targetPath
        if (-not (Test-RuleHarnessDocAllowed -RepoRoot $RepoRoot -RelativePath $targetPath -Config $Config)) {
            [void]$results.Add([pscustomobject]@{
                targetPath = $targetPath
                status     = 'rejected'
                reason     = 'Target path is outside the doc allowlist.'
            })
            continue
        }

        $fullPath = Join-Path $RepoRoot $targetPath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            [void]$results.Add([pscustomobject]@{
                targetPath = $targetPath
                status     = 'failed'
                reason     = 'Target file does not exist.'
            })
            continue
        }

        $currentText = Get-Content -Path $fullPath -Raw
        $searchText = [string]$edit.searchText
        $replaceText = [string]$edit.replaceText
        if ([string]::IsNullOrWhiteSpace($searchText) -or -not $currentText.Contains($searchText)) {
            [void]$results.Add([pscustomobject]@{
                targetPath = $targetPath
                status     = 'failed'
                reason     = 'searchText was not found in target file.'
            })
            continue
        }

        $updatedText = $currentText.Replace($searchText, $replaceText)
        $broken = [System.Collections.Generic.List[string]]::new()
        foreach ($target in Get-RuleHarnessMarkdownTargets -Content $updatedText) {
            $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath $targetPath -Target $target
            if ($null -ne $resolved -and -not $resolved.Exists) {
                [void]$broken.Add($target)
            }
        }

        if ($broken.Count -gt 0) {
            [void]$results.Add([pscustomobject]@{
                targetPath = $targetPath
                status     = 'failed'
                reason     = "Patch would leave broken references: $($broken -join ', ')"
            })
            continue
        }

        $status = if ($DryRun) { 'proposed' } else { 'applied' }
        if (-not $DryRun) {
            Set-Content -Path $fullPath -Value $updatedText -Encoding UTF8
            $touched = $true
        }

        [void]$results.Add([pscustomobject]@{
            targetPath = $targetPath
            status     = $status
            reason     = [string]$edit.reason
        })
    }

    [pscustomobject]@{
        touched = $touched
        edits   = @($results)
    }
}

function Get-RuleHarnessBootstrapSetupContent {
    param(
        [Parameter(Mandatory)]
        [string]$FeatureName
    )

@"
namespace Features.$FeatureName
{
    /// <summary>
    /// Rule-harness generated composition root placeholder.
    /// Replace this scaffold with the real feature setup when wiring the feature.
    /// </summary>
    public sealed class ${FeatureName}Setup
    {
    }
}
"@
}

function Get-RuleHarnessRelatedFeatureNamesForFinding {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    $featureNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($evidence in @($Finding.evidence)) {
        $evidencePath = [string]$evidence.path
        if ($evidencePath -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
            [void]$featureNames.Add([string]$Matches['feature'])
        }
    }

    @($featureNames | Sort-Object)
}

function Test-RuleHarnessSupportsUsingDirectiveFix {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    $title = [string]$Finding.title
    $message = [string]$Finding.message
    if ($title -match 'Unity API used in Application' -or $title -match 'Framework API used in Domain') {
        return $true
    }

    if ($message -match 'Application layer file' -and $message -match 'Unity|Photon') {
        return $true
    }

    if ($message -match 'Domain layer file' -and $message -match 'Unity|Photon') {
        return $true
    }

    foreach ($evidence in @($Finding.evidence)) {
        $snippet = [string]$evidence.snippet
        if ($snippet -match '^\s*using\s+UnityEngine\s*;' -or $snippet -match '^\s*using\s+Photon(?:\.[A-Za-z0-9_]+)*\s*;') {
            return $true
        }
    }

    return $false
}

function Test-RuleHarnessSupportsMathfScalarFix {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    $title = [string]$Finding.title
    $message = [string]$Finding.message
    if (-not ($title -match 'Unity API used in Application' -or $title -match 'Framework API used in Domain')) {
        if (-not (($message -match 'Application layer file' -or $message -match 'Domain layer file') -and $message -match 'Unity|Photon')) {
            return $false
        }
    }

    foreach ($evidence in @($Finding.evidence)) {
        $snippet = [string]$evidence.snippet
        if ($snippet -match 'UnityEngine\.Mathf\.(Max|Min)\s*\(') {
            return $true
        }
    }

    return $false
}

function Get-RuleHarnessMathfScalarFixContent {
    param(
        [Parameter(Mandatory)]
        [string]$Content
    )

    $pattern = 'UnityEngine\.Mathf\.(?<method>Max|Min)\(\s*(?<left>[^(),\r\n]+(?:\s*[-+*/%]\s*[^(),\r\n]+)*)\s*,\s*(?<right>[^()\r\n]+?)\s*\)'
    [regex]::Replace($Content, $pattern, {
        param($match)

        $method = [string]$match.Groups['method'].Value
        $left = [string]$match.Groups['left'].Value
        $right = [string]$match.Groups['right'].Value
        if ([string]::IsNullOrWhiteSpace($method) -or [string]::IsNullOrWhiteSpace($left) -or [string]::IsNullOrWhiteSpace($right)) {
            return $match.Value
        }

        "System.Math.$method($left, $right)"
    })
}

function Get-RuleHarnessFeatureNameFromPath {
    param(
        [string]$RelativePath
    )

    if ([string]::IsNullOrWhiteSpace($RelativePath)) {
        return $null
    }

    if ($RelativePath.Replace('\', '/') -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
        return [string]$Matches['feature']
    }

    return $null
}

function Get-RuleHarnessPrimaryCodeEvidencePath {
    param(
        [Parameter(Mandatory)]
        [object]$Finding
    )

    foreach ($evidence in @($Finding.evidence)) {
        $path = [string]$evidence.path
        if (-not [string]::IsNullOrWhiteSpace($path) -and $path.EndsWith('.cs')) {
            return $path
        }
    }

    return $null
}

function Get-RuleHarnessPhantomEventBusReplacement {
    param(
        [Parameter(Mandatory)]
        [string]$CodeText
    )

    if ($CodeText -notmatch '\bIEventBus\b') {
        return $null
    }

    $hasPublishUsage = $CodeText -match '\.\s*Publish\s*(?:<|\()' -or $CodeText -match '\bPublish\s*(?:<|\()'
    $hasSubscribeUsage = $CodeText -match '\.\s*Subscribe\s*(?:<|\()' -or
        $CodeText -match '\bSubscribe\s*(?:<|\()' -or
        $CodeText -match '\.\s*UnsubscribeAll\s*\(' -or
        $CodeText -match '\bUnsubscribeAll\s*\('

    if ($hasPublishUsage -and -not $hasSubscribeUsage) {
        return 'IEventPublisher'
    }

    if ($hasSubscribeUsage -and -not $hasPublishUsage) {
        return 'IEventSubscriber'
    }

    return $null
}

function Add-RuleHarnessUsingDirective {
    param(
        [Parameter(Mandatory)]
        [string]$Content,
        [Parameter(Mandatory)]
        [string]$UsingDirective
    )

    if ([string]::IsNullOrWhiteSpace($UsingDirective)) {
        return $Content
    }

    if ($Content -match ("(?m)^[ \t]*{0}\s*$" -f [regex]::Escape($UsingDirective))) {
        return $Content
    }

    $newline = if ($Content -match "`r`n") { "`r`n" } else { "`n" }
    $usingMatches = [regex]::Matches($Content, '(?m)^[ \t]*using\s+[^\r\n;]+;\s*(?:\r?\n|$)')
    if ($usingMatches.Count -gt 0) {
        $lastUsing = $usingMatches[$usingMatches.Count - 1]
        return $Content.Insert($lastUsing.Index + $lastUsing.Length, $UsingDirective + $newline)
    }

    $namespaceMatch = [regex]::Match($Content, '(?m)^[ \t]*namespace\b')
    if ($namespaceMatch.Success) {
        return $Content.Insert($namespaceMatch.Index, $UsingDirective + $newline + $newline)
    }

    return $UsingDirective + $newline + $newline + $Content
}

function Get-RuleHarnessPhantomEventBusFixContent {
    param(
        [Parameter(Mandatory)]
        [string]$Content
    )

    $codeWithoutCommentsOrStrings = Get-RuleHarnessCodeWithoutCommentsOrStrings -Content $Content
    $replacement = Get-RuleHarnessPhantomEventBusReplacement -CodeText $codeWithoutCommentsOrStrings
    if ([string]::IsNullOrWhiteSpace($replacement)) {
        return $Content
    }

    if ($Content -notmatch '\bIEventBus\b') {
        return $Content
    }

    return [regex]::Replace($Content, '\bIEventBus\b', $replacement)
}

function Get-RuleHarnessMissingImportFixContent {
    param(
        [Parameter(Mandatory)]
        [object[]]$Findings,
        [Parameter(Mandatory)]
        [string]$Content
    )

    $requiredUsings = [System.Collections.Generic.List[string]]::new()
    foreach ($finding in @($Findings)) {
        if ([string]$finding.title -ne 'Missing import after symbol move') {
            continue
        }

        if ([string]$finding.message -match "Add '(?<using>using [^']+;)'") {
            $usingDirective = [string]$Matches['using']
            if ($usingDirective -notin @($requiredUsings)) {
                [void]$requiredUsings.Add($usingDirective)
            }
        }
    }

    $updatedContent = $Content
    foreach ($usingDirective in @($requiredUsings)) {
        $updatedContent = Add-RuleHarnessUsingDirective -Content $updatedContent -UsingDirective $usingDirective
    }

    return $updatedContent
}

function Get-RuleHarnessEventContractDriftFixContent {
    param(
        [Parameter(Mandatory)]
        [object[]]$Findings,
        [Parameter(Mandatory)]
        [string]$Content
    )

    $updatedContent = $Content
    foreach ($finding in @($Findings)) {
        if ([string]$finding.title -ne 'Event contract drift') {
            continue
        }

        if ([string]$finding.message -match "stale event member '(?<member>[^']+)' on '(?<event>[^']+)'") {
            $staleMember = [string]$Matches['member']
            $eventToken = [string]$Matches['event']
            if ($eventToken -eq 'DamageAppliedEvent' -and $staleMember -eq 'RemainingHp') {
                $updatedContent = [regex]::Replace($updatedContent, '(?<=\.)RemainingHp\b', 'RemainingHealth')
            }
        }
    }

    return $updatedContent
}

function Get-RuleHarnessShortTypeShadowingFixContent {
    param(
        [Parameter(Mandatory)]
        [string]$TargetPath,
        [Parameter(Mandatory)]
        [object[]]$Findings,
        [Parameter(Mandatory)]
        [string]$Content
    )

    if (@($Findings | Where-Object { [string]$_.title -eq 'Feature short-type shadowing' }).Count -eq 0) {
        return $Content
    }

    $featureName = Get-RuleHarnessFeatureNameFromPath -RelativePath $TargetPath
    if ([string]::IsNullOrWhiteSpace($featureName)) {
        return $Content
    }

    $fullyQualifiedType = "global::Features.$featureName.Domain.$featureName"
    $typeTokenPattern = "(?<![\w\.]){0}(?![\w])" -f [regex]::Escape($featureName)
    $declarationPattern = "^\s*(?:\[[^\]]+\]\s*)*(?:(?:public|internal|protected|private)\s+)?(?:(?:sealed|abstract|static|partial)\s+)*(?:class|struct|interface|enum|record)\s+{0}\b" -f [regex]::Escape($featureName)
    $lineMatches = [regex]::Matches($Content, '.*(?:\r?\n|$)')
    $builder = [System.Text.StringBuilder]::new()
    $replacedAny = $false

    foreach ($lineMatch in @($lineMatches)) {
        $line = [string]$lineMatch.Value
        if ($line.Length -eq 0) {
            continue
        }

        $trimmed = $line.TrimStart()
        if ($trimmed -match '^using\s+' -or
            $trimmed -match '^namespace\s+' -or
            $trimmed -match '^//' -or
            $trimmed -match '^(?s)/\*' -or
            $trimmed -match $declarationPattern) {
            [void]$builder.Append($line)
            continue
        }

        $updatedLine = [regex]::Replace($line, $typeTokenPattern, $fullyQualifiedType)
        if ($updatedLine -ne $line) {
            $replacedAny = $true
        }
        [void]$builder.Append($updatedLine)
    }

    if (-not $replacedAny) {
        return $Content
    }

    return $builder.ToString()
}

function Get-RuleHarnessTimeProviderCodeFixOperations {
    param(
        [Parameter(Mandatory)]
        [object]$Finding,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$TargetPath,
        [Parameter(Mandatory)]
        [string]$Content
    )

    if ($TargetPath.Replace('\', '/') -notmatch '/Application/' -or $Content -notmatch 'UnityEngine\.Time\.[A-Za-z_][A-Za-z0-9_]*') {
        return @()
    }

    $timeExpressions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($match in @([regex]::Matches($Content, 'UnityEngine\.Time\.(?<member>[A-Za-z_][A-Za-z0-9_]*)'))) {
        [void]$timeExpressions.Add([string]$match.Value)
    }
    $distinctTimeExpressions = @($timeExpressions)
    if ($distinctTimeExpressions.Count -ne 1) {
        return @()
    }
    $timeExpression = [string]$distinctTimeExpressions[0]

    $featureName = Get-RuleHarnessFeatureNameFromPath -RelativePath $TargetPath
    if ([string]::IsNullOrWhiteSpace($featureName)) {
        return @()
    }

    $className = [System.IO.Path]::GetFileNameWithoutExtension($TargetPath)
    if ([string]::IsNullOrWhiteSpace($className)) {
        return @()
    }

    $constructorPattern = "(?ms)^(?<indent>[ \t]*)(?<access>(?:public|internal|protected|private)\s+)$([regex]::Escape($className))\s*\((?<params>.*?)\)\s*\r?\n(?<braceIndent>[ \t]*)\{"
    $constructorMatches = [regex]::Matches($Content, $constructorPattern)
    if ($constructorMatches.Count -ne 1) {
        return @()
    }

    $constructorMatch = $constructorMatches[0]
    $paramsText = [string]$constructorMatch.Groups['params'].Value
    if ($paramsText -match 'Func\s*<\s*float\s*>\s+timeProvider' -or $paramsText -match 'timeProvider') {
        return @()
    }

    $paramText = 'global::System.Func<float> timeProvider'
    $newParamsText = $paramText
    if (-not [string]::IsNullOrWhiteSpace($paramsText)) {
        if ($paramsText.Contains("`n")) {
            $paramIndentMatch = [regex]::Matches($paramsText, '(?m)^(?<indent>[ \t]*)\S') | Select-Object -First 1
            $paramIndent = if ($null -ne $paramIndentMatch) { [string]$paramIndentMatch.Groups['indent'].Value } else { ([string]$constructorMatch.Groups['indent'].Value + '    ') }
            $trimmedParams = $paramsText.TrimEnd()
            $newParamsText = "$trimmedParams,`r`n$paramIndent$paramText"
        }
        else {
            $newParamsText = "$paramsText, $paramText"
        }
    }

    $updatedContent = $Content.Substring(0, $constructorMatch.Groups['params'].Index) + $newParamsText + $Content.Substring($constructorMatch.Groups['params'].Index + $constructorMatch.Groups['params'].Length)
    $fieldDeclaration = 'private readonly global::System.Func<float> _timeProvider;'
    if ($updatedContent -notmatch [regex]::Escape($fieldDeclaration)) {
        $fieldMatches = [regex]::Matches($updatedContent, '(?m)^[ \t]*private readonly .+;\r?$')
        if ($fieldMatches.Count -gt 0) {
            $lastField = $fieldMatches[$fieldMatches.Count - 1]
            $insertAt = $lastField.Index + $lastField.Length
            $newline = if ($updatedContent.Substring([Math]::Max(0, $insertAt - 1), [Math]::Min(1, $updatedContent.Length - [Math]::Max(0, $insertAt - 1))) -eq "`n") { '' } else { "`r`n" }
            $updatedContent = $updatedContent.Insert($insertAt, "$newline$([string]$constructorMatch.Groups['indent'].Value)$fieldDeclaration")
        }
        else {
            $classBraceMatch = [regex]::Match($updatedContent, '(?ms)\bclass\b[^{]+\{')
            if (-not $classBraceMatch.Success) {
                return @()
            }

            $insertAt = $classBraceMatch.Index + $classBraceMatch.Length
            $updatedContent = $updatedContent.Insert($insertAt, "`r`n$([string]$constructorMatch.Groups['indent'].Value)    $fieldDeclaration")
        }
    }

    if ($updatedContent -notmatch '_timeProvider\s*=') {
        $assignmentText = "$([string]$constructorMatch.Groups['indent'].Value)    _timeProvider = timeProvider ?? throw new global::System.ArgumentNullException(nameof(timeProvider));`r`n"
        $constructorStartPattern = "(?ms)^(?<indent>[ \t]*)(?<access>(?:public|internal|protected|private)\s+)$([regex]::Escape($className))\s*\((?<params>.*?)\)\s*\r?\n(?<braceIndent>[ \t]*)\{"
        $updatedContent = [regex]::Replace(
            $updatedContent,
            $constructorStartPattern,
            {
                param($match)
                $match.Value + "`r`n" + $assignmentText
            },
            1)
    }

    $updatedContent = $updatedContent.Replace($timeExpression, '_timeProvider()')
    if ($updatedContent -eq $Content) {
        return @()
    }

    $featureRoot = Join-Path $RepoRoot ("Assets/Scripts/Features/{0}" -f $featureName)
    $callSiteFiles = @()
    if (Test-Path -LiteralPath $featureRoot) {
        $callSiteFiles = @(
            Get-ChildItem -LiteralPath $featureRoot -Recurse -File -Filter *.cs |
                Where-Object {
                    $relative = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $_.FullName
                    $relative -ne $TargetPath -and ($_.Name -like '*Setup.cs' -or $_.Name -like '*Bootstrap.cs')
                }
        )
    }

    $callSiteMatches = [System.Collections.Generic.List[object]]::new()
    foreach ($file in @($callSiteFiles)) {
        $callSiteRelativePath = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $file.FullName
        $callSiteContent = Get-Content -Path $file.FullName -Raw
        $matches = [regex]::Matches($callSiteContent, "new\s+$([regex]::Escape($className))\s*\((?<args>.*?)\)")
        foreach ($match in @($matches)) {
            [void]$callSiteMatches.Add([pscustomobject]@{
                path    = $callSiteRelativePath
                content = $callSiteContent
                match   = $match
            })
        }
    }

    if ($callSiteMatches.Count -ne 1) {
        return @()
    }

    $callSiteMatch = $callSiteMatches[0]
    $callArgs = [string]$callSiteMatch.match.Groups['args'].Value
    if ($callArgs -match 'UnityEngine\.Time\.[A-Za-z_][A-Za-z0-9_]*' -or $callArgs -match 'timeProvider') {
        return @()
    }

    $newCallArgs = if ([string]::IsNullOrWhiteSpace($callArgs)) { "() => $timeExpression" } else { "$callArgs, () => $timeExpression" }
    $updatedCallSiteContent = $callSiteMatch.content.Substring(0, $callSiteMatch.match.Groups['args'].Index) + $newCallArgs + $callSiteMatch.content.Substring($callSiteMatch.match.Groups['args'].Index + $callSiteMatch.match.Groups['args'].Length)
    if ($updatedCallSiteContent -eq $callSiteMatch.content) {
        return @()
    }

    @(
        [pscustomobject]@{
            type       = 'write_file'
            targetPath = $TargetPath
            content    = $updatedContent
        },
        [pscustomobject]@{
            type       = 'write_file'
            targetPath = [string]$callSiteMatch.path
            content    = $updatedCallSiteContent
        }
    )
}

function Get-RuleHarnessGroupedExistingCodeFixOperations {
    param(
        [Parameter(Mandatory)]
        [object[]]$Findings,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$TargetPath
    )

    $fullPath = Join-Path $RepoRoot $TargetPath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        return @()
    }

    $content = Get-Content -Path $fullPath -Raw
    $updatedContent = Get-RuleHarnessPhantomEventBusFixContent -Content $content
    $updatedContent = Get-RuleHarnessMissingImportFixContent -Findings $Findings -Content $updatedContent
    $updatedContent = Get-RuleHarnessEventContractDriftFixContent -Findings $Findings -Content $updatedContent
    $updatedContent = Get-RuleHarnessShortTypeShadowingFixContent -TargetPath $TargetPath -Findings $Findings -Content $updatedContent
    if ($updatedContent -eq $content) {
        return @()
    }

    @(
        [pscustomobject]@{
            type       = 'write_file'
            targetPath = $TargetPath
            content    = $updatedContent
        }
    )
}

function Get-RuleHarnessExistingCodeFixOperations {
    param(
        [Parameter(Mandatory)]
        [object]$Finding,
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $operations = [System.Collections.Generic.List[object]]::new()
    foreach ($evidence in @($Finding.evidence)) {
        $targetPath = [string]$evidence.path
        if ([string]::IsNullOrWhiteSpace($targetPath) -or -not $targetPath.EndsWith('.cs')) {
            continue
        }

        $fullPath = Join-Path $RepoRoot $targetPath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            continue
        }

        $content = Get-Content -Path $fullPath -Raw
        $updatedContent = $content
        if (Test-RuleHarnessSupportsUsingDirectiveFix -Finding $Finding) {
            $updatedContent = [regex]::Replace($updatedContent, '^[ \t]*using\s+UnityEngine\s*;\r?\n', '', 'Multiline')
            $updatedContent = [regex]::Replace($updatedContent, '^[ \t]*using\s+Photon(?:\.[A-Za-z0-9_]+)*\s*;\r?\n', '', 'Multiline')
        }
        if (Test-RuleHarnessSupportsMathfScalarFix -Finding $Finding) {
            $updatedContent = Get-RuleHarnessMathfScalarFixContent -Content $updatedContent
        }

        if ($updatedContent -ne $content) {
            [void]$operations.Add([pscustomobject]@{
                type       = 'write_file'
                targetPath = $targetPath
                content    = $updatedContent
            })
            continue
        }

        foreach ($operation in @(Get-RuleHarnessTimeProviderCodeFixOperations -Finding $Finding -RepoRoot $RepoRoot -TargetPath $targetPath -Content $content)) {
            [void]$operations.Add($operation)
        }
    }

    @(
        $operations |
            Group-Object targetPath |
            ForEach-Object { $_.Group | Select-Object -Last 1 }
    )
}

function New-RuleHarnessBatch {
    param(
        [Parameter(Mandatory)]
        [string]$Id,
        [Parameter(Mandatory)]
        [string]$Kind,
        [Parameter(Mandatory)]
        [string[]]$TargetFiles,
        [Parameter(Mandatory)]
        [string]$Reason,
        [Parameter(Mandatory)]
        [string[]]$Validation,
        [Parameter(Mandatory)]
        [string[]]$ExpectedFindingsResolved,
        [Parameter(Mandatory)]
        [object[]]$Operations,
        [string[]]$FeatureNames = @(),
        [string[]]$OwnerDocs = @(),
        [string[]]$SourceFindingTypes = @()
    )

    [pscustomobject]@{
        id                       = $Id
        kind                     = $Kind
        targetFiles              = @($TargetFiles)
        reason                   = $Reason
        validation               = @($Validation)
        expectedFindingsResolved = @($ExpectedFindingsResolved)
        status                   = 'planned'
        featureNames             = @($FeatureNames)
        ownerDocs                = @($OwnerDocs | Sort-Object -Unique)
        sourceFindingTypes       = @($SourceFindingTypes | Sort-Object -Unique)
        fingerprint              = $null
        riskScore                = $null
        riskLabel                = $null
        ownershipStatus          = 'pending'
        operations               = @($Operations)
    }
}

function Get-RuleHarnessPlannedBatches {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$ReviewedFindings,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$DocEdits,
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $batches = [System.Collections.Generic.List[object]]::new()
    $consumedFindingKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $counter = 1

    if (@($ReviewedFindings).Count -eq 0 -and @($DocEdits).Count -eq 0) {
        return @()
    }

    foreach ($group in @($DocEdits | Group-Object targetPath)) {
        $batchId = 'batch-{0:d3}' -f $counter
        $counter++
        $ownerFindings = @($ReviewedFindings | Where-Object {
            $_.ownerDoc -eq $group.Name -and $_.remediationKind -in @('rule_fix', 'mixed_fix')
        })
        foreach ($finding in $ownerFindings) {
            [void]$consumedFindingKeys.Add((Get-RuleHarnessFindingKey -Finding $finding))
        }

        [void]$batches.Add((New-RuleHarnessBatch `
            -Id $batchId `
            -Kind 'rule_fix' `
            -TargetFiles @($group.Name) `
            -Reason "Synchronize SSOT document '$($group.Name)' with repo state." `
            -Validation @('rule_harness_tests', 'static_scan') `
            -ExpectedFindingsResolved @($ownerFindings | ForEach-Object { Get-RuleHarnessFindingKey -Finding $_ }) `
            -Operations @([pscustomobject]@{ type = 'doc_edit'; edits = @($group.Group) }) `
            -OwnerDocs @($ownerFindings | ForEach-Object { $_.ownerDoc }) `
            -SourceFindingTypes @($ownerFindings | ForEach-Object { $_.findingType })))
    }

    $groupedExistingFileFixTitles = @(
        'Phantom shared contract name',
        'Missing import after symbol move',
        'Feature short-type shadowing',
        'Event contract drift'
    )
    $groupedCodeFixFindings = @(
        $ReviewedFindings |
            Where-Object {
                $_.remediationKind -eq 'code_fix' -and
                [string]$_.title -in $groupedExistingFileFixTitles
            }
    )
    foreach ($group in @($groupedCodeFixFindings | Group-Object { Get-RuleHarnessPrimaryCodeEvidencePath -Finding $_ })) {
        $targetPath = [string]$group.Name
        if ([string]::IsNullOrWhiteSpace($targetPath)) {
            continue
        }

        $operations = @(Get-RuleHarnessGroupedExistingCodeFixOperations -Findings @($group.Group) -RepoRoot $RepoRoot -TargetPath $targetPath)
        if ($operations.Count -eq 0) {
            continue
        }

        $batchId = 'batch-{0:d3}' -f $counter
        $counter++
        $expectedFindingsResolved = @($group.Group | ForEach-Object { Get-RuleHarnessFindingKey -Finding $_ })
        foreach ($finding in @($group.Group)) {
            [void]$consumedFindingKeys.Add((Get-RuleHarnessFindingKey -Finding $finding))
        }

        [void]$batches.Add((New-RuleHarnessBatch `
            -Id $batchId `
            -Kind 'code_fix' `
            -TargetFiles @($targetPath) `
            -Reason ("Resolve {0} existing-file code finding(s) in '{1}'." -f @($group.Group).Count, $targetPath) `
            -Validation @('rule_harness_tests', 'inferred_validation', 'static_scan') `
            -ExpectedFindingsResolved $expectedFindingsResolved `
            -Operations @($operations) `
            -FeatureNames @($group.Group | ForEach-Object { Get-RuleHarnessRelatedFeatureNamesForFinding -Finding $_ } | Sort-Object -Unique) `
            -OwnerDocs @($group.Group | ForEach-Object { $_.ownerDoc } | Sort-Object -Unique) `
            -SourceFindingTypes @($group.Group | ForEach-Object { $_.findingType } | Sort-Object -Unique)))
    }

    foreach ($finding in $ReviewedFindings) {
        $key = Get-RuleHarnessFindingKey -Finding $finding
        if ($consumedFindingKeys.Contains($key)) {
            continue
        }

        if ($finding.remediationKind -eq 'code_fix' -and $finding.title -eq 'Missing feature bootstrap root') {
            $featureName = $null
            foreach ($evidence in @($finding.evidence)) {
                $evidencePath = [string]$evidence.path
                if ($evidencePath -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
                    $featureName = $Matches['feature']
                    break
                }
            }

            if ([string]::IsNullOrWhiteSpace($featureName) -and [string]$finding.message -match "Feature '([^']+)'") {
                $featureName = $Matches[1]
            }

            if ([string]::IsNullOrWhiteSpace($featureName)) {
                continue
            }

            $targetPath = "Assets/Scripts/Features/$featureName/${featureName}Setup.cs"
            if (Test-Path -LiteralPath (Join-Path $RepoRoot $targetPath)) {
                continue
            }

            $batchId = 'batch-{0:d3}' -f $counter
            $counter++
            [void]$batches.Add((New-RuleHarnessBatch `
                -Id $batchId `
                -Kind 'code_fix' `
                -TargetFiles @($targetPath) `
                -Reason "Add missing root setup scaffold for feature '$featureName' to satisfy the architecture bootstrap contract." `
                -Validation @('rule_harness_tests', 'inferred_validation', 'static_scan') `
                -ExpectedFindingsResolved @($key) `
                -Operations @([pscustomobject]@{
                    type       = 'write_file'
                    targetPath = $targetPath
                    content    = (Get-RuleHarnessBootstrapSetupContent -FeatureName $featureName)
                }) `
                -FeatureNames @($featureName) `
                -OwnerDocs @($finding.ownerDoc) `
                -SourceFindingTypes @($finding.findingType)))
            continue
        }

        if ($finding.remediationKind -eq 'code_fix') {
            $operations = @(Get-RuleHarnessExistingCodeFixOperations -Finding $finding -RepoRoot $RepoRoot)
            if ($operations.Count -eq 0) {
                continue
            }

            $batchId = 'batch-{0:d3}' -f $counter
            $counter++
            $targetFiles = @($operations | ForEach-Object { [string]$_.targetPath } | Sort-Object -Unique)
            [void]$batches.Add((New-RuleHarnessBatch `
                -Id $batchId `
                -Kind 'code_fix' `
                -TargetFiles $targetFiles `
                -Reason ([string]$finding.message) `
                -Validation @('rule_harness_tests', 'inferred_validation', 'static_scan') `
                -ExpectedFindingsResolved @($key) `
                -Operations @($operations) `
                -FeatureNames @(Get-RuleHarnessRelatedFeatureNamesForFinding -Finding $finding) `
                -OwnerDocs @($finding.ownerDoc) `
                -SourceFindingTypes @($finding.findingType)))
        }
    }

    @($batches)
}

function Get-RuleHarnessUnplannedFindings {
    param(
        [AllowEmptyCollection()]
        [object[]]$ReviewedFindings,
        [AllowEmptyCollection()]
        [object[]]$PlannedBatches
    )

    $plannedFindingKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($batch in @($PlannedBatches)) {
        foreach ($findingKey in @($batch.expectedFindingsResolved)) {
            if (-not [string]::IsNullOrWhiteSpace([string]$findingKey)) {
                [void]$plannedFindingKeys.Add([string]$findingKey)
            }
        }
    }

    @(
        $ReviewedFindings |
            Where-Object {
                $_.severity -in @('high', 'medium') -and
                $_.remediationKind -eq 'code_fix' -and
                -not $plannedFindingKeys.Contains((Get-RuleHarnessFindingKey -Finding $_))
            }
    )
}

function Get-RuleHarnessDirtyTargetPaths {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string[]]$TargetFiles
    )

    if ($TargetFiles.Count -eq 0) {
        return @()
    }

    $output = Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments (@('status', '--porcelain', '--') + @($TargetFiles))
    if ($LASTEXITCODE -ne 0) {
        return @()
    }

    $dirty = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($line in @($output)) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        if ($line.Length -lt 4) {
            continue
        }

        [void]$dirty.Add($line.Substring(3).Trim().Replace('\', '/'))
    }

    @($dirty)
}

function Get-RuleHarnessFileSnapshots {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string[]]$TargetFiles
    )

    $snapshots = [System.Collections.Generic.List[object]]::new()
    foreach ($relativePath in $TargetFiles | Sort-Object -Unique) {
        $fullPath = Join-Path $RepoRoot $relativePath
        $exists = Test-Path -LiteralPath $fullPath
        [void]$snapshots.Add([pscustomobject]@{
            path    = $relativePath
            exists  = $exists
            content = if ($exists) { Get-Content -Path $fullPath -Raw } else { $null }
        })
    }

    @($snapshots)
}

function Restore-RuleHarnessFileSnapshots {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object[]]$Snapshots
    )

    foreach ($snapshot in $Snapshots) {
        $fullPath = Join-Path $RepoRoot $snapshot.path
        $parent = Split-Path -Parent $fullPath
        if (-not (Test-Path -LiteralPath $parent)) {
            New-Item -ItemType Directory -Path $parent -Force | Out-Null
        }

        if ($snapshot.exists) {
            Set-Content -Path $fullPath -Value $snapshot.content -Encoding UTF8
        }
        elseif (Test-Path -LiteralPath $fullPath) {
            Remove-Item -LiteralPath $fullPath -Force
        }
    }
}

function Invoke-RuleHarnessBatchOperations {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [switch]$DryRun
    )

    $touchedPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $docEditResults = [System.Collections.Generic.List[object]]::new()

    foreach ($operation in $Batch.operations) {
        switch ($operation.type) {
            'doc_edit' {
                $docResult = Invoke-RuleHarnessDocEdits -Edits @($operation.edits) -RepoRoot $RepoRoot -Config $Config -DryRun:$DryRun
                foreach ($edit in $docResult.edits) {
                    [void]$docEditResults.Add($edit)
                }
                if (-not $DryRun) {
                    foreach ($edit in $docResult.edits | Where-Object status -eq 'applied') {
                        [void]$touchedPaths.Add($edit.targetPath)
                    }
                }
            }
            'write_file' {
                if ($DryRun) {
                    continue
                }

                $targetPath = [string]$operation.targetPath
                $fullPath = Join-Path $RepoRoot $targetPath
                $parent = Split-Path -Parent $fullPath
                if (-not (Test-Path -LiteralPath $parent)) {
                    New-Item -ItemType Directory -Path $parent -Force | Out-Null
                }

                Set-Content -Path $fullPath -Value ([string]$operation.content) -Encoding UTF8
                [void]$touchedPaths.Add($targetPath)
            }
            default {
                throw "Unsupported batch operation type: $($operation.type)"
            }
        }
    }

    [pscustomobject]@{
        touchedPaths   = @($touchedPaths)
        docEditResults = @($docEditResults)
    }
}

function Invoke-RuleHarnessValidationScript {
    param(
        [Parameter(Mandatory)]
        [string]$ScriptPath,
        [Parameter(Mandatory)]
        [string]$ValidationName,
        [Parameter(Mandatory)]
        [string]$BatchId,
        [Parameter(Mandatory)]
        [string]$Source,
        [Parameter(Mandatory)]
        [string]$FailureReason,
        [string]$Label
    )

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & powershell -NoProfile -ExecutionPolicy Bypass -File $ScriptPath | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "$ValidationName script exited with code $LASTEXITCODE."
        }

        $details = if ([string]::IsNullOrWhiteSpace($Label)) {
            "ElapsedMs=$($stopwatch.ElapsedMilliseconds)"
        }
        else {
            "$Label ElapsedMs=$($stopwatch.ElapsedMilliseconds)"
        }

        return [pscustomobject]@{
            passed         = $true
            result         = [pscustomobject]@{
                batchId    = $BatchId
                validation = $ValidationName
                status     = 'passed'
                source     = $Source
                details    = $details
            }
            failureReason  = $null
            failureMessage = $null
            failureSource  = $null
        }
    }
    catch {
        $failureMessage = $_.Exception.Message
        $details = if ([string]::IsNullOrWhiteSpace($Label)) {
            $failureMessage
        }
        else {
            "${Label}: $failureMessage"
        }

        return [pscustomobject]@{
            passed         = $false
            result         = [pscustomobject]@{
                batchId    = $BatchId
                validation = $ValidationName
                status     = 'failed'
                source     = $Source
                details    = $details
            }
            failureReason  = $FailureReason
            failureMessage = $failureMessage
            failureSource  = $ScriptPath
        }
    }
}

function Invoke-RuleHarnessBatchValidation {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [object]$ValidationPlan,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$BaselineStaticFindings,
        [string]$StaticScanScopeId
    )

    $results = [System.Collections.Generic.List[object]]::new()
    [void]$results.Add([pscustomobject]@{
        batchId    = $Batch.id
        validation = 'discovered_validation_plan'
        status     = 'passed'
        source     = [string]$ValidationPlan.source
        details    = "Confidence=$($ValidationPlan.confidence) Runnable=$($ValidationPlan.runnable) Checks=$(@($ValidationPlan.checks).Count) InferredChecks=$(@($ValidationPlan.inferredChecks).Count) FeatureAssets=$(@($ValidationPlan.featureTestAssets).Count)"
    })

    if ($Config.validation.runHarnessTests) {
        $scriptPath = Join-Path $RepoRoot ([string]$Config.validation.harnessTestScript)
        $scriptResult = Invoke-RuleHarnessValidationScript `
            -ScriptPath $scriptPath `
            -ValidationName 'rule_harness_tests' `
            -BatchId ([string]$Batch.id) `
            -Source ([string]$Config.validation.harnessTestScript) `
            -FailureReason 'rule-harness-tests-failed'
        [void]$results.Add($scriptResult.result)
        if (-not $scriptResult.passed) {
            return [pscustomobject]@{
                passed        = $false
                findingsAfter = $BaselineStaticFindings
                results       = @($results)
                failureReason = [string]$scriptResult.failureReason
                failureMessage = [string]$scriptResult.failureMessage
                failureSource = [string]$scriptResult.failureSource
            }
        }
    }

    if ($Batch.kind -ne 'rule_fix') {
        $checkNotes = [System.Collections.Generic.List[string]]::new()
        $missingTargets = @($Batch.targetFiles | Where-Object {
            -not (Test-Path -LiteralPath (Join-Path $RepoRoot ([string]$_)))
        })
        if ($missingTargets.Count -gt 0) {
            $details = "Missing target files after apply: $($missingTargets -join ', ')"
            [void]$results.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'inferred_validation'
                status     = 'failed'
                source     = 'inferred'
                details    = $details
            })
            return [pscustomobject]@{
                passed        = $false
                findingsAfter = $BaselineStaticFindings
                results       = @($results)
                failureReason = 'inferred-check-failed'
                failureMessage = $details
                failureSource = 'inferred'
            }
        }
        [void]$checkNotes.Add('target_files_exist')

        $featureNames = @(Get-RuleHarnessBatchFeatureNames -Batch $Batch)
        $featureRootContracts = Test-RuleHarnessFeatureRootContracts -RepoRoot $RepoRoot -FeatureNames $featureNames
        if (-not $featureRootContracts.passed) {
            $details = "Missing root feature contracts after apply: $($featureRootContracts.missing -join ', ')"
            [void]$results.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'inferred_validation'
                status     = 'failed'
                source     = 'inferred'
                details    = $details
            })
            return [pscustomobject]@{
                passed         = $false
                findingsAfter  = $BaselineStaticFindings
                results        = @($results)
                failureReason  = 'inferred-check-failed'
                failureMessage = $details
                failureSource  = 'inferred'
            }
        }
        [void]$checkNotes.Add('feature_root_contract_present')

        if ([string]::IsNullOrWhiteSpace($StaticScanScopeId)) {
            $ownerDocReferences = Test-RuleHarnessOwnerDocReferences -RepoRoot $RepoRoot -Config $Config
            if (-not $ownerDocReferences.passed) {
                $details = "Broken owner doc references after apply: $($ownerDocReferences.broken -join '; ')"
                [void]$results.Add([pscustomobject]@{
                    batchId    = $Batch.id
                    validation = 'inferred_validation'
                    status     = 'failed'
                    source     = 'inferred'
                    details    = $details
                })
                return [pscustomobject]@{
                    passed         = $false
                    findingsAfter  = $BaselineStaticFindings
                    results        = @($results)
                    failureReason  = 'inferred-check-failed'
                    failureMessage = $details
                    failureSource  = 'inferred'
                }
            }
            [void]$checkNotes.Add('owner_doc_references_resolve')
        }

        if (@($ValidationPlan.inferredChecks).Count -gt 0) {
            [void]$checkNotes.Add('expected_findings_resolved')
            [void]$checkNotes.Add('no_new_high_severity_findings')
        }

        [void]$results.Add([pscustomobject]@{
            batchId    = $Batch.id
            validation = 'inferred_validation'
            status     = 'passed'
            source     = [string]$ValidationPlan.source
            details    = "Checks=$($checkNotes -join ', ')"
        })
    }
    else {
        [void]$results.Add([pscustomobject]@{
            batchId    = $Batch.id
            validation = 'inferred_validation'
            status     = 'skipped'
            source     = 'rule-only'
            details    = 'rule-only batch'
        })
    }

    $afterFindings = @(Get-RuleHarnessStaticFindings -RepoRoot $RepoRoot -Config $Config -ScopeId $StaticScanScopeId)
    $afterKeys = @($afterFindings | ForEach-Object { Get-RuleHarnessFindingKey -Finding $_ })
    $baselineHigh = @($BaselineStaticFindings | Where-Object severity -eq 'high' | ForEach-Object { Get-RuleHarnessFindingKey -Finding $_ })
    $afterHigh = @($afterFindings | Where-Object severity -eq 'high' | ForEach-Object { Get-RuleHarnessFindingKey -Finding $_ })
    $resolvedCount = @($Batch.expectedFindingsResolved | Where-Object { $_ -notin $afterKeys }).Count
    $newHigh = @($afterHigh | Where-Object { $_ -notin $baselineHigh })
    $staticPassed = ($resolvedCount -eq $Batch.expectedFindingsResolved.Count) -and ($newHigh.Count -eq 0) -and ($afterFindings.Count -lt $BaselineStaticFindings.Count -or $resolvedCount -gt 0)
    $staticDetails = "Before=$($BaselineStaticFindings.Count) After=$($afterFindings.Count) Resolved=$resolvedCount Expected=$($Batch.expectedFindingsResolved.Count) NewHigh=$($newHigh.Count)"
    [void]$results.Add([pscustomobject]@{
        batchId    = $Batch.id
        validation = 'static_scan'
        status     = if ($staticPassed) { 'passed' } else { 'failed' }
        source     = 'static-scan'
        details    = $staticDetails
    })

    [pscustomobject]@{
        passed        = $staticPassed
        findingsAfter = @($afterFindings)
        results       = @($results)
        failureReason = if ($staticPassed) { $null } else { 'static-scan-failed' }
        failureMessage = if ($staticPassed) { $null } else { $staticDetails }
        failureSource = if ($staticPassed) { $null } else { 'static-scan' }
    }
}

function Get-RuleHarnessPreferredRepairStrategy {
    param(
        [Parameter(Mandatory)]
        [object]$ScopeInfo
    )

    switch ([string]$ScopeInfo.scopeType) {
        'feature' { return 'Update the owning rule doc or strengthen inferred validation coverage before retrying.' }
        'harness' { return 'Improve harness prompts, config, tests, or docs before retrying.' }
        default { return 'Amend the owning global rule doc before retrying.' }
    }
}

function New-RuleHarnessBatchAttemptResult {
    param(
        [Parameter(Mandatory)]
        [bool]$Success,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$FindingsAfter,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$DocEdits,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$TouchedPaths,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$ValidationResults,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$LearningTrace,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$MemoryHits,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$MemoryUpdates,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PromotionCandidates,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$DecisionTrace,
        [string]$FailureReason,
        [string]$FailureMessage,
        [int]$RetryAttempts
    )

    [pscustomobject]@{
        success             = $Success
        findingsAfter       = @($FindingsAfter)
        docEdits            = @($DocEdits)
        touchedPaths        = @($TouchedPaths)
        validationResults   = @($ValidationResults)
        learningTrace       = @($LearningTrace)
        memoryHits          = @($MemoryHits)
        memoryUpdates       = @($MemoryUpdates)
        promotionCandidates = @($PromotionCandidates)
        decisionTrace       = @($DecisionTrace)
        failureReason       = $FailureReason
        failureMessage      = $FailureMessage
        retryAttempts       = [int]$RetryAttempts
    }
}

function Invoke-RuleHarnessBatchAttemptLoop {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$BaselineStaticFindings,
        [Parameter(Mandatory)]
        [object]$ValidationPlan,
        [Parameter(Mandatory)]
        [object]$MemoryStore,
        [Parameter(Mandatory)]
        [object]$LearningSettings,
        [Parameter(Mandatory)]
        [string]$CommitSha,
        [string]$StaticScanScopeId
    )

    $validationResults = [System.Collections.Generic.List[object]]::new()
    $learningTrace = [System.Collections.Generic.List[object]]::new()
    $memoryHits = [System.Collections.Generic.List[object]]::new()
    $memoryUpdates = [System.Collections.Generic.List[object]]::new()
    $promotionCandidates = [System.Collections.Generic.List[object]]::new()
    $decisionTrace = [System.Collections.Generic.List[string]]::new()
    $scopeInfo = Get-RuleHarnessScopeInfo -RepoRoot $RepoRoot -OwnerDoc ([string](@($Batch.ownerDocs)[0])) -TargetFiles @($Batch.targetFiles)
    $batchSnapshots = Get-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -TargetFiles @($Batch.targetFiles)
    $attemptLimit = [Math]::Max(1, [int]$LearningSettings.maxSameRunAttempts)
    $retryAttempts = 0
    $lastSignature = $null
    $retryMemoryEntries = @()
    $currentFindings = @($BaselineStaticFindings)

    for ($attemptIndex = 1; $attemptIndex -le $attemptLimit; $attemptIndex++) {
        $memoryEntriesUsedThisAttempt = @($retryMemoryEntries)
        if ($attemptIndex -gt 1) {
            $retryAttempts++
            [void]$decisionTrace.Add("Retry attempt $attemptIndex/$attemptLimit started for batch $($Batch.id).")
        }

        $attemptDocEdits = @()
        $attemptTouchedPaths = @()
        try {
            $applyResult = Invoke-RuleHarnessBatchOperations -Batch $Batch -RepoRoot $RepoRoot -Config $Config
            $attemptDocEdits = @($applyResult.docEditResults)
            $attemptTouchedPaths = @($applyResult.touchedPaths)
            $validation = Invoke-RuleHarnessBatchValidation -Batch $Batch -RepoRoot $RepoRoot -Config $Config -ValidationPlan $ValidationPlan -BaselineStaticFindings $currentFindings -StaticScanScopeId $StaticScanScopeId
            foreach ($result in $validation.results) {
                [void]$validationResults.Add($result)
            }

            if ($validation.passed) {
                [void]$learningTrace.Add([pscustomobject]@{
                    batchId                    = $Batch.id
                    attempt                    = $attemptIndex
                    normalizedFailureSignature = $null
                    memoryEntriesUsed          = @($memoryEntriesUsedThisAttempt | ForEach-Object { [string]$_.signature })
                    repairDelta                = if ($attemptIndex -eq 1) { 'initial-attempt' } else { 'memory-informed-retry' }
                    verificationResult         = 'passed'
                })
                return (New-RuleHarnessBatchAttemptResult `
                    -Success $true `
                    -FindingsAfter @($validation.findingsAfter) `
                    -DocEdits @($attemptDocEdits) `
                    -TouchedPaths @($attemptTouchedPaths) `
                    -ValidationResults @($validationResults) `
                    -LearningTrace @($learningTrace) `
                    -MemoryHits @($memoryHits) `
                    -MemoryUpdates @($memoryUpdates) `
                    -PromotionCandidates @($promotionCandidates) `
                    -DecisionTrace @($decisionTrace) `
                    -RetryAttempts $retryAttempts)
            }

            Restore-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -Snapshots $batchSnapshots
            $failureReason = [string]$validation.failureReason
            $failureMessage = [string]$validation.failureMessage
            $failureSource = if ([string]::IsNullOrWhiteSpace([string]$validation.failureSource)) { [string]$ValidationPlan.source } else { [string]$validation.failureSource }
        }
        catch {
            Restore-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -Snapshots $batchSnapshots
            $failureReason = 'apply-failed'
            $failureMessage = $_.Exception.Message
            $failureSource = 'apply'
            [void]$validationResults.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'apply'
                status     = 'failed'
                source     = 'apply'
                details    = $failureMessage
            })
        }

        $signature = Get-RuleHarnessFailureSignature -FailureReason $failureReason -PrimarySource $failureSource -OwnerDoc $scopeInfo.scopePath -BatchKind ([string]$Batch.kind) -Message $failureMessage
        $matchedMemoryEntry = Find-RuleHarnessMemoryEntry -MemoryStore $MemoryStore -Signature $signature -ScopePath $scopeInfo.scopePath
        if ($null -ne $matchedMemoryEntry) {
            [void]$memoryHits.Add([pscustomobject]@{
                batchId           = $Batch.id
                attempt           = $attemptIndex
                signature         = [string]$matchedMemoryEntry.signature
                scopePath         = [string]$matchedMemoryEntry.scopePath
                preferredStrategy = [string]$matchedMemoryEntry.preferredRepairStrategy
                promotionTarget   = [string]$matchedMemoryEntry.promotionTarget
            })
            $retryMemoryEntries = @($matchedMemoryEntry)
        }
        else {
            $retryMemoryEntries = @()
        }

        [void]$learningTrace.Add([pscustomobject]@{
            batchId                    = $Batch.id
            attempt                    = $attemptIndex
            normalizedFailureSignature = $signature
            memoryEntriesUsed          = @($memoryEntriesUsedThisAttempt | ForEach-Object { [string]$_.signature })
            repairDelta                = if ($attemptIndex -eq 1) { 'initial-attempt' } else { 'memory-informed-retry' }
            verificationResult         = $failureReason
        })
        [void]$decisionTrace.Add("Attempt $attemptIndex for batch $($Batch.id) failed with signature $signature")

        if ($attemptIndex -ge $attemptLimit) {
            break
        }

        if (-not [string]::IsNullOrWhiteSpace($lastSignature) -and $signature -eq $lastSignature) {
            [void]$decisionTrace.Add("Batch $($Batch.id) repeated the same normalized failure signature; no further retries will run.")
            break
        }

        $lastSignature = $signature
    }

    $memoryEntry = Update-RuleHarnessMemoryStoreEntry `
        -MemoryStore $MemoryStore `
        -Signature $signature `
        -ScopeType $scopeInfo.scopeType `
        -ScopePath $scopeInfo.scopePath `
        -Symptoms $failureMessage `
        -PreferredRepairStrategy (Get-RuleHarnessPreferredRepairStrategy -ScopeInfo $scopeInfo) `
        -ValidationHints @(@($ValidationPlan.featureTestAssets) + @($ValidationPlan.checks | ForEach-Object { [string]$_.name })) `
        -Confidence ([string]$ValidationPlan.confidence) `
        -CommitSha $CommitSha `
        -PromotionTarget $scopeInfo.promotionTarget `
        -Status 'observed'
    [void]$memoryUpdates.Add([pscustomobject]@{
        batchId             = $Batch.id
        signature           = [string]$memoryEntry.signature
        scopeType           = [string]$memoryEntry.scopeType
        scopePath           = [string]$memoryEntry.scopePath
        confidence          = [string]$memoryEntry.confidence
        hitCount            = [int]$memoryEntry.hitCount
        distinctCommitCount = [int]$memoryEntry.distinctCommitCount
        promotionTarget     = [string]$memoryEntry.promotionTarget
        status              = [string]$memoryEntry.status
    })

    $promotionCandidate = Get-RuleHarnessPromotionCandidate -Entry $memoryEntry -Rationale ("Recurring failure for batch {0}: {1}" -f [string]$Batch.id, [string]$failureReason) -Config $Config
    if ($null -ne $promotionCandidate) {
        [void]$promotionCandidates.Add($promotionCandidate)
    }

    return (New-RuleHarnessBatchAttemptResult `
        -Success $false `
        -FindingsAfter @($BaselineStaticFindings) `
        -DocEdits @() `
        -TouchedPaths @() `
        -ValidationResults @($validationResults) `
        -LearningTrace @($learningTrace) `
        -MemoryHits @($memoryHits) `
        -MemoryUpdates @($memoryUpdates) `
        -PromotionCandidates @($promotionCandidates) `
        -DecisionTrace @($decisionTrace) `
        -FailureReason $failureReason `
        -FailureMessage $failureMessage `
        -RetryAttempts $retryAttempts)
}

function Invoke-RuleHarnessCommit {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [string[]]$TargetFiles,
        [Parameter(Mandatory)]
        [object[]]$AppliedBatches
    )

    $branch = Get-RuleHarnessCurrentBranch -RepoRoot $RepoRoot
    $prefix = [string]$Config.mutation.commitMessagePrefix
    $commitMessage = "{0} apply {1} batch(es) on {2}" -f $prefix, $AppliedBatches.Count, $(if ([string]::IsNullOrWhiteSpace($branch)) { 'current branch' } else { $branch })

    Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('config', 'user.name', 'rule-harness[bot]') | Out-Null
    Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('config', 'user.email', 'rule-harness[bot]@users.noreply.github.com') | Out-Null
    Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments (@('add', '--') + @($TargetFiles)) | Out-Null
    Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('diff', '--cached', '--quiet', '--exit-code') | Out-Null
    if ($LASTEXITCODE -eq 0) {
        return [pscustomobject]@{
            attempted = $false
            created   = $false
            sha       = $null
            branch    = $branch
            message   = $commitMessage
        }
    }

    Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('commit', '-m', $commitMessage, '--no-verify') | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw 'git commit failed.'
    }

    [pscustomobject]@{
        attempted = $true
        created   = $true
        sha       = ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim()
        branch    = $branch
        message   = $commitMessage
    }
}

function Invoke-RuleHarnessMutationPlan {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PlannedBatches,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$InitialStaticFindings,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [object]$MutationState,
        [object]$ScopeInfo = $null,
        [string]$StaticScanScopeId,
        [switch]$RelaxScopeGuards,
        [switch]$DryRun
    )

    return Invoke-RuleHarnessMutationPlanCore `
        -PlannedBatches $PlannedBatches `
        -InitialStaticFindings $InitialStaticFindings `
        -RepoRoot $RepoRoot `
        -Config $Config `
        -MutationState $MutationState `
        -ScopeInfo $ScopeInfo `
        -StaticScanScopeId $StaticScanScopeId `
        -RelaxScopeGuards:$RelaxScopeGuards `
        -DryRun:$DryRun
}

function New-RuleHarnessMutationPlanResult {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$StageResults,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$ActionItems,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$DecisionTrace,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$ValidationResults,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$AppliedBatches,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$SkippedBatches,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$RollbackBatches,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$DocEdits,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$FinalStaticFindings,
        [Parameter(Mandatory)]
        [object]$HistorySummary,
        [Parameter(Mandatory)]
        [object]$Commit,
        [Parameter(Mandatory)]
        [bool]$RollbackPerformed,
        [Parameter(Mandatory)]
        [bool]$Failed,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$DiscoveredValidationPlan,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$LearningTrace,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$MemoryHits,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$MemoryUpdates,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PromotionCandidates,
        [Parameter(Mandatory)]
        [int]$RetryAttempts
    )

    [pscustomobject]@{
        stageResults             = @($StageResults)
        actionItems              = @(Merge-RuleHarnessActionItems -Items @($ActionItems))
        decisionTrace            = @($DecisionTrace)
        validationResults        = @($ValidationResults)
        appliedBatches           = @($AppliedBatches)
        skippedBatches           = @($SkippedBatches)
        rollbackBatches          = @($RollbackBatches)
        docEdits                 = @($DocEdits)
        finalStaticFindings      = @($FinalStaticFindings)
        historySummary           = $HistorySummary
        commit                   = $Commit
        rollback                 = [pscustomobject]@{
            performed     = $RollbackPerformed
            failedBatches = @($RollbackBatches | ForEach-Object { $_.id })
        }
        failed                   = $Failed
        applied                  = (@($AppliedBatches).Count -gt 0)
        discoveredValidationPlan = @($DiscoveredValidationPlan)
        learningTrace            = @($LearningTrace)
        memoryHits               = @($MemoryHits)
        memoryUpdates            = @($MemoryUpdates)
        promotionCandidates      = @($PromotionCandidates)
        retryAttempts            = [int]$RetryAttempts
    }
}

function Invoke-RuleHarnessMutationPlanCore {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PlannedBatches,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$InitialStaticFindings,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [object]$MutationState,
        [object]$ScopeInfo = $null,
        [string]$StaticScanScopeId,
        [switch]$RelaxScopeGuards,
        [switch]$DryRun
    )

    $decisionTrace = [System.Collections.Generic.List[string]]::new()
    $validationResults = [System.Collections.Generic.List[object]]::new()
    $appliedBatches = [System.Collections.Generic.List[object]]::new()
    $skippedBatches = [System.Collections.Generic.List[object]]::new()
    $rollbackBatches = [System.Collections.Generic.List[object]]::new()
    $docEdits = [System.Collections.Generic.List[object]]::new()
    $stageResults = [System.Collections.Generic.List[object]]::new()
    $actionItems = [System.Collections.Generic.List[object]]::new()
    $discoveredValidationPlans = [System.Collections.Generic.List[object]]::new()
    $learningTrace = [System.Collections.Generic.List[object]]::new()
    $memoryHits = [System.Collections.Generic.List[object]]::new()
    $memoryUpdates = [System.Collections.Generic.List[object]]::new()
    $promotionCandidates = [System.Collections.Generic.List[object]]::new()
    $retryAttempts = 0
    $finalStaticFindings = @($InitialStaticFindings)
    $branch = Get-RuleHarnessCurrentBranch -RepoRoot $RepoRoot
    $commitSha = ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim()

    Write-Host 'Rule harness history lookup started.'
    $historyState = Read-RuleHarnessHistoryState -RepoRoot $RepoRoot -Config $Config
    Write-Host "Rule harness history lookup finished. Entries: $($historyState.loadedEntryCount) GcRemoved: $($historyState.gcRemovedCount)"

    $memoryStore = Read-RuleHarnessMemoryStore -RepoRoot $RepoRoot -Config $Config
    $learningSettings = Get-RuleHarnessLearningSettings -Config $Config

    foreach ($batch in @($PlannedBatches)) {
        $fingerprint = Get-RuleHarnessBatchFingerprint -Batch $batch
        $riskAssessment = Get-RuleHarnessBatchRiskAssessment -Batch $batch -RepoRoot $RepoRoot -Config $Config -MutationMode $MutationState.mode
        $ownershipAssessment = Get-RuleHarnessBatchOwnershipAssessment -Batch $batch -RepoRoot $RepoRoot -Config $Config
        Set-RuleHarnessObjectProperty -Object $batch -Name 'fingerprint' -Value $fingerprint
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskScore' -Value $riskAssessment.score
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskLabel' -Value $riskAssessment.label
        Set-RuleHarnessObjectProperty -Object $batch -Name 'ownershipStatus' -Value $ownershipAssessment.status
    }

    $historySummary = [pscustomobject]@{
        statePath         = $historyState.path
        loadedEntryCount  = $historyState.loadedEntryCount
        activeEntryCount  = @($historyState.entries.Keys).Count
        gcRemovedCount    = $historyState.gcRemovedCount
        touchedEntryCount = 0
    }

    $emptyCommit = [pscustomobject]@{
        attempted = $false
        created   = $false
        sha       = $null
        branch    = $branch
        message   = $null
    }

    if (-not $MutationState.enabled -or $MutationState.mode -eq 'report_only') {
        [void]$decisionTrace.Add("Mutation loop disabled. Mode=$($MutationState.mode)")
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'mutation' `
            -Status 'skipped' `
            -Attempted $false `
            -Summary ("Mutation loop is disabled for mode {0}." -f $MutationState.mode) `
            -Details ([pscustomobject]@{
                mode = $MutationState.mode
                retryAttempts = 0
            })))
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'commit' `
            -Status 'skipped' `
            -Attempted $false `
            -Summary 'Commit stage was skipped because no mutation ran.' `
            -Details $null))
        Save-RuleHarnessHistoryState -HistoryState $historyState
        return (New-RuleHarnessMutationPlanResult `
            -StageResults @($stageResults) `
            -ActionItems @() `
            -DecisionTrace @($decisionTrace) `
            -ValidationResults @() `
            -AppliedBatches @() `
            -SkippedBatches @() `
            -RollbackBatches @() `
            -DocEdits @() `
            -FinalStaticFindings @($finalStaticFindings) `
            -HistorySummary $historySummary `
            -Commit $emptyCommit `
            -RollbackPerformed $false `
            -Failed $false `
            -DiscoveredValidationPlan @() `
            -LearningTrace @() `
            -MemoryHits @() `
            -MemoryUpdates @() `
            -PromotionCandidates @() `
            -RetryAttempts 0)
    }

    [void]$decisionTrace.Add("Mutation loop enabled. Mode=$($MutationState.mode)")
    if ($PlannedBatches.Count -eq 0) {
        [void]$decisionTrace.Add('No planned batches were generated. Mutation stage completed without edits.')
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'mutation' `
            -Status 'passed' `
            -Attempted $true `
            -Summary 'No planned batches were generated.' `
            -Details ([pscustomobject]@{
                plannedBatchCount = 0
                retryAttempts = 0
            })))
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'commit' `
            -Status 'skipped' `
            -Attempted $false `
            -Summary 'Commit stage was skipped because no batch touched the workspace.' `
            -Details $null))
        Save-RuleHarnessHistoryState -HistoryState $historyState
        return (New-RuleHarnessMutationPlanResult `
            -StageResults @($stageResults) `
            -ActionItems @() `
            -DecisionTrace @($decisionTrace) `
            -ValidationResults @() `
            -AppliedBatches @() `
            -SkippedBatches @() `
            -RollbackBatches @() `
            -DocEdits @() `
            -FinalStaticFindings @($finalStaticFindings) `
            -HistorySummary $historySummary `
            -Commit $emptyCommit `
            -RollbackPerformed $false `
            -Failed $false `
            -DiscoveredValidationPlan @() `
            -LearningTrace @() `
            -MemoryHits @() `
            -MemoryUpdates @() `
            -PromotionCandidates @() `
            -RetryAttempts 0)
    }

    $globalTargetFiles = @($PlannedBatches | ForEach-Object { $_.targetFiles } | Sort-Object -Unique)
    $globalSnapshots = Get-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -TargetFiles $globalTargetFiles
    $currentFindings = @($InitialStaticFindings)
    $touchedTargetFiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $failureTriggered = $false
    $appliedBatchObjects = [System.Collections.Generic.List[object]]::new()
    $failureReasonCode = $null
    $nonActionableReasons = if ($Config.history.PSObject.Properties.Name -contains 'nonActionableReasons') { @($Config.history.nonActionableReasons) } else { @() }
    $maxAttemptsPerCommit = if ($Config.history.PSObject.Properties.Name -contains 'maxAttemptsPerCommit') { [int]$Config.history.maxAttemptsPerCommit } else { 2 }

    foreach ($batch in @($PlannedBatches | Select-Object -First ([int]$Config.mutation.maxBatchesPerRun))) {
        $fingerprint = [string]$batch.fingerprint
        $historyEntry = Get-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint

        if ($null -ne $ScopeInfo -and [string]$ScopeInfo.scopeGuard -eq 'rules-only') {
            $violatingTargets = @(Get-RuleHarnessRulesOnlyViolationTargets -TargetFiles @($batch.targetFiles))
            if ($violatingTargets.Count -gt 0) {
                $historyEntry = Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'skipped' -Reason 'rules-scope-mutation-violation'
                Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
                $skippedBatch = [pscustomobject]@{
                    id           = $batch.id
                    kind         = $batch.kind
                    reason       = ("Rules-only scope '{0}' may only mutate AGENTS/docs/repo-local skill entries/hooks/docs-lint/rule-harness. Blocked targets: {1}. If this is a real code task, lock user intent again before mutating." -f [string]$ScopeInfo.scopePath, ($violatingTargets -join ', '))
                    reasonCode   = 'rules-scope-mutation-violation'
                    status       = 'skipped'
                    targets      = @($violatingTargets)
                    fingerprint  = $fingerprint
                    attemptCount = [int]$historyEntry.attemptCount
                }
                [void]$skippedBatches.Add($skippedBatch)
                [void]$actionItems.Add((Get-RuleHarnessActionItemForSkippedBatch -SkippedBatch $skippedBatch -PlannedBatch $batch -Config $Config))
                $failureTriggered = $true
                $failureReasonCode = 'rules-scope-mutation-violation'
                break
            }
        }

        if ($MutationState.mode -eq 'doc_only' -and $batch.kind -ne 'rule_fix') {
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = 'mutation-mode-rejected'
                reasonCode   = 'mutation-mode-rejected'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = if ($null -ne $historyEntry) { [int]$historyEntry.attemptCount } else { 0 }
            })
            continue
        }

        if (@($batch.targetFiles).Count -gt [int]$Config.mutation.maxFilesPerBatch) {
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = 'too-many-target-files'
                reasonCode   = 'too-many-target-files'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = if ($null -ne $historyEntry) { [int]$historyEntry.attemptCount } else { 0 }
            })
            continue
        }

        $dirtyTargets = if ($Config.mutation.requireCleanTargets) { @(Get-RuleHarnessDirtyTargetPaths -RepoRoot $RepoRoot -TargetFiles $batch.targetFiles) } else { @() }
        if (@($dirtyTargets).Count -gt 0) {
            $historyEntry = Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'skipped' -Reason 'dirty-target-files'
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
            $skippedBatch = [pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = 'dirty-target-files'
                reasonCode   = 'dirty-target-files'
                status       = 'skipped'
                targets      = $dirtyTargets
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            }
            [void]$skippedBatches.Add($skippedBatch)
            [void]$actionItems.Add((Get-RuleHarnessActionItemForSkippedBatch -SkippedBatch $skippedBatch -PlannedBatch $batch -Config $Config))
            continue
        }

        $discoveredPlan = Get-RuleHarnessDiscoveredValidationPlan -Batch $batch -RepoRoot $RepoRoot -Config $Config
        [void]$discoveredValidationPlans.Add($discoveredPlan)
        [void]$decisionTrace.Add("Batch $($batch.id) capability discovery: source=$($discoveredPlan.source) confidence=$($discoveredPlan.confidence) checks=$(@($discoveredPlan.checks).Count) inferredChecks=$(@($discoveredPlan.inferredChecks).Count)")
        if ($batch.kind -ne 'rule_fix' -and [string]$discoveredPlan.confidence -ne 'high') {
            [void]$actionItems.Add((New-RuleHarnessActionItem `
                -Kind 'increase-inference-coverage' `
                -Severity $(if ([string]$discoveredPlan.confidence -eq 'low') { 'high' } else { 'medium' }) `
                -Summary ("Increase inference coverage for batch {0}" -f [string]$batch.id) `
                -Details ("Runnerless validation is relying on inferred checks only. Confidence={0}. Add stronger inferred signals or broader static coverage before trusting auto-apply." -f [string]$discoveredPlan.confidence) `
                -RelatedPaths (Get-RuleHarnessCombinedPaths -Primary @($batch.targetFiles) -Secondary @($discoveredPlan.featureTestAssets))))
        }

        $ownershipAssessment = Get-RuleHarnessBatchOwnershipAssessment -Batch $batch -RepoRoot $RepoRoot -Config $Config
        Set-RuleHarnessObjectProperty -Object $batch -Name 'ownershipStatus' -Value $ownershipAssessment.status
        if ($ownershipAssessment.status -eq 'rejected') {
            $historyEntry = Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'skipped' -Reason 'ownership-preflight-rejected'
            $skippedBatch = [pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = $ownershipAssessment.reason
                reasonCode   = 'ownership-preflight-rejected'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            }
            [void]$skippedBatches.Add($skippedBatch)
            [void]$actionItems.Add((Get-RuleHarnessActionItemForSkippedBatch -SkippedBatch $skippedBatch -PlannedBatch $batch -Config $Config))
            continue
        }

        if ((-not $RelaxScopeGuards) -and $batch.kind -ne 'rule_fix' -and [string]$discoveredPlan.confidence -eq 'low') {
            $historyEntry = Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'skipped' -Reason 'manual-validation-required'
            $skippedBatch = [pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = "Validation confidence is low for this runnerless batch. Manual validation is required before auto-apply. Source=$($discoveredPlan.source)"
                reasonCode   = 'manual-validation-required'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            }
            [void]$skippedBatches.Add($skippedBatch)
            [void]$actionItems.Add((Get-RuleHarnessActionItemForSkippedBatch -SkippedBatch $skippedBatch -PlannedBatch $batch -Config $Config))
            continue
        }

        $riskAssessment = Get-RuleHarnessBatchRiskAssessment -Batch $batch -RepoRoot $RepoRoot -Config $Config -MutationMode $MutationState.mode
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskScore' -Value $riskAssessment.score
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskLabel' -Value $riskAssessment.label
        if ((-not $RelaxScopeGuards) -and (-not $riskAssessment.allowed)) {
            $skippedBatch = [pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = "Score $($riskAssessment.score) exceeded threshold $($riskAssessment.threshold)."
                reasonCode   = 'risk-threshold-exceeded'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = if ($null -ne $historyEntry) { [int]$historyEntry.attemptCount } else { 0 }
            }
            [void]$skippedBatches.Add($skippedBatch)
            [void]$actionItems.Add((Get-RuleHarnessActionItemForSkippedBatch -SkippedBatch $skippedBatch -PlannedBatch $batch -Config $Config))
            continue
        }

        $historyEntry = Get-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint
        if ($null -ne $historyEntry -and [string]$historyEntry.lastStatus -eq 'applied') {
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = 'already-applied-on-commit'
                reasonCode   = 'already-applied-on-commit'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            })
            continue
        }

        if ($null -ne $historyEntry -and [string]$historyEntry.lastReason -in $nonActionableReasons) {
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = [string]$historyEntry.lastReason
                reasonCode   = [string]$historyEntry.lastReason
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            })
            continue
        }

        if ($null -ne $historyEntry -and [int]$historyEntry.attemptCount -ge $maxAttemptsPerCommit) {
            $skippedBatch = [pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = 'max-attempts-reached'
                reasonCode   = 'max-attempts-reached'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            }
            [void]$skippedBatches.Add($skippedBatch)
            [void]$actionItems.Add((Get-RuleHarnessActionItemForSkippedBatch -SkippedBatch $skippedBatch -PlannedBatch $batch -Config $Config))
            continue
        }

        if ($DryRun) {
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'proposed'
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = 'dry-run'
                reasonCode   = 'dry-run'
                status       = 'proposed'
                fingerprint  = $fingerprint
                attemptCount = if ($null -ne $historyEntry) { [int]$historyEntry.attemptCount } else { 0 }
            })
            continue
        }

        $attemptResult = Invoke-RuleHarnessBatchAttemptLoop `
            -Batch $batch `
            -RepoRoot $RepoRoot `
            -Config $Config `
            -BaselineStaticFindings $currentFindings `
            -ValidationPlan $discoveredPlan `
            -MemoryStore $memoryStore `
            -LearningSettings $learningSettings `
            -CommitSha $commitSha `
            -StaticScanScopeId $StaticScanScopeId

        foreach ($entry in @($attemptResult.validationResults)) { [void]$validationResults.Add($entry) }
        foreach ($entry in @($attemptResult.learningTrace)) { [void]$learningTrace.Add($entry) }
        foreach ($entry in @($attemptResult.memoryHits)) { [void]$memoryHits.Add($entry) }
        foreach ($entry in @($attemptResult.memoryUpdates)) { [void]$memoryUpdates.Add($entry) }
        foreach ($entry in @($attemptResult.promotionCandidates)) { [void]$promotionCandidates.Add($entry) }
        foreach ($entry in @($attemptResult.decisionTrace)) { [void]$decisionTrace.Add($entry) }
        $retryAttempts += [int]$attemptResult.retryAttempts

        if ($attemptResult.success) {
            foreach ($docEdit in @($attemptResult.docEdits)) { [void]$docEdits.Add($docEdit) }
            foreach ($target in @($attemptResult.touchedPaths)) { [void]$touchedTargetFiles.Add($target) }
            $currentFindings = @($attemptResult.findingsAfter)
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'applied'
            [void]$appliedBatches.Add([pscustomobject]@{
                id          = $batch.id
                kind        = $batch.kind
                targetFiles = $batch.targetFiles
                status      = 'applied'
                reason      = $batch.reason
            })
            [void]$appliedBatchObjects.Add($batch)
            continue
        }

        [void](Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'failed' -Reason ([string]$attemptResult.failureReason))
        foreach ($promotionCandidate in @($attemptResult.promotionCandidates)) {
            [void]$actionItems.Add((New-RuleHarnessActionItem `
                -Kind 'promote-durable-rule' `
                -Severity 'high' `
                -Summary ("Promote recurring failure guidance to {0}" -f [string]$promotionCandidate.targetDoc) `
                -Details ("Signature {0} repeated across {1} runs and {2} commits. Capture the durable rule in the owning doc." -f [string]$promotionCandidate.signature, [int]$promotionCandidate.hitCount, [int]$promotionCandidate.distinctCommitCount) `
                -RelatedPaths (Get-RuleHarnessCombinedPaths -Primary @($batch.targetFiles) -Secondary @([string]$promotionCandidate.targetDoc))))
        }
        $failureTriggered = $true
        $failureReasonCode = [string]$attemptResult.failureReason
        if ($Config.mutation.stopOnFirstFailure) {
            break
        }
    }

    $rollbackPerformed = $false
    if ($failureTriggered) {
        Restore-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -Snapshots $globalSnapshots
        $rollbackPerformed = $true
        foreach ($appliedBatch in $appliedBatches) {
            [void](Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint ([string]($appliedBatchObjects | Where-Object id -eq $appliedBatch.id | Select-Object -First 1).fingerprint) -Status 'failed' -Reason 'rolled-back-after-batch-failure')
            [void]$rollbackBatches.Add([pscustomobject]@{
                id     = $appliedBatch.id
                kind   = $appliedBatch.kind
                status = 'rolled_back'
            })
        }
        $appliedBatches.Clear()
        $touchedTargetFiles.Clear()
        $finalStaticFindings = @($InitialStaticFindings)
    }
    else {
        $finalStaticFindings = @($currentFindings)
    }

    if (-not $DryRun -and $memoryStore.dirty) {
        Save-RuleHarnessMemoryStore -MemoryStore $memoryStore
    }

    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'mutation' `
        -Status $(if ($failureTriggered) { 'failed' } elseif (@($appliedBatches).Count -gt 0) { 'passed' } elseif (@($skippedBatches).Count -gt 0) { 'skipped' } else { 'passed' }) `
        -Attempted $true `
        -Summary $(if ($failureReasonCode -eq 'rules-scope-mutation-violation') { 'Mutation stage stopped because a rules-only scope attempted to mutate non-rule targets.' } elseif ($failureTriggered) { 'Mutation stage failed and rolled back touched files.' } elseif (@($appliedBatches).Count -gt 0) { "Applied $(@($appliedBatches).Count) batch(es)." } elseif (@($skippedBatches).Count -gt 0) { "No batch was applied. Skipped $(@($skippedBatches).Count) batch(es)." } else { 'Mutation stage completed without workspace changes.' }) `
        -Details ([pscustomobject]@{
            appliedBatchCount = @($appliedBatches).Count
            skippedBatchCount = @($skippedBatches).Count
            retryAttempts = $retryAttempts
            memoryUpdates = @($memoryUpdates).Count
            promotionCandidates = @($promotionCandidates).Count
            failureReason = $failureReasonCode
        })))

    $commitResult = $emptyCommit
    if (-not $failureTriggered -and $touchedTargetFiles.Count -gt 0) {
        try {
            $commitResult = Invoke-RuleHarnessCommit -RepoRoot $RepoRoot -Config $Config -TargetFiles @($touchedTargetFiles) -AppliedBatches @($appliedBatches)
            if ($commitResult.created) {
                foreach ($batch in $appliedBatchObjects) {
                    [void](Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint ([string]$batch.fingerprint) -Status 'applied' -Reason 'applied')
                }
            }
            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'commit' `
                -Status 'passed' `
                -Attempted $true `
                -Summary $(if ($commitResult.created) { "Created commit $($commitResult.sha)." } else { 'Commit stage ran without creating a new commit.' }) `
                -Details ([pscustomobject]@{
                    branch = $commitResult.branch
                    message = $commitResult.message
                })))
        }
        catch {
            Restore-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -Snapshots $globalSnapshots
            $rollbackPerformed = $true
            $failureTriggered = $true
            $finalStaticFindings = @($InitialStaticFindings)
            foreach ($appliedBatch in $appliedBatches) {
                [void](Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint ([string]($appliedBatchObjects | Where-Object id -eq $appliedBatch.id | Select-Object -First 1).fingerprint) -Status 'failed' -Reason 'commit-failed')
                [void]$rollbackBatches.Add([pscustomobject]@{ id = $appliedBatch.id; kind = $appliedBatch.kind; status = 'rolled_back' })
            }
            $appliedBatches.Clear()
            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'commit' `
                -Status 'failed' `
                -Attempted $true `
                -Summary 'Commit stage failed and changes were rolled back.' `
                -Details ([pscustomobject]@{
                    message = $_.Exception.Message
                })))
        }
    }
    elseif (-not $failureTriggered) {
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'commit' `
            -Status 'skipped' `
            -Attempted $false `
            -Summary 'Commit stage was skipped because no batch touched the workspace.' `
            -Details $null))
    }
    else {
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'commit' `
            -Status 'skipped' `
            -Attempted $false `
            -Summary 'Commit stage was skipped because mutation failed before commit.' `
            -Details ([pscustomobject]@{
                failureReason = $failureReasonCode
            })))
    }

    Save-RuleHarnessHistoryState -HistoryState $historyState
    $historySummary = [pscustomobject]@{
        statePath         = $historyState.path
        loadedEntryCount  = $historyState.loadedEntryCount
        activeEntryCount  = @($historyState.entries.Keys).Count
        gcRemovedCount    = $historyState.gcRemovedCount
        touchedEntryCount = $historyState.touchedKeys.Count
    }

    return (New-RuleHarnessMutationPlanResult `
        -StageResults @($stageResults) `
        -ActionItems @($actionItems) `
        -DecisionTrace @($decisionTrace) `
        -ValidationResults @($validationResults) `
        -AppliedBatches @($appliedBatches) `
        -SkippedBatches @($skippedBatches) `
        -RollbackBatches @($rollbackBatches) `
        -DocEdits @($docEdits) `
        -FinalStaticFindings @($finalStaticFindings) `
        -HistorySummary $historySummary `
        -Commit $commitResult `
        -RollbackPerformed $rollbackPerformed `
        -Failed $failureTriggered `
        -DiscoveredValidationPlan @($discoveredValidationPlans) `
        -LearningTrace @($learningTrace) `
        -MemoryHits @($memoryHits) `
        -MemoryUpdates @($memoryUpdates) `
        -PromotionCandidates @($promotionCandidates) `
        -RetryAttempts $retryAttempts)
}

function Write-RuleHarnessSummary {
    param(
        [Parameter(Mandatory)]
        [object]$Report,
        [Parameter(Mandatory)]
        [string]$SummaryPath
    )

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add('## Rule Harness')
    [void]$lines.Add('')
    [void]$lines.Add(('- Commit SHA: `{0}`' -f $Report.commitSha))
    [void]$lines.Add("- Branch: $($Report.execution.branch)")
    [void]$lines.Add("- Dry run: $($Report.execution.dryRun)")
    [void]$lines.Add("- LLM enabled: $($Report.execution.llmEnabled)")
    [void]$lines.Add("- LLM model: $($Report.execution.llmModel)")
    [void]$lines.Add("- Mutation enabled: $($Report.execution.mutationEnabled)")
    [void]$lines.Add("- Mutation mode: $($Report.execution.mutationMode)")
    [void]$lines.Add("- Compile verified: $($Report.execution.compileVerified)")
    [void]$lines.Add("- Clean level: $($Report.execution.cleanLevel)")
    [void]$lines.Add("- Compile gate status: $($Report.execution.compileGateStatus)")
    [void]$lines.Add("- Feature dependency gate status: $($Report.execution.featureDependencyGateStatus)")
    [void]$lines.Add("- Feature dependency cycles: $($Report.execution.featureDependencyCycleCount)")
    [void]$lines.Add("- Feature dependency repair status: $($Report.execution.featureDependencyRepairStatus)")
    [void]$lines.Add("- Feature dependency repair attempts: $($Report.execution.featureDependencyRepairAttemptCount)")
    [void]$lines.Add("- Feature dependency unsupported cycles: $($Report.execution.featureDependencyUnsupportedCycleCount)")
    [void]$lines.Add("- Scanned features: $($Report.scannedFeatures.Count)")
    [void]$lines.Add("- Completed scopes: $($Report.completedScopes.Count)")
    [void]$lines.Add("- Findings: $($Report.findings.Count)")
    [void]$lines.Add("- Doc proposals: $($Report.docProposals.Count)")
    [void]$lines.Add("- Doc edits: $($Report.docEdits.Count)")
    [void]$lines.Add("- Planned batches: $($Report.plannedBatches.Count)")
    [void]$lines.Add("- Applied batches: $($Report.appliedBatches.Count)")
    [void]$lines.Add("- Skipped batches: $($Report.skippedBatches.Count)")
    [void]$lines.Add("- Rollback performed: $($Report.rollback.performed)")
    [void]$lines.Add("- Commit created: $($Report.commit.created)")
    [void]$lines.Add("- Retry attempts: $($Report.retryAttempts)")
    [void]$lines.Add("- Memory hits: $($Report.memoryHits.Count)")
    [void]$lines.Add("- Memory updates: $($Report.memoryUpdates.Count)")
    [void]$lines.Add("- Promotion candidates: $($Report.promotionCandidates.Count)")
    [void]$lines.Add("- Applied: $($Report.applied)")
    [void]$lines.Add("- Failed: $($Report.failed)")
    [void]$lines.Add('')

    if ($Report.stageResults.Count -gt 0) {
        [void]$lines.Add('### Stage Status')
        foreach ($stageResult in $Report.stageResults) {
            $attemptText = if ($stageResult.attempted) { 'attempted' } else { 'not-attempted' }
            [void]$lines.Add(('- `{0}` [{1}/{2}] {3}' -f $stageResult.stage, $stageResult.status, $attemptText, $stageResult.summary))
        }
        [void]$lines.Add('')
    }

    if ($Report.actionItems.Count -gt 0) {
        [void]$lines.Add('### Next Actions')
        foreach ($item in @($Report.actionItems | Select-Object -First 5)) {
            [void]$lines.Add(('- [{0}] {1}' -f $item.severity, $item.summary))
            [void]$lines.Add(('  {0}' -f $item.details))
        }
        [void]$lines.Add('')
    }

    if ($null -ne $Report.stoppedScope) {
        [void]$lines.Add('### Stopped Scope')
        [void]$lines.Add(('- `{0}` findings={1} severity={2} plannedBatches={3} docProposals={4} finalStatus={5} remainingFindings={6} resolvedInRun={7}' -f [string]$Report.stoppedScope.scopeId, [int]$Report.stoppedScope.findingCount, [string]$Report.stoppedScope.highestSeverity, [int]$Report.stoppedScope.plannedBatchCount, [int]$Report.stoppedScope.docProposalCount, [string]$Report.stoppedScope.finalStatus, [int]$Report.stoppedScope.remainingFindingCount, [bool]$Report.stoppedScope.resolvedInRun))
        [void]$lines.Add('')
    }

    if ($Report.promotionCandidates.Count -gt 0) {
        [void]$lines.Add('### Promotion Candidates')
        foreach ($candidate in @($Report.promotionCandidates | Select-Object -First 5)) {
            [void]$lines.Add(('- `{0}` -> `{1}` ({2} runs / {3} commits)' -f $candidate.signature, $candidate.targetDoc, $candidate.hitCount, $candidate.distinctCommitCount))
        }
        [void]$lines.Add('')
    }

    if ($Report.featureDependencyRepairSummaries.Count -gt 0) {
        [void]$lines.Add('### Feature Dependency Repair')
        foreach ($summary in @($Report.featureDependencyRepairSummaries | Select-Object -First 5)) {
            [void]$lines.Add(('- `{0}` recipe={1} status={2} remainingCycles={3}' -f [string]$summary.cyclePath, [string]$summary.recipe, [string]$summary.status, [string]$summary.remainingCycles))
        }
        [void]$lines.Add('')
    }

    if ($Report.decisionTrace.Count -gt 0) {
        [void]$lines.Add('### Decision Trace')
        foreach ($entry in $Report.decisionTrace) {
            [void]$lines.Add("- $entry")
        }
        [void]$lines.Add('')
    }

    foreach ($severity in @('high', 'medium', 'low')) {
        $subset = @($Report.findings | Where-Object severity -eq $severity)
        if ($subset.Count -eq 0) {
            continue
        }

        [void]$lines.Add("### $severity")
        foreach ($finding in $subset) {
            [void]$lines.Add(('- [{0}] {1} - `{2}` ({3})' -f $finding.findingType, $finding.title, $finding.ownerDoc, $finding.remediationKind))
        }
        [void]$lines.Add('')
    }

    Set-Content -Path $SummaryPath -Value ($lines -join "`n") -Encoding UTF8
}

function Invoke-RuleHarness {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$ConfigPath,
        [string]$ApiKey,
        [string]$ApiBaseUrl,
        [string]$Model,
        [string]$MutationMode,
        [switch]$EnableMutation,
        [switch]$DisableMutation,
        [switch]$DryRun,
        [switch]$DisableLlm,
        [string]$ReviewJsonPath,
        [string]$SummaryPath,
        [string]$ReportPathHint,
        [string]$LogPathHint
    )

    $config = Get-RuleHarnessConfig -ConfigPath $ConfigPath
    if ([string]::IsNullOrWhiteSpace($Model)) {
        $Model = $config.llm.defaultModel
    }
    $timeoutSec = if ($config.llm.PSObject.Properties.Name -contains 'requestTimeoutSec') {
        [int]$config.llm.requestTimeoutSec
    }
    else {
        120
    }

    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        if ($Model -match '^glm-' -and -not [string]::IsNullOrWhiteSpace($env:GLM_API_KEY)) {
            $ApiKey = $env:GLM_API_KEY
        }
        elseif (-not [string]::IsNullOrWhiteSpace($env:RULE_HARNESS_API_KEY)) {
            $ApiKey = $env:RULE_HARNESS_API_KEY
        }
    }

    if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
        if (-not [string]::IsNullOrWhiteSpace($env:RULE_HARNESS_API_BASE_URL)) {
            $ApiBaseUrl = $env:RULE_HARNESS_API_BASE_URL
        }
        elseif ($Model -match '^glm-') {
            $ApiBaseUrl = $config.llm.glmApiBaseUrl
        }
        else {
            $ApiBaseUrl = $config.llm.defaultApiBaseUrl
        }
    }

    if ($DisableLlm) {
        $ApiKey = $null
    }

    $llmEnabled = -not [string]::IsNullOrWhiteSpace($ApiKey)
    $mutationState = Get-RuleHarnessMutationState -Config $config -MutationMode $MutationMode -EnableMutation:$EnableMutation -DisableMutation:$DisableMutation
    $harnessErrors = [System.Collections.Generic.List[object]]::new()
    $stageResults = [System.Collections.Generic.List[object]]::new()
    $runId = [guid]::NewGuid().ToString()
    $featureScanState = Read-RuleHarnessFeatureScanState -RepoRoot $RepoRoot -Config $config
    $docProposalBacklog = Read-RuleHarnessDocProposalBacklog -RepoRoot $RepoRoot -Config $config
    $maxScopesPerRun = if ($config.scan.PSObject.Properties.Name -contains 'maxScopesPerRun') { [int]$config.scan.maxScopesPerRun } else { 1 }
    $orderedScopes = @(Get-RuleHarnessOrderedFeatureScopes -RepoRoot $RepoRoot -Config $config)
    $selectedScopes = @($orderedScopes | Select-Object -First $maxScopesPerRun)
    $attemptedScopes = [System.Collections.Generic.List[string]]::new()
    $completedScopes = [System.Collections.Generic.List[string]]::new()
    $findings = [System.Collections.Generic.List[object]]::new()
    $reviewedFindingCount = 0
    $staticFindingCount = 0
    $plannedBatchCount = 0
    $docProposals = [System.Collections.Generic.List[object]]::new()
    $scopeInfo = $null
    $stoppedScope = $null
    $plannedBatches = @()
    $unplannedScopeFindings = @()
    $scopeStaticFindingsForMutation = @()
    $mutationResult = $null
    $diagnoseAttempted = $llmEnabled -or -not [string]::IsNullOrWhiteSpace($ReviewJsonPath)
    $diagnoseFailed = $false
    $preMutationActionItems = [System.Collections.Generic.List[object]]::new()
    $resolvedProposalCount = 0
    $reactivatedProposalCount = 0
    $sameRunResolvedScopeCount = 0
    $staleStateRepairedCount = 0
    $compileGateStatus = $null
    $rulesOnlyRecurrenceCloseoutStatus = $null
    $policyStaticFindings = @(Get-RuleHarnessHardcodedMcpUiSmokeFindings -RepoRoot $RepoRoot -Config $config)
    $staticFindingCount += $policyStaticFindings.Count
    foreach ($finding in @($policyStaticFindings)) {
        [void]$findings.Add($finding)
    }

    Write-Host 'Rule harness discover stage started.'
    Write-Host "Rule harness discover stage finished. Features: $($orderedScopes.Count) Selected: $($selectedScopes.Count)"
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'discover' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Selected {0} feature scope(s) from {1} total feature(s)." -f $selectedScopes.Count, $orderedScopes.Count) `
        -Details ([pscustomobject]@{
            featureCount = $orderedScopes.Count
            selectedScopes = @($selectedScopes | ForEach-Object { $_.scopeId })
        })))
    foreach ($scope in @($selectedScopes)) {
        [void]$attemptedScopes.Add([string]$scope.scopeId)
        $previousScopeState = if ($featureScanState.entries.ContainsKey([string]$scope.scopeId)) { $featureScanState.entries[[string]$scope.scopeId] } else { $null }
        $scopeStaticFindings = @(Get-RuleHarnessStaticFindings -RepoRoot $RepoRoot -Config $config -ScopeId ([string]$scope.scopeId))
        $staticFindingCount += $scopeStaticFindings.Count

        try {
            $scopeReviewedFindings = @(
                Invoke-RuleHarnessAgentReview `
                    -StaticFindings $scopeStaticFindings `
                    -RepoRoot $RepoRoot `
                    -Config $config `
                    -ApiKey $ApiKey `
                    -ApiBaseUrl $ApiBaseUrl `
                    -Model $Model `
                    -TimeoutSec $timeoutSec `
                    -ReviewJsonPath $ReviewJsonPath
            )
        }
        catch {
            $diagnoseFailed = $true
            [void]$harnessErrors.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $config.severityPolicy.agentFailure `
                -OwnerDoc 'tools/rule-harness/README.md' `
                -Title 'Rule harness agent review failed' `
                -Message "Scope=$($scope.scopeId) Stage=diagnose TimeoutSec=$timeoutSec $($_.Exception.Message)" `
                -Evidence @([pscustomobject]@{ path = 'tools/rule-harness'; line = $null; snippet = $_.Exception.GetType().FullName }) `
                -Confidence 'high' `
                -Source 'harness' `
                -RemediationKind 'report_only'))
            $scopeReviewedFindings = @(ConvertTo-RuleHarnessReviewedFindings -Findings $scopeStaticFindings)
        }

        $reviewedFindingCount += @($scopeReviewedFindings).Count
        foreach ($finding in @($scopeReviewedFindings)) {
            [void]$findings.Add($finding)
        }

        $scopeErrors = @($scopeReviewedFindings | Where-Object { $_.severity -in @('high', 'medium') })
        if ($scopeErrors.Count -eq 0) {
            $cleanupResult = Resolve-RuleHarnessDocProposalBacklogForScope `
                -Backlog $docProposalBacklog `
                -ScopeId ([string]$scope.scopeId) `
                -CurrentFindings @() `
                -RunId $runId
            $resolvedProposalCount += [int]$cleanupResult.resolvedCount
            if ($null -ne $previousScopeState -and (
                [string]$previousScopeState.lastResult -ne 'clean' -or
                -not [string]::IsNullOrWhiteSpace([string]$previousScopeState.lastFindingSeverity) -or
                -not [string]::IsNullOrWhiteSpace([string]$previousScopeState.lastStoppedReason)
            )) {
                $staleStateRepairedCount++
            }
            [void]$completedScopes.Add([string]$scope.scopeId)
            Set-RuleHarnessFeatureScanEntry `
                -FeatureScanState $featureScanState `
                -ScopeId ([string]$scope.scopeId) `
                -LastResult 'clean' `
                -LastFindingSeverity $null `
                -LastRunId $runId `
                -LastCommitSha ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim() `
                -LastStoppedReason $null
            continue
        }

        $scopeDocProposals = @(
            $scopeErrors |
                Where-Object { $_.severity -in @('high', 'medium') -and -not [string]::IsNullOrWhiteSpace([string]$_.ownerDoc) } |
                ForEach-Object { ConvertTo-RuleHarnessDocProposal -Finding $_ -ScopeId ([string]$scope.scopeId) }
        )
        foreach ($proposal in @($scopeDocProposals)) {
            [void]$docProposals.Add($proposal)
        }
        $backlogUpdate = Update-RuleHarnessDocProposalBacklog -Backlog $docProposalBacklog -Proposals @($scopeDocProposals) -RunId $runId
        $reactivatedProposalCount += [int]$backlogUpdate.reactivatedCount

        Write-Host "Rule harness patch plan stage started for scope $($scope.scopeId)."
        $plannedBatches = @(Get-RuleHarnessPlannedBatches -ReviewedFindings @($scopeErrors) -DocEdits @() -RepoRoot $RepoRoot)
        $plannedBatchCount = @($plannedBatches).Count
        $unplannedScopeFindings = @(Get-RuleHarnessUnplannedFindings -ReviewedFindings @($scopeErrors) -PlannedBatches @($plannedBatches))
        $scopeOwnerDoc = @(
            @($scopeErrors | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.ownerDoc) } | Select-Object -ExpandProperty ownerDoc) |
                Select-Object -First 1
        )
        $scopeInfo = Get-RuleHarnessScopeInfo `
            -RepoRoot $RepoRoot `
            -OwnerDoc $(if ($scopeOwnerDoc.Count -gt 0) { [string]$scopeOwnerDoc[0] } else { $null }) `
            -TargetFiles @($plannedBatches | ForEach-Object { @($_.targetFiles) } | Sort-Object -Unique)
        foreach ($unplannedFinding in @($unplannedScopeFindings)) {
            [void]$preMutationActionItems.Add((Get-RuleHarnessActionItemForUnplannedFinding -Finding $unplannedFinding))
        }
        Write-Host "Rule harness patch plan stage finished for scope $($scope.scopeId). Planned batches: $plannedBatchCount"

        $scopeStaticFindingsForMutation = @($scopeStaticFindings)
        $mutationResult = Invoke-RuleHarnessMutationPlan `
            -PlannedBatches $plannedBatches `
            -InitialStaticFindings $scopeStaticFindingsForMutation `
            -RepoRoot $RepoRoot `
            -Config $config `
            -MutationState $mutationState `
            -ScopeInfo $scopeInfo `
            -StaticScanScopeId ([string]$scope.scopeId) `
            -RelaxScopeGuards `
            -DryRun:$DryRun

        $remainingScopeFindings = @(
            @($mutationResult.finalStaticFindings) |
                Where-Object {
                    $_.severity -in @('high', 'medium') -and
                    (Test-RuleHarnessFindingMatchesScope -Finding $_ -ScopeId ([string]$scope.scopeId))
                }
        )
        $cleanupResult = Resolve-RuleHarnessDocProposalBacklogForScope `
            -Backlog $docProposalBacklog `
            -ScopeId ([string]$scope.scopeId) `
            -CurrentFindings @($remainingScopeFindings) `
            -RunId $runId
        $resolvedProposalCount += [int]$cleanupResult.resolvedCount

        $scopeResult = if ([bool]$mutationResult.failed) {
            'failed'
        }
        elseif ($remainingScopeFindings.Count -eq 0) {
            'clean'
        }
        else {
            'findings'
        }
        if ($scopeResult -eq 'clean') {
            $sameRunResolvedScopeCount++
            if ($null -ne $previousScopeState -and (
                [string]$previousScopeState.lastResult -ne 'clean' -or
                -not [string]::IsNullOrWhiteSpace([string]$previousScopeState.lastFindingSeverity) -or
                -not [string]::IsNullOrWhiteSpace([string]$previousScopeState.lastStoppedReason)
            )) {
                $staleStateRepairedCount++
            }
        }
        $scopeCommitSha = ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim()
        Set-RuleHarnessFeatureScanEntry `
            -FeatureScanState $featureScanState `
            -ScopeId ([string]$scope.scopeId) `
            -LastResult $scopeResult `
            -LastFindingSeverity $(if ($scopeResult -eq 'clean') { $null } elseif ($remainingScopeFindings.Count -gt 0) { Get-RuleHarnessHighestSeverity -Findings @($remainingScopeFindings) } else { Get-RuleHarnessHighestSeverity -Findings @($scopeErrors) }) `
            -LastRunId $runId `
            -LastCommitSha $scopeCommitSha `
            -LastStoppedReason $(if ($scopeResult -eq 'clean') { $null } else { $scopeResult })

        $stoppedScope = [pscustomobject]@{
            scopeId             = [string]$scope.scopeId
            findingCount        = $scopeErrors.Count
            highestSeverity     = Get-RuleHarnessHighestSeverity -Findings @($scopeErrors)
            plannedBatchCount   = $plannedBatchCount
            docProposalCount    = @($scopeDocProposals).Count
            finalStatus         = $scopeResult
            remainingFindingCount = $remainingScopeFindings.Count
            resolvedInRun       = ($scopeResult -eq 'clean')
        }
        break
    }

    Save-RuleHarnessFeatureScanState -FeatureScanState $featureScanState
    Save-RuleHarnessDocProposalBacklog -Backlog $docProposalBacklog

    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'static_scan' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Scanned {0} scope(s) and produced {1} finding(s)." -f $attemptedScopes.Count, $staticFindingCount) `
        -Details ([pscustomobject]@{
            scopeCount = $attemptedScopes.Count
            findingCount = $staticFindingCount
        })))

    $diagnoseStatus = if ($diagnoseFailed) { 'failed' } elseif ($diagnoseAttempted) { 'passed' } else { 'skipped' }
    $diagnoseSummary = if ($diagnoseFailed) {
        'Diagnose stage failed for at least one scope; static findings were used as fallback.'
    }
    elseif ($diagnoseAttempted) {
        "Diagnose stage reviewed $reviewedFindingCount finding(s)."
    }
    else {
        'Diagnose stage used static findings because LLM review was disabled.'
    }
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'diagnose' `
        -Status $diagnoseStatus `
        -Attempted $diagnoseAttempted `
        -Summary $diagnoseSummary `
        -Details ([pscustomobject]@{
            reviewedFindingCount = $reviewedFindingCount
            llmEnabled = [bool]$llmEnabled
            reviewJsonPath = $ReviewJsonPath
        })))

    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'doc_proposals' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Doc mutation is disabled. Generated {0} proposal(s)." -f $docProposals.Count) `
        -Details ([pscustomobject]@{
            docProposalCount = $docProposals.Count
        })))

    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'state_cleanup' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Resolved {0} proposal(s), reactivated {1}, repaired {2} stale state entrie(s), same-run resolved scopes={3}." -f $resolvedProposalCount, $reactivatedProposalCount, $staleStateRepairedCount, $sameRunResolvedScopeCount) `
        -Details ([pscustomobject]@{
            resolvedProposalCount = $resolvedProposalCount
            reactivatedProposalCount = $reactivatedProposalCount
            sameRunResolvedScopeCount = $sameRunResolvedScopeCount
            staleStateRepairedCount = $staleStateRepairedCount
        })))

    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'patch_plan' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Patch plan produced {0} batch(es)." -f $plannedBatchCount) `
        -Details ([pscustomobject]@{
            plannedBatchCount = $plannedBatchCount
            unplannedFindingCount = @($unplannedScopeFindings).Count
            scopeGuard = if ($null -ne $scopeInfo) { [string]$scopeInfo.scopeGuard } else { 'default' }
            stoppedScope = if ($null -ne $stoppedScope) { [string]$stoppedScope.scopeId } else { $null }
        })))

    $rulesOnlyRecurrenceCloseoutStatus = Get-RuleHarnessRulesOnlyRecurrenceCloseoutStatus `
        -RepoRoot $RepoRoot `
        -ScopeInfo $scopeInfo `
        -TargetFiles @($plannedBatches | ForEach-Object { @($_.targetFiles) } | Sort-Object -Unique)
    [void]$stageResults.Add($rulesOnlyRecurrenceCloseoutStatus.stageResult)

    $featureDependencyRefresh = Invoke-RuleHarnessFeatureDependencyReportRefresh -RepoRoot $RepoRoot
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'feature_dependency_refresh' `
        -Status $(if ($featureDependencyRefresh.succeeded) { 'passed' } elseif ($featureDependencyRefresh.attempted) { 'failed' } else { 'skipped' }) `
        -Attempted ([bool]$featureDependencyRefresh.attempted) `
        -Summary $(if ($featureDependencyRefresh.succeeded) { 'Feature dependency report refreshed before gate evaluation.' } elseif ($featureDependencyRefresh.attempted) { 'Feature dependency report refresh failed before gate evaluation.' } else { 'Feature dependency report refresh script is not available in this repository snapshot.' }) `
        -Details $featureDependencyRefresh))

    $featureDependencyGateStatus = Get-RuleHarnessFeatureDependencyGateStatus -RepoRoot $RepoRoot -Config $config
    [void]$stageResults.Add($featureDependencyGateStatus.stageResult)

    $featureDependencyRepair = Invoke-RuleHarnessFeatureDependencyRepair -RepoRoot $RepoRoot -Config $config -DryRun:$DryRun
    foreach ($repairStage in @($featureDependencyRepair.stageResults)) {
        [void]$stageResults.Add($repairStage)
    }

    $featureDependencyGateStatus = Get-RuleHarnessFeatureDependencyGateStatus -RepoRoot $RepoRoot -Config $config
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'feature_dependency_gate_post_repair' `
        -Status $(if ([bool]$featureDependencyGateStatus.failed) { 'failed' } else { 'passed' }) `
        -Attempted ([bool]$featureDependencyRepair.attempted) `
        -Summary ("Post-repair feature dependency gate status={0} cycles={1}." -f [string]$featureDependencyGateStatus.featureDependencyGateStatus, [int]$featureDependencyGateStatus.featureDependencyCycleCount) `
        -Details ([pscustomobject]@{
            gateStatus = [string]$featureDependencyGateStatus.featureDependencyGateStatus
            cycleCount = [int]$featureDependencyGateStatus.featureDependencyCycleCount
            reportPath = [string]$featureDependencyGateStatus.reportPath
        })))

    $compileGateStatus = Get-RuleHarnessCompileGateStatus -RepoRoot $RepoRoot -Config $config
    [void]$stageResults.Add($compileGateStatus.stageResult)

    if ($null -eq $mutationResult) {
        $mutationResult = Invoke-RuleHarnessMutationPlan `
            -PlannedBatches @() `
            -InitialStaticFindings @() `
            -RepoRoot $RepoRoot `
            -Config $config `
            -MutationState $mutationState `
            -DryRun:$DryRun
    }

    $reportMemoryHits = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($mutationResult.memoryHits)) {
        [void]$reportMemoryHits.Add($entry)
    }
    foreach ($entry in @($featureDependencyRepair.memoryHits)) {
        [void]$reportMemoryHits.Add($entry)
    }
    $reportMemoryUpdates = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($mutationResult.memoryUpdates)) {
        [void]$reportMemoryUpdates.Add($entry)
    }
    foreach ($entry in @($featureDependencyRepair.memoryUpdates)) {
        [void]$reportMemoryUpdates.Add($entry)
    }
    $reportPromotionCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($mutationResult.promotionCandidates)) {
        [void]$reportPromotionCandidates.Add($entry)
    }
    foreach ($entry in @($featureDependencyRepair.promotionCandidates)) {
        [void]$reportPromotionCandidates.Add($entry)
    }

    $recurringFailurePromotion = Invoke-RuleHarnessRecurringFailurePromotion `
        -RepoRoot $RepoRoot `
        -Config $config `
        -PromotionCandidates @($reportPromotionCandidates) `
        -DryRun:$DryRun

    foreach ($promotionStage in @($recurringFailurePromotion.stageResults)) {
        [void]$stageResults.Add($promotionStage)
    }

    $reportLearningTrace = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($mutationResult.learningTrace)) {
        [void]$reportLearningTrace.Add($entry)
    }
    foreach ($entry in @($featureDependencyRepair.learningTrace)) {
        [void]$reportLearningTrace.Add($entry)
    }
    $reportCommitSha = ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim()

    if ($harnessErrors.Count -gt 0) {
        $harnessMemoryStore = Read-RuleHarnessMemoryStore -RepoRoot $RepoRoot -Config $config
        foreach ($harnessError in @($harnessErrors)) {
            $scopeInfo = Get-RuleHarnessScopeInfo -RepoRoot $RepoRoot -OwnerDoc ([string]$harnessError.ownerDoc) -TargetFiles @('tools/rule-harness')
            $signature = Get-RuleHarnessFailureSignature `
                -FailureReason 'harness-stage-failed' `
                -PrimarySource ([string]$harnessError.title) `
                -OwnerDoc $scopeInfo.scopePath `
                -BatchKind 'harness' `
                -Message ([string]$harnessError.message)
            $matchedMemoryEntry = Find-RuleHarnessMemoryEntry -MemoryStore $harnessMemoryStore -Signature $signature -ScopePath $scopeInfo.scopePath
            if ($null -ne $matchedMemoryEntry) {
                [void]$reportMemoryHits.Add([pscustomobject]@{
                    batchId           = 'harness'
                    attempt           = 1
                    signature         = [string]$matchedMemoryEntry.signature
                    scopePath         = [string]$matchedMemoryEntry.scopePath
                    preferredStrategy = [string]$matchedMemoryEntry.preferredRepairStrategy
                    promotionTarget   = [string]$matchedMemoryEntry.promotionTarget
                })
            }

            $updatedEntry = Update-RuleHarnessMemoryStoreEntry `
                -MemoryStore $harnessMemoryStore `
                -Signature $signature `
                -ScopeType $scopeInfo.scopeType `
                -ScopePath $scopeInfo.scopePath `
                -Symptoms ([string]$harnessError.message) `
                -PreferredRepairStrategy 'Improve harness prompts, config, tests, or docs before retrying.' `
                -ValidationHints @('tools/rule-harness/tests/Run-RuleHarnessTests.ps1', 'tools/rule-harness/README.md') `
                -Confidence 'high' `
                -CommitSha $reportCommitSha `
                -PromotionTarget $scopeInfo.promotionTarget `
                -Status 'observed'
            [void]$reportMemoryUpdates.Add([pscustomobject]@{
                batchId             = 'harness'
                signature           = [string]$updatedEntry.signature
                scopeType           = [string]$updatedEntry.scopeType
                scopePath           = [string]$updatedEntry.scopePath
                confidence          = [string]$updatedEntry.confidence
                hitCount            = [int]$updatedEntry.hitCount
                distinctCommitCount = [int]$updatedEntry.distinctCommitCount
                promotionTarget     = [string]$updatedEntry.promotionTarget
                status              = [string]$updatedEntry.status
            })
            [void]$reportLearningTrace.Add([pscustomobject]@{
                batchId                    = 'harness'
                attempt                    = 1
                normalizedFailureSignature = $signature
                memoryEntriesUsed          = if ($null -ne $matchedMemoryEntry) { @([string]$matchedMemoryEntry.signature) } else { @() }
                repairDelta                = 'harness-learning-recorded'
                verificationResult         = 'failed'
            })

            $promotionCandidate = Get-RuleHarnessPromotionCandidate -Entry $updatedEntry -Rationale ([string]$harnessError.title) -Config $config
            if ($null -ne $promotionCandidate) {
                [void]$reportPromotionCandidates.Add($promotionCandidate)
            }
        }

        if (-not $DryRun -and $harnessMemoryStore.dirty) {
            Save-RuleHarnessMemoryStore -MemoryStore $harnessMemoryStore
        }
    }

    $reportFindings = @($findings + $featureDependencyGateStatus.findings + @($featureDependencyRepair.unsupportedFindings))
    if ($null -ne $stoppedScope) {
        $reportFindings = @(
            @($reportFindings | Where-Object { -not (Test-RuleHarnessFindingMatchesScope -Finding $_ -ScopeId ([string]$stoppedScope.scopeId)) }) +
            @($mutationResult.finalStaticFindings)
        )
    }

    $reportFindings = @($reportFindings + $harnessErrors)
    $failed = (@($reportFindings | Where-Object { $_.severity -eq 'high' -and $_.findingType -eq 'code_violation' }).Count -gt 0) -or
        ($harnessErrors.Count -gt 0) -or
        [bool]$mutationResult.failed
    $docProposalPath = if (-not [string]::IsNullOrWhiteSpace($ReportPathHint)) {
        Join-Path (Split-Path -Parent $ReportPathHint) 'rule-harness-doc-proposals.md'
    }
    elseif (-not [string]::IsNullOrWhiteSpace($SummaryPath)) {
        Join-Path (Split-Path -Parent $SummaryPath) 'rule-harness-doc-proposals.md'
    }
    else {
        Join-Path (Join-Path $RepoRoot 'Temp/RuleHarness') 'rule-harness-doc-proposals.md'
    }
    $reportActionItems = @(Merge-RuleHarnessActionItems -Items @(
        @($preMutationActionItems) +
        $(if ($null -ne $rulesOnlyRecurrenceCloseoutStatus) { @($rulesOnlyRecurrenceCloseoutStatus.actionItems) } else { @() }) +
        @($featureDependencyGateStatus.actionItems) +
        @($featureDependencyRepair.actionItems) +
        @($compileGateStatus.actionItems) +
        @($mutationResult.actionItems) +
        @($recurringFailurePromotion.actionItems) +
        @($reportPromotionCandidates | ForEach-Object {
            New-RuleHarnessActionItem `
                -Kind 'promote-durable-rule' `
                -Severity 'high' `
                -Summary ("Promote recurring failure guidance to {0}" -f [string]$_.targetDoc) `
                -Details ("Signature {0} repeated across {1} runs and {2} commits. Capture the durable rule in the owning doc." -f [string]$_.signature, [int]$_.hitCount, [int]$_.distinctCommitCount) `
                -RelatedPaths @([string]$_.targetDoc)
        }) +
        @(Get-RuleHarnessActionItemsForFindings `
            -Findings $reportFindings `
            -ReportPathHint $ReportPathHint `
            -SummaryPathHint $SummaryPath `
            -LogPathHint $LogPathHint)
    ))
    $allStageResults = @($stageResults) + @($mutationResult.stageResults)
    $nextScopeCandidates = @((Get-RuleHarnessOrderedFeatureScopes -RepoRoot $RepoRoot -Config $config | Select-Object -First 5) | ForEach-Object { $_.scopeId })

    $report = [pscustomobject]@{
        runId            = $runId
        commitSha        = $reportCommitSha
        execution        = [pscustomobject]@{
            dryRun          = [bool]$DryRun
            llmEnabled      = [bool]$llmEnabled
            llmModel        = if ($llmEnabled) { $Model } else { $null }
            llmApiBaseUrl   = if ($llmEnabled) { $ApiBaseUrl } else { $null }
            llmTimeoutSec   = if ($llmEnabled) { $timeoutSec } else { $null }
            mutationEnabled = [bool]$mutationState.enabled
            mutationMode    = $mutationState.mode
            branch          = Get-RuleHarnessCurrentBranch -RepoRoot $RepoRoot
            statePath       = $mutationResult.historySummary.statePath
            reportPath      = $ReportPathHint
            summaryPath     = $SummaryPath
            logPath         = $LogPathHint
            docProposalPath = $docProposalPath
            compileVerified = [bool]$compileGateStatus.compileVerified
            cleanLevel      = [string]$compileGateStatus.cleanLevel
            compileGateStatus = [string]$compileGateStatus.compileGateStatus
            compileGateReasonCode = [string]$compileGateStatus.compileGateReasonCode
            compileStatusPath = [string]$compileGateStatus.statusPath
            featureDependencyGateStatus = [string]$featureDependencyGateStatus.featureDependencyGateStatus
            featureDependencyCycleCount = [int]$featureDependencyGateStatus.featureDependencyCycleCount
            featureDependencyReportPath = [string]$featureDependencyGateStatus.reportPath
            featureDependencyRepairStatus = [string]$featureDependencyRepair.status
            featureDependencyRepairAttemptCount = [int]$featureDependencyRepair.attemptCount
            featureDependencyUnsupportedCycleCount = [int]$featureDependencyRepair.unsupportedCycleCount
        }
        scannedFeatures   = @($attemptedScopes)
        scannedScopes     = @($attemptedScopes)
        completedScopes   = @($completedScopes)
        stoppedScope      = $stoppedScope
        nextScopeCandidates = @($nextScopeCandidates)
        findings          = @($reportFindings)
        docProposals      = @($docProposals)
        docEdits          = @($mutationResult.docEdits)
        plannedBatches    = @($plannedBatches | ForEach-Object {
            [pscustomobject]@{
                id                       = $_.id
                kind                     = $_.kind
                targetFiles              = $_.targetFiles
                reason                   = $_.reason
                validation               = $_.validation
                expectedFindingsResolved = $_.expectedFindingsResolved
                status                   = $_.status
                riskScore                = $_.riskScore
                riskLabel                = $_.riskLabel
                fingerprint              = $_.fingerprint
                ownershipStatus          = $_.ownershipStatus
            }
        })
        appliedBatches    = @($mutationResult.appliedBatches)
        skippedBatches    = @($mutationResult.skippedBatches)
        rollbackBatches   = @($mutationResult.rollbackBatches)
        stageResults      = @($allStageResults)
        actionItems       = @($reportActionItems)
        decisionTrace     = @($mutationResult.decisionTrace)
        validationResults = @(@($mutationResult.validationResults) + @($featureDependencyRepair.validationResults))
        discoveredValidationPlan = @($mutationResult.discoveredValidationPlan)
        learningTrace     = @($reportLearningTrace) + @($recurringFailurePromotion.learningTrace)
        memoryHits        = @($reportMemoryHits)
        memoryUpdates     = @($reportMemoryUpdates) + @($recurringFailurePromotion.memoryUpdates)
        promotionCandidates = @($reportPromotionCandidates)
        featureDependencyRepairSummaries = @($featureDependencyRepair.summaries)
        featureDependencyRepairCodeCommits = @($featureDependencyRepair.codeCommits)
        featureDependencyRepairDocCommits = @($featureDependencyRepair.docCommits)
        featureDependencyRepairPolicySnapshot = $featureDependencyRepair.policySnapshot
        retryAttempts     = [int]$mutationResult.retryAttempts
        historySummary    = $mutationResult.historySummary
        commit            = $mutationResult.commit
        rollback          = $mutationResult.rollback
        applied           = [bool]$mutationResult.applied
        failed            = ([bool]$failed -or [bool]$featureDependencyGateStatus.failed -or [bool]$compileGateStatus.failed -or [bool]$featureDependencyRepair.failed)
    }

    if (-not [string]::IsNullOrWhiteSpace($docProposalPath)) {
        Write-RuleHarnessDocProposalFile -Proposals @($docProposals) -Path $docProposalPath
    }

    if (-not [string]::IsNullOrWhiteSpace($SummaryPath)) {
        Write-RuleHarnessSummary -Report $report -SummaryPath $SummaryPath
    }

    $report
}

function Test-RuleHarnessSafeRelativePath {
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $normalized = $Path.Replace('\', '/')
    if ($normalized.StartsWith('/') -or [System.IO.Path]::IsPathRooted($Path)) {
        return $false
    }

    foreach ($segment in @($normalized -split '/')) {
        if ($segment -eq '..') {
            return $false
        }
    }

    return $true
}

function Get-RuleHarnessProjectReviewSnapshot {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$ConfigPath,
        [switch]$AllScopes,
        [switch]$ReadOnly
    )

    $config = Get-RuleHarnessConfig -ConfigPath $ConfigPath
    $orderedScopes = @(Get-RuleHarnessOrderedFeatureScopes -RepoRoot $RepoRoot -Config $config)
    $selectedScopes = if ($AllScopes) {
        @($orderedScopes)
    }
    else {
        $maxScopesPerRun = if ($config.scan.PSObject.Properties.Name -contains 'maxScopesPerRun') { [int]$config.scan.maxScopesPerRun } else { 1 }
        @($orderedScopes | Select-Object -First $maxScopesPerRun)
    }

    $findings = [System.Collections.Generic.List[object]]::new()
    foreach ($policyFinding in @(Get-RuleHarnessHardcodedMcpUiSmokeFindings -RepoRoot $RepoRoot -Config $config)) {
        [void]$findings.Add($policyFinding)
    }

    foreach ($scope in @($selectedScopes)) {
        foreach ($finding in @(Get-RuleHarnessStaticFindings -RepoRoot $RepoRoot -Config $config -ScopeId ([string]$scope.scopeId))) {
            [void]$findings.Add($finding)
        }
    }

    $featureDependencyGateStatus = Get-RuleHarnessFeatureDependencyGateStatus -RepoRoot $RepoRoot -Config $config
    $compileGateStatus = Get-RuleHarnessCompileGateStatus -RepoRoot $RepoRoot -Config $config
    $commitSha = ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim()

    [pscustomobject]@{
        runId                 = [guid]::NewGuid().ToString()
        baseCommitSha         = $commitSha
        generatedAtUtc        = (Get-Date).ToUniversalTime().ToString('o')
        readOnly              = [bool]$ReadOnly
        allScopes             = [bool]$AllScopes
        scannedScopes         = @($selectedScopes | ForEach-Object { [string]$_.scopeId })
        totalScopeCount       = $orderedScopes.Count
        findings              = @($findings)
        featureDependencyGate = [pscustomobject]@{
            status      = [string]$featureDependencyGateStatus.featureDependencyGateStatus
            cycleCount  = [int]$featureDependencyGateStatus.featureDependencyCycleCount
            reportPath  = [string]$featureDependencyGateStatus.reportPath
            failed      = [bool]$featureDependencyGateStatus.failed
            findings    = @($featureDependencyGateStatus.findings)
            actionItems = @($featureDependencyGateStatus.actionItems)
        }
        compileGate           = [pscustomobject]@{
            status     = [string]$compileGateStatus.compileGateStatus
            reasonCode = [string]$compileGateStatus.compileGateReasonCode
            cleanLevel = [string]$compileGateStatus.cleanLevel
            verified   = [bool]$compileGateStatus.compileVerified
            failed     = [bool]$compileGateStatus.failed
            actionItems = @($compileGateStatus.actionItems)
        }
    }
}

function Get-RuleHarnessAgentRunnerSettings {
    param([Parameter(Mandatory)][object]$Config)

    $settings = if ($Config.PSObject.Properties.Name -contains 'agentRunner') { $Config.agentRunner } else { $null }
    [pscustomobject]@{
        enabled        = if ($null -ne $settings -and $settings.PSObject.Properties.Name -contains 'enabled') { [bool]$settings.enabled } else { $false }
        commandPath    = if ($null -ne $settings -and $settings.PSObject.Properties.Name -contains 'commandPath') { [string]$settings.commandPath } else { '' }
        model          = if ($null -ne $settings -and $settings.PSObject.Properties.Name -contains 'model') { [string]$settings.model } else { '' }
        reasoningEffort = if ($null -ne $settings -and $settings.PSObject.Properties.Name -contains 'reasoningEffort') { [string]$settings.reasoningEffort } else { 'low' }
        timeoutSec     = if ($null -ne $settings -and $settings.PSObject.Properties.Name -contains 'timeoutSec') { [int]$settings.timeoutSec } else { 180 }
        maxTasksPerRun = if ($null -ne $settings -and $settings.PSObject.Properties.Name -contains 'maxTasksPerRun') { [int]$settings.maxTasksPerRun } else { 1 }
    }
}

function New-RuleHarnessAgentWorkReport {
    param(
        [Parameter(Mandatory)][object]$Task,
        [Parameter(Mandatory)][string]$Status,
        [Parameter(Mandatory)][string]$Summary,
        [string]$BlockedReason = '',
        [string[]]$ChangedFiles = @(),
        [string]$OutputPath = '',
        [string]$LogPath = ''
    )

    [pscustomobject]@{
        taskId = [string]$Task.taskId
        status = $Status
        changedFiles = @($ChangedFiles)
        summary = $Summary
        blockedReason = $BlockedReason
        validationCommands = @()
        riskNotes = @()
        outputPath = $OutputPath
        logPath = $LogPath
    }
}

function Add-RuleHarnessAgentWorkState {
    param(
        [Parameter(Mandatory)][object]$Report,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$AgentWorkQueue,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$AgentWorkReports,
        [Parameter(Mandatory)][string]$WorkLabel
    )

    $existingSkipped = @($Report.skippedBatches)
    $agentSkipped = @(
        $AgentWorkReports |
            Where-Object { [string]$_.status -eq 'blocked' } |
            ForEach-Object {
                $taskId = [string]$_.taskId
                [pscustomobject]@{
                    id = $taskId
                    kind = 'agent_work'
                    reason = [string]$_.summary
                    reasonCode = [string]$_.blockedReason
                    status = 'blocked'
                    targets = @($AgentWorkQueue | Where-Object { [string]$_.taskId -eq $taskId } | ForEach-Object { @($_.candidateFiles) })
                }
            }
    )

    $existingStages = @($Report.stageResults)
    $agentStageStatus = if (@($AgentWorkReports | Where-Object { [string]$_.status -eq 'blocked' }).Count -gt 0) { 'blocked' } else { 'passed' }
    $agentStageSummary = if ($agentStageStatus -eq 'blocked') {
        "Coding agent runner blocked for $(@($AgentWorkQueue).Count) queued $WorkLabel task(s)."
    }
    elseif ($AgentWorkQueue.Count -gt 0) {
        "Queued $(@($AgentWorkQueue).Count) $WorkLabel task(s)."
    }
    else {
        "No $WorkLabel agent work was queued."
    }

    if ($agentStageStatus -eq 'blocked') {
        $Report | Add-Member -NotePropertyName 'failed' -NotePropertyValue $true -Force
    }
    $Report | Add-Member -NotePropertyName 'skippedBatches' -NotePropertyValue @($existingSkipped + $agentSkipped) -Force
    $Report | Add-Member -NotePropertyName 'stageResults' -NotePropertyValue @($existingStages + [pscustomobject]@{
        stage = 'agent_work'
        status = $agentStageStatus
        attempted = ($AgentWorkQueue.Count -gt 0)
        summary = $agentStageSummary
    }) -Force
    $Report | Add-Member -NotePropertyName 'agentWorkQueue' -NotePropertyValue @($AgentWorkQueue) -Force
    $Report | Add-Member -NotePropertyName 'agentWorkReports' -NotePropertyValue @($AgentWorkReports) -Force
}

function Resolve-RuleHarnessCodexCommand {
    param([string]$ConfiguredPath)

    if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
        $resolved = Resolve-Path -LiteralPath $ConfiguredPath -ErrorAction SilentlyContinue
        if ($null -ne $resolved) {
            return [string]$resolved.Path
        }
    }

    $command = Get-Command codex -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        return ''
    }

    [string]$command.Source
}

function ConvertTo-RuleHarnessProcessArgument {
    param([string]$Value)

    if ($null -eq $Value) {
        return "''"
    }

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    '"' + $Value.Replace('"', '\"') + '"'
}

function Invoke-RuleHarnessProcess {
    param(
        [Parameter(Mandatory)][string]$FileName,
        [Parameter(Mandatory)][AllowEmptyCollection()][string[]]$Arguments,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [Parameter(Mandatory)][int]$TimeoutSec
    )

    $argumentLine = (@($Arguments) | ForEach-Object { ConvertTo-RuleHarnessProcessArgument -Value ([string]$_) }) -join ' '
    $stdoutPath = [System.IO.Path]::GetTempFileName()
    $stderrPath = [System.IO.Path]::GetTempFileName()
    $process = Start-Process `
        -FilePath $FileName `
        -ArgumentList $argumentLine `
        -WorkingDirectory $WorkingDirectory `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath `
        -NoNewWindow `
        -PassThru

    $completed = $process.WaitForExit([Math]::Max(1, $TimeoutSec) * 1000)
    $stdout = if (Test-Path -LiteralPath $stdoutPath) { Get-Content -Path $stdoutPath -Raw } else { '' }
    $stderr = if (Test-Path -LiteralPath $stderrPath) { Get-Content -Path $stderrPath -Raw } else { '' }
    Remove-Item -LiteralPath $stdoutPath, $stderrPath -Force -ErrorAction SilentlyContinue
    if (-not $completed) {
        try { $process.Kill() } catch { }
        return [pscustomobject]@{
            exitCode = -1
            timedOut = $true
            stdout = $stdout
            stderr = $stderr
        }
    }

    [pscustomobject]@{
        exitCode = $process.ExitCode
        timedOut = $false
        stdout = $stdout
        stderr = $stderr
    }
}

function New-RuleHarnessAgentBatchPrompt {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$RoleName,
        [Parameter(Mandatory)][object]$Task,
        [switch]$StrictContract
    )

    $taskJson = $Task | ConvertTo-Json -Depth 40
    $snapshotLines = [System.Collections.Generic.List[string]]::new()
    foreach ($candidate in @($Task.candidateFiles | Select-Object -First 4)) {
        $relativePath = ([string]$candidate).Replace('\', '/')
        if ([string]::IsNullOrWhiteSpace($relativePath) -or -not (Test-RuleHarnessSafeRelativePath -Path $relativePath)) {
            continue
        }

        $fullPath = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            continue
        }

        $content = Get-Content -LiteralPath $fullPath -Raw
        $truncated = $false
        if ($content.Length -gt 60000) {
            $content = $content.Substring(0, 60000)
            $truncated = $true
        }

        [void]$snapshotLines.Add("### $relativePath")
        [void]$snapshotLines.Add("truncated: $truncated")
        [void]$snapshotLines.Add('```')
        [void]$snapshotLines.Add($content)
        [void]$snapshotLines.Add('```')
        [void]$snapshotLines.Add('')
    }
    $snapshotText = $snapshotLines -join [Environment]::NewLine
    $strictContractText = if ($StrictContract) {
        @"

Strict correction:
- A previous attempt violated the batch-synthesis contract.
- The read-only sandbox is expected and is not a blocker when snapshots are complete.
- Do not call apply_patch, shell commands, file-writing tools, or any extra inspection.
- Return a JSON batch with write_file operations, or block only for a real safety reason such as missing/truncated snapshots or unsafe targets.
"@
    }
    else {
        ''
    }
@"
You are the rule harness coding-agent batch synthesizer.

Return only JSON that matches the supplied output schema. Do not edit files directly.
Use only the task JSON and file snapshots below. Do not run shell commands or inspect extra files.
The final output must be a guarded mutation batch proposal.

Role: $RoleName

Hard constraints:
- Do not write, move, delete, or commit files.
- Propose at most one batch for this task.
- Use only repo-relative paths.
- Never use absolute paths or '..' path traversal.
- Target files must be within the task candidateFiles list. If candidateFiles is empty or insufficient, return no batches and add a blocked item.
- Use operation type 'write_file' with full replacement file content.
- Set batch status to 'proposed'.
- Use validation ['rule_harness_tests'] unless this is a rule_fix with a clearer existing validation.
- If you cannot make a small, safe, exact patch, return no batches and explain the blocker.
- `truncated: False` means the full file content is present. Only block for truncation when a snapshot explicitly says `truncated: True` or a required candidate file has no snapshot.
- The runner intentionally executes you in a read-only sandbox. That sandbox is normal and must not be reported as `read_only_sandbox` when the candidate snapshots are complete.
- Never call `apply_patch`, shell commands, file-writing tools, or additional file readers. Doing so is an agent-runner contract violation; synthesize the full replacement content directly in JSON instead.
$strictContractText

Task JSON:
$taskJson

Candidate file snapshots:
$snapshotText
"@
}

function Test-RuleHarnessAgentTaskSnapshotsComplete {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][object]$Task
    )

    $candidates = @($Task.candidateFiles | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($candidates.Count -eq 0 -or $candidates.Count -gt 4) {
        return $false
    }

    foreach ($candidate in $candidates) {
        $relativePath = ([string]$candidate).Replace('\', '/')
        if (-not (Test-RuleHarnessSafeRelativePath -Path $relativePath)) {
            return $false
        }

        $fullPath = Join-Path $RepoRoot $relativePath
        if (-not (Test-Path -LiteralPath $fullPath)) {
            return $false
        }

        $content = Get-Content -LiteralPath $fullPath -Raw
        if ($content.Length -gt 60000) {
            return $false
        }
    }

    $true
}

function Test-RuleHarnessAgentBatchTargets {
    param(
        [Parameter(Mandatory)][object]$Batch,
        [Parameter(Mandatory)][object]$Task
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $allowed = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($candidate in @($Task.candidateFiles)) {
        $normalizedCandidate = ([string]$candidate).Replace('\', '/')
        if (-not [string]::IsNullOrWhiteSpace($normalizedCandidate)) {
            [void]$allowed.Add($normalizedCandidate)
        }
    }

    foreach ($target in @($Batch.targetFiles)) {
        $normalized = ([string]$target).Replace('\', '/')
        if (-not (Test-RuleHarnessSafeRelativePath -Path $normalized)) {
            [void]$errors.Add("Unsafe target path '$target'.")
            continue
        }
        if (-not $allowed.Contains($normalized)) {
            [void]$errors.Add("Target path '$target' is outside task candidateFiles.")
        }
    }

    foreach ($operation in @($Batch.operations)) {
        $targetPath = ([string]$operation.targetPath).Replace('\', '/')
        if (-not (Test-RuleHarnessSafeRelativePath -Path $targetPath)) {
            [void]$errors.Add("Unsafe operation targetPath '$targetPath'.")
            continue
        }
        if (-not $allowed.Contains($targetPath)) {
            [void]$errors.Add("Operation targetPath '$targetPath' is outside task candidateFiles.")
        }
    }

    @($errors)
}

function Invoke-RuleHarnessAgentBatchRunner {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$ConfigPath,
        [Parameter(Mandatory)][string]$RoleName,
        [Parameter(Mandatory)][AllowEmptyCollection()][object[]]$AgentWorkQueue,
        [Parameter(Mandatory)][string]$OutputDir
    )

    $reports = [System.Collections.Generic.List[object]]::new()
    $plannedBatches = [System.Collections.Generic.List[object]]::new()
    if ($AgentWorkQueue.Count -eq 0) {
        return [pscustomobject]@{ plannedBatches = @(); agentWorkReports = @() }
    }

    $config = Get-RuleHarnessConfig -ConfigPath $ConfigPath
    $settings = Get-RuleHarnessAgentRunnerSettings -Config $config
    if (-not $settings.enabled) {
        foreach ($task in @($AgentWorkQueue)) {
            [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-disabled' -Summary 'Coding agent runner is disabled in rule-harness config.'))
        }
        return [pscustomobject]@{ plannedBatches = @(); agentWorkReports = @($reports) }
    }

    $codexPath = Resolve-RuleHarnessCodexCommand -ConfiguredPath ([string]$settings.commandPath)
    if ([string]::IsNullOrWhiteSpace($codexPath)) {
        foreach ($task in @($AgentWorkQueue)) {
            [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-unavailable' -Summary 'Codex CLI was not found on PATH and no agentRunner.commandPath was configured.'))
        }
        return [pscustomobject]@{ plannedBatches = @(); agentWorkReports = @($reports) }
    }

    $runnerDir = Join-Path $OutputDir 'agent-runner'
    if (-not (Test-Path -LiteralPath $runnerDir)) {
        New-Item -ItemType Directory -Path $runnerDir -Force | Out-Null
    }
    $tracePath = Join-Path $runnerDir 'trace.txt'
    Set-Content -Path $tracePath -Value "Agent runner started role=$RoleName tasks=$($AgentWorkQueue.Count)" -Encoding UTF8

    $schemaPath = Join-Path $PSScriptRoot 'schemas/agent-batch.schema.json'
    $taskLimit = [Math]::Max(1, [int]$settings.maxTasksPerRun)
    $selectedTasks = @($AgentWorkQueue | Select-Object -First $taskLimit)
    foreach ($task in @($AgentWorkQueue | Select-Object -Skip $taskLimit)) {
        [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'skipped' -BlockedReason 'agent-runner-task-limit' -Summary "Agent task was left queued because maxTasksPerRun=$taskLimit."))
    }

    foreach ($task in @($selectedTasks)) {
        $taskId = [string]$task.taskId
        $snapshotsComplete = Test-RuleHarnessAgentTaskSnapshotsComplete -RepoRoot $RepoRoot -Task $task
        $completedTask = $false
        for ($attempt = 0; $attempt -lt 2 -and -not $completedTask; $attempt++) {
            $attemptSuffix = if ($attempt -eq 0) { '' } else { ".retry$attempt" }
            Add-Content -Path $tracePath -Value "Preparing task $taskId attempt=$attempt" -Encoding UTF8
            $outputPath = Join-Path $runnerDir "$taskId$attemptSuffix.output.json"
            $logPath = Join-Path $runnerDir "$taskId$attemptSuffix.log.txt"
            $promptPath = Join-Path $runnerDir "$taskId$attemptSuffix.prompt.md"
            $prompt = New-RuleHarnessAgentBatchPrompt -RepoRoot $RepoRoot -RoleName $RoleName -Task $task -StrictContract:($attempt -gt 0)
            $prompt | Set-Content -Path $promptPath -Encoding UTF8
            Add-Content -Path $tracePath -Value "Prompt written for task $taskId attempt=$attempt" -Encoding UTF8
            $repoRootForPrompt = (Resolve-Path -LiteralPath $RepoRoot).Path.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
            $promptFullPath = (Resolve-Path -LiteralPath $promptPath).Path
            $promptRelativePath = if ($promptFullPath.StartsWith($repoRootForPrompt, [System.StringComparison]::OrdinalIgnoreCase)) {
                $promptFullPath.Substring($repoRootForPrompt.Length).Replace('\', '/')
            }
            else {
                $promptFullPath
            }
            $arguments = @()
            $fileName = $codexPath
            if ($codexPath -like '*.cmd' -or $codexPath -like '*.bat') {
                $fileName = 'cmd.exe'
                $arguments += @('/d', '/c', $codexPath)
            }
            elseif ($codexPath -like '*.ps1') {
                $codexBaseDir = Split-Path -Parent $codexPath
                $nodePath = Join-Path $codexBaseDir 'node.exe'
                if (-not (Test-Path -LiteralPath $nodePath)) {
                    $nodePath = 'node.exe'
                }
                $codexJsPath = Join-Path $codexBaseDir 'node_modules/@openai/codex/bin/codex.js'
                $fileName = $nodePath
                $arguments += @($codexJsPath)
            }
            $arguments += @('exec', '-C', $RepoRoot, '--sandbox', 'read-only', '-c', "approval_policy='never'", '-c', ("model_reasoning_effort='{0}'" -f [string]$settings.reasoningEffort), '--output-schema', $schemaPath, '-o', $outputPath)
            if (-not [string]::IsNullOrWhiteSpace([string]$settings.model)) {
                $arguments += @('--model', [string]$settings.model)
            }
            $arguments += @("Read and follow the full batch synthesis instructions in '$promptRelativePath'. Return only JSON matching the provided output schema.")

            $commandName = Split-Path -Leaf $fileName
            Add-Content -Path $tracePath -Value ("Command: {0} task={1} attempt={2} model={3} output={4}" -f $commandName, $taskId, $attempt, [string]$settings.model, $outputPath) -Encoding UTF8
            Add-Content -Path $tracePath -Value "Invoking Codex for task $taskId attempt=$attempt" -Encoding UTF8
            $processResult = Invoke-RuleHarnessProcess -FileName $fileName -Arguments $arguments -WorkingDirectory $RepoRoot -TimeoutSec ([int]$settings.timeoutSec)
            Add-Content -Path $tracePath -Value "Codex finished for task $taskId attempt=$attempt exit=$($processResult.exitCode) timeout=$($processResult.timedOut)" -Encoding UTF8
            @(
                "ExitCode=$($processResult.exitCode)"
                "TimedOut=$($processResult.timedOut)"
                ''
                'STDOUT:'
                [string]$processResult.stdout
                ''
                'STDERR:'
                [string]$processResult.stderr
            ) | Set-Content -Path $logPath -Encoding UTF8

            if ($processResult.timedOut) {
                [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-timeout' -Summary "Codex agent runner timed out after $($settings.timeoutSec) seconds." -OutputPath $outputPath -LogPath $logPath))
                $completedTask = $true
                continue
            }
            if ([int]$processResult.exitCode -ne 0) {
                [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-failed' -Summary "Codex agent runner exited with code $($processResult.exitCode)." -OutputPath $outputPath -LogPath $logPath))
                $completedTask = $true
                continue
            }
            if (-not (Test-Path -LiteralPath $outputPath)) {
                [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-missing-output' -Summary 'Codex agent runner completed without writing structured output.' -OutputPath $outputPath -LogPath $logPath))
                $completedTask = $true
                continue
            }

            try {
                $agentOutput = Get-Content -Path $outputPath -Raw | ConvertFrom-Json
            }
            catch {
                [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-invalid-json' -Summary "Codex agent runner output was not valid JSON: $($_.Exception.Message)" -OutputPath $outputPath -LogPath $logPath))
                $completedTask = $true
                continue
            }

            $blockedEntries = @($agentOutput.blocked)
            $blockedReason = if ($blockedEntries.Count -gt 0) { [string]$blockedEntries[0].reasonCode } else { '' }
            if (@($agentOutput.batches).Count -eq 0 -and $snapshotsComplete -and $blockedReason -eq 'read_only_sandbox') {
                if ($attempt -eq 0) {
                    Add-Content -Path $tracePath -Value "Retrying task $taskId because read_only_sandbox is a contract violation with complete snapshots." -Encoding UTF8
                    continue
                }

                [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-contract-violation' -Summary 'Codex agent runner reported read_only_sandbox even though complete candidate snapshots were provided; this is a batch synthesis contract violation, not a candidate-file recurrence target.' -OutputPath $outputPath -LogPath $logPath))
                $completedTask = $true
                continue
            }

            $acceptedTargets = [System.Collections.Generic.List[string]]::new()
            foreach ($batch in @($agentOutput.batches)) {
                $targetErrors = @(Test-RuleHarnessAgentBatchTargets -Batch $batch -Task $task)
                if ($targetErrors.Count -gt 0) {
                    [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-unsafe-batch' -Summary ($targetErrors -join ' ') -OutputPath $outputPath -LogPath $logPath))
                    continue
                }

                $batchId = [string]$batch.id
                if (-not $batchId.StartsWith($taskId, [System.StringComparison]::OrdinalIgnoreCase)) {
                    Set-RuleHarnessObjectProperty -Object $batch -Name 'id' -Value ("{0}-{1}" -f $taskId, $batchId)
                }
                Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'planned'
                if ($batch.PSObject.Properties.Name -notcontains 'featureNames') {
                    Set-RuleHarnessObjectProperty -Object $batch -Name 'featureNames' -Value @()
                }
                if ($batch.PSObject.Properties.Name -notcontains 'sourceFindingTypes') {
                    Set-RuleHarnessObjectProperty -Object $batch -Name 'sourceFindingTypes' -Value @()
                }
                if ($batch.PSObject.Properties.Name -notcontains 'fingerprint') {
                    Set-RuleHarnessObjectProperty -Object $batch -Name 'fingerprint' -Value $null
                }
                if ($batch.PSObject.Properties.Name -notcontains 'riskScore') {
                    Set-RuleHarnessObjectProperty -Object $batch -Name 'riskScore' -Value $null
                }
                if ($batch.PSObject.Properties.Name -notcontains 'riskLabel') {
                    Set-RuleHarnessObjectProperty -Object $batch -Name 'riskLabel' -Value $null
                }
                if ($batch.PSObject.Properties.Name -notcontains 'ownershipStatus') {
                    Set-RuleHarnessObjectProperty -Object $batch -Name 'ownershipStatus' -Value 'pending'
                }
                foreach ($target in @($batch.targetFiles)) {
                    [void]$acceptedTargets.Add(([string]$target).Replace('\', '/'))
                }
                [void]$plannedBatches.Add($batch)
            }

            if ($acceptedTargets.Count -gt 0) {
                [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'proposed' -Summary "Codex agent runner proposed $(@($agentOutput.batches).Count) batch(es)." -ChangedFiles @($acceptedTargets | Sort-Object -Unique) -OutputPath $outputPath -LogPath $logPath))
            }
            elseif (@($agentOutput.blocked).Count -gt 0) {
                $blockedSummary = (@($agentOutput.blocked) | ForEach-Object { [string]$_.summary }) -join ' '
                $blockedReason = [string](@($agentOutput.blocked) | Select-Object -First 1).reasonCode
                [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason $blockedReason -Summary $blockedSummary -OutputPath $outputPath -LogPath $logPath))
            }
            else {
                [void]$reports.Add((New-RuleHarnessAgentWorkReport -Task $task -Status 'blocked' -BlockedReason 'agent-runner-no-batch' -Summary 'Codex agent runner returned no batch and no explicit blocked reason.' -OutputPath $outputPath -LogPath $logPath))
            }

            $completedTask = $true
        }
    }

    [pscustomobject]@{
        plannedBatches = @($plannedBatches)
        agentWorkReports = @($reports)
    }
}

function Test-RuleHarnessRoleInput {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$InputPath,
        [switch]$ThrowOnError
    )

    $errors = [System.Collections.Generic.List[string]]::new()
    $resolvedPath = $null
    $payload = $null

    try {
        $resolvedPath = (Resolve-Path -LiteralPath $InputPath -ErrorAction Stop).Path
        $payload = Get-Content -Path $resolvedPath -Raw | ConvertFrom-Json
    }
    catch {
        [void]$errors.Add("Input artifact could not be read as JSON: $($_.Exception.Message)")
    }

    if ($null -ne $payload) {
        $currentCommit = ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim()
        if (-not ($payload.PSObject.Properties.Name -contains 'baseCommitSha') -or [string]$payload.baseCommitSha -ne $currentCommit) {
            [void]$errors.Add("Input artifact baseCommitSha must match current HEAD. Expected=$currentCommit Actual=$([string]$payload.baseCommitSha)")
        }

        foreach ($batch in @($payload.recommendedBatches)) {
            if (-not ($batch.PSObject.Properties.Name -contains 'targetFiles')) {
                [void]$errors.Add("Batch '$([string]$batch.id)' is missing targetFiles.")
                continue
            }

            foreach ($target in @($batch.targetFiles)) {
                if (-not (Test-RuleHarnessSafeRelativePath -Path ([string]$target))) {
                    [void]$errors.Add("Batch '$([string]$batch.id)' contains unsafe target path '$target'.")
                }
            }
        }
    }

    if ($errors.Count -gt 0 -and $ThrowOnError) {
        throw ($errors -join ' ')
    }

    [pscustomobject]@{
        valid = ($errors.Count -eq 0)
        errors = @($errors)
        path = $resolvedPath
        payload = $payload
    }
}

function ConvertTo-RuleHarnessRoleBatches {
    param(
        [Parameter(Mandatory)]
        [object]$InputObject
    )

    $batches = [System.Collections.Generic.List[object]]::new()
    foreach ($batch in @($InputObject.recommendedBatches)) {
        foreach ($target in @($batch.targetFiles)) {
            if (-not (Test-RuleHarnessSafeRelativePath -Path ([string]$target))) {
                throw "Unsafe target path in role batch '$([string]$batch.id)': $target"
            }
        }

        [void]$batches.Add($batch)
    }

    @($batches)
}

function Invoke-RuleHarnessRoleMutation {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$ConfigPath,
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$PlannedBatches,
        [string]$RoleInputPath,
        [switch]$DryRun
    )

    $config = Get-RuleHarnessConfig -ConfigPath $ConfigPath
    $mutationState = Get-RuleHarnessMutationState -Config $config -MutationMode 'code_and_rules' -EnableMutation
    $targetFiles = @($PlannedBatches | ForEach-Object { @($_.targetFiles) } | Sort-Object -Unique)
    foreach ($target in @($targetFiles)) {
        if (-not (Test-RuleHarnessSafeRelativePath -Path ([string]$target))) {
            throw "Unsafe target path in role mutation input: $target"
        }
    }

    $ownerDoc = @(
        $PlannedBatches |
            ForEach-Object { @($_.ownerDocs) } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Select-Object -First 1
    )
    $scopeInfo = Get-RuleHarnessScopeInfo `
        -RepoRoot $RepoRoot `
        -OwnerDoc $(if ($ownerDoc.Count -gt 0) { [string]$ownerDoc[0] } else { $null }) `
        -TargetFiles $targetFiles
    $targetFeatureNames = @(
        $targetFiles |
            ForEach-Object {
                $normalized = ([string]$_).Replace('\', '/')
                if ($normalized -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
                    [string]$Matches['feature']
                }
            } |
            Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } |
            Sort-Object -Unique
    )
    $roleStaticScanScopeId = if ($targetFeatureNames.Count -eq 1) { [string]$targetFeatureNames[0] } else { $null }
    $rulesOnlyCloseout = Get-RuleHarnessRulesOnlyRecurrenceCloseoutStatus `
        -RepoRoot $RepoRoot `
        -ScopeInfo $scopeInfo `
        -TargetFiles $targetFiles

    if ([bool]$rulesOnlyCloseout.failed) {
        $emptyResult = Invoke-RuleHarnessMutationPlan `
            -PlannedBatches @() `
            -InitialStaticFindings @() `
            -RepoRoot $RepoRoot `
            -Config $config `
            -MutationState $mutationState `
            -ScopeInfo $scopeInfo `
            -StaticScanScopeId $roleStaticScanScopeId `
            -DryRun:$DryRun
        $combinedStageResults = @($rulesOnlyCloseout.stageResult) + @($emptyResult.stageResults)
        $combinedActionItems = @(Merge-RuleHarnessActionItems -Items (@($rulesOnlyCloseout.actionItems) + @($emptyResult.actionItems)))
        Set-RuleHarnessObjectProperty -Object $emptyResult -Name 'stageResults' -Value $combinedStageResults
        Set-RuleHarnessObjectProperty -Object $emptyResult -Name 'actionItems' -Value $combinedActionItems
        Set-RuleHarnessObjectProperty -Object $emptyResult -Name 'failed' -Value $true
        Set-RuleHarnessObjectProperty -Object $emptyResult -Name 'roleInputPath' -Value $RoleInputPath
        return $emptyResult
    }

    $result = Invoke-RuleHarnessMutationPlan `
        -PlannedBatches @($PlannedBatches) `
        -InitialStaticFindings @() `
        -RepoRoot $RepoRoot `
        -Config $config `
        -MutationState $mutationState `
        -ScopeInfo $scopeInfo `
        -StaticScanScopeId $roleStaticScanScopeId `
        -DryRun:$DryRun

    $combinedStageResults = @($rulesOnlyCloseout.stageResult) + @($result.stageResults)
    $combinedActionItems = @(Merge-RuleHarnessActionItems -Items (@($rulesOnlyCloseout.actionItems) + @($result.actionItems)))
    Set-RuleHarnessObjectProperty -Object $result -Name 'stageResults' -Value $combinedStageResults
    Set-RuleHarnessObjectProperty -Object $result -Name 'actionItems' -Value $combinedActionItems
    Set-RuleHarnessObjectProperty -Object $result -Name 'roleInputPath' -Value $RoleInputPath
    $result
}

Export-ModuleMember -Function `
    Get-RuleHarnessConfig, `
    Get-RuleHarnessStaticFindings, `
    Invoke-RuleHarnessDocEdits, `
    Invoke-RuleHarness, `
    Test-RuleHarnessDocAllowed, `
    ConvertTo-RuleHarnessReviewedFindings, `
    Get-RuleHarnessPlannedBatches, `
    Get-RuleHarnessDirtyTargetPaths, `
    Get-RuleHarnessMutationState, `
    Invoke-RuleHarnessMutationPlan, `
    Get-RuleHarnessProjectReviewSnapshot, `
    Get-RuleHarnessArchitectureOwnerDoc, `
    Test-RuleHarnessRoleInput, `
    ConvertTo-RuleHarnessRoleBatches, `
    Invoke-RuleHarnessAgentBatchRunner, `
    Add-RuleHarnessAgentWorkState, `
    Invoke-RuleHarnessRoleMutation, `
    Read-RuleHarnessHistoryState, `
    Read-RuleHarnessFeatureScanState, `
    Read-RuleHarnessDocProposalBacklog, `
    Get-RuleHarnessRecurringFailureDocEdits, `
    Invoke-RuleHarnessRecurringFailurePromotion
