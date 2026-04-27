param(
    [Parameter(Mandatory = $true)][string]$SurfaceId,
    [string]$HtmlPath = "",
    [string]$ImagePath = "",
    [string]$PresentationOutputPath = "",
    [string]$TargetAssetPath = "",
    [switch]$CanGenerateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
}

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return [System.IO.Path]::GetFullPath($PathValue)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-RepoRoot) $PathValue))
}

function Read-JsonFile {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $resolvedPath = Resolve-RepoPath -PathValue $PathValue
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "File not found: $resolvedPath"
    }

    return Get-Content -LiteralPath $resolvedPath -Raw | ConvertFrom-Json
}

function Convert-ToRepoRelativePath {
    param([Parameter(Mandatory = $true)][string]$AbsolutePath)

    $repoRoot = (Get-RepoRoot).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $fullPath = [System.IO.Path]::GetFullPath($AbsolutePath)
    if ($fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($repoRoot.Length).Replace('\', '/')
    }

    return $fullPath.Replace('\', '/')
}

function Read-AllText {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $resolvedPath = Resolve-RepoPath -PathValue $PathValue
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "File not found: $resolvedPath"
    }

    return Get-Content -LiteralPath $resolvedPath -Raw
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [Parameter(Mandatory = $true)][object]$InputObject
    )

    $resolvedPath = Resolve-RepoPath -PathValue $PathValue
    $directoryPath = Split-Path -Parent $resolvedPath
    if (-not [string]::IsNullOrWhiteSpace($directoryPath) -and -not (Test-Path -LiteralPath $directoryPath)) {
        New-Item -ItemType Directory -Path $directoryPath -Force | Out-Null
    }

    $json = $InputObject | ConvertTo-Json -Depth 30
    [System.IO.File]::WriteAllText($resolvedPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
    return $resolvedPath
}

function Convert-ToSurfaceSlug {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $expandedValue = [regex]::Replace($Value.Trim(), '(?<=[a-z0-9])([A-Z])', '-$1')
    $lowerValue = $expandedValue.ToLowerInvariant()
    $normalized = [regex]::Replace($lowerValue, '[^a-z0-9]+', '-')
    $collapsed = [regex]::Replace($normalized, '-{2,}', '-')
    return $collapsed.Trim('-')
}

function Get-SlugTokens {
    param([string]$Value)

    $slug = Convert-ToSurfaceSlug -Value $Value
    if ([string]::IsNullOrWhiteSpace($slug)) {
        return @()
    }

    return @($slug -split '-' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-UniqueSlugList {
    param([string[]]$Values)

    $seen = New-Object System.Collections.Generic.HashSet[string] ([System.StringComparer]::OrdinalIgnoreCase)
    $result = New-Object System.Collections.Generic.List[string]
    foreach ($value in @($Values)) {
        $slug = Convert-ToSurfaceSlug -Value $value
        if ([string]::IsNullOrWhiteSpace($slug)) {
            continue
        }

        if ($seen.Add($slug)) {
            $result.Add($slug)
        }
    }

    return @($result.ToArray())
}

function Get-HtmlTitleText {
    param([Parameter(Mandatory = $true)][string]$HtmlPath)

    if (-not (Test-Path -LiteralPath $HtmlPath)) {
        return ""
    }

    $html = Get-Content -LiteralPath $HtmlPath -Raw
    $titleMatch = [regex]::Match($html, '<title>\s*(?<text>.*?)\s*</title>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $titleMatch.Success) {
        return ""
    }

    return ([System.Net.WebUtility]::HtmlDecode([string]$titleMatch.Groups["text"].Value)).Trim()
}

function Get-MetadataAliasCandidates {
    param(
        [Parameter(Mandatory = $true)][object]$Meta,
        [Parameter(Mandatory = $true)][string]$HtmlPath
    )

    $values = New-Object System.Collections.Generic.List[string]

    if ($null -ne $Meta.PSObject.Properties["screenName"]) {
        $values.Add([string]$Meta.screenName)
    }

    if ($null -ne $Meta.PSObject.Properties["prompt"]) {
        $prompt = [string]$Meta.prompt
        if (-not [string]::IsNullOrWhiteSpace($prompt)) {
            $firstLine = ($prompt -split "(\r?\n)+" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
            if (-not [string]::IsNullOrWhiteSpace([string]$firstLine)) {
                $values.Add([string]$firstLine)
            }
        }
    }

    $titleText = Get-HtmlTitleText -HtmlPath $HtmlPath
    if (-not [string]::IsNullOrWhiteSpace($titleText)) {
        $values.Add($titleText)
    }

    return @(Get-UniqueSlugList -Values @($values))
}

function Get-MetadataSurfaceMatchScore {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [AllowEmptyCollection()][string[]]$AliasCandidates
    )

    $surfaceSlug = Convert-ToSurfaceSlug -Value $SurfaceId
    if ([string]::IsNullOrWhiteSpace($surfaceSlug)) {
        return 0
    }

    $surfaceTokens = @(Get-SlugTokens -Value $surfaceSlug)
    $bestScore = 0
    foreach ($alias in @($AliasCandidates)) {
        if ([string]::IsNullOrWhiteSpace($alias)) {
            continue
        }

        $aliasTokens = @(Get-SlugTokens -Value $alias)
        if ($aliasTokens.Count -eq 0) {
            continue
        }

        if ($alias -eq $surfaceSlug) {
            return 100
        }

        if ($alias.Contains($surfaceSlug) -or $surfaceSlug.Contains($alias)) {
            if ([Math]::Min($surfaceTokens.Count, $aliasTokens.Count) -ge 2) {
                $bestScore = [Math]::Max($bestScore, 75)
            }
            else {
                $bestScore = [Math]::Max($bestScore, 35)
            }
            continue
        }

        $overlapCount = @($surfaceTokens | Where-Object { $aliasTokens -contains $_ }).Count
        if ($overlapCount -eq 0) {
            continue
        }

        $sharedPrefix = if ($surfaceTokens.Count -gt 0 -and $aliasTokens.Count -gt 0 -and $surfaceTokens[0] -eq $aliasTokens[0]) { 10 } else { 0 }
        $score = ($overlapCount * 20) + $sharedPrefix
        if ($overlapCount -eq $surfaceTokens.Count -or $overlapCount -eq $aliasTokens.Count) {
            $score += 10
        }

        $bestScore = [Math]::Max($bestScore, $score)
    }

    return $bestScore
}

function Get-ArtifactSourceMetadata {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $normalizedSurfaceId = Convert-ToSurfaceSlug -Value $SurfaceId
    $artifactRoot = Resolve-RepoPath -PathValue "artifacts/stitch"
    if (-not (Test-Path -LiteralPath $artifactRoot)) {
        return $null
    }

    $candidates = New-Object System.Collections.Generic.List[object]
    foreach ($metaFile in Get-ChildItem -LiteralPath $artifactRoot -Filter "meta.json" -Recurse -File) {
        $meta = Get-Content -LiteralPath $metaFile.FullName -Raw | ConvertFrom-Json
        $screenDirectory = Split-Path -Parent $metaFile.FullName
        $htmlPath = Join-Path $screenDirectory "screen.html"
        $imagePath = Join-Path $screenDirectory "screen.png"
        if (-not (Test-Path -LiteralPath $htmlPath) -or -not (Test-Path -LiteralPath $imagePath)) {
            continue
        }

        $aliasCandidates = @(Get-MetadataAliasCandidates -Meta $meta -HtmlPath $htmlPath)
        $score = Get-MetadataSurfaceMatchScore -SurfaceId $normalizedSurfaceId -AliasCandidates $aliasCandidates
        if ($score -lt 50) {
            continue
        }

        $candidates.Add([PSCustomObject]@{
            score = $score
            fetchedAt = if ($null -ne $meta.PSObject.Properties["fetchedAt"]) { [string]$meta.fetchedAt } else { "" }
            sourceRef = $normalizedSurfaceId
            projectId = if ($null -ne $meta.PSObject.Properties["projectId"]) { [string]$meta.projectId } else { "" }
            screenId = if ($null -ne $meta.PSObject.Properties["screenId"]) { [string]$meta.screenId } else { "" }
            url = if ($null -ne $meta.PSObject.Properties["htmlUrl"] -and -not [string]::IsNullOrWhiteSpace([string]$meta.htmlUrl)) { [string]$meta.htmlUrl } elseif ($null -ne $meta.PSObject.Properties["imageUrl"]) { [string]$meta.imageUrl } else { "" }
            htmlPath = Convert-ToRepoRelativePath -AbsolutePath $htmlPath
            imagePath = Convert-ToRepoRelativePath -AbsolutePath $imagePath
            aliasCandidates = @($aliasCandidates)
        })
    }

    if ($candidates.Count -eq 0) {
        return $null
    }

    return @($candidates | Sort-Object -Property @{ Expression = "score"; Descending = $true }, @{ Expression = "fetchedAt"; Descending = $true })[0]
}

function Get-DesignSourceMetadata {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $designRoot = Resolve-RepoPath -PathValue ".stitch/designs"
    if (-not (Test-Path -LiteralPath $designRoot)) {
        return $null
    }

    $candidateFiles = New-Object System.Collections.Generic.List[object]
    foreach ($candidate in @(Get-ChildItem -LiteralPath $designRoot -Filter "*.html" -File)) {
        $pairedImagePath = Join-Path $candidate.DirectoryName ($candidate.BaseName + ".png")
        if (-not (Test-Path -LiteralPath $pairedImagePath)) {
            continue
        }

        $aliasCandidates = Get-UniqueSlugList -Values @(
            $candidate.BaseName,
            (Get-HtmlTitleText -HtmlPath $candidate.FullName)
        )
        $score = Get-MetadataSurfaceMatchScore -SurfaceId $SurfaceId -AliasCandidates @($aliasCandidates)
        if ($score -le 0) {
            continue
        }

        $candidateFiles.Add([PSCustomObject]@{
            file = $candidate
            imagePath = $pairedImagePath
            score = $score
        })
    }

    foreach ($candidate in @($candidateFiles | Sort-Object -Property @{ Expression = "score"; Descending = $true }, @{ Expression = { $_.file.BaseName }; Descending = $false })) {
        $relativeHtmlPath = Convert-ToRepoRelativePath -AbsolutePath $candidate.file.FullName
        $relativeImagePath = Convert-ToRepoRelativePath -AbsolutePath $candidate.imagePath
        return [PSCustomObject]@{
            sourceRef = [string]$candidate.file.BaseName
            projectId = ""
            screenId = [string]$candidate.file.BaseName
            url = $relativeImagePath
            htmlPath = $relativeHtmlPath
            imagePath = $relativeImagePath
        }
    }

    return $null
}

function Get-ActiveSourceFreezePaths {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $artifactMetadata = Get-ArtifactSourceMetadata -SurfaceId $SurfaceId
    if ($null -ne $artifactMetadata) {
        return $artifactMetadata
    }

    return Get-DesignSourceMetadata -SurfaceId $SurfaceId
}

function Get-OptionalMatch {
    param(
        [Parameter(Mandatory = $true)][string]$InputText,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    return [regex]::Match($InputText, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
}

function Get-RequiredMatch {
    param(
        [Parameter(Mandatory = $true)][string]$InputText,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$ErrorMessage
    )

    $match = Get-OptionalMatch -InputText $InputText -Pattern $Pattern
    if (-not $match.Success) {
        throw $ErrorMessage
    }

    return $match
}

function Clean-InnerText {
    param([Parameter(Mandatory = $true)][string]$Value)

    $withoutIcons = [regex]::Replace($Value, '<span[^>]*material-symbols-outlined[^>]*>.*?</span>', ' ', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $withoutTags = [regex]::Replace($withoutIcons, '<[^>]+>', ' ')
    $decoded = [System.Net.WebUtility]::HtmlDecode($withoutTags)
    return ([regex]::Replace($decoded, '\s+', ' ')).Trim()
}

function Convert-MaterialSymbolToFallbackText {
    param([string]$Text)

    $normalized = ([string]$Text).Trim()
    switch ($normalized) {
        "settings" { return "*" }
        "smart_toy" { return "AI" }
        "security" { return "D" }
        "precision_manufacturing" { return "P" }
        "add" { return "+" }
        "swords" { return "X" }
        "view_in_ar" { return "[]" }
        "speed" { return ">" }
        "filter_center_focus" { return "[]" }
        "garage" { return "G" }
        "radar" { return "R" }
        "stars" { return "*" }
        "terminal" { return ">" }
        "android" { return "MECH" }
        "save" { return "" }
        default { return $normalized }
    }
}

function Split-ClassTokens {
    param([string]$Classes)

    if ([string]::IsNullOrWhiteSpace($Classes)) {
        return @()
    }

    return @($Classes -split '\s+' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-SpacingScaleValue {
    param([Parameter(Mandatory = $true)][string]$Token)

    $scale = @{
        "0" = 0
        "0.5" = 2
        "1" = 4
        "1.5" = 6
        "2" = 8
        "2.5" = 10
        "3" = 12
        "3.5" = 14
        "4" = 16
        "5" = 20
        "6" = 24
        "7" = 28
        "8" = 32
        "9" = 36
        "10" = 40
        "12" = 48
        "14" = 56
        "16" = 64
        "20" = 80
        "24" = 96
        "28" = 112
        "32" = 128
        "36" = 144
        "40" = 160
        "44" = 176
        "48" = 192
        "52" = 208
        "56" = 224
        "60" = 240
        "64" = 256
        "72" = 288
        "80" = 320
        "96" = 384
    }

    if (-not $scale.ContainsKey($Token)) {
        throw "Unsupported spacing token '$Token'."
    }

    return [int]$scale[$Token]
}

function Get-TextSizePx {
    param([Parameter(Mandatory = $true)][string]$Classes)

    foreach ($token in Split-ClassTokens -Classes $Classes) {
        if ($token -match '^text-\[(?<px>\d+)px\]$') {
            return [int]$Matches["px"]
        }

        switch ($token) {
            "text-xs" { return 12 }
            "text-sm" { return 14 }
            "text-base" { return 16 }
            "text-lg" { return 18 }
            "text-xl" { return 20 }
        }
    }

    return 14
}

function Get-ClassValuePx {
    param(
        [Parameter(Mandatory = $true)][string]$Classes,
        [Parameter(Mandatory = $true)][string[]]$Prefixes,
        [int]$DefaultValue = 0
    )

    foreach ($token in Split-ClassTokens -Classes $Classes) {
        foreach ($prefix in $Prefixes) {
            if ($token -match ('^{0}-(?<value>[0-9.]+)$' -f [regex]::Escape($prefix))) {
                return Get-SpacingScaleValue -Token $Matches["value"]
            }

            if ($token -match ('^{0}-\[(?<px>\d+)px\]$' -f [regex]::Escape($prefix))) {
                return [int]$Matches["px"]
            }
        }
    }

    return $DefaultValue
}

function Get-ColorTokenFromClasses {
    param(
        [Parameter(Mandatory = $true)][string]$Classes,
        [Parameter(Mandatory = $true)][string]$Prefix
    )

    foreach ($token in Split-ClassTokens -Classes $Classes) {
        if (-not $token.StartsWith("$Prefix-", [System.StringComparison]::Ordinal)) {
            continue
        }

        $value = $token.Substring($Prefix.Length + 1)
        if ($Prefix -eq "text" -and ($value -match '^(xs|sm|base|lg|xl|\[[0-9]+px\])$')) {
            continue
        }

        return $value
    }

    return ""
}

function Get-MaxWidthPx {
    param([Parameter(Mandatory = $true)][string]$Classes)

    foreach ($token in Split-ClassTokens -Classes $Classes) {
        if ($token -match '^max-w-\[(?<px>\d+)px\]$') {
            return [int]$Matches["px"]
        }

        switch ($token) {
            "max-w-sm" { return 358 }
            "max-w-xs" { return 320 }
        }
    }

    return 320
}

function Get-TailwindPalette {
    return @{
        "black" = "#000000"
        "white" = "#FFFFFF"
        "gray-900" = "#111827"
        "zinc-950" = "#09090B"
        "zinc-900" = "#18181B"
        "zinc-800" = "#27272A"
        "zinc-700" = "#3F3F46"
        "zinc-600" = "#52525B"
        "zinc-500" = "#71717A"
        "zinc-400" = "#A1A1AA"
        "zinc-300" = "#D4D4D8"
        "zinc-200" = "#E4E4E7"
        "zinc-100" = "#F4F4F5"
        "amber-400" = "#FBBF24"
        "amber-500" = "#F59E0B"
        "blue-400" = "#60A5FA"
        "blue-500" = "#5EB6FF"
    }
}

function Get-SurfaceSourcePath {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [Parameter(Mandatory = $true)][string]$Extension
    )

    $activeSourceFreeze = Get-ActiveSourceFreezePaths -SurfaceId $SurfaceId
    if ($null -ne $activeSourceFreeze) {
        $propertyName = if ($Extension -eq "html") { "htmlPath" } else { "imagePath" }
        $activePathProperty = $activeSourceFreeze.PSObject.Properties[$propertyName]
        $activePath = if ($null -ne $activePathProperty) { [string]$activePathProperty.Value } else { "" }
        if (-not [string]::IsNullOrWhiteSpace($activePath)) {
            return $activePath
        }
    }

    $designRoot = Resolve-RepoPath -PathValue ".stitch/designs"
    $candidate = Get-ChildItem -LiteralPath $designRoot -File | Where-Object { $_.Name -like "*$SurfaceId.$Extension" } | Select-Object -First 1
    if ($null -eq $candidate) {
        throw "Could not resolve '.$Extension' source for surface '$SurfaceId' under '$designRoot'."
    }

    return Convert-ToRepoRelativePath -AbsolutePath $candidate.FullName
}

function Convert-ToPascalCase {
    param([Parameter(Mandatory = $true)][string]$Value)

    return ((($Value -split '[-_ ]+') | Where-Object { $_ } | ForEach-Object {
        if ($_.Length -eq 1) {
            $_.ToUpperInvariant()
        }
        else {
            $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1)
        }
    }) -join '')
}

function Get-FeatureName {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $tokens = @(Get-SlugTokens -Value $SurfaceId)
    foreach ($token in $tokens) {
        switch ($token) {
            { $_ -in @("lobby", "room", "rooms", "create") } { return "Lobby" }
            "garage" { return "Garage" }
            { $_ -in @("account", "delete", "login") } { return "Account" }
            { $_ -in @("common", "error") } { return "Common" }
            { $_ -in @("battle", "wave", "core", "hud") } { return "Battle" }
            { $_ -in @("result", "victory", "defeat", "mission", "feedback") } { return "Result" }
        }
    }

    if ($tokens.Count -eq 0) {
        return "Shared"
    }

    return Convert-ToPascalCase -Value $tokens[0]
}

function Get-DefaultTargetAssetPath {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $featureName = Get-FeatureName -SurfaceId $SurfaceId
    $baseName = Convert-ToPascalCase -Value $SurfaceId
    $assetRootName = if ($baseName.EndsWith("Dialog", [System.StringComparison]::Ordinal)) {
        "{0}Root" -f $baseName
    }
    else {
        "{0}DialogRoot" -f $baseName
    }
    $assetName = "$assetRootName.uxml"
    return "Assets/UI/UIToolkit/$featureName/$assetName"
}

function Get-ActionSpec {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Position
    )

    if ($Text -match '삭제') {
        return [PSCustomObject]@{
            id = "delete-cta"
            outcome = "delete-account"
            hostName = "DeleteButton"
            variant = "destructive-confirm"
        }
    }

    if ($Text -match '재시도') {
        return [PSCustomObject]@{
            id = "retry-cta"
            outcome = "retry-network-link"
            hostName = "RetryButton"
            variant = "signal-orange"
        }
    }

    if ($Text -match '취소') {
        return [PSCustomObject]@{
            id = "cancel-cta"
            outcome = "dismiss-delete-confirm"
            hostName = "CancelButton"
            variant = "dismiss"
        }
    }

    if ($Text -match '종료') {
        return [PSCustomObject]@{
            id = "dismiss-cta"
            outcome = "dismiss-error-dialog"
            hostName = "DismissButton"
            variant = "dismiss"
        }
    }

    if ($Position -eq "primary") {
        return [PSCustomObject]@{
            id = "primary-cta"
            outcome = "primary-action"
            hostName = "PrimaryActionButton"
            variant = "primary"
        }
    }

    return [PSCustomObject]@{
        id = "secondary-cta"
        outcome = "secondary-action"
        hostName = "SecondaryActionButton"
        variant = "dismiss"
    }
}

function Get-OverlaySemanticSpec {
    param(
        [Parameter(Mandatory = $true)][string]$PrimaryText,
        [Parameter(Mandatory = $true)][bool]$HasSummary
    )

    if ($PrimaryText -match '삭제') {
        return [PSCustomObject]@{
            headerBlockId = "warning-header"
            bodyBlockId = "confirmation-copy"
            titleVariant = "danger-title"
            summaryVariant = "critical-action-required"
            bodyVariant = "danger-copy"
            footerVariant = "stacked-actions"
            requiredChecks = @(
                "destructive-header-dominant",
                "critical-copy-readable",
                "cancel-secondary-weight",
                "delete-cta-dominant",
                "overlay-scrim-focus-isolation"
            )
        }
    }

    if ($PrimaryText -match '재시도') {
        return [PSCustomObject]@{
            headerBlockId = "error-header"
            bodyBlockId = "error-copy"
            titleVariant = "error-title"
            summaryVariant = ""
            bodyVariant = "error-copy"
            footerVariant = "dual-actions"
            requiredChecks = @(
                "error-title-dominant",
                "retry-cta-dominant",
                "dismiss-secondary-weight",
                "overlay-scrim-focus-isolation",
                "network-copy-readable"
            )
        }
    }

    return [PSCustomObject]@{
        headerBlockId = "dialog-header"
        bodyBlockId = "dialog-copy"
        titleVariant = "dialog-title"
        summaryVariant = $(if ($HasSummary) { "dialog-summary" } else { "" })
        bodyVariant = "dialog-copy"
        footerVariant = "dialog-actions"
        requiredChecks = @(
            "dialog-title-readable",
            "primary-cta-dominant",
            "overlay-scrim-focus-isolation"
        )
    }
}

function Get-SetIdFromPath {
    param(
        [Parameter(Mandatory = $true)][string]$PathValue,
        [string]$SourceRef = ""
    )

    foreach ($candidate in @($SourceRef, [System.IO.Path]::GetFileNameWithoutExtension($PathValue))) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if ($candidate -match '^(?<set>set-[a-z])\-') {
            return $Matches["set"]
        }
    }

    return "set-unknown"
}

function Get-DialogContext {
    param([Parameter(Mandatory = $true)][string]$Html)

    if ($Html -notmatch '(role="dialog"|Overlay Scrim|Scrim Overlay|Modal Panel|absolute inset-0 bg-[^"]+/[0-9]+)') {
        throw "Could not detect overlay dialog structure from source HTML."
    }

    $scrimMatch = Get-OptionalMatch `
        -InputText $Html `
        -Pattern '(?:<!--\s*Overlay Scrim\s*-->|<!--\s*Scrim Overlay\s*-->|<!--\s*Tactical Scrim / Overlay\s*-->)\s*<div[^>]*class="(?<classes>[^"]+)"'
    $panelMatch = Get-RequiredMatch `
        -InputText $Html `
        -Pattern '(?:<!--\s*Modal Panel\s*-->|<!--\s*Error Dialog Panel\s*-->)\s*<div[^>]*class="(?<classes>[^"]+)"' `
        -ErrorMessage "Could not find overlay panel classes."
    $headerMatch = Get-RequiredMatch `
        -InputText $Html `
        -Pattern '(?:<!--\s*Modal Header\s*-->|<!--\s*Panel Header\s*-->)\s*<div[^>]*class="(?<classes>[^"]+)"[^>]*>(?<inner>.*?)</div>\s*(?:<!--\s*Modal Body(?:\s*\(Form\))?\s*-->|<!--\s*Panel Content\s*-->)' `
        -ErrorMessage "Could not find overlay header section."
    $bodyMatch = Get-RequiredMatch `
        -InputText $Html `
        -Pattern '(?:<!--\s*Modal Body(?:\s*\(Form\))?\s*-->|<!--\s*Panel Content\s*-->)\s*<div[^>]*class="(?<classes>[^"]+)"[^>]*>(?<inner>.*?)(?=(?:<!--\s*Modal Footer(?:\s*\(Actions\))?\s*-->|</div>\s*</div>))' `
        -ErrorMessage "Could not find overlay body section."
    $footerMatch = Get-OptionalMatch `
        -InputText $Html `
        -Pattern '(?:<!--\s*Modal Footer(?:\s*\(Actions\))?\s*-->)\s*<div[^>]*class="(?<classes>[^"]+)"[^>]*>(?<inner>.*?)</div>'

    $bodyInner = [string]$bodyMatch.Groups["inner"].Value
    $titleMatch = Get-RequiredMatch `
        -InputText ([string]$headerMatch.Groups["inner"].Value) `
        -Pattern '<h2[^>]*class="(?<classes>[^"]+)"[^>]*>\s*(?<text>.*?)\s*</h2>' `
        -ErrorMessage "Could not find dialog title."
    $summaryMatch = Get-OptionalMatch `
        -InputText ([string]$headerMatch.Groups["inner"].Value) `
        -Pattern '<p[^>]*class="(?<classes>[^"]+)"[^>]*>\s*(?<text>.*?)\s*</p>'
    $bodyTextMatch = Get-RequiredMatch `
        -InputText $bodyInner `
        -Pattern '<p[^>]*class="(?<classes>[^"]+)"[^>]*>\s*(?<text>.*?)\s*</p>' `
        -ErrorMessage "Could not find dialog body copy."

    $bodyTextClasses = [string]$bodyTextMatch.Groups["classes"].Value
    $bodyText = Clean-InnerText -Value ([string]$bodyTextMatch.Groups["text"].Value)

    $footerSectionHtml = if ($footerMatch.Success) { [string]$footerMatch.Groups["inner"].Value } else { $bodyInner }
    $footerClasses = if ($footerMatch.Success) {
        [string]$footerMatch.Groups["classes"].Value
    }
    else {
        (Get-RequiredMatch `
            -InputText $bodyInner `
            -Pattern '<div[^>]*class="(?<classes>[^"]*flex[^"]*gap-[^"]*)"[^>]*>\s*<button' `
            -ErrorMessage "Could not find dialog footer action row.").Groups["classes"].Value
    }

    $buttonMatches = [regex]::Matches($footerSectionHtml, '<button[^>]*class="(?<classes>[^"]+)"[^>]*>\s*(?<content>.*?)\s*</button>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($buttonMatches.Count -lt 2) {
        throw "Expected at least two dialog footer buttons."
    }

    $iconBadgeMatch = Get-OptionalMatch `
        -InputText ([string]$headerMatch.Groups["inner"].Value) `
        -Pattern '<div[^>]*class="(?<classes>[^"]*w-[^"]*h-[^"]*bg-[^"]*)"[^>]*>\s*<span class="material-symbols-outlined[^"]*"[^>]*>(?<text>.*?)</span>'
    $iconTextMatch = Get-OptionalMatch `
        -InputText ([string]$headerMatch.Groups["inner"].Value) `
        -Pattern '<span class="material-symbols-outlined(?: (?<classes>[^"]+))?"[^>]*>\s*(?<text>.*?)\s*</span>'

    $iconKind = if ($iconBadgeMatch.Success) { "badge" } else { "text" }
    $iconClasses = if ($iconBadgeMatch.Success) { [string]$iconBadgeMatch.Groups["classes"].Value } else { [string]$iconTextMatch.Groups["classes"].Value }
    $iconText = if ($iconBadgeMatch.Success) {
        Clean-InnerText -Value ([string]$iconBadgeMatch.Groups["text"].Value)
    }
    else {
        Clean-InnerText -Value ([string]$iconTextMatch.Groups["text"].Value)
    }

    return [PSCustomObject]@{
        scrimClasses = $(if ($scrimMatch.Success) { [string]$scrimMatch.Groups["classes"].Value } else { "" })
        panelClasses = [string]$panelMatch.Groups["classes"].Value
        headerClasses = [string]$headerMatch.Groups["classes"].Value
        bodyClasses = [string]$bodyMatch.Groups["classes"].Value
        footerClasses = [string]$footerClasses
        titleClasses = [string]$titleMatch.Groups["classes"].Value
        titleText = Clean-InnerText -Value ([string]$titleMatch.Groups["text"].Value)
        summaryClasses = $(if ($summaryMatch.Success) { [string]$summaryMatch.Groups["classes"].Value } else { "" })
        summaryText = $(if ($summaryMatch.Success) { Clean-InnerText -Value ([string]$summaryMatch.Groups["text"].Value) } else { "" })
        bodyTextClasses = $bodyTextClasses
        bodyText = $bodyText
        secondaryButtonClasses = [string]$buttonMatches[0].Groups["classes"].Value
        secondaryButtonText = Clean-InnerText -Value ([string]$buttonMatches[0].Groups["content"].Value)
        primaryButtonClasses = [string]$buttonMatches[1].Groups["classes"].Value
        primaryButtonText = Clean-InnerText -Value ([string]$buttonMatches[1].Groups["content"].Value)
        iconKind = $iconKind
        iconClasses = $iconClasses
        iconText = $iconText
    }
}

function Get-WorkspaceSlotItems {
    param([Parameter(Mandatory = $true)][string]$Html)

    $items = New-Object System.Collections.Generic.List[object]
    $buttonMatches = [regex]::Matches($Html, '<button class="(?<classes>[^"]+)"[^>]*>(?<inner>.*?)</button>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($buttonMatch in $buttonMatches) {
        $inner = [string]$buttonMatch.Groups["inner"].Value
        $labelMatches = [regex]::Matches($inner, '<span[^>]*>\s*(?<text>.*?)\s*</span>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
        if ($labelMatches.Count -lt 2) {
            continue
        }

        $iconMatch = Get-OptionalMatch -InputText $inner -Pattern '<span class="material-symbols-outlined[^"]*"[^>]*>\s*(?<text>.*?)\s*</span>'
        $items.Add([PSCustomObject]@{
            classes = [string]$buttonMatch.Groups["classes"].Value
            unitText = Clean-InnerText -Value ([string]$labelMatches[0].Groups["text"].Value)
            roleText = Clean-InnerText -Value ([string]$labelMatches[$labelMatches.Count - 1].Groups["text"].Value)
            iconText = if ($iconMatch.Success) { Convert-MaterialSymbolToFallbackText -Text (Clean-InnerText -Value ([string]$iconMatch.Groups["text"].Value)) } else { "" }
            active = ([string]$buttonMatch.Groups["classes"].Value -match 'blue-500')
        })
    }

    return @($items.ToArray())
}

function Get-WorkspaceFocusTabs {
    param([Parameter(Mandatory = $true)][string]$Html)

    $items = New-Object System.Collections.Generic.List[object]
    $buttonMatches = [regex]::Matches($Html, '<button class="(?<classes>[^"]+)"[^>]*>(?<inner>.*?)</button>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($buttonMatch in $buttonMatches) {
        $inner = [string]$buttonMatch.Groups["inner"].Value
        $iconMatch = Get-OptionalMatch -InputText $inner -Pattern '<span class="material-symbols-outlined[^"]*"[^>]*>\s*(?<text>.*?)\s*</span>'
        $labelInner = [regex]::Replace(
            $inner,
            '<span class="material-symbols-outlined[^"]*"[^>]*>.*?</span>',
            '',
            [System.Text.RegularExpressions.RegexOptions]::Singleline)
        $label = Clean-InnerText -Value $labelInner
        if ([string]::IsNullOrWhiteSpace($label)) {
            continue
        }

        $items.Add([PSCustomObject]@{
            classes = [string]$buttonMatch.Groups["classes"].Value
            labelText = $label
            iconText = if ($iconMatch.Success) { Convert-MaterialSymbolToFallbackText -Text (Clean-InnerText -Value ([string]$iconMatch.Groups["text"].Value)) } else { "" }
            active = ([string]$buttonMatch.Groups["classes"].Value -match 'blue-500')
        })
    }

    return @($items.ToArray())
}

function Get-WorkspaceEditorContext {
    param([Parameter(Mandatory = $true)][string]$Html)

    $titleMatch = Get-OptionalMatch -InputText $Html -Pattern '<h2[^>]*class="(?<classes>[^"]+)"[^>]*>\s*(?<text>.*?)\s*</h2>'
    $descriptionMatch = Get-OptionalMatch -InputText $Html -Pattern '<p[^>]*class="(?<classes>[^"]*leading-tight[^"]*)"[^>]*>\s*(?<text>.*?)\s*</p>'
    $badgeMatch = Get-OptionalMatch -InputText $Html -Pattern '<span[^>]*class="(?<classes>[^"]*px-1\.5[^"]*)"[^>]*>\s*(?<text>.*?)\s*</span>'
    $iconMatch = Get-OptionalMatch -InputText $Html -Pattern '<div class="(?<classes>[^"]*shrink-0[^"]*)"[^>]*>\s*<span class="material-symbols-outlined[^"]*"[^>]*>(?<text>.*?)</span>'

    $stats = New-Object System.Collections.Generic.List[object]
    $statMatches = [regex]::Matches($Html, '<div class="(?<classes>[^"]*justify-between[^"]*)"[^>]*>\s*<span[^>]*>\s*(?<label>.*?)\s*</span>\s*<span[^>]*>\s*(?<value>.*?)\s*</span>\s*</div>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($statMatch in $statMatches) {
        $stats.Add([PSCustomObject]@{
            classes = [string]$statMatch.Groups["classes"].Value
            labelText = Clean-InnerText -Value ([string]$statMatch.Groups["label"].Value)
            valueText = Clean-InnerText -Value ([string]$statMatch.Groups["value"].Value)
            valueEmphasis = if ([string]$statMatch.Groups["value"].Value -match 'amber-') { "accent" } else { "default" }
        })
    }

    $modifierButtons = New-Object System.Collections.Generic.List[object]
    $modifierMatches = [regex]::Matches($Html, '<button class="(?<classes>[^"]*min-w-\[[^"]*)"[^>]*>(?<inner>.*?)</button>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    foreach ($modifierMatch in $modifierMatches) {
        $inner = [string]$modifierMatch.Groups["inner"].Value
        $titleCandidates = [regex]::Matches($inner, '<(?:div|span)[^>]*>\s*(?<text>.*?)\s*</(?:div|span)>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
        $iconInnerMatch = Get-OptionalMatch -InputText $inner -Pattern '<span class="material-symbols-outlined[^"]*"[^>]*>\s*(?<text>.*?)\s*</span>'
        if ($titleCandidates.Count -eq 0) {
            continue
        }

        $titleText = Clean-InnerText -Value ([string]$titleCandidates[0].Groups["text"].Value)
        $subtitleText = if ($titleCandidates.Count -ge 2) { Clean-InnerText -Value ([string]$titleCandidates[1].Groups["text"].Value) } else { "" }
        $modifierButtons.Add([PSCustomObject]@{
            classes = [string]$modifierMatch.Groups["classes"].Value
            titleText = $titleText
            subtitleText = $subtitleText
            iconText = if ($iconInnerMatch.Success) { Convert-MaterialSymbolToFallbackText -Text (Clean-InnerText -Value ([string]$iconInnerMatch.Groups["text"].Value)) } else { "" }
            active = ([string]$modifierMatch.Groups["classes"].Value -match 'blue-500')
        })
    }

    return [PSCustomObject]@{
        badgeText = if ($badgeMatch.Success) { Clean-InnerText -Value ([string]$badgeMatch.Groups["text"].Value) } else { "" }
        titleText = if ($titleMatch.Success) { Clean-InnerText -Value ([string]$titleMatch.Groups["text"].Value) } else { "" }
        descriptionText = if ($descriptionMatch.Success) { Clean-InnerText -Value ([string]$descriptionMatch.Groups["text"].Value) } else { "" }
        iconText = if ($iconMatch.Success) { Clean-InnerText -Value ([string]$iconMatch.Groups["text"].Value) } else { "" }
        stats = @($stats.ToArray())
        modifiers = @($modifierButtons.ToArray())
    }
}

function Get-WorkspacePreviewContext {
    param([Parameter(Mandatory = $true)][string]$Html)

    $titleMatch = Get-OptionalMatch -InputText $Html -Pattern '<span class="(?<classes>[^"]*uppercase[^"]*)"[^>]*>\s*(?<text>.*?)\s*</span>'
    $iconMatch = Get-OptionalMatch -InputText $Html -Pattern '<span class="material-symbols-outlined[^"]*text-4xl[^"]*"[^>]*>\s*(?<text>.*?)\s*</span>'
    $tagMatches = [regex]::Matches($Html, '<span class="(?<classes>[^"]*px-1\.5[^"]*)"[^>]*>\s*(?<text>.*?)\s*</span>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $tags = New-Object System.Collections.Generic.List[object]
    foreach ($tagMatch in $tagMatches) {
        $text = Clean-InnerText -Value ([string]$tagMatch.Groups["text"].Value)
        if ([string]::IsNullOrWhiteSpace($text)) {
            continue
        }

        $tags.Add([PSCustomObject]@{
            classes = [string]$tagMatch.Groups["classes"].Value
            text = $text
        })
    }

    return [PSCustomObject]@{
        titleText = if ($titleMatch.Success) { Clean-InnerText -Value ([string]$titleMatch.Groups["text"].Value) } else { "" }
        iconText = if ($iconMatch.Success) { Convert-MaterialSymbolToFallbackText -Text (Clean-InnerText -Value ([string]$iconMatch.Groups["text"].Value)) } else { "" }
        tags = @($tags.ToArray())
    }
}

function Get-WorkspaceContext {
    param([Parameter(Mandatory = $true)][string]$Html)

    if ($Html -notmatch '<main[^>]*>' -or $Html -notmatch '<header[^>]*>' -or $Html -notmatch '저장') {
        throw "Could not detect workspace screen structure from source HTML."
    }

    $headerMatch = Get-RequiredMatch `
        -InputText $Html `
        -Pattern '<header class="(?<classes>[^"]+)">(?<inner>.*?)</header>' `
        -ErrorMessage "Could not find workspace header."
    $mainMatch = Get-RequiredMatch `
        -InputText $Html `
        -Pattern '<main class="(?<classes>[^"]+)">(?<inner>.*?)</main>' `
        -ErrorMessage "Could not find workspace main body."
    $saveDockMatch = Get-RequiredMatch `
        -InputText $Html `
        -Pattern '<div class="(?<classes>[^"]*fixed[^"]*bottom-0[^"]*)">\s*<button class="(?<buttonClasses>[^"]+)"[^>]*>(?<buttonInner>.*?)</button>' `
        -ErrorMessage "Could not find persistent save dock."

    $titleMatch = Get-RequiredMatch `
        -InputText ([string]$headerMatch.Groups["inner"].Value) `
        -Pattern '<h1 class="(?<classes>[^"]+)">\s*(?<text>.*?)\s*</h1>' `
        -ErrorMessage "Could not find workspace header title."
    $subtitleMatch = Get-OptionalMatch `
        -InputText ([string]$headerMatch.Groups["inner"].Value) `
        -Pattern '<p class="(?<classes>[^"]+)">\s*(?<text>.*?)\s*</p>'

    $mainInner = [string]$mainMatch.Groups["inner"].Value
    $sections = [regex]::Matches($mainInner, '<section class="(?<classes>[^"]+)">(?<inner>.*?)</section>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($sections.Count -lt 3) {
        throw "Workspace screen requires at least three main sections."
    }

    $slotSection = $sections[0]
    $focusSection = $sections[1]
    $editorSection = $sections[2]
    $previewSection = if ($sections.Count -ge 4) { $sections[3] } else { $sections[$sections.Count - 1] }
    $slotInner = [string]$slotSection.Groups["inner"].Value
    $focusInner = [string]$focusSection.Groups["inner"].Value
    $editorInner = [string]$editorSection.Groups["inner"].Value
    $previewInner = [string]$previewSection.Groups["inner"].Value
    $summaryBarMatch = Get-OptionalMatch `
        -InputText $previewInner `
        -Pattern '<div class="(?<barClasses>[^"]*p-1[^"]*flex[^"]*gap-2[^"]*)">\s*<div class="(?<trackClasses>[^"]+)">\s*<div class="(?<fillClasses>[^"]+)"></div>\s*</div>\s*<span class="(?<textClasses>[^"]+)">\s*(?<text>.*?)\s*</span>'
    $summaryFillPercent = 0
    if ($summaryBarMatch.Success) {
        $fillWidthMatch = Get-OptionalMatch -InputText ([string]$summaryBarMatch.Groups["fillClasses"].Value) -Pattern 'w-\[(?<pct>\d+)%\]'
        if ($fillWidthMatch.Success) {
            $summaryFillPercent = [int]$fillWidthMatch.Groups["pct"].Value
        }
    }

    $settingsButtonMatch = Get-OptionalMatch `
        -InputText ([string]$headerMatch.Groups["inner"].Value) `
        -Pattern '<button class="(?<classes>[^"]+)"[^>]*>\s*<span class="material-symbols-outlined[^"]*">(?<icon>.*?)</span>'

    $saveText = Clean-InnerText -Value ([string]$saveDockMatch.Groups["buttonInner"].Value)
    $saveButtonText = if ([string]::IsNullOrWhiteSpace($saveText)) { "저장" } else { $saveText }

    return [PSCustomObject]@{
        headerClasses = [string]$headerMatch.Groups["classes"].Value
        headerInner = [string]$headerMatch.Groups["inner"].Value
        mainClasses = [string]$mainMatch.Groups["classes"].Value
        titleClasses = [string]$titleMatch.Groups["classes"].Value
        titleText = Clean-InnerText -Value ([string]$titleMatch.Groups["text"].Value)
        subtitleClasses = if ($subtitleMatch.Success) { [string]$subtitleMatch.Groups["classes"].Value } else { "" }
        subtitleText = if ($subtitleMatch.Success) { Clean-InnerText -Value ([string]$subtitleMatch.Groups["text"].Value) } else { "" }
        settingsButtonClasses = if ($settingsButtonMatch.Success) { [string]$settingsButtonMatch.Groups["classes"].Value } else { "" }
        settingsIconText = if ($settingsButtonMatch.Success) { Convert-MaterialSymbolToFallbackText -Text (Clean-InnerText -Value ([string]$settingsButtonMatch.Groups["icon"].Value)) } else { Convert-MaterialSymbolToFallbackText -Text "settings" }
        slotSectionClasses = [string]$slotSection.Groups["classes"].Value
        focusSectionClasses = [string]$focusSection.Groups["classes"].Value
        editorSectionClasses = [string]$editorSection.Groups["classes"].Value
        previewSectionClasses = [string]$previewSection.Groups["classes"].Value
        slotItems = @(Get-WorkspaceSlotItems -Html $slotInner)
        focusTabs = @(Get-WorkspaceFocusTabs -Html $focusInner)
        editor = Get-WorkspaceEditorContext -Html $editorInner
        preview = Get-WorkspacePreviewContext -Html $previewInner
        summaryBarClasses = if ($summaryBarMatch.Success) { [string]$summaryBarMatch.Groups["barClasses"].Value } else { "" }
        summaryTrackClasses = if ($summaryBarMatch.Success) { [string]$summaryBarMatch.Groups["trackClasses"].Value } else { "" }
        summaryFillClasses = if ($summaryBarMatch.Success) { [string]$summaryBarMatch.Groups["fillClasses"].Value } else { "" }
        summaryTextClasses = if ($summaryBarMatch.Success) { [string]$summaryBarMatch.Groups["textClasses"].Value } else { "" }
        summaryText = if ($summaryBarMatch.Success) { Clean-InnerText -Value ([string]$summaryBarMatch.Groups["text"].Value) } else { "" }
        summaryFillPercent = $summaryFillPercent
        saveDockClasses = [string]$saveDockMatch.Groups["classes"].Value
        saveButtonClasses = [string]$saveDockMatch.Groups["buttonClasses"].Value
        saveButtonText = $saveButtonText
    }
}

function New-OverlayDialogProfile {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [Parameter(Mandatory = $true)][string]$HtmlPath,
        [Parameter(Mandatory = $true)][string]$ImagePath,
        [Parameter(Mandatory = $true)][string]$PresentationOutputPath,
        [Parameter(Mandatory = $true)][string]$TargetAssetPath,
        [Parameter(Mandatory = $true)][object]$Context,
        [object]$SourceMetadata = $null
    )

    $sourceRef = if ($null -ne $SourceMetadata -and -not [string]::IsNullOrWhiteSpace([string]$SourceMetadata.sourceRef)) {
        [string]$SourceMetadata.sourceRef
    }
    else {
        [System.IO.Path]::GetFileNameWithoutExtension($ImagePath)
    }
    $setId = Get-SetIdFromPath -PathValue $HtmlPath -SourceRef $sourceRef
    $dialogWidthPx = Get-MaxWidthPx -Classes ([string]$Context.panelClasses)
    $headerLayout = if (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryText)) { "stacked" } else { "inline" }
    $footerLayout = if (([string]$Context.footerClasses) -match '\bflex-col\b') { "vertical" } else { "horizontal" }
    $headerHeightPx = if ($headerLayout -eq "stacked") { 64 } else { 56 }
    $bodyHeightPx = if ($footerLayout -eq "vertical") { 82 } else { 112 }
    $footerHeightPx = if ($footerLayout -eq "vertical") { 108 } else { 52 }
    $headerPaddingX = Get-ClassValuePx -Classes ([string]$Context.headerClasses) -Prefixes @("px") -DefaultValue 16
    $headerPaddingY = Get-ClassValuePx -Classes ([string]$Context.headerClasses) -Prefixes @("py") -DefaultValue 16
    $bodyPadding = Get-ClassValuePx -Classes ([string]$Context.bodyClasses) -Prefixes @("p") -DefaultValue 20
    $footerPaddingX = Get-ClassValuePx -Classes ([string]$Context.footerClasses) -Prefixes @("px") -DefaultValue 20
    $footerPaddingTop = Get-ClassValuePx -Classes ([string]$Context.footerClasses) -Prefixes @("pt", "py") -DefaultValue 0
    $footerPaddingBottom = Get-ClassValuePx -Classes ([string]$Context.footerClasses) -Prefixes @("pb", "py") -DefaultValue 20
    $footerGap = Get-ClassValuePx -Classes ([string]$Context.footerClasses) -Prefixes @("gap") -DefaultValue 12
    $titleGap = Get-ClassValuePx -Classes ([string]$Context.headerClasses) -Prefixes @("gap") -DefaultValue 12
    $titleFontPx = Get-TextSizePx -Classes ([string]$Context.titleClasses)
    $summaryFontPx = if (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryClasses)) { Get-TextSizePx -Classes ([string]$Context.summaryClasses) } else { 0 }
    $bodyFontPx = Get-TextSizePx -Classes ([string]$Context.bodyTextClasses)
    $buttonFontPx = Get-TextSizePx -Classes ([string]$Context.primaryButtonClasses)
    $buttonHeightPx = if (([string]$Context.primaryButtonClasses) -match '\bpy-2\.5\b') { 40 } else { 32 }
    $iconSizePx = if ($Context.iconKind -eq "badge") {
        Get-ClassValuePx -Classes ([string]$Context.iconClasses) -Prefixes @("w", "h") -DefaultValue 32
    }
    else {
        Get-TextSizePx -Classes ([string]$Context.iconClasses)
    }

    $titleStackWidthPx = if ($headerLayout -eq "stacked") {
        $dialogWidthPx - ($headerPaddingX * 2) - $iconSizePx - $titleGap
    }
    else {
        0
    }
    $titleWidthPx = if ($headerLayout -eq "inline") {
        $dialogWidthPx - ($headerPaddingX * 2) - $iconSizePx - $titleGap
    }
    else {
        0
    }

    $secondarySpec = Get-ActionSpec -Text ([string]$Context.secondaryButtonText) -Position "secondary"
    $primarySpec = Get-ActionSpec -Text ([string]$Context.primaryButtonText) -Position "primary"
    $semanticSpec = Get-OverlaySemanticSpec -PrimaryText ([string]$Context.primaryButtonText) -HasSummary (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryText))

    $headerHasImage = -not [string]::IsNullOrWhiteSpace((Get-ColorTokenFromClasses -Classes ([string]$Context.headerClasses) -Prefix "bg"))
    $bodyHasImage = -not [string]::IsNullOrWhiteSpace((Get-ColorTokenFromClasses -Classes ([string]$Context.bodyClasses) -Prefix "bg"))
    $footerHasImage = -not [string]::IsNullOrWhiteSpace((Get-ColorTokenFromClasses -Classes ([string]$Context.footerClasses) -Prefix "bg"))

    $headerRequiredComponents = @("HorizontalLayoutGroup")
    if ($headerHasImage) { $headerRequiredComponents = @("Image") + $headerRequiredComponents }

    $bodyRequiredComponents = @("LayoutElement")
    if ($bodyHasImage) { $bodyRequiredComponents = @("Image") + $bodyRequiredComponents }

    $footerRequiredComponents = @($(if ($footerLayout -eq "vertical") { "VerticalLayoutGroup" } else { "HorizontalLayoutGroup" }), "LayoutElement")
    if ($footerHasImage) { $footerRequiredComponents = @("Image") + $footerRequiredComponents }

    $secondaryRequiredComponents = @("Button", "Image")
    if ($footerLayout -eq "horizontal") { $secondaryRequiredComponents += "LayoutElement" }
    $primaryRequiredComponents = @("Button", "Image")
    if ($footerLayout -eq "horizontal") { $primaryRequiredComponents += "LayoutElement" }

    $headerPath = "DialogPanel/HeaderRow"
    $bodyPath = "DialogPanel/BodyBlock"
    $footerPath = "DialogPanel/FooterRow"
    $secondaryPath = "$footerPath/$($secondarySpec.hostName)"
    $primaryPath = "$footerPath/$($primarySpec.hostName)"
    $secondaryLabelPath = "$secondaryPath/Label"
    $primaryLabelPath = "$primaryPath/Label"
    $titlePath = if ($headerLayout -eq "stacked") { "$headerPath/TitleStack/TitleText" } else { "$headerPath/TitleText" }

    $profile = [ordered]@{
        surfaceId = $SurfaceId
        targetKind = "overlay-root"
        reviewRoute = (New-ReviewRouteConfig -RouteId "surface-review" -MenuPath "Tools/Scene/Prepare Stitch Runtime Review/Surface")
        defaults = [ordered]@{
            htmlPath = $HtmlPath
            imagePath = $ImagePath
            outputPath = $PresentationOutputPath
        }
        viewport = [ordered]@{
            label = "390x844 mobile-first"
        }
        patterns = [ordered]@{
            titlePattern = "<h2[^>]*class=`"(?<classes>[^`"]+)`"[^>]*>\s*(?<text>.*?)\s*</h2>"
            summaryPattern = "<h2[^>]*>.*?</h2>\s*<p[^>]*class=`"(?<classes>[^`"]+)`"[^>]*>\s*(?<text>.*?)\s*</p>"
            bodyPattern = "(?:<!-- Modal Body(?: \(Form\))? -->|<!-- Panel Content -->).*?<p[^>]*class=`"(?<classes>[^`"]+)`"[^>]*>\s*(?<text>.*?)\s*</p>"
            buttonPattern = "<button[^>]*class=`"(?<classes>[^`"]+)`"[^>]*>\s*(?<content>.*?)\s*</button>"
        }
        colors = [ordered]@{
            scrim = Get-ColorTokenFromClasses -Classes ([string]$Context.scrimClasses) -Prefix "bg"
            panel = Get-ColorTokenFromClasses -Classes ([string]$Context.panelClasses) -Prefix "bg"
            header = Get-ColorTokenFromClasses -Classes ([string]$Context.headerClasses) -Prefix "bg"
            icon = $(if ($Context.iconKind -eq "badge") {
                Get-ColorTokenFromClasses -Classes ([string]$Context.iconClasses) -Prefix "bg"
            } else {
                Get-ColorTokenFromClasses -Classes ([string]$Context.iconClasses) -Prefix "text"
            })
            title = Get-ColorTokenFromClasses -Classes ([string]$Context.titleClasses) -Prefix "text"
            summary = $(if (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryClasses)) { Get-ColorTokenFromClasses -Classes ([string]$Context.summaryClasses) -Prefix "text" } else { "" })
            bodyBackground = Get-ColorTokenFromClasses -Classes ([string]$Context.bodyClasses) -Prefix "bg"
            bodyText = Get-ColorTokenFromClasses -Classes ([string]$Context.bodyTextClasses) -Prefix "text"
            footerBackground = Get-ColorTokenFromClasses -Classes ([string]$Context.footerClasses) -Prefix "bg"
            secondaryButton = Get-ColorTokenFromClasses -Classes ([string]$Context.secondaryButtonClasses) -Prefix "bg"
            secondaryText = Get-ColorTokenFromClasses -Classes ([string]$Context.secondaryButtonClasses) -Prefix "text"
            primaryButton = Get-ColorTokenFromClasses -Classes ([string]$Context.primaryButtonClasses) -Prefix "bg"
            primaryText = Get-ColorTokenFromClasses -Classes ([string]$Context.primaryButtonClasses) -Prefix "text"
        }
        layout = [ordered]@{
            dialogWidthPx = $dialogWidthPx
            headerHeightPx = $headerHeightPx
            bodyHeightPx = $bodyHeightPx
            footerHeightPx = $footerHeightPx
            headerPaddingX = $headerPaddingX
            headerPaddingY = $headerPaddingY
            bodyPadding = $bodyPadding
            footerPaddingX = $footerPaddingX
            footerPaddingTop = $footerPaddingTop
            footerPaddingBottom = $footerPaddingBottom
            footerGap = $footerGap
            titleGap = $titleGap
            titleFontPx = $titleFontPx
            bodyFontPx = $bodyFontPx
            buttonFontPx = $buttonFontPx
            buttonHeightPx = $buttonHeightPx
            headerLayout = $headerLayout
            footerLayout = $footerLayout
        }
        header = [ordered]@{
            path = $headerPath
            iconKind = $Context.iconKind
            iconPath = $(if ($Context.iconKind -eq "badge") { "$headerPath/WarningBadge" } else { "$headerPath/WarningIcon" })
            iconSizePx = $iconSizePx
            titlePath = $titlePath
        }
        body = [ordered]@{
            path = $bodyPath
            textPath = "$bodyPath/BodyText"
        }
        footer = [ordered]@{
            path = $footerPath
            secondaryPath = $secondaryPath
            secondaryLabelPath = $secondaryLabelPath
            primaryPath = $primaryPath
            primaryLabelPath = $primaryLabelPath
        }
        compiler = [ordered]@{
            manifest = [ordered]@{
                setId = $setId
                surfaceRole = "overlay"
                status = "accepted"
                source = [ordered]@{
                    tool = "stitch"
                    sourceRef = $sourceRef
                    projectId = if ($null -ne $SourceMetadata) { [string]$SourceMetadata.projectId } else { "" }
                    screenId = if ($null -ne $SourceMetadata -and -not [string]::IsNullOrWhiteSpace([string]$SourceMetadata.screenId)) { [string]$SourceMetadata.screenId } else { $sourceRef }
                    url = if ($null -ne $SourceMetadata -and -not [string]::IsNullOrWhiteSpace([string]$SourceMetadata.url)) { [string]$SourceMetadata.url } else { $ImagePath }
                }
                ctaPriority = @(
                    [ordered]@{
                        id = $primarySpec.id
                        priority = "primary"
                        outcome = $primarySpec.outcome
                    },
                    [ordered]@{
                        id = $secondarySpec.id
                        priority = "secondary"
                        outcome = $secondarySpec.outcome
                    }
                )
                states = [ordered]@{
                    default = $true
                    empty = $false
                    loading = $false
                    error = ($primarySpec.id -eq "retry-cta")
                    selected = $false
                    disabled = ($primarySpec.id -eq "delete-cta")
                }
                validation = [ordered]@{
                    firstReadOrder = @(
                        $semanticSpec.headerBlockId,
                        $semanticSpec.bodyBlockId,
                        "footer-actions"
                    )
                    requiredChecks = @($semanticSpec.requiredChecks)
                }
                notes = @(
                    "Auto-generated overlay dialog manifest from source HTML.",
                    "Source-driven compiler data should be preferred over hand-authored screen-specific profile edits."
                )
                blocks = @(
                    [ordered]@{
                        blockId = $semanticSpec.headerBlockId
                        role = "shared-chrome"
                        sourceName = "dialog-header"
                        children = @()
                        componentComposition = @(
                            [ordered]@{
                                componentId = "status-text"
                                slot = "title"
                                variant = $semanticSpec.titleVariant
                            }
                        ) + $(if (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryText)) {
                            @([ordered]@{
                                componentId = "status-text"
                                slot = "summary"
                                variant = $semanticSpec.summaryVariant
                            })
                        } else { @() }) + @(
                            [ordered]@{
                                componentId = $(if ($Context.iconKind -eq "badge") { "icon-button" } else { "status-text" })
                                slot = $(if ($Context.iconKind -eq "badge") { "leading" } else { "leading-icon" })
                                variant = $(if ($primarySpec.id -eq "delete-cta") { "warning-badge" } else { "warning-signal" })
                            }
                        )
                    },
                    [ordered]@{
                        blockId = $semanticSpec.bodyBlockId
                        role = "content"
                        sourceName = "dialog-body"
                        children = @()
                        componentComposition = @(
                            [ordered]@{
                                componentId = "section-card"
                                slot = "shell"
                                variant = $semanticSpec.bodyVariant
                            }
                        )
                    },
                    [ordered]@{
                        blockId = "footer-actions"
                        role = "shared-chrome"
                        sourceName = "dialog-footer"
                        children = @($secondarySpec.id, $primarySpec.id)
                        componentComposition = @(
                            [ordered]@{
                                componentId = "section-card"
                                slot = "shell"
                                variant = $semanticSpec.footerVariant
                            }
                        )
                    },
                    [ordered]@{
                        blockId = $secondarySpec.id
                        role = "cta"
                        sourceName = "secondary-action"
                        children = @()
                        componentComposition = @(
                            [ordered]@{
                                componentId = "secondary-button"
                                slot = "self"
                                variant = $secondarySpec.variant
                            }
                        )
                    },
                    [ordered]@{
                        blockId = $primarySpec.id
                        role = "cta"
                        sourceName = "primary-action"
                        children = @()
                        componentComposition = @(
                            [ordered]@{
                                componentId = "primary-button"
                                slot = "self"
                                variant = $primarySpec.variant
                            }
                        )
                    }
                )
            }
            map = [ordered]@{
                target = [ordered]@{
                    kind = "uitoolkit-candidate"
                    assetPath = $TargetAssetPath
                }
                translationStrategy = "uitoolkit-candidate-v1"
                strategyMode = "candidate-authoring"
                artifactPaths = [ordered]@{
                    pipelineResult = "artifacts/unity/$sourceRef-pipeline-result.json"
                }
                notes = @(
                    "Auto-generated overlay dialog map from source HTML.",
                    "Host paths follow the current canonical overlay prefab structure."
                )
                blocks = @(
                    [ordered]@{
                        blockId = $semanticSpec.headerBlockId
                        hostPath = $headerPath
                        requiredComponents = @($headerRequiredComponents)
                    },
                    [ordered]@{
                        blockId = $semanticSpec.bodyBlockId
                        hostPath = $bodyPath
                        requiredComponents = @($bodyRequiredComponents)
                    },
                    [ordered]@{
                        blockId = "footer-actions"
                        hostPath = $footerPath
                        requiredComponents = @($footerRequiredComponents)
                    },
                    [ordered]@{
                        blockId = $secondarySpec.id
                        hostPath = $secondaryPath
                        requiredComponents = @($secondaryRequiredComponents)
                    },
                    [ordered]@{
                        blockId = $primarySpec.id
                        hostPath = $primaryPath
                        requiredComponents = @($primaryRequiredComponents)
                    }
                )
            }
        }
        unresolvedDerivedFields = @(
            "warning-icon-glyph-font"
        )
    }

    if ($headerLayout -eq "stacked") {
        $profile.layout.titleStackWidthPx = $titleStackWidthPx
        $profile.layout.summaryFontPx = $summaryFontPx
        $profile.header.titleStackPath = "$headerPath/TitleStack"
        $profile.header.summaryPath = "$headerPath/TitleStack/SummaryText"
    }
    else {
        $profile.layout.titleWidthPx = $titleWidthPx
        $profile.header.iconText = $Context.iconText
    }

    return [PSCustomObject]$profile
}

function Get-WorkspaceTargetAssetPath {
    param([Parameter(Mandatory = $true)][string]$SurfaceId)

    $featureName = Get-FeatureName -SurfaceId $SurfaceId
    return "Assets/UI/UIToolkit/$featureName/$featureName`Workspace.uxml"
}

function New-ReviewRouteConfig {
    param(
        [Parameter(Mandatory = $true)][string]$RouteId,
        [Parameter(Mandatory = $true)][string]$MenuPath
    )

    return [ordered]@{
        routeId = $RouteId
        kind = "temp-scene-sceneview"
        menuPath = $MenuPath
    }
}

function New-WorkspaceProfile {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [Parameter(Mandatory = $true)][string]$HtmlPath,
        [Parameter(Mandatory = $true)][string]$ImagePath,
        [Parameter(Mandatory = $true)][string]$PresentationOutputPath,
        [Parameter(Mandatory = $true)][string]$TargetAssetPath,
        [Parameter(Mandatory = $true)][object]$Context,
        [object]$SourceMetadata = $null
    )

    $sourceRef = if ($null -ne $SourceMetadata -and -not [string]::IsNullOrWhiteSpace([string]$SourceMetadata.sourceRef)) {
        [string]$SourceMetadata.sourceRef
    }
    else {
        [System.IO.Path]::GetFileNameWithoutExtension($ImagePath)
    }
    $setId = Get-SetIdFromPath -PathValue $HtmlPath -SourceRef $sourceRef

    $headerHeightPx = Get-ClassValuePx -Classes ([string]$Context.headerClasses) -Prefixes @("h") -DefaultValue 56
    $headerPaddingX = Get-ClassValuePx -Classes ([string]$Context.headerClasses) -Prefixes @("px") -DefaultValue 16
    $mainPaddingX = Get-ClassValuePx -Classes ([string]$Context.mainClasses) -Prefixes @("px") -DefaultValue 12
    $mainGap = Get-ClassValuePx -Classes ([string]$Context.mainClasses) -Prefixes @("space-y") -DefaultValue 16
    $slotGap = Get-ClassValuePx -Classes ([string]$Context.slotSectionClasses) -Prefixes @("gap") -DefaultValue 8
    $focusGap = Get-ClassValuePx -Classes ([string]$Context.focusSectionClasses) -Prefixes @("gap") -DefaultValue 4
    $editorPadding = Get-ClassValuePx -Classes ([string]$Context.editorSectionClasses) -Prefixes @("p") -DefaultValue 12
    $previewHeight = Get-ClassValuePx -Classes ([string]$Context.previewSectionClasses) -Prefixes @("h") -DefaultValue 192
    $summaryTrackHeight = if (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryTrackClasses)) { Get-ClassValuePx -Classes ([string]$Context.summaryTrackClasses) -Prefixes @("h") -DefaultValue 6 } else { 6 }
    $summaryTextWidth = if (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryTextClasses)) { Get-ClassValuePx -Classes ([string]$Context.summaryTextClasses) -Prefixes @("w") -DefaultValue 48 } else { 48 }
    $summaryTextFontPx = if (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryTextClasses)) { Get-TextSizePx -Classes ([string]$Context.summaryTextClasses) } else { 8 }
    $summaryBarHeight = if (-not [string]::IsNullOrWhiteSpace([string]$Context.summaryBarClasses)) { [Math]::Max(20, $summaryTrackHeight + 10) } else { 20 }
    $saveButtonHeight = if (([string]$Context.saveButtonClasses) -match '\bpy-3\.5\b') { 52 } else { 44 }
    $saveDockPaddingX = Get-ClassValuePx -Classes ([string]$Context.saveDockClasses) -Prefixes @("px") -DefaultValue 16
    $saveDockPaddingTop = Get-ClassValuePx -Classes ([string]$Context.saveDockClasses) -Prefixes @("pt") -DefaultValue 8

    $profile = [ordered]@{
        surfaceId = $SurfaceId
        targetKind = "workspace-root"
        reviewRoute = (New-ReviewRouteConfig -RouteId "surface-review" -MenuPath "Tools/Scene/Prepare Stitch Runtime Review/Surface")
        defaults = [ordered]@{
            htmlPath = $HtmlPath
            imagePath = $ImagePath
            outputPath = $PresentationOutputPath
        }
        viewport = [ordered]@{
            label = "390x844 mobile-first"
        }
        colors = [ordered]@{
            header = Get-ColorTokenFromClasses -Classes ([string]$Context.headerClasses) -Prefix "bg"
            title = Get-ColorTokenFromClasses -Classes ([string]$Context.titleClasses) -Prefix "text"
            subtitle = if (-not [string]::IsNullOrWhiteSpace([string]$Context.subtitleClasses)) { Get-ColorTokenFromClasses -Classes ([string]$Context.subtitleClasses) -Prefix "text" } else { "zinc-500" }
            slot = Get-ColorTokenFromClasses -Classes ([string]$Context.slotSectionClasses) -Prefix "bg"
            editor = Get-ColorTokenFromClasses -Classes ([string]$Context.editorSectionClasses) -Prefix "bg"
            preview = Get-ColorTokenFromClasses -Classes ([string]$Context.previewSectionClasses) -Prefix "bg"
            summaryBar = Get-ColorTokenFromClasses -Classes ([string]$Context.summaryBarClasses) -Prefix "bg"
            summaryTrack = Get-ColorTokenFromClasses -Classes ([string]$Context.summaryTrackClasses) -Prefix "bg"
            summaryFill = Get-ColorTokenFromClasses -Classes ([string]$Context.summaryFillClasses) -Prefix "bg"
            summaryText = Get-ColorTokenFromClasses -Classes ([string]$Context.summaryTextClasses) -Prefix "text"
            saveDock = Get-ColorTokenFromClasses -Classes ([string]$Context.saveDockClasses) -Prefix "bg"
            primaryButton = Get-ColorTokenFromClasses -Classes ([string]$Context.saveButtonClasses) -Prefix "bg"
            primaryText = Get-ColorTokenFromClasses -Classes ([string]$Context.saveButtonClasses) -Prefix "text"
        }
        workspace = [ordered]@{
            headerHeightPx = $headerHeightPx
            headerPaddingX = $headerPaddingX
            mainPaddingX = $mainPaddingX
            mainGap = $mainGap
            slotGap = $slotGap
            focusGap = $focusGap
            editorPadding = $editorPadding
            previewHeightPx = $previewHeight
            summaryBarHeightPx = $summaryBarHeight
            summaryTrackHeightPx = $summaryTrackHeight
            summaryFillPercent = [int]$Context.summaryFillPercent
            summaryText = [string]$Context.summaryText
            summaryTextFontPx = $summaryTextFontPx
            summaryTextWidthPx = $summaryTextWidth
            saveButtonHeightPx = $saveButtonHeight
            saveDockPaddingX = $saveDockPaddingX
            saveDockPaddingTop = $saveDockPaddingTop
            titleText = [string]$Context.titleText
            subtitleText = [string]$Context.subtitleText
            saveButtonText = [string]$Context.saveButtonText
            settingsIconText = [string]$Context.settingsIconText
            slotItems = @($Context.slotItems)
            focusTabs = @($Context.focusTabs)
            editor = $Context.editor
            preview = $Context.preview
        }
        compiler = [ordered]@{
            manifest = [ordered]@{
                setId = $setId
                surfaceRole = "root"
                status = "accepted"
                source = [ordered]@{
                    tool = "stitch"
                    sourceRef = $sourceRef
                    projectId = if ($null -ne $SourceMetadata) { [string]$SourceMetadata.projectId } else { "" }
                    screenId = if ($null -ne $SourceMetadata -and -not [string]::IsNullOrWhiteSpace([string]$SourceMetadata.screenId)) { [string]$SourceMetadata.screenId } else { $sourceRef }
                    url = if ($null -ne $SourceMetadata -and -not [string]::IsNullOrWhiteSpace([string]$SourceMetadata.url)) { [string]$SourceMetadata.url } else { $ImagePath }
                }
                ctaPriority = @(
                    [ordered]@{
                        id = "save-roster"
                        priority = "primary"
                        outcome = "save-roster"
                    },
                    [ordered]@{
                        id = "open-settings"
                        priority = "secondary"
                        outcome = "open-settings"
                    }
                )
                states = [ordered]@{
                    default = $true
                    empty = $true
                    loading = $false
                    error = $false
                    selected = $true
                    disabled = $true
                }
                validation = [ordered]@{
                    firstReadOrder = @(
                        "slot-selector",
                        "focus-bar",
                        "editor-panel",
                        "preview-card",
                        "summary-card",
                        "save-dock"
                    )
                    requiredChecks = @(
                        "slot-strip-horizontal",
                        "focus-bar-compact-row",
                        "editor-dominant-first-screen",
                        "evaluative-summary-retained",
                        "persistent-primary-save-dock"
                    )
                }
                blocks = @(
                    [ordered]@{ blockId = "header-chrome"; role = "shared-chrome"; sourceName = "top-app-bar"; children = @("aux-action") },
                    [ordered]@{ blockId = "slot-selector"; role = "section"; sourceName = "slot-selector"; children = @() },
                    [ordered]@{ blockId = "focus-bar"; role = "section"; sourceName = "focus-bar"; children = @() },
                    [ordered]@{ blockId = "editor-panel"; role = "content"; sourceName = "editor-panel"; children = @() },
                    [ordered]@{ blockId = "preview-card"; role = "content"; sourceName = "preview-card"; children = @("summary-card") },
                    [ordered]@{ blockId = "summary-card"; role = "status"; sourceName = "preview-summary.summary"; children = @() },
                    [ordered]@{ blockId = "save-dock"; role = "cta"; sourceName = "persistent-save-dock"; children = @("primary-cta") },
                    [ordered]@{ blockId = "primary-cta"; role = "cta"; sourceName = "save-roster"; children = @() },
                    [ordered]@{ blockId = "aux-action"; role = "cta"; sourceName = "open-settings"; children = @() }
                )
            }
            map = [ordered]@{
                target = [ordered]@{
                    kind = "uitoolkit-candidate"
                    assetPath = $TargetAssetPath
                }
                translationStrategy = "uitoolkit-candidate-v1"
                strategyMode = "candidate-authoring"
                artifactPaths = [ordered]@{
                    pipelineResult = "artifacts/unity/$sourceRef-pipeline-result.json"
                }
                blocks = @(
                    [ordered]@{ blockId = "header-chrome"; hostPath = "HeaderChrome"; requiredComponents = @("Image", "HorizontalLayoutGroup", "LayoutElement") },
                    [ordered]@{ blockId = "slot-selector"; hostPath = "MainScroll/Content/SlotSelector"; requiredComponents = @("Image", "HorizontalLayoutGroup", "LayoutElement") },
                    [ordered]@{ blockId = "focus-bar"; hostPath = "MainScroll/Content/FocusBar"; requiredComponents = @("HorizontalLayoutGroup", "LayoutElement") },
                    [ordered]@{ blockId = "editor-panel"; hostPath = "MainScroll/Content/EditorPanel"; requiredComponents = @("Image", "VerticalLayoutGroup", "LayoutElement") },
                    [ordered]@{ blockId = "preview-card"; hostPath = "MainScroll/Content/PreviewCard"; requiredComponents = @("Image", "LayoutElement") },
                    [ordered]@{ blockId = "summary-card"; hostPath = "MainScroll/Content/PreviewCard/SummaryBar"; requiredComponents = @("Image", "HorizontalLayoutGroup", "LayoutElement") },
                    [ordered]@{ blockId = "save-dock"; hostPath = "SaveDock"; requiredComponents = @("Image", "HorizontalLayoutGroup", "LayoutElement") },
                    [ordered]@{ blockId = "primary-cta"; hostPath = "SaveDock/PrimaryButton"; requiredComponents = @("Button", "Image", "LayoutElement") },
                    [ordered]@{ blockId = "aux-action"; hostPath = "HeaderChrome/SettingsButton"; requiredComponents = @("Button", "Image", "LayoutElement") }
                )
            }
        }
        unresolvedDerivedFields = @()
    }

    return [PSCustomObject]$profile
}

$sourceMetadata = Get-ActiveSourceFreezePaths -SurfaceId $SurfaceId
$resolvedHtmlPath = if ([string]::IsNullOrWhiteSpace($HtmlPath)) { Get-SurfaceSourcePath -SurfaceId $SurfaceId -Extension "html" } else { Convert-ToRepoRelativePath -AbsolutePath (Resolve-RepoPath -PathValue $HtmlPath) }
$resolvedImagePath = if ([string]::IsNullOrWhiteSpace($ImagePath)) { Get-SurfaceSourcePath -SurfaceId $SurfaceId -Extension "png" } else { Convert-ToRepoRelativePath -AbsolutePath (Resolve-RepoPath -PathValue $ImagePath) }
$resolvedPresentationOutputPath = if ([string]::IsNullOrWhiteSpace($PresentationOutputPath)) {
    "in-memory://compiled/$SurfaceId/presentation-contract"
}
elseif ($PresentationOutputPath.StartsWith("in-memory://", [System.StringComparison]::OrdinalIgnoreCase)) {
    $PresentationOutputPath
}
else {
    Convert-ToRepoRelativePath -AbsolutePath (Resolve-RepoPath -PathValue $PresentationOutputPath)
}
$resolvedTargetAssetPath = if ([string]::IsNullOrWhiteSpace($TargetAssetPath)) { "" } else { $TargetAssetPath }

$html = Read-AllText -PathValue $resolvedHtmlPath
$profile = $null
$reason = ""

try {
    $context = Get-DialogContext -Html $html
    if ([string]::IsNullOrWhiteSpace($resolvedTargetAssetPath)) {
        $resolvedTargetAssetPath = Get-DefaultTargetAssetPath -SurfaceId $SurfaceId
    }
    $profile = New-OverlayDialogProfile `
        -SurfaceId $SurfaceId `
        -HtmlPath $resolvedHtmlPath `
        -ImagePath $resolvedImagePath `
        -PresentationOutputPath $resolvedPresentationOutputPath `
        -TargetAssetPath $resolvedTargetAssetPath `
        -Context $context `
        -SourceMetadata $sourceMetadata
}
catch {
    $reason = $_.Exception.Message
}

if ($null -eq $profile) {
    try {
        $workspaceContext = Get-WorkspaceContext -Html $html
        if ([string]::IsNullOrWhiteSpace($resolvedTargetAssetPath)) {
            $resolvedTargetAssetPath = Get-WorkspaceTargetAssetPath -SurfaceId $SurfaceId
        }
        $profile = New-WorkspaceProfile `
            -SurfaceId $SurfaceId `
            -HtmlPath $resolvedHtmlPath `
            -ImagePath $resolvedImagePath `
            -PresentationOutputPath $resolvedPresentationOutputPath `
            -TargetAssetPath $resolvedTargetAssetPath `
            -Context $workspaceContext `
            -SourceMetadata $sourceMetadata
    }
    catch {
        if (-not [string]::IsNullOrWhiteSpace($_.Exception.Message)) {
            $reason = $_.Exception.Message
        }
    }
}

if ($null -eq $profile) {
    if ($CanGenerateOnly) {
        [PSCustomObject]@{
            success = $true
            supported = $false
            surfaceId = $SurfaceId
            htmlPath = $resolvedHtmlPath
            imagePath = $resolvedImagePath
            presentationOutputPath = $resolvedPresentationOutputPath
            targetAssetPath = $resolvedTargetAssetPath
            reason = $reason
            checkedAt = (Get-Date).ToString("o")
        } | ConvertTo-Json -Depth 20
        exit 0
    }

    throw $reason
}

if ($CanGenerateOnly) {
    [PSCustomObject]@{
        success = $true
        supported = $true
        surfaceId = $SurfaceId
        htmlPath = $resolvedHtmlPath
        imagePath = $resolvedImagePath
        presentationOutputPath = $resolvedPresentationOutputPath
        targetAssetPath = $resolvedTargetAssetPath
        checkedAt = (Get-Date).ToString("o")
    } | ConvertTo-Json -Depth 20
    exit 0
}

[PSCustomObject]@{
    success = $true
    supported = $true
    surfaceId = $SurfaceId
    targetKind = if ($null -ne $profile.PSObject.Properties["targetKind"]) { [string]$profile.targetKind } else { "" }
    htmlPath = $resolvedHtmlPath
    imagePath = $resolvedImagePath
    presentationOutputPath = $resolvedPresentationOutputPath
    targetAssetPath = $resolvedTargetAssetPath
    profile = $profile
    generatedAt = (Get-Date).ToString("o")
} | ConvertTo-Json -Depth 20
