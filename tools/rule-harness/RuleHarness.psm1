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
        'missing-validation-registry' {
            return New-RuleHarnessActionItem `
                -Kind 'extend-validation-registry' `
                -Severity 'high' `
                -Summary ("Add validation coverage for batch {0}" -f [string]$SkippedBatch.id) `
                -Details ("Targeted validation is missing. Add or fix an entry in {0} so this code batch can run feature validation." -f [string]$Config.validation.registryPath) `
                -RelatedPaths (Get-RuleHarnessCombinedPaths -Primary $targetFiles -Secondary @([string]$Config.validation.registryPath))
        }
        'no-targeted-tests' {
            return New-RuleHarnessActionItem `
                -Kind 'add-targeted-tests' `
                -Severity 'high' `
                -Summary ("Add targeted validation for batch {0}" -f [string]$SkippedBatch.id) `
                -Details ("The validation registry resolved no runnable scripts for this batch. Add targeted tests or smoke scripts referenced by {0}." -f [string]$Config.validation.registryPath) `
                -RelatedPaths (Get-RuleHarnessCombinedPaths -Primary $targetFiles -Secondary @([string]$Config.validation.registryPath))
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

    $claude = Join-Path $RepoRoot 'CLAUDE.md'
    if (-not (Test-Path -LiteralPath $claude)) {
        return @()
    }

    $docs = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($target in Get-RuleHarnessMarkdownTargets -Content (Get-Content -Path $claude -Raw)) {
        $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath 'CLAUDE.md' -Target $target
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

    $claude = Join-Path $RepoRoot 'CLAUDE.md'
    $referencedDocs = @(Get-RuleHarnessClaudeReferencedDocs -RepoRoot $RepoRoot)
    if (-not (Test-Path -LiteralPath $claude)) {
        return 'CLAUDE.md'
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

        foreach ($target in Get-RuleHarnessMarkdownTargets -Content $line) {
            $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath 'CLAUDE.md' -Target $target
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

    return 'CLAUDE.md'
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

function Test-RuleHarnessGlobalRuleDoc {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$RelativePath
    )

    $normalized = $RelativePath.Replace('\', '/')
    if ($normalized -eq 'CLAUDE.md') {
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

function Get-RuleHarnessValidationRegistryPath {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $relativePath = if ($Config.validation.PSObject.Properties.Name -contains 'registryPath') {
        [string]$Config.validation.registryPath
    }
    else {
        'tools/rule-harness/validation-registry.json'
    }

    [pscustomobject]@{
        relativePath = $relativePath.Replace('\', '/')
        fullPath     = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $relativePath))
    }
}

function Get-RuleHarnessValidationRegistry {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $pathInfo = Get-RuleHarnessValidationRegistryPath -RepoRoot $RepoRoot -Config $Config
    if (-not (Test-Path -LiteralPath $pathInfo.fullPath)) {
        return [pscustomobject]@{
            path         = $pathInfo.fullPath
            relativePath = $pathInfo.relativePath
            schemaVersion = 1
            features     = [pscustomobject]@{}
        }
    }

    $raw = Get-Content -Path $pathInfo.fullPath -Raw | ConvertFrom-Json
    [pscustomobject]@{
        path          = $pathInfo.fullPath
        relativePath  = $pathInfo.relativePath
        schemaVersion = if ($raw.PSObject.Properties.Name -contains 'schemaVersion') { [int]$raw.schemaVersion } else { 1 }
        features      = if ($raw.PSObject.Properties.Name -contains 'features') { $raw.features } else { [pscustomobject]@{} }
    }
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

    $scopePath = if (-not [string]::IsNullOrWhiteSpace($OwnerDoc)) { [string]$OwnerDoc } elseif (@($TargetFiles).Count -gt 0) { [string]$TargetFiles[0] } else { 'CLAUDE.md' }
    $scopeType = 'global'
    $promotionTarget = $scopePath
    $architectureDoc = if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) { Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot } else { $null }
    $governanceDoc = if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) { Get-RuleHarnessGovernanceOwnerDoc -RepoRoot $RepoRoot } else { $null }
    $unityMcpDoc = if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) { Get-RuleHarnessPreferredClaudeDoc -RepoRoot $RepoRoot -Keywords @('unity_mcp', 'Unity MCP', 'MCP', 'editor automation') } else { $null }

    if ($scopePath -like 'Assets/Scripts/Features/*' -or $scopePath -like 'Assets/Scripts/Shared/*') {
        $scopeType = 'global'
        $promotionTarget = if ([string]::IsNullOrWhiteSpace($architectureDoc)) { 'CLAUDE.md' } else { $architectureDoc }
    }
    elseif ($scopePath -like 'Assets/Editor/UnityMcp/*') {
        $scopeType = 'global'
        $promotionTarget = if ([string]::IsNullOrWhiteSpace($unityMcpDoc)) { 'CLAUDE.md' } else { $unityMcpDoc }
    }
    elseif (-not [string]::IsNullOrWhiteSpace($RepoRoot) -and (Test-RuleHarnessGlobalRuleDoc -RepoRoot $RepoRoot -RelativePath $scopePath)) {
        $scopeType = 'global'
        if ($scopePath -eq $architectureDoc -or $scopePath -eq 'CLAUDE.md') {
            if ([string]::IsNullOrWhiteSpace($governanceDoc) -or $governanceDoc -eq 'CLAUDE.md' -or $governanceDoc -eq $scopePath) {
                $promotionTarget = if ([string]::IsNullOrWhiteSpace($architectureDoc)) { 'CLAUDE.md' } else { $architectureDoc }
            }
            else {
                $promotionTarget = $governanceDoc
            }
        }
        else {
            $promotionTarget = $scopePath
        }
    }
    elseif ($scopePath -eq 'CLAUDE.md') {
        $scopeType = 'global'
        $promotionTarget = if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) { Get-RuleHarnessGovernanceOwnerDoc -RepoRoot $RepoRoot } else { 'CLAUDE.md' }
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

function Get-RuleHarnessInferredStructuralChecks {
    param(
        [Parameter(Mandatory)]
        [object]$Batch
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
    }

    @($checks)
}

function Get-RuleHarnessDiscoveredValidationPlan {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [object]$ValidationRegistry
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
            scripts            = @()
            featureTestAssets  = @()
            inferredChecks     = @()
            registryHints      = @()
            checks             = @()
            status             = 'rule_only'
            reasonCode         = 'rule-only'
        }
    }

    $featureNames = @($Batch.featureNames | Sort-Object -Unique)
    $runnerScripts = @(Get-RuleHarnessTargetedTestScripts -RepoRoot $RepoRoot -FeatureNames $featureNames)
    $featureTestAssets = @(Get-RuleHarnessFeatureTestAssets -RepoRoot $RepoRoot -FeatureNames $featureNames)
    $registryHints = [System.Collections.Generic.List[string]]::new()
    $registryScripts = [System.Collections.Generic.List[string]]::new()
    foreach ($featureName in $featureNames) {
        $featureProperty = $ValidationRegistry.features.PSObject.Properties[$featureName]
        if ($null -eq $featureProperty) {
            continue
        }

        [void]$registryHints.Add("$($ValidationRegistry.relativePath):$featureName")
        $entry = $featureProperty.Value
        foreach ($scriptPath in @($entry.scripts) + @($entry.smoke)) {
            if ([string]::IsNullOrWhiteSpace([string]$scriptPath)) {
                continue
            }

            $fullPath = Join-Path $RepoRoot ([string]$scriptPath)
            if (Test-Path -LiteralPath $fullPath) {
                [void]$registryScripts.Add([System.IO.Path]::GetFullPath($fullPath))
            }
        }
    }

    $selectedScripts = if (@($runnerScripts).Count -gt 0) {
        @($runnerScripts)
    }
    else {
        @($registryScripts | Sort-Object -Unique)
    }
    $inferredChecks = @(Get-RuleHarnessInferredStructuralChecks -Batch $Batch)
    $source = if (@($runnerScripts).Count -gt 0) {
        'feature_runner'
    }
    elseif (@($registryScripts).Count -gt 0) {
        'registry_hint'
    }
    elseif (@($featureTestAssets).Count -gt 0) {
        'feature_test_assets'
    }
    else {
        'inferred'
    }
    $confidence = if (@($selectedScripts).Count -gt 0) {
        'high'
    }
    elseif (@($featureTestAssets).Count -gt 0) {
        'medium'
    }
    else {
        'low'
    }

    $checks = [System.Collections.Generic.List[object]]::new()
    foreach ($script in @($selectedScripts)) {
        [void]$checks.Add([pscustomobject]@{
            name     = 'targeted_script'
            source   = $source
            runnable = $true
            details  = $script
        })
    }
    foreach ($check in $inferredChecks) {
        [void]$checks.Add($check)
    }

    [pscustomobject]@{
        batchId            = $Batch.id
        scopeType          = $scopeInfo.scopeType
        scopePath          = $scopeInfo.scopePath
        source             = $source
        confidence         = $confidence
        runnable           = (@($selectedScripts).Count -gt 0)
        scripts            = @($selectedScripts | Sort-Object -Unique)
        featureTestAssets  = @($featureTestAssets)
        inferredChecks     = @($inferredChecks)
        registryHints      = @($registryHints)
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
        $score = if ($isScaffoldBatch) { [int]$scores.featureRootScaffold } else { [int]$scores.existingCodeFileEdit }
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
                    reason = "UnityMcp path '$normalized' requires the current Unity MCP rule doc referenced from CLAUDE.md."
                }
            }
            continue
        }

        if ($normalized -eq 'CLAUDE.md') {
            if ($Batch.kind -ne 'rule_fix' -or @($Batch.sourceFindingTypes | Where-Object { $_ -notin @('broken_reference', 'doc_drift') }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = 'CLAUDE.md only allows rule-fix batches backed by broken_reference or doc_drift findings.'
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

function Get-RuleHarnessTargetedValidationPlan {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [object]$ValidationRegistry
    )

    if ($Batch.kind -eq 'rule_fix') {
        return [pscustomobject]@{
            status     = 'rule_only'
            scripts    = @()
            source     = 'rule-only'
            reasonCode = 'rule-only'
        }
    }

    $featureNames = @($Batch.featureNames | Sort-Object -Unique)
    if ($featureNames.Count -eq 0) {
        return [pscustomobject]@{
            status     = 'missing'
            scripts    = @()
            source     = $ValidationRegistry.relativePath
            reasonCode = 'missing-validation-registry'
        }
    }

    $scripts = [System.Collections.Generic.List[string]]::new()
    $sources = [System.Collections.Generic.List[string]]::new()
    foreach ($featureName in $featureNames) {
        $featureProperty = $ValidationRegistry.features.PSObject.Properties[$featureName]
        if ($null -eq $featureProperty) {
            return [pscustomobject]@{
                status     = 'missing'
                scripts    = @()
                source     = $ValidationRegistry.relativePath
                reasonCode = 'missing-validation-registry'
            }
        }

        $entry = $featureProperty.Value
        $requiredForKinds = if ($entry.PSObject.Properties.Name -contains 'requiredForKinds' -and @($entry.requiredForKinds).Count -gt 0) {
            @($entry.requiredForKinds)
        }
        else {
            @('code_fix', 'mixed_fix')
        }

        if ($Batch.kind -notin $requiredForKinds) {
            return [pscustomobject]@{
                status     = 'missing'
                scripts    = @()
                source     = $ValidationRegistry.relativePath
                reasonCode = 'missing-validation-registry'
            }
        }

        foreach ($scriptPath in @($entry.scripts) + @($entry.smoke)) {
            if ([string]::IsNullOrWhiteSpace([string]$scriptPath)) {
                continue
            }

            $fullPath = Join-Path $RepoRoot ([string]$scriptPath)
            if (-not (Test-Path -LiteralPath $fullPath)) {
                return [pscustomobject]@{
                    status     = 'missing'
                    scripts    = @()
                    source     = $ValidationRegistry.relativePath
                    reasonCode = 'missing-validation-registry'
                }
            }

            [void]$scripts.Add([System.IO.Path]::GetFullPath($fullPath))
        }
        [void]$sources.Add("$($ValidationRegistry.relativePath):$featureName")
    }

    if ($scripts.Count -eq 0) {
        if ($Config.validation.requireRegistryForCodeBatches) {
            return [pscustomobject]@{
                status     = 'missing'
                scripts    = @()
                source     = $ValidationRegistry.relativePath
                reasonCode = 'missing-validation-registry'
            }
        }

        return [pscustomobject]@{
            status     = 'missing'
            scripts    = @()
            source     = $ValidationRegistry.relativePath
            reasonCode = 'no-targeted-tests'
        }
    }

    [pscustomobject]@{
        status     = 'ok'
        scripts    = @($scripts | Sort-Object -Unique)
        source     = ($sources -join ', ')
        reasonCode = $null
    }
}

function Get-RuleHarnessStaticFindings {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $findings = [System.Collections.Generic.List[object]]::new()
    $architectureOwnerDoc = Get-RuleHarnessArchitectureOwnerDoc -RepoRoot $RepoRoot

    foreach ($doc in Get-RuleHarnessScopeDocs -RepoRoot $RepoRoot -Config $Config) {
        $docFull = Join-Path $RepoRoot $doc
        if (-not (Test-Path -LiteralPath $docFull)) {
            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'broken_reference' `
                -Severity $Config.severityPolicy.missingSsotReference `
                -OwnerDoc 'CLAUDE.md' `
                -Title 'Missing SSOT document' `
                -Message "SSOT scope references a document that does not exist: $doc" `
                -Evidence @([pscustomobject]@{ path = 'CLAUDE.md'; line = $null; snippet = $doc }) `
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

    foreach ($feature in Get-RuleHarnessFeatureDirectories -RepoRoot $RepoRoot) {
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
        }
    }

    foreach ($script in Get-RuleHarnessScriptFiles -RepoRoot $RepoRoot -Config $Config) {
        $relative = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $script.FullName
        $lines = Get-Content -Path $script.FullName
        $content = $lines -join "`n"

        if ($relative -match '/Domain/') {
            $pattern = 'using\s+UnityEngine\s*;|using\s+Photon|\bUnityEngine\.|\bPhoton\.'
            if ($content -match $pattern) {
                $line = Get-RuleHarnessFirstMatchLine -Lines $lines -Pattern $pattern
                [void]$findings.Add((New-RuleHarnessFinding `
                    -FindingType 'code_violation' `
                    -Severity $Config.severityPolicy.unityInDomain `
                    -OwnerDoc $architectureOwnerDoc `
                    -Title 'Framework API used in Domain' `
                    -Message "Domain layer file '$relative' references Unity or Photon APIs." `
                    -Evidence @([pscustomobject]@{ path = $relative; line = $line; snippet = (Get-RuleHarnessLineSnippet -Lines $lines -Line $line) }) `
                    -Confidence 'high' `
                    -RemediationKind 'code_fix' `
                    -Rationale 'The code is violating a stable architecture rule and should be refactored.'))
            }
        }

        if ($relative -match '/Application/') {
            $pattern = 'using\s+UnityEngine\s*;|using\s+Photon|\bMonoBehaviour\b|\bGameObject\b|\bSprite\b|\bAudioClip\b|\bColor\b|Debug\.Log'
            if ($content -match $pattern) {
                $line = Get-RuleHarnessFirstMatchLine -Lines $lines -Pattern $pattern
                [void]$findings.Add((New-RuleHarnessFinding `
                    -FindingType 'code_violation' `
                    -Severity $Config.severityPolicy.unityInApplication `
                    -OwnerDoc $architectureOwnerDoc `
                    -Title 'Unity API used in Application' `
                    -Message "Application layer file '$relative' appears to reference Unity or Photon API types." `
                    -Evidence @([pscustomobject]@{ path = $relative; line = $line; snippet = (Get-RuleHarnessLineSnippet -Lines $lines -Line $line) }) `
                    -Confidence 'high' `
                    -RemediationKind 'code_fix' `
                    -Rationale 'The code is violating a stable architecture rule and should be refactored.'))
            }
        }
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
                -Validation @('rule_harness_tests', 'targeted_tests', 'static_scan') `
                -ExpectedFindingsResolved @($key) `
                -Operations @([pscustomobject]@{
                    type       = 'write_file'
                    targetPath = $targetPath
                    content    = (Get-RuleHarnessBootstrapSetupContent -FeatureName $featureName)
                }) `
                -FeatureNames @($featureName) `
                -OwnerDocs @($finding.ownerDoc) `
                -SourceFindingTypes @($finding.findingType)))
        }
    }

    @($batches)
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

function Get-RuleHarnessTargetedTestScripts {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string[]]$FeatureNames
    )

    $scripts = [System.Collections.Generic.List[string]]::new()
    foreach ($featureName in $FeatureNames | Sort-Object -Unique) {
        $featureTestRoot = Join-Path $RepoRoot "Tests/$featureName"
        if (-not (Test-Path -LiteralPath $featureTestRoot)) {
            continue
        }

        foreach ($script in Get-ChildItem -LiteralPath $featureTestRoot -Recurse -File -Filter 'Run-*.ps1' -ErrorAction SilentlyContinue) {
            [void]$scripts.Add($script.FullName)
        }
    }

    @($scripts | Sort-Object -Unique)
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
        [object[]]$BaselineStaticFindings
    )

    $results = [System.Collections.Generic.List[object]]::new()
    [void]$results.Add([pscustomobject]@{
        batchId    = $Batch.id
        validation = 'discovered_validation_plan'
        status     = 'passed'
        source     = [string]$ValidationPlan.source
        details    = "Confidence=$($ValidationPlan.confidence) Runnable=$($ValidationPlan.runnable) Scripts=$(@($ValidationPlan.scripts).Count) InferredChecks=$(@($ValidationPlan.inferredChecks).Count) FeatureAssets=$(@($ValidationPlan.featureTestAssets).Count) RegistryHints=$(@($ValidationPlan.registryHints).Count)"
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
        if (@($ValidationPlan.scripts).Count -eq 0) {
            [void]$results.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'targeted_tests'
                status     = 'skipped'
                source     = [string]$ValidationPlan.source
                details    = 'No runnable feature-local runner was discovered; relying on inferred structural checks and static scan.'
            })
        }
        else {
            foreach ($script in @($ValidationPlan.scripts)) {
                $scriptResult = Invoke-RuleHarnessValidationScript `
                    -ScriptPath $script `
                    -ValidationName 'targeted_tests' `
                    -BatchId ([string]$Batch.id) `
                    -Source ([string]$ValidationPlan.source) `
                    -FailureReason 'targeted-tests-failed' `
                    -Label (Split-Path -Leaf $script)
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
        }

        $missingTargets = @($Batch.targetFiles | Where-Object {
            -not (Test-Path -LiteralPath (Join-Path $RepoRoot ([string]$_)))
        })
        if ($missingTargets.Count -gt 0) {
            $details = "Missing target files after apply: $($missingTargets -join ', ')"
            [void]$results.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'inferred_structural_checks'
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
        elseif (@($ValidationPlan.inferredChecks).Count -gt 0) {
            [void]$results.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'inferred_structural_checks'
                status     = 'passed'
                source     = 'inferred'
                details    = "Checks=$((@($ValidationPlan.inferredChecks | ForEach-Object { $_.name }) -join ', '))"
            })
        }
    }
    else {
        [void]$results.Add([pscustomobject]@{
            batchId    = $Batch.id
            validation = 'targeted_tests'
            status     = 'skipped'
            source     = 'rule-only'
            details    = 'rule-only batch'
        })
    }

    $afterFindings = @(Get-RuleHarnessStaticFindings -RepoRoot $RepoRoot -Config $Config)
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
        'feature' { return 'Update the owner README or add a feature-local runner before retrying.' }
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
        [object[]]$BaselineStaticFindings,
        [Parameter(Mandatory)]
        [object]$ValidationPlan,
        [Parameter(Mandatory)]
        [object]$MemoryStore,
        [Parameter(Mandatory)]
        [object]$LearningSettings,
        [Parameter(Mandatory)]
        [string]$CommitSha
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
            $validation = Invoke-RuleHarnessBatchValidation -Batch $Batch -RepoRoot $RepoRoot -Config $Config -ValidationPlan $ValidationPlan -BaselineStaticFindings $currentFindings
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
        -ValidationHints @($ValidationPlan.scripts + $ValidationPlan.featureTestAssets + $ValidationPlan.registryHints) `
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
        [switch]$DryRun
    )

    return Invoke-RuleHarnessMutationPlanCore `
        -PlannedBatches $PlannedBatches `
        -InitialStaticFindings $InitialStaticFindings `
        -RepoRoot $RepoRoot `
        -Config $Config `
        -MutationState $MutationState `
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

    Write-Host 'Rule harness validation registry lookup started.'
    $validationRegistry = Get-RuleHarnessValidationRegistry -RepoRoot $RepoRoot -Config $Config
    $validationRegistryCount = if ($null -ne $validationRegistry.features) { @($validationRegistry.features.PSObject.Properties).Count } else { 0 }
    Write-Host "Rule harness validation registry lookup finished. Path: $($validationRegistry.relativePath) Features: $validationRegistryCount"
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'validation_registry_lookup' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Loaded validation registry hints with {0} feature entries." -f $validationRegistryCount) `
        -Details ([pscustomobject]@{
            path = $validationRegistry.relativePath
            featureCount = $validationRegistryCount
            mode = 'optional-hints'
        })))

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

        $discoveredPlan = Get-RuleHarnessDiscoveredValidationPlan -Batch $batch -RepoRoot $RepoRoot -Config $Config -ValidationRegistry $validationRegistry
        [void]$discoveredValidationPlans.Add($discoveredPlan)
        [void]$decisionTrace.Add("Batch $($batch.id) capability discovery: source=$($discoveredPlan.source) confidence=$($discoveredPlan.confidence) scripts=$(@($discoveredPlan.scripts).Count) inferredChecks=$(@($discoveredPlan.inferredChecks).Count)")
        if ($batch.kind -ne 'rule_fix' -and @($discoveredPlan.scripts).Count -eq 0) {
            [void]$actionItems.Add((New-RuleHarnessActionItem `
                -Kind 'increase-validation-confidence' `
                -Severity $(if ([string]$discoveredPlan.confidence -eq 'low') { 'high' } else { 'medium' }) `
                -Summary ("Add runnable feature validation for batch {0}" -f [string]$batch.id) `
                -Details ("No `Tests/<Feature>/Run-*.ps1` runner was discovered. The harness will proceed with inferred checks and static scan. Confidence={0}." -f [string]$discoveredPlan.confidence) `
                -RelatedPaths (Get-RuleHarnessCombinedPaths -Primary @($batch.targetFiles) -Secondary @($discoveredPlan.featureTestAssets + $discoveredPlan.registryHints))))
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

        $riskAssessment = Get-RuleHarnessBatchRiskAssessment -Batch $batch -RepoRoot $RepoRoot -Config $Config -MutationMode $MutationState.mode
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskScore' -Value $riskAssessment.score
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskLabel' -Value $riskAssessment.label
        if (-not $riskAssessment.allowed) {
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
            -CommitSha $commitSha

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
        -Summary $(if ($failureTriggered) { 'Mutation stage failed and rolled back touched files.' } elseif (@($appliedBatches).Count -gt 0) { "Applied $(@($appliedBatches).Count) batch(es)." } elseif (@($skippedBatches).Count -gt 0) { "No batch was applied. Skipped $(@($skippedBatches).Count) batch(es)." } else { 'Mutation stage completed without workspace changes.' }) `
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
    [void]$lines.Add("- Scanned features: $($Report.scannedFeatures.Count)")
    [void]$lines.Add("- Findings: $($Report.findings.Count)")
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

    if ($Report.promotionCandidates.Count -gt 0) {
        [void]$lines.Add('### Promotion Candidates')
        foreach ($candidate in @($Report.promotionCandidates | Select-Object -First 5)) {
            [void]$lines.Add(('- `{0}` -> `{1}` ({2} runs / {3} commits)' -f $candidate.signature, $candidate.targetDoc, $candidate.hitCount, $candidate.distinctCommitCount))
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

    Write-Host 'Rule harness discover stage started.'
    $scannedFeatures = @(Get-RuleHarnessFeatureDirectories -RepoRoot $RepoRoot | ForEach-Object { $_.Name })
    Write-Host "Rule harness discover stage finished. Features: $($scannedFeatures.Count)"
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'discover' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Discovered {0} feature directories." -f $scannedFeatures.Count) `
        -Details ([pscustomobject]@{
            featureCount = $scannedFeatures.Count
            features = @($scannedFeatures)
        })))

    Write-Host 'Rule harness static scan started.'
    $staticFindings = @(Get-RuleHarnessStaticFindings -RepoRoot $RepoRoot -Config $config)
    Write-Host "Rule harness static scan finished. Findings: $($staticFindings.Count)"
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'static_scan' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Static scan produced {0} finding(s)." -f $staticFindings.Count) `
        -Details ([pscustomobject]@{
            findingCount = $staticFindings.Count
        })))

    $diagnoseAttempted = $llmEnabled -or -not [string]::IsNullOrWhiteSpace($ReviewJsonPath)
    try {
        $reviewedFindings = @(
            Invoke-RuleHarnessAgentReview `
                -StaticFindings $staticFindings `
                -RepoRoot $RepoRoot `
                -Config $config `
                -ApiKey $ApiKey `
                -ApiBaseUrl $ApiBaseUrl `
                -Model $Model `
                -TimeoutSec $timeoutSec `
                -ReviewJsonPath $ReviewJsonPath
        )
        Write-Host "Rule harness reviewed findings count: $($reviewedFindings.Count)"
        $diagnoseStatus = if ($diagnoseAttempted) { 'passed' } else { 'skipped' }
        $diagnoseSummary = if ($diagnoseAttempted) {
            "Diagnose stage reviewed $($reviewedFindings.Count) finding(s)."
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
                reviewedFindingCount = $reviewedFindings.Count
                llmEnabled = [bool]$llmEnabled
                reviewJsonPath = $ReviewJsonPath
            })))
    }
    catch {
        [void]$harnessErrors.Add((New-RuleHarnessFinding `
            -FindingType 'missing_rule' `
            -Severity $config.severityPolicy.agentFailure `
            -OwnerDoc 'tools/rule-harness/README.md' `
            -Title 'Rule harness agent review failed' `
            -Message "Stage=diagnose TimeoutSec=$timeoutSec $($_.Exception.Message)" `
            -Evidence @([pscustomobject]@{ path = 'tools/rule-harness'; line = $null; snippet = $_.Exception.GetType().FullName }) `
            -Confidence 'high' `
            -Source 'harness' `
            -RemediationKind 'report_only'))
        $reviewedFindings = @(ConvertTo-RuleHarnessReviewedFindings -Findings $staticFindings)
        Write-Host "Rule harness diagnose stage failed. TimeoutSec: $timeoutSec Error: $($_.Exception.Message)"
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'diagnose' `
            -Status 'failed' `
            -Attempted $diagnoseAttempted `
            -Summary 'Diagnose stage failed; static findings were used as fallback.' `
            -Details ([pscustomobject]@{
                message = $_.Exception.Message
                timeoutSec = $timeoutSec
            })))
    }

    $eligibleDocFindings = @(
        $reviewedFindings | Where-Object {
            $_.remediationKind -in @('rule_fix', 'mixed_fix') -and
            $_.confidence -eq 'high' -and
            (Test-RuleHarnessDocAllowed -RepoRoot $RepoRoot -RelativePath $_.ownerDoc -Config $config)
        }
    )
    Write-Host "Rule harness eligible doc findings: $($eligibleDocFindings.Count)"

    $docEdits = @()
    if ($llmEnabled -and $eligibleDocFindings.Count -gt 0) {
        try {
            $docEdits = @(
                Invoke-RuleHarnessDocSync `
                    -Findings $eligibleDocFindings `
                    -RepoRoot $RepoRoot `
                    -Config $config `
                    -ApiKey $ApiKey `
                    -ApiBaseUrl $ApiBaseUrl `
                    -Model $Model `
                    -TimeoutSec $timeoutSec
            )
            Write-Host "Rule harness doc sync produced edits: $($docEdits.Count)"
            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'doc_sync' `
                -Status 'passed' `
                -Attempted $true `
                -Summary ("Doc sync proposed {0} edit(s)." -f $docEdits.Count) `
                -Details ([pscustomobject]@{
                    eligibleFindingCount = $eligibleDocFindings.Count
                    docEditCount = $docEdits.Count
                })))
        }
        catch {
            [void]$harnessErrors.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $config.severityPolicy.agentFailure `
                -OwnerDoc 'tools/rule-harness/README.md' `
                -Title 'Rule harness doc sync failed' `
                -Message "Stage=doc_sync TimeoutSec=$timeoutSec $($_.Exception.Message)" `
                -Evidence @([pscustomobject]@{ path = 'tools/rule-harness'; line = $null; snippet = $_.Exception.GetType().FullName }) `
                -Confidence 'high' `
                -Source 'harness'))
            Write-Host "Rule harness doc sync failed. TimeoutSec: $timeoutSec Error: $($_.Exception.Message)"
            [void]$stageResults.Add((New-RuleHarnessStageResult `
                -Stage 'doc_sync' `
                -Status 'failed' `
                -Attempted $true `
                -Summary 'Doc sync failed and no doc edits were produced.' `
                -Details ([pscustomobject]@{
                    eligibleFindingCount = $eligibleDocFindings.Count
                    message = $_.Exception.Message
                    timeoutSec = $timeoutSec
                })))
        }
    }
    else {
        $docSyncSummary = if (-not $llmEnabled) {
            'Doc sync was skipped because LLM mode is disabled.'
        }
        elseif ($eligibleDocFindings.Count -eq 0) {
            'Doc sync was skipped because there were no eligible high-confidence doc findings.'
        }
        else {
            'Doc sync was skipped.'
        }
        [void]$stageResults.Add((New-RuleHarnessStageResult `
            -Stage 'doc_sync' `
            -Status 'skipped' `
            -Attempted $false `
            -Summary $docSyncSummary `
            -Details ([pscustomobject]@{
                eligibleFindingCount = $eligibleDocFindings.Count
                llmEnabled = [bool]$llmEnabled
            })))
    }

    Write-Host 'Rule harness patch plan stage started.'
    $plannedBatches = @(Get-RuleHarnessPlannedBatches -ReviewedFindings $reviewedFindings -DocEdits $docEdits -RepoRoot $RepoRoot)
    Write-Host "Rule harness patch plan stage finished. Planned batches: $($plannedBatches.Count)"
    [void]$stageResults.Add((New-RuleHarnessStageResult `
        -Stage 'patch_plan' `
        -Status 'passed' `
        -Attempted $true `
        -Summary ("Patch plan produced {0} batch(es)." -f $plannedBatches.Count) `
        -Details ([pscustomobject]@{
            plannedBatchCount = $plannedBatches.Count
        })))

    $mutationResult = Invoke-RuleHarnessMutationPlan `
        -PlannedBatches $plannedBatches `
        -InitialStaticFindings $staticFindings `
        -RepoRoot $RepoRoot `
        -Config $config `
        -MutationState $mutationState `
        -DryRun:$DryRun

    $reportMemoryHits = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($mutationResult.memoryHits)) {
        [void]$reportMemoryHits.Add($entry)
    }
    $reportMemoryUpdates = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($mutationResult.memoryUpdates)) {
        [void]$reportMemoryUpdates.Add($entry)
    }
    $reportPromotionCandidates = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($mutationResult.promotionCandidates)) {
        [void]$reportPromotionCandidates.Add($entry)
    }
    $reportLearningTrace = [System.Collections.Generic.List[object]]::new()
    foreach ($entry in @($mutationResult.learningTrace)) {
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

    $findings = @($mutationResult.finalStaticFindings + $harnessErrors)
    $failed = (@($findings | Where-Object { $_.severity -eq 'high' -and $_.findingType -eq 'code_violation' }).Count -gt 0) -or
        ($harnessErrors.Count -gt 0) -or
        [bool]$mutationResult.failed
    $reportActionItems = @(Merge-RuleHarnessActionItems -Items @(
        @($mutationResult.actionItems) +
        @($reportPromotionCandidates | ForEach-Object {
            New-RuleHarnessActionItem `
                -Kind 'promote-durable-rule' `
                -Severity 'high' `
                -Summary ("Promote recurring failure guidance to {0}" -f [string]$_.targetDoc) `
                -Details ("Signature {0} repeated across {1} runs and {2} commits. Capture the durable rule in the owning doc." -f [string]$_.signature, [int]$_.hitCount, [int]$_.distinctCommitCount) `
                -RelatedPaths @([string]$_.targetDoc)
        }) +
        @(Get-RuleHarnessActionItemsForFindings `
            -Findings $findings `
            -ReportPathHint $ReportPathHint `
            -SummaryPathHint $SummaryPath `
            -LogPathHint $LogPathHint)
    ))
    $allStageResults = @($stageResults) + @($mutationResult.stageResults)

    $report = [pscustomobject]@{
        runId            = [guid]::NewGuid().ToString()
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
        }
        scannedFeatures   = $scannedFeatures
        findings          = $findings
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
        validationResults = @($mutationResult.validationResults)
        discoveredValidationPlan = @($mutationResult.discoveredValidationPlan)
        learningTrace     = @($reportLearningTrace)
        memoryHits        = @($reportMemoryHits)
        memoryUpdates     = @($reportMemoryUpdates)
        promotionCandidates = @($reportPromotionCandidates)
        retryAttempts     = [int]$mutationResult.retryAttempts
        historySummary    = $mutationResult.historySummary
        commit            = $mutationResult.commit
        rollback          = $mutationResult.rollback
        applied           = [bool]$mutationResult.applied
        failed            = [bool]$failed
    }

    if (-not [string]::IsNullOrWhiteSpace($SummaryPath)) {
        Write-RuleHarnessSummary -Report $report -SummaryPath $SummaryPath
    }

    $report
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
    Read-RuleHarnessHistoryState
