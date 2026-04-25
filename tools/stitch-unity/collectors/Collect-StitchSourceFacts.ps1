param(
    [Parameter(Mandatory = $true)][string]$SurfaceId,
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

function Convert-ToRepoRelativePath {
    param([Parameter(Mandatory = $true)][string]$AbsolutePath)

    $repoRoot = (Get-RepoRoot).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $fullPath = [System.IO.Path]::GetFullPath($AbsolutePath)
    if ($fullPath.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $fullPath.Substring($repoRoot.Length).Replace('\', '/')
    }

    return $fullPath.Replace('\', '/')
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

function Get-HtmlTitleText {
    param([Parameter(Mandatory = $true)][string]$Html)

    $titleMatch = [regex]::Match($Html, '<title>\s*(?<text>.*?)\s*</title>', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $titleMatch.Success) {
        return ""
    }

    return ([System.Net.WebUtility]::HtmlDecode([string]$titleMatch.Groups["text"].Value)).Trim()
}

function Resolve-SourcePath {
    param(
        [Parameter(Mandatory = $true)][string]$SurfaceId,
        [Parameter(Mandatory = $true)][string]$Extension,
        [string]$ExplicitPath = ""
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        $resolved = Resolve-RepoPath -PathValue $ExplicitPath
        if (-not (Test-Path -LiteralPath $resolved)) {
            throw "Source file not found: $resolved"
        }

        return Convert-ToRepoRelativePath -AbsolutePath $resolved
    }

    $designRoot = Resolve-RepoPath -PathValue ".stitch/designs"
    if (-not (Test-Path -LiteralPath $designRoot)) {
        throw "Design source directory not found: $designRoot"
    }

    $surfaceSlug = Convert-ToSurfaceSlug -Value $SurfaceId
    $candidates = @(
        Get-ChildItem -LiteralPath $designRoot -Filter "*.$Extension" -File |
            Where-Object {
                (Convert-ToSurfaceSlug -Value $_.BaseName) -eq $surfaceSlug -or
                (Convert-ToSurfaceSlug -Value $_.BaseName).Contains($surfaceSlug)
            } |
            Sort-Object -Property Name
    )

    if ($candidates.Count -eq 0) {
        throw "Could not resolve '.$Extension' source for surface '$SurfaceId' under '$designRoot'."
    }

    return Convert-ToRepoRelativePath -AbsolutePath $candidates[0].FullName
}

function Read-SourceHtml {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $resolvedPath = Resolve-RepoPath -PathValue $PathValue
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "HTML source not found: $resolvedPath"
    }

    return Get-Content -LiteralPath $resolvedPath -Raw
}

function Clean-InnerText {
    param([AllowEmptyString()][string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return ""
    }

    $withoutScript = [regex]::Replace($Value, '<script\b[^>]*>.*?</script>', ' ', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $withoutStyle = [regex]::Replace($withoutScript, '<style\b[^>]*>.*?</style>', ' ', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $withoutSvg = [regex]::Replace($withoutStyle, '<svg\b[^>]*>.*?</svg>', ' ', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $withoutTags = [regex]::Replace($withoutSvg, '<[^>]+>', ' ')
    $decoded = [System.Net.WebUtility]::HtmlDecode($withoutTags)
    return ([regex]::Replace($decoded, '\s+', ' ')).Trim()
}

function Add-UniqueString {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[string]]$List,
        [AllowEmptyString()][string]$Value
    )

    $clean = ([string]$Value).Trim()
    if ([string]::IsNullOrWhiteSpace($clean)) {
        return
    }

    if (-not $List.Contains($clean)) {
        $List.Add($clean)
    }
}

function Get-ClassTokens {
    param([Parameter(Mandatory = $true)][string]$Html)

    $tokens = New-Object System.Collections.Generic.List[string]
    foreach ($match in [regex]::Matches($Html, '\bclass=["''](?<class>[^"'']+)["'']', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        foreach ($token in ([string]$match.Groups["class"].Value -split '\s+')) {
            Add-UniqueString -List $tokens -Value $token
        }
    }

    return @($tokens.ToArray())
}

function Get-VisibleTexts {
    param([Parameter(Mandatory = $true)][string]$Html)

    $texts = New-Object System.Collections.Generic.List[string]
    $body = [regex]::Replace($Html, '<script\b[^>]*>.*?</script>', ' ', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $body = [regex]::Replace($body, '<style\b[^>]*>.*?</style>', ' ', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

    foreach ($match in [regex]::Matches($body, '>(?<text>[^<>]+)<', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        $text = Clean-InnerText -Value ([string]$match.Groups["text"].Value)
        if ($text.Length -gt 1 -and -not ($text -match '^[{}();,.\[\]:]+$')) {
            Add-UniqueString -List $texts -Value $text
        }
    }

    return @($texts.ToArray())
}

function Get-ButtonTexts {
    param([Parameter(Mandatory = $true)][string]$Html)

    $buttons = New-Object System.Collections.Generic.List[string]
    $pattern = '<button\b[^>]*>(?<content>.*?)</button>'
    foreach ($match in [regex]::Matches($Html, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        Add-UniqueString -List $buttons -Value (Clean-InnerText -Value ([string]$match.Groups["content"].Value))
    }

    foreach ($match in [regex]::Matches($Html, '\brole=["'']button["''][^>]*>(?<content>.*?)</[^>]+>', [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        Add-UniqueString -List $buttons -Value (Clean-InnerText -Value ([string]$match.Groups["content"].Value))
    }

    return @($buttons.ToArray())
}

function Get-MaterialIcons {
    param([Parameter(Mandatory = $true)][string]$Html)

    $icons = New-Object System.Collections.Generic.List[string]
    $pattern = '<[^>]*class=["''][^"'']*material-symbols-outlined[^"'']*["''][^>]*>(?<content>.*?)</[^>]+>'
    foreach ($match in [regex]::Matches($Html, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        Add-UniqueString -List $icons -Value (Clean-InnerText -Value ([string]$match.Groups["content"].Value))
    }

    return @($icons.ToArray())
}

function Get-InputCandidates {
    param([Parameter(Mandatory = $true)][string]$Html)

    $inputs = New-Object System.Collections.Generic.List[object]
    foreach ($match in [regex]::Matches($Html, '<(?<tag>input|select|textarea)\b(?<attrs>[^>]*)>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
        $attrs = [string]$match.Groups["attrs"].Value
        $candidate = [ordered]@{
            tag = ([string]$match.Groups["tag"].Value).ToLowerInvariant()
            type = ""
            name = ""
            placeholder = ""
            ariaLabel = ""
            id = ""
        }

        foreach ($attrName in @("type", "name", "placeholder", "aria-label", "id")) {
            $attrMatch = [regex]::Match($attrs, ('\b{0}=["''](?<value>[^"'']*)["'']' -f [regex]::Escape($attrName)), [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($attrMatch.Success) {
                $propertyName = if ($attrName -eq "aria-label") { "ariaLabel" } else { $attrName }
                $candidate[$propertyName] = [System.Net.WebUtility]::HtmlDecode([string]$attrMatch.Groups["value"].Value)
            }
        }

        $inputs.Add([PSCustomObject]$candidate)
    }

    return @($inputs.ToArray())
}

function Get-RepeatedClassCandidates {
    param([Parameter(Mandatory = $true)][string]$Html)

    $groups = @{}
    foreach ($match in [regex]::Matches($Html, '<(?<tag>[a-zA-Z0-9]+)\b(?<attrs>[^>]*)class=["''](?<class>[^"'']+)["''][^>]*>', [System.Text.RegularExpressions.RegexOptions]::Singleline)) {
        $classText = [regex]::Replace(([string]$match.Groups["class"].Value).Trim(), '\s+', ' ')
        if ([string]::IsNullOrWhiteSpace($classText)) {
            continue
        }

        $signatureTokens = @($classText -split '\s+' | Where-Object {
            $_ -match '^(flex|grid|rounded|border|bg-|text-|p[trblxy]?-|m[trblxy]?-|gap-|space-[xy]-|h-|w-|min-h-|max-w-|items-|justify-|overflow)'
        })
        $signature = if ($signatureTokens.Count -gt 0) {
            [string]::Join(' ', @($signatureTokens | Select-Object -First 12))
        }
        else {
            $classText
        }

        if (-not $groups.ContainsKey($signature)) {
            $groups[$signature] = 0
        }

        $groups[$signature] = [int]$groups[$signature] + 1
    }

    $result = New-Object System.Collections.Generic.List[object]
    foreach ($key in $groups.Keys) {
        $count = [int]$groups[$key]
        if ($count -lt 2) {
            continue
        }

        $result.Add([PSCustomObject][ordered]@{
            classSignature = $key
            count = $count
        })
    }

    return @($result.ToArray() | Sort-Object -Property @{ Expression = "count"; Descending = $true }, classSignature | Select-Object -First 12)
}

function Get-TokenSubset {
    param(
        [string[]]$Tokens,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    return @($Tokens | Where-Object { $_ -match $Pattern } | Select-Object -Unique)
}

function Get-ImageInfo {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $resolvedPath = Resolve-RepoPath -PathValue $PathValue
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        return [PSCustomObject]@{
            exists = $false
            width = 0
            height = 0
            fileSizeBytes = 0
        }
    }

    $file = Get-Item -LiteralPath $resolvedPath
    $width = 0
    $height = 0
    try {
        Add-Type -AssemblyName System.Drawing
        $image = [System.Drawing.Image]::FromFile($resolvedPath)
        try {
            $width = [int]$image.Width
            $height = [int]$image.Height
        }
        finally {
            $image.Dispose()
        }
    }
    catch {
        $width = 0
        $height = 0
    }

    return [PSCustomObject]@{
        exists = $true
        width = $width
        height = $height
        fileSizeBytes = [int64]$file.Length
    }
}

function Write-Json {
    param(
        [Parameter(Mandatory = $true)][object]$InputObject,
        [string]$PathValue = ""
    )

    $json = $InputObject | ConvertTo-Json -Depth 20
    if (-not [string]::IsNullOrWhiteSpace($PathValue)) {
        $resolvedPath = Resolve-RepoPath -PathValue $PathValue
        $directoryPath = Split-Path -Parent $resolvedPath
        if (-not [string]::IsNullOrWhiteSpace($directoryPath) -and -not (Test-Path -LiteralPath $directoryPath)) {
            New-Item -ItemType Directory -Path $directoryPath -Force | Out-Null
        }

        [System.IO.File]::WriteAllText($resolvedPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
    }

    $json
}

$resolvedHtmlPath = Resolve-SourcePath -SurfaceId $SurfaceId -Extension "html" -ExplicitPath $HtmlPath
$resolvedImagePath = Resolve-SourcePath -SurfaceId $SurfaceId -Extension "png" -ExplicitPath $ImagePath
$html = Read-SourceHtml -PathValue $resolvedHtmlPath
$classTokens = @(Get-ClassTokens -Html $html)
$imageInfo = Get-ImageInfo -PathValue $resolvedImagePath

$facts = [PSCustomObject][ordered]@{
    schemaVersion = "1.0.0"
    artifactKind = "stitch-source-facts"
    surfaceId = $SurfaceId
    source = [PSCustomObject][ordered]@{
        htmlPath = $resolvedHtmlPath
        imagePath = $resolvedImagePath
        title = Get-HtmlTitleText -Html $html
        image = $imageInfo
    }
    target = [PSCustomObject][ordered]@{
        kind = "prefab"
        assetPath = $TargetAssetPath
    }
    facts = [PSCustomObject][ordered]@{
        visibleTexts = @(Get-VisibleTexts -Html $html | Select-Object -First 80)
        buttons = @(Get-ButtonTexts -Html $html)
        icons = @(Get-MaterialIcons -Html $html)
        inputs = @(Get-InputCandidates -Html $html)
        repeatedCandidates = @(Get-RepeatedClassCandidates -Html $html)
        colorTokens = @(Get-TokenSubset -Tokens $classTokens -Pattern '^(bg|text|border|from|to|via)-' | Where-Object { $_ -notmatch '^text-(xs|sm|base|lg|xl|2xl|3xl|\[[0-9]+px\])$' })
        layoutHints = @(Get-TokenSubset -Tokens $classTokens -Pattern '^(fixed|sticky|absolute|relative|grid|flex|inline-flex|overflow|space-[xy]-|gap-|grid-cols-|col-span-|row-span-|items-|justify-|content-|h-|w-|min-h-|max-h-|min-w-|max-w-|p[trblxy]?-)')
    }
    request = [PSCustomObject][ordered]@{
        task = "Create a Unity translation contract draft from these Stitch source facts and the paired screenshot."
        requiredOutput = "manifest/map/presentation contract draft"
        instructions = @(
            "Use source facts and screenshot evidence for semantic block and CTA judgment.",
            "Do not invent runtime wiring.",
            "Leave uncertain fields explicit instead of hiding them."
        )
    }
    generatedAt = (Get-Date).ToString("o")
}

Write-Json -InputObject $facts -PathValue $OutputPath
