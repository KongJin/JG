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

function Get-RuleHarnessDocumentedCustomPropertyKeys {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot
    )

    $stateDoc = Join-Path $RepoRoot 'agent/state_ownership.md'
    if (-not (Test-Path -LiteralPath $stateDoc)) {
        return @()
    }

    $keys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($match in [regex]::Matches((Get-Content -Path $stateDoc -Raw), '`(?<key>[A-Za-z][A-Za-z0-9_]*)`')) {
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
                -Evidence @([pscustomobject]@{ path = 'CLAUDE.md'; line = $null; snippet = $doc })))
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
                    -Confidence 'high'))
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
                -Evidence @([pscustomobject]@{ path = $relative; line = $null; snippet = $feature.Name })))
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
                -Evidence @([pscustomobject]@{ path = $relative; line = $null; snippet = 'Expected root-level Setup/Bootstrap file' })))
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
                    -Confidence 'high'))
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
                    -Confidence 'high'))
            }
        }
    }

    $documentedKeys = Get-RuleHarnessDocumentedCustomPropertyKeys -RepoRoot $RepoRoot
    foreach ($script in Get-RuleHarnessScriptFiles -RepoRoot $RepoRoot -Config $Config) {
        $relative = ConvertTo-RuleHarnessRelativePath -RepoRoot $RepoRoot -Path $script.FullName
        foreach ($key in Get-RuleHarnessCustomPropertyKeysFromFile -FilePath $script.FullName) {
            if ($key -in $documentedKeys) {
                continue
            }

            [void]$findings.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $Config.severityPolicy.undocumentedCustomProperty `
                -OwnerDoc 'agent/state_ownership.md' `
                -Title 'Undocumented CustomProperties key' `
                -Message "CustomProperties key '$key' is used in code but not documented in agent/state_ownership.md." `
                -Evidence @([pscustomobject]@{ path = $relative; line = $null; snippet = $key }) `
                -Confidence 'high'))
        }
    }

    @($findings)
}

function Invoke-RuleHarnessChatCompletion {
    param(
        [Parameter(Mandatory)]
        [string]$Model,
        [Parameter(Mandatory)]
        [string]$SystemPrompt,
        [Parameter(Mandatory)]
        [string]$UserPrompt,
        [Parameter(Mandatory)]
        [string]$ApiKey
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

    $response = Invoke-RestMethod `
        -Uri 'https://api.openai.com/v1/chat/completions' `
        -Method Post `
        -Headers @{
            Authorization = "Bearer $ApiKey"
            'Content-Type' = 'application/json'
        } `
        -Body ($body | ConvertTo-Json -Depth 50)

    $response.choices[0].message.content
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
        [string]$Model,
        [string]$ReviewJsonPath
    )

    if ($ReviewJsonPath) {
        return @((Get-Content -Path $ReviewJsonPath -Raw | ConvertFrom-Json).findings)
    }

    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        return @($StaticFindings)
    }

    $systemPrompt = Get-Content -Path (Join-Path $RepoRoot 'tools/rule-harness/prompts/review.md') -Raw
    $payload = @{
        repository     = Split-Path -Leaf $RepoRoot
        staticFindings = @($StaticFindings | Select-Object -First $Config.llm.maxFindingsForReview)
    } | ConvertTo-Json -Depth 50

    $raw = Invoke-RuleHarnessChatCompletion -Model $Model -SystemPrompt $systemPrompt -UserPrompt $payload -ApiKey $ApiKey
    @(($raw | ConvertFrom-Json).findings)
}

function Invoke-RuleHarnessDocSync {
    param(
        [Parameter(Mandatory)]
        [object[]]$Findings,
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$ApiKey,
        [Parameter(Mandatory)]
        [string]$Model
    )

    if ([string]::IsNullOrWhiteSpace($ApiKey)) {
        return @()
    }

    $systemPrompt = Get-Content -Path (Join-Path $RepoRoot 'tools/rule-harness/prompts/doc-sync.md') -Raw
    $edits = [System.Collections.Generic.List[object]]::new()
    foreach ($group in ($Findings | Group-Object ownerDoc)) {
        $fullPath = Join-Path $RepoRoot $group.Name
        if (-not (Test-Path -LiteralPath $fullPath)) {
            continue
        }

        $payload = @{
            targetPath  = $group.Name
            currentText = Get-Content -Path $fullPath -Raw
            findings    = @($group.Group)
        } | ConvertTo-Json -Depth 50

        $raw = Invoke-RuleHarnessChatCompletion -Model $Model -SystemPrompt $systemPrompt -UserPrompt $payload -ApiKey $ApiKey
        foreach ($edit in @(($raw | ConvertFrom-Json).edits)) {
            [void]$edits.Add($edit)
        }
    }

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
    [void]$lines.Add(('- Commit: `{0}`' -f $Report.commitSha))
    [void]$lines.Add("- Scanned features: $($Report.scannedFeatures.Count)")
    [void]$lines.Add("- Findings: $($Report.findings.Count)")
    [void]$lines.Add("- Doc edits: $($Report.docEdits.Count)")
    [void]$lines.Add("- Applied: $($Report.applied)")
    [void]$lines.Add("- Failed: $($Report.failed)")
    [void]$lines.Add('')

    foreach ($severity in @('high', 'medium', 'low')) {
        $subset = @($Report.findings | Where-Object severity -eq $severity)
        if ($subset.Count -eq 0) {
            continue
        }

        [void]$lines.Add("### $severity")
        foreach ($finding in $subset) {
            [void]$lines.Add(('- [{0}] {1} - `{2}`' -f $finding.findingType, $finding.title, $finding.ownerDoc))
        }
        [void]$lines.Add('')
    }

    Add-Content -Path $SummaryPath -Value ($lines -join "`n")
}

function Invoke-RuleHarness {
    param(
        [Parameter(Mandatory)]
        [string]$RepoRoot,
        [Parameter(Mandatory)]
        [string]$ConfigPath,
        [string]$ApiKey,
        [string]$Model,
        [switch]$DryRun,
        [string]$ReviewJsonPath,
        [string]$SummaryPath
    )

    $config = Get-RuleHarnessConfig -ConfigPath $ConfigPath
    if ([string]::IsNullOrWhiteSpace($Model)) {
        $Model = $config.llm.defaultModel
    }

    $staticFindings = Get-RuleHarnessStaticFindings -RepoRoot $RepoRoot -Config $config
    $harnessErrors = [System.Collections.Generic.List[object]]::new()

    try {
        $reviewedFindings = @(
            Invoke-RuleHarnessAgentReview `
                -StaticFindings $staticFindings `
                -RepoRoot $RepoRoot `
                -Config $config `
                -ApiKey $ApiKey `
                -Model $Model `
                -ReviewJsonPath $ReviewJsonPath
        )
    }
    catch {
        [void]$harnessErrors.Add((New-RuleHarnessFinding `
            -FindingType 'missing_rule' `
            -Severity $config.severityPolicy.agentFailure `
            -OwnerDoc 'CLAUDE.md' `
            -Title 'Rule harness agent review failed' `
            -Message $_.Exception.Message `
            -Evidence @([pscustomobject]@{ path = 'tools/rule-harness'; line = $null; snippet = $_.Exception.GetType().FullName }) `
            -Confidence 'high' `
            -Source 'harness'))
        $reviewedFindings = @($staticFindings)
    }

    $eligibleDocFindings = @(
        $reviewedFindings | Where-Object {
            $_.findingType -in @('doc_drift', 'broken_reference') -and
            $_.confidence -eq 'high' -and
            (Test-RuleHarnessDocAllowed -RelativePath $_.ownerDoc -Config $config)
        }
    )

    $docEdits = @()
    if ($eligibleDocFindings.Count -gt 0) {
        try {
            $docEdits = @(
                Invoke-RuleHarnessDocSync `
                    -Findings $eligibleDocFindings `
                    -RepoRoot $RepoRoot `
                    -ApiKey $ApiKey `
                    -Model $Model
            )
        }
        catch {
            [void]$harnessErrors.Add((New-RuleHarnessFinding `
                -FindingType 'missing_rule' `
                -Severity $config.severityPolicy.agentFailure `
                -OwnerDoc 'CLAUDE.md' `
                -Title 'Rule harness doc sync failed' `
                -Message $_.Exception.Message `
                -Evidence @([pscustomobject]@{ path = 'tools/rule-harness'; line = $null; snippet = $_.Exception.GetType().FullName }) `
                -Confidence 'high' `
                -Source 'harness'))
        }
    }

    $applyResult = Invoke-RuleHarnessDocEdits -Edits $docEdits -RepoRoot $RepoRoot -Config $config -DryRun:$DryRun
    $findings = @($reviewedFindings + $harnessErrors)
    $failed = (@($findings | Where-Object { $_.severity -eq 'high' -and $_.findingType -eq 'code_violation' }).Count -gt 0) -or ($harnessErrors.Count -gt 0)

    $report = [pscustomobject]@{
        runId           = [guid]::NewGuid().ToString()
        commitSha       = (git -C $RepoRoot rev-parse HEAD).Trim()
        scannedFeatures = @(Get-RuleHarnessFeatureDirectories -RepoRoot $RepoRoot | ForEach-Object { $_.Name })
        findings        = $findings
        docEdits        = @($applyResult.edits)
        applied         = [bool]$applyResult.touched
        failed          = [bool]$failed
    }

    if (-not [string]::IsNullOrWhiteSpace($SummaryPath)) {
        Write-RuleHarnessSummary -Report $report -SummaryPath $SummaryPath
    }

    $report
}

Export-ModuleMember -Function Get-RuleHarnessConfig, Get-RuleHarnessStaticFindings, Invoke-RuleHarnessDocEdits, Invoke-RuleHarness, Test-RuleHarnessDocAllowed
