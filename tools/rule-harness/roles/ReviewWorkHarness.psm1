Import-Module (Join-Path $PSScriptRoot '..\RuleHarness.psm1') -Force

function Get-ReviewWorkSummaryLines {
    param([Parameter(Mandatory)][object]$Report)

    $lines = [System.Collections.Generic.List[string]]::new()
    [void]$lines.Add('# Review Work Harness')
    [void]$lines.Add('')
    [void]$lines.Add("- Input review: $($Report.inputReviewPath)")
    [void]$lines.Add("- Applied batches: $(@($Report.appliedBatches).Count)")
    [void]$lines.Add("- Skipped batches: $(@($Report.skippedBatches).Count)")
    [void]$lines.Add("- Retry attempts: $($Report.retryAttempts)")
    [void]$lines.Add("- Failed: $($Report.failed)")
    [void]$lines.Add('')
    [void]$lines.Add('## Stage Status')
    foreach ($stage in @($Report.stageResults)) {
        [void]$lines.Add("- $($stage.stage) [$($stage.status)] $($stage.summary)")
    }
    @($lines)
}

function Invoke-ReviewWorkHarness {
    param(
        [Parameter(Mandatory)][string]$RepoRoot,
        [Parameter(Mandatory)][string]$ConfigPath,
        [Parameter(Mandatory)][string]$ReviewPath,
        [Parameter(Mandatory)][string]$OutputDir,
        [switch]$DryRun
    )

    if (-not (Test-Path -LiteralPath $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }

    $input = Test-RuleHarnessRoleInput -RepoRoot $RepoRoot -InputPath $ReviewPath -ThrowOnError
    $batches = @(ConvertTo-RuleHarnessRoleBatches -InputObject $input.payload)
    $mutation = Invoke-RuleHarnessRoleMutation -RepoRoot $RepoRoot -ConfigPath $ConfigPath -PlannedBatches $batches -RoleInputPath $input.path -DryRun:$DryRun
    $mutation | Add-Member -NotePropertyName 'inputReviewPath' -NotePropertyValue $input.path -Force
    $mutation | Add-Member -NotePropertyName 'baseCommitSha' -NotePropertyValue ([string]$input.payload.baseCommitSha) -Force

    $reportPath = Join-Path $OutputDir 'report.json'
    $summaryPath = Join-Path $OutputDir 'summary.md'
    $mutation | ConvertTo-Json -Depth 50 | Set-Content -Path $reportPath -Encoding UTF8
    Get-ReviewWorkSummaryLines -Report $mutation | Set-Content -Path $summaryPath -Encoding UTF8
    $mutation
}

Export-ModuleMember -Function Invoke-ReviewWorkHarness
