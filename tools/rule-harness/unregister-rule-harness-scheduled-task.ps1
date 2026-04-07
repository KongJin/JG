param(
    [string]$TaskName = 'JG Rule Harness GLM'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    Write-Host "Unregistered scheduled task: $TaskName"
}
else {
    Write-Host "Scheduled task not found: $TaskName"
}
