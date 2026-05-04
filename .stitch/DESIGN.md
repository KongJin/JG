# Design System: JG Tactical Hangar Mobile
**Project Title:** JG UI Refresh - Lobby Garage GameScene
**Project ID:** 11729197788183873077
**Baseline Date:** 2026-04-19

## 1. Visual Theme & Atmosphere

Design every screen as a **mobile-first tactical hangar interface** for a co-op PvE build defense game.
The mood is **dark, compact, practical, and command-oriented**, not cinematic key art and not generic SaaS admin UI.
The player fantasy is not "browsing menus" but **preparing units, reading pressure, and making clear summon decisions**.

Use this atmosphere:

- Deep off-black hangar canvas with layered metal-like panels
- Strong information hierarchy before decoration
- Compact cards with low wasted space
- Calm base surfaces with limited bright accents
- Finished empty states that still look intentional and useful

The visual language should feel like:

- **Nova-style garage preparation** in the garage flow
- **Short readable Clash-style summon clarity** in battle HUD flow
- A unified tactical product, not separate themes per page

## 2. Platform & Layout Baseline

- Platform: Mobile-first
- Validation frame: **390x844**
- Default color mode: Dark
- Default density: Medium-high
- Default variance: Controlled asymmetry, not chaotic
- Default roundness: Compact rounded corners in the **8-12px** range

Never design desktop-first structures and then compress them.
Every important flow must read correctly in a single vertical mobile pass.

## 3. Product Reading Order

### Lobby

The first read must be:

1. Open rooms / room list
2. Create room
3. Garage summary entry

Rules:

- Room list is the primary surface
- Create room is secondary, never a hero banner
- Garage summary is visible but quieter than room actions
- Empty room state must look like a complete tactical panel, not a blank container

### Lobby Accepted Source Freeze - 2026-05-04

Use the following Stitch screens as the current accepted Lobby flow baseline inside project `11729197788183873077`.
Treat each state as its own source freeze and do not mix visual decisions across unrelated candidates.

1. Populated matchmaking lobby
   - Screen: `Matchmaking Lobby`
   - Screen ID: `d64845cd097a4a30b8fb2e1fb4435347`
   - Local export: `.stitch/designs/set-a-matchmaking-lobby-populated-v2.{html,png}`
2. Empty room list baseline
   - Screen: `Matchmaking Lobby (Empty State)`
   - Screen ID: `0e5a4d83630e479eb81eb1d7463dfac5`
   - Local export: `.stitch/designs/set-a-matchmaking-lobby-empty-state-v2.{html,png}`
3. Create room overlay
   - Screen: `Create Operation Modal Overlay`
   - Screen ID: `d308a69f1e684a1189b3681671bac049`
   - Local export: `.stitch/designs/set-a-create-operation-modal-overlay-v2.{html,png}`
4. Selected room detail state
   - Screen: `Room Detail Panel - Selected State`
   - Screen ID: `f716ca6e17f84ba7bd9338838cf43752`
   - Local export: `.stitch/designs/set-a-room-detail-panel-selected-v2.{html,png}`

Current design judgment:

- Main home baseline favors the empty-state lobby over the older sparse list layout.
- Room selection should escalate into the selected-room detail panel before join.
- Room creation should stay an overlay flow, not a peer full-screen page.

### Garage

The first read must be:

1. Current roster slots
2. Focused editor
3. Preview + summary
4. Save dock

Rules:

- Garage should feel like a **single scroll workspace**
- Slot selection comes before deep part browsing
- Settings and account surfaces are auxiliary
- Save action must remain the clearest persistent CTA
- Empty preview, empty slot, and disconnected states must still look complete

### Battle HUD

The first read must be:

1. Selectable unit slots
2. Current energy state
3. Placement feedback
4. Core pressure / wave state

Rules:

- The player should immediately understand `select -> place -> summon`
- Placement errors and "cannot afford" states must be understood in-context
- Visual emphasis should favor tactical readiness, not flashy effects

## 4. Color Palette & Roles

Use one stable palette across all generated screens.
Do not drift between warm-gray and cool-gray families.

- **Hangar Canvas** (`#111827`) — main background and deepest structural surface
- **Steel Surface** (`#1F2937` inferred) — primary cards and control panels
- **Raised Plate** (`#273449` inferred) — selected or elevated sections
- **Signal Orange** (`#F59E0B`) — primary CTA, key active action, confirmed summon/save emphasis
- **Command Blue** (`#5EB6FF`) — secondary action, focus state, informational highlight
- **Soft Fog Text** (`#E5E7EB` inferred) — primary text on dark surfaces
- **Muted Readout** (`#9CA3AF` inferred) — secondary text, hints, inactive labels
- **Danger Red** (`#EF4444` inferred) — destructive or critical warning only

Color rules:

- Orange leads action
- Blue supports navigation, focus, or secondary emphasis
- Do not let more than two strong accents compete in one viewport
- Never use purple accents, neon glows, or rainbow gradients
- Never use pure black `#000000`

## 5. Typography Rules

- **Headline / Section:** `Space Grotesk`
- **Body / Supporting UI:** `IBM Plex Sans`
- **Numeric / tactical readout feel:** use tighter, more structured styling when showing energy, slot state, or compact metadata

Typography behavior:

- Prefer short, confident labels over long explanatory sentences
- Use strong section titles and restrained body copy
- Let hierarchy come from placement, weight, and contrast before extreme size jumps
- Default copy language is **Korean**
- Prefer numbers, tags, and short state labels before descriptive prose

Copy tone:

- Short
- Practical
- System-like
- No marketing slogans
- No fake metrics or invented system statistics

## 6. Component Styling

### Buttons

- Primary buttons use **Signal Orange**
- Secondary buttons use dark surfaces with blue or subtle outline emphasis
- Destructive buttons use restrained red, clearly separated from primary actions
- Buttons should feel compact and pressable, not glossy

### Cards and Panels

- Use cards to express hierarchy, not to create a grid for its own sake
- Large sections should feel like nested tactical plates
- Separate regions using surface changes, spacing, and accent bars before using visible borders
- Empty states must include a clear title, short guidance, and a finished frame

### Slot Cards

- Slot state must be instantly legible: selected, filled, empty, disabled
- Selected slot gets the strongest local emphasis
- Empty slot still looks like a valid tactical container, not a missing asset

### Editors and Summary Blocks

- Focused editor should dominate the active work area
- Preview and summary should read as evaluation surfaces, not placeholders
- Delta or role changes should be visible at a glance

### Dialogs and Overlays

- Modal surfaces should interrupt clearly but not feel louder than the entire screen system
- Loading overlays should feel integrated into the tactical UI, not generic spinners on black
- Confirm dialogs must isolate destructive intent clearly

## 7. Layout Principles

- Prefer vertical flow over fragmented tabbed mini-panels
- Use asymmetric balance, but keep internal alignment strict
- Avoid generic equal-width 3-card rows
- Use spacing to separate priorities before adding more decoration
- Keep important persistent actions anchored near the thumb zone
- Minimum touch target: **44px**

Unity translation rules to respect:

- Generated layouts are **visual drafts**, not final runtime truth
- Scene-owned layout remains the final implementation contract
- Favor blocks that can map cleanly to Unity layout groups and stable roots
- Do not depend on overlapping absolute-positioned hero compositions

## 8. Motion & Feedback

- Motion should reinforce readiness, focus, and confirmation
- Use small transitions for selection, hover, focus, and save feedback
- Favor opacity and transform-based motion only
- Avoid dramatic floating, glowing, or ornamental loops

Recommended feel:

- Crisp state changes
- Short staggered reveals for lists
- Subtle emphasis on selected slot, active editor, and successful save

## 9. Anti-Patterns

Never generate any of the following:

- Desktop-first dashboard layouts
- Marketing-site hero sections
- Generic SaaS admin card grids
- Purple or neon sci-fi glow aesthetics
- Loud popups that overpower the base page
- Empty black boxes as placeholders
- Overwritten screens packed with equal-priority panels
- Long explanatory copy blocks
- Fake numbers, fake uptime, fake KPIs, fake percentages
- Decorative icons without tactical meaning
- Rounded toy-like buttons or bubbly consumer UI
- "Scroll to explore" style filler copy
- Generic English placeholder naming when Korean product copy is expected

## 10. Prompting Reminder

When generating or editing screens in Stitch:

- Keep the output mobile-first at **390x844**
- State the exact reading order for the target surface
- Name the primary CTA and secondary CTA explicitly
- Always specify empty, loading, and error-state expectations
- Preserve one unified tactical hangar visual language across Lobby, Garage, and Battle HUD
