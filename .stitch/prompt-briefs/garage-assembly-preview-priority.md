# Garage Prompt Brief - Assembly Preview Priority

**Project ID:** `11729197788183873077`
**Baseline Screen:** `Tactical Unit Assembly Workspace - Revised`
**Baseline Screen ID:** `5616b99f60774415826ad6ad33c7c4d1`
**Date:** `2026-05-01`
**Status:** prompt brief only; not an accepted source freeze

## Goal

Move the current Garage direction from "part list first" toward "assembled unit first".

The player should immediately read the screen as a unit assembly workspace:

1. which slot is active
2. which part category is active
3. what unit is currently assembled
4. what part is selected
5. where to save

This brief is based on the current Unity Garage SetB candidate after the lower detail panel was removed and the selected part preview was compacted.

## Current Friction

- The part list still feels heavier than the final assembled unit preview.
- `Selected Part Preview` and `Unit Preview` can compete because both read as preview cards.
- The final assembled unit preview should be the visual reward of the Garage, not a secondary panel.
- Large stat/detail panels should stay removed; stats need to remain compact.

## Required Reading Order

1. `Top App Bar`
2. `Slot Selector`
3. `Part Focus Tabs`
4. `Assembled Unit Preview`
5. `Selected Part Compact Preview`
6. `Part Picker`
7. `Save Dock`

## Non-Negotiable Rules

- Mobile-first at `390x844`
- Korean-first user-facing copy
- Dark tactical hangar atmosphere
- Single scroll workspace
- `조립 미리보기` is primary
- `선택 부품` is secondary
- Keep `기동 / 프레임 / 무장` as part focus controls
- Keep `저장 및 배치` as the strongest CTA
- Do not restore the large lower stat/detail panel
- Avoid marketing-site layouts
- Avoid generic SaaS card grids
- Avoid decorative blobs, oversized text, or long explanations

## Target Edit Prompt

```md
Create a refined mobile-first Garage / unit assembly screen for a Nova1492-inspired tactical sci-fi game.

Context:
- This is a redesign pass based on an existing Unity Garage UI.
- Current screen already has: top app bar, slot selector, part focus tabs, part list, compact selected part preview, assembled unit preview, save button.
- The current weakness is hierarchy: the part list feels more important than the final assembled unit preview.
- Redesign the same flow so the screen reads first as "I am assembling a unit", not merely browsing a list.

Viewport and style:
- Mobile-first, designed for 390x844.
- Dark tactical hangar dashboard, compact and readable.
- Use black/zinc backgrounds, restrained blue active states, amber primary save action.
- Avoid marketing layouts, generic SaaS dashboards, decorative blobs, oversized explanatory copy.
- Keep cards squared or lightly rounded; practical game tool feeling.

Required user-facing Korean labels:
- 격납고 관리
- 기동 / 프레임 / 무장
- 조립 미리보기
- 선택 부품
- 저장 및 배치

Information hierarchy:
1. Top compact header: title, roster/status, settings icon.
2. Slot selector: active slot and empty slots visible near top.
3. Part focus tabs: 기동 / 프레임 / 무장.
4. Large assembled unit preview: this is the primary visual reward. It should be bigger and more central than the selected part preview. It should look like a finished stage/grid, not an empty placeholder. Include a visible assembled mech/unit silhouette or model placeholder.
5. Compact selected part preview: smaller, secondary. It can sit beside/above part list with selected part name and tiny stat chips.
6. Part picker: search + scrollable rows. It can be shorter than before; 5-6 visible rows are enough.
7. Persistent bottom save dock: 저장 및 배치 is the strongest action.

Specific layout preference:
- Put 조립 미리보기 above the part picker, immediately after the part focus tabs, so it becomes the main focus of the first viewport.
- Put 선택 부품 as a compact horizontal card below the assembled preview or attached to the part picker header.
- Remove any large lower stat/detail panel. Stats should be compact chips in rows/cards.
- Keep the flow a single scroll workspace.

Content sample:
- Active slot: UNIT_01 / 전선 고정
- Tabs: 기동 active, 프레임, 무장
- Selected part: 로드런너
- Part rows: 로드런너, 크롤러, 델피누스, 스파이더, 피파울
- Preview: compact assembled unit silhouette with three colored part hints

Acceptance target:
- In the first viewport, the assembled unit preview is visibly dominant.
- The player can immediately answer: which slot is active, which category is active, what unit is assembled, what part is selected, and where to save.
```

## Tool Attempt Log

- `generate_variants` was attempted against `5616b99f60774415826ad6ad33c7c4d1`, but the Stitch tool rejected the variant options as invalid.
- `generate_screen_from_text` was attempted in project `11729197788183873077`; the call timed out after 120 seconds and no new screen appeared in `list_screens` during the immediate follow-up check.
- Do not treat this brief as a source freeze. Pick or generate an accepted screen first, then export/freeze `screen.html` and `screen.png`.
