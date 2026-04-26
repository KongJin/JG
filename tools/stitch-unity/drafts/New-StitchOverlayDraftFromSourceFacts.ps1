param(
    [Parameter(Mandatory = $true)][string]$SurfaceId,
    [Parameter(Mandatory = $true)][string]$TargetAssetPath,
    [string]$DraftPath = "",
    [string]$SetId = "",
    [string]$HtmlPath = "",
    [string]$ImagePath = "",
    [switch]$NoActionButton,
    [switch]$AllowHeuristicResolved
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

function Get-FirstMeaningfulText {
    param(
        [string[]]$Texts,
        [string[]]$Skip
    )

    foreach ($text in @($Texts)) {
        $candidate = ([string]$text).Trim()
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if (@($Skip) -contains $candidate) {
            continue
        }

        if ($candidate.Length -le 1) {
            continue
        }

        return $candidate
    }

    return ""
}

function New-TextProperty {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    return [PSCustomObject][ordered]@{
        componentType = "TextMeshProUGUI"
        propertyName = $Name
        value = $Value
    }
}

function New-ImageProperty {
    param([Parameter(Mandatory = $true)][string]$Color)

    return [PSCustomObject][ordered]@{
        componentType = "Image"
        propertyName = "m_Color"
        value = $Color
    }
}

function New-LayoutProperty {
    param(
        [Parameter(Mandatory = $true)][string]$ComponentType,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value
    )

    return [PSCustomObject][ordered]@{
        componentType = $ComponentType
        propertyName = $Name
        value = $Value
    }
}

$repoRoot = Get-RepoRoot
$collectorPath = Join-Path $repoRoot "tools\stitch-unity\collectors\Collect-StitchSourceFacts.ps1"
$collectorArgs = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $collectorPath,
    "-SurfaceId", $SurfaceId,
    "-TargetAssetPath", $TargetAssetPath
)
if (-not [string]::IsNullOrWhiteSpace($HtmlPath)) {
    $collectorArgs += @("-HtmlPath", $HtmlPath)
}
if (-not [string]::IsNullOrWhiteSpace($ImagePath)) {
    $collectorArgs += @("-ImagePath", $ImagePath)
}

$collectorJson = & powershell @collectorArgs
if ($LASTEXITCODE -ne 0) {
    throw "Collect-StitchSourceFacts failed for $SurfaceId."
}

$facts = ($collectorJson | Out-String) | ConvertFrom-Json
$visibleTexts = @($facts.facts.visibleTexts | ForEach-Object { [string]$_ })
$buttons = @($facts.facts.buttons | ForEach-Object { [string]$_ })
$htmlTitle = [string]$facts.source.title

if ([string]::IsNullOrWhiteSpace($SetId)) {
    if ($SurfaceId -match '^(set-[a-z])-') {
        $SetId = $Matches[1]
    }
    else {
        $SetId = "set-unknown"
    }
}

if ($PSBoundParameters.ContainsKey("Title") -or
    $PSBoundParameters.ContainsKey("PrimaryText") -or
    $PSBoundParameters.ContainsKey("BodyText")) {
    throw "Manual text overrides are not allowed in the Stitch reimport path. Generate a draft from source facts with an LLM, then pass it through -DraftPath."
}

$Title = Get-FirstMeaningfulText -Texts $visibleTexts -Skip @($htmlTitle)
if ([string]::IsNullOrWhiteSpace($Title)) {
    throw "Could not derive an overlay title from source facts for '$SurfaceId'. Use the LLM draft route instead of script-side fallback."
}

$hasActionButton = (-not $NoActionButton.IsPresent -and $buttons.Count -gt 0)
$PrimaryText = ""
if ($hasActionButton) {
    $PrimaryText = [string]$buttons[0]
}

$bodyCandidates = @(
    $visibleTexts |
        Where-Object {
            $candidate = ([string]$_).Trim()
            -not [string]::IsNullOrWhiteSpace($candidate) -and
            $candidate -ne $htmlTitle -and
            $candidate -ne $Title -and
            $candidate -ne $PrimaryText
        } |
        Select-Object -First 8
)

$BodyText = [string]::Join("`n", @($bodyCandidates))
if ([string]::IsNullOrWhiteSpace($BodyText)) {
    throw "Could not derive overlay body text from source facts for '$SurfaceId'. Use the LLM draft route instead of script-side fallback."
}

if ([string]::IsNullOrWhiteSpace($DraftPath)) {
    $DraftPath = "Temp/StitchDraftRoute/$SurfaceId-draft.json"
}

$htmlPath = [string]$facts.source.htmlPath
$imagePath = [string]$facts.source.imagePath
$extractionStatus = if ($AllowHeuristicResolved.IsPresent) { "resolved" } else { "pending-source-derivation" }
$unresolvedDerivedFields = if ($AllowHeuristicResolved.IsPresent) {
    @()
}
else {
    @(
        "semantic block priority requires LLM/source-derived review",
        "presentation values are heuristic skeleton defaults",
        "primary CTA may not be the source primary action"
    )
}
$primaryBlockRole = if ($hasActionButton) { "cta" } else { "status" }
$primaryHostPath = if ($hasActionButton) { "DialogPanel/PrimaryButton" } else { "DialogPanel/PrimaryStatus" }
$primaryComponents = if ($hasActionButton) { @("Button", "Image", "LayoutElement") } else { @("TextMeshProUGUI", "LayoutElement") }
$primaryPresentation = if ($hasActionButton) {
    @(
        [PSCustomObject][ordered]@{
            path = "DialogPanel/PrimaryButton"
            components = @("Button", "ButtonSoundEmitter", "Image", "LayoutElement")
            properties = @(
                [PSCustomObject][ordered]@{
                    componentType = "ButtonSoundEmitter"
                    propertyName = "soundKey"
                    value = "ui_confirm"
                },
                (New-ImageProperty -Color "#F59E0BFF"),
                (New-LayoutProperty -ComponentType "LayoutElement" -Name "m_PreferredHeight" -Value "40")
            )
        },
        [PSCustomObject][ordered]@{
            path = "DialogPanel/PrimaryButton/Label"
            components = @("TextMeshProUGUI")
            rect = [PSCustomObject][ordered]@{
                anchorMin = "(0,0)"
                anchorMax = "(1,1)"
                pivot = "(0.5,0.5)"
                anchoredPosition = "(0,0)"
                sizeDelta = "(0,0)"
            }
            properties = @(
                (New-TextProperty -Name "m_text" -Value $PrimaryText),
                (New-TextProperty -Name "m_fontSize" -Value "13"),
                (New-TextProperty -Name "m_fontColor" -Value "#020617FF"),
                (New-TextProperty -Name "m_HorizontalAlignment" -Value "2"),
                (New-TextProperty -Name "m_VerticalAlignment" -Value "512")
            )
        }
    )
}
else {
    @(
        [PSCustomObject][ordered]@{
            path = "DialogPanel/PrimaryStatus"
            components = @("TextMeshProUGUI", "LayoutElement")
            properties = @(
                (New-TextProperty -Name "m_text" -Value $PrimaryText),
                (New-TextProperty -Name "m_fontSize" -Value "13"),
                (New-TextProperty -Name "m_fontColor" -Value "#F59E0BFF"),
                (New-LayoutProperty -ComponentType "LayoutElement" -Name "m_PreferredHeight" -Value "28")
            )
        }
    )
}

$draft = [PSCustomObject][ordered]@{
    schemaVersion = "1.0.0"
    artifactKind = "stitch-contract-draft"
    surfaceId = $SurfaceId
    source = [PSCustomObject][ordered]@{
        htmlPath = $htmlPath
        imagePath = $imagePath
    }
    target = [PSCustomObject][ordered]@{
        kind = "prefab"
        assetPath = $TargetAssetPath
    }
    contracts = [PSCustomObject][ordered]@{
        manifest = [PSCustomObject][ordered]@{
            schemaVersion = "1.1.0"
            contractKind = "screen-manifest"
            setId = $SetId
            surfaceId = $SurfaceId
            surfaceRole = "overlay"
            status = "accepted"
            source = [PSCustomObject][ordered]@{
                tool = "stitch"
                sourceRef = $SurfaceId
                projectId = ""
                screenId = $SurfaceId
                url = $imagePath
            }
            ctaPriority = @(
                [PSCustomObject][ordered]@{
                    id = "primary-action"
                    priority = "primary"
                    outcome = if ($hasActionButton) { "primary-action" } else { "primary-status" }
                }
            )
            states = [PSCustomObject][ordered]@{
                default = $true
                empty = $false
                loading = ($SurfaceId -match "loading")
                error = ($SurfaceId -match "error|defeat|warning")
                selected = $false
                disabled = $false
            }
            blocks = @(
                [PSCustomObject][ordered]@{
                    blockId = "overlay-shell"
                    role = "dialog"
                    sourceName = $Title
                    children = @("dialog-header", "dialog-body", "primary-action")
                },
                [PSCustomObject][ordered]@{
                    blockId = "dialog-header"
                    role = "header"
                    sourceName = $Title
                    children = @()
                },
                [PSCustomObject][ordered]@{
                    blockId = "dialog-body"
                    role = "content"
                    sourceName = "source-visible-text-summary"
                    children = @()
                },
                [PSCustomObject][ordered]@{
                    blockId = "primary-action"
                    role = $primaryBlockRole
                    sourceName = $PrimaryText
                    children = @()
                }
            )
            validation = [PSCustomObject][ordered]@{
                firstReadOrder = @("dialog-header", "dialog-body", "primary-action")
                requiredChecks = @("title-readable", "body-visible", "primary-action-or-status-visible")
            }
            notes = @(
                "Generated from source facts using generic overlay draft route.",
                "Visual fidelity judgment remains separate from mechanical translation verdict."
            )
        }
        map = [PSCustomObject][ordered]@{
            schemaVersion = "1.0.0"
            contractKind = "unity-surface-map"
            surfaceId = $SurfaceId
            targetKind = "overlay-root"
            target = [PSCustomObject][ordered]@{
                kind = "prefab"
                assetPath = $TargetAssetPath
            }
            contractRefs = [PSCustomObject][ordered]@{
                manifestPath = "in-memory://draft/$SurfaceId/screen-manifest"
                presentationPath = "in-memory://draft/$SurfaceId/presentation-contract"
            }
            translationStrategy = "contract-complete-translator-v1"
            strategyMode = "generate-or-patch"
            artifactPaths = [PSCustomObject][ordered]@{
                pipelineResult = "artifacts/unity/$SurfaceId-pipeline-result.json"
            }
            blocks = [PSCustomObject][ordered]@{
                "overlay-shell" = [PSCustomObject][ordered]@{
                    hostPath = "DialogPanel"
                    requiredComponents = @("Image", "VerticalLayoutGroup")
                }
                "dialog-header" = [PSCustomObject][ordered]@{
                    hostPath = "DialogPanel/HeaderText"
                    requiredComponents = @("TextMeshProUGUI", "LayoutElement")
                }
                "dialog-body" = [PSCustomObject][ordered]@{
                    hostPath = "DialogPanel/BodyText"
                    requiredComponents = @("TextMeshProUGUI", "LayoutElement")
                }
                "primary-action" = [PSCustomObject][ordered]@{
                    hostPath = $primaryHostPath
                    requiredComponents = $primaryComponents
                }
            }
            reviewRoute = [PSCustomObject][ordered]@{
                routeId = "surface-review"
                kind = "temp-scene-sceneview"
                menuPath = "Tools/Scene/Prepare Stitch Runtime Review/Surface"
            }
        }
        presentation = [PSCustomObject][ordered]@{
            schemaVersion = "1.0.0"
            contractKind = "presentation-contract"
            surfaceId = $SurfaceId
            surfaceRole = "overlay"
            extractionStatus = $extractionStatus
            sourceRefs = [PSCustomObject][ordered]@{
                imagePath = $imagePath
                htmlPath = $htmlPath
            }
            derivedFrom = [PSCustomObject][ordered]@{
                viewport = "390x844 mobile-first"
                htmlTitle = $htmlTitle
                title = $Title
                primary = $PrimaryText
            }
            unresolvedDerivedFields = @($unresolvedDerivedFields)
            elements = @(
                [PSCustomObject][ordered]@{
                    path = ""
                    components = @("Image")
                    rect = [PSCustomObject][ordered]@{
                        anchorMin = "(0,0)"
                        anchorMax = "(1,1)"
                        pivot = "(0.5,0.5)"
                        anchoredPosition = "(0,0)"
                        sizeDelta = "(0,0)"
                    }
                    properties = @(
                        (New-ImageProperty -Color "#00000000")
                    )
                },
                [PSCustomObject][ordered]@{
                    path = "DialogPanel"
                    components = @("Image", "VerticalLayoutGroup")
                    rect = [PSCustomObject][ordered]@{
                        anchorMin = "(0.5,0.5)"
                        anchorMax = "(0.5,0.5)"
                        pivot = "(0.5,0.5)"
                        anchoredPosition = "(0,0)"
                        sizeDelta = "(330,280)"
                    }
                    properties = @(
                        (New-ImageProperty -Color "#0F172AFF"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_Spacing" -Value "12"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_ChildControlWidth" -Value "true"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_ChildControlHeight" -Value "false"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_ChildForceExpandWidth" -Value "true"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_ChildForceExpandHeight" -Value "false"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_Padding.m_Left" -Value "18"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_Padding.m_Right" -Value "18"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_Padding.m_Top" -Value "18"),
                        (New-LayoutProperty -ComponentType "VerticalLayoutGroup" -Name "m_Padding.m_Bottom" -Value "18")
                    )
                },
                [PSCustomObject][ordered]@{
                    path = "DialogPanel/HeaderText"
                    components = @("TextMeshProUGUI", "LayoutElement")
                    properties = @(
                        (New-TextProperty -Name "m_text" -Value $Title),
                        (New-TextProperty -Name "m_fontSize" -Value "18"),
                        (New-TextProperty -Name "m_fontColor" -Value "#F8FAFCFF"),
                        (New-LayoutProperty -ComponentType "LayoutElement" -Name "m_PreferredHeight" -Value "42")
                    )
                },
                [PSCustomObject][ordered]@{
                    path = "DialogPanel/BodyText"
                    components = @("TextMeshProUGUI", "LayoutElement")
                    properties = @(
                        (New-TextProperty -Name "m_text" -Value $BodyText),
                        (New-TextProperty -Name "m_fontSize" -Value "12"),
                        (New-TextProperty -Name "m_fontColor" -Value "#CBD5E1FF"),
                        (New-LayoutProperty -ComponentType "LayoutElement" -Name "m_PreferredHeight" -Value "130")
                    )
                }
            ) + $primaryPresentation
        }
    }
    notes = @(
        "Generated heuristic draft from source facts.",
        "Do not use this as an active translation-ready contract unless AllowHeuristicResolved was explicitly set for a debug lane.",
        "Production reimport should use Collect-StitchSourceFacts followed by an LLM-authored draft and Test-StitchContractDraft."
    )
}

$resolvedDraftPath = Resolve-RepoPath -PathValue $DraftPath
$draftDirectory = [System.IO.Path]::GetDirectoryName($resolvedDraftPath)
if (-not (Test-Path -LiteralPath $draftDirectory)) {
    New-Item -ItemType Directory -Path $draftDirectory -Force | Out-Null
}

$draft | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $resolvedDraftPath -Encoding UTF8

[PSCustomObject][ordered]@{
    schemaVersion = "1.0.0"
    success = $true
    surfaceId = $SurfaceId
    draftPath = $DraftPath
    targetAssetPath = $TargetAssetPath
    title = $Title
    primaryText = $PrimaryText
    hasActionButton = $hasActionButton
} | ConvertTo-Json -Depth 10
