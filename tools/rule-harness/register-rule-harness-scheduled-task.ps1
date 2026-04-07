param(
    [string]$TaskName = 'JG Rule Harness GLM',
    [int]$IntervalMinutes = 60,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$Model = 'glm-5',
    [string]$ApiBaseUrl = 'https://open.bigmodel.cn/api/paas/v4',
    [switch]$WakeToRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($IntervalMinutes -lt 15) {
    throw 'IntervalMinutes must be 15 or greater.'
}

$scriptPath = Join-Path $RepoRoot 'tools/rule-harness/run-rule-harness-scheduled.ps1'
if (-not (Test-Path -LiteralPath $scriptPath)) {
    throw "Scheduled script not found: $scriptPath"
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
    '-File', ('"{0}"' -f $scriptPath),
    '-RequireLlm',
    '-Model', ('"{0}"' -f $Model),
    '-ApiBaseUrl', ('"{0}"' -f $ApiBaseUrl)
) -join ' '

$action = New-ScheduledTaskAction `
    -Execute 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' `
    -Argument $actionArgs `
    -WorkingDirectory $RepoRoot

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable

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
    -Description "Periodic local rule-harness run using GLM provider. Repo: $RepoRoot"

Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

Write-Host "Registered scheduled task: $TaskName"
Write-Host "Interval (minutes): $IntervalMinutes"
Write-Host "First run: $startAt"
Write-Host "Command: $scriptPath"
Write-Host "Model: $Model"
Write-Host "ApiBaseUrl: $ApiBaseUrl"
Write-Host "WakeToRun: $WakeToRun"
