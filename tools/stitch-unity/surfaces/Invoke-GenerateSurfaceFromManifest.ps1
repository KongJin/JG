param(
    [Parameter(Mandatory = $true)][string]$ScreenManifestPath,
    [string]$UnityBridgeUrl = "",
    [string]$ArtifactPath = "artifacts/unity/stitch-surface-translation-result.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "..\..\unity-mcp\McpHelpers.ps1")
. (Join-Path $PSScriptRoot "..\..\unity-mcp\McpPrefabPackHelpers.ps1")

function Get-RequiredProperty {
    param(
        [Parameter(Mandatory = $true)][object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $InputObject -or $null -eq $InputObject.PSObject.Properties[$Name]) {
        throw "Required property '$Name' is missing."
    }

    return $InputObject.PSObject.Properties[$Name].Value
}

function Get-ManifestCtaLabel {
    param(
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][string]$CtaId
    )

    $cta = @($Manifest.ctaPriority | Where-Object { $_.id -eq $CtaId }) | Select-Object -First 1
    if ($null -eq $cta) {
        return ""
    }

    if ($null -eq $cta.PSObject.Properties["label"]) {
        return ""
    }

    return [string]$cta.label
}

function Get-OptionalProperty {
    param(
        [object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name,
        [object]$Default = $null
    )

    if ($null -eq $InputObject) {
        return $Default
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $Default
    }

    return $property.Value
}

function Get-ManifestSemanticBlocks {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $blocks = @(Get-OptionalProperty -InputObject $Manifest -Name "blocks" -Default @())
    if ($blocks.Count -eq 0) {
        throw "screen manifest must declare blocks[]."
    }

    return $blocks
}

function Get-ManifestSemanticBlockIds {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    return @(Get-ManifestSemanticBlocks -Manifest $Manifest | ForEach-Object { [string]$_.blockId })
}

function Get-ManifestSemanticBlockLookup {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $lookup = @{}
    foreach ($block in @(Get-ManifestSemanticBlocks -Manifest $Manifest)) {
        $lookup[[string]$block.blockId] = $block
    }

    return $lookup
}

function Get-SurfaceBlockVocabularyAliases {
    return [ordered]@{
        "header-chrome" = @("header-chrome", "garage-header-row")
        "slot-selector" = @("slot-selector")
        "focus-bar" = @("focus-bar")
        "editor-panel" = @("editor-panel", "focused-editor")
        "preview-card" = @("preview-card")
        "summary-card" = @("summary-card", "result-summary")
        "save-dock" = @("save-dock")
        "primary-cta" = @("primary-cta")
        "aux-action" = @("aux-action", "aux-settings")
    }
}

function Resolve-ManifestBlockIdByVocabularyKey {
    param(
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][string]$VocabularyKey
    )

    $semanticBlockIds = @(Get-ManifestSemanticBlockIds -Manifest $Manifest)
    $aliases = Get-SurfaceBlockVocabularyAliases
    if (-not $aliases.Contains($VocabularyKey)) {
        return ""
    }

    foreach ($candidate in @($aliases[$VocabularyKey])) {
        if ($semanticBlockIds -contains $candidate) {
            return [string]$candidate
        }
    }

    return ""
}

function Get-ManifestCanonicalBlockIds {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $canonical = New-Object System.Collections.Generic.List[string]
    $aliases = Get-SurfaceBlockVocabularyAliases
    foreach ($entry in $aliases.GetEnumerator()) {
        if (-not [string]::IsNullOrWhiteSpace((Resolve-ManifestBlockIdByVocabularyKey -Manifest $Manifest -VocabularyKey $entry.Key))) {
            $canonical.Add([string]$entry.Key)
        }
    }

    return @($canonical)
}

function Test-ManifestHasVocabularyKeys {
    param(
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][string[]]$VocabularyKeys
    )

    foreach ($vocabularyKey in $VocabularyKeys) {
        if ([string]::IsNullOrWhiteSpace((Resolve-ManifestBlockIdByVocabularyKey -Manifest $Manifest -VocabularyKey $vocabularyKey))) {
            return $false
        }
    }

    return $true
}

function Convert-ScenePathToRelativeChildPath {
    param(
        [Parameter(Mandatory = $true)][string]$SceneRoot,
        [Parameter(Mandatory = $true)][string]$ScenePath
    )

    if ([string]::IsNullOrWhiteSpace($ScenePath) -or $ScenePath -eq $SceneRoot) {
        return ""
    }

    if ($ScenePath.StartsWith($SceneRoot + "/", [System.StringComparison]::OrdinalIgnoreCase)) {
        return $ScenePath.Substring($SceneRoot.Length + 1)
    }

    return $ScenePath.TrimStart('/')
}

function Get-GarageSemanticBlockChildPathMap {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $targets = Get-RequiredProperty -InputObject $Manifest -Name "targets"
    $sceneRoot = [string](@(Get-RequiredProperty -InputObject $targets -Name "sceneRoots")[0])
    $lookup = Get-ManifestSemanticBlockLookup -Manifest $Manifest

    $map = @{}
    foreach ($entry in $lookup.GetEnumerator()) {
        $scenePath = [string](Get-OptionalProperty -InputObject $entry.Value -Name "unityTargetPath" -Default "")
        $map[$entry.Key] = Convert-ScenePathToRelativeChildPath -SceneRoot $sceneRoot -ScenePath $scenePath
    }

    if (-not $map.ContainsKey("primary-cta")) {
        $resolvedPrimaryCtaBlockId = Resolve-ManifestBlockIdByVocabularyKey -Manifest $Manifest -VocabularyKey "primary-cta"
        if (-not [string]::IsNullOrWhiteSpace($resolvedPrimaryCtaBlockId)) {
            $map[$resolvedPrimaryCtaBlockId] = "MobileSaveDock/MobileSaveButton"
        }
    }

    $resolvedAuxActionBlockId = Resolve-ManifestBlockIdByVocabularyKey -Manifest $Manifest -VocabularyKey "aux-action"
    if (-not [string]::IsNullOrWhiteSpace($resolvedAuxActionBlockId) -and -not $map.ContainsKey($resolvedAuxActionBlockId)) {
        $map[$resolvedAuxActionBlockId] = "GarageHeaderRow/SettingsButton"
    }

    return $map
}

function Get-GarageVerifiedChildPaths {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $pathMap = Get-GarageSemanticBlockChildPathMap -Manifest $Manifest
    $semanticBlockIds = @(Get-ManifestSemanticBlockIds -Manifest $Manifest)
    $verified = New-Object System.Collections.Generic.List[string]

    foreach ($vocabularyKey in @(
        "aux-action",
        "focus-bar",
        "slot-selector",
        "editor-panel",
        "preview-card",
        "summary-card",
        "primary-cta"
    )) {
        $resolvedBlockId = Resolve-ManifestBlockIdByVocabularyKey -Manifest $Manifest -VocabularyKey $vocabularyKey
        if (-not [string]::IsNullOrWhiteSpace($resolvedBlockId) -and $semanticBlockIds -contains $resolvedBlockId -and -not [string]::IsNullOrWhiteSpace($pathMap[$resolvedBlockId])) {
            $verified.Add([string]$pathMap[$resolvedBlockId])
        }
    }

    if (-not [string]::IsNullOrWhiteSpace((Resolve-ManifestBlockIdByVocabularyKey -Manifest $Manifest -VocabularyKey "header-chrome"))) {
        $verified.Add("GarageSettingsOverlay/AccountCard/SettingsCloseButton")
    }

    return @($verified | Select-Object -Unique)
}

function Get-GaragePrefabCheckDefinitions {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $pathMap = Get-GarageSemanticBlockChildPathMap -Manifest $Manifest
    $semanticBlockIds = @(Get-ManifestSemanticBlockIds -Manifest $Manifest)
    $definitions = @(@{ Name = "rootFound"; ChildPath = "" })

    $checkMap = [ordered]@{
        "slot-selector" = "slotStripFound"
        "editor-panel" = "editorFound"
        "preview-card" = "previewFound"
        "summary-card" = "resultFound"
        "primary-cta" = "saveButtonFound"
    }

    foreach ($entry in $checkMap.GetEnumerator()) {
        $resolvedBlockId = Resolve-ManifestBlockIdByVocabularyKey -Manifest $Manifest -VocabularyKey $entry.Key
        if (-not [string]::IsNullOrWhiteSpace($resolvedBlockId) -and $semanticBlockIds -contains $resolvedBlockId -and -not [string]::IsNullOrWhiteSpace($pathMap[$resolvedBlockId])) {
            $definitions += @{ Name = $entry.Value; ChildPath = [string]$pathMap[$resolvedBlockId] }
        }
    }

    return $definitions
}

function Get-SurfaceBuildRegistry {
    return @(
        [PSCustomObject]@{
            builderId = "workspace-garage-root-v1"
            surfaceRole = "root"
            requiredVocabularyKeys = @("header-chrome", "slot-selector", "focus-bar", "editor-panel", "preview-card", "summary-card", "save-dock", "primary-cta")
            buildFunction = "Build-GaragePageRootFromContract"
            verifiedChildPathFunction = "Get-GarageVerifiedChildPaths"
            prefabCheckDefinitionFunction = "Get-GaragePrefabCheckDefinitions"
        }
    )
}

function Resolve-SurfaceBuildDefinition {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $surfaceRole = [string](Get-RequiredProperty -InputObject $Manifest -Name "surfaceRole")
    foreach ($definition in @(Get-SurfaceBuildRegistry)) {
        if ([string]$definition.surfaceRole -ne $surfaceRole) {
            continue
        }

        if (Test-ManifestHasVocabularyKeys -Manifest $Manifest -VocabularyKeys @($definition.requiredVocabularyKeys)) {
            return $definition
        }
    }

    $canonicalBlockIds = @((Get-ManifestCanonicalBlockIds -Manifest $Manifest) -join ", ")
    throw "No surface builder matches surfaceRole '$surfaceRole' with canonical blocks '$canonicalBlockIds'."
}

function Invoke-SurfaceBuildDefinition {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][object]$Definition
    )

    $buildFunctionName = [string]$Definition.buildFunction
    $buildCommand = Get-Command -Name $buildFunctionName -CommandType Function -ErrorAction Stop
    return & $buildCommand -Root $Root -Manifest $Manifest
}

function Convert-HtmlToPlainText {
    param([AllowEmptyString()][string]$Html)

    if ([string]::IsNullOrWhiteSpace($Html)) {
        return ""
    }

    $text = $Html -replace '<[^>]+>', ' '
    $text = $text -replace '&nbsp;', ' '
    $text = $text -replace '&amp;', '&'
    $text = $text -replace '&lt;', '<'
    $text = $text -replace '&gt;', '>'
    $text = $text -replace '\s+', ' '
    return $text.Trim()
}

function Get-StitchArtifactPath {
    param(
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][string]$FileName
    )

    $source = Get-RequiredProperty -InputObject $Manifest -Name "source"
    $projectId = [string](Get-RequiredProperty -InputObject $source -Name "projectId")
    $screenId = [string](Get-RequiredProperty -InputObject $source -Name "screenId")
    $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
    return Join-Path $repoRoot ("artifacts\stitch\{0}\{1}\{2}" -f $projectId, $screenId, $FileName)
}

function Ensure-StitchArtifact {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $htmlPath = Get-StitchArtifactPath -Manifest $Manifest -FileName "screen.html"
    if (Test-Path -LiteralPath $htmlPath) {
        return $htmlPath
    }

    $source = Get-RequiredProperty -InputObject $Manifest -Name "source"
    $projectId = [string](Get-RequiredProperty -InputObject $source -Name "projectId")
    $screenId = [string](Get-RequiredProperty -InputObject $source -Name "screenId")
    $repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))

    Push-Location $repoRoot
    try {
        & npm run stitch:fetch:screen -- --project $projectId --screen $screenId | Out-Null
    }
    finally {
        Pop-Location
    }

    if (-not (Test-Path -LiteralPath $htmlPath)) {
        throw "Stitch source artifact could not be resolved: $htmlPath"
    }

    return $htmlPath
}

function Get-StitchImageDimensions {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $imagePath = Get-StitchArtifactPath -Manifest $Manifest -FileName "screen.png"
    if (-not (Test-Path -LiteralPath $imagePath)) {
        throw "Stitch image artifact could not be resolved: $imagePath"
    }

    Add-Type -AssemblyName System.Drawing
    $image = [System.Drawing.Image]::FromFile($imagePath)
    try {
        return [PSCustomObject]@{
            path = $imagePath
            width = [int]$image.Width
            height = [int]$image.Height
        }
    }
    finally {
        $image.Dispose()
    }
}

function Get-RegexGroupValue {
    param(
        [AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [string]$GroupName = "value"
    )

    $match = [regex]::Match($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $match.Success) {
        return ""
    }

    return (Convert-HtmlToPlainText -Html $match.Groups[$GroupName].Value)
}

function Get-RegexRawGroupValue {
    param(
        [AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [string]$GroupName = "value"
    )

    $match = [regex]::Match($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $match.Success) {
        return ""
    }

    return [string]$match.Groups[$GroupName].Value
}

function Get-RegexAllGroupValues {
    param(
        [AllowEmptyString()][string]$Text,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [string]$GroupName = "value"
    )

    $matches = [regex]::Matches($Text, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $values = @()
    foreach ($match in $matches) {
        $values += (Convert-HtmlToPlainText -Html $match.Groups[$GroupName].Value)
    }

    return $values
}

function Convert-HexToUnityColor {
    param([string]$Hex)

    if ([string]::IsNullOrWhiteSpace($Hex)) {
        return ""
    }

    $normalized = $Hex.Trim()
    if ($normalized -notmatch '^#') {
        $normalized = "#$normalized"
    }

    if ($normalized -match '^#[0-9a-fA-F]{6}$') {
        return ($normalized.ToUpperInvariant() + "FF")
    }

    if ($normalized -match '^#[0-9a-fA-F]{8}$') {
        return $normalized.ToUpperInvariant()
    }

    return ""
}

function Convert-OpacityPercentToHex {
    param([string]$OpacityText)

    if ([string]::IsNullOrWhiteSpace($OpacityText)) {
        return "FF"
    }

    $opacity = [double]::Parse($OpacityText, [System.Globalization.CultureInfo]::InvariantCulture)
    $opacity = [Math]::Max(0.0, [Math]::Min(100.0, $opacity))
    $alpha = [int][Math]::Round(($opacity / 100.0) * 255.0)
    return $alpha.ToString("X2")
}

function Get-TailwindColorTable {
    param([AllowEmptyString()][string]$Html)

    $table = @{
        "transparent" = "#00000000"
        "white" = "#FFFFFFFF"
        "black" = "#000000FF"
        "gray-900" = "#111827FF"
    }

    foreach ($family in @("zinc", "amber", "blue")) {
        $familyBlock = Get-RegexRawGroupValue -Text $Html -Pattern ("{0}:\s*\{{(?<value>.*?)\n\s*\}}" -f [regex]::Escape($family))
        if ([string]::IsNullOrWhiteSpace($familyBlock)) {
            continue
        }

        $shadeMatches = [regex]::Matches(
            $familyBlock,
            '(?<shade>\d+):\s*''(?<hex>#[0-9a-fA-F]{6})''',
            [System.Text.RegularExpressions.RegexOptions]::Singleline)

        foreach ($shadeMatch in $shadeMatches) {
            $table["$family-$($shadeMatch.Groups["shade"].Value)"] = Convert-HexToUnityColor -Hex $shadeMatch.Groups["hex"].Value
        }
    }

    return $table
}

function Resolve-TailwindColor {
    param(
        [string]$Token,
        [hashtable]$ColorTable,
        [string]$Default = ""
    )

    if ([string]::IsNullOrWhiteSpace($Token)) {
        return $Default
    }

    if ($Token -eq "transparent") {
        return "#00000000"
    }

    if ($Token -match '^(?<base>[a-z]+-\d+)(?:/(?<opacity>\d+))?$') {
        $base = $matches["base"]
        if ($ColorTable.ContainsKey($base)) {
            $color = [string]$ColorTable[$base]
            if ([string]::IsNullOrWhiteSpace($matches["opacity"])) {
                return $color
            }

            $alpha = Convert-OpacityPercentToHex -OpacityText $matches["opacity"]
            return ($color.Substring(0, 7) + $alpha)
        }
    }

    $direct = Convert-HexToUnityColor -Hex $Token
    if (-not [string]::IsNullOrWhiteSpace($direct)) {
        return $direct
    }

    return $Default
}

function Get-ClassUtilityToken {
    param(
        [AllowEmptyString()][string]$ClassText,
        [Parameter(Mandatory = $true)][string]$Prefix
    )

    if ([string]::IsNullOrWhiteSpace($ClassText)) {
        return ""
    }

    $pattern = "(?:^|\s){0}(?<value>\[[^\]]+\]|[^\s]+)" -f [regex]::Escape($Prefix)
    $match = [regex]::Match($ClassText, $pattern)
    if (-not $match.Success) {
        return ""
    }

    return [string]$match.Groups["value"].Value
}

function Convert-TailwindLengthToPixels {
    param([string]$Token)

    if ([string]::IsNullOrWhiteSpace($Token)) {
        return $null
    }

    if ($Token -match '^\[(?<px>\d+)px\]$') {
        return [int]$matches["px"]
    }

    if ($Token -match '^(?<n>\d+(?:\.\d+)?)$') {
        $value = [double]::Parse($matches["n"], [System.Globalization.CultureInfo]::InvariantCulture)
        return [int][Math]::Round($value * 4.0)
    }

    return $null
}

function Get-TailwindLengthFromClass {
    param(
        [AllowEmptyString()][string]$ClassText,
        [Parameter(Mandatory = $true)][string]$Prefix,
        [int]$Default = 0
    )

    $token = Get-ClassUtilityToken -ClassText $ClassText -Prefix $Prefix
    $pixels = Convert-TailwindLengthToPixels -Token $token
    if ($null -eq $pixels) {
        return $Default
    }

    return [int]$pixels
}

function Get-TailwindFontSizeFromClass {
    param(
        [AllowEmptyString()][string]$ClassText,
        [int]$Default = 14
    )

    $token = Get-ClassUtilityToken -ClassText $ClassText -Prefix "text-"
    if ([string]::IsNullOrWhiteSpace($token)) {
        return $Default
    }

    if ($token -match '^\[(?<px>\d+)px\]$') {
        return [int]$matches["px"]
    }

    switch ($token) {
        "xs" { return 12 }
        "sm" { return 14 }
        "base" { return 16 }
        "lg" { return 18 }
        "xl" { return 20 }
        "2xl" { return 24 }
        "4xl" { return 36 }
        default { return $Default }
    }
}

function Get-TailwindColorFromClass {
    param(
        [AllowEmptyString()][string]$ClassText,
        [Parameter(Mandatory = $true)][string]$Prefix,
        [hashtable]$ColorTable,
        [string]$Default = ""
    )

    $token = Get-ClassUtilityToken -ClassText $ClassText -Prefix $Prefix
    return Resolve-TailwindColor -Token $token -ColorTable $ColorTable -Default $Default
}

function Get-TailwindHorizontalPaddingFromClass {
    param(
        [AllowEmptyString()][string]$ClassText,
        [int]$Default = 0
    )

    $all = Get-TailwindLengthFromClass -ClassText $ClassText -Prefix "p-" -Default ([int]::MinValue)
    if ($all -ne [int]::MinValue) {
        return $all
    }

    return Get-TailwindLengthFromClass -ClassText $ClassText -Prefix "px-" -Default $Default
}

function Get-TailwindVerticalPaddingFromClass {
    param(
        [AllowEmptyString()][string]$ClassText,
        [int]$Default = 0
    )

    $all = Get-TailwindLengthFromClass -ClassText $ClassText -Prefix "p-" -Default ([int]::MinValue)
    if ($all -ne [int]::MinValue) {
        return $all
    }

    return Get-TailwindLengthFromClass -ClassText $ClassText -Prefix "py-" -Default $Default
}

function Get-GarageStitchDerivedSpec {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $htmlPath = Ensure-StitchArtifact -Manifest $Manifest
    $imageInfo = Get-StitchImageDimensions -Manifest $Manifest
    $html = Get-Content -LiteralPath $htmlPath -Raw
    $colorTable = Get-TailwindColorTable -Html $html

    $headerRaw = Get-RegexRawGroupValue -Text $html -Pattern '<header class="(?<headerClass>[^"]+)"[^>]*>(?<value>.*?)</header>'
    $headerClass = Get-RegexRawGroupValue -Text $html -Pattern '<header class="(?<value>[^"]+)"'
    $mainClass = Get-RegexRawGroupValue -Text $html -Pattern '<main class="(?<value>[^"]+)"'
    $slotSectionRaw = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Slot Selector -->\s*<section[^>]*>(?<value>.*?)</section>'
    $slotSectionClass = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Slot Selector -->\s*<section class="(?<value>[^"]+)"'
    $focusSectionRaw = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Part Focus Bar -->\s*<section[^>]*>(?<value>.*?)</section>'
    $focusSectionClass = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Part Focus Bar -->\s*<section class="(?<value>[^"]+)"'
    $editorSectionRaw = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Focused Editor Area -->\s*<section[^>]*>(?<value>.*?)</section>'
    $editorSectionClass = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Focused Editor Area -->\s*<section class="(?<value>[^"]+)"'
    $previewSectionRaw = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Preview & Summary -->\s*<section[^>]*>(?<value>.*?)</section>'
    $previewSectionClass = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Preview & Summary -->\s*<section class="(?<value>[^"]+)"'
    $saveDockSectionRaw = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Persistent Bottom Dock \(Action\) -->\s*<div[^>]*>(?<value>.*?)</div>'
    $saveDockClass = Get-RegexRawGroupValue -Text $html -Pattern '<!-- Persistent Bottom Dock \(Action\) -->\s*<div class="(?<value>[^"]+)"'
    $slotButtons = [regex]::Matches(
        $slotSectionRaw,
        '<button\b(?<attrs>[^>]*)>(?<body>.*?)</button>',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)

    $slots = @()
    foreach ($button in $slotButtons) {
        $body = [string]$button.Groups["body"].Value
        $attrs = [string]$button.Groups["attrs"].Value
        $buttonClass = Get-RegexRawGroupValue -Text $attrs -Pattern 'class="(?<value>[^"]+)"'
        $unit = Get-RegexGroupValue -Text $body -Pattern '<span[^>]*>(?<value>UNIT[_ ]?\d+)</span>'
        $roleMatches = Get-RegexAllGroupValues -Text $body -Pattern '<span[^>]*>(?<value>[A-Z_]+)</span>'
        $role = @($roleMatches | Where-Object { $_ -notmatch '^UNIT' }) | Select-Object -Last 1
        $icon = Get-RegexGroupValue -Text $body -Pattern '<span class="material-symbols-outlined[^"]*"[^>]*>(?<value>.*?)</span>'
        $unitClass = Get-RegexRawGroupValue -Text $body -Pattern '<span class="(?<value>[^"]*font-mono[^"]*text-\[10px\][^"]*)"[^>]*>UNIT[_ ]?\d+</span>'
        $iconPlateClass = Get-RegexRawGroupValue -Text $body -Pattern '<div class="(?<value>[^"]*w-10 h-10[^"]*justify-center[^"]*)">'
        $iconClass = Get-RegexRawGroupValue -Text $body -Pattern '<span class="(?<value>material-symbols-outlined[^"]*)"[^>]*>.*?</span>'
        $roleClass = Get-RegexRawGroupValue -Text $body -Pattern '<span class="(?<value>[^"]*text-\[9px\][^"]*font-bold[^"]*)">[A-Z_]+</span>'
        $slots += [PSCustomObject]@{
            unit = if ([string]::IsNullOrWhiteSpace($unit)) { "" } else { $unit }
            role = if ([string]::IsNullOrWhiteSpace([string]$role)) { "" } else { [string]$role }
            icon = $icon
            isSelected = ($attrs -match 'text-blue-400' -or $attrs -match 'border-blue-500/50')
            classes = [PSCustomObject]@{
                button = $buttonClass
                unit = $unitClass
                iconPlate = $iconPlateClass
                icon = $iconClass
                role = $roleClass
            }
        }
    }

    $tabs = @()
    $focusButtons = [regex]::Matches(
        $focusSectionRaw,
        '<button\b(?<attrs>[^>]*)>(?<body>.*?)</button>',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)

    foreach ($button in $focusButtons) {
        $tabs += [PSCustomObject]@{
            label = (Convert-HtmlToPlainText -Html $button.Groups["body"].Value)
            isActive = ([string]$button.Groups["attrs"].Value -match 'text-blue-500')
            class = (Get-RegexRawGroupValue -Text ([string]$button.Groups["attrs"].Value) -Pattern 'class="(?<value>[^"]+)"')
        }
    }

    $stats = @()
    $statMatches = [regex]::Matches(
        $editorSectionRaw,
        '<span class="text-\[10px\][^"]*">(?<label>.*?)</span>\s*<span class="text-xs[^"]*">(?<value>.*?)</span>',
        [System.Text.RegularExpressions.RegexOptions]::Singleline)

    foreach ($stat in $statMatches) {
        $stats += [PSCustomObject]@{
            label = (Convert-HtmlToPlainText -Html $stat.Groups["label"].Value)
            value = (Convert-HtmlToPlainText -Html $stat.Groups["value"].Value)
        }
    }

    $headerLeadGroupClass = Get-RegexRawGroupValue -Text $headerRaw -Pattern '<div class="(?<value>[^"]*items-center gap-[^"]*)"'
    $headerBadgeClass = Get-RegexRawGroupValue -Text $headerRaw -Pattern '<div class="(?<value>[^"]*w-8 h-8[^"]*justify-center[^"]*)">'
    $headerTitleClass = Get-RegexRawGroupValue -Text $headerRaw -Pattern '<h1 class="(?<value>[^"]+)"'
    $headerStatusClass = Get-RegexRawGroupValue -Text $headerRaw -Pattern '<p class="(?<value>[^"]+)"'
    $settingsButtonClass = Get-RegexRawGroupValue -Text $headerRaw -Pattern '<button class="(?<value>[^"]+)"'
    $settingsIconClass = Get-RegexRawGroupValue -Text $headerRaw -Pattern '<span class="(?<value>material-symbols-outlined[^"]*)"[^>]*>settings</span>'
    $contentWrapClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<div class="(?<value>[^"]*z-10 p-\d+[^"]*justify-between[^"]*)">'
    $previewHeaderClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<span class="(?<value>[^"]*uppercase[^"]*)">Blueprint View</span>'
    $previewTagClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<span class="(?<value>[^"]*px-1\.5 py-0\.5 bg-zinc-800[^"]*)">대장갑</span>'
    $previewRingClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<div class="(?<value>[^"]*w-24 h-24[^"]*rounded-full[^"]*)"'
    $previewGlyphClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<span class="(?<value>[^"]*text-4xl[^"]*)"[^>]*>android</span>'
    $powerTrackWrapClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<div class="(?<value>[^"]*w-full bg-zinc-950[^"]*p-1[^"]*gap-2[^"]*)">'
    $powerTrackClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<div class="(?<value>[^"]*flex-1 bg-zinc-900 h-1\.5[^"]*)">'
    $powerFillClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<div class="(?<value>[^"]*bg-blue-500[^"]*)"'
    $powerLabelClass = Get-RegexRawGroupValue -Text $previewSectionRaw -Pattern '<span class="(?<value>[^"]*text-\[8px\][^"]*text-blue-500[^"]*)">전력 70%</span>'
    $saveButtonClass = Get-RegexRawGroupValue -Text $saveDockSectionRaw -Pattern '<button class="(?<value>[^"]+)"'
    $editorTopRowClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<div class="(?<value>[^"]*justify-between items-start[^"]*)">'
    $editorHeaderGroupClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<div class="(?<value>[^"]*items-center gap-2 mb-1[^"]*)">'
    $editorBadgeClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<span class="(?<value>[^"]*text-\[9px\][^"]*border-blue-500/30[^"]*)">'
    $editorTitleClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<h2 class="(?<value>[^"]+)"'
    $editorSubtitleClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<p class="(?<value>[^"]*text-xs[^"]*)">'
    $editorIconPlateClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<div class="(?<value>[^"]*w-12 h-12[^"]*justify-center[^"]*)">'
    $statsGridClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<!-- Stats Grid -->\s*<div class="(?<value>[^"]+)">'
    $statCardClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<div class="(?<value>[^"]*bg-zinc-950/50 p-2[^"]*justify-between[^"]*)">'
    $modifierSectionClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<!-- Modifiers -->\s*<div class="(?<value>[^"]+)">'
    $modifierHeaderClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<div class="(?<value>[^"]*text-\[10px\][^"]*mb-2[^"]*justify-between[^"]*)">'
    $modifierScrollerClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<div class="(?<value>[^"]*hide-scrollbar[^"]*pb-1[^"]*)">'
    $modifierButtonClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<button class="(?<value>[^"]*min-w-\[120px\][^"]*py-1\.5[^"]*bg-blue-500/10[^"]*)">'
    $modifierTitleClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<div class="(?<value>[^"]*text-\[10px\][^"]*font-bold[^"]*)">광학 조준경</div>'
    $modifierSubtitleClass = Get-RegexRawGroupValue -Text $editorSectionRaw -Pattern '<div class="(?<value>[^"]*text-\[8px\][^"]*font-mono[^"]*)">\+ 명중률</div>'

    $selectedSlot = @($slots | Where-Object { $_.isSelected } | Select-Object -First 1)
    if ($selectedSlot.Count -eq 0) {
        $selectedSlot = @($slots | Select-Object -First 1)
    }
    $inactiveSlot = @($slots | Where-Object { -not $_.isSelected -and $_.role -ne "EMPTY" } | Select-Object -First 1)
    if ($inactiveSlot.Count -eq 0) {
        $inactiveSlot = @($slots | Select-Object -Skip 1 -First 1)
    }
    $emptySlot = @($slots | Where-Object { $_.role -eq "EMPTY" } | Select-Object -First 1)
    if ($emptySlot.Count -eq 0) {
        $emptySlot = @($slots | Select-Object -Last 1)
    }
    $firstTab = @($tabs | Select-Object -First 1)
    $activeTab = @($tabs | Where-Object { $_.isActive } | Select-Object -First 1)
    if ($activeTab.Count -eq 0) {
        $activeTab = $firstTab
    }

    $slotPadding = Get-TailwindHorizontalPaddingFromClass -ClassText ([string]$selectedSlot[0].classes.button) -Default 8
    $slotGap = Get-TailwindLengthFromClass -ClassText $slotSectionClass -Prefix "gap-" -Default 8
    $slotUnitFont = Get-TailwindFontSizeFromClass -ClassText ([string]$selectedSlot[0].classes.unit) -Default 10
    $slotUnitMargin = Get-TailwindLengthFromClass -ClassText ([string]$selectedSlot[0].classes.unit) -Prefix "mb-" -Default 4
    $slotIconSize = Get-TailwindLengthFromClass -ClassText ([string]$selectedSlot[0].classes.iconPlate) -Prefix "w-" -Default 40
    $slotIconMargin = Get-TailwindLengthFromClass -ClassText ([string]$selectedSlot[0].classes.iconPlate) -Prefix "mb-" -Default 4
    $slotRoleFont = Get-TailwindFontSizeFromClass -ClassText ([string]$selectedSlot[0].classes.role) -Default 9
    $slotHeight = [int][Math]::Ceiling(($slotPadding * 2) + $slotUnitFont + $slotUnitMargin + $slotIconSize + $slotIconMargin + $slotRoleFont)
    $editorPadding = Get-TailwindHorizontalPaddingFromClass -ClassText $editorSectionClass -Default 12
    $editorGap = Get-TailwindLengthFromClass -ClassText $editorSectionClass -Prefix "space-y-" -Default 16
    $editorBadgeFontSize = Get-TailwindFontSizeFromClass -ClassText $editorBadgeClass -Default 9
    $editorBadgePaddingY = Get-TailwindVerticalPaddingFromClass -ClassText $editorBadgeClass -Default 2
    $editorTitleFontSize = Get-TailwindFontSizeFromClass -ClassText $editorTitleClass -Default 14
    $editorSubtitleFontSize = Get-TailwindFontSizeFromClass -ClassText $editorSubtitleClass -Default 12
    $editorIconSize = Get-TailwindLengthFromClass -ClassText $editorIconPlateClass -Prefix "w-" -Default 48
    $editorTopGap = Get-TailwindLengthFromClass -ClassText $editorHeaderGroupClass -Prefix "mb-" -Default 4
    $editorTopRowHeight = [int][Math]::Max($editorIconSize, (($editorBadgeFontSize + ($editorBadgePaddingY * 2)) + $editorTopGap + $editorTitleFontSize + $editorSubtitleFontSize))
    $statsGap = Get-TailwindLengthFromClass -ClassText $statsGridClass -Prefix "gap-" -Default 8
    $statsPaddingTop = Get-TailwindLengthFromClass -ClassText $statsGridClass -Prefix "pt-" -Default 8
    $statCardPadding = Get-TailwindHorizontalPaddingFromClass -ClassText $statCardClass -Default 8
    $statLabelFontSize = 10
    $statValueFontSize = 12
    $statCardHeight = [int](($statCardPadding * 2) + [Math]::Max($statLabelFontSize, $statValueFontSize))
    $statsSectionHeight = [int]($statsPaddingTop + ($statCardHeight * 2) + $statsGap)
    $modifierPaddingTop = Get-TailwindLengthFromClass -ClassText $modifierSectionClass -Prefix "pt-" -Default 8
    $modifierHeaderFontSize = Get-TailwindFontSizeFromClass -ClassText $modifierHeaderClass -Default 10
    $modifierHeaderMarginBottom = Get-TailwindLengthFromClass -ClassText $modifierHeaderClass -Prefix "mb-" -Default 8
    $modifierScrollerGap = Get-TailwindLengthFromClass -ClassText $modifierScrollerClass -Prefix "gap-" -Default 8
    $modifierScrollerPaddingBottom = Get-TailwindLengthFromClass -ClassText $modifierScrollerClass -Prefix "pb-" -Default 4
    $modifierButtonPaddingY = Get-TailwindVerticalPaddingFromClass -ClassText $modifierButtonClass -Default 6
    $modifierTitleFontSize = Get-TailwindFontSizeFromClass -ClassText $modifierTitleClass -Default 10
    $modifierSubtitleFontSize = Get-TailwindFontSizeFromClass -ClassText $modifierSubtitleClass -Default 8
    $modifierButtonHeight = [int](($modifierButtonPaddingY * 2) + $modifierTitleFontSize + $modifierSubtitleFontSize)
    $modifierSectionHeight = [int]($modifierPaddingTop + $modifierHeaderFontSize + $modifierHeaderMarginBottom + $modifierButtonHeight + $modifierScrollerPaddingBottom)
    $selectorTitleFontSize = $modifierHeaderFontSize
    $selectorValueFontSize = $editorTitleFontSize
    $selectorHintFontSize = $modifierSubtitleFontSize
    $selectorInnerPadding = $editorPadding
    $selectorHeight = [int](($selectorInnerPadding * 2) + $selectorTitleFontSize + $selectorValueFontSize + $selectorHintFontSize)
    $clearButtonHeight = [int]($selectorHintFontSize + ($editorPadding * 1.5))
    $editorComputedHeight = [int](($editorPadding * 2) + $editorTopRowHeight + $editorGap + $statsSectionHeight + $editorGap + $modifierSectionHeight + ($editorGap * 3) + ($selectorHeight * 3) + $clearButtonHeight)
    $saveButtonFontSize = [int][Math]::Max($editorTitleFontSize, (Get-TailwindFontSizeFromClass -ClassText $headerTitleClass -Default 14))
    $saveButtonPaddingY = Get-TailwindVerticalPaddingFromClass -ClassText $saveButtonClass -Default 14
    $saveButtonHeight = [int](($saveButtonPaddingY * 2) + $saveButtonFontSize)
    $resultPadding = $editorPadding
    $resultTitleFontSize = $editorTitleFontSize
    $resultBodyFontSize = $editorSubtitleFontSize
    $resultGap = [int][Math]::Max(8, [Math]::Round($editorGap / 2.0))
    $toastHeight = [int](($resultBodyFontSize * 2) + $resultPadding)
    $resultComputedHeight = [int](($resultPadding * 2) + $resultTitleFontSize + $resultGap + $resultBodyFontSize + $resultGap + $resultBodyFontSize + $resultGap + $saveButtonHeight + $resultGap + $toastHeight)

    return [PSCustomObject]@{
        htmlPath = $htmlPath
        imagePath = $imageInfo.path
        headerTitle = Get-RegexGroupValue -Text $html -Pattern '<h1[^>]*>(?<value>.*?)</h1>'
        headerStatus = Get-RegexGroupValue -Text $html -Pattern '<p class="text-\[10px\][^"]*font-mono">(?<value>.*?)</p>'
        slots = $slots
        tabs = $tabs
        editorBadge = Get-RegexGroupValue -Text $editorSectionRaw -Pattern '<span class="px-1\.5 py-0\.5[^"]*">(?<value>.*?)</span>'
        editorTitle = Get-RegexGroupValue -Text $editorSectionRaw -Pattern '<h2[^>]*>(?<value>.*?)</h2>'
        editorSubtitle = Get-RegexGroupValue -Text $editorSectionRaw -Pattern '<p class="text-xs[^"]*">(?<value>.*?)</p>'
        stats = $stats
        previewHeader = Get-RegexGroupValue -Text $previewSectionRaw -Pattern '<span class="text-\[10px\][^"]*uppercase[^"]*">(?<value>.*?)</span>'
        previewTags = @(Get-RegexAllGroupValues -Text $previewSectionRaw -Pattern '<span class="px-1\.5 py-0\.5 bg-zinc-800[^"]*">(?<value>.*?)</span>')
        powerLabel = (@(Get-RegexAllGroupValues -Text $previewSectionRaw -Pattern '<span class="text-\[8px\][^"]*">(?<value>.*?)</span>') | Select-Object -Last 1)
        saveLabel = ((Get-RegexGroupValue -Text $saveDockSectionRaw -Pattern '<button[^>]*>(?<value>.*?)</button>') -replace '^save\s*', '').Trim()
        styles = [PSCustomObject]@{
            root = [PSCustomObject]@{
                referenceWidth = $imageInfo.width
                referenceHeight = $imageInfo.height
                backgroundColor = Get-TailwindColorFromClass -ClassText (Get-RegexRawGroupValue -Text $html -Pattern '<body class="(?<value>[^"]+)"') -Prefix "bg-" -ColorTable $colorTable -Default "#111827FF"
                contentPaddingX = Get-TailwindLengthFromClass -ClassText $mainClass -Prefix "px-" -Default 12
                contentPaddingTop = Get-TailwindLengthFromClass -ClassText $mainClass -Prefix "pt-" -Default 64
                contentGap = Get-TailwindLengthFromClass -ClassText $mainClass -Prefix "space-y-" -Default 16
                contentPaddingBottom = Get-TailwindLengthFromClass -ClassText $mainClass -Prefix "pb-" -Default 96
            }
            header = [PSCustomObject]@{
                height = Get-TailwindLengthFromClass -ClassText $headerClass -Prefix "h-" -Default 56
                backgroundColor = Get-TailwindColorFromClass -ClassText $headerClass -Prefix "bg-" -ColorTable $colorTable -Default "#09090BE6"
                borderColor = Get-TailwindColorFromClass -ClassText $headerClass -Prefix "border-" -ColorTable $colorTable -Default "#27272AFF"
                paddingX = Get-TailwindLengthFromClass -ClassText $headerClass -Prefix "px-" -Default 16
                contentGap = Get-TailwindLengthFromClass -ClassText $headerLeadGroupClass -Prefix "gap-" -Default 12
                badgeSize = Get-TailwindLengthFromClass -ClassText $headerBadgeClass -Prefix "w-" -Default 32
                badgeColor = Get-TailwindColorFromClass -ClassText $headerBadgeClass -Prefix "bg-" -ColorTable $colorTable -Default "#27272AFF"
                titleFontSize = Get-TailwindFontSizeFromClass -ClassText $headerTitleClass -Default 14
                titleColor = Get-TailwindColorFromClass -ClassText $headerTitleClass -Prefix "text-" -ColorTable $colorTable -Default "#F59E0BFF"
                statusFontSize = Get-TailwindFontSizeFromClass -ClassText $headerStatusClass -Default 10
                statusColor = Get-TailwindColorFromClass -ClassText $headerStatusClass -Prefix "text-" -ColorTable $colorTable -Default "#71717AFF"
                settingsSize = Get-TailwindLengthFromClass -ClassText $settingsButtonClass -Prefix "w-" -Default 32
                settingsColor = Get-TailwindColorFromClass -ClassText $settingsButtonClass -Prefix "text-" -ColorTable $colorTable -Default "#71717AFF"
                settingsFontSize = Get-TailwindFontSizeFromClass -ClassText $settingsIconClass -Default 20
            }
            slots = [PSCustomObject]@{
                gridGap = $slotGap
                slotPadding = $slotPadding
                itemHeight = $slotHeight
                selectedBackgroundColor = Get-TailwindColorFromClass -ClassText ([string]$selectedSlot[0].classes.button) -Prefix "bg-" -ColorTable $colorTable -Default "#5EB6FF1A"
                selectedBorderColor = Get-TailwindColorFromClass -ClassText ([string]$selectedSlot[0].classes.button) -Prefix "border-" -ColorTable $colorTable -Default "#5EB6FF80"
                inactiveBackgroundColor = Get-TailwindColorFromClass -ClassText ([string]$inactiveSlot[0].classes.button) -Prefix "bg-" -ColorTable $colorTable -Default "#18181B80"
                inactiveBorderColor = Get-TailwindColorFromClass -ClassText ([string]$inactiveSlot[0].classes.button) -Prefix "border-" -ColorTable $colorTable -Default "#27272AFF"
                emptyBackgroundColor = Get-TailwindColorFromClass -ClassText ([string]$emptySlot[0].classes.button) -Prefix "bg-" -ColorTable $colorTable -Default "#18181B4D"
                emptyBorderColor = Get-TailwindColorFromClass -ClassText ([string]$emptySlot[0].classes.button) -Prefix "border-" -ColorTable $colorTable -Default "#27272AFF"
                unitFontSize = $slotUnitFont
                unitSelectedColor = Get-TailwindColorFromClass -ClassText ([string]$selectedSlot[0].classes.unit) -Prefix "text-" -ColorTable $colorTable -Default "#60A5FAFF"
                unitInactiveColor = Get-TailwindColorFromClass -ClassText ([string]$inactiveSlot[0].classes.unit) -Prefix "text-" -ColorTable $colorTable -Default "#71717AFF"
                unitEmptyColor = Get-TailwindColorFromClass -ClassText ([string]$emptySlot[0].classes.unit) -Prefix "text-" -ColorTable $colorTable -Default "#52525BFF"
                iconSize = $slotIconSize
                iconPlateSelectedColor = Get-TailwindColorFromClass -ClassText ([string]$selectedSlot[0].classes.iconPlate) -Prefix "bg-" -ColorTable $colorTable -Default "#09090BFF"
                iconPlateInactiveColor = Get-TailwindColorFromClass -ClassText ([string]$inactiveSlot[0].classes.iconPlate) -Prefix "bg-" -ColorTable $colorTable -Default "#09090BFF"
                iconSelectedColor = Get-TailwindColorFromClass -ClassText ([string]$selectedSlot[0].classes.icon) -Prefix "text-" -ColorTable $colorTable -Default "#5EB6FFFF"
                iconInactiveColor = Get-TailwindColorFromClass -ClassText ([string]$inactiveSlot[0].classes.icon) -Prefix "text-" -ColorTable $colorTable -Default "#52525BFF"
                roleFontSize = $slotRoleFont
                roleSelectedColor = Get-TailwindColorFromClass -ClassText ([string]$selectedSlot[0].classes.role) -Prefix "text-" -ColorTable $colorTable -Default "#D4D4D8FF"
                roleInactiveColor = Get-TailwindColorFromClass -ClassText ([string]$inactiveSlot[0].classes.role) -Prefix "text-" -ColorTable $colorTable -Default "#71717AFF"
                roleEmptyColor = Get-TailwindColorFromClass -ClassText ([string]$emptySlot[0].classes.role) -Prefix "text-" -ColorTable $colorTable -Default "#52525BFF"
                topAccentColor = "#5EB6FFFF"
            }
            tabs = [PSCustomObject]@{
                gap = Get-TailwindLengthFromClass -ClassText $focusSectionClass -Prefix "gap-" -Default 4
                paddingBottom = Get-TailwindLengthFromClass -ClassText $focusSectionClass -Prefix "pb-" -Default 8
                borderColor = Get-TailwindColorFromClass -ClassText $focusSectionClass -Prefix "border-" -ColorTable $colorTable -Default "#27272AFF"
                buttonHeight = ((Get-TailwindVerticalPaddingFromClass -ClassText ([string]$firstTab[0].class) -Default 8) * 2) + (Get-TailwindFontSizeFromClass -ClassText ([string]$firstTab[0].class) -Default 12)
                inactiveTextColor = Get-TailwindColorFromClass -ClassText ([string]$firstTab[0].class) -Prefix "text-" -ColorTable $colorTable -Default "#A1A1AAFF"
                activeTextColor = Get-TailwindColorFromClass -ClassText ([string]$activeTab[0].class) -Prefix "text-" -ColorTable $colorTable -Default "#5EB6FFFF"
                activeBorderColor = Get-TailwindColorFromClass -ClassText ([string]$activeTab[0].class) -Prefix "border-" -ColorTable $colorTable -Default "#5EB6FFFF"
                activeBackgroundColor = Get-TailwindColorFromClass -ClassText ([string]$activeTab[0].class) -Prefix "from-" -ColorTable $colorTable -Default "#5EB6FF1A"
                fontSize = Get-TailwindFontSizeFromClass -ClassText ([string]$firstTab[0].class) -Default 12
            }
            editor = [PSCustomObject]@{
                backgroundColor = Get-TailwindColorFromClass -ClassText $editorSectionClass -Prefix "bg-" -ColorTable $colorTable -Default "#18181BFF"
                borderColor = Get-TailwindColorFromClass -ClassText $editorSectionClass -Prefix "border-" -ColorTable $colorTable -Default "#27272AFF"
                padding = $editorPadding
                gap = $editorGap
                titleFontSize = $editorTitleFontSize
                subtitleFontSize = $editorSubtitleFontSize
                badgeFontSize = $editorBadgeFontSize
                iconSize = $editorIconSize
                selectorTitleFontSize = $selectorTitleFontSize
                selectorValueFontSize = $selectorValueFontSize
                selectorHintFontSize = $selectorHintFontSize
                selectorHeight = $selectorHeight
                clearButtonHeight = $clearButtonHeight
                statCardHeight = $statCardHeight
                computedHeight = $editorComputedHeight
            }
            preview = [PSCustomObject]@{
                height = Get-TailwindLengthFromClass -ClassText $previewSectionClass -Prefix "h-" -Default 192
                backgroundColor = Get-TailwindColorFromClass -ClassText $previewSectionClass -Prefix "bg-" -ColorTable $colorTable -Default "#18181BFF"
                borderColor = Get-TailwindColorFromClass -ClassText $previewSectionClass -Prefix "border-" -ColorTable $colorTable -Default "#27272AFF"
                padding = Get-TailwindHorizontalPaddingFromClass -ClassText $contentWrapClass -Default 12
                headerFontSize = Get-TailwindFontSizeFromClass -ClassText $previewHeaderClass -Default 10
                headerColor = Get-TailwindColorFromClass -ClassText $previewHeaderClass -Prefix "text-" -ColorTable $colorTable -Default "#71717AFF"
                tagFontSize = Get-TailwindFontSizeFromClass -ClassText $previewTagClass -Default 8
                tagBackgroundColor = Get-TailwindColorFromClass -ClassText $previewTagClass -Prefix "bg-" -ColorTable $colorTable -Default "#27272AFF"
                tagTextColor = Get-TailwindColorFromClass -ClassText $previewTagClass -Prefix "text-" -ColorTable $colorTable -Default "#D4D4D8FF"
                ringSize = Get-TailwindLengthFromClass -ClassText $previewRingClass -Prefix "w-" -Default 96
                ringBorderColor = Get-TailwindColorFromClass -ClassText $previewRingClass -Prefix "border-" -ColorTable $colorTable -Default "#5EB6FF4D"
                glyphFontSize = Get-TailwindFontSizeFromClass -ClassText $previewGlyphClass -Default 36
                glyphColor = Get-TailwindColorFromClass -ClassText $previewGlyphClass -Prefix "text-" -ColorTable $colorTable -Default "#60A5FAFF"
                powerTrackWrapColor = Get-TailwindColorFromClass -ClassText $powerTrackWrapClass -Prefix "bg-" -ColorTable $colorTable -Default "#09090BFF"
                powerTrackPadding = Get-TailwindHorizontalPaddingFromClass -ClassText $powerTrackWrapClass -Default 4
                powerTrackGap = Get-TailwindLengthFromClass -ClassText $powerTrackWrapClass -Prefix "gap-" -Default 8
                powerTrackHeight = Get-TailwindLengthFromClass -ClassText $powerTrackClass -Prefix "h-" -Default 6
                powerTrackColor = Get-TailwindColorFromClass -ClassText $powerTrackClass -Prefix "bg-" -ColorTable $colorTable -Default "#18181BFF"
                powerFillColor = Get-TailwindColorFromClass -ClassText $powerFillClass -Prefix "bg-" -ColorTable $colorTable -Default "#5EB6FFFF"
                powerLabelFontSize = Get-TailwindFontSizeFromClass -ClassText $powerLabelClass -Default 8
                powerLabelColor = Get-TailwindColorFromClass -ClassText $powerLabelClass -Prefix "text-" -ColorTable $colorTable -Default "#5EB6FFFF"
            }
            saveDock = [PSCustomObject]@{
                backgroundColor = Get-TailwindColorFromClass -ClassText $saveDockClass -Prefix "bg-" -ColorTable $colorTable -Default "#09090BF2"
                borderColor = Get-TailwindColorFromClass -ClassText $saveDockClass -Prefix "border-" -ColorTable $colorTable -Default "#27272AFF"
                paddingX = Get-TailwindLengthFromClass -ClassText $saveDockClass -Prefix "px-" -Default 16
                paddingTop = Get-TailwindLengthFromClass -ClassText $saveDockClass -Prefix "pt-" -Default 8
                buttonPaddingY = Get-TailwindVerticalPaddingFromClass -ClassText $saveButtonClass -Default 14
                buttonFontSize = $saveButtonFontSize
                buttonHeight = $saveButtonHeight
                buttonBackgroundColor = Get-TailwindColorFromClass -ClassText $saveButtonClass -Prefix "bg-" -ColorTable $colorTable -Default "#F59E0BFF"
                buttonTextColor = Get-TailwindColorFromClass -ClassText $saveButtonClass -Prefix "text-" -ColorTable $colorTable -Default "#09090BFF"
                buttonMarginBottom = Get-TailwindLengthFromClass -ClassText $saveButtonClass -Prefix "mb-" -Default 16
            }
            result = [PSCustomObject]@{
                padding = $resultPadding
                titleFontSize = $resultTitleFontSize
                bodyFontSize = $resultBodyFontSize
                gap = $resultGap
                toastHeight = $toastHeight
                computedHeight = $resultComputedHeight
            }
        }
    }
}

function Open-TempScene {
    param([Parameter(Mandatory = $true)][string]$Root)

    Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/scene/open" -Body @{
        scenePath = "Assets/Scenes/TempScene.unity"
        saveCurrentSceneIfDirty = $true
    } -TimeoutSec 60 | Out-Null
}

function Set-SceneComponentReference {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$GameObjectPath,
        [Parameter(Mandatory = $true)][string]$ComponentType,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][string]$Value
    )

    Set-McpProperty -Root $Root -Path $GameObjectPath -ComponentType $ComponentType -PropertyName $PropertyName -Value $Value
}

function Set-SceneArrayReference {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$GameObjectPath,
        [Parameter(Mandatory = $true)][string]$ComponentType,
        [Parameter(Mandatory = $true)][string]$ArrayName,
        [Parameter(Mandatory = $true)][string[]]$Values
    )

    for ($i = 0; $i -lt $Values.Length; $i++) {
        Set-McpProperty -Root $Root -Path $GameObjectPath -ComponentType $ComponentType -PropertyName "$ArrayName.Array.data[$i]" -Value $Values[$i]
    }
}

function New-GarageSlotItem {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$ParentPath,
        [Parameter(Mandatory = $true)][int]$SlotIndex,
        [object]$SlotSpec = $null,
        [object]$SlotStyles = $null
    )

    $name = "GarageSlot$SlotIndex"
    $isSelected = [bool](Get-OptionalProperty -InputObject $SlotSpec -Name "isSelected" -Default ($SlotIndex -eq 1))
    $slotUnit = [string](Get-OptionalProperty -InputObject $SlotSpec -Name "unit" -Default ("UNIT_{0:00}" -f $SlotIndex))
    $slotRole = [string](Get-OptionalProperty -InputObject $SlotSpec -Name "role" -Default $(switch ($SlotIndex) { 1 { "ASSAULT" } 2 { "DEFENSE" } 3 { "SUPPORT" } 4 { "EMPTY" } default { "EMPTY" } }))
    $slotIcon = [string](Get-OptionalProperty -InputObject $SlotSpec -Name "icon" -Default $(switch ($SlotIndex) { 1 { "▲" } 2 { "■" } 3 { "✦" } 4 { "+" } default { "•" } }))
    $slotSummary = if ($isSelected) { "ACTIVE" } else { "" }
    $isEmpty = ($slotRole -eq "EMPTY")
    $slotWidth = [int](Get-OptionalProperty -InputObject $SlotStyles -Name "itemWidth" -Default 84)
    $slotHeight = [int](Get-OptionalProperty -InputObject $SlotStyles -Name "itemHeight" -Default 82)
    $slotPadding = [int](Get-OptionalProperty -InputObject $SlotStyles -Name "slotPadding" -Default 8)
    $iconSize = [int](Get-OptionalProperty -InputObject $SlotStyles -Name "iconSize" -Default 40)
    $unitFontSize = [int](Get-OptionalProperty -InputObject $SlotStyles -Name "unitFontSize" -Default 10)
    $roleFontSize = [int](Get-OptionalProperty -InputObject $SlotStyles -Name "roleFontSize" -Default 9)
    $backgroundColor = if ($isSelected) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "selectedBackgroundColor" -Default "#5EB6FF1A")
    } elseif ($isEmpty) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "emptyBackgroundColor" -Default "#18181B4D")
    } else {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "inactiveBackgroundColor" -Default "#18181B80")
    }
    $borderColor = if ($isSelected) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "selectedBorderColor" -Default "#5EB6FF80")
    } elseif ($isEmpty) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "emptyBorderColor" -Default "#27272AFF")
    } else {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "inactiveBorderColor" -Default "#27272AFF")
    }
    $unitColor = if ($isSelected) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "unitSelectedColor" -Default "#60A5FAFF")
    } elseif ($isEmpty) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "unitEmptyColor" -Default "#52525BFF")
    } else {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "unitInactiveColor" -Default "#71717AFF")
    }
    $roleColor = if ($isSelected) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "roleSelectedColor" -Default "#D4D4D8FF")
    } elseif ($isEmpty) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "roleEmptyColor" -Default "#52525BFF")
    } else {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "roleInactiveColor" -Default "#71717AFF")
    }
    $iconPlateColor = if ($isSelected) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "iconPlateSelectedColor" -Default "#09090BFF")
    } else {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "iconPlateInactiveColor" -Default "#09090BFF")
    }
    $iconColor = if ($isSelected) {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "iconSelectedColor" -Default "#5EB6FFFF")
    } else {
        [string](Get-OptionalProperty -InputObject $SlotStyles -Name "iconInactiveColor" -Default "#52525BFF")
    }

    New-McpPanel -Root $Root -Name $name -ParentPath $ParentPath -Width $slotWidth -Height $slotHeight
    $slotPath = "$ParentPath/$name"
    Add-McpComponent -Root $Root -Path $slotPath -ComponentType "CanvasGroup"
    Add-McpComponent -Root $Root -Path $slotPath -ComponentType "GarageSlotItemView"
    Add-McpLayoutElement -Root $Root -Path $slotPath -PreferredHeight $slotHeight
    Set-McpProperty -Root $Root -Path $slotPath -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value ([string]$slotWidth)
    Set-McpImageColor -Root $Root -Path $slotPath -Color $backgroundColor

    New-McpButton -Root $Root -Name "SelectButton" -ParentPath $slotPath -Text ""
    $buttonPath = "$slotPath/SelectButton"
    Set-McpRectTransform -Root $Root -Path $buttonPath -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
    Set-McpImageColor -Root $Root -Path $buttonPath -Color "#00000000"

    New-McpPanel -Root $Root -Name "TopAccent" -ParentPath $slotPath -Width 0 -Height 2
    Set-McpRectTransform -Root $Root -Path "$slotPath/TopAccent" -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,0)" -SizeDelta "(0,2)"
    Set-McpImageColor -Root $Root -Path "$slotPath/TopAccent" -Color ($(if ($isSelected) { [string](Get-OptionalProperty -InputObject $SlotStyles -Name "topAccentColor" -Default "#5EB6FFFF") } else { "#00000000" }))

    New-McpText -Root $Root -Name "SlotNumberText" -ParentPath $slotPath -Text $slotUnit -FontSize $unitFontSize -Color $unitColor | Out-Null
    Set-McpRectTransform -Root $Root -Path "$slotPath/SlotNumberText" -AnchorMin "(0.5,1)" -AnchorMax "(0.5,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,-$slotPadding)" -SizeDelta "($([Math]::Max(56, $slotWidth - ($slotPadding * 2))),12)"

    New-McpPanel -Root $Root -Name "IconPlate" -ParentPath $slotPath -Width $iconSize -Height $iconSize
    Set-McpRectTransform -Root $Root -Path "$slotPath/IconPlate" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,-2)" -SizeDelta "($iconSize,$iconSize)"
    Set-McpImageColor -Root $Root -Path "$slotPath/IconPlate" -Color $iconPlateColor
    New-McpText -Root $Root -Name "IconGlyphText" -ParentPath "$slotPath/IconPlate" -Text $slotIcon -FontSize ([Math]::Max(14, [int]($iconSize * 0.45))) -Color $iconColor | Out-Null
    Set-McpRectTransform -Root $Root -Path "$slotPath/IconPlate/IconGlyphText" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(24,24)"

    New-McpText -Root $Root -Name "TitleText" -ParentPath $slotPath -Text $slotRole -FontSize $roleFontSize -Color $roleColor | Out-Null
    Set-McpRectTransform -Root $Root -Path "$slotPath/TitleText" -AnchorMin "(0.5,0)" -AnchorMax "(0.5,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,$slotPadding)" -SizeDelta "($([Math]::Max(56, $slotWidth - ($slotPadding * 2))),12)"

    New-McpText -Root $Root -Name "SummaryText" -ParentPath $slotPath -Text $slotSummary -FontSize 6 -Color "#7D8FA4FF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$slotPath/SummaryText" -AnchorMin "(0.5,0)" -AnchorMax "(0.5,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,$($slotPadding + 10))" -SizeDelta "($([Math]::Max(56, $slotWidth - ($slotPadding * 2))),10)"

    New-McpPanel -Root $Root -Name "ArrowIndicator" -ParentPath $slotPath -Width 10 -Height 10
    Set-McpRectTransform -Root $Root -Path "$slotPath/ArrowIndicator" -AnchorMin "(1,1)" -AnchorMax "(1,1)" -Pivot "(1,1)" -AnchoredPosition "(-6,-6)" -SizeDelta "(10,10)"
    Set-McpImageColor -Root $Root -Path "$slotPath/ArrowIndicator" -Color ($(if ($isSelected) { $iconColor } else { "#00000000" }))

    New-McpPanel -Root $Root -Name "BorderImage" -ParentPath $slotPath -Width 0 -Height 0
    Set-McpRectTransform -Root $Root -Path "$slotPath/BorderImage" -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
    Set-McpImageColor -Root $Root -Path "$slotPath/BorderImage" -Color $borderColor

    Set-SceneComponentReference -Root $Root -GameObjectPath $slotPath -ComponentType "GarageSlotItemView" -PropertyName "_button" -Value $buttonPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $slotPath -ComponentType "GarageSlotItemView" -PropertyName "_background" -Value $slotPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $slotPath -ComponentType "GarageSlotItemView" -PropertyName "_slotNumberText" -Value "$slotPath/SlotNumberText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $slotPath -ComponentType "GarageSlotItemView" -PropertyName "_titleText" -Value "$slotPath/TitleText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $slotPath -ComponentType "GarageSlotItemView" -PropertyName "_summaryText" -Value "$slotPath/SummaryText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $slotPath -ComponentType "GarageSlotItemView" -PropertyName "_arrowIndicator" -Value "$slotPath/ArrowIndicator"
    Set-SceneComponentReference -Root $Root -GameObjectPath $slotPath -ComponentType "GarageSlotItemView" -PropertyName "_borderImage" -Value "$slotPath/BorderImage"
    Set-SceneComponentReference -Root $Root -GameObjectPath $slotPath -ComponentType "GarageSlotItemView" -PropertyName "_canvasGroup" -Value $slotPath

    return $slotPath
}

function New-GaragePartSelector {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$ParentPath,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Title,
        [object]$EditorStyles = $null
    )

    $selectorHeight = [int](Get-OptionalProperty -InputObject $EditorStyles -Name "selectorHeight" -Default 72)
    $selectorTitleFontSize = [int](Get-OptionalProperty -InputObject $EditorStyles -Name "selectorTitleFontSize" -Default 10)
    $selectorValueFontSize = [int](Get-OptionalProperty -InputObject $EditorStyles -Name "selectorValueFontSize" -Default 14)
    $selectorHintFontSize = [int](Get-OptionalProperty -InputObject $EditorStyles -Name "selectorHintFontSize" -Default 8)
    $selectorPadding = [int](Get-OptionalProperty -InputObject $EditorStyles -Name "padding" -Default 12)
    New-McpPanel -Root $Root -Name $Name -ParentPath $ParentPath -Width 0 -Height $selectorHeight
    $selectorPath = "$ParentPath/$Name"
    Add-McpComponent -Root $Root -Path $selectorPath -ComponentType "GaragePartSelectorView"
    Add-McpLayoutElement -Root $Root -Path $selectorPath -PreferredHeight $selectorHeight
    Set-McpImageColor -Root $Root -Path $selectorPath -Color "#0E141DFF"

    New-McpText -Root $Root -Name "TitleText" -ParentPath $selectorPath -Text $Title -FontSize $selectorTitleFontSize -Color "#8FA4BBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$selectorPath/TitleText" -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0,1)" -AnchoredPosition "($selectorPadding,-10)" -SizeDelta "(-$($selectorPadding * 2),14)"

    New-McpButton -Root $Root -Name "PrevButton" -ParentPath $selectorPath -Text "<"
    Set-McpRectTransform -Root $Root -Path "$selectorPath/PrevButton" -AnchorMin "(0,0.5)" -AnchorMax "(0,0.5)" -Pivot "(0,0.5)" -AnchoredPosition "($selectorPadding,-4)" -SizeDelta "(30,28)"
    Set-McpImageColor -Root $Root -Path "$selectorPath/PrevButton" -Color "#1A2430FF"

    New-McpButton -Root $Root -Name "NextButton" -ParentPath $selectorPath -Text ">"
    Set-McpRectTransform -Root $Root -Path "$selectorPath/NextButton" -AnchorMin "(1,0.5)" -AnchorMax "(1,0.5)" -Pivot "(1,0.5)" -AnchoredPosition "(-$selectorPadding,-4)" -SizeDelta "(30,28)"
    Set-McpImageColor -Root $Root -Path "$selectorPath/NextButton" -Color "#1A2430FF"

    New-McpText -Root $Root -Name "ValueText" -ParentPath $selectorPath -Text "HV-42 레일건" -FontSize $selectorValueFontSize -Color "#F2F7FBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$selectorPath/ValueText" -AnchorMin "(0,0.5)" -AnchorMax "(1,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,-4)" -SizeDelta "(-84,22)"

    New-McpText -Root $Root -Name "HintText" -ParentPath $selectorPath -Text "좌우로 모듈 변경" -FontSize $selectorHintFontSize -Color "#8FA4BBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$selectorPath/HintText" -AnchorMin "(0,0)" -AnchorMax "(1,0)" -Pivot "(0,0)" -AnchoredPosition "($selectorPadding,8)" -SizeDelta "(-$($selectorPadding * 2),14)"

    Set-SceneComponentReference -Root $Root -GameObjectPath $selectorPath -ComponentType "GaragePartSelectorView" -PropertyName "_prevButton" -Value "$selectorPath/PrevButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $selectorPath -ComponentType "GaragePartSelectorView" -PropertyName "_nextButton" -Value "$selectorPath/NextButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $selectorPath -ComponentType "GaragePartSelectorView" -PropertyName "_titleText" -Value "$selectorPath/TitleText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $selectorPath -ComponentType "GaragePartSelectorView" -PropertyName "_valueText" -Value "$selectorPath/ValueText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $selectorPath -ComponentType "GaragePartSelectorView" -PropertyName "_hintText" -Value "$selectorPath/HintText"

    return $selectorPath
}

function New-GarageSurfaceRootContext {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RootName,
        [Parameter(Mandatory = $true)][object]$Styles
    )

    $rootStyles = Get-OptionalProperty -InputObject $Styles -Name "root" -Default $null
    $referenceWidth = [int](Get-OptionalProperty -InputObject $rootStyles -Name "referenceWidth" -Default 390)
    $referenceHeight = [int](Get-OptionalProperty -InputObject $rootStyles -Name "referenceHeight" -Default 844)

    Remove-McpGameObjectIfExists -Root $Root -Path "/Canvas"
    Remove-McpGameObjectIfExists -Root $Root -Path "/LobbyView"
    Remove-McpGameObjectIfExists -Root $Root -Path "/EventSystem"

    New-McpScratchCanvas -Root $Root -CanvasPath "/Canvas"
    Set-McpProperty -Root $Root -Path "/Canvas" -ComponentType "CanvasScaler" -PropertyName "m_UiScaleMode" -Value "1"
    Set-McpProperty -Root $Root -Path "/Canvas" -ComponentType "CanvasScaler" -PropertyName "m_ReferenceResolution" -Value "($referenceWidth,$referenceHeight)"
    Set-McpProperty -Root $Root -Path "/Canvas" -ComponentType "CanvasScaler" -PropertyName "m_ScreenMatchMode" -Value "0"
    Set-McpProperty -Root $Root -Path "/Canvas" -ComponentType "CanvasScaler" -PropertyName "m_MatchWidthOrHeight" -Value "1"

    $rootPath = "/Canvas/$RootName"
    New-McpPanel -Root $Root -Name $RootName -ParentPath "/Canvas" -Width $referenceWidth -Height $referenceHeight
    Add-McpComponent -Root $Root -Path $rootPath -ComponentType "CanvasGroup"
    Add-McpComponent -Root $Root -Path $rootPath -ComponentType "GaragePageController"
    Set-McpRectTransform -Root $Root -Path $rootPath -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
    Set-McpImageColor -Root $Root -Path $rootPath -Color ([string](Get-OptionalProperty -InputObject $rootStyles -Name "backgroundColor" -Default "#111827FF"))

    return [PSCustomObject]@{
        rootPath = $rootPath
        referenceWidth = $referenceWidth
        referenceHeight = $referenceHeight
    }
}

function New-GarageHeaderChromeBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RootPath,
        [Parameter(Mandatory = $true)][int]$ReferenceWidth,
        [Parameter(Mandatory = $true)][int]$ReferenceHeight,
        [Parameter(Mandatory = $true)][object]$HeaderStyles,
        [Parameter(Mandatory = $true)][object]$SourceSpec
    )

    $headerHeight = [int](Get-OptionalProperty -InputObject $HeaderStyles -Name "height" -Default 56)
    $headerPaddingX = [int](Get-OptionalProperty -InputObject $HeaderStyles -Name "paddingX" -Default 16)
    $headerLeadGap = [int](Get-OptionalProperty -InputObject $HeaderStyles -Name "contentGap" -Default 12)
    $headerBadgeSize = [int](Get-OptionalProperty -InputObject $HeaderStyles -Name "badgeSize" -Default 32)
    $headerTextX = $headerPaddingX + $headerBadgeSize + $headerLeadGap

    New-McpPanel -Root $Root -Name "GarageHeaderRow" -ParentPath $RootPath -Width 0 -Height $headerHeight
    $headerPath = "$RootPath/GarageHeaderRow"
    Add-McpLayoutElement -Root $Root -Path $headerPath -PreferredHeight $headerHeight
    Set-McpRectTransform -Root $Root -Path $headerPath -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,0)" -SizeDelta "(0,$headerHeight)"
    Set-McpImageColor -Root $Root -Path $headerPath -Color ([string](Get-OptionalProperty -InputObject $HeaderStyles -Name "backgroundColor" -Default "#09090BE6"))

    New-McpPanel -Root $Root -Name "HeaderBadgePlate" -ParentPath $headerPath -Width $headerBadgeSize -Height $headerBadgeSize
    Set-McpRectTransform -Root $Root -Path "$headerPath/HeaderBadgePlate" -AnchorMin "(0,0.5)" -AnchorMax "(0,0.5)" -Pivot "(0,0.5)" -AnchoredPosition "($headerPaddingX,0)" -SizeDelta "($headerBadgeSize,$headerBadgeSize)"
    Set-McpImageColor -Root $Root -Path "$headerPath/HeaderBadgePlate" -Color ([string](Get-OptionalProperty -InputObject $HeaderStyles -Name "badgeColor" -Default "#27272AFF"))
    New-McpText -Root $Root -Name "HeaderBadgeText" -ParentPath "$headerPath/HeaderBadgePlate" -Text "✦" -FontSize 15 -Color "#8FA4BBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$headerPath/HeaderBadgePlate/HeaderBadgeText" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(18,18)"

    New-McpText -Root $Root -Name "GarageHeaderTitleText" -ParentPath $headerPath -Text $SourceSpec.headerTitle -FontSize ([int](Get-OptionalProperty -InputObject $HeaderStyles -Name "titleFontSize" -Default 14)) -Color ([string](Get-OptionalProperty -InputObject $HeaderStyles -Name "titleColor" -Default "#F59E0BFF")) | Out-Null
    Set-McpRectTransform -Root $Root -Path "$headerPath/GarageHeaderTitleText" -AnchorMin "(0,1)" -AnchorMax "(0,1)" -Pivot "(0,1)" -AnchoredPosition "($headerTextX,-10)" -SizeDelta "(180,16)"
    New-McpText -Root $Root -Name "GarageHeaderSummaryText" -ParentPath $headerPath -Text $SourceSpec.headerStatus -FontSize ([int](Get-OptionalProperty -InputObject $HeaderStyles -Name "statusFontSize" -Default 10)) -Color ([string](Get-OptionalProperty -InputObject $HeaderStyles -Name "statusColor" -Default "#71717AFF")) | Out-Null
    Set-McpRectTransform -Root $Root -Path "$headerPath/GarageHeaderSummaryText" -AnchorMin "(0,0)" -AnchorMax "(0,0)" -Pivot "(0,0)" -AnchoredPosition "($headerTextX,10)" -SizeDelta "(220,12)"

    New-McpButton -Root $Root -Name "SettingsButton" -ParentPath $headerPath -Text "⚙"
    $settingsSize = [int](Get-OptionalProperty -InputObject $HeaderStyles -Name "settingsSize" -Default 32)
    Set-McpRectTransform -Root $Root -Path "$headerPath/SettingsButton" -AnchorMin "(1,0.5)" -AnchorMax "(1,0.5)" -Pivot "(1,0.5)" -AnchoredPosition "(-$headerPaddingX,0)" -SizeDelta "($settingsSize,$settingsSize)"
    Set-McpImageColor -Root $Root -Path "$headerPath/SettingsButton" -Color "#00000000"
    Set-McpTmpStyle -Root $Root -Path "$headerPath/SettingsButton/Text (TMP)" -Text "⚙" -FontSize ([int](Get-OptionalProperty -InputObject $HeaderStyles -Name "settingsFontSize" -Default 20)) -Color ([string](Get-OptionalProperty -InputObject $HeaderStyles -Name "settingsColor" -Default "#71717AFF"))

    New-McpPanel -Root $Root -Name "GarageSettingsOverlay" -ParentPath $RootPath -Width $ReferenceWidth -Height $ReferenceHeight
    $settingsOverlayPath = "$RootPath/GarageSettingsOverlay"
    Set-McpRectTransform -Root $Root -Path $settingsOverlayPath -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
    Set-McpImageColor -Root $Root -Path $settingsOverlayPath -Color "#081018D6"
    Set-McpActive -Root $Root -Path $settingsOverlayPath -Active $false

    $accountCardWidth = [int]([Math]::Round($ReferenceWidth * 0.82))
    $accountCardHeight = [int]([Math]::Round($ReferenceHeight * 0.28))
    New-McpPanel -Root $Root -Name "AccountCard" -ParentPath $settingsOverlayPath -Width $accountCardWidth -Height $accountCardHeight
    $accountCardPath = "$settingsOverlayPath/AccountCard"
    Set-McpRectTransform -Root $Root -Path $accountCardPath -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "($accountCardWidth,$accountCardHeight)"
    Set-McpImageColor -Root $Root -Path $accountCardPath -Color "#17212DFF"
    New-McpText -Root $Root -Name "AccountTitleText" -ParentPath $accountCardPath -Text "차고 설정" -FontSize 18 -Color "#F1F7FEFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$accountCardPath/AccountTitleText" -AnchorMin "(0.5,1)" -AnchorMax "(0.5,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,-22)" -SizeDelta "(240,28)"
    New-McpButton -Root $Root -Name "SettingsCloseButton" -ParentPath $accountCardPath -Text "닫기"
    Set-McpRectTransform -Root $Root -Path "$accountCardPath/SettingsCloseButton" -AnchorMin "(0.5,0)" -AnchorMax "(0.5,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,18)" -SizeDelta "(164,42)"
    Set-McpImageColor -Root $Root -Path "$accountCardPath/SettingsCloseButton" -Color "#2A3B4DFF"

    return [PSCustomObject]@{
        headerPath = $headerPath
        headerHeight = $headerHeight
        settingsOverlayPath = $settingsOverlayPath
        accountCardPath = $accountCardPath
    }
}

function New-GarageWorkspaceShellBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RootPath,
        [Parameter(Mandatory = $true)][int]$HeaderHeight,
        [Parameter(Mandatory = $true)][object]$RootStyles,
        [Parameter(Mandatory = $true)][object]$TabStyles,
        [Parameter(Mandatory = $true)][object]$SaveDockStyles,
        [Parameter(Mandatory = $true)][object]$SourceSpec
    )

    $saveDockHeight = [int]((Get-OptionalProperty -InputObject $SaveDockStyles -Name "paddingTop" -Default 8) + (Get-OptionalProperty -InputObject $SaveDockStyles -Name "buttonHeight" -Default 44) + (Get-OptionalProperty -InputObject $SaveDockStyles -Name "buttonMarginBottom" -Default 16))
    New-McpPanel -Root $Root -Name "GarageMobileStackRoot" -ParentPath $RootPath -Width 0 -Height 0
    $stackPath = "$RootPath/GarageMobileStackRoot"
    Set-McpRectTransform -Root $Root -Path $stackPath -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,$(([int]($saveDockHeight - $HeaderHeight)) / 2))" -SizeDelta "(0,-$($HeaderHeight + $saveDockHeight))"
    Set-McpImageColor -Root $Root -Path $stackPath -Color "#00000000"

    $tabBarHeight = [int]((Get-OptionalProperty -InputObject $TabStyles -Name "buttonHeight" -Default 28) + (Get-OptionalProperty -InputObject $TabStyles -Name "paddingBottom" -Default 8))
    New-McpPanel -Root $Root -Name "GarageMobileTabBar" -ParentPath $stackPath -Width 0 -Height $tabBarHeight
    $tabBarPath = "$stackPath/GarageMobileTabBar"
    Configure-McpHorizontalLayout -Root $Root -Path $tabBarPath -Spacing ([int](Get-OptionalProperty -InputObject $TabStyles -Name "gap" -Default 4)) -PaddingLeft ([int](Get-OptionalProperty -InputObject $RootStyles -Name "contentPaddingX" -Default 12)) -PaddingRight ([int](Get-OptionalProperty -InputObject $RootStyles -Name "contentPaddingX" -Default 12)) -PaddingTop 0 -PaddingBottom ([int](Get-OptionalProperty -InputObject $TabStyles -Name "paddingBottom" -Default 8)) -ControlWidth $true -ForceExpandWidth $true
    Set-McpRectTransform -Root $Root -Path $tabBarPath -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,0)" -SizeDelta "(0,$tabBarHeight)"
    Set-McpImageColor -Root $Root -Path $tabBarPath -Color "#00000000"

    New-McpPanel -Root $Root -Name "TabDivider" -ParentPath $tabBarPath -Width 0 -Height 1
    Set-McpRectTransform -Root $Root -Path "$tabBarPath/TabDivider" -AnchorMin "(0,0)" -AnchorMax "(1,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,0)" -SizeDelta "(0,1)"
    Set-McpImageColor -Root $Root -Path "$tabBarPath/TabDivider" -Color ([string](Get-OptionalProperty -InputObject $TabStyles -Name "borderColor" -Default "#27272AFF"))

    $tabSpecs = @(
        @{ Name = "MobileEditTabButton"; Label = $(if ($SourceSpec.tabs.Count -ge 1) { [string]$SourceSpec.tabs[0].label } else { "[프레임]" }); Active = $(if ($SourceSpec.tabs.Count -ge 1) { [bool]$SourceSpec.tabs[0].isActive } else { $false }) },
        @{ Name = "MobilePreviewTabButton"; Label = $(if ($SourceSpec.tabs.Count -ge 2) { [string]$SourceSpec.tabs[1].label } else { "[무장]" }); Active = $(if ($SourceSpec.tabs.Count -ge 2) { [bool]$SourceSpec.tabs[1].isActive } else { $true }) },
        @{ Name = "MobileSummaryTabButton"; Label = $(if ($SourceSpec.tabs.Count -ge 3) { [string]$SourceSpec.tabs[2].label } else { "[기동]" }); Active = $(if ($SourceSpec.tabs.Count -ge 3) { [bool]$SourceSpec.tabs[2].isActive } else { $false }) }
    )
    foreach ($tab in $tabSpecs) {
        New-McpButton -Root $Root -Name $tab.Name -ParentPath $tabBarPath -Text $tab.Label
        Set-McpRectTransform -Root $Root -Path "$tabBarPath/$($tab.Name)" -AnchorMin "(0,0.5)" -AnchorMax "(1,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,$([int](Get-OptionalProperty -InputObject $TabStyles -Name "buttonHeight" -Default 28)))"
        Set-McpImageColor -Root $Root -Path "$tabBarPath/$($tab.Name)" -Color ($(if ($tab.Active) { [string](Get-OptionalProperty -InputObject $TabStyles -Name "activeBackgroundColor" -Default "#5EB6FF1A") } else { "#00000000" }))
    }
    foreach ($tab in $tabSpecs) {
        Set-McpTmpStyle -Root $Root -Path "$tabBarPath/$($tab.Name)/Text (TMP)" -Text $tab.Label -FontSize ([int](Get-OptionalProperty -InputObject $TabStyles -Name "fontSize" -Default 12)) -Color ($(if ($tab.Active) { [string](Get-OptionalProperty -InputObject $TabStyles -Name "activeTextColor" -Default "#5EB6FFFF") } else { [string](Get-OptionalProperty -InputObject $TabStyles -Name "inactiveTextColor" -Default "#71717AFF") }))
    }

    New-McpPanel -Root $Root -Name "MobileBodyHost" -ParentPath $stackPath -Width 0 -Height 0
    $mobileBodyHostPath = "$stackPath/MobileBodyHost"
    Add-McpComponent -Root $Root -Path $mobileBodyHostPath -ComponentType "ScrollRect"
    Set-McpRectTransform -Root $Root -Path $mobileBodyHostPath -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,-$([int]($tabBarHeight / 2)))" -SizeDelta "(0,-$tabBarHeight)"
    Set-McpImageColor -Root $Root -Path $mobileBodyHostPath -Color "#00000000"

    New-McpPanel -Root $Root -Name "MobileBodyScrollContent" -ParentPath $mobileBodyHostPath -Width 0 -Height 0
    $scrollContentPath = "$mobileBodyHostPath/MobileBodyScrollContent"
    Configure-McpVerticalLayout -Root $Root -Path $scrollContentPath -Spacing ([int](Get-OptionalProperty -InputObject $RootStyles -Name "contentGap" -Default 16)) -PaddingLeft ([int](Get-OptionalProperty -InputObject $RootStyles -Name "contentPaddingX" -Default 12)) -PaddingRight ([int](Get-OptionalProperty -InputObject $RootStyles -Name "contentPaddingX" -Default 12)) -PaddingTop 0 -PaddingBottom ([int](Get-OptionalProperty -InputObject $RootStyles -Name "contentPaddingBottom" -Default 96))
    Add-McpComponent -Root $Root -Path $scrollContentPath -ComponentType "ContentSizeFitter"
    Set-McpProperty -Root $Root -Path $scrollContentPath -ComponentType "ContentSizeFitter" -PropertyName "m_VerticalFit" -Value "2"
    Set-McpProperty -Root $Root -Path $scrollContentPath -ComponentType "ContentSizeFitter" -PropertyName "m_HorizontalFit" -Value "0"
    Set-McpRectTransform -Root $Root -Path $scrollContentPath -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
    Set-McpProperty -Root $Root -Path $mobileBodyHostPath -ComponentType "ScrollRect" -PropertyName "m_Content" -Value $scrollContentPath
    Set-McpProperty -Root $Root -Path $mobileBodyHostPath -ComponentType "ScrollRect" -PropertyName "m_Horizontal" -Value "false"
    Set-McpProperty -Root $Root -Path $mobileBodyHostPath -ComponentType "ScrollRect" -PropertyName "m_Vertical" -Value "true"

    return [PSCustomObject]@{
        saveDockHeight = $saveDockHeight
        stackPath = $stackPath
        tabBarHeight = $tabBarHeight
        tabBarPath = $tabBarPath
        tabSpecs = $tabSpecs
        mobileBodyHostPath = $mobileBodyHostPath
        scrollContentPath = $scrollContentPath
    }
}

function New-GarageSlotSelectorBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$ParentPath,
        [Parameter(Mandatory = $true)][int]$ReferenceWidth,
        [object]$RootStyles = $null,
        [object]$SlotStyles = $null,
        [Parameter(Mandatory = $true)][object]$SourceSpec
    )

    $slotHeight = [int](Get-OptionalProperty -InputObject $SlotStyles -Name "itemHeight" -Default 82)
    $slotGap = [int](Get-OptionalProperty -InputObject $SlotStyles -Name "gridGap" -Default 8)
    $slotWidth = [int][Math]::Round(($ReferenceWidth - (2 * [int](Get-OptionalProperty -InputObject $RootStyles -Name "contentPaddingX" -Default 12)) - (3 * $slotGap)) / 4.0)
    if ($slotWidth -lt 72) {
        $slotWidth = 84
    }

    $SlotStyles | Add-Member -NotePropertyName "itemWidth" -NotePropertyValue $slotWidth -Force

    New-McpPanel -Root $Root -Name "RosterListPane" -ParentPath $ParentPath -Width 0 -Height $slotHeight
    $rosterPanePath = "$ParentPath/RosterListPane"
    Add-McpComponent -Root $Root -Path $rosterPanePath -ComponentType "GarageRosterListView"
    Add-McpComponent -Root $Root -Path $rosterPanePath -ComponentType "RectMask2D"
    Add-McpLayoutElement -Root $Root -Path $rosterPanePath -PreferredHeight $slotHeight
    Set-McpImageColor -Root $Root -Path $rosterPanePath -Color "#00000000"

    New-McpPanel -Root $Root -Name "SlotStripRow" -ParentPath $rosterPanePath -Width 0 -Height $slotHeight
    $slotStripPath = "$rosterPanePath/SlotStripRow"
    Configure-McpHorizontalLayout -Root $Root -Path $slotStripPath -Spacing $slotGap -PaddingLeft 0 -PaddingRight 0 -PaddingTop 0 -PaddingBottom 0 -ControlWidth $false -ForceExpandWidth $false
    Add-McpComponent -Root $Root -Path $slotStripPath -ComponentType "ContentSizeFitter"
    Add-McpLayoutElement -Root $Root -Path $slotStripPath -PreferredHeight $slotHeight
    Set-McpProperty -Root $Root -Path $slotStripPath -ComponentType "ContentSizeFitter" -PropertyName "m_HorizontalFit" -Value "2"
    Set-McpProperty -Root $Root -Path $slotStripPath -ComponentType "ContentSizeFitter" -PropertyName "m_VerticalFit" -Value "2"
    Set-McpRectTransform -Root $Root -Path $slotStripPath -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"

    $slotPaths = @()
    for ($slotIndex = 1; $slotIndex -le 6; $slotIndex++) {
        $slotSpec = if ($SourceSpec.slots.Count -ge $slotIndex) { $SourceSpec.slots[$slotIndex - 1] } else { $null }
        $slotPaths += New-GarageSlotItem -Root $Root -ParentPath $slotStripPath -SlotIndex $slotIndex -SlotSpec $slotSpec -SlotStyles $SlotStyles
    }

    Set-SceneArrayReference -Root $Root -GameObjectPath $rosterPanePath -ComponentType "GarageRosterListView" -ArrayName "_slotViews" -Values $slotPaths

    return [PSCustomObject]@{
        rosterPanePath = $rosterPanePath
        slotStripPath = $slotStripPath
        slotPaths = @($slotPaths)
    }
}

function New-GarageFocusedEditorBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$ParentPath,
        [Parameter(Mandatory = $true)][object]$EditorStyles,
        [Parameter(Mandatory = $true)][object]$SourceSpec,
        [Parameter(Mandatory = $true)][object[]]$TabSpecs
    )

    $editorComputedHeight = [int](Get-OptionalProperty -InputObject $EditorStyles -Name "computedHeight" -Default 452)
    New-McpPanel -Root $Root -Name "UnitEditorPane" -ParentPath $ParentPath -Width 0 -Height $editorComputedHeight
    $unitEditorPanePath = "$ParentPath/UnitEditorPane"
    Add-McpComponent -Root $Root -Path $unitEditorPanePath -ComponentType "GarageUnitEditorView"
    Configure-McpVerticalLayout -Root $Root -Path $unitEditorPanePath -Spacing ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "gap" -Default 16)) -PaddingLeft ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "padding" -Default 12)) -PaddingRight ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "padding" -Default 12)) -PaddingTop ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "padding" -Default 12)) -PaddingBottom ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "padding" -Default 12))
    Add-McpLayoutElement -Root $Root -Path $unitEditorPanePath -PreferredHeight $editorComputedHeight
    Set-McpImageColor -Root $Root -Path $unitEditorPanePath -Color ([string](Get-OptionalProperty -InputObject $EditorStyles -Name "backgroundColor" -Default "#18181BFF"))

    $editorBadgeHeight = [int]((Get-OptionalProperty -InputObject $EditorStyles -Name "badgeFontSize" -Default 9) + 4)
    New-McpPanel -Root $Root -Name "EditorBadge" -ParentPath $unitEditorPanePath -Width 52 -Height $editorBadgeHeight
    Set-McpImageColor -Root $Root -Path "$unitEditorPanePath/EditorBadge" -Color "#11304D66"
    New-McpText -Root $Root -Name "EditorBadgeText" -ParentPath "$unitEditorPanePath/EditorBadge" -Text $SourceSpec.editorBadge -FontSize ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "badgeFontSize" -Default 9)) -Color "#67B8FFFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/EditorBadge/EditorBadgeText" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(44,12)"
    New-McpText -Root $Root -Name "SelectionTitleText" -ParentPath $unitEditorPanePath -Text $SourceSpec.editorTitle -FontSize ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "titleFontSize" -Default 14)) -Color "#F2F7FBFF" | Out-Null
    New-McpText -Root $Root -Name "SelectionSubtitleText" -ParentPath $unitEditorPanePath -Text $SourceSpec.editorSubtitle -FontSize ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "subtitleFontSize" -Default 12)) -Color "#8FA4BBFF" | Out-Null
    $editorIconSize = [int](Get-OptionalProperty -InputObject $EditorStyles -Name "iconSize" -Default 48)
    New-McpPanel -Root $Root -Name "EditorIconPlate" -ParentPath $unitEditorPanePath -Width $editorIconSize -Height $editorIconSize
    Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/EditorIconPlate" -AnchorMin "(1,1)" -AnchorMax "(1,1)" -Pivot "(1,1)" -AnchoredPosition "(-12,-12)" -SizeDelta "($editorIconSize,$editorIconSize)"
    Set-McpImageColor -Root $Root -Path "$unitEditorPanePath/EditorIconPlate" -Color "#090D14FF"
    New-McpText -Root $Root -Name "EditorIconText" -ParentPath "$unitEditorPanePath/EditorIconPlate" -Text "◎" -FontSize 18 -Color "#6B7280FF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/EditorIconPlate/EditorIconText" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(20,20)"

    $derivedStats = @($SourceSpec.stats)
    if ($derivedStats.Count -lt 4) {
        $derivedStats = @(
            [PSCustomObject]@{ label = "공격력 (ATK)"; value = "840" },
            [PSCustomObject]@{ label = "사거리 (RNG)"; value = "12.5m" },
            [PSCustomObject]@{ label = "연사력 (RTS)"; value = "Low" },
            [PSCustomObject]@{ label = "전력 소모 (PWR)"; value = "45kW" }
        )
    }

    foreach ($stat in @(
        @{ Name = "StatAtk"; Label = [string]$derivedStats[0].label; Value = [string]$derivedStats[0].value; Accent = "#E5E7EBFF" },
        @{ Name = "StatRng"; Label = [string]$derivedStats[1].label; Value = [string]$derivedStats[1].value; Accent = "#E5E7EBFF" },
        @{ Name = "StatRts"; Label = [string]$derivedStats[2].label; Value = [string]$derivedStats[2].value; Accent = "#F59E0BFF" },
        @{ Name = "StatPwr"; Label = [string]$derivedStats[3].label; Value = [string]$derivedStats[3].value; Accent = "#E5E7EBFF" }
    )) {
        New-McpPanel -Root $Root -Name $stat.Name -ParentPath $unitEditorPanePath -Width 0 -Height ([int](Get-OptionalProperty -InputObject $EditorStyles -Name "statCardHeight" -Default 34))
        Set-McpImageColor -Root $Root -Path "$unitEditorPanePath/$($stat.Name)" -Color "#0B1017CC"
        New-McpText -Root $Root -Name "LabelText" -ParentPath "$unitEditorPanePath/$($stat.Name)" -Text $stat.Label -FontSize 7 -Color "#6B7280FF" | Out-Null
        Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/$($stat.Name)/LabelText" -AnchorMin "(0,0.5)" -AnchorMax "(0,0.5)" -Pivot "(0,0.5)" -AnchoredPosition "(8,0)" -SizeDelta "(84,12)"
        New-McpText -Root $Root -Name "ValueText" -ParentPath "$unitEditorPanePath/$($stat.Name)" -Text $stat.Value -FontSize 9 -Color $stat.Accent | Out-Null
        Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/$($stat.Name)/ValueText" -AnchorMin "(1,0.5)" -AnchorMax "(1,0.5)" -Pivot "(1,0.5)" -AnchoredPosition "(-8,0)" -SizeDelta "(44,12)"
    }

    Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/StatAtk" -AnchorMin "(0,1)" -AnchorMax "(0.5,1)" -Pivot "(0,1)" -AnchoredPosition "(0,-92)" -SizeDelta "(-6,34)"
    Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/StatRng" -AnchorMin "(0.5,1)" -AnchorMax "(1,1)" -Pivot "(0,1)" -AnchoredPosition "(6,-92)" -SizeDelta "(-6,34)"
    Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/StatRts" -AnchorMin "(0,1)" -AnchorMax "(0.5,1)" -Pivot "(0,1)" -AnchoredPosition "(0,-132)" -SizeDelta "(-6,34)"
    Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/StatPwr" -AnchorMin "(0.5,1)" -AnchorMax "(1,1)" -Pivot "(0,1)" -AnchoredPosition "(6,-132)" -SizeDelta "(-6,34)"

    $frameSelectorPath = New-GaragePartSelector -Root $Root -ParentPath $unitEditorPanePath -Name "FrameSelectorView" -Title "프레임" -EditorStyles $EditorStyles
    $firepowerSelectorPath = New-GaragePartSelector -Root $Root -ParentPath $unitEditorPanePath -Name "FirepowerSelectorView" -Title "무장" -EditorStyles $EditorStyles
    $mobilitySelectorPath = New-GaragePartSelector -Root $Root -ParentPath $unitEditorPanePath -Name "MobilitySelectorView" -Title "기동" -EditorStyles $EditorStyles
    New-McpButton -Root $Root -Name "ClearButton" -ParentPath $unitEditorPanePath -Text "초기화"
    Set-McpRectTransform -Root $Root -Path "$unitEditorPanePath/ClearButton" -AnchorMin "(1,0)" -AnchorMax "(1,0)" -Pivot "(1,0)" -AnchoredPosition "(-12,12)" -SizeDelta "(82,$([int](Get-OptionalProperty -InputObject $EditorStyles -Name "clearButtonHeight" -Default 26)))"
    Set-McpImageColor -Root $Root -Path "$unitEditorPanePath/ClearButton" -Color "#10151DFF"

    $activeTabLabel = @($TabSpecs | Where-Object { $_.Active } | Select-Object -First 1).Label
    Set-McpActive -Root $Root -Path $frameSelectorPath -Active ($activeTabLabel -like "*프레임*")
    Set-McpActive -Root $Root -Path $firepowerSelectorPath -Active ($activeTabLabel -like "*무장*")
    Set-McpActive -Root $Root -Path $mobilitySelectorPath -Active ($activeTabLabel -like "*기동*")

    Set-SceneComponentReference -Root $Root -GameObjectPath $unitEditorPanePath -ComponentType "GarageUnitEditorView" -PropertyName "_selectionTitleText" -Value "$unitEditorPanePath/SelectionTitleText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $unitEditorPanePath -ComponentType "GarageUnitEditorView" -PropertyName "_selectionSubtitleText" -Value "$unitEditorPanePath/SelectionSubtitleText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $unitEditorPanePath -ComponentType "GarageUnitEditorView" -PropertyName "_frameSelectorView" -Value $frameSelectorPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $unitEditorPanePath -ComponentType "GarageUnitEditorView" -PropertyName "_firepowerSelectorView" -Value $firepowerSelectorPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $unitEditorPanePath -ComponentType "GarageUnitEditorView" -PropertyName "_mobilitySelectorView" -Value $mobilitySelectorPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $unitEditorPanePath -ComponentType "GarageUnitEditorView" -PropertyName "_clearButton" -Value "$unitEditorPanePath/ClearButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $unitEditorPanePath -ComponentType "GarageUnitEditorView" -PropertyName "_clearButtonText" -Value "$unitEditorPanePath/ClearButton/Text (TMP)"

    return [PSCustomObject]@{
        unitEditorPanePath = $unitEditorPanePath
        frameSelectorPath = $frameSelectorPath
        firepowerSelectorPath = $firepowerSelectorPath
        mobilitySelectorPath = $mobilitySelectorPath
    }
}

function New-GaragePreviewCardBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$ParentPath,
        [Parameter(Mandatory = $true)][object]$PreviewStyles,
        [Parameter(Mandatory = $true)][object]$SourceSpec
    )

    New-McpPanel -Root $Root -Name "RightRailRoot" -ParentPath $ParentPath -Width 0 -Height 1
    $rightRailRootPath = "$ParentPath/RightRailRoot"
    Set-McpImageColor -Root $Root -Path $rightRailRootPath -Color "#00000000"

    $previewHeight = [int](Get-OptionalProperty -InputObject $PreviewStyles -Name "height" -Default 192)
    $previewPadding = [int](Get-OptionalProperty -InputObject $PreviewStyles -Name "padding" -Default 12)
    New-McpPanel -Root $Root -Name "PreviewCard" -ParentPath $ParentPath -Width 0 -Height $previewHeight
    $previewCardPath = "$ParentPath/PreviewCard"
    Add-McpComponent -Root $Root -Path $previewCardPath -ComponentType "GarageUnitPreviewView"
    Add-McpLayoutElement -Root $Root -Path $previewCardPath -PreferredHeight $previewHeight
    Set-McpImageColor -Root $Root -Path $previewCardPath -Color ([string](Get-OptionalProperty -InputObject $PreviewStyles -Name "backgroundColor" -Default "#18181BFF"))

    New-McpPanel -Root $Root -Name "BlueprintGridOverlay" -ParentPath $previewCardPath -Width 0 -Height 0
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/BlueprintGridOverlay" -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
    Set-McpImageColor -Root $Root -Path "$previewCardPath/BlueprintGridOverlay" -Color "#0F1E2A66"
    New-McpText -Root $Root -Name "PreviewHeaderText" -ParentPath $previewCardPath -Text $SourceSpec.previewHeader -FontSize ([int](Get-OptionalProperty -InputObject $PreviewStyles -Name "headerFontSize" -Default 10)) -Color ([string](Get-OptionalProperty -InputObject $PreviewStyles -Name "headerColor" -Default "#71717AFF")) | Out-Null
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/PreviewHeaderText" -AnchorMin "(0,1)" -AnchorMax "(0,1)" -Pivot "(0,1)" -AnchoredPosition "($previewPadding,-$previewPadding)" -SizeDelta "(112,14)"
    $previewTagOne = if ($SourceSpec.previewTags.Count -ge 1) { [string]$SourceSpec.previewTags[0] } else { "대장갑" }
    $previewTagTwo = if ($SourceSpec.previewTags.Count -ge 2) { [string]$SourceSpec.previewTags[1] } else { "장거리" }
    New-McpText -Root $Root -Name "PreviewTagOne" -ParentPath $previewCardPath -Text $previewTagOne -FontSize ([int](Get-OptionalProperty -InputObject $PreviewStyles -Name "tagFontSize" -Default 8)) -Color ([string](Get-OptionalProperty -InputObject $PreviewStyles -Name "tagTextColor" -Default "#D4D4D8FF")) | Out-Null
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/PreviewTagOne" -AnchorMin "(1,1)" -AnchorMax "(1,1)" -Pivot "(1,1)" -AnchoredPosition "(-56,-$previewPadding)" -SizeDelta "(36,12)"
    New-McpText -Root $Root -Name "PreviewTagTwo" -ParentPath $previewCardPath -Text $previewTagTwo -FontSize ([int](Get-OptionalProperty -InputObject $PreviewStyles -Name "tagFontSize" -Default 8)) -Color ([string](Get-OptionalProperty -InputObject $PreviewStyles -Name "tagTextColor" -Default "#D4D4D8FF")) | Out-Null
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/PreviewTagTwo" -AnchorMin "(1,1)" -AnchorMax "(1,1)" -Pivot "(1,1)" -AnchoredPosition "(-$previewPadding,-$previewPadding)" -SizeDelta "(36,12)"
    New-McpRawImage -Root $Root -Name "PreviewRawImage" -ParentPath $previewCardPath -Width 140 -Height 110
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/PreviewRawImage" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,8)" -SizeDelta "(140,96)"
    Set-McpRawImageColor -Root $Root -Path "$previewCardPath/PreviewRawImage" -Color "#00000000"
    $ringSize = [int](Get-OptionalProperty -InputObject $PreviewStyles -Name "ringSize" -Default 96)
    New-McpPanel -Root $Root -Name "TargetRing" -ParentPath $previewCardPath -Width $ringSize -Height $ringSize
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/TargetRing" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,8)" -SizeDelta "($ringSize,$ringSize)"
    Set-McpImageColor -Root $Root -Path "$previewCardPath/TargetRing" -Color "#10192500"
    New-McpText -Root $Root -Name "TargetGlyphText" -ParentPath "$previewCardPath/TargetRing" -Text "◉" -FontSize ([int](Get-OptionalProperty -InputObject $PreviewStyles -Name "glyphFontSize" -Default 36)) -Color ([string](Get-OptionalProperty -InputObject $PreviewStyles -Name "glyphColor" -Default "#60A5FAFF")) | Out-Null
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/TargetRing/TargetGlyphText" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(32,32)"
    New-McpText -Root $Root -Name "EmptyStateText" -ParentPath $previewCardPath -Text "" -FontSize 14 -Color "#8FA4BBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/EmptyStateText" -AnchorMin "(0.5,0)" -AnchorMax "(0.5,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,$($previewPadding + 6))" -SizeDelta "(220,20)"
    $powerTrackWrapPadding = [int](Get-OptionalProperty -InputObject $PreviewStyles -Name "powerTrackPadding" -Default 4)
    $powerTrackHeight = [int](Get-OptionalProperty -InputObject $PreviewStyles -Name "powerTrackHeight" -Default 6)
    $powerWrapHeight = $powerTrackHeight + ($powerTrackWrapPadding * 2)
    New-McpPanel -Root $Root -Name "PowerBarTrack" -ParentPath $previewCardPath -Width 0 -Height $powerWrapHeight
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/PowerBarTrack" -AnchorMin "(0,0)" -AnchorMax "(1,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,$previewPadding)" -SizeDelta "(-$($previewPadding * 2),$powerWrapHeight)"
    Set-McpImageColor -Root $Root -Path "$previewCardPath/PowerBarTrack" -Color ([string](Get-OptionalProperty -InputObject $PreviewStyles -Name "powerTrackWrapColor" -Default "#09090BFF"))
    New-McpPanel -Root $Root -Name "PowerBarFill" -ParentPath "$previewCardPath/PowerBarTrack" -Width 0 -Height 0
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/PowerBarTrack/PowerBarFill" -AnchorMin "(0,0)" -AnchorMax "(0.7,1)" -Pivot "(0,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
    Set-McpImageColor -Root $Root -Path "$previewCardPath/PowerBarTrack/PowerBarFill" -Color ([string](Get-OptionalProperty -InputObject $PreviewStyles -Name "powerFillColor" -Default "#5EB6FFFF"))
    New-McpText -Root $Root -Name "PowerLabelText" -ParentPath $previewCardPath -Text ([string]$SourceSpec.powerLabel) -FontSize ([int](Get-OptionalProperty -InputObject $PreviewStyles -Name "powerLabelFontSize" -Default 8)) -Color ([string](Get-OptionalProperty -InputObject $PreviewStyles -Name "powerLabelColor" -Default "#5EB6FFFF")) | Out-Null
    Set-McpRectTransform -Root $Root -Path "$previewCardPath/PowerLabelText" -AnchorMin "(1,0)" -AnchorMax "(1,0)" -Pivot "(1,0)" -AnchoredPosition "(-$previewPadding,$($previewPadding + $powerWrapHeight + 4))" -SizeDelta "(52,10)"
    New-McpGameObject -Root $Root -Name "PreviewCamera" -ParentPath $previewCardPath -Components @("UnityEngine.Camera")
    Set-McpTransformLocal -Root $Root -Path "$previewCardPath/PreviewCamera" -LocalPosition "(0,0,-6)" -LocalScale "(1,1,1)"
    New-McpPrimitive -Root $Root -Name "FrameTemplate" -PrimitiveType "Cube" -ParentPath $previewCardPath
    New-McpPrimitive -Root $Root -Name "WeaponTemplate" -PrimitiveType "Cylinder" -ParentPath $previewCardPath
    New-McpPrimitive -Root $Root -Name "ThrusterTemplate" -PrimitiveType "Capsule" -ParentPath $previewCardPath
    Set-McpActive -Root $Root -Path "$previewCardPath/FrameTemplate" -Active $false
    Set-McpActive -Root $Root -Path "$previewCardPath/WeaponTemplate" -Active $false
    Set-McpActive -Root $Root -Path "$previewCardPath/ThrusterTemplate" -Active $false

    Set-SceneComponentReference -Root $Root -GameObjectPath $previewCardPath -ComponentType "GarageUnitPreviewView" -PropertyName "_previewCamera" -Value "$previewCardPath/PreviewCamera"
    Set-SceneComponentReference -Root $Root -GameObjectPath $previewCardPath -ComponentType "GarageUnitPreviewView" -PropertyName "_rawImage" -Value "$previewCardPath/PreviewRawImage"
    Set-SceneComponentReference -Root $Root -GameObjectPath $previewCardPath -ComponentType "GarageUnitPreviewView" -PropertyName "_emptyStateText" -Value "$previewCardPath/EmptyStateText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $previewCardPath -ComponentType "GarageUnitPreviewView" -PropertyName "_framePrefab" -Value "$previewCardPath/FrameTemplate"
    Set-SceneComponentReference -Root $Root -GameObjectPath $previewCardPath -ComponentType "GarageUnitPreviewView" -PropertyName "_weaponPrefab" -Value "$previewCardPath/WeaponTemplate"
    Set-SceneComponentReference -Root $Root -GameObjectPath $previewCardPath -ComponentType "GarageUnitPreviewView" -PropertyName "_thrusterPrefab" -Value "$previewCardPath/ThrusterTemplate"

    return [PSCustomObject]@{
        previewCardPath = $previewCardPath
        rightRailRootPath = $rightRailRootPath
    }
}

function New-GarageSummaryCardBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$ParentPath,
        [Parameter(Mandatory = $true)][int]$ReferenceWidth,
        [Parameter(Mandatory = $true)][object]$ResultStyles,
        [Parameter(Mandatory = $true)][object]$SaveDockStyles
    )

    $resultHeight = [int](Get-OptionalProperty -InputObject $ResultStyles -Name "computedHeight" -Default 248)
    New-McpPanel -Root $Root -Name "ResultPane" -ParentPath $ParentPath -Width 0 -Height $resultHeight
    $resultPanePath = "$ParentPath/ResultPane"
    Add-McpComponent -Root $Root -Path $resultPanePath -ComponentType "GarageResultPanelView"
    Add-McpLayoutElement -Root $Root -Path $resultPanePath -PreferredHeight $resultHeight
    Set-McpImageColor -Root $Root -Path $resultPanePath -Color "#151F2AFF"
    $resultPadding = [int](Get-OptionalProperty -InputObject $ResultStyles -Name "padding" -Default 12)
    $resultTitleFontSize = [int](Get-OptionalProperty -InputObject $ResultStyles -Name "titleFontSize" -Default 14)
    $resultBodyFontSize = [int](Get-OptionalProperty -InputObject $ResultStyles -Name "bodyFontSize" -Default 12)
    $resultGap = [int](Get-OptionalProperty -InputObject $ResultStyles -Name "gap" -Default 8)
    New-McpText -Root $Root -Name "RosterStatusText" -ParentPath $resultPanePath -Text "활성 0/6" -FontSize $resultTitleFontSize -Color "#F2F7FBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$resultPanePath/RosterStatusText" -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0,1)" -AnchoredPosition "($resultPadding,-$resultPadding)" -SizeDelta "(-$($resultPadding * 2),24)"
    New-McpText -Root $Root -Name "ValidationText" -ParentPath $resultPanePath -Text "조립을 시작하면 저장 조건이 표시됩니다" -FontSize $resultBodyFontSize -Color "#8FA4BBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$resultPanePath/ValidationText" -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0,1)" -AnchoredPosition "($resultPadding,-$($resultPadding + $resultTitleFontSize + $resultGap))" -SizeDelta "(-$($resultPadding * 2),24)"
    New-McpText -Root $Root -Name "StatsText" -ParentPath $resultPanePath -Text "공격 | 사거리 | 내구 | 기동" -FontSize $resultBodyFontSize -Color "#8FA4BBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$resultPanePath/StatsText" -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0,1)" -AnchoredPosition "($resultPadding,-$($resultPadding + $resultTitleFontSize + ($resultGap * 2) + $resultBodyFontSize))" -SizeDelta "(-$($resultPadding * 2),24)"
    New-McpButton -Root $Root -Name "InlineSaveButton" -ParentPath $resultPanePath -Text "편성 저장"
    Set-McpRectTransform -Root $Root -Path "$resultPanePath/InlineSaveButton" -AnchorMin "(1,0)" -AnchorMax "(1,0)" -Pivot "(1,0)" -AnchoredPosition "(-$resultPadding,$resultPadding)" -SizeDelta "(132,$([int](Get-OptionalProperty -InputObject $SaveDockStyles -Name "buttonHeight" -Default 44)))"
    Set-McpImageColor -Root $Root -Path "$resultPanePath/InlineSaveButton" -Color "#D56A2BFF"
    $toastHeight = [int](Get-OptionalProperty -InputObject $ResultStyles -Name "toastHeight" -Default 36)
    New-McpPanel -Root $Root -Name "ToastPanel" -ParentPath $resultPanePath -Width ($ReferenceWidth - ($resultPadding * 4)) -Height $toastHeight
    Set-McpRectTransform -Root $Root -Path "$resultPanePath/ToastPanel" -AnchorMin "(0.5,0)" -AnchorMax "(0.5,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,$resultPadding)" -SizeDelta "(240,$toastHeight)"
    Add-McpComponent -Root $Root -Path "$resultPanePath/ToastPanel" -ComponentType "CanvasGroup"
    New-McpText -Root $Root -Name "ToastText" -ParentPath "$resultPanePath/ToastPanel" -Text "" -FontSize $resultBodyFontSize -Color "#F2F7FBFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$resultPanePath/ToastPanel/ToastText" -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(-16,-8)"
    New-McpPanel -Root $Root -Name "LoadingIndicator" -ParentPath $resultPanePath -Width 24 -Height 24
    Set-McpRectTransform -Root $Root -Path "$resultPanePath/LoadingIndicator" -AnchorMin "(0,0)" -AnchorMax "(0,0)" -Pivot "(0,0)" -AnchoredPosition "(16,16)" -SizeDelta "(24,24)"
    Set-McpImageColor -Root $Root -Path "$resultPanePath/LoadingIndicator" -Color "#6CAEFF"
    Set-McpActive -Root $Root -Path "$resultPanePath/LoadingIndicator" -Active $false

    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_rosterStatusText" -Value "$resultPanePath/RosterStatusText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_validationText" -Value "$resultPanePath/ValidationText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_statsText" -Value "$resultPanePath/StatsText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_saveButton" -Value "$resultPanePath/InlineSaveButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_saveButtonText" -Value "$resultPanePath/InlineSaveButton/Text (TMP)"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_saveButtonImage" -Value "$resultPanePath/InlineSaveButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_toastPanel" -Value "$resultPanePath/ToastPanel"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_toastCanvasGroup" -Value "$resultPanePath/ToastPanel"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_toastText" -Value "$resultPanePath/ToastPanel/ToastText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $resultPanePath -ComponentType "GarageResultPanelView" -PropertyName "_loadingIndicator" -Value "$resultPanePath/LoadingIndicator"

    return [PSCustomObject]@{
        resultPanePath = $resultPanePath
    }
}

function New-GarageSaveDockBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RootPath,
        [Parameter(Mandatory = $true)][int]$SaveDockHeight,
        [Parameter(Mandatory = $true)][object]$SaveDockStyles,
        [Parameter(Mandatory = $true)][object]$SourceSpec,
        [Parameter(Mandatory = $true)][object]$Manifest
    )

    New-McpPanel -Root $Root -Name "MobileSaveDock" -ParentPath $RootPath -Width 0 -Height $SaveDockHeight
    $saveDockPath = "$RootPath/MobileSaveDock"
    Set-McpRectTransform -Root $Root -Path $saveDockPath -AnchorMin "(0,0)" -AnchorMax "(1,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,0)" -SizeDelta "(0,$SaveDockHeight)"
    Set-McpImageColor -Root $Root -Path $saveDockPath -Color ([string](Get-OptionalProperty -InputObject $SaveDockStyles -Name "backgroundColor" -Default "#09090BF2"))

    New-McpButton -Root $Root -Name "MobileSaveButton" -ParentPath $saveDockPath -Text "저장 및 배치"
    $saveButtonPath = "$saveDockPath/MobileSaveButton"
    $saveDockPaddingX = [int](Get-OptionalProperty -InputObject $SaveDockStyles -Name "paddingX" -Default 16)
    $saveDockPaddingTop = [int](Get-OptionalProperty -InputObject $SaveDockStyles -Name "paddingTop" -Default 8)
    $saveButtonMarginBottom = [int](Get-OptionalProperty -InputObject $SaveDockStyles -Name "buttonMarginBottom" -Default 16)
    Set-McpRectTransform -Root $Root -Path $saveButtonPath -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,$(([int]($saveButtonMarginBottom - $saveDockPaddingTop)) / 2))" -SizeDelta "(-$($saveDockPaddingX * 2),-$($saveDockPaddingTop + $saveButtonMarginBottom))"
    Set-McpImageColor -Root $Root -Path $saveButtonPath -Color ([string](Get-OptionalProperty -InputObject $SaveDockStyles -Name "buttonBackgroundColor" -Default "#F59E0BFF"))
    $saveLabel = if (-not [string]::IsNullOrWhiteSpace([string]$SourceSpec.saveLabel)) { [string]$SourceSpec.saveLabel } else { Get-ManifestCtaLabel -Manifest $Manifest -CtaId "save-roster" }
    if (-not [string]::IsNullOrWhiteSpace($saveLabel)) {
        Set-McpTmpStyle -Root $Root -Path "$saveButtonPath/Text (TMP)" -Text $saveLabel -FontSize ([int](Get-OptionalProperty -InputObject $SaveDockStyles -Name "buttonFontSize" -Default 14)) -Color ([string](Get-OptionalProperty -InputObject $SaveDockStyles -Name "buttonTextColor" -Default "#09090BFF"))
    }

    New-McpText -Root $Root -Name "MobileSaveStatusText" -ParentPath $saveDockPath -Text "현재 조합을 저장해 출격 편성에 반영" -FontSize 14 -Color "#FFB36FFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$saveDockPath/MobileSaveStatusText" -AnchorMin "(0,1)" -AnchorMax "(1,1)" -Pivot "(0,1)" -AnchoredPosition "(16,-8)" -SizeDelta "(-32,18)"
    Set-McpActive -Root $Root -Path "$saveDockPath/MobileSaveStatusText" -Active $false

    return [PSCustomObject]@{
        saveDockPath = $saveDockPath
        saveButtonPath = $saveButtonPath
    }
}

function Connect-GaragePageControllerReferences {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$RootPath,
        [Parameter(Mandatory = $true)][string]$RosterPanePath,
        [Parameter(Mandatory = $true)][string]$UnitEditorPanePath,
        [Parameter(Mandatory = $true)][string]$ResultPanePath,
        [Parameter(Mandatory = $true)][string]$PreviewCardPath,
        [Parameter(Mandatory = $true)][string]$StackPath,
        [Parameter(Mandatory = $true)][string]$MobileBodyHostPath,
        [Parameter(Mandatory = $true)][string]$RightRailRootPath,
        [Parameter(Mandatory = $true)][string]$TabBarPath,
        [Parameter(Mandatory = $true)][string]$HeaderPath,
        [Parameter(Mandatory = $true)][string]$SettingsOverlayPath,
        [Parameter(Mandatory = $true)][string]$AccountCardPath,
        [Parameter(Mandatory = $true)][string]$SaveDockPath,
        [Parameter(Mandatory = $true)][string]$SaveButtonPath
    )

    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_rosterListView" -Value $RosterPanePath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_unitEditorView" -Value $UnitEditorPanePath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_resultPanelView" -Value $ResultPanePath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_unitPreviewView" -Value $PreviewCardPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileContentRoot" -Value $StackPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileBodyHost" -Value $MobileBodyHostPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileSlotHost" -Value $RosterPanePath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_rightRailRoot" -Value $RightRailRootPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_previewCard" -Value $PreviewCardPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_resultPane" -Value $ResultPanePath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileTabBar" -Value $TabBarPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileEditTabButton" -Value "$TabBarPath/MobileEditTabButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileEditTabLabel" -Value "$TabBarPath/MobileEditTabButton/Text (TMP)"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobilePreviewTabButton" -Value "$TabBarPath/MobilePreviewTabButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobilePreviewTabLabel" -Value "$TabBarPath/MobilePreviewTabButton/Text (TMP)"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileSummaryTabButton" -Value "$TabBarPath/MobileSummaryTabButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileSummaryTabLabel" -Value "$TabBarPath/MobileSummaryTabButton/Text (TMP)"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_garageHeaderSummaryText" -Value "$HeaderPath/GarageHeaderSummaryText"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_settingsOpenButton" -Value "$HeaderPath/SettingsButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_settingsOpenButtonLabel" -Value "$HeaderPath/SettingsButton/Text (TMP)"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_settingsOverlayRoot" -Value $SettingsOverlayPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_settingsCloseButton" -Value "$AccountCardPath/SettingsCloseButton"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_settingsCloseButtonLabel" -Value "$AccountCardPath/SettingsCloseButton/Text (TMP)"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileSaveDockRoot" -Value $SaveDockPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileSaveButton" -Value $SaveButtonPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileSaveButtonLabel" -Value "$SaveButtonPath/Text (TMP)"
    Set-SceneComponentReference -Root $Root -GameObjectPath $RootPath -ComponentType "GaragePageController" -PropertyName "_mobileSaveStateText" -Value "$SaveDockPath/MobileSaveStatusText"
}

function Build-GaragePageRootFromContract {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Manifest
    )

    $targets = Get-RequiredProperty -InputObject $Manifest -Name "targets"
    $sourceSpec = Get-GarageStitchDerivedSpec -Manifest $Manifest
    $styles = Get-OptionalProperty -InputObject $sourceSpec -Name "styles" -Default $null
    $rootStyles = Get-OptionalProperty -InputObject $styles -Name "root" -Default $null
    $headerStyles = Get-OptionalProperty -InputObject $styles -Name "header" -Default $null
    $tabStyles = Get-OptionalProperty -InputObject $styles -Name "tabs" -Default $null
    $slotStyles = Get-OptionalProperty -InputObject $styles -Name "slots" -Default $null
    $editorStyles = Get-OptionalProperty -InputObject $styles -Name "editor" -Default $null
    $previewStyles = Get-OptionalProperty -InputObject $styles -Name "preview" -Default $null
    $resultStyles = Get-OptionalProperty -InputObject $styles -Name "result" -Default $null
    $saveDockStyles = Get-OptionalProperty -InputObject $styles -Name "saveDock" -Default $null
    $prefabPath = [string](Get-RequiredProperty -InputObject $targets -Name "prefabPath")
    $sceneRoot = [string](@(Get-RequiredProperty -InputObject $targets -Name "sceneRoots")[0])
    $rootName = (($sceneRoot -split "/") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 1)
    $activeSemanticBlocks = @(Get-ManifestSemanticBlockIds -Manifest $Manifest)
    $rootContext = New-GarageSurfaceRootContext -Root $Root -RootName $rootName -Styles $styles
    $rootPath = [string]$rootContext.rootPath
    $referenceWidth = [int]$rootContext.referenceWidth
    $referenceHeight = [int]$rootContext.referenceHeight

    $headerChrome = New-GarageHeaderChromeBlock -Root $Root -RootPath $rootPath -ReferenceWidth $referenceWidth -ReferenceHeight $referenceHeight -HeaderStyles $headerStyles -SourceSpec $sourceSpec
    $headerPath = [string]$headerChrome.headerPath
    $headerHeight = [int]$headerChrome.headerHeight
    $settingsOverlayPath = [string]$headerChrome.settingsOverlayPath
    $accountCardPath = [string]$headerChrome.accountCardPath

    $workspaceShell = New-GarageWorkspaceShellBlock -Root $Root -RootPath $rootPath -HeaderHeight $headerHeight -RootStyles $rootStyles -TabStyles $tabStyles -SaveDockStyles $saveDockStyles -SourceSpec $sourceSpec
    $saveDockHeight = [int]$workspaceShell.saveDockHeight
    $stackPath = [string]$workspaceShell.stackPath
    $tabBarPath = [string]$workspaceShell.tabBarPath
    $tabSpecs = @($workspaceShell.tabSpecs)
    $mobileBodyHostPath = [string]$workspaceShell.mobileBodyHostPath
    $scrollContentPath = [string]$workspaceShell.scrollContentPath

    $slotSelectorBlock = New-GarageSlotSelectorBlock -Root $Root -ParentPath $scrollContentPath -ReferenceWidth $referenceWidth -RootStyles $rootStyles -SlotStyles $slotStyles -SourceSpec $sourceSpec
    $rosterPanePath = [string]$slotSelectorBlock.rosterPanePath

    $focusedEditorBlock = New-GarageFocusedEditorBlock -Root $Root -ParentPath $scrollContentPath -EditorStyles $editorStyles -SourceSpec $sourceSpec -TabSpecs $tabSpecs
    $unitEditorPanePath = [string]$focusedEditorBlock.unitEditorPanePath

    $previewBlock = New-GaragePreviewCardBlock -Root $Root -ParentPath $scrollContentPath -PreviewStyles $previewStyles -SourceSpec $sourceSpec
    $previewCardPath = [string]$previewBlock.previewCardPath
    $rightRailRootPath = [string]$previewBlock.rightRailRootPath

    $summaryBlock = New-GarageSummaryCardBlock -Root $Root -ParentPath $scrollContentPath -ReferenceWidth $referenceWidth -ResultStyles $resultStyles -SaveDockStyles $saveDockStyles
    $resultPanePath = [string]$summaryBlock.resultPanePath

    $saveDockBlock = New-GarageSaveDockBlock -Root $Root -RootPath $rootPath -SaveDockHeight $saveDockHeight -SaveDockStyles $saveDockStyles -SourceSpec $sourceSpec -Manifest $Manifest
    $saveDockPath = [string]$saveDockBlock.saveDockPath
    $saveButtonPath = [string]$saveDockBlock.saveButtonPath

    Connect-GaragePageControllerReferences -Root $Root -RootPath $rootPath -RosterPanePath $rosterPanePath -UnitEditorPanePath $unitEditorPanePath -ResultPanePath $resultPanePath -PreviewCardPath $previewCardPath -StackPath $stackPath -MobileBodyHostPath $mobileBodyHostPath -RightRailRootPath $rightRailRootPath -TabBarPath $tabBarPath -HeaderPath $headerPath -SettingsOverlayPath $settingsOverlayPath -AccountCardPath $accountCardPath -SaveDockPath $saveDockPath -SaveButtonPath $saveButtonPath

    Save-McpPrefabAsset -Root $Root -ScenePath $rootPath -SavePath $prefabPath

    $verifiedChildPaths = @(Get-GarageVerifiedChildPaths -Manifest $Manifest)

    return [PSCustomObject]@{
        prefabPath = $prefabPath
        sceneRoot = $sceneRoot
        scratchRootPath = $rootPath
        activeSemanticBlocks = $activeSemanticBlocks
        verifiedChildPaths = $verifiedChildPaths
    }
}

function Get-SurfacePrefabChecks {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$BuildDefinition,
        [Parameter(Mandatory = $true)][object]$Manifest,
        [Parameter(Mandatory = $true)][string]$PrefabPath
    )

    $prefabCheckDefinitionFunctionName = [string]$BuildDefinition.prefabCheckDefinitionFunction
    $prefabCheckDefinitionFunction = Get-Command -Name $prefabCheckDefinitionFunctionName -CommandType Function -ErrorAction Stop
    $definitions = @(& $prefabCheckDefinitionFunction -Manifest $Manifest)

    $checks = [ordered]@{}
    foreach ($definition in $definitions) {
        $node = Get-McpPrefabNode -Root $Root -AssetPath $PrefabPath -ChildPath ([string]$definition.ChildPath)
        $checks[[string]$definition.Name] = ($null -ne $node)
    }

    return [PSCustomObject]$checks
}

function Get-SurfaceAppliedContract {
    param([Parameter(Mandatory = $true)][object]$Manifest)

    $ctaLabels = [ordered]@{}
    foreach ($cta in @($Manifest.ctaPriority)) {
        $ctaId = [string](Get-OptionalProperty -InputObject $cta -Name "id" -Default "")
        $ctaLabel = [string](Get-OptionalProperty -InputObject $cta -Name "label" -Default "")
        if (-not [string]::IsNullOrWhiteSpace($ctaId)) {
            $ctaLabels[$ctaId] = $ctaLabel
        }
    }

    return [PSCustomObject]@{
        ctaLabels = [PSCustomObject]$ctaLabels
        semanticBlocks = @(Get-ManifestSemanticBlockIds -Manifest $Manifest)
        canonicalBlocks = @(Get-ManifestCanonicalBlockIds -Manifest $Manifest)
    }
}

if (-not (Test-Path -LiteralPath $ScreenManifestPath)) {
    throw "Manifest not found: $ScreenManifestPath"
}

$resolvedManifestPath = (Resolve-Path -LiteralPath $ScreenManifestPath).Path
$manifest = Get-Content -Path $resolvedManifestPath -Raw | ConvertFrom-Json
$surfaceId = [string](Get-RequiredProperty -InputObject $manifest -Name "surfaceId")

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 30
$compile = Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs 120000

if (-not (Test-McpResponseSuccess -Response $compile.Wait)) {
    throw "Unity compile wait failed before surface generation."
}

Open-TempScene -Root $root
$buildDefinition = Resolve-SurfaceBuildDefinition -Manifest $manifest
$translation = Invoke-SurfaceBuildDefinition -Root $root -Manifest $manifest -Definition $buildDefinition

$prefabChecks = Get-SurfacePrefabChecks -Root $root -BuildDefinition $buildDefinition -Manifest $manifest -PrefabPath $translation.prefabPath
$appliedContract = Get-SurfaceAppliedContract -Manifest $manifest

$result = [PSCustomObject]@{
    success = $true
    manifestPath = $resolvedManifestPath
    surfaceId = $surfaceId
    builderId = [string]$buildDefinition.builderId
    prefabPath = $translation.prefabPath
    sceneRoot = $translation.sceneRoot
    mode = "generate-from-contract"
    compileSucceeded = (Test-McpResponseSuccess -Response $compile.Wait)
    bridgeHealth = $health.State
    verifiedChildPaths = $translation.verifiedChildPaths
    prefabChecks = $prefabChecks
    appliedContract = $appliedContract
}

Ensure-McpParentDirectory -PathValue $ArtifactPath
$result | ConvertTo-Json -Depth 20 | Set-Content -Path $ArtifactPath -Encoding utf8
$result | ConvertTo-Json -Depth 20
