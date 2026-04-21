param(
    [string]$ScreenManifestPath = ".stitch/contracts/screens/set-c-login-loading-overlay.screen.json",
    [string]$UnityBridgeUrl = "",
    [string]$ArtifactPath = "artifacts/unity/stitch-overlay-translation-result.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot/McpHelpers.ps1"
. "$PSScriptRoot/McpPrefabPackHelpers.ps1"

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

function Set-SceneComponentReference {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][string]$GameObjectPath,
        [Parameter(Mandatory = $true)][string]$ComponentType,
        [Parameter(Mandatory = $true)][string]$PropertyName,
        [Parameter(Mandatory = $true)][string]$Value
    )

    Invoke-McpPostJson -Root $Root -SubPath "/component/set" -Body @{
        gameObjectPath = $GameObjectPath
        componentType = $ComponentType
        propertyName = $PropertyName
        value = $Value
    } | Out-Null
}

function Open-TempScene {
    param([Parameter(Mandatory = $true)][string]$Root)

    Invoke-McpJsonWithTransientRetry -Root $Root -SubPath "/scene/open" -Body @{
        scenePath = "Assets/Scenes/TempScene.unity"
        saveCurrentSceneIfDirty = $true
    } -TimeoutSec 60 | Out-Null
}

function Build-LoginLoadingOverlayFromManifest {
    param(
        [Parameter(Mandatory = $true)][string]$Root,
        [Parameter(Mandatory = $true)][object]$Manifest
    )

    $targets = Get-RequiredProperty -InputObject $Manifest -Name "targets"
    $sceneRoots = @(Get-RequiredProperty -InputObject $targets -Name "sceneRoots")
    $sceneRoot = [string]$sceneRoots[0]
    $prefabPath = Get-RequiredProperty -InputObject $targets -Name "prefabPath"

    $canvasPath = "/Canvas"
    $rootName = (($sceneRoot -split "/") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 1)
    $rootPath = "$canvasPath/$rootName"
    $overlayCardPath = "$rootPath/OverlayCard"
    $loadingPanelPath = "$overlayCardPath/LoadingPanel"
    $statusTextPath = "$loadingPanelPath/StatusText"
    $errorPanelPath = "$overlayCardPath/ErrorPanel"
    $errorTextPath = "$errorPanelPath/ErrorText"
    $retryButtonPath = "$errorPanelPath/SecondaryActionButton"
    $retryButtonTextPath = "$retryButtonPath/Text (TMP)"

    Remove-McpGameObjectIfExists -Root $Root -Path $canvasPath
    Remove-McpGameObjectIfExists -Root $Root -Path "/LobbyView"
    New-McpScratchCanvas -Root $Root -CanvasPath $canvasPath

    Set-McpProperty -Root $Root -Path $canvasPath -ComponentType "CanvasScaler" -PropertyName "m_UiScaleMode" -Value "1"
    Set-McpProperty -Root $Root -Path $canvasPath -ComponentType "CanvasScaler" -PropertyName "m_ReferenceResolution" -Value "(390,844)"
    Set-McpProperty -Root $Root -Path $canvasPath -ComponentType "CanvasScaler" -PropertyName "m_ScreenMatchMode" -Value "0"
    Set-McpProperty -Root $Root -Path $canvasPath -ComponentType "CanvasScaler" -PropertyName "m_MatchWidthOrHeight" -Value "1"

    New-McpPanel -Root $Root -Name $rootName -ParentPath $canvasPath -Width 390 -Height 844
    Set-McpRectTransform -Root $Root -Path $rootPath -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
    Set-McpImageColor -Root $Root -Path $rootPath -Color "#0A0F14C7"
    Add-McpComponent -Root $Root -Path $rootPath -ComponentType "CanvasGroup"
    Add-McpComponent -Root $Root -Path $rootPath -ComponentType "LoginLoadingView"

    New-McpPanel -Root $Root -Name "OverlayCard" -ParentPath $rootPath -Width 312 -Height 236
    Set-McpRectTransform -Root $Root -Path $overlayCardPath -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,-10)" -SizeDelta "(312,236)"
    Set-McpImageColor -Root $Root -Path $overlayCardPath -Color "#141A24FA"

    New-McpText -Root $Root -Name "HeaderText" -ParentPath $overlayCardPath -Text "SIGN IN STATUS" -FontSize 18 -Color "#F0F7FFFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$overlayCardPath/HeaderText" -AnchorMin "(0.5,1)" -AnchorMax "(0.5,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,-22)" -SizeDelta "(260,28)"

    New-McpPanel -Root $Root -Name "LoadingPanel" -ParentPath $overlayCardPath -Width 264 -Height 92
    Set-McpRectTransform -Root $Root -Path $loadingPanelPath -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,6)" -SizeDelta "(264,92)"
    Set-McpImageColor -Root $Root -Path $loadingPanelPath -Color "#1C2630F0"

    New-McpText -Root $Root -Name "StatusText" -ParentPath $loadingPanelPath -Text "Signing in..." -FontSize 20 -Color "#F5FAFFFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path $statusTextPath -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,8)" -SizeDelta "(220,32)"

    New-McpText -Root $Root -Name "StatusHintText" -ParentPath $loadingPanelPath -Text "Profile sync and room data are preparing." -FontSize 13 -Color "#B7C9E0FF" | Out-Null
    Set-McpRectTransform -Root $Root -Path "$loadingPanelPath/StatusHintText" -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,-20)" -SizeDelta "(220,34)"

    New-McpPanel -Root $Root -Name "ErrorPanel" -ParentPath $overlayCardPath -Width 264 -Height 124
    Set-McpRectTransform -Root $Root -Path $errorPanelPath -AnchorMin "(0.5,0.5)" -AnchorMax "(0.5,0.5)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,12)" -SizeDelta "(264,124)"
    Set-McpImageColor -Root $Root -Path $errorPanelPath -Color "#291919F5"
    Set-McpActive -Root $Root -Path $errorPanelPath -Active $false

    New-McpText -Root $Root -Name "ErrorText" -ParentPath $errorPanelPath -Text "Please check your network connection." -FontSize 16 -Color "#FFEAEAFF" | Out-Null
    Set-McpRectTransform -Root $Root -Path $errorTextPath -AnchorMin "(0.5,1)" -AnchorMax "(0.5,1)" -Pivot "(0.5,1)" -AnchoredPosition "(0,-18)" -SizeDelta "(220,46)"

    New-McpButton -Root $Root -Name "SecondaryActionButton" -ParentPath $errorPanelPath -Text "Retry"
    Set-McpRectTransform -Root $Root -Path $retryButtonPath -AnchorMin "(0.5,0)" -AnchorMax "(0.5,0)" -Pivot "(0.5,0)" -AnchoredPosition "(0,14)" -SizeDelta "(188,40)"
    Set-McpImageColor -Root $Root -Path $retryButtonPath -Color "#D15E2EFF"
    Set-McpTmpStyle -Root $Root -Path $retryButtonTextPath -Text "Retry" -FontSize 16 -Color "#FFFFFFFF"

    Set-SceneComponentReference -Root $Root -GameObjectPath $rootPath -ComponentType "LoginLoadingView" -PropertyName "_loadingPanel" -Value $loadingPanelPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $rootPath -ComponentType "LoginLoadingView" -PropertyName "_statusText" -Value $statusTextPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $rootPath -ComponentType "LoginLoadingView" -PropertyName "_errorPanel" -Value $errorPanelPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $rootPath -ComponentType "LoginLoadingView" -PropertyName "_errorText" -Value $errorTextPath
    Set-SceneComponentReference -Root $Root -GameObjectPath $rootPath -ComponentType "LoginLoadingView" -PropertyName "_retryButton" -Value $retryButtonPath

    Save-McpPrefabAsset -Root $Root -ScenePath $rootPath -SavePath $prefabPath

    return [PSCustomObject]@{
        prefabPath = $prefabPath
        sceneRoot = $sceneRoot
        scratchRootPath = $rootPath
        verifiedChildPaths = @(
            "OverlayCard",
            "OverlayCard/LoadingPanel",
            "OverlayCard/LoadingPanel/StatusText",
            "OverlayCard/ErrorPanel",
            "OverlayCard/ErrorPanel/ErrorText",
            "OverlayCard/ErrorPanel/SecondaryActionButton"
        )
    }
}

$manifest = Get-Content -Path $ScreenManifestPath -Raw | ConvertFrom-Json
$surfaceId = Get-RequiredProperty -InputObject $manifest -Name "surfaceId"
$extends = Get-RequiredProperty -InputObject $manifest -Name "extends"

if ($extends -ne "overlay-modal") {
    throw "Only overlay-modal manifests are supported by this first-pass translator. Current extends='$extends'."
}

if ($surfaceId -ne "login-loading-overlay") {
    throw "This first-pass translator currently supports only 'login-loading-overlay'. Current surfaceId='$surfaceId'."
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$health = Wait-McpBridgeHealthy -Root $root -TimeoutSec 30
$compile = Invoke-McpCompileRequestAndWait -Root $root -TimeoutMs 120000

if (-not (Test-McpResponseSuccess -Response $compile.Wait)) {
    throw "Unity compile wait failed before translation."
}

Open-TempScene -Root $root
$translation = Build-LoginLoadingOverlayFromManifest -Root $root -Manifest $manifest

$prefabRoot = Get-McpPrefabNode -Root $root -AssetPath $translation.prefabPath -ChildPath ""
$prefabOverlayCard = Get-McpPrefabNode -Root $root -AssetPath $translation.prefabPath -ChildPath "OverlayCard"
$prefabRetryButton = Get-McpPrefabNode -Root $root -AssetPath $translation.prefabPath -ChildPath "OverlayCard/ErrorPanel/SecondaryActionButton"
$prefabYaml = Get-Content -Path $translation.prefabPath -Raw

$serializedReferenceChecks = [ordered]@{}
foreach ($fieldName in @("_loadingPanel", "_statusText", "_errorPanel", "_errorText", "_retryButton")) {
    $match = [regex]::Match($prefabYaml, "(?m)^\s*${fieldName}:\s+\{fileID:\s*(-?\d+)\}")
    $serializedReferenceChecks[$fieldName] = ($match.Success -and $match.Groups[1].Value -ne "0")
}

$result = [PSCustomObject]@{
    success = $true
    manifestPath = (Resolve-Path $ScreenManifestPath).Path
    surfaceId = $surfaceId
    prefabPath = $translation.prefabPath
    sceneRoot = $translation.sceneRoot
    verifiedChildPaths = $translation.verifiedChildPaths
    compileSucceeded = (Test-McpResponseSuccess -Response $compile.Wait)
    bridgeHealth = $health.State
    prefabChecks = [PSCustomObject]@{
        rootFound = ($null -ne $prefabRoot)
        overlayCardFound = ($null -ne $prefabOverlayCard)
        retryButtonFound = ($null -ne $prefabRetryButton)
    }
    serializedReferenceChecks = [PSCustomObject]$serializedReferenceChecks
}

Ensure-McpParentDirectory -PathValue $ArtifactPath
$result | ConvertTo-Json -Depth 30 | Set-Content -Path $ArtifactPath -Encoding utf8
$result | ConvertTo-Json -Depth 20
