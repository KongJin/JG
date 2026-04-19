# JG Doc Map

Use this file as a quick index. Load only the files relevant to the task.

## Core Rules

- `AGENTS.md`
- `agent/architecture.md`
- `agent/anti_patterns.md`
- `agent/initialization_order.md`
- `agent/state_ownership.md`

## Unity MCP

- `Assets/Editor/UnityMcp/README.md`
- `ProjectSettings/UnityMcpPort.txt`
- `tools/mcp-test-lobby-scene.ps1`
- `tools/mcp-lobby-to-game.ps1`

## Scene And Runtime Contracts Often Needed For `JG_GameScene`

- `agent/initialization_order.md`
- relevant `*Setup`, `*Bootstrap`, `*Root`, and scene-facing contracts under `Assets/Scripts/Features/Player/`
- relevant `*Setup`, `*Bootstrap`, `*Root`, and scene-facing contracts under `Assets/Scripts/Features/Wave/`
- relevant `*Setup`, `*Bootstrap`, `*Root`, and scene-facing contracts under `Assets/Scripts/Features/Skill/`
- relevant scene-facing contracts under `Assets/Scripts/Features/Combat/` and `Assets/Scripts/Features/Enemy/`
- shared scene/runtime contracts under `Assets/Scripts/Shared/`

## Fast Search Patterns

- Scene hierarchy or object names:
  - `rg -n "GameSceneBootstrap|JG_GameScene|ObjectiveCore|WaveSystems|StatusSystems|PlayerSceneTickers" Assets/Scenes Assets/Scripts`
- Scene/runtime contract hints for a feature:
  - `rg -n "씬 의존성|씬 계약|initialization|late-join|Runtime Lookup|Setup|Bootstrap|Root" Assets/Scripts/Features/<Name> Assets/Scripts/Shared`
- MCP helper usage:
  - `rg -n "UnityMcp|mcp-test|/scene/|/component/" Assets/Editor tools`
