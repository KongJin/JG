# UI Flow Gap Review

- Generated: 2026-04-27 13:22 KST
- Capture run: `artifacts/unity/game-flow/ui-flow-review-20260427-132056/ui-flow-capture-run.json`
- Contact sheet: `artifacts/unity/game-flow/ui-flow-review-20260427-132056/00-contact-sheet.png`
- Viewport: mobile portrait GameView, `390x844`
- Runtime result: base flow capture `success: true`, console error count `0`
- Result smoke:
  - Victory: `11-victory-smoke.json`, `success: true`, `newErrorCount: 0`
  - Defeat: `12-defeat-smoke.json`, `success: true`, `newErrorCount: 0`

## Capture Index

| Flow | Evidence | Verdict |
|---|---|---|
| Lobby home | `01-lobby-home.png` | functional, visually sparse |
| Garage home | `02-garage-home.png` | functional, needs visual cleanup |
| Garage settings | `03-garage-settings.png` | functional, needs hierarchy/copy pass |
| Lobby return | `04-lobby-return.png` | functional |
| Create room filled | `05-create-room-filled.png` | functional |
| Room detail | `06-room-detail.png` | improved, needs context polish |
| Room ready | `07-room-ready.png` | functional |
| Battle entry | `08-battle-entry.png` | functional, text density weak |
| Battle placement preview | `09-battle-placement-preview.png` | functional, placement affordance weak |
| Battle after placement | `10-battle-after-placement.png` | functional |
| Victory result | `11-battle-victory.png` | visible, needs result overlay polish |
| Defeat result | `12-battle-defeat.png` | visible, needs result overlay polish |

## Priority Gaps

### P0 - None

No captured flow is visually blocked. Lobby -> Garage -> room create -> ready -> BattleScene -> placement -> victory/defeat result is reachable in mobile portrait.

### P1 - Result overlays still look like debug/contracts

Evidence:

- `11-battle-victory.png`
- `12-battle-defeat.png`

Issues:

- CTA text exposes key-like copy: `exit_to_app RETURN_TO_LOBBY`.
- Result body mixes Korean labels with raw strings such as `waves`, `WAVE_CLEARED`, `FINAL_WAVE`.
- Progress bar is visually detached and too close to the title area.
- Battle command dock remains visible behind the modal, which weakens end-state focus.

Recommended next pass:

- Replace key-like CTA/body strings with final Korean copy.
- Make the result modal own the end-state focus: stronger scrim or hide/disable command dock.
- Align progress bar and title into a deliberate header layout.

### P1 - Battle HUD is readable only as a debug HUD

Evidence:

- `08-battle-entry.png`
- `09-battle-placement-preview.png`
- `10-battle-after-placement.png`

Issues:

- Top status cards use tiny text and raw system-like labels.
- Unit slot cards are cramped; `INSUFFICIENT_ENERGY` reads as a key, not UI copy.
- Placement preview has a visible green bar, but the actual placement action/area affordance is weak.

Recommended next pass:

- Define final Battle HUD copy and scale for mobile portrait.
- Convert raw status keys to player-facing labels.
- Add a clear placement confirmation affordance or stronger placement zone feedback.

### P2 - Garage main screen has overlap and density problems

Evidence:

- `02-garage-home.png`

Issues:

- Preview area text/model labels overlap around `Bastion`, HP, and ASPD.
- Garage page has many dense controls: roster, tabs, preview, Nova Parts, save dock, operation summary.
- Nova Parts rows are too small to evaluate parts confidently.
- Save dock and operation summary compete at the bottom.

Recommended next pass:

- Separate 3D preview from stat text and selected unit title.
- Give Nova Parts a focused expanded state or a separate panel mode.
- Decide whether operation summary belongs in Garage primary surface or a secondary drawer.

### P2 - Lobby home is functional but under-informs the player

Evidence:

- `01-lobby-home.png`
- `05-create-room-filled.png`

Issues:

- Default fields `4` and `Pilot` lack labels/context once the player starts typing.
- Empty room state occupies most of the screen without a clear next action beyond Create.
- No visible account/garage readiness signal before entering room flow.

Recommended next pass:

- Add clearer field labels or compact helper text.
- Add player readiness/garage status summary near Create.
- Improve empty state copy for "no rooms" and explain create/join expectation.

### P2 - Room detail is improved but still context-light

Evidence:

- `06-room-detail.png`
- `07-room-ready.png`

Issues:

- Large empty area makes the room feel unfinished with only one member.
- Member row has tiny status glyphs with little meaning.
- `Red` / `Blue` team buttons lack selected/meaning context.

Recommended next pass:

- Add member-slot placeholders or compact roster rows.
- Make ready/team state visually explicit.
- Confirm whether Start should look available in single-client smoke or indicate 2-player requirement in actual multiplayer UX.

### P3 - Garage settings is operational but not yet product-grade

Evidence:

- `03-garage-settings.png`

Issues:

- Account action buttons dominate the overlay.
- Delete/Logout/Google actions are close together with little hierarchy.
- Close affordance is small compared with destructive actions.

Recommended next pass:

- Split account identity, linking, logout, and delete into hierarchy groups.
- De-emphasize destructive action unless explicitly opened.
- Confirm Account Settings Stitch source and Unity prefab contract are aligned.

## Missing Captures

These were not covered by this pass and should be captured in follow-up passes:

- Garage Nova Parts search -> select -> apply flow.
- Garage save success/failure feedback.
- Account Settings full interaction and delete confirmation.
- Room list populated/join existing room flow.
- Result return-to-lobby CTA after victory and defeat.
- 2-client host/join and late-join screens. Current Phase 5 runner remains blocked.
- WebGL player capture. This pass is Editor GameView only.

## Re-review

- Over-scope check: This report does not prescribe architecture or new systems; it only classifies visible gaps from captured flows.
- Under-scope check: Functional pass, visual gaps, missing captures, and follow-up priority are all separated.
- Rereview: clean for capture review; remaining work is visual/copy polish and additional flow captures, not runtime blocker repair.
