---
name: unity-ui
description: "Edit Unity UI prefabs/scenes without GUI: RectTransform, anchors, colors, text, .prefab, .unity, and serialized UI changes."
---

# Unity UI

Edit Unity UI assets such as prefabs and scenes without relying on visual tools.

## Purpose

Use this skill for targeted UI changes in serialized Unity assets while keeping diffs small and reviewable.

Typical targets:
- `.prefab`
- `.unity`
- `.asset`

Typical changes:
- RectTransform anchor or position tweaks
- Image color or sprite swaps
- Text content updates
- Small hierarchy additions inside isolated UI prefabs

## Core Principles

### 1. Minimal diffs

Only change the fields needed for the task.
- Do not reformat unrelated YAML
- Do not reorder objects
- Keep surrounding serialization intact

### 2. Prefer prefab isolation

When possible, edit dedicated UI prefabs instead of scene-embedded UI.

Preferred structure:

```text
Assets/
  Prefabs/
    UI/
      HUD.prefab
      HealthBar.prefab
      Dialogs/
        ConfirmDialog.prefab
```

### 3. Avoid blind reserialization

Do not open scenes or prefabs in the Unity Editor just to "check" them if that risks broad serialization churn.

### 4. One logical change at a time

Good:
- "Move the health bar 50 px right"
- "Change the top banner color"

Avoid bundling many unrelated UI edits into one pass.

## YAML Reminders

Unity UI assets are serialized as YAML. Common blocks:

### GameObject

```yaml
--- !u!1 &123456789
GameObject:
  m_Name: HealthBar
  m_Component:
  - component: {fileID: 123456790}
```

### RectTransform

```yaml
--- !u!224 &123456790
RectTransform:
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 0, y: 1}
  m_AnchoredPosition: {x: 100, y: -50}
  m_SizeDelta: {x: 200, y: 30}
  m_Pivot: {x: 0, y: 1}
```

### Image-like component values

```yaml
m_Color: {r: 1, g: 1, b: 1, a: 1}
m_Sprite: {fileID: 21300000, guid: <sprite-guid>, type: 3}
```

## Safe Workflow

### Step 1. Identify the target object

Search by object name or nearby serialized values.

Examples:
- `rg -n "m_Name: HealthBar" Assets`
- `rg -n "m_AnchoredPosition" Assets/Prefabs/UI/HUD.prefab`

### Step 2. Make the smallest possible edit

Example:

```yaml
# Before
m_AnchoredPosition: {x: 100, y: -50}

# After
m_AnchoredPosition: {x: 150, y: -50}
```

### Step 3. Sanity-check the result

Verify:
- The file still has valid YAML-like Unity serialization structure
- FileIDs and references were not accidentally changed
- The diff size is proportional to the requested change

### Step 4. Give a visual verification checklist

Because headless verification is limited, always tell the user what to confirm locally.

Example checklist:
- Health bar appears in the top-left corner
- Size is still 200x30
- Color changed as intended
- Text still renders correctly

## Common Tasks

### Change color

Update:

```yaml
m_Color: {r: 1, g: 0.5, b: 0, a: 1}
```

### Change anchoring

Top-left:

```yaml
m_AnchorMin: {x: 0, y: 1}
m_AnchorMax: {x: 0, y: 1}
```

Stretch top:

```yaml
m_AnchorMin: {x: 0, y: 1}
m_AnchorMax: {x: 1, y: 1}
```

Center:

```yaml
m_AnchorMin: {x: 0.5, y: 0.5}
m_AnchorMax: {x: 0.5, y: 0.5}
```

### Add a small UI child

Only do this when:
- The parent prefab is clearly isolated
- You understand the local fileID relationships
- The task is small and specific

If the edit would require broad hierarchy surgery, prefer Unity Editor automation or a safer prefab workflow.

## Policies

- Change only what is necessary
- Prefer prefabs over scene-embedded UI
- Avoid broad reserialization
- Review `git diff` before finishing
- Always provide local visual verification steps
