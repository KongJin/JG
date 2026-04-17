param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$ProjectFile = "Assembly-CSharp-Editor.csproj",
    [switch]$UseEditorLogFallback
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Get-BuildLineMatches {
    param(
        [string[]]$Lines,
        [string]$Pattern
    )

    return @($Lines | Where-Object { $_ -match $Pattern })
}

function Invoke-DotnetCompileCheck {
    param(
        [string]$Root,
        [string]$ProjectName
    )

    $resolvedProject = Join-Path $Root $ProjectName
    if (-not (Test-Path -LiteralPath $resolvedProject)) {
        throw "Project file not found: $resolvedProject"
    }

    $dotnet = Get-Command dotnet -ErrorAction Stop
    $lines = @(& $dotnet.Source build $resolvedProject -nologo -v minimal 2>&1)
    $exitCode = $LASTEXITCODE

    $errorLines = @(Get-BuildLineMatches -Lines $lines -Pattern ':\s+error\s+[A-Z]{2,}\d+:')
    $warningLines = @(Get-BuildLineMatches -Lines $lines -Pattern ':\s+warning\s+[A-Z]{2,}\d+:')
    $summaryLine = $lines | Where-Object { $_ -match 'Build (FAILED|succeeded)\.' } | Select-Object -Last 1

    [PSCustomObject]@{
        Mode = "dotnet-build"
        ProjectFile = $resolvedProject
        ExitCode = $exitCode
        Errors = $errorLines.Count
        Warnings = $warningLines.Count
        Summary = if ($summaryLine) { $summaryLine.Trim() } else { "" }
        ErrorLines = $errorLines
        WarningLines = $warningLines
        RawLines = $lines
    }
}

function Invoke-EditorLogFallbackCheck {
    $editorLog = Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"
    if (-not (Test-Path -LiteralPath $editorLog)) {
        throw "Editor.log not found: $editorLog"
    }

    $lines = @(Get-Content -LiteralPath $editorLog)
    $errorLines = @(Get-BuildLineMatches -Lines $lines -Pattern '\berror\b')
    $warningLines = @(Get-BuildLineMatches -Lines $lines -Pattern '\bwarning\b')
    $lastWriteUtc = (Get-Item -LiteralPath $editorLog).LastWriteTimeUtc

    [PSCustomObject]@{
        Mode = "editor-log-fallback"
        ProjectFile = $editorLog
        ExitCode = if ($errorLines.Count -gt 0) { 1 } else { 0 }
        Errors = $errorLines.Count
        Warnings = $warningLines.Count
        Summary = "Fallback only. Editor.log may contain stale compile output. LastWriteUtc=$($lastWriteUtc.ToString('u'))"
        ErrorLines = @($errorLines | Select-Object -Last 20)
        WarningLines = @($warningLines | Select-Object -Last 20)
        RawLines = $null
    }
}

$result = $null
try {
    $result = Invoke-DotnetCompileCheck -Root $ProjectRoot -ProjectName $ProjectFile
}
catch {
    if (-not $UseEditorLogFallback) {
        throw
    }

    Write-Warning ("dotnet build check failed, falling back to Editor.log: {0}" -f $_.Exception.Message)
    $result = Invoke-EditorLogFallbackCheck
}

Write-Host ("MODE: {0}" -f $result.Mode) -ForegroundColor Cyan
Write-Host ("TARGET: {0}" -f $result.ProjectFile) -ForegroundColor Cyan
Write-Host ("ERRORS: {0}" -f $result.Errors) -ForegroundColor Red
Write-Host ("WARNINGS: {0}" -f $result.Warnings) -ForegroundColor Yellow
if (-not [string]::IsNullOrWhiteSpace($result.Summary)) {
    Write-Host ("SUMMARY: {0}" -f $result.Summary) -ForegroundColor Gray
}

if ($result.ErrorLines.Count -gt 0) {
    Write-Host ""
    Write-Host "Recent error lines:" -ForegroundColor Red
    $result.ErrorLines | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
}

if ($result.Warnings -gt 0 -and $result.WarningLines.Count -gt 0) {
    Write-Host ""
    Write-Host "Recent warning lines:" -ForegroundColor Yellow
    $result.WarningLines | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
}

if ($result.Errors -gt 0) {
    exit 1
}
