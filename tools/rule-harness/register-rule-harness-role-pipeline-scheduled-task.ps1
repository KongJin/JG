param(
    [string]$TaskName = 'JG Rule Harness Roles Pipeline',
    [int]$IntervalMinutes = 120,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [switch]$DryRun,
    [switch]$WakeToRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($IntervalMinutes -lt 1) {
    throw 'IntervalMinutes must be 1 or greater.'
}

$scriptPath = Join-Path $RepoRoot 'tools/rule-harness/run-harness-pipeline.ps1'
if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "Role pipeline script not found: $scriptPath"
}

$startAt = (Get-Date).AddMinutes(2)
$trigger = New-ScheduledTaskTrigger `
    -Once `
    -At $startAt `
    -RepetitionInterval (New-TimeSpan -Minutes $IntervalMinutes) `
    -RepetitionDuration (New-TimeSpan -Days 3650)

$actionArgs = @(
    '-NoProfile',
    '-ExecutionPolicy', 'Bypass',
    '-File', ('"{0}"' -f $scriptPath)
)

if ($DryRun) {
    $actionArgs += '-DryRun'
}

$action = New-ScheduledTaskAction `
    -Execute 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' `
    -Argument ($actionArgs -join ' ') `
    -WorkingDirectory $RepoRoot

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -MultipleInstances IgnoreNew

if ($WakeToRun) {
    $settings.WakeToRun = $true
}

$principal = New-ScheduledTaskPrincipal `
    -UserId $env:USERNAME `
    -LogonType Interactive `
    -RunLevel Limited

$task = New-ScheduledTask `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Description "Periodic local 4-role rule-harness pipeline. Repo: $RepoRoot"

Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

Write-Host "Registered scheduled task: $TaskName"
Write-Host "Interval (minutes): $IntervalMinutes"
Write-Host "First run: $startAt"
Write-Host "Command: $scriptPath"
Write-Host "DryRun: $DryRun"
Write-Host "WakeToRun: $WakeToRun"
