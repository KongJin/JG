param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

& node "$PSScriptRoot\test-garage-presentation-structure.mjs" $RepoRoot
exit $LASTEXITCODE
