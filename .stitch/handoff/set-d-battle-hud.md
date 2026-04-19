# Set D Handoff - Battle HUD

> Accepted set date: 2026-04-19

## Accepted Screens

- Main baseline: `Refined Battle HUD - Tactical Command` (`03af04196bfa4615b06d4284c66cf1f8`)
- Warning state: `Battle HUD - Critical Core Warning State` (`84f0abf5723c4ad6960a7d849de527da`)
- Supporting popup variant: unit stats popup variant on battle HUD (`e41a9d7e3b2946e681f68cac1b4f4edf`)

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

## Validation Focus

- First glance makes summon affordance obvious
- Selected slot, cannot-afford state, and invalid-placement feedback are all distinguishable
- Core warning escalates clearly without breaking command clarity
- Wave information stays readable but secondary
- No MOBA-shop clutter or FPS-style overlay noise leaks into the runtime HUD

## Assumptions

- Drag placement automation is still a known runtime validation risk, so this handoff focuses on hierarchy and wording rather than prescribing drag-specific ornamentation.
