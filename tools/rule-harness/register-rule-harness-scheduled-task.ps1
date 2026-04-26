param(
    [string]$TaskName = 'JG Rule Harness Static',
    [int]$IntervalMinutes = 60,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
    [string]$Model = 'glm-5',
    [string]$ApiBaseUrl = 'https://open.bigmodel.cn/api/paas/v4',
    [string]$MutationMode = 'code_and_rules',
    [switch]$EnableLlm,
    [switch]$RequireLlm,
    [switch]$WakeToRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($IntervalMinutes -lt 1) {
    throw 'IntervalMinutes must be 1 or greater.'
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
    '-MutationMode', ('"{0}"' -f $MutationMode)
)

if ($RequireLlm) {
    $actionArgs += @(
        '-RequireLlm',
        '-Model', ('"{0}"' -f $Model),
        '-ApiBaseUrl', ('"{0}"' -f $ApiBaseUrl)
    )
}
elseif ($EnableLlm) {
    $actionArgs += @(
        '-EnableLlm',
        '-Model', ('"{0}"' -f $Model),
        '-ApiBaseUrl', ('"{0}"' -f $ApiBaseUrl)
    )
}

$actionArgs = $actionArgs -join ' '

$action = New-ScheduledTaskAction `
    -Execute 'C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe' `
    -Argument $actionArgs `
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
    -Description "Periodic local deterministic rule-harness run. Optional LLM mode must be explicitly enabled. Repo: $RepoRoot"

Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

Write-Host "Registered scheduled task: $TaskName"
Write-Host "Interval (minutes): $IntervalMinutes"
Write-Host "First run: $startAt"
Write-Host "Command: $scriptPath"
Write-Host "LlmMode: $(if ($RequireLlm) { 'required' } elseif ($EnableLlm) { 'optional' } else { 'disabled' })"
if ($RequireLlm -or $EnableLlm) {
    Write-Host "Model: $Model"
    Write-Host "ApiBaseUrl: $ApiBaseUrl"
}
Write-Host "MutationMode: $MutationMode"
Write-Host "WakeToRun: $WakeToRun"
