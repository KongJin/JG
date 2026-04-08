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

    $claude = Join-Path $RepoRoot 'CLAUDE.md'
    if (Test-Path -LiteralPath $claude) {
        foreach ($target in Get-RuleHarnessMarkdownTargets -Content (Get-Content -Path $claude -Raw)) {
            $resolved = Resolve-RuleHarnessTargetPath -RepoRoot $RepoRoot -SourcePath 'CLAUDE.md' -Target $target
            if ($null -ne $resolved) {
                [void]$docs.Add($resolved.RelativePath)
            }
        }
    }

    @($docs | Sort-Object)
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
        [string]$RelativeScriptPath
    )

    if ($RelativeScriptPath -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
        return "Assets/Scripts/Features/$($Matches['feature'])/README.md"
    }

    if ($RelativeScriptPath -like 'Assets/Scripts/Shared/*') {
        return 'Assets/Scripts/Shared/README.md'
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

    if (@($Batch.targetFiles | Where-Object { $_ -like 'agent/*.md' }).Count -gt 0) {
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

    foreach ($targetPath in @($Batch.targetFiles)) {
        $normalized = $targetPath.Replace('\', '/')
        if (Test-RuleHarnessWildcardMatch -Path $normalized -Patterns $Config.docs.denylist) {
            return [pscustomobject]@{
                status = 'rejected'
                reason = 'Target path is denied for mutation.'
            }
        }

        if ($normalized -match '^Assets/Scripts/Features/(?<feature>[^/]+)/') {
            $expectedOwner = "Assets/Scripts/Features/$($Matches['feature'])/README.md"
            if (@($ownerDocs | Where-Object { $_ -ne $expectedOwner }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = "Feature-owned path '$normalized' requires owner doc '$expectedOwner'."
                }
            }
            continue
        }

        if ($normalized -like 'Assets/Scripts/Shared/*') {
            if (@($ownerDocs | Where-Object { $_ -notin @('Assets/Scripts/Shared/README.md', 'agent/architecture.md') }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = "Shared path '$normalized' only allows Shared README or agent/architecture.md ownership."
                }
            }
            continue
        }

        if ($normalized -like 'Assets/Editor/UnityMcp/*') {
            if (@($ownerDocs | Where-Object { $_ -ne 'Assets/Editor/UnityMcp/README.md' }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = "UnityMcp path '$normalized' requires Assets/Editor/UnityMcp/README.md ownership."
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

        if ($normalized -like 'agent/*.md') {
            if ($Batch.kind -ne 'rule_fix' -or @($Batch.sourceFindingTypes | Where-Object { $_ -notin @('broken_reference', 'doc_drift') }).Count -gt 0) {
                return [pscustomobject]@{
                    status = 'rejected'
                    reason = "Agent rule docs only allow rule-fix batches backed by broken_reference or doc_drift findings."
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
        $readme = Join-Path $feature.FullName 'README.md'
        if (-not (Test-Path -LiteralPath $readme)) {
            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $Config.severityPolicy.missingFeatureReadme `
                -OwnerDoc "$relative/README.md" `
                -Title 'Missing feature README' `
                -Message "Feature '$($feature.Name)' does not declare a local README contract." `
                -Evidence @([pscustomobject]@{ path = $relative; line = $null; snippet = $feature.Name }) `
                -RemediationKind 'rule_fix' `
                -Rationale 'Feature-local contracts live in README files.'))
        }

        $rootBootstrap = Get-ChildItem -LiteralPath $feature.FullName -File |
            Where-Object { $_.Extension -eq '.cs' -and ($_.Name -like '*Setup.cs' -or $_.Name -like '*Bootstrap.cs') } |
            Select-Object -First 1

        if ($null -eq $rootBootstrap) {
            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $Config.severityPolicy.missingFeatureBootstrap `
                -OwnerDoc "$relative/README.md" `
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
                    -OwnerDoc 'agent/architecture.md' `
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
                    -OwnerDoc 'agent/architecture.md' `
                    -Title 'Unity API used in Application' `
                    -Message "Application layer file '$relative' appears to reference Unity or Photon API types." `
                    -Evidence @([pscustomobject]@{ path = $relative; line = $line; snippet = (Get-RuleHarnessLineSnippet -Lines $lines -Line $line) }) `
                    -Confidence 'high' `
                    -RemediationKind 'code_fix' `
                    -Rationale 'The code is violating a stable architecture rule and should be refactored.'))
            }
        }
    }

    foreach ($script in Get-RuleHarnessScriptFiles -RepoRoot $RepoRoot -Config $Config) {
        $relative = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $script.FullName
        $ownerDoc = Get-RuleHarnessOwningRuleDocForScript -RelativeScriptPath $relative
        if ([string]::IsNullOrWhiteSpace($ownerDoc)) {
            continue
        }

        $documentedKeys = Get-RuleHarnessDocumentedCustomPropertyKeys -RepoRoot $RepoRoot -OwnerDoc $ownerDoc
        foreach ($key in Get-RuleHarnessCustomPropertyKeysFromFile -FilePath $script.FullName) {
            if ($key -in $documentedKeys) {
                continue
            }

            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $Config.severityPolicy.undocumentedCustomProperty `
                -OwnerDoc $ownerDoc `
                -Title 'Undocumented CustomProperties key' `
                -Message "CustomProperties key '$key' is used in code but not documented in the owning feature README." `
                -Evidence @([pscustomobject]@{ path = $relative; line = $null; snippet = $key }) `
                -Confidence 'high' `
                -RemediationKind 'rule_fix' `
                -Rationale 'CustomProperties ownership now lives in the owning feature README and should reflect code-visible keys.'))
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
        [object[]]$Findings
    )

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
        [string]$RelativePath,
        [Parameter(Mandatory)]
        [object]$Config
    )

    $normalized = $RelativePath.Replace('\', '/')
    if (Test-RuleHarnessWildcardMatch -Path $normalized -Patterns $Config.docs.denylist) {
        return $false
    }

    Test-RuleHarnessWildcardMatch -Path $normalized -Patterns $Config.docs.allowlist
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
        if (-not (Test-RuleHarnessDocAllowed -RelativePath $targetPath -Config $Config)) {
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
            if ($finding.ownerDoc -match '^Assets/Scripts/Features/(?<feature>[^/]+)/README\.md$') {
                $featureName = $Matches['feature']
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

function Invoke-RuleHarnessBatchValidation {
    param(
        [Parameter(Mandatory)]
        [object]$Batch,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [object]$Config,
        [Parameter(Mandatory)]
        [object]$TargetedValidationPlan,
        [Parameter(Mandatory)]
        [object[]]$BaselineStaticFindings
    )

    $results = [System.Collections.Generic.List[object]]::new()

    if ($Config.validation.runHarnessTests) {
        $scriptPath = Join-Path $RepoRoot ([string]$Config.validation.harnessTestScript)
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            & powershell -NoProfile -ExecutionPolicy Bypass -File $scriptPath | Out-Null
            if ($LASTEXITCODE -ne 0) {
                throw "Harness test script exited with code $LASTEXITCODE."
            }
            [void]$results.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'rule_harness_tests'
                status     = 'passed'
                source     = [string]$Config.validation.harnessTestScript
                details    = "ElapsedMs=$($stopwatch.ElapsedMilliseconds)"
            })
        }
        catch {
            [void]$results.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'rule_harness_tests'
                status     = 'failed'
                source     = [string]$Config.validation.harnessTestScript
                details    = $_.Exception.Message
            })
            return [pscustomobject]@{
                passed        = $false
                findingsAfter = $BaselineStaticFindings
                results       = @($results)
                failureReason = 'rule-harness-tests-failed'
            }
        }
    }

    if ($Batch.kind -ne 'rule_fix') {
        if ($TargetedValidationPlan.status -ne 'ok' -or @($TargetedValidationPlan.scripts).Count -eq 0) {
            [void]$results.Add([pscustomobject]@{
                batchId    = $Batch.id
                validation = 'targeted_tests'
                status     = 'skipped'
                source     = [string]$TargetedValidationPlan.source
                details    = [string]$TargetedValidationPlan.reasonCode
            })
            return [pscustomobject]@{
                passed        = $false
                findingsAfter = $BaselineStaticFindings
                results       = @($results)
                failureReason = [string]$TargetedValidationPlan.reasonCode
            }
        }

        foreach ($script in @($TargetedValidationPlan.scripts)) {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                & powershell -NoProfile -ExecutionPolicy Bypass -File $script | Out-Null
                if ($LASTEXITCODE -ne 0) {
                    throw "Targeted test script exited with code $LASTEXITCODE."
                }
                [void]$results.Add([pscustomobject]@{
                    batchId    = $Batch.id
                    validation = 'targeted_tests'
                    status     = 'passed'
                    source     = [string]$TargetedValidationPlan.source
                    details    = "$(Split-Path -Leaf $script) ElapsedMs=$($stopwatch.ElapsedMilliseconds)"
                })
            }
            catch {
                [void]$results.Add([pscustomobject]@{
                    batchId    = $Batch.id
                    validation = 'targeted_tests'
                    status     = 'failed'
                    source     = [string]$TargetedValidationPlan.source
                    details    = "$(Split-Path -Leaf $script): $($_.Exception.Message)"
                })
                return [pscustomobject]@{
                    passed        = $false
                    findingsAfter = $BaselineStaticFindings
                    results       = @($results)
                    failureReason = 'targeted-tests-failed'
                }
            }
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
    [void]$results.Add([pscustomobject]@{
        batchId    = $Batch.id
        validation = 'static_scan'
        status     = if ($staticPassed) { 'passed' } else { 'failed' }
        source     = 'static-scan'
        details    = "Before=$($BaselineStaticFindings.Count) After=$($afterFindings.Count) Resolved=$resolvedCount Expected=$($Batch.expectedFindingsResolved.Count) NewHigh=$($newHigh.Count)"
    })

    [pscustomobject]@{
        passed        = $staticPassed
        findingsAfter = @($afterFindings)
        results       = @($results)
        failureReason = if ($staticPassed) { $null } else { 'static-scan-failed' }
    }
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

    foreach ($batch in @($PlannedBatches)) {
        $fingerprint = Get-RuleHarnessBatchFingerprint -Batch $batch
        $riskAssessment = Get-RuleHarnessBatchRiskAssessment -Batch $batch -RepoRoot $RepoRoot -Config $Config -MutationMode $MutationState.mode
        $ownershipAssessment = Get-RuleHarnessBatchOwnershipAssessment -Batch $batch -Config $Config
        Set-RuleHarnessObjectProperty -Object $batch -Name 'fingerprint' -Value $fingerprint
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskScore' -Value $riskAssessment.score
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskLabel' -Value $riskAssessment.label
        Set-RuleHarnessObjectProperty -Object $batch -Name 'ownershipStatus' -Value $ownershipAssessment.status
    }

    $historySummary = [pscustomobject]@{
        statePath        = $historyState.path
        loadedEntryCount = $historyState.loadedEntryCount
        activeEntryCount = @($historyState.entries.Keys).Count
        gcRemovedCount   = $historyState.gcRemovedCount
        touchedEntryCount = 0
    }

    if (-not $MutationState.enabled -or $MutationState.mode -eq 'report_only') {
        [void]$decisionTrace.Add("Mutation loop disabled. Mode=$($MutationState.mode)")
        Save-RuleHarnessHistoryState -HistoryState $historyState
        return [pscustomobject]@{
            decisionTrace       = @($decisionTrace)
            validationResults   = @()
            appliedBatches      = @()
            skippedBatches      = @()
            rollbackBatches     = @()
            docEdits            = @()
            finalStaticFindings = @($finalStaticFindings)
            historySummary      = $historySummary
            commit              = [pscustomobject]@{
                attempted = $false
                created   = $false
                sha       = $null
                branch    = $branch
                message   = $null
            }
            rollback            = [pscustomobject]@{
                performed    = $false
                failedBatches = @()
            }
            failed              = $false
            applied             = $false
        }
    }

    [void]$decisionTrace.Add("Mutation loop enabled. Mode=$($MutationState.mode)")
    if ($PlannedBatches.Count -eq 0) {
        [void]$decisionTrace.Add('No planned batches were generated. Mutation stage completed without edits.')
        Save-RuleHarnessHistoryState -HistoryState $historyState
        return [pscustomobject]@{
            decisionTrace       = @($decisionTrace)
            validationResults   = @()
            appliedBatches      = @()
            skippedBatches      = @()
            rollbackBatches     = @()
            docEdits            = @()
            finalStaticFindings = @($finalStaticFindings)
            historySummary      = $historySummary
            commit              = [pscustomobject]@{
                attempted = $false
                created   = $false
                sha       = $null
                branch    = $branch
                message   = $null
            }
            rollback            = [pscustomobject]@{
                performed     = $false
                failedBatches = @()
            }
            failed              = $false
            applied             = $false
        }
    }

    $globalTargetFiles = @($PlannedBatches | ForEach-Object { $_.targetFiles } | Sort-Object -Unique)
    $globalSnapshots = Get-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -TargetFiles $globalTargetFiles
    $currentFindings = @($InitialStaticFindings)
    $touchedTargetFiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $failureTriggered = $false
    $appliedBatchObjects = [System.Collections.Generic.List[object]]::new()
    $failureReasonCode = $null

    $nonActionableReasons = if ($Config.history.PSObject.Properties.Name -contains 'nonActionableReasons') {
        @($Config.history.nonActionableReasons)
    }
    else {
        @()
    }
    $maxAttemptsPerCommit = if ($Config.history.PSObject.Properties.Name -contains 'maxAttemptsPerCommit') {
        [int]$Config.history.maxAttemptsPerCommit
    }
    else {
        2
    }

    foreach ($batch in @($PlannedBatches | Select-Object -First ([int]$Config.mutation.maxBatchesPerRun))) {
        Write-Host "Rule harness batch planning resumed. Id: $($batch.id) Kind: $($batch.kind)"
        $fingerprint = [string]$batch.fingerprint
        $historyEntry = Get-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint

        if ($MutationState.mode -eq 'doc_only' -and $batch.kind -ne 'rule_fix') {
            [void]$decisionTrace.Add("Skipped $($batch.id) because doc_only mode does not allow kind=$($batch.kind).")
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
            [void]$decisionTrace.Add("Skipped $($batch.id) because target file count exceeded maxFilesPerBatch.")
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

        $dirtyTargets = if ($Config.mutation.requireCleanTargets) {
            @(Get-RuleHarnessDirtyTargetPaths -RepoRoot $RepoRoot -TargetFiles $batch.targetFiles)
        }
        else {
            @()
        }
        if (@($dirtyTargets).Count -gt 0) {
            $historyEntry = Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'skipped' -Reason 'dirty-target-files'
            [void]$decisionTrace.Add("Rule harness batch suppressed before apply. Id: $($batch.id) Reason: dirty-target-files Targets: $($dirtyTargets -join ', ')")
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = 'dirty-target-files'
                reasonCode   = 'dirty-target-files'
                status       = 'skipped'
                targets      = $dirtyTargets
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            })
            continue
        }

        $targetedValidationPlan = Get-RuleHarnessTargetedValidationPlan -Batch $batch -RepoRoot $RepoRoot -Config $Config -ValidationRegistry $validationRegistry
        if ($Batch.kind -ne 'rule_fix' -and $targetedValidationPlan.status -ne 'ok') {
            $historyEntry = Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'skipped' -Reason ([string]$targetedValidationPlan.reasonCode)
            [void]$decisionTrace.Add("Rule harness batch suppressed before apply. Id: $($batch.id) Reason: $($targetedValidationPlan.reasonCode)")
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = [string]$targetedValidationPlan.reasonCode
                reasonCode   = [string]$targetedValidationPlan.reasonCode
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            })
            continue
        }

        $ownershipAssessment = Get-RuleHarnessBatchOwnershipAssessment -Batch $batch -Config $Config
        Set-RuleHarnessObjectProperty -Object $batch -Name 'ownershipStatus' -Value $ownershipAssessment.status
        if ($ownershipAssessment.status -eq 'rejected') {
            $historyEntry = Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'skipped' -Reason 'ownership-preflight-rejected'
            [void]$decisionTrace.Add("Rule harness batch suppressed before apply. Id: $($batch.id) Reason: ownership-preflight-rejected Detail: $($ownershipAssessment.reason)")
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = $ownershipAssessment.reason
                reasonCode   = 'ownership-preflight-rejected'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            })
            continue
        }

        $riskAssessment = Get-RuleHarnessBatchRiskAssessment -Batch $batch -RepoRoot $RepoRoot -Config $Config -MutationMode $MutationState.mode
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskScore' -Value $riskAssessment.score
        Set-RuleHarnessObjectProperty -Object $batch -Name 'riskLabel' -Value $riskAssessment.label
        if (-not $riskAssessment.allowed) {
            [void]$decisionTrace.Add("Rule harness batch suppressed before apply. Id: $($batch.id) Reason: risk-threshold-exceeded Score: $($riskAssessment.score) Threshold: $($riskAssessment.threshold)")
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = "Score $($riskAssessment.score) exceeded threshold $($riskAssessment.threshold)."
                reasonCode   = 'risk-threshold-exceeded'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = if ($null -ne $historyEntry) { [int]$historyEntry.attemptCount } else { 0 }
            })
            continue
        }

        $historyEntry = Get-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint
        if ($null -ne $historyEntry -and [string]$historyEntry.lastStatus -eq 'applied') {
            [void]$decisionTrace.Add("Rule harness batch suppressed before apply. Id: $($batch.id) Reason: already-applied-on-commit")
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
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
            [void]$decisionTrace.Add("Rule harness batch suppressed before apply. Id: $($batch.id) Reason: $($historyEntry.lastReason)")
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
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
            [void]$decisionTrace.Add("Rule harness batch suppressed before apply. Id: $($batch.id) Reason: max-attempts-reached Attempts: $($historyEntry.attemptCount)")
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'skipped'
            [void]$skippedBatches.Add([pscustomobject]@{
                id           = $batch.id
                kind         = $batch.kind
                reason       = 'max-attempts-reached'
                reasonCode   = 'max-attempts-reached'
                status       = 'skipped'
                fingerprint  = $fingerprint
                attemptCount = [int]$historyEntry.attemptCount
            })
            continue
        }

        if ($DryRun) {
            [void]$decisionTrace.Add("Dry run left $($batch.id) in proposed state.")
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

        try {
            Write-Host "Rule harness patch apply started. Batch: $($batch.id) Targets: $($batch.targetFiles -join ', ')"
            $applyResult = Invoke-RuleHarnessBatchOperations -Batch $batch -RepoRoot $RepoRoot -Config $Config
            foreach ($docEdit in $applyResult.docEditResults) {
                [void]$docEdits.Add($docEdit)
            }
            foreach ($target in $applyResult.touchedPaths) {
                [void]$touchedTargetFiles.Add($target)
            }
            Write-Host "Rule harness patch apply finished. Batch: $($batch.id) Touched: $(@($applyResult.touchedPaths).Count)"

            Write-Host "Rule harness validation started. Batch: $($batch.id)"
            $validation = Invoke-RuleHarnessBatchValidation -Batch $batch -RepoRoot $RepoRoot -Config $Config -TargetedValidationPlan $targetedValidationPlan -BaselineStaticFindings $currentFindings
            foreach ($result in $validation.results) {
                [void]$validationResults.Add($result)
            }

            if (-not $validation.passed) {
                $failureReasonCode = [string]$validation.failureReason
                [void]$decisionTrace.Add("Batch $($batch.id) failed validation: $failureReasonCode")
                [void](Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'failed' -Reason $failureReasonCode)
                $failureTriggered = $true
                break
            }

            $currentFindings = @($validation.findingsAfter)
            Set-RuleHarnessObjectProperty -Object $batch -Name 'status' -Value 'applied'
            [void]$appliedBatches.Add([pscustomobject]@{
                id          = $batch.id
                kind        = $batch.kind
                targetFiles = $batch.targetFiles
                status      = 'applied'
                reason      = $batch.reason
            })
            [void]$appliedBatchObjects.Add($batch)
            [void]$decisionTrace.Add("Batch $($batch.id) applied successfully and reduced findings.")
        }
        catch {
            [void]$validationResults.Add([pscustomobject]@{
                batchId    = $batch.id
                validation = 'apply'
                status     = 'failed'
                source     = 'apply'
                details    = $_.Exception.Message
            })
            [void]$decisionTrace.Add("Batch $($batch.id) threw an exception: $($_.Exception.Message)")
            $failureReasonCode = 'apply-failed'
            [void](Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint $fingerprint -Status 'failed' -Reason $failureReasonCode)
            $failureTriggered = $true
            break
        }

        if ($failureTriggered -and $Config.mutation.stopOnFirstFailure) {
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

    $commitResult = [pscustomobject]@{
        attempted = $false
        created   = $false
        sha       = $null
        branch    = $branch
        message   = $null
    }

    if (-not $failureTriggered -and $touchedTargetFiles.Count -gt 0) {
        try {
            Write-Host "Rule harness commit stage started. Files: $($touchedTargetFiles.Count)"
            $commitResult = Invoke-RuleHarnessCommit -RepoRoot $RepoRoot -Config $Config -TargetFiles @($touchedTargetFiles) -AppliedBatches @($appliedBatches)
            Write-Host "Rule harness commit stage finished. Created: $($commitResult.created)"
            if ($commitResult.created) {
                foreach ($batch in $appliedBatchObjects) {
                    [void](Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint ([string]$batch.fingerprint) -Status 'applied' -Reason 'applied')
                }
                [void]$decisionTrace.Add("Created commit $($commitResult.sha) on branch $($commitResult.branch).")
            }
        }
        catch {
            Restore-RuleHarnessFileSnapshots -RepoRoot $RepoRoot -Snapshots $globalSnapshots
            $rollbackPerformed = $true
            $failureTriggered = $true
            $finalStaticFindings = @($InitialStaticFindings)
            foreach ($appliedBatch in $appliedBatches) {
                [void](Set-RuleHarnessHistoryEntry -HistoryState $historyState -Branch $branch -CommitSha $commitSha -Fingerprint ([string]($appliedBatchObjects | Where-Object id -eq $appliedBatch.id | Select-Object -First 1).fingerprint) -Status 'failed' -Reason 'commit-failed')
                [void]$rollbackBatches.Add([pscustomobject]@{
                    id     = $appliedBatch.id
                    kind   = $appliedBatch.kind
                    status = 'rolled_back'
                })
            }
            $appliedBatches.Clear()
            $commitResult = [pscustomobject]@{
                attempted = $true
                created   = $false
                sha       = $null
                branch    = $branch
                message   = $null
            }
            [void]$decisionTrace.Add("Commit failed and all mutations were rolled back: $($_.Exception.Message)")
        }
    }
    elseif (-not $failureTriggered) {
        [void]$decisionTrace.Add('No batch produced committed workspace changes.')
    }

    Save-RuleHarnessHistoryState -HistoryState $historyState
    $historySummary = [pscustomobject]@{
        statePath         = $historyState.path
        loadedEntryCount  = $historyState.loadedEntryCount
        activeEntryCount  = @($historyState.entries.Keys).Count
        gcRemovedCount    = $historyState.gcRemovedCount
        touchedEntryCount = $historyState.touchedKeys.Count
    }

    [pscustomobject]@{
        decisionTrace     = @($decisionTrace)
        validationResults = @($validationResults)
        appliedBatches    = @($appliedBatches)
        skippedBatches    = @($skippedBatches)
        rollbackBatches   = @($rollbackBatches)
        docEdits          = @($docEdits)
        finalStaticFindings = @($finalStaticFindings)
        historySummary    = $historySummary
        commit            = $commitResult
        rollback          = [pscustomobject]@{
            performed    = $rollbackPerformed
            failedBatches = @($rollbackBatches | ForEach-Object { $_.id })
        }
        failed            = $failureTriggered
        applied           = ($appliedBatches.Count -gt 0)
    }
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
    [void]$lines.Add("- Applied: $($Report.applied)")
    [void]$lines.Add("- Failed: $($Report.failed)")
    [void]$lines.Add('')

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
        [string]$SummaryPath
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

    Write-Host 'Rule harness discover stage started.'
    $scannedFeatures = @(Get-RuleHarnessFeatureDirectories -RepoRoot $RepoRoot | ForEach-Object { $_.Name })
    Write-Host "Rule harness discover stage finished. Features: $($scannedFeatures.Count)"

    Write-Host 'Rule harness static scan started.'
    $staticFindings = @(Get-RuleHarnessStaticFindings -RepoRoot $RepoRoot -Config $config)
    Write-Host "Rule harness static scan finished. Findings: $($staticFindings.Count)"

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
    }
    catch {
        [void]$harnessErrors.Add((New-RuleHarnessFinding `
            -FindingType 'missing_rule' `
            -Severity $config.severityPolicy.agentFailure `
            -OwnerDoc 'CLAUDE.md' `
            -Title 'Rule harness agent review failed' `
            -Message "Stage=diagnose TimeoutSec=$timeoutSec $($_.Exception.Message)" `
            -Evidence @([pscustomobject]@{ path = 'tools/rule-harness'; line = $null; snippet = $_.Exception.GetType().FullName }) `
            -Confidence 'high' `
            -Source 'harness' `
            -RemediationKind 'report_only'))
        $reviewedFindings = @(ConvertTo-RuleHarnessReviewedFindings -Findings $staticFindings)
        Write-Host "Rule harness diagnose stage failed. TimeoutSec: $timeoutSec Error: $($_.Exception.Message)"
    }

    $eligibleDocFindings = @(
        $reviewedFindings | Where-Object {
            $_.remediationKind -in @('rule_fix', 'mixed_fix') -and
            $_.confidence -eq 'high' -and
            (Test-RuleHarnessDocAllowed -RelativePath $_.ownerDoc -Config $config)
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
        }
        catch {
            [void]$harnessErrors.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $config.severityPolicy.agentFailure `
                -OwnerDoc 'CLAUDE.md' `
                -Title 'Rule harness doc sync failed' `
                -Message "Stage=doc_sync TimeoutSec=$timeoutSec $($_.Exception.Message)" `
                -Evidence @([pscustomobject]@{ path = 'tools/rule-harness'; line = $null; snippet = $_.Exception.GetType().FullName }) `
                -Confidence 'high' `
                -Source 'harness'))
            Write-Host "Rule harness doc sync failed. TimeoutSec: $timeoutSec Error: $($_.Exception.Message)"
        }
    }

    Write-Host 'Rule harness patch plan stage started.'
    $plannedBatches = @(Get-RuleHarnessPlannedBatches -ReviewedFindings $reviewedFindings -DocEdits $docEdits -RepoRoot $RepoRoot)
    Write-Host "Rule harness patch plan stage finished. Planned batches: $($plannedBatches.Count)"

    $mutationResult = Invoke-RuleHarnessMutationPlan `
        -PlannedBatches $plannedBatches `
        -InitialStaticFindings $staticFindings `
        -RepoRoot $RepoRoot `
        -Config $config `
        -MutationState $mutationState `
        -DryRun:$DryRun

    $findings = @($mutationResult.finalStaticFindings + $harnessErrors)
    $failed = (@($findings | Where-Object { $_.severity -eq 'high' -and $_.findingType -eq 'code_violation' }).Count -gt 0) -or
        ($harnessErrors.Count -gt 0) -or
        [bool]$mutationResult.failed

    $report = [pscustomobject]@{
        runId            = [guid]::NewGuid().ToString()
        commitSha        = ((Invoke-RuleHarnessGit -RepoRoot $RepoRoot -Arguments @('rev-parse', 'HEAD')) | Select-Object -First 1).Trim()
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
        decisionTrace     = @($mutationResult.decisionTrace)
        validationResults = @($mutationResult.validationResults)
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
