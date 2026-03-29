[CmdletBinding()]
param(
    [string]$Channel = "qa",
    [string]$OutputPath = "Build/WebGL",
    [string]$BaseUrl,
    [switch]$Live,
    [switch]$Fast,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

function Get-UnityMcpBaseUrl {
    param([string]$ExplicitBaseUrl)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitBaseUrl)) {
        return $ExplicitBaseUrl.TrimEnd("/")
    }

    $portFile = Join-Path $PSScriptRoot "..\ProjectSettings\UnityMcpPort.txt"
    $defaultPort = 51234

    if (Test-Path $portFile) {
        $portText = (Get-Content -Path $portFile -Raw).Trim()
        if ($portText -match '^\d+$') {
            return "http://127.0.0.1:$portText"
        }
    }

    return "http://127.0.0.1:$defaultPort"
}

function Invoke-UnityWebGlBuild {
    param(
        [string]$ResolvedBaseUrl,
        [string]$ResolvedOutputPath,
        [switch]$FastBuild
    )

    Write-Host "[1/2] Building WebGL via Unity MCP..." -ForegroundColor Cyan
    Write-Host "      Endpoint: $ResolvedBaseUrl/build/webgl"
    Write-Host "      Output:   $ResolvedOutputPath"
    Write-Host "      Mode:     $(if ($FastBuild) { 'fast (development + gzip)' } else { 'release' })"

    $payload = @{
        outputPath = $ResolvedOutputPath
        fastBuild = [bool]$FastBuild
    } | ConvertTo-Json -Compress
    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "$ResolvedBaseUrl/build/webgl" `
        -ContentType "application/json" `
        -Body $payload

    if (-not $response.success) {
        throw "Unity WebGL build failed. $($response.message)"
    }

    Write-Host "      $($response.message)" -ForegroundColor Green
}

function Get-FirebaseCommand {
    $cmd = Get-Command firebase.cmd -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    throw "firebase.cmd not found. Install Firebase CLI or add it to PATH."
}

function Invoke-FirebaseDeploy {
    param(
        [string]$FirebaseCommand,
        [string]$ResolvedChannel,
        [switch]$DeployLive
    )

    if ($DeployLive) {
        Write-Host "[2/2] Deploying live hosting..." -ForegroundColor Cyan
        & $FirebaseCommand deploy --only hosting
        if ($LASTEXITCODE -ne 0) {
            throw "Firebase live deploy failed with exit code $LASTEXITCODE."
        }

        Write-Host "      Live deploy completed." -ForegroundColor Green
        return
    }

    Write-Host "[2/2] Deploying preview channel..." -ForegroundColor Cyan
    Write-Host "      Channel:  $ResolvedChannel"

    & $FirebaseCommand hosting:channel:deploy $ResolvedChannel
    if ($LASTEXITCODE -ne 0) {
        throw "Firebase preview deploy failed with exit code $LASTEXITCODE."
    }

    Write-Host "      Preview deploy completed." -ForegroundColor Green
}

try {
    $resolvedBaseUrl = Get-UnityMcpBaseUrl -ExplicitBaseUrl $BaseUrl
    $firebaseCommand = Get-FirebaseCommand

    if (-not $SkipBuild) {
        Invoke-UnityWebGlBuild -ResolvedBaseUrl $resolvedBaseUrl -ResolvedOutputPath $OutputPath -FastBuild:$Fast
    }
    else {
        Write-Host "[1/2] Skipping WebGL build." -ForegroundColor Yellow
    }

    Invoke-FirebaseDeploy -FirebaseCommand $firebaseCommand -ResolvedChannel $Channel -DeployLive:$Live

    Write-Host ""
    if ($Live) {
        Write-Host "Done. Hosting live deploy finished." -ForegroundColor Green
    }
    else {
        Write-Host "Done. Hosting preview deploy finished." -ForegroundColor Green
    }
}
catch {
    Write-Error $_
    exit 1
}
