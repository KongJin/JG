# Rosie Serializer Contract

This reference summarizes the file contracts discovered in `pocketzworld/studio-ai` serializer code.

## Output Directory

The serializer writes under:

```text
Temp/Highrise/Serializer/
```

Key files:
- `active_scene.json`
- `all_component_types.json`
- `all_prefabs.json`
- `prefab_request.json`
- `edit.json`
- `console.json` when available
- requested prefab serializations at `<prefab asset path>.json`

## Active Scene

`active_scene.json` represents the whole scene as a serialized `SceneRoot`.

Key details seen in code:
- root object reference ID: `SceneRoot`
- each GameObject has:
  - `referenceId`
  - `objectProperties`
  - `components`
  - `children`
- common `objectProperties`:
  - `name`
  - `activeSelf`
  - `isStatic`
  - `layer`
  - `layerName`
  - `tag`
  - `parentGameObject`
  - `prefabPath`

## Addable Components

`all_component_types.json` lists addable component types plus editable property metadata.

Use it before:
- `addComponent`
- `setProperty` on unfamiliar components

## Prefab Requests

Prefabs are serialized on demand.

Write:

```json
[
  "Assets/Prefabs/UI/HUD.prefab",
  "Assets/Prefabs/UI/Dialog.prefab"
]
```

to:

```text
Temp/Highrise/Serializer/prefab_request.json
```

Then wait for files like:

```text
Temp/Highrise/Serializer/Assets/Prefabs/UI/HUD.prefab.json
```

## Edit Queue

The bridge consumes a JSON array from:

```text
Temp/Highrise/Serializer/edit.json
```

Supported edit operations inferred from `ObjectEditor.ObjectEdit`:

### Delete

```json
{
  "editType": "delete",
  "referenceIdToDelete": "..."
}
```

### Set Property

```json
{
  "editType": "setProperty",
  "referenceIdOfObjectWithPropertyToSet": "...",
  "nameOfPropertyToSet": "anchoredPosition",
  "newPropertyValue": { "x": 10, "y": 20 }
}
```

### Create GameObject

```json
{
  "editType": "createGameObject",
  "referenceIdOfParentGameObject": "SceneRoot",
  "nameOfGameObjectToCreate": "New Panel",
  "referenceIdOfGameObjectToCreate": "new-guid-here",
  "prefabPathForGameObjectToCreate": null
}
```

If `prefabPathForGameObjectToCreate` is set, the bridge instantiates that prefab.

### Add Component

```json
{
  "editType": "addComponent",
  "referenceIdOfGameObjectToAddComponent": "...",
  "componentTypeToAdd": "UnityEngine.CanvasGroup",
  "referenceIdOfComponentToAdd": "new-component-guid"
}
```

### Save Object As Prefab

```json
{
  "editType": "saveObjectAsPrefab",
  "referenceIdOfObjectToSaveAsPrefab": "...",
  "pathToSavePrefabAs": "Assets/Prefabs/UI/NewPanel.prefab"
}
```

## Post-Edit Behavior

After consuming `edit.json`, the serializer code:
- applies each edit
- saves open scenes automatically
- schedules scene re-serialization
- re-serializes modified prefabs automatically

So the expected verification loop is:
1. write `edit.json`
2. wait for Unity to consume it
3. read fresh scene or prefab JSON
4. inspect console output if something failed

## Trigger Files

The editor trigger script supports these files in the project root:
- `.play`
- `.stop`
- `.focus`
- `.rebuild`
- `.screenshot`
- `.rebake`
- `.upload`
- `.catalog`
- `.install`

Only use these when the user explicitly asked for that action.

## Guardrails Derived From Changelog

- use current IDs from fresh serialized output, not stale IDs
- request prefab serialization only when needed
- inspect `console.json` after edits when available
- do not attach uncompiled Lua scripts to GameObjects
- avoid unnecessary editor manipulations unless the user asked for them
