# Set D Handoff - Battle HUD

> Accepted set date: 2026-04-19

## Accepted Screens

- Main baseline: `Refined Battle HUD - Tactical Command` (`03af04196bfa4615b06d4284c66cf1f8`) -> `set-d-battle-hud-baseline.{html,png}`
- Warning state: `Battle HUD - Critical Core Warning State` (`84f0abf5723c4ad6960a7d849de527da`) -> `set-d-low-core-warning.{html,png}`
- Supporting popup variant: Stitch title `Refined Battle HUD - Tactical Command` (`e41a9d7e3b2946e681f68cac1b4f4edf`) -> local role/export `set-d-unit-stats-popup.{html,png}`
- Non-baseline project screen: `Battle HUD - Tactical View` (`bf3d08890f2d4a4e98f81c25e14d6073`) remains a project-side pre-refinement candidate only and should not be used as the current HUD baseline or Unity translation source

Title sync note:
The Stitch project currently uses the same title for `03af04196bfa4615b06d4284c66cf1f8` and `e41a9d7e3b2946e681f68cac1b4f4edf`.
In local handoff usage, `e41a9d7e3b2946e681f68cac1b4f4edf` remains the supporting popup/detail variant mapped to `set-d-unit-stats-popup`.

## Intent

Battle HUD should teach and reinforce one loop:

`select -> place -> summon`

The player should immediately see summonable units, current energy, placement validity, and core pressure before reading any decorative combat signal.

## Reading Order

1. `Summon Command Bar`
2. `Current Energy`
3. `Placement Feedback / Selected Unit State`
4. `Core HP`
5. `Wave / Countdown`

## Screen Block Map

- `Summon Command Bar`
  - Purpose: expose summonable units and current selected command
  - Must survive in Unity as the dominant control strip and main player-facing surface
- `Current Energy`
  - Purpose: answer "can I summon this now?" without mental math
  - Must survive in Unity visually tied to summon action
- `Placement Feedback / Selected Unit State`
  - Purpose: explain whether the current action is valid and what the selected unit implies
  - Must survive in Unity near the active summon context, not detached near the top HUD
- `Core HP`
  - Purpose: keep base survival visible as the main battle risk
  - Must survive in Unity as a strong warning surface, but not louder than active summon control in baseline state
- `Wave / Countdown`
  - Purpose: provide pacing and encounter progress
  - Must survive in Unity as orientation info, not as the main command surface

## CTA Priority Matrix

- Primary CTA: select a summon slot
- Primary conditional CTA: confirm placement when a valid selected summon exists
- Secondary CTA: read placement validity / selected-unit context
- Tertiary CTA: inspect wave, countdown, and non-critical battle stats
- Optional tertiary CTA: unit stats popup entry

Priority rules:

- The HUD should always answer "what can I summon right now?" before "how is the battle trending?"
- `cannot afford`, `invalid placement`, and `valid ready-to-place` states must feel different at a glance.
- Critical core warning may escalate urgency, but it must not hide the summon command loop.

## CTA Priority

- Primary: selected summon slot / confirmable summon action
- Secondary: placement guidance and selected-unit context
- Tertiary: wave or status information

The HUD should not compete with itself using equal-priority combat chrome.

## Covered States

- Baseline tactical command state
- Critical core warning state
- Unit stats popup / detail inspection state

## Unity Translation Targets

- Scene root: `/SystemsRoot/GameSceneRoot`
- HUD command root: `/HudCanvas/UnitSummonUi`
- Summon slot strip: `/HudCanvas/UnitSummonUi/SlotRow`
- Placement feedback: `/HudCanvas/PlacementErrorView`
- Top wave region: `/HudCanvas/WaveUi/TopBar`
- Outcome overlay root reference: `/HudCanvas/WaveUi/WaveEndOverlay`

## Translation Rules

- Treat the bottom summon command zone as the face of the HUD.
- Energy must read as a direct summon resource, not as a detached stat label.
- Placement validity should live close to summon context and battlefield targeting feedback.
- Core warning can escalate color and urgency, but must not bury summon readability.
- The stats popup should read like a tactical inspection card, not like an RPG inventory panel.
- If HUD density becomes crowded, reduce passive battle chrome before weakening summon-slot clarity or placement-state contrast.

## Validation Focus

- First glance makes summon affordance obvious
- Selected slot, cannot-afford state, and invalid-placement feedback are all distinguishable
- Core warning escalates clearly without breaking command clarity
- Wave information stays readable but secondary
- No MOBA-shop clutter or FPS-style overlay noise leaks into the runtime HUD

## Assumptions

- Drag placement automation is still a known runtime validation risk, so this handoff focuses on hierarchy and wording rather than prescribing drag-specific ornamentation.
- The HUD succeeds when a first-time player can infer the `select -> place -> summon` loop from the HUD alone.
