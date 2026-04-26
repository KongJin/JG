Set-StrictMode -Version Latest

function Get-WorkflowRepoPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $root = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd("\", "/")
    $fullPath = if ([System.IO.Path]::IsPathRooted($Path)) {
        [System.IO.Path]::GetFullPath($Path)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
    }

    if ($fullPath.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return ($fullPath.Substring($root.Length).TrimStart("\", "/") -replace "\\", "/")
    }

    return ($Path -replace "\\", "/")
}

function Invoke-WorkflowGit {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [switch]$AllowFailure
    )

    $output = @(& git -C $RepoRoot @Arguments 2>&1)
    $exitCode = $LASTEXITCODE

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw ("git {0} failed. {1}" -f ($Arguments -join " "), (($output | Out-String).Trim()))
    }

    [PSCustomObject]@{
        ExitCode = $exitCode
        Lines = @($output | ForEach-Object { [string]$_ })
    }
}

function Get-WorkflowChangedFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [switch]$StagedOnly
    )

    $arguments = if ($StagedOnly) {
        @("diff", "--cached", "--name-only", "--diff-filter=ACMRTUXB")
    }
    else {
        @("status", "--short")
    }

    $result = Invoke-WorkflowGit -RepoRoot $RepoRoot -Arguments $arguments
    if ($StagedOnly) {
        return @(
            $result.Lines |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                ForEach-Object { ($_ -replace "\\", "/").Trim() } |
                Sort-Object -Unique
        )
    }

    return @(
        foreach ($line in $result.Lines) {
            if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) {
                continue
            }

            $pathText = $line.Substring(3).Trim()
            if ($pathText -match " -> ") {
                $pathText = ($pathText -split " -> ")[-1]
            }

            $normalizedPath = $pathText -replace "\\", "/"
            $absolutePath = Join-Path $RepoRoot $normalizedPath
            if ($normalizedPath.EndsWith("/") -and (Test-Path -LiteralPath $absolutePath -PathType Container)) {
                Get-ChildItem -LiteralPath $absolutePath -Recurse -File |
                    ForEach-Object { Get-WorkflowRepoPath -RepoRoot $RepoRoot -Path $_.FullName }
            }
            else {
                $normalizedPath
            }
        }
    ) | Sort-Object -Unique
}

function Test-WorkflowPathMatch {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    foreach ($pattern in @($Patterns)) {
        if ($Path -match $pattern) {
            return $true
        }
    }

    return $false
}

function Get-WorkflowPathsMatching {
    param(
        [string[]]$Paths,

        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    return @(
        foreach ($path in @($Paths)) {
            if (Test-WorkflowPathMatch -Path $path -Patterns $Patterns) {
                $path
            }
        }
    ) | Sort-Object -Unique
}

function Get-WorkflowPathsOutside {
    param(
        [string[]]$Paths,

        [Parameter(Mandatory = $true)]
        [string[]]$Patterns
    )

    return @(
        foreach ($path in @($Paths)) {
            if (-not (Test-WorkflowPathMatch -Path $path -Patterns $Patterns)) {
                $path
            }
        }
    ) | Sort-Object -Unique
}

function Write-WorkflowSection {
    param([Parameter(Mandatory = $true)][string]$Title)

    Write-Host ""
    Write-Host ("== {0} ==" -f $Title) -ForegroundColor Cyan
}

function Invoke-WorkflowCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$Label,

        [Parameter(Mandatory = $true)]
        [string]$FileName,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    Write-Host ("RUN {0}" -f $Label) -ForegroundColor Cyan
    Push-Location -LiteralPath $RepoRoot
    try {
        $output = @(& $FileName @Arguments 2>&1)
        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    $summary = (($output | Select-Object -Last 12) | Out-String).Trim()
    [PSCustomObject]@{
        Label = $Label
        ExitCode = $exitCode
        Passed = ($exitCode -eq 0)
        Summary = $summary
    }
}

function Get-WorkflowUnityEditorInstance {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $instancePath = Join-Path $RepoRoot "Library\EditorInstance.json"
    if (-not (Test-Path -LiteralPath $instancePath)) {
        return $null
    }

    try {
        $instance = Get-Content -LiteralPath $instancePath -Raw | ConvertFrom-Json
    }
    catch {
        return [PSCustomObject]@{
            Exists = $true
            Path = $instancePath
            ParseError = $_.Exception.Message
            ProcessId = $null
            ProcessAlive = $false
            CommandLine = $null
        }
    }

    $processId = if ($null -ne $instance.process_id) { [int]$instance.process_id } else { $null }
    $process = $null
    if ($null -ne $processId) {
        $process = Get-CimInstance Win32_Process -Filter "ProcessId = $processId" -ErrorAction SilentlyContinue
    }

    return [PSCustomObject]@{
        Exists = $true
        Path = $instancePath
        ParseError = $null
        ProcessId = $processId
        ProcessAlive = $null -ne $process
        CommandLine = if ($null -ne $process) { [string]$process.CommandLine } else { $null }
        Version = if ($null -ne $instance.version) { [string]$instance.version } else { $null }
        AppPath = if ($null -ne $instance.app_path) { [string]$instance.app_path } else { $null }
    }
}

function Test-WorkflowUnityCliRunTestsPreflight {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $instance = Get-WorkflowUnityEditorInstance -RepoRoot $RepoRoot
    if ($null -ne $instance -and $instance.ProcessAlive) {
        return [PSCustomObject]@{
            Allowed = $false
            Reason = "open-editor-owns-project"
            Message = "Unity CLI -runTests is blocked because this project is already open in Unity Editor. Use an in-editor/MCP test route or close the editor before running batchmode tests."
            EditorInstance = $instance
        }
    }

    return [PSCustomObject]@{
        Allowed = $true
        Reason = ""
        Message = "Unity CLI -runTests preflight passed."
        EditorInstance = $instance
    }
}

function Resolve-WorkflowUnityExe {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [string]$UnityPath
    )

    if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
        if (-not (Test-Path -LiteralPath $UnityPath)) {
            throw "Unity executable not found: $UnityPath"
        }

        return $UnityPath
    }

    $projectVersionPath = Join-Path $RepoRoot "ProjectSettings\ProjectVersion.txt"
    if (Test-Path -LiteralPath $projectVersionPath) {
        $versionText = Get-Content -LiteralPath $projectVersionPath -Raw
        if ($versionText -match "m_EditorVersion:\s*(\S+)") {
            $candidate = Join-Path "C:\Program Files\Unity\Hub\Editor" (Join-Path $matches[1] "Editor\Unity.exe")
            if (Test-Path -LiteralPath $candidate) {
                return $candidate
            }
        }
    }

    $where = @(& where.exe Unity 2>$null)
    if ($LASTEXITCODE -eq 0 -and $where.Count -gt 0) {
        return [string]$where[0]
    }

    throw "Unity executable could not be resolved. Pass -UnityPath explicitly."
}
