# Serena Shared Proxy MCP

> 마지막 업데이트: 2026-05-03
> 상태: active
> doc_id: tools.serena-shared-proxy-readme
> role: reference
> owner_scope: Serena Shared Proxy MCP 실행 reference와 Codex MCP registration guide
> upstream: repo.agents, docs.index, ops.document-management-workflow
> artifacts: `tools/serena-shared-proxy/`

Local stdio MCP proxy for Codex that keeps Serena/OmniSharp singleton per repo.

Codex may start one MCP server process per agent/session. Direct Serena registration
therefore spawns one Serena server and one OmniSharp process per agent. This proxy
keeps the Codex-facing process lightweight:

```text
Codex agent A -> serena-shared-proxy -> shared daemon -> Serena -> OmniSharp
Codex agent B -> serena-shared-proxy ----^
Codex agent C -> serena-shared-proxy ----^
```

The daemon listens on a deterministic localhost port derived from the repo root.
`tools/list` returns immediately from a live cache or fallback schema while the
real Serena backend warms up in the background. Actual `tools/call` requests wait
for the shared backend and are serialized through that one process.

This is intended for read-heavy symbolic lookup. Serena write/edit tools still
go through one shared backend, so use them carefully and avoid concurrent
mutation work.

The proxy adds one local diagnostic tool:

- `serena_shared_proxy_status`: reports daemon PID, backend PID, readiness, and
  log/cache paths.

## Codex Config

Use this instead of direct Serena registration:

```toml
[mcp_servers.serena]
command = 'C:\Program Files\nodejs\node.exe'
args = ['C:\Users\SOL\Documents\JG\tools\serena-shared-proxy\serena-shared-proxy.mjs']
```

## Environment

- `SERENA_SHARED_PROXY_PORT`: optional fixed localhost port.
- `SERENA_SHARED_PROXY_REPO_ROOT`: optional repo root override.
- `SERENA_SHARED_PROXY_LOG_DIR`: optional log directory.
- `SERENA_SHARED_PROXY_BACKEND_COMMAND`: optional Serena command override.
- `SERENA_SHARED_PROXY_BACKEND_ARGS_JSON`: optional JSON array of Serena args.
- `SERENA_SHARED_PROXY_IDLE_TIMEOUT_MS`: daemon idle timeout, default 30 minutes.
- `SERENA_SHARED_PROXY_BACKEND_START_TIMEOUT_MS`: Serena initialization timeout,
  default 7 minutes.
- `SERENA_SHARED_PROXY_BACKEND_TOOL_CALL_TIMEOUT_MS`: Serena tool call timeout,
  default 7 minutes.

Logs and the live Serena tool-schema cache are written to
`Temp/SerenaSharedProxy/`.
