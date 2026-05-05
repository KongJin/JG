---
name: unity-editor-integration
description: "Use Rosie/Highrise-style serialized Unity editor bridge files (active_scene.json, edit.json, trigger files) for safe scene/prefab edits."
---

# Unity Editor Integration

This skill captures the useful parts of the Rosie `use-unity-editor` workflow and adapts them into a Codex-readable repo-imported skill.

Use it only when the project already has a serialized Unity editor bridge in place.

## When This Skill Applies

Look for one or more of these signals:
- `Temp/Highrise/Serializer/active_scene.json`
- `Temp/Highrise/Serializer/all_component_types.json`
- `Temp/Highrise/Serializer/all_prefabs.json`
- `Temp/Highrise/Serializer/prefab_request.json`
- `Temp/Highrise/Serializer/edit.json`
- editor-side serializer scripts under `Assets/Editor/`
- project docs mentioning Rosie, Highrise Studio, `use-unity-editor`, or serialized scene editing

If those files do not exist, do not force this workflow.

## What This Workflow Does

The editor serializes the active scene into JSON, lets the agent queue edits through `edit.json`, and can serialize specific prefabs on demand. The bridge then applies those edits in Unity, saves the scene, and re-serializes results.

Read the exact file contracts in [rosie-serializer-contract.md](references/rosie-serializer-contract.md).

## Core Workflow

### 1. Confirm the bridge is present

Before relying on this skill, verify the serializer output directory and at least one current JSON artifact exist.

Good first checks:
- `Temp/Highrise/Serializer/active_scene.json`
- `Temp/Highrise/Serializer/all_component_types.json`
- `Temp/Highrise/Serializer/all_prefabs.json`

### 2. Read the latest serialized state

Use the latest generated JSON as the source of truth.

Important:
- Do not rely on stale reference IDs from earlier turns
- Re-read the serialized scene before planning edits
- If the file is large, inspect with shell tools rather than blindly dumping the whole thing

Useful targets:
- `active_scene.json` for the scene graph
- `all_component_types.json` for addable components and their editable properties
- `all_prefabs.json` for prefab asset paths
- requested prefab JSON files for prefab-local edits

### 3. Request prefab serialization when needed

Prefab serialization is on-demand in this workflow.

When you need prefab contents:
- write a JSON array of prefab asset paths to `Temp/Highrise/Serializer/prefab_request.json`
- wait for the bridge to emit `<prefab path>.json` files under the same serializer directory

Do not assume prefabs are already serialized.

### 4. Build a small `edit.json`

Edits are queued as a JSON array of operations. Keep batches small and intentional.

Supported edit types captured from the implementation:
- `delete`
- `setProperty`
- `createGameObject`
- `addComponent`
- `saveObjectAsPrefab`

Prefer one logical change group at a time. Large mixed batches are harder to debug.

### 5. Let Unity consume the edits

After `edit.json` is written:
- the bridge applies edits
- open scenes are saved automatically
- modified prefabs are re-serialized automatically
- the active scene is re-serialized shortly after

Do not manually assume success. Re-read the generated JSON and inspect the console output afterward.

### 6. Validate after every mutation

After edits:
- read the updated `active_scene.json`
- inspect any updated prefab JSON files
- check `console.json` if available
- make sure the target object hierarchy and property values actually changed

## Safety Rules

- Use current reference IDs only. Refresh from the latest JSON before editing.
- Use `all_component_types.json` before adding a component or setting unfamiliar properties.
- Do not attach uncompiled Lua scripts to GameObjects.
- Only trigger `.play`, `.stop`, `.focus`, `.rebuild`, `.rebake`, or `.screenshot` when the user explicitly asked for them.
- Prefer small, reviewable edit batches over broad rewrites.
- If the bridge is absent or broken, switch to another workflow rather than improvising this protocol.

## Practical Notes

- Scene root is represented as `SceneRoot`
- Prefab asset references can appear as `prefab:<asset-path>`
- Common editable GameObject properties include:
  - `activeSelf`
  - `isStatic`
  - `layer`
  - `layerName`
  - `tag`
  - `parentGameObject`
- For unknown object or component property types, do not guess the JSON shape. Derive it from current serialized output

## When Not To Use This Skill

Do not use this skill when:
- the project only needs direct YAML prefab edits
- the project already exposes a richer Unity MCP or REST server
- the necessary serializer output files are missing
- the user only wants design guidance rather than editor mutations
