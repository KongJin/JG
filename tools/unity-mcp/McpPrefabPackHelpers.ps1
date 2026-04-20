function Invoke-McpPostJson {
    param(
        [string]$Root,
        [string]$SubPath,
        [object]$Body
    )

    return Invoke-RestMethod `
        -Method Post `
        -Uri "$Root$SubPath" `
        -ContentType "application/json" `
        -Body ($Body | ConvertTo-Json -Depth 20 -Compress)
}

function New-McpGameObject {
    param(
        [string]$Root,
        [string]$Name,
        [string]$ParentPath,
        [string[]]$Components
    )

    $body = @{
        name = $Name
        components = $Components
    }

    if (-not [string]::IsNullOrWhiteSpace($ParentPath)) {
        $body.parent = $ParentPath
    }

    Invoke-McpPostJson -Root $Root -SubPath "/gameobject/create" -Body $body | Out-Null
}

function New-McpPanel {
    param(
        [string]$Root,
        [string]$Name,
        [string]$ParentPath,
        [double]$Width,
        [double]$Height
    )

    Invoke-McpPostJson -Root $Root -SubPath "/ui/create-panel" -Body @{
        name = $Name
        parent = $ParentPath
        width = $Width
        height = $Height
    } | Out-Null
}

function New-McpButton {
    param(
        [string]$Root,
        [string]$Name,
        [string]$ParentPath,
        [string]$Text
    )

    Invoke-McpPostJson -Root $Root -SubPath "/ui/create-button" -Body @{
        name = $Name
        parent = $ParentPath
        buttonText = $Text
    } | Out-Null
}

function New-McpRawImage {
    param(
        [string]$Root,
        [string]$Name,
        [string]$ParentPath,
        [double]$Width,
        [double]$Height
    )

    Invoke-McpPostJson -Root $Root -SubPath "/ui/create-raw-image" -Body @{
        name = $Name
        parent = $ParentPath
        width = $Width
        height = $Height
    } | Out-Null
}

function New-McpPrimitive {
    param(
        [string]$Root,
        [string]$Name,
        [string]$PrimitiveType,
        [string]$ParentPath
    )

    Invoke-McpPostJson -Root $Root -SubPath "/gameobject/create-primitive" -Body @{
        name = $Name
        primitiveType = $PrimitiveType
    } | Out-Null

    Invoke-McpPostJson -Root $Root -SubPath "/gameobject/set-parent" -Body @{
        path = "/$Name"
        parentPath = $ParentPath
    } | Out-Null
}

function Add-McpComponent {
    param(
        [string]$Root,
        [string]$Path,
        [string]$ComponentType
    )

    Invoke-McpPostJson -Root $Root -SubPath "/component/add" -Body @{
        gameObjectPath = $Path
        componentType = $ComponentType
    } | Out-Null
}

function Set-McpProperty {
    param(
        [string]$Root,
        [string]$Path,
        [string]$ComponentType,
        [string]$PropertyName,
        [string]$Value
    )

    Invoke-McpPostJson -Root $Root -SubPath "/component/set" -Body @{
        gameObjectPath = $Path
        componentType = $ComponentType
        propertyName = $PropertyName
        value = $Value
    } | Out-Null
}

function Set-McpTextValue {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Value
    )

    Invoke-McpPostJson -Root $Root -SubPath "/ui/set-value" -Body @{
        path = $Path
        value = $Value
    } | Out-Null
}

function Set-McpActive {
    param(
        [string]$Root,
        [string]$Path,
        [bool]$Active
    )

    Invoke-McpPostJson -Root $Root -SubPath "/gameobject/set-active" -Body @{
        path = $Path
        active = $Active
    } | Out-Null
}

function Remove-McpGameObjectIfExists {
    param(
        [string]$Root,
        [string]$Path
    )

    try {
        Invoke-McpPostJson -Root $Root -SubPath "/gameobject/destroy" -Body @{
            path = $Path
        } | Out-Null
    }
    catch {
    }
}

function Set-McpRectTransform {
    param(
        [string]$Root,
        [string]$Path,
        [string]$AnchorMin,
        [string]$AnchorMax,
        [string]$Pivot,
        [string]$AnchoredPosition,
        [string]$SizeDelta
    )

    Set-McpProperty -Root $Root -Path $Path -ComponentType "RectTransform" -PropertyName "m_AnchorMin" -Value $AnchorMin
    Set-McpProperty -Root $Root -Path $Path -ComponentType "RectTransform" -PropertyName "m_AnchorMax" -Value $AnchorMax
    Set-McpProperty -Root $Root -Path $Path -ComponentType "RectTransform" -PropertyName "m_Pivot" -Value $Pivot
    Set-McpProperty -Root $Root -Path $Path -ComponentType "RectTransform" -PropertyName "m_AnchoredPosition" -Value $AnchoredPosition
    Set-McpProperty -Root $Root -Path $Path -ComponentType "RectTransform" -PropertyName "m_SizeDelta" -Value $SizeDelta
}

function Set-McpTransformLocal {
    param(
        [string]$Root,
        [string]$Path,
        [string]$LocalPosition,
        [string]$LocalScale
    )

    Set-McpProperty -Root $Root -Path $Path -ComponentType "Transform" -PropertyName "m_LocalPosition" -Value $LocalPosition
    Set-McpProperty -Root $Root -Path $Path -ComponentType "Transform" -PropertyName "m_LocalScale" -Value $LocalScale
}

function Set-McpImageColor {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Color
    )

    Set-McpProperty -Root $Root -Path $Path -ComponentType "Image" -PropertyName "m_Color" -Value $Color
}

function Set-McpRawImageColor {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Color
    )

    Set-McpProperty -Root $Root -Path $Path -ComponentType "RawImage" -PropertyName "m_Color" -Value $Color
}

function Set-McpTmpStyle {
    param(
        [string]$Root,
        [string]$Path,
        [string]$Text,
        [double]$FontSize,
        [string]$Color
    )

    Set-McpTextValue -Root $Root -Path $Path -Value $Text
    Set-McpProperty -Root $Root -Path $Path -ComponentType "TextMeshProUGUI" -PropertyName "m_fontSize" -Value ([string]$FontSize)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "TextMeshProUGUI" -PropertyName "m_fontColor" -Value $Color
    Set-McpProperty -Root $Root -Path $Path -ComponentType "TextMeshProUGUI" -PropertyName "m_RaycastTarget" -Value "false"
}

function Add-McpLayoutElement {
    param(
        [string]$Root,
        [string]$Path,
        [double]$PreferredHeight
    )

    Add-McpComponent -Root $Root -Path $Path -ComponentType "LayoutElement"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "LayoutElement" -PropertyName "m_PreferredHeight" -Value ([string]$PreferredHeight)
}

function Configure-McpVerticalLayout {
    param(
        [string]$Root,
        [string]$Path,
        [double]$Spacing,
        [int]$PaddingLeft,
        [int]$PaddingRight,
        [int]$PaddingTop,
        [int]$PaddingBottom
    )

    Add-McpComponent -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_Spacing" -Value ([string]$Spacing)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value "true"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "true"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value "true"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value ([string]$PaddingLeft)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value ([string]$PaddingRight)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value ([string]$PaddingTop)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "VerticalLayoutGroup" -PropertyName "m_Padding.m_Bottom" -Value ([string]$PaddingBottom)
}

function Configure-McpHorizontalLayout {
    param(
        [string]$Root,
        [string]$Path,
        [double]$Spacing,
        [int]$PaddingLeft = 0,
        [int]$PaddingRight = 0,
        [int]$PaddingTop = 0,
        [int]$PaddingBottom = 0,
        [bool]$ControlWidth = $false,
        [bool]$ForceExpandWidth = $false
    )

    Add-McpComponent -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Spacing" -Value ([string]$Spacing)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlWidth" -Value ($ControlWidth.ToString().ToLowerInvariant())
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildControlHeight" -Value "true"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandWidth" -Value ($ForceExpandWidth.ToString().ToLowerInvariant())
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_ChildForceExpandHeight" -Value "false"
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Left" -Value ([string]$PaddingLeft)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Right" -Value ([string]$PaddingRight)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Top" -Value ([string]$PaddingTop)
    Set-McpProperty -Root $Root -Path $Path -ComponentType "HorizontalLayoutGroup" -PropertyName "m_Padding.m_Bottom" -Value ([string]$PaddingBottom)
}

function New-McpText {
    param(
        [string]$Root,
        [string]$Name,
        [string]$ParentPath,
        [string]$Text,
        [double]$FontSize,
        [string]$Color
    )

    New-McpGameObject -Root $Root -Name $Name -ParentPath $ParentPath -Components @("RectTransform", "TextMeshProUGUI")
    $path = "$ParentPath/$Name"
    Set-McpTmpStyle -Root $Root -Path $path -Text $Text -FontSize $FontSize -Color $Color
    return $path
}

function Save-McpPrefabAsset {
    param(
        [string]$Root,
        [string]$ScenePath,
        [string]$SavePath
    )

    Invoke-McpPostJson -Root $Root -SubPath "/prefab/save" -Body @{
        gameObjectPath = $ScenePath
        savePath = $SavePath
        destroySceneObject = $false
        connectSceneObject = $false
    } | Out-Null
}

function Get-McpPrefabNode {
    param(
        [string]$Root,
        [string]$AssetPath,
        [string]$ChildPath
    )

    $body = @{
        assetPath = $AssetPath
    }

    if (-not [string]::IsNullOrWhiteSpace($ChildPath)) {
        $body.childPath = $ChildPath
    }

    return Invoke-McpPostJson -Root $Root -SubPath "/prefab/get" -Body $body
}

function Set-McpArrayReference {
    param(
        [string]$Root,
        [string]$Path,
        [string]$ComponentType,
        [string]$ArrayName,
        [string[]]$Values
    )

    for ($i = 0; $i -lt $Values.Length; $i++) {
        Set-McpProperty -Root $Root -Path $Path -ComponentType $ComponentType -PropertyName "$ArrayName.Array.data[$i]" -Value $Values[$i]
    }
}

function New-McpScratchCanvas {
    param(
        [string]$Root,
        [string]$CanvasPath = "/Canvas"
    )

    $lastSlashIndex = $CanvasPath.LastIndexOf("/")
    if ($lastSlashIndex -le 0) {
        $canvasName = $CanvasPath.TrimStart("/")
        $parentPath = ""
    }
    else {
        $canvasName = $CanvasPath.Substring($lastSlashIndex + 1)
        $parentPath = $CanvasPath.Substring(0, $lastSlashIndex)
    }

    New-McpGameObject -Root $Root -Name $canvasName -ParentPath $parentPath -Components @("RectTransform", "Canvas", "CanvasScaler", "GraphicRaycaster")
    Set-McpRectTransform -Root $Root -Path $CanvasPath -AnchorMin "(0,0)" -AnchorMax "(1,1)" -Pivot "(0.5,0.5)" -AnchoredPosition "(0,0)" -SizeDelta "(0,0)"
}

function Invoke-McpPrefabPackGeneration {
    param(
        [string]$BaseUrl,
        [string]$OutputSummaryPath,
        [string]$PrefabFolder,
        [string[]]$CleanupPaths = @("/Canvas"),
        [scriptblock]$BuildScript
    )

    $Root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
    Wait-McpBridgeHealthy -Root $Root -TimeoutSec 60 | Out-Null
    $compile = Invoke-McpCompileRequestAndWait -Root $Root -TimeoutMs 120000

    if ($compile.HealthAfterWait.isPlaying) {
        Invoke-McpPlayStopAndWait -Root $Root -TimeoutSec 90 | Out-Null
    }

    Ensure-McpParentDirectory -PathValue $OutputSummaryPath
    Ensure-McpParentDirectory -PathValue (Join-Path $PrefabFolder "placeholder")
    if (-not (Test-Path -LiteralPath $PrefabFolder)) {
        New-Item -ItemType Directory -Path $PrefabFolder -Force | Out-Null
    }

    foreach ($path in $CleanupPaths) {
        Remove-McpGameObjectIfExists -Root $Root -Path $path
    }

    try {
        $buildResult = & $BuildScript $Root
        if ($buildResult -isnot [System.Collections.IDictionary] -or -not $buildResult.Contains("prefabMap")) {
            throw "BuildScript must return a dictionary containing 'prefabMap'."
        }

        $summary = [ordered]@{
            generatedAt = (Get-Date).ToString("o")
            baseUrl = $Root
            compile = $compile
            prefabs = @()
        }

        foreach ($entry in $buildResult.prefabMap.GetEnumerator()) {
            Save-McpPrefabAsset -Root $Root -ScenePath $entry.Value.scenePath -SavePath $entry.Value.assetPath
            $prefabRoot = Get-McpPrefabNode -Root $Root -AssetPath $entry.Value.assetPath -ChildPath ""
            $prefabChild = Get-McpPrefabNode -Root $Root -AssetPath $entry.Value.assetPath -ChildPath $entry.Value.verifyChildPath

            $summary.prefabs += [ordered]@{
                name = $entry.Key
                assetPath = $entry.Value.assetPath
                rootFound = $prefabRoot.found
                rootPath = $prefabRoot.path
                verifyChildPath = $entry.Value.verifyChildPath
                verifyChildFound = $prefabChild.found
                verifyChildResolvedPath = $prefabChild.path
            }
        }

        $summary | ConvertTo-Json -Depth 10 | Set-Content -Path $OutputSummaryPath -Encoding utf8
        return Get-Content -Path $OutputSummaryPath -Raw
    }
    finally {
        foreach ($path in $CleanupPaths) {
            Remove-McpGameObjectIfExists -Root $Root -Path $path
        }
    }
}
