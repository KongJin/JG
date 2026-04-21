param(
    [string]$UnityBridgeUrl,
    [string]$ScenePath = "Assets/Scenes/LobbyScene.unity",
    [string]$GarageTabButtonPath = "/Canvas/LobbyGarageNavBar/GarageTabButton",
    [string]$LobbyTabButtonPath = "/Canvas/LobbyGarageNavBar/LobbyTabButton",
    [string]$LobbyRootPath = "/Canvas/LobbyPageRoot",
    [string]$GarageRootPath = "/Canvas/GaragePageRoot",
    [string]$LoginLoadingPanelPath = "/Canvas/LoginLoadingOverlay/LoadingPanel",
    [string]$LobbyOutputPath = "artifacts/unity/lobby-page-smoke-lobby-initial.png",
    [string]$GarageOutputPath = "artifacts/unity/lobby-page-smoke-garage.png",
    [string]$ReturnedLobbyOutputPath = "artifacts/unity/lobby-page-smoke-lobby-returned.png",
    [string]$ResultPath = "artifacts/unity/lobby-garage-page-switch-result.json",
    [int]$TargetWidth = 390,
    [int]$TargetHeight = 844,
    [int]$TimeoutSec = 90,
    [int]$UiSettleMs = 500
)

Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force | Out-Null
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. "$PSScriptRoot\McpHelpers.ps1"

Assert-McpSceneAssetExistsForWorkflow -ScenePath $ScenePath -WorkflowName "Invoke-LobbyGaragePageSwitchSmoke.ps1"

Add-Type -AssemblyName System.Drawing

function Convert-McpCaptureToFixedFrame {
    param(
        [string]$RelativePath,
        [int]$Width,
        [int]$Height
    )

    $absolutePath = Resolve-McpAbsolutePath -PathValue $RelativePath
    if (-not (Test-Path -LiteralPath $absolutePath)) {
        throw "Screenshot not found for frame normalization: $RelativePath"
    }

    $sourceBytes = [System.IO.File]::ReadAllBytes($absolutePath)
    $sourceStream = [System.IO.MemoryStream]::new($sourceBytes)
    $sourceBitmap = [System.Drawing.Bitmap]::new($sourceStream)
    try {
        $sourceWidth = [int]$sourceBitmap.Width
        $sourceHeight = [int]$sourceBitmap.Height
        $targetAspect = [double]$Width / [double]$Height
        $sourceAspect = [double]$sourceWidth / [double]$sourceHeight

        $cropX = 0
        $cropY = 0
        $cropWidth = $sourceWidth
        $cropHeight = $sourceHeight

        if ([math]::Abs($sourceAspect - $targetAspect) -gt 0.0001) {
            if ($sourceAspect -gt $targetAspect) {
                $cropWidth = [int][math]::Round($sourceHeight * $targetAspect)
                $cropX = [int][math]::Floor(($sourceWidth - $cropWidth) / 2)
            }
            else {
                $cropHeight = [int][math]::Round($sourceWidth / $targetAspect)
                $cropY = [int][math]::Floor(($sourceHeight - $cropHeight) / 2)
            }
        }

        $targetBitmap = [System.Drawing.Bitmap]::new($Width, $Height)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($targetBitmap)
            try {
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.DrawImage(
                    $sourceBitmap,
                    [System.Drawing.Rectangle]::new(0, 0, $Width, $Height),
                    [System.Drawing.Rectangle]::new($cropX, $cropY, $cropWidth, $cropHeight),
                    [System.Drawing.GraphicsUnit]::Pixel
                )
            }
            finally {
                $graphics.Dispose()
            }

            $targetBitmap.Save($absolutePath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $targetBitmap.Dispose()
        }

        return [PSCustomObject]@{
            relativePath = $RelativePath
            absolutePath = $absolutePath
            sourceWidth = $sourceWidth
            sourceHeight = $sourceHeight
            outputWidth = $Width
            outputHeight = $Height
            cropX = $cropX
            cropY = $cropY
            cropWidth = $cropWidth
            cropHeight = $cropHeight
        }
    }
    finally {
        $sourceBitmap.Dispose()
        $sourceStream.Dispose()
    }
}

$root = Get-UnityMcpBaseUrl -ExplicitBaseUrl $UnityBridgeUrl
$resultAbsolutePath = Resolve-McpAbsolutePath -PathValue $ResultPath
$startedPlayHere = $false

try {
    $session = Invoke-McpPrepareLobbyPlaySession `
        -Root $root `
        -ScenePath $ScenePath `
        -LoginLoadingPanelPath $LoginLoadingPanelPath `
        -TimeoutSec $TimeoutSec
    $startedPlayHere = $true
    Start-Sleep -Milliseconds $UiSettleMs
    $lobbyCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $LobbyOutputPath -Overwrite
    $lobbyCaptureFrame = Convert-McpCaptureToFixedFrame -RelativePath $lobbyCapture.relativePath -Width $TargetWidth -Height $TargetHeight
    $lobbyStates = Get-McpPageStateSnapshot -Root $root -LobbyRootPath $LobbyRootPath -GarageRootPath $GarageRootPath

    $garageInvoke = Invoke-McpUiInvoke -Root $root -Path $GarageTabButtonPath -Method "click"
    $garageReady = Wait-McpUiActive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
    $lobbyHidden = Wait-McpUiInactive -Root $root -Path $LobbyRootPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $garageCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $GarageOutputPath -Overwrite
    $garageCaptureFrame = Convert-McpCaptureToFixedFrame -RelativePath $garageCapture.relativePath -Width $TargetWidth -Height $TargetHeight
    $garageStates = Get-McpPageStateSnapshot -Root $root -LobbyRootPath $LobbyRootPath -GarageRootPath $GarageRootPath

    $backInvoke = Invoke-McpUiInvoke -Root $root -Path $LobbyTabButtonPath -Method "click"
    $lobbyReturned = Wait-McpUiActive -Root $root -Path $LobbyRootPath -TimeoutMs ($TimeoutSec * 1000)
    $garageHiddenAfterReturn = Wait-McpUiInactive -Root $root -Path $GarageRootPath -TimeoutMs ($TimeoutSec * 1000)
    Start-Sleep -Milliseconds $UiSettleMs
    $returnedLobbyCapture = Invoke-McpScreenshotCapture -Root $root -OutputPath $ReturnedLobbyOutputPath -Overwrite
    $returnedLobbyCaptureFrame = Convert-McpCaptureToFixedFrame -RelativePath $returnedLobbyCapture.relativePath -Width $TargetWidth -Height $TargetHeight
    $returnedLobbyStates = Get-McpPageStateSnapshot -Root $root -LobbyRootPath $LobbyRootPath -GarageRootPath $GarageRootPath

    $consoleSummary = Get-McpConsoleSummary -Root $root -LogLimit 80 -ErrorLimit 20

    $report = [PSCustomObject]@{
        success = $true
        generatedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ssK")
        scenePath = $ScenePath
        resultPath = $resultAbsolutePath
        captureFrame = [PSCustomObject]@{
            width = $TargetWidth
            height = $TargetHeight
            contract = "center-crop-and-resize"
        }
        uiPaths = [PSCustomObject]@{
            garageTabButton = $GarageTabButtonPath
            lobbyTabButton = $LobbyTabButtonPath
            lobbyRoot = $LobbyRootPath
            garageRoot = $GarageRootPath
        }
        stoppedPreExistingPlay = $session.stoppedPreExistingPlay
        captures = [PSCustomObject]@{
            lobbyInitial = $lobbyCapture.relativePath
            garage = $garageCapture.relativePath
            lobbyReturned = $returnedLobbyCapture.relativePath
        }
        captureDetails = [PSCustomObject]@{
            lobbyInitial = $lobbyCaptureFrame
            garage = $garageCaptureFrame
            lobbyReturned = $returnedLobbyCaptureFrame
        }
        pageStates = [PSCustomObject]@{
            lobbyInitial = $lobbyStates
            garageCapture = $garageStates
            lobbyReturned = $returnedLobbyStates
        }
        transitions = [PSCustomObject]@{
            toGarage = [PSCustomObject]@{
                invoked = Test-McpResponseSuccess -Response $garageInvoke
                garageReady = Test-McpResponseSuccess -Response $garageReady
                lobbyHidden = Test-McpResponseSuccess -Response $lobbyHidden
            }
            toLobby = [PSCustomObject]@{
                invoked = Test-McpResponseSuccess -Response $backInvoke
                lobbyReturned = Test-McpResponseSuccess -Response $lobbyReturned
                garageHidden = Test-McpResponseSuccess -Response $garageHiddenAfterReturn
            }
        }
        warningCount = $consoleSummary.warningCount
        errorCount = $consoleSummary.errorCount
        warnings = $consoleSummary.warnings
        errors = $consoleSummary.errors
    }

    Ensure-McpParentDirectory -PathValue $resultAbsolutePath
    ($report | ConvertTo-Json -Depth 8) | Set-Content -Path $resultAbsolutePath -Encoding UTF8
    [PSCustomObject]@{
        success = $true
        scenePath = $ScenePath
        resultPath = $resultAbsolutePath
        targetFrame = "$TargetWidth x $TargetHeight"
        lobbyScreenshot = $lobbyCapture.relativePath
        garageScreenshot = $garageCapture.relativePath
        returnedLobbyScreenshot = $returnedLobbyCapture.relativePath
        warningCount = $consoleSummary.warningCount
        errorCount = $consoleSummary.errorCount
    } | ConvertTo-Json -Depth 5
}
finally {
    if ($startedPlayHere) {
        try {
            Invoke-McpPlayStopAndWait -Root $root -TimeoutSec $TimeoutSec | Out-Null
        }
        catch {
            Write-Warning ("Failed to stop Play Mode after page-switch smoke: {0}" -f $_.Exception.Message)
        }
    }
}
