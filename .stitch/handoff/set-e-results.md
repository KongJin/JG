# Set E Handoff - Battle Results

> Accepted set date: 2026-04-19

## Accepted Screens

- Victory result: `Mission Victory Summary` (`895e6c337c2d47da92a8e28d01ea2376`)
- Defeat result: `Mission Defeat Summary` (`83c5d82066184ef4acb7676e3e823db8`)

## Intent

Result overlays should conclude the battle loop with one strong card, one clear outcome, short tactical summary, and one obvious return path.
They should feel connected to the battle HUD language, not like a separate reward screen product.

## Reading Order

1. Outcome title
2. Short mission summary
3. Key result stats
4. Return / next action

## CTA Priority

- Primary: `Return To Lobby`
- Secondary: any optional review or summary action if later added

The result CTA should be singular and calming.

## Covered States

- Victory summary
- Defeat summary

## Unity Translation Targets

- Overlay root: `/HudCanvas/WaveUi/WaveEndOverlay`
- Main card root: `/HudCanvas/WaveUi/WaveEndOverlay/Panel`
- Outcome text: `/HudCanvas/WaveUi/WaveEndOverlay/Panel/ResultText`
- Stats text: `/HudCanvas/WaveUi/WaveEndOverlay/Panel/StatsText`
- Return action: `/HudCanvas/WaveUi/WaveEndOverlay/Panel/ReturnToLobbyButton`

## Translation Rules

- Keep the overlay card compact and centered on closure.
- Victory can use stronger uplift, but still within the same tactical palette discipline.
- Defeat should emphasize recovery and clarity, not punishment or red-saturated overload.
- Stats should stay short and scannable rather than turning into a report wall.

## Validation Focus

- Outcome title is readable immediately
- `ReturnToLobbyButton` is unmistakably the main next step
- Victory and defeat differ clearly in tone while sharing one component grammar
- Overlay does not feel detached from the prior HUD visual language

## Assumptions

- No rewards chest, loot carousel, or progression summary is included in the baseline result flow for this pass.
