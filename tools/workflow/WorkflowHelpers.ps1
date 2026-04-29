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

function Resolve-WorkflowAbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
}

function Ensure-WorkflowParentDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
}

function Get-WorkflowUnityResourceLock {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [string]$LockPath = "Temp/UnityMcp/unity-resource.lock"
    )

    $absolutePath = Resolve-WorkflowAbsolutePath -RepoRoot $RepoRoot -Path $LockPath
    if (-not (Test-Path -LiteralPath $absolutePath)) {
        return $null
    }

    $raw = ""
    $json = $null
    try {
        $raw = (Get-Content -LiteralPath $absolutePath -Raw -ErrorAction Stop).Trim()
        if (-not [string]::IsNullOrWhiteSpace($raw)) {
            $json = $raw | ConvertFrom-Json
        }
    }
    catch {
        return [PSCustomObject]@{
            Exists = $true
            Path = $LockPath
            AbsolutePath = $absolutePath
            ParseError = $_.Exception.Message
            Raw = $raw
            Name = $null
            Owner = $null
            Mode = $null
            Token = $null
            ProcessId = $null
            ProcessAlive = $false
            IsCurrentProcess = $false
            StartedAt = $null
        }
    }

    $processId = if ($null -ne $json -and $null -ne $json.PSObject.Properties["pid"]) { [int]$json.pid } else { $null }
    $process = if ($null -ne $processId) { Get-Process -Id $processId -ErrorAction SilentlyContinue } else { $null }

    return [PSCustomObject]@{
        Exists = $true
        Path = $LockPath
        AbsolutePath = $absolutePath
        ParseError = $null
        Raw = $raw
        Name = if ($null -ne $json.PSObject.Properties["name"]) { [string]$json.name } else { $null }
        Owner = if ($null -ne $json.PSObject.Properties["owner"]) { [string]$json.owner } else { $null }
        Mode = if ($null -ne $json.PSObject.Properties["mode"]) { [string]$json.mode } else { $null }
        Token = if ($null -ne $json.PSObject.Properties["token"]) { [string]$json.token } else { $null }
        ProcessId = $processId
        ProcessAlive = $null -ne $process
        IsCurrentProcess = $null -ne $processId -and $processId -eq $PID
        StartedAt = if ($null -ne $json.PSObject.Properties["startedAt"]) { [string]$json.startedAt } else { $null }
    }
}

function Enter-WorkflowUnityResourceLock {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [string]$Owner = $env:USERNAME,
        [string]$Mode = "exclusive",
        [string]$LockPath = "Temp/UnityMcp/unity-resource.lock",
        [int]$TimeoutSec = 0,
        [double]$PollSec = 0.5,
        [int]$StaleAfterMinutes = 90
    )

    $absolutePath = Resolve-WorkflowAbsolutePath -RepoRoot $RepoRoot -Path $LockPath
    Ensure-WorkflowParentDirectory -Path $absolutePath

    $token = [guid]::NewGuid().ToString("N")
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $payload = [ordered]@{
        name = $Name
        owner = if ([string]::IsNullOrWhiteSpace($Owner)) { "unknown" } else { $Owner }
        mode = $Mode
        token = $token
        pid = $PID
        startedAt = (Get-Date).ToString("o")
    }

    while ($true) {
        try {
            $stream = [System.IO.File]::Open($absolutePath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            try {
                $writer = New-Object System.IO.StreamWriter($stream, [System.Text.Encoding]::UTF8)
                try {
                    $writer.Write(($payload | ConvertTo-Json -Depth 4))
                    $writer.Flush()
                }
                finally {
                    $writer.Dispose()
                }
            }
            finally {
                $stream.Dispose()
            }

            return [PSCustomObject]@{
                Name = $Name
                Owner = $payload.owner
                Mode = $Mode
                Token = $token
                Path = $LockPath
                AbsolutePath = $absolutePath
            }
        }
        catch [System.IO.IOException] {
            $existing = Get-WorkflowUnityResourceLock -RepoRoot $RepoRoot -LockPath $LockPath
            if ($null -ne $existing) {
                if ($existing.ProcessId -gt 0 -and -not $existing.ProcessAlive) {
                    Remove-Item -LiteralPath $absolutePath -Force
                    continue
                }

                $lockItem = Get-Item -LiteralPath $absolutePath
                if ($StaleAfterMinutes -gt 0 -and $lockItem.LastWriteTimeUtc -lt (Get-Date).ToUniversalTime().AddMinutes(-1 * $StaleAfterMinutes)) {
                    Remove-Item -LiteralPath $absolutePath -Force
                    continue
                }
            }

            if ($TimeoutSec -le 0 -or (Get-Date) -ge $deadline) {
                throw ("Unity resource lock is held. lock='{0}' requested='{1}' owner='{2}' existing='{3}'" -f $LockPath, $Name, $Owner, $(if ($null -ne $existing) { $existing.Raw } else { "" }))
            }

            Start-Sleep -Seconds $PollSec
        }
    }
}

function Exit-WorkflowUnityResourceLock {
    param([object]$Lock)

    if ($null -eq $Lock -or [string]::IsNullOrWhiteSpace([string]$Lock.AbsolutePath)) {
        return
    }

    if (-not (Test-Path -LiteralPath $Lock.AbsolutePath)) {
        return
    }

    try {
        $json = Get-Content -LiteralPath $Lock.AbsolutePath -Raw -ErrorAction Stop | ConvertFrom-Json
        if ($null -ne $json.PSObject.Properties["token"] -and [string]$json.token -ne [string]$Lock.Token) {
            return
        }
    }
    catch {
        return
    }

    Remove-Item -LiteralPath $Lock.AbsolutePath -Force
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

    $unityLock = Get-WorkflowUnityResourceLock -RepoRoot $RepoRoot
    if ($null -ne $unityLock -and -not $unityLock.IsCurrentProcess) {
        return [PSCustomObject]@{
            Allowed = $false
            Reason = "unity-resource-lock-held"
            Message = "Unity CLI -runTests is blocked because another workflow owns the Unity resource lock."
            UnityResourceLock = $unityLock
            EditorInstance = $null
        }
    }

    $instance = Get-WorkflowUnityEditorInstance -RepoRoot $RepoRoot
    if ($null -ne $instance -and $instance.ProcessAlive) {
        return [PSCustomObject]@{
            Allowed = $false
            Reason = "open-editor-owns-project"
            Message = "Unity CLI -runTests is blocked because this project is already open in Unity Editor. Use an in-editor/MCP test route or close the editor before running batchmode tests."
            UnityResourceLock = $unityLock
            EditorInstance = $instance
        }
    }

    return [PSCustomObject]@{
        Allowed = $true
        Reason = ""
        Message = "Unity CLI -runTests preflight passed."
        UnityResourceLock = $unityLock
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
