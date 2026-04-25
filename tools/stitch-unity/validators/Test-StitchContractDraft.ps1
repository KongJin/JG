param(
    [Parameter(Mandatory = $true)][string]$DraftPath,
    [string]$SurfaceId = "",
    [string]$TargetAssetPath = ""
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

function Read-Json {
    param([Parameter(Mandatory = $true)][string]$PathValue)

    $resolvedPath = Resolve-RepoPath -PathValue $PathValue
    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "Draft JSON not found: $resolvedPath"
    }

    return Get-Content -LiteralPath $resolvedPath -Raw | ConvertFrom-Json
}

function Add-Issue {
    param(
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Issues,
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][string]$Message,
        [string]$Path = ""
    )

    $Issues.Add([PSCustomObject][ordered]@{
        code = $Code
        message = $Message
        path = $Path
    })
}

function Test-HasProperty {
    param(
        [object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name
    )

    return ($null -ne $InputObject -and $null -ne $InputObject.PSObject.Properties[$Name])
}

function Get-Value {
    param(
        [object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not (Test-HasProperty -InputObject $InputObject -Name $Name)) {
        return $null
    }

    return $InputObject.PSObject.Properties[$Name].Value
}

function Test-NonEmptyString {
    param([object]$Value)

    return ($null -ne $Value -and -not [string]::IsNullOrWhiteSpace([string]$Value))
}

function Test-RequiredString {
    param(
        [object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Issues,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $value = Get-Value -InputObject $InputObject -Name $Name
    if (-not (Test-NonEmptyString -Value $value)) {
        Add-Issue -Issues $Issues -Code "missing-required-string" -Message "Required string '$Name' is missing or empty." -Path $Path
        return ""
    }

    return [string]$value
}

function Test-ExpectedValue {
    param(
        [object]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Expected,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Issues,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $actual = Test-RequiredString -InputObject $InputObject -Name $Name -Issues $Issues -Path $Path
    if (-not [string]::IsNullOrWhiteSpace($actual) -and $actual -ne $Expected) {
        Add-Issue -Issues $Issues -Code "unexpected-value" -Message "Expected '$Name' to be '$Expected' but got '$actual'." -Path $Path
    }
}

function Get-ArrayValue {
    param([object]$Value)

    if ($null -eq $Value) {
        return @()
    }

    return @($Value)
}

function Get-MapBlockIds {
    param([object]$MapBlocks)

    if ($null -eq $MapBlocks) {
        return @()
    }

    return @($MapBlocks.PSObject.Properties | ForEach-Object { [string]$_.Name })
}

function Test-DuplicateValues {
    param(
        [string[]]$Values,
        [Parameter(Mandatory = $true)][string]$IssueCode,
        [Parameter(Mandatory = $true)][string]$MessagePrefix,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][AllowEmptyCollection()][System.Collections.Generic.List[object]]$Issues
    )

    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
    $duplicates = New-Object System.Collections.Generic.List[string]
    foreach ($value in @($Values)) {
        if ([string]::IsNullOrWhiteSpace($value)) {
            continue
        }

        if (-not $seen.Add($value) -and -not $duplicates.Contains($value)) {
            $duplicates.Add($value)
        }
    }

    if ($duplicates.Count -gt 0) {
        Add-Issue -Issues $Issues -Code $IssueCode -Message ("{0}: {1}" -f $MessagePrefix, [string]::Join(', ', @($duplicates.ToArray()))) -Path $Path
    }
}

function Test-ContractDraft {
    param(
        [Parameter(Mandatory = $true)][object]$Draft,
        [string]$ExpectedSurfaceId = "",
        [string]$ExpectedTargetAssetPath = ""
    )

    $issues = New-Object System.Collections.Generic.List[object]

    Test-ExpectedValue -InputObject $Draft -Name "schemaVersion" -Expected "1.0.0" -Issues $issues -Path "schemaVersion"
    Test-ExpectedValue -InputObject $Draft -Name "artifactKind" -Expected "stitch-contract-draft" -Issues $issues -Path "artifactKind"
    $draftSurfaceId = Test-RequiredString -InputObject $Draft -Name "surfaceId" -Issues $issues -Path "surfaceId"

    if (-not [string]::IsNullOrWhiteSpace($ExpectedSurfaceId) -and $draftSurfaceId -ne $ExpectedSurfaceId) {
        Add-Issue -Issues $issues -Code "surface-id-mismatch" -Message "Draft surfaceId '$draftSurfaceId' does not match expected '$ExpectedSurfaceId'." -Path "surfaceId"
    }

    $source = Get-Value -InputObject $Draft -Name "source"
    if ($null -eq $source) {
        Add-Issue -Issues $issues -Code "missing-source" -Message "Draft source is missing." -Path "source"
    }
    else {
        Test-RequiredString -InputObject $source -Name "htmlPath" -Issues $issues -Path "source.htmlPath" | Out-Null
        Test-RequiredString -InputObject $source -Name "imagePath" -Issues $issues -Path "source.imagePath" | Out-Null
    }

    $target = Get-Value -InputObject $Draft -Name "target"
    $targetAssetPath = ""
    if ($null -eq $target) {
        Add-Issue -Issues $issues -Code "missing-target" -Message "Draft target is missing." -Path "target"
    }
    else {
        Test-ExpectedValue -InputObject $target -Name "kind" -Expected "prefab" -Issues $issues -Path "target.kind"
        $targetAssetPath = Test-RequiredString -InputObject $target -Name "assetPath" -Issues $issues -Path "target.assetPath"
        if (-not [string]::IsNullOrWhiteSpace($ExpectedTargetAssetPath) -and $targetAssetPath -ne $ExpectedTargetAssetPath) {
            Add-Issue -Issues $issues -Code "target-asset-path-mismatch" -Message "Draft target assetPath '$targetAssetPath' does not match expected '$ExpectedTargetAssetPath'." -Path "target.assetPath"
        }
    }

    $contracts = Get-Value -InputObject $Draft -Name "contracts"
    if ($null -eq $contracts) {
        Add-Issue -Issues $issues -Code "missing-contracts" -Message "Draft contracts object is missing." -Path "contracts"
        return $issues
    }

    $manifest = Get-Value -InputObject $contracts -Name "manifest"
    $map = Get-Value -InputObject $contracts -Name "map"
    $presentation = Get-Value -InputObject $contracts -Name "presentation"
    if ($null -eq $manifest) {
        Add-Issue -Issues $issues -Code "missing-manifest" -Message "contracts.manifest is missing." -Path "contracts.manifest"
    }
    if ($null -eq $map) {
        Add-Issue -Issues $issues -Code "missing-map" -Message "contracts.map is missing." -Path "contracts.map"
    }
    if ($null -eq $presentation) {
        Add-Issue -Issues $issues -Code "missing-presentation" -Message "contracts.presentation is missing." -Path "contracts.presentation"
    }
    if ($null -eq $manifest -or $null -eq $map -or $null -eq $presentation) {
        return $issues
    }

    Test-ExpectedValue -InputObject $manifest -Name "contractKind" -Expected "screen-manifest" -Issues $issues -Path "contracts.manifest.contractKind"
    Test-ExpectedValue -InputObject $map -Name "contractKind" -Expected "unity-surface-map" -Issues $issues -Path "contracts.map.contractKind"
    Test-ExpectedValue -InputObject $presentation -Name "contractKind" -Expected "presentation-contract" -Issues $issues -Path "contracts.presentation.contractKind"

    $manifestSurfaceId = Test-RequiredString -InputObject $manifest -Name "surfaceId" -Issues $issues -Path "contracts.manifest.surfaceId"
    $mapSurfaceId = Test-RequiredString -InputObject $map -Name "surfaceId" -Issues $issues -Path "contracts.map.surfaceId"
    $presentationSurfaceId = Test-RequiredString -InputObject $presentation -Name "surfaceId" -Issues $issues -Path "contracts.presentation.surfaceId"
    foreach ($candidate in @($manifestSurfaceId, $mapSurfaceId, $presentationSurfaceId)) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and -not [string]::IsNullOrWhiteSpace($draftSurfaceId) -and $candidate -ne $draftSurfaceId) {
            Add-Issue -Issues $issues -Code "contract-surface-id-mismatch" -Message "Contract surfaceId '$candidate' does not match draft surfaceId '$draftSurfaceId'." -Path "contracts"
        }
    }

    $manifestBlocks = @(Get-ArrayValue -Value (Get-Value -InputObject $manifest -Name "blocks"))
    if ($manifestBlocks.Count -eq 0) {
        Add-Issue -Issues $issues -Code "manifest-has-no-blocks" -Message "contracts.manifest.blocks must contain at least one block." -Path "contracts.manifest.blocks"
    }

    $ctaPriority = @(Get-ArrayValue -Value (Get-Value -InputObject $manifest -Name "ctaPriority"))
    if ($ctaPriority.Count -eq 0) {
        Add-Issue -Issues $issues -Code "missing-cta-priority" -Message "contracts.manifest.ctaPriority must contain at least one CTA." -Path "contracts.manifest.ctaPriority"
    }
    elseif (@($ctaPriority | Where-Object { [string](Get-Value -InputObject $_ -Name "priority") -eq "primary" }).Count -eq 0) {
        Add-Issue -Issues $issues -Code "missing-primary-cta" -Message "contracts.manifest.ctaPriority must contain one primary CTA." -Path "contracts.manifest.ctaPriority"
    }

    $manifestBlockIds = @()
    foreach ($block in $manifestBlocks) {
        $blockId = Test-RequiredString -InputObject $block -Name "blockId" -Issues $issues -Path "contracts.manifest.blocks[].blockId"
        Test-RequiredString -InputObject $block -Name "role" -Issues $issues -Path "contracts.manifest.blocks[].role" | Out-Null
        Test-RequiredString -InputObject $block -Name "sourceName" -Issues $issues -Path "contracts.manifest.blocks[].sourceName" | Out-Null
        if (-not [string]::IsNullOrWhiteSpace($blockId)) {
            $manifestBlockIds += $blockId
        }
    }
    Test-DuplicateValues -Values $manifestBlockIds -IssueCode "duplicate-manifest-block-id" -MessagePrefix "Duplicate manifest blockId" -Path "contracts.manifest.blocks" -Issues $issues

    $mapTarget = Get-Value -InputObject $map -Name "target"
    if ($null -eq $mapTarget) {
        Add-Issue -Issues $issues -Code "missing-map-target" -Message "contracts.map.target is missing." -Path "contracts.map.target"
    }
    else {
        $mapTargetAssetPath = Test-RequiredString -InputObject $mapTarget -Name "assetPath" -Issues $issues -Path "contracts.map.target.assetPath"
        if (-not [string]::IsNullOrWhiteSpace($targetAssetPath) -and $mapTargetAssetPath -ne $targetAssetPath) {
            Add-Issue -Issues $issues -Code "map-target-mismatch" -Message "contracts.map.target.assetPath does not match draft target assetPath." -Path "contracts.map.target.assetPath"
        }
    }

    Test-RequiredString -InputObject $map -Name "translationStrategy" -Issues $issues -Path "contracts.map.translationStrategy" | Out-Null
    Test-RequiredString -InputObject $map -Name "strategyMode" -Issues $issues -Path "contracts.map.strategyMode" | Out-Null

    $mapBlocks = Get-Value -InputObject $map -Name "blocks"
    $mapBlockIds = @(Get-MapBlockIds -MapBlocks $mapBlocks)
    if ($mapBlockIds.Count -eq 0) {
        Add-Issue -Issues $issues -Code "map-has-no-blocks" -Message "contracts.map.blocks must contain at least one block binding." -Path "contracts.map.blocks"
    }

    $hostPaths = @()
    if ($null -ne $mapBlocks) {
        foreach ($property in @($mapBlocks.PSObject.Properties)) {
            $mapping = $property.Value
            $hostPath = Test-RequiredString -InputObject $mapping -Name "hostPath" -Issues $issues -Path ("contracts.map.blocks.{0}.hostPath" -f $property.Name)
            if (-not [string]::IsNullOrWhiteSpace($hostPath)) {
                $hostPaths += $hostPath
            }
        }
    }
    Test-DuplicateValues -Values $hostPaths -IssueCode "duplicate-host-path" -MessagePrefix "Duplicate hostPath" -Path "contracts.map.blocks" -Issues $issues

    $missingInMap = @($manifestBlockIds | Where-Object { $mapBlockIds -notcontains $_ })
    if ($missingInMap.Count -gt 0) {
        Add-Issue -Issues $issues -Code "manifest-blocks-missing-from-map" -Message ("Manifest blocks missing map bindings: {0}" -f [string]::Join(', ', $missingInMap)) -Path "contracts.map.blocks"
    }

    $extraInMap = @($mapBlockIds | Where-Object { $manifestBlockIds -notcontains $_ })
    if ($extraInMap.Count -gt 0) {
        Add-Issue -Issues $issues -Code "map-blocks-not-declared-in-manifest" -Message ("Map blocks not declared in manifest: {0}" -f [string]::Join(', ', $extraInMap)) -Path "contracts.map.blocks"
    }

    $extractionStatus = Test-RequiredString -InputObject $presentation -Name "extractionStatus" -Issues $issues -Path "contracts.presentation.extractionStatus"
    if (-not [string]::IsNullOrWhiteSpace($extractionStatus) -and $extractionStatus -ne "resolved") {
        Add-Issue -Issues $issues -Code "presentation-not-resolved" -Message "contracts.presentation.extractionStatus must be 'resolved' before translation." -Path "contracts.presentation.extractionStatus"
    }

    $elements = @(Get-ArrayValue -Value (Get-Value -InputObject $presentation -Name "elements"))
    if ($elements.Count -eq 0) {
        Add-Issue -Issues $issues -Code "presentation-has-no-elements" -Message "contracts.presentation.elements must contain at least one element." -Path "contracts.presentation.elements"
    }

    $elementPaths = @()
    foreach ($element in $elements) {
        if (-not (Test-HasProperty -InputObject $element -Name "path")) {
            Add-Issue -Issues $issues -Code "presentation-element-missing-path" -Message "Every presentation element must declare path." -Path "contracts.presentation.elements[].path"
            continue
        }

        $path = [string](Get-Value -InputObject $element -Name "path")
        $elementPaths += $path
    }
    Test-DuplicateValues -Values $elementPaths -IssueCode "duplicate-presentation-element-path" -MessagePrefix "Duplicate presentation element path" -Path "contracts.presentation.elements" -Issues $issues

    return $issues
}

$draft = Read-Json -PathValue $DraftPath
$issues = @(Test-ContractDraft -Draft $draft -ExpectedSurfaceId $SurfaceId -ExpectedTargetAssetPath $TargetAssetPath)
$success = ($issues.Count -eq 0)
$result = [PSCustomObject][ordered]@{
    schemaVersion = "1.0.0"
    success = $success
    terminalVerdict = if ($success) { "passed" } else { "blocked" }
    blockedReason = if ($success) { "" } else { [string]::Join(" | ", @($issues | ForEach-Object { [string]$_.message })) }
    draftPath = $DraftPath
    surfaceId = if (Test-HasProperty -InputObject $draft -Name "surfaceId") { [string]$draft.surfaceId } else { "" }
    issueCount = $issues.Count
    issues = @($issues)
    checkedAt = (Get-Date).ToString("o")
}

$result | ConvertTo-Json -Depth 20
if (-not $success) {
    exit 1
}
