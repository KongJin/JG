param(
    [string]$SurfaceId = "account-delete-confirm",
    [string]$HtmlPath = "",
    [string]$ImagePath = "",
    [string]$TargetAssetPath = "",
    [string]$OutputPath = ""
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

function Read-AllText {
    param([Parameter(Mandatory = $true)][string]$PathValue)
    $resolvedPath = Resolve-RepoPath -PathValue $PathValue
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "File not found: $resolvedPath"
    }

    return Get-Content -LiteralPath $resolvedPath -Raw
}

function Read-JsonObject {
    param([Parameter(Mandatory = $true)][string]$PathValue)
    return Read-AllText -PathValue $PathValue | ConvertFrom-Json
}

function Test-InMemoryPath {
    param([string]$PathValue)

    return (-not [string]::IsNullOrWhiteSpace($PathValue)) -and $PathValue.StartsWith("in-memory://", [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-RequiredValue {
    param(
        [Parameter(Mandatory = $true)][object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $InputObject -or $null -eq $InputObject.PSObject.Properties[$Name]) {
        throw "Required property '$Name' is missing."
    }

    return $InputObject.PSObject.Properties[$Name].Value
}

function Get-OptionalValue {
    param(
        [Parameter(Mandatory = $true)][object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if ($null -eq $InputObject) {
        return $null
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

function Get-CustomColors {
    param([Parameter(Mandatory = $true)][string]$Html)

    $colors = @{}
    $configMatch = [regex]::Match($Html, 'colors:\s*\{(?<body>.*?)\}\s*,\s*(?:borderRadius|fontFamily):', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $configMatch.Success) {
        return $colors
    }

    $body = $configMatch.Groups["body"].Value
    $matches = [regex]::Matches($body, "(?<name>[A-Za-z0-9_]+)\s*:\s*['""](?<value>[^'""]+)['""]")
    foreach ($match in $matches) {
        $colors[$match.Groups["name"].Value] = $match.Groups["value"].Value
    }

    return $colors
}

function Convert-RgbaToHex {
    param([Parameter(Mandatory = $true)][string]$Value)

    $match = [regex]::Match($Value, 'rgba\(\s*(?<r>\d+)\s*,\s*(?<g>\d+)\s*,\s*(?<b>\d+)\s*,\s*(?<a>[0-9.]+)\s*\)')
    if (-not $match.Success) {
        return $Value
    }

    $r = [int]$match.Groups["r"].Value
    $g = [int]$match.Groups["g"].Value
    $b = [int]$match.Groups["b"].Value
    $a = [double]$match.Groups["a"].Value
    $alpha = [Math]::Round($a * 255)
    return ('#{0:X2}{1:X2}{2:X2}{3:X2}' -f $r, $g, $b, [int]$alpha)
}

function Get-TailwindPalette {
    return @{
        "black" = "#000000"
        "white" = "#FFFFFF"
        "gray-900" = "#111827"
        "slate-950" = "#020617"
        "slate-900" = "#0F172A"
        "slate-800" = "#1E293B"
        "slate-700" = "#334155"
        "slate-600" = "#475569"
        "slate-500" = "#64748B"
        "slate-400" = "#94A3B8"
        "slate-300" = "#CBD5E1"
        "slate-200" = "#E2E8F0"
        "slate-100" = "#F1F5F9"
        "zinc-950" = "#09090B"
        "zinc-900" = "#18181B"
        "zinc-800" = "#27272A"
        "zinc-600" = "#52525B"
        "zinc-700" = "#3F3F46"
        "zinc-500" = "#71717A"
        "zinc-400" = "#A1A1AA"
        "zinc-300" = "#D4D4D8"
        "zinc-200" = "#E4E4E7"
        "zinc-100" = "#F4F4F5"
        "red-600" = "#DC2626"
        "red-500" = "#EF4444"
        "red-400" = "#F87171"
        "amber-400" = "#FBBF24"
        "amber-500" = "#F59E0B"
        "orange-500" = "#F97316"
        "blue-400" = "#60A5FA"
        "blue-500" = "#5EB6FF"
        "cyan-400" = "#22D3EE"
        "emerald-400" = "#34D399"
    }
}

function Convert-HexWithOpacity {
    param(
        [Parameter(Mandatory = $true)][string]$Hex,
        [Parameter(Mandatory = $true)][double]$Opacity
    )

    $clean = $Hex.TrimStart("#")
    if ($clean.Length -lt 6) {
        throw "Unsupported hex value '$Hex'."
    }

    $r = [Convert]::ToInt32($clean.Substring(0, 2), 16)
    $g = [Convert]::ToInt32($clean.Substring(2, 2), 16)
    $b = [Convert]::ToInt32($clean.Substring(4, 2), 16)
    $alpha = [Math]::Round($Opacity * 255)
    return ('#{0:X2}{1:X2}{2:X2}{3:X2}' -f $r, $g, $b, [int]$alpha)
}

function Resolve-ColorToken {
    param(
        [Parameter(Mandatory = $true)][string]$Token,
        [Parameter(Mandatory = $true)][hashtable]$CustomColors
    )

    $raw = $Token
    if ($raw -match '^(?<base>[A-Za-z0-9\-]+)\/(?<opacity>\d+)$') {
        $base = $Matches["base"]
        $opacity = [int]$Matches["opacity"] / 100.0
    }
    else {
        $base = $raw
        $opacity = 1.0
    }

    if ($CustomColors.ContainsKey($base)) {
        $baseValue = [string]$CustomColors[$base]
        if ($baseValue.StartsWith("rgba", [System.StringComparison]::OrdinalIgnoreCase)) {
            $baseValue = Convert-RgbaToHex -Value $baseValue
        }
    }
    elseif ($base -match '^\[(?<hex>\#[0-9A-Fa-f]{6})\]$') {
        $baseValue = [string]$Matches["hex"]
    }
    elseif ($base -match '^\#(?<hex>[0-9A-Fa-f]{6})$') {
        $baseValue = ('#{0}' -f [string]$Matches["hex"])
    }
    else {
        $palette = Get-TailwindPalette
        if (-not $palette.ContainsKey($base)) {
            throw "Unsupported color token '$Token'."
        }
        $baseValue = [string]$palette[$base]
    }

    if ($opacity -ge 0.999) {
        if ($baseValue.Length -eq 9) {
            return $baseValue
        }

        return $baseValue.ToUpperInvariant()
    }

    if ($baseValue.Length -eq 9) {
        $baseValue = "#" + $baseValue.Substring(1, 6)
    }

    return Convert-HexWithOpacity -Hex $baseValue -Opacity $opacity
}

function Resolve-ConfiguredColor {
    param(
        [string]$Token,
        [Parameter(Mandatory = $true)][hashtable]$CustomColors
    )

    if ([string]::IsNullOrWhiteSpace($Token)) {
        return ""
    }

    if ($Token -eq "transparent") {
        return "#00000000"
    }

    return Resolve-ColorToken -Token $Token -CustomColors $CustomColors
}

function Get-SingleMatch {
    param(
        [Parameter(Mandatory = $true)][string]$Html,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    $match = [regex]::Match($Html, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $match.Success) {
        throw "Pattern not found: $Pattern"
    }

    return $match
}

function Clean-InnerText {
    param([Parameter(Mandatory = $true)][string]$Value)

    $withoutIcons = [regex]::Replace($Value, '<span[^>]*material-symbols-outlined[^>]*>.*?</span>', '', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    $withoutTags = [regex]::Replace($withoutIcons, '<[^>]+>', '')
    $decoded = [System.Net.WebUtility]::HtmlDecode($withoutTags)
    return ([regex]::Replace($decoded, '\s+', ' ')).Trim()
}

function Get-CleanTextByPattern {
    param(
        [Parameter(Mandatory = $true)][string]$Html,
        [AllowEmptyString()][string]$Pattern = "",
        [string]$GroupName = "text",
        [switch]$Optional
    )

    if ([string]::IsNullOrWhiteSpace($Pattern)) {
        if ($Optional) {
            return ""
        }

        throw "Pattern is required."
    }

    $match = [regex]::Match($Html, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $match.Success) {
        if ($Optional) {
            return ""
        }

        throw "Pattern not found: $Pattern"
    }

    return Clean-InnerText -Value $match.Groups[$GroupName].Value
}

function Get-ButtonTextsByPattern {
    param(
        [Parameter(Mandatory = $true)][string]$Html,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    $matches = [regex]::Matches($Html, $Pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($matches.Count -lt 2) {
        throw "Expected at least two footer buttons from pattern '$Pattern'."
    }

    return @(
        (Clean-InnerText -Value $matches[0].Groups["content"].Value),
        (Clean-InnerText -Value $matches[1].Groups["content"].Value)
    )
}

function New-Property {
    param(
        [Parameter(Mandatory = $true)][string]$ComponentType,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [string]$Value = "",
        [string]$AssetReferencePath = ""
    )

    $obj = [ordered]@{
        componentType = $ComponentType
        propertyName = $PropertyName
    }

    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        $obj.value = $Value
    }

    if (-not [string]::IsNullOrWhiteSpace($AssetReferencePath)) {
        $obj.assetReferencePath = $AssetReferencePath
    }

    return [PSCustomObject]$obj
}

function Get-ProfileGeneratorScriptPath {
    return Resolve-RepoPath -PathValue "tools/stitch-unity/presentations/Generate-StitchPresentationProfile.ps1"
}

function Get-ProfileGeneratorProbe {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [string]$HtmlPath = "",
        [string]$ImagePath = "",
        [string]$TargetAssetPath = ""
    )

    $generatorPath = Get-ProfileGeneratorScriptPath
    if (-not (Test-Path -LiteralPath $generatorPath)) {
        throw "Presentation profile generator not found: $generatorPath"
    }

    $invocationArgs = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $generatorPath,
        "-SurfaceId", $SurfaceId,
        "-CanGenerateOnly"
    )
    if (-not [string]::IsNullOrWhiteSpace($HtmlPath)) {
        $invocationArgs += @("-HtmlPath", $HtmlPath)
    }
    if (-not [string]::IsNullOrWhiteSpace($ImagePath)) {
        $invocationArgs += @("-ImagePath", $ImagePath)
    }
    if (-not [string]::IsNullOrWhiteSpace($TargetAssetPath)) {
        $invocationArgs += @("-TargetAssetPath", $TargetAssetPath)
    }

    $json = & powershell @invocationArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Presentation profile generator probe failed for '$SurfaceId': $json"
    }

    return ($json | Out-String | ConvertFrom-Json)
}

function Get-GeneratedPresentationProfile {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [string]$HtmlPath = "",
        [string]$ImagePath = "",
        [string]$TargetAssetPath = ""
    )

    $probe = Get-ProfileGeneratorProbe -SurfaceId $SurfaceId -HtmlPath $HtmlPath -ImagePath $ImagePath -TargetAssetPath $TargetAssetPath
    if (-not [bool]$probe.supported) {
        throw "Presentation profile generator does not support '$SurfaceId': $([string]$probe.reason)"
    }

    $generatorPath = Get-ProfileGeneratorScriptPath
    $invocationArgs = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", $generatorPath,
        "-SurfaceId", $SurfaceId
    )
    if (-not [string]::IsNullOrWhiteSpace($HtmlPath)) {
        $invocationArgs += @("-HtmlPath", $HtmlPath)
    }
    if (-not [string]::IsNullOrWhiteSpace($ImagePath)) {
        $invocationArgs += @("-ImagePath", $ImagePath)
    }
    if (-not [string]::IsNullOrWhiteSpace($TargetAssetPath)) {
        $invocationArgs += @("-TargetAssetPath", $TargetAssetPath)
    }

    $json = & powershell @invocationArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Presentation profile generation failed for '$SurfaceId': $json"
    }

    $result = $json | Out-String | ConvertFrom-Json
    if ($null -eq $result.PSObject.Properties["profile"] -or $null -eq $result.profile) {
        throw "Presentation profile generation returned no in-memory profile for '$SurfaceId'."
    }

    return $result.profile
}

function Get-PresentationProfile {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [string]$HtmlPath = "",
        [string]$ImagePath = "",
        [string]$TargetAssetPath = ""
    )

    return (Get-GeneratedPresentationProfile -SurfaceId $SurfaceId -HtmlPath $HtmlPath -ImagePath $ImagePath -TargetAssetPath $TargetAssetPath)
}

function New-OverlayDialogPresentationContract {
    param(
        [Parameter(Mandatory = $true)][object]$Profile,
        [Parameter(Mandatory = $true)][string]$Html,
        [Parameter(Mandatory = $true)][hashtable]$CustomColors,
        [Parameter(Mandatory = $true)][string]$HtmlPath,
        [Parameter(Mandatory = $true)][string]$ImagePath,
        [Parameter(Mandatory = $true)][string]$FontAssetPath
    )

    $patterns = Get-RequiredValue -InputObject $Profile -Name "patterns"
    $layout = Get-RequiredValue -InputObject $Profile -Name "layout"
    $colors = Get-RequiredValue -InputObject $Profile -Name "colors"
    $header = Get-RequiredValue -InputObject $Profile -Name "header"
    $body = Get-RequiredValue -InputObject $Profile -Name "body"
    $footer = Get-RequiredValue -InputObject $Profile -Name "footer"
    $viewport = Get-RequiredValue -InputObject $Profile -Name "viewport"

    $titleText = Get-CleanTextByPattern -Html $Html -Pattern ([string](Get-RequiredValue -InputObject $patterns -Name "titlePattern"))
    $summaryPattern = [string](Get-OptionalValue -InputObject $patterns -Name "summaryPattern")
    $summaryText = Get-CleanTextByPattern -Html $Html -Pattern $summaryPattern -Optional
    $bodyText = Get-CleanTextByPattern -Html $Html -Pattern ([string](Get-RequiredValue -InputObject $patterns -Name "bodyPattern"))
    $buttonTexts = @(Get-ButtonTextsByPattern -Html $Html -Pattern ([string](Get-RequiredValue -InputObject $patterns -Name "buttonPattern")))
    $secondaryText = [string]$buttonTexts[0]
    $primaryText = [string]$buttonTexts[1]

    $dialogWidthPx = [int](Get-RequiredValue -InputObject $layout -Name "dialogWidthPx")
    $headerHeightPx = [int](Get-RequiredValue -InputObject $layout -Name "headerHeightPx")
    $bodyHeightPx = [int](Get-RequiredValue -InputObject $layout -Name "bodyHeightPx")
    $footerHeightPx = [int](Get-RequiredValue -InputObject $layout -Name "footerHeightPx")
    $panelHeightPx = $headerHeightPx + $bodyHeightPx + $footerHeightPx
    $headerPaddingX = [int](Get-RequiredValue -InputObject $layout -Name "headerPaddingX")
    $headerPaddingY = [int](Get-RequiredValue -InputObject $layout -Name "headerPaddingY")
    $bodyPadding = [int](Get-RequiredValue -InputObject $layout -Name "bodyPadding")
    $footerPaddingX = [int](Get-RequiredValue -InputObject $layout -Name "footerPaddingX")
    $footerPaddingTop = [int](Get-RequiredValue -InputObject $layout -Name "footerPaddingTop")
    $footerPaddingBottom = [int](Get-RequiredValue -InputObject $layout -Name "footerPaddingBottom")
    $footerGap = [int](Get-RequiredValue -InputObject $layout -Name "footerGap")
    $titleGap = [int](Get-RequiredValue -InputObject $layout -Name "titleGap")
    $titleFontPx = [int](Get-RequiredValue -InputObject $layout -Name "titleFontPx")
    $bodyFontPx = [int](Get-RequiredValue -InputObject $layout -Name "bodyFontPx")
    $buttonFontPx = [int](Get-RequiredValue -InputObject $layout -Name "buttonFontPx")
    $buttonHeightPx = [int](Get-RequiredValue -InputObject $layout -Name "buttonHeightPx")
    $headerLayout = [string](Get-RequiredValue -InputObject $layout -Name "headerLayout")
    $footerLayout = [string](Get-RequiredValue -InputObject $layout -Name "footerLayout")

    $summaryFontPxValue = Get-OptionalValue -InputObject $layout -Name "summaryFontPx"
    $summaryFontPx = if ($null -ne $summaryFontPxValue) { [int]$summaryFontPxValue } else { 0 }
    $titleStackWidthValue = Get-OptionalValue -InputObject $layout -Name "titleStackWidthPx"
    $titleStackWidthPx = if ($null -ne $titleStackWidthValue) { [int]$titleStackWidthValue } else { 0 }
    $titleWidthValue = Get-OptionalValue -InputObject $layout -Name "titleWidthPx"
    $titleWidthPx = if ($null -ne $titleWidthValue) { [int]$titleWidthValue } else { 0 }

    $scrimColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "scrim")) -CustomColors $CustomColors
    $panelColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "panel")) -CustomColors $CustomColors
    $headerColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "header")) -CustomColors $CustomColors
    $iconColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "icon")) -CustomColors $CustomColors
    $titleColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "title")) -CustomColors $CustomColors
    $summaryColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "summary")) -CustomColors $CustomColors
    $bodyBackgroundColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "bodyBackground")) -CustomColors $CustomColors
    $bodyTextColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "bodyText")) -CustomColors $CustomColors
    $footerBackgroundColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "footerBackground")) -CustomColors $CustomColors
    $secondaryButtonColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "secondaryButton")) -CustomColors $CustomColors
    $secondaryTextColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "secondaryText")) -CustomColors $CustomColors
    $primaryButtonColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "primaryButton")) -CustomColors $CustomColors
    $primaryTextColor = Resolve-ConfiguredColor -Token ([string](Get-RequiredValue -InputObject $colors -Name "primaryText")) -CustomColors $CustomColors

    $elements = New-Object System.Collections.Generic.List[object]

    $elements.Add([PSCustomObject][ordered]@{
        path = ""
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value "#00000000")
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "Scrim"
        role = "overlay-scrim"
        components = @("Image")
        siblingIndex = 0
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $scrimColor)
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "DialogPanel"
        role = "dialog-shell"
        components = @("Image", "VerticalLayoutGroup")
        siblingIndex = 1
        rect = [ordered]@{
            anchorMin = "(0.5,0.5)"
            anchorMax = "(0.5,0.5)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = ("({0},{1})" -f $dialogWidthPx, $panelHeightPx)
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $panelColor),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Spacing" -Value "0"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false")
        )
    })

    $headerProperties = @(
        (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $headerColor),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing" -Value ([string]$titleGap)),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "false"),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "false"),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value ([string]$headerPaddingX)),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value ([string]$headerPaddingX)),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value ([string]$headerPaddingY)),
        (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Bottom" -Value ([string]$headerPaddingY)),
        (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$headerHeightPx))
    )
    $elements.Add([PSCustomObject][ordered]@{
        path = [string](Get-RequiredValue -InputObject $header -Name "path")
        role = "dialog-header"
        components = @("LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0.5,0.5)"
            anchorMax = "(0.5,0.5)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = ("({0},{1})" -f $dialogWidthPx, $headerHeightPx)
        }
        properties = @($headerProperties)
    })

    $iconKind = [string](Get-RequiredValue -InputObject $header -Name "iconKind")
    $iconPath = [string](Get-RequiredValue -InputObject $header -Name "iconPath")
    $iconSizePx = [int](Get-RequiredValue -InputObject $header -Name "iconSizePx")
    if ($iconKind -eq "badge") {
        $elements.Add([PSCustomObject][ordered]@{
            path = $iconPath
            role = "warning-badge-shell"
            components = @("Image", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0.5,0.5)"
                anchorMax = "(0.5,0.5)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = ("({0},{1})" -f $iconSizePx, $iconSizePx)
            }
            properties = @(
                (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $iconColor),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value ([string]$iconSizePx)),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$iconSizePx))
            )
        })
    }
    elseif ($iconKind -eq "text") {
        $elements.Add([PSCustomObject][ordered]@{
            path = $iconPath
            role = "warning-icon"
            components = @("TextMeshProUGUI", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0.5,0.5)"
                anchorMax = "(0.5,0.5)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = ("({0},{0})" -f $iconSizePx)
            }
            properties = @(
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string](Get-RequiredValue -InputObject $header -Name "iconText"))),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value ([string]$iconSizePx)),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $iconColor),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "2"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value ([string]$iconSizePx)),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$iconSizePx))
            )
        })
    }
    else {
        throw "Unsupported iconKind '$iconKind'."
    }

    if ($headerLayout -eq "stacked") {
        $titleStackPath = [string](Get-RequiredValue -InputObject $header -Name "titleStackPath")
        $elements.Add([PSCustomObject][ordered]@{
            path = $titleStackPath
            role = "header-copy-stack"
            components = @("VerticalLayoutGroup", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0.5,0.5)"
                anchorMax = "(0.5,0.5)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = ("({0},{1})" -f $titleStackWidthPx, ($headerHeightPx - (2 * $headerPaddingY)))
            }
            properties = @(
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Spacing" -Value "4"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value ([string]$titleStackWidthPx))
            )
        })
    }

    $titlePath = [string](Get-RequiredValue -InputObject $header -Name "titlePath")
    if ($headerLayout -eq "stacked") {
        $titleAnchorMin = "(0,0)"
        $titleAnchorMax = "(1,1)"
        $titleSizeDelta = "(0,0)"
    }
    else {
        $titleAnchorMin = "(0.5,0.5)"
        $titleAnchorMax = "(0.5,0.5)"
        $titleSizeDelta = ("({0},{1})" -f $titleWidthPx, 24)
    }

    $titleProperties = @(
        (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $titleText),
        (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value ([string]$titleFontPx)),
        (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $titleColor),
        (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
        (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "1"),
        (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512")
    )
    if ($headerLayout -eq "stacked") {
        $titleProperties += (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]($titleFontPx + 6)))
    }
    else {
        $titleProperties += (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value ([string]$titleWidthPx))
    }

    $elements.Add([PSCustomObject][ordered]@{
        path = $titlePath
        role = "header-title"
        components = @("TextMeshProUGUI", "LayoutElement")
        rect = [ordered]@{
            anchorMin = $titleAnchorMin
            anchorMax = $titleAnchorMax
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = $titleSizeDelta
        }
        properties = @($titleProperties)
    })

    if (-not [string]::IsNullOrWhiteSpace($summaryText)) {
        $summaryPath = [string](Get-RequiredValue -InputObject $header -Name "summaryPath")
        $elements.Add([PSCustomObject][ordered]@{
            path = $summaryPath
            role = "header-summary"
            components = @("TextMeshProUGUI", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0,0)"
                anchorMax = "(1,1)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,0)"
            }
            properties = @(
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $summaryText),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value ([string]$summaryFontPx)),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $summaryColor),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "1"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]($summaryFontPx + 4)))
            )
        })
    }

    $bodyProperties = @()
    if (-not [string]::IsNullOrWhiteSpace($bodyBackgroundColor)) {
        $bodyProperties += (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $bodyBackgroundColor)
    }
    $bodyProperties += (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$bodyHeightPx))
    $elements.Add([PSCustomObject][ordered]@{
        path = [string](Get-RequiredValue -InputObject $body -Name "path")
        role = "dialog-body"
        rect = [ordered]@{
            anchorMin = "(0.5,0.5)"
            anchorMax = "(0.5,0.5)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = ("({0},{1})" -f $dialogWidthPx, $bodyHeightPx)
        }
        properties = @($bodyProperties)
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = [string](Get-RequiredValue -InputObject $body -Name "textPath")
        role = "body-copy"
        components = @("TextMeshProUGUI")
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = ("(-{0},-{0})" -f (2 * $bodyPadding))
        }
        properties = @(
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $bodyText),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value ([string]$bodyFontPx)),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $bodyTextColor),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "1"),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "256"),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_margin" -Value "(20,20,20,20)")
        )
    })

    if ($footerLayout -eq "vertical") {
        $footerGroupType = "VerticalLayoutGroup"
        $secondaryButtonWidthPx = $dialogWidthPx - (2 * $footerPaddingX)
        $primaryButtonWidthPx = $secondaryButtonWidthPx
    }
    elseif ($footerLayout -eq "horizontal") {
        $footerGroupType = "HorizontalLayoutGroup"
        $secondaryButtonWidthPx = [int](($dialogWidthPx - (2 * $footerPaddingX) - $footerGap) / 2)
        $primaryButtonWidthPx = $secondaryButtonWidthPx
    }
    else {
        throw "Unsupported footerLayout '$footerLayout'."
    }

    $footerProperties = @()
    if (-not [string]::IsNullOrWhiteSpace($footerBackgroundColor)) {
        $footerProperties += (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $footerBackgroundColor)
    }
    $footerProperties += @(
        (New-Property -ComponentType $footerGroupType -PropertyName "m_Spacing" -Value ([string]$footerGap)),
        (New-Property -ComponentType $footerGroupType -PropertyName "m_ChildControlWidth" -Value "false"),
        (New-Property -ComponentType $footerGroupType -PropertyName "m_ChildControlHeight" -Value "false"),
        (New-Property -ComponentType $footerGroupType -PropertyName "m_ChildForceExpandWidth" -Value "false"),
        (New-Property -ComponentType $footerGroupType -PropertyName "m_ChildForceExpandHeight" -Value "false"),
        (New-Property -ComponentType $footerGroupType -PropertyName "m_Padding.m_Left" -Value ([string]$footerPaddingX)),
        (New-Property -ComponentType $footerGroupType -PropertyName "m_Padding.m_Right" -Value ([string]$footerPaddingX))
    )
    if ($footerPaddingTop -gt 0) {
        $footerProperties += (New-Property -ComponentType $footerGroupType -PropertyName "m_Padding.m_Top" -Value ([string]$footerPaddingTop))
    }
    if ($footerPaddingBottom -gt 0) {
        $footerProperties += (New-Property -ComponentType $footerGroupType -PropertyName "m_Padding.m_Bottom" -Value ([string]$footerPaddingBottom))
    }
    $footerProperties += (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$footerHeightPx))
    $elements.Add([PSCustomObject][ordered]@{
        path = [string](Get-RequiredValue -InputObject $footer -Name "path")
        role = "dialog-footer"
        rect = [ordered]@{
            anchorMin = "(0.5,0.5)"
            anchorMax = "(0.5,0.5)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = ("({0},{1})" -f $dialogWidthPx, $footerHeightPx)
        }
        properties = @($footerProperties)
    })

    $secondaryButtonProperties = @(
        (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $secondaryButtonColor)
    )
    if ($footerLayout -eq "horizontal") {
        $secondaryButtonProperties += (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value ([string]$secondaryButtonWidthPx))
    }
    $secondaryButtonProperties += (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$buttonHeightPx))
    $elements.Add([PSCustomObject][ordered]@{
        path = [string](Get-RequiredValue -InputObject $footer -Name "secondaryPath")
        role = "secondary-cta"
        rect = [ordered]@{
            anchorMin = "(0.5,0.5)"
            anchorMax = "(0.5,0.5)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = ("({0},{1})" -f $secondaryButtonWidthPx, $buttonHeightPx)
        }
        properties = @($secondaryButtonProperties)
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = [string](Get-RequiredValue -InputObject $footer -Name "secondaryLabelPath")
        role = "secondary-cta-label"
        components = @("TextMeshProUGUI")
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $secondaryText),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value ([string]$buttonFontPx)),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $secondaryTextColor),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "2"),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512")
        )
    })

    $primaryButtonProperties = @(
        (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $primaryButtonColor)
    )
    if ($footerLayout -eq "horizontal") {
        $primaryButtonProperties += (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value ([string]$primaryButtonWidthPx))
    }
    $primaryButtonProperties += (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$buttonHeightPx))
    $elements.Add([PSCustomObject][ordered]@{
        path = [string](Get-RequiredValue -InputObject $footer -Name "primaryPath")
        role = "primary-cta"
        rect = [ordered]@{
            anchorMin = "(0.5,0.5)"
            anchorMax = "(0.5,0.5)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = ("({0},{1})" -f $primaryButtonWidthPx, $buttonHeightPx)
        }
        properties = @($primaryButtonProperties)
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = [string](Get-RequiredValue -InputObject $footer -Name "primaryLabelPath")
        role = "primary-cta-label"
        components = @("TextMeshProUGUI")
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $primaryText),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value ([string]$buttonFontPx)),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $primaryTextColor),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "2"),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512")
        )
    })

    return [ordered]@{
        schemaVersion = "1.0.0"
        contractKind = "presentation-contract"
        surfaceId = [string](Get-RequiredValue -InputObject $Profile -Name "surfaceId")
        surfaceRole = "overlay"
        extractionStatus = "resolved"
        sourceRefs = [ordered]@{
            imagePath = $ImagePath
            htmlPath = $HtmlPath
        }
        derivedFrom = [ordered]@{
            dialogViewport = [string](Get-RequiredValue -InputObject $viewport -Name "label")
            htmlTitle = $titleText
            footerButtons = @($secondaryText, $primaryText)
        }
        unresolvedDerivedFields = @([string[]](Get-RequiredValue -InputObject $Profile -Name "unresolvedDerivedFields"))
        elements = @($elements.ToArray())
        notes = @(
            ("Generated from {0} source freeze." -f [System.IO.Path]::GetFileName($HtmlPath)),
            "Values in this file are source-derived from Tailwind classes and inline content, not hand-authored UI constants."
        )
    }
}

function New-WorkspacePresentationContract {
    param(
        [Parameter(Mandatory = $true)][object]$Profile,
        [Parameter(Mandatory = $true)][hashtable]$CustomColors,
        [Parameter(Mandatory = $true)][string]$HtmlPath,
        [Parameter(Mandatory = $true)][string]$ImagePath,
        [Parameter(Mandatory = $true)][string]$FontAssetPath
    )

    $viewport = Get-RequiredValue -InputObject $Profile -Name "viewport"
    $workspace = Get-RequiredValue -InputObject $Profile -Name "workspace"
    $colors = Get-RequiredValue -InputObject $Profile -Name "colors"

    $headerColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "header")) -CustomColors $CustomColors
    $titleColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "title")) -CustomColors $CustomColors
    $subtitleColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "subtitle")) -CustomColors $CustomColors
    $slotColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "slot")) -CustomColors $CustomColors
    $editorColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "editor")) -CustomColors $CustomColors
    $previewColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "preview")) -CustomColors $CustomColors
    $summaryBarColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "summaryBar")) -CustomColors $CustomColors
    $summaryTrackColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "summaryTrack")) -CustomColors $CustomColors
    $summaryFillColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "summaryFill")) -CustomColors $CustomColors
    $summaryTextColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "summaryText")) -CustomColors $CustomColors
    $saveDockColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "saveDock")) -CustomColors $CustomColors
    $primaryButtonColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "primaryButton")) -CustomColors $CustomColors
    $primaryTextColor = Resolve-ConfiguredColor -Token ([string](Get-OptionalValue -InputObject $colors -Name "primaryText")) -CustomColors $CustomColors

    $headerHeightPx = [int](Get-RequiredValue -InputObject $workspace -Name "headerHeightPx")
    $headerPaddingX = [int](Get-RequiredValue -InputObject $workspace -Name "headerPaddingX")
    $mainPaddingX = [int](Get-RequiredValue -InputObject $workspace -Name "mainPaddingX")
    $mainGap = [int](Get-RequiredValue -InputObject $workspace -Name "mainGap")
    $slotGap = [int](Get-RequiredValue -InputObject $workspace -Name "slotGap")
    $focusGap = [int](Get-RequiredValue -InputObject $workspace -Name "focusGap")
    $editorPadding = [int](Get-RequiredValue -InputObject $workspace -Name "editorPadding")
    $previewHeightPx = [int](Get-RequiredValue -InputObject $workspace -Name "previewHeightPx")
    $summaryBarHeightPx = [int](Get-OptionalValue -InputObject $workspace -Name "summaryBarHeightPx")
    $summaryTrackHeightPx = [int](Get-OptionalValue -InputObject $workspace -Name "summaryTrackHeightPx")
    $summaryFillPercent = [int](Get-OptionalValue -InputObject $workspace -Name "summaryFillPercent")
    $summaryText = [string](Get-OptionalValue -InputObject $workspace -Name "summaryText")
    $summaryTextFontPx = [int](Get-OptionalValue -InputObject $workspace -Name "summaryTextFontPx")
    $summaryTextWidthPx = [int](Get-OptionalValue -InputObject $workspace -Name "summaryTextWidthPx")
    $saveButtonHeightPx = [int](Get-RequiredValue -InputObject $workspace -Name "saveButtonHeightPx")
    $saveDockPaddingX = [int](Get-RequiredValue -InputObject $workspace -Name "saveDockPaddingX")
    $saveDockPaddingTop = [int](Get-RequiredValue -InputObject $workspace -Name "saveDockPaddingTop")
    $titleText = [string](Get-RequiredValue -InputObject $workspace -Name "titleText")
    $subtitleText = [string](Get-OptionalValue -InputObject $workspace -Name "subtitleText")
    $saveButtonText = [string](Get-RequiredValue -InputObject $workspace -Name "saveButtonText")
    $settingsIconText = [string](Get-OptionalValue -InputObject $workspace -Name "settingsIconText")
    $slotItems = @(Get-OptionalValue -InputObject $workspace -Name "slotItems")
    $focusTabs = @(Get-OptionalValue -InputObject $workspace -Name "focusTabs")
    $editor = Get-OptionalValue -InputObject $workspace -Name "editor"
    $preview = Get-OptionalValue -InputObject $workspace -Name "preview"
    $summaryFillAnchorMaxX = [Math]::Max(0.0, [Math]::Min(1.0, ($summaryFillPercent / 100.0)))
    $summaryFillAnchorMaxXText = [string]::Format([System.Globalization.CultureInfo]::InvariantCulture, "{0:0.##}", $summaryFillAnchorMaxX)
    $accentColor = Resolve-ConfiguredColor -Token "blue-500" -CustomColors $CustomColors
    $accentTextColor = Resolve-ConfiguredColor -Token "blue-400" -CustomColors $CustomColors
    $mutedTextColor = Resolve-ConfiguredColor -Token "zinc-500" -CustomColors $CustomColors
    $surfaceMutedColor = Resolve-ConfiguredColor -Token "zinc-900" -CustomColors $CustomColors
    $surfaceStrongColor = Resolve-ConfiguredColor -Token "zinc-950" -CustomColors $CustomColors
    $borderMutedColor = Resolve-ConfiguredColor -Token "zinc-800" -CustomColors $CustomColors
    $accentBorderColor = Resolve-ConfiguredColor -Token "blue-500/50" -CustomColors $CustomColors
    $subtitleMutedColor = Resolve-ConfiguredColor -Token "zinc-400" -CustomColors $CustomColors

    $elements = New-Object System.Collections.Generic.List[object]

    $elements.Add([PSCustomObject][ordered]@{
        path = ""
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value "#00000000")
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "HeaderChrome"
        role = "header-chrome"
        components = @("Image", "HorizontalLayoutGroup", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0,1)"
            anchorMax = "(1,1)"
            pivot = "(0.5,1)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,$headerHeightPx)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $headerColor),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing" -Value "12"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value ([string]$headerPaddingX)),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value ([string]$headerPaddingX)),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$headerHeightPx))
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "HeaderChrome/TitleGroup"
        role = "header-title-group"
        components = @("VerticalLayoutGroup", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Spacing" -Value "2"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_FlexibleWidth" -Value "1")
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "HeaderChrome/TitleGroup/TitleText"
        role = "header-title"
        components = @("TextMeshProUGUI")
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $titleText),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "16"),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $titleColor),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "1"),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512"),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath)
        )
    })

    if (-not [string]::IsNullOrWhiteSpace($subtitleText)) {
        $elements.Add([PSCustomObject][ordered]@{
            path = "HeaderChrome/TitleGroup/SubtitleText"
            role = "header-subtitle"
            components = @("TextMeshProUGUI")
            rect = [ordered]@{
                anchorMin = "(0,0)"
                anchorMax = "(1,1)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,0)"
            }
            properties = @(
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $subtitleText),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "10"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $subtitleColor),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "1"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath)
            )
        })
    }

    $elements.Add([PSCustomObject][ordered]@{
        path = "HeaderChrome/SettingsButton"
        role = "aux-action"
        components = @("Button", "Image", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(1,0.5)"
            anchorMax = "(1,0.5)"
            pivot = "(1,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(32,32)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value "#00000000"),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value "32"),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "32")
        )
    })

    if (-not [string]::IsNullOrWhiteSpace($settingsIconText)) {
        $elements.Add([PSCustomObject][ordered]@{
            path = "HeaderChrome/SettingsButton/IconText"
            role = "aux-action-icon"
            components = @("TextMeshProUGUI")
            rect = [ordered]@{
                anchorMin = "(0,0)"
                anchorMax = "(1,1)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,0)"
            }
            properties = @(
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $settingsIconText),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "18"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $mutedTextColor),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "2"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512")
            )
        })
    }

    $elements.Add([PSCustomObject][ordered]@{
        path = "MainScroll"
        role = "main-scroll"
        components = @("ScrollRect", "RectMask2D")
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,10)"
            sizeDelta = "(0,-132)"
        }
        properties = @(
            (New-Property -ComponentType "ScrollRect" -PropertyName "m_Horizontal" -Value "false"),
            (New-Property -ComponentType "ScrollRect" -PropertyName "m_Vertical" -Value "true"),
            (New-Property -ComponentType "ScrollRect" -PropertyName "m_MovementType" -Value "1")
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "MainScroll/Content"
        role = "main-scroll-content"
        components = @("VerticalLayoutGroup", "ContentSizeFitter")
        rect = [ordered]@{
            anchorMin = "(0,1)"
            anchorMax = "(1,1)"
            pivot = "(0.5,1)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Spacing" -Value ([string]$mainGap)),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value ([string]$mainPaddingX)),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value ([string]$mainPaddingX)),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value ([string]$mainGap)),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Bottom" -Value ([string]$mainGap)),
            (New-Property -ComponentType "ContentSizeFitter" -PropertyName "m_HorizontalFit" -Value "0"),
            (New-Property -ComponentType "ContentSizeFitter" -PropertyName "m_VerticalFit" -Value "2")
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "MainScroll/Content/SlotSelector"
        role = "slot-selector"
        components = @("Image", "HorizontalLayoutGroup", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0,1)"
            anchorMax = "(1,1)"
            pivot = "(0.5,1)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,88)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(if ([string]::IsNullOrWhiteSpace($slotColor)) { "#00000000" } else { $slotColor })),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing" -Value ([string]$slotGap)),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "88")
        )
    })

    for ($slotIndex = 0; $slotIndex -lt $slotItems.Count; $slotIndex++) {
        $slot = $slotItems[$slotIndex]
        $slotPath = "MainScroll/Content/SlotSelector/Slot{0:D2}" -f ($slotIndex + 1)
        $slotLabelColor = if ([bool]$slot.active) { $accentTextColor } else { $mutedTextColor }
        $slotRoleColor = if ([bool]$slot.active) { Resolve-ConfiguredColor -Token "zinc-300" -CustomColors $CustomColors } else { Resolve-ConfiguredColor -Token "zinc-600" -CustomColors $CustomColors }
        $slotPanelColor = if ([bool]$slot.active) { Resolve-ConfiguredColor -Token "blue-500/10" -CustomColors $CustomColors } else { Resolve-ConfiguredColor -Token "zinc-900/50" -CustomColors $CustomColors }
        $slotBorderColor = if ([bool]$slot.active) { $accentBorderColor } else { $borderMutedColor }
        $slotIconPanelColor = if ([bool]$slot.active) { $surfaceStrongColor } else { Resolve-ConfiguredColor -Token "zinc-950" -CustomColors $CustomColors }

        $elements.Add([PSCustomObject][ordered]@{
            path = $slotPath
            role = "slot-item"
            components = @("Button", "Image", "VerticalLayoutGroup", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0,0.5)"
                anchorMax = "(0,0.5)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(84,88)"
            }
            properties = @(
                (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $slotPanelColor),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Spacing" -Value "4"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildAlignment" -Value "0"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value "8"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value "8"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value "8"),
                (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Bottom" -Value "8"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value "84"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "88")
            )
        })

        $elements.Add([PSCustomObject][ordered]@{
            path = "$slotPath/UnitText"
            role = "slot-item-unit"
            components = @("TextMeshProUGUI", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0,1)"
                anchorMax = "(1,1)"
                pivot = "(0.5,1)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,12)"
            }
            properties = @(
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$slot.unitText)),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "10"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $slotLabelColor),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "12")
            )
        })

        $elements.Add([PSCustomObject][ordered]@{
            path = "$slotPath/IconFrame"
            role = "slot-item-icon-frame"
            components = @("Image", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0.5,0.5)"
                anchorMax = "(0.5,0.5)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(40,40)"
            }
            properties = @(
                (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $slotIconPanelColor),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value "40"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "40")
            )
        })

        if (-not [string]::IsNullOrWhiteSpace([string]$slot.iconText)) {
            $elements.Add([PSCustomObject][ordered]@{
                path = "$slotPath/IconFrame/IconText"
                role = "slot-item-icon"
                components = @("TextMeshProUGUI")
                rect = [ordered]@{
                    anchorMin = "(0,0)"
                    anchorMax = "(1,1)"
                    pivot = "(0.5,0.5)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(0,0)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$slot.iconText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "18"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $slotLabelColor),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "2"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512")
                )
            })
        }

        $elements.Add([PSCustomObject][ordered]@{
            path = "$slotPath/RoleText"
            role = "slot-item-role"
            components = @("TextMeshProUGUI", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0,0)"
                anchorMax = "(1,0)"
                pivot = "(0.5,0)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,12)"
            }
            properties = @(
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$slot.roleText)),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "9"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $slotRoleColor),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "12")
            )
        })
    }

    $elements.Add([PSCustomObject][ordered]@{
        path = "MainScroll/Content/FocusBar"
        role = "focus-bar"
        components = @("HorizontalLayoutGroup", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0,1)"
            anchorMax = "(1,1)"
            pivot = "(0.5,1)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,36)"
        }
        properties = @(
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing" -Value ([string]$focusGap)),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "36")
        )
    })

    for ($focusIndex = 0; $focusIndex -lt $focusTabs.Count; $focusIndex++) {
        $focusTab = $focusTabs[$focusIndex]
        $focusPath = "MainScroll/Content/FocusBar/Tab{0:D2}" -f ($focusIndex + 1)
        $focusColor = if ([bool]$focusTab.active) { $accentTextColor } else { $mutedTextColor }

        $elements.Add([PSCustomObject][ordered]@{
            path = $focusPath
            role = "focus-tab"
            components = @("Button", "Image", "HorizontalLayoutGroup", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0,0.5)"
                anchorMax = "(0,0.5)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(116,36)"
            }
            properties = @(
                (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(if ([bool]$focusTab.active) { Resolve-ConfiguredColor -Token "blue-500/10" -CustomColors $CustomColors } else { "#00000000" })),
                (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing" -Value "4"),
                (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildAlignment" -Value "4"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value "116"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "36")
            )
        })

        if (-not [string]::IsNullOrWhiteSpace([string]$focusTab.iconText)) {
            $elements.Add([PSCustomObject][ordered]@{
                path = "$focusPath/IconText"
                role = "focus-tab-icon"
                components = @("TextMeshProUGUI", "LayoutElement")
                rect = [ordered]@{
                    anchorMin = "(0,0.5)"
                    anchorMax = "(0,0.5)"
                    pivot = "(0,0.5)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(20,20)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$focusTab.iconText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "12"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $focusColor),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                    (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value "20")
                )
            })
        }

        $elements.Add([PSCustomObject][ordered]@{
            path = "$focusPath/LabelText"
            role = "focus-tab-label"
            components = @("TextMeshProUGUI")
            rect = [ordered]@{
                anchorMin = "(0,0)"
                anchorMax = "(1,1)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,0)"
            }
            properties = @(
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$focusTab.labelText)),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "11"),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $focusColor),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath)
            )
        })
    }

    $elements.Add([PSCustomObject][ordered]@{
        path = "MainScroll/Content/EditorPanel"
        role = "editor-panel"
        components = @("Image", "VerticalLayoutGroup", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0,1)"
            anchorMax = "(1,1)"
            pivot = "(0.5,1)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,320)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(if ([string]::IsNullOrWhiteSpace($editorColor)) { "#18181B" } else { $editorColor })),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Spacing" -Value ([string]$editorPadding)),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value ([string]$editorPadding)),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value ([string]$editorPadding)),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value ([string]$editorPadding)),
            (New-Property -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Bottom" -Value ([string]$editorPadding)),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "320")
        )
    })

    if ($null -ne $editor) {
        if (-not [string]::IsNullOrWhiteSpace([string]$editor.badgeText)) {
            $elements.Add([PSCustomObject][ordered]@{
                path = "MainScroll/Content/EditorPanel/BadgeText"
                role = "editor-badge"
                components = @("TextMeshProUGUI", "LayoutElement")
                rect = [ordered]@{
                    anchorMin = "(0,1)"
                    anchorMax = "(1,1)"
                    pivot = "(0.5,1)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(0,16)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$editor.badgeText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "9"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $accentTextColor),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                    (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "16")
                )
            })
        }

        if (-not [string]::IsNullOrWhiteSpace([string]$editor.titleText)) {
            $elements.Add([PSCustomObject][ordered]@{
                path = "MainScroll/Content/EditorPanel/TitleText"
                role = "editor-title"
                components = @("TextMeshProUGUI", "LayoutElement")
                rect = [ordered]@{
                    anchorMin = "(0,1)"
                    anchorMax = "(1,1)"
                    pivot = "(0.5,1)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(0,20)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$editor.titleText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "14"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value "#FFFFFFFF"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                    (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "20")
                )
            })
        }

        if (-not [string]::IsNullOrWhiteSpace([string]$editor.descriptionText)) {
            $elements.Add([PSCustomObject][ordered]@{
                path = "MainScroll/Content/EditorPanel/DescriptionText"
                role = "editor-description"
                components = @("TextMeshProUGUI", "LayoutElement")
                rect = [ordered]@{
                    anchorMin = "(0,1)"
                    anchorMax = "(1,1)"
                    pivot = "(0.5,1)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(0,36)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$editor.descriptionText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "11"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $subtitleMutedColor),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                    (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "36")
                )
            })
        }

        for ($statIndex = 0; $statIndex -lt @($editor.stats).Count; $statIndex++) {
            $stat = $editor.stats[$statIndex]
            $statPath = "MainScroll/Content/EditorPanel/Stat{0:D2}" -f ($statIndex + 1)
            $statValueColor = if ([string]$stat.valueEmphasis -eq "accent") { Resolve-ConfiguredColor -Token "amber-500" -CustomColors $CustomColors } else { Resolve-ConfiguredColor -Token "zinc-200" -CustomColors $CustomColors }
            $elements.Add([PSCustomObject][ordered]@{
                path = $statPath
                role = "editor-stat"
                components = @("Image", "HorizontalLayoutGroup", "LayoutElement")
                rect = [ordered]@{
                    anchorMin = "(0,1)"
                    anchorMax = "(1,1)"
                    pivot = "(0.5,1)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(0,28)"
                }
                properties = @(
                    (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(Resolve-ConfiguredColor -Token "zinc-950/50" -CustomColors $CustomColors)),
                    (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
                    (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
                    (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value "8"),
                    (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value "8"),
                    (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "28")
                )
            })

            $elements.Add([PSCustomObject][ordered]@{
                path = "$statPath/LabelText"
                role = "editor-stat-label"
                components = @("TextMeshProUGUI")
                rect = [ordered]@{
                    anchorMin = "(0,0)"
                    anchorMax = "(0.6,1)"
                    pivot = "(0,0.5)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(0,0)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$stat.labelText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "10"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $mutedTextColor),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath)
                )
            })

            $elements.Add([PSCustomObject][ordered]@{
                path = "$statPath/ValueText"
                role = "editor-stat-value"
                components = @("TextMeshProUGUI")
                rect = [ordered]@{
                    anchorMin = "(0.4,0)"
                    anchorMax = "(1,1)"
                    pivot = "(1,0.5)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(0,0)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$stat.valueText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "11"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $statValueColor),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "4")
                )
            })
        }
    }

    $elements.Add([PSCustomObject][ordered]@{
        path = "MainScroll/Content/PreviewCard"
        role = "preview-card"
        components = @("Image", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0,1)"
            anchorMax = "(1,1)"
            pivot = "(0.5,1)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,$previewHeightPx)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(if ([string]::IsNullOrWhiteSpace($previewColor)) { "#18181B" } else { $previewColor })),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$previewHeightPx))
        )
    })

    if ($null -ne $preview) {
        if (-not [string]::IsNullOrWhiteSpace([string]$preview.titleText)) {
            $elements.Add([PSCustomObject][ordered]@{
                path = "MainScroll/Content/PreviewCard/TitleText"
                role = "preview-title"
                components = @("TextMeshProUGUI")
                rect = [ordered]@{
                    anchorMin = "(0,1)"
                    anchorMax = "(0,1)"
                    pivot = "(0,1)"
                    anchoredPosition = "(12,-12)"
                    sizeDelta = "(160,16)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$preview.titleText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "10"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $mutedTextColor),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath)
                )
            })
        }

        if (-not [string]::IsNullOrWhiteSpace([string]$preview.iconText)) {
            $elements.Add([PSCustomObject][ordered]@{
                path = "MainScroll/Content/PreviewCard/CenterIcon"
                role = "preview-icon"
                components = @("TextMeshProUGUI")
                rect = [ordered]@{
                    anchorMin = "(0.5,0.5)"
                    anchorMax = "(0.5,0.5)"
                    pivot = "(0.5,0.5)"
                    anchoredPosition = "(0,0)"
                    sizeDelta = "(72,72)"
                }
                properties = @(
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value ([string]$preview.iconText)),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "32"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $accentTextColor),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_HorizontalAlignment" -Value "2"),
                    (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_VerticalAlignment" -Value "512")
                )
            })
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($summaryText)) {
        $elements.Add([PSCustomObject][ordered]@{
            path = "MainScroll/Content/PreviewCard/SummaryBar"
            role = "summary-card"
            components = @("Image", "HorizontalLayoutGroup", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0,0)"
                anchorMax = "(1,0)"
                pivot = "(0.5,0)"
                anchoredPosition = "(0,12)"
                sizeDelta = "(0,$summaryBarHeightPx)"
            }
            properties = @(
                (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(if ([string]::IsNullOrWhiteSpace($summaryBarColor)) { "#09090BE6" } else { $summaryBarColor })),
                (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing" -Value "8"),
                (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value "4"),
                (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value "4"),
                (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value "4"),
                (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Bottom" -Value "4"),
                (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "false"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$summaryBarHeightPx))
            )
        })

        $elements.Add([PSCustomObject][ordered]@{
            path = "MainScroll/Content/PreviewCard/SummaryBar/GaugeTrack"
            role = "summary-gauge-track"
            components = @("Image", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(0,0.5)"
                anchorMax = "(1,0.5)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,$summaryTrackHeightPx)"
            }
            properties = @(
                (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(if ([string]::IsNullOrWhiteSpace($summaryTrackColor)) { "#18181B" } else { $summaryTrackColor })),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_FlexibleWidth" -Value "1"),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$summaryTrackHeightPx))
            )
        })

        $elements.Add([PSCustomObject][ordered]@{
            path = "MainScroll/Content/PreviewCard/SummaryBar/GaugeTrack/GaugeFill"
            role = "summary-gauge-fill"
            components = @("Image")
            rect = [ordered]@{
                anchorMin = "(0,0)"
                anchorMax = "($summaryFillAnchorMaxXText,1)"
                pivot = "(0,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,0)"
            }
            properties = @(
                (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(if ([string]::IsNullOrWhiteSpace($summaryFillColor)) { "#5EB6FF" } else { $summaryFillColor }))
            )
        })

        $elements.Add([PSCustomObject][ordered]@{
            path = "MainScroll/Content/PreviewCard/SummaryBar/SummaryText"
            role = "summary-text"
            components = @("TextMeshProUGUI", "LayoutElement")
            rect = [ordered]@{
                anchorMin = "(1,0.5)"
                anchorMax = "(1,0.5)"
                pivot = "(1,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "($summaryTextWidthPx,0)"
            }
            properties = @(
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $summaryText),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value ([string]$summaryTextFontPx)),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $(if ([string]::IsNullOrWhiteSpace($summaryTextColor)) { "#5EB6FF" } else { $summaryTextColor })),
                (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath),
                (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredWidth" -Value ([string]$summaryTextWidthPx))
            )
        })
    }

    $elements.Add([PSCustomObject][ordered]@{
        path = "SaveDock"
        role = "save-dock"
        components = @("Image", "HorizontalLayoutGroup", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,0)"
            pivot = "(0.5,0)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,76)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $(if ([string]::IsNullOrWhiteSpace($saveDockColor)) { "#18181B" } else { $saveDockColor })),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "false"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value ([string]$saveDockPaddingX)),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value ([string]$saveDockPaddingX)),
            (New-Property -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value ([string]$saveDockPaddingTop)),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value "76")
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "SaveDock/PrimaryButton"
        role = "primary-cta"
        components = @("Button", "Image", "LayoutElement")
        rect = [ordered]@{
            anchorMin = "(0,0.5)"
            anchorMax = "(1,0.5)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,$saveButtonHeightPx)"
        }
        properties = @(
            (New-Property -ComponentType "Image" -PropertyName "m_Color" -Value $primaryButtonColor),
            (New-Property -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$saveButtonHeightPx))
        )
    })

    $elements.Add([PSCustomObject][ordered]@{
        path = "SaveDock/PrimaryButton/Label"
        role = "primary-cta-label"
        components = @("TextMeshProUGUI")
        rect = [ordered]@{
            anchorMin = "(0,0)"
            anchorMax = "(1,1)"
            pivot = "(0.5,0.5)"
            anchoredPosition = "(0,0)"
            sizeDelta = "(0,0)"
        }
        properties = @(
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_text" -Value $saveButtonText),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value "14"),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $primaryTextColor),
            (New-Property -ComponentType "TextMeshProUGUI" -PropertyName "m_fontAsset" -AssetReferencePath $FontAssetPath)
        )
    })

    return [ordered]@{
        schemaVersion = "1.0.0"
        contractKind = "presentation-contract"
        surfaceId = [string](Get-RequiredValue -InputObject $Profile -Name "surfaceId")
        surfaceRole = "root"
        extractionStatus = "resolved"
        sourceRefs = [ordered]@{
            imagePath = $ImagePath
            htmlPath = $HtmlPath
        }
        derivedFrom = [ordered]@{
            viewport = [string](Get-RequiredValue -InputObject $viewport -Name "label")
            headerTitle = $titleText
            primaryAction = $saveButtonText
        }
        unresolvedDerivedFields = @([string[]](Get-RequiredValue -InputObject $Profile -Name "unresolvedDerivedFields"))
        elements = @($elements.ToArray())
        notes = @(
            ("Generated from {0} source freeze." -f [System.IO.Path]::GetFileName($HtmlPath)),
            "Values in this file are source-derived from source freeze and common workspace rules."
        )
    }
}

$profile = Get-PresentationProfile -SurfaceId $SurfaceId -HtmlPath $HtmlPath -ImagePath $ImagePath -TargetAssetPath $TargetAssetPath
$defaults = Get-RequiredValue -InputObject $profile -Name "defaults"

if ([string]::IsNullOrWhiteSpace($HtmlPath)) {
    $HtmlPath = [string](Get-RequiredValue -InputObject $defaults -Name "htmlPath")
}

if ([string]::IsNullOrWhiteSpace($ImagePath)) {
    $ImagePath = [string](Get-RequiredValue -InputObject $defaults -Name "imagePath")
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = [string](Get-RequiredValue -InputObject $defaults -Name "outputPath")
}

$html = Read-AllText -PathValue $HtmlPath
$customColors = Get-CustomColors -Html $html
$fontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSansKR Dynamic.asset"
$profileKind = if ($null -ne $profile.PSObject.Properties["footer"] -and $null -ne $profile.PSObject.Properties["body"]) {
    "overlay-dialog"
}
elseif ($null -ne $profile.PSObject.Properties["workspace"]) {
    "workspace-screen"
}
else {
    ""
}
switch ($profileKind) {
    "overlay-dialog" {
        $contract = New-OverlayDialogPresentationContract `
            -Profile $profile `
            -Html $html `
            -CustomColors $customColors `
            -HtmlPath $HtmlPath `
            -ImagePath $ImagePath `
            -FontAssetPath $fontAssetPath
        break
    }
    "workspace-screen" {
        $contract = New-WorkspacePresentationContract `
            -Profile $profile `
            -CustomColors $customColors `
            -HtmlPath $HtmlPath `
            -ImagePath $ImagePath `
            -FontAssetPath $fontAssetPath
        break
    }
    default {
        throw "Unsupported presentation profile structure for '$SurfaceId'."
    }
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath) -and -not (Test-InMemoryPath -PathValue $OutputPath)) {
    $resolvedOutputPath = Resolve-RepoPath -PathValue $OutputPath
    $outputDirectory = Split-Path -Parent $resolvedOutputPath
    if (-not (Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }

    $contract | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $resolvedOutputPath -Encoding utf8
}

$contract | ConvertTo-Json -Depth 20
