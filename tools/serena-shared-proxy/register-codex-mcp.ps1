param(
    [string]$ServerName = "serena"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverScript = Join-Path $scriptDir "serena-shared-proxy.mjs"

if (-not (Test-Path -LiteralPath $serverScript)) {
    Write-Error "Serena shared proxy script not found: $serverScript"
    exit 1
}

$resolvedServerScript = (Resolve-Path -LiteralPath $serverScript).Path

& codex mcp remove $ServerName *> $null

Write-Host "Registering MCP server '$ServerName' through Serena shared proxy..."
& codex mcp add $ServerName -- node $resolvedServerScript

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Current MCP server config:"
& codex mcp get $ServerName
