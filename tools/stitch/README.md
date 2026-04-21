# Stitch SDK Helpers

> 마지막 업데이트: 2026-04-21
> 상태: active
> doc_id: tools.stitch-readme
> role: reference
> owner_scope: Stitch SDK helper command reference와 artifact export 안내
> upstream: repo.agents, docs.index, ops.stitch-data-workflow, design.ui-reference-workflow
> artifacts: `tools/stitch/`, `artifacts/stitch/`

JG uses the official `@google/stitch-sdk` package for lightweight Stitch access from the repo.
Stitch owner 문서의 current path는 `docs/index.md`에서 찾고, 이 문서는 SDK 명령 실행 reference로만 유지한다.

## What This Is For

- create a Stitch project for JG UI work
- create a design system for that project
- generate JG-focused mobile screens from prompts
- list accessible Stitch projects
- list screens in a Stitch project
- fetch one Stitch screen's HTML and screenshot into `artifacts/stitch/`
- generate overlay-family draft manifests from `screen-intake` JSON
- compare generated draft manifests against accepted screen manifests

This keeps Stitch as a design-input tool while Unity scene contracts remain the runtime SSOT.

## Setup

1. Install dependencies:

```bash
npm install
```

2. Set your API key:

```bash
$env:STITCH_API_KEY="your-api-key"
```

## Commands

Show help:

```bash
npm run stitch:help
```

List projects:

```bash
npm run stitch:list:projects
```

Create a project:

```bash
npm run stitch:create:project -- --title "JG UI Refresh - Lobby Garage GameScene"
```

Create a design system in an existing project:

```bash
npm run stitch:create:design-system -- --project 15511739434163767886
```

Generate one screen:

```bash
npm run stitch:generate:screen -- --project 15511739434163767886 --name "Lobby" --prompt "A mobile-first tactical sci-fi lobby"
```

Run the JG bootstrap flow:

```bash
npm run stitch:bootstrap:jg
```

List screens in a project:

```bash
npm run stitch:list:screens -- --project 15511739434163767886
```

Fetch one screen from a full Stitch URL:

```bash
npm run stitch:fetch:screen -- --url "https://stitch.withgoogle.com/projects/15511739434163767886?node-id=2225f2733de747d298f1e0c445fbb47c"
```

Fetch one screen from explicit IDs:

```bash
npm run stitch:fetch:screen -- --project 15511739434163767886 --screen 2225f2733de747d298f1e0c445fbb47c
```

Generate one overlay-family draft manifest preview from a `screen-intake`:

```bash
npm run stitch:generate:overlay-draft -- --input .stitch/contracts/intakes/set-c-common-error-dialog.intake.json
```

Generate and compare against the current accepted manifest:

```bash
npm run stitch:generate:overlay-draft -- --input .stitch/contracts/intakes/set-c-common-error-dialog.intake.json --compare .stitch/contracts/screens/set-c-common-error-dialog.screen.json
```

## Output

Each fetched screen is saved under:

```text
artifacts/stitch/<projectId>/<screenId>/
```

With:

- `screen.html`
- `screen.png`
- `meta.json`

The JG bootstrap flow also writes:

- `artifacts/stitch/<projectId>/jg-bootstrap-summary.json`

Overlay-family draft generation writes:

- `artifacts/stitch/generated-drafts/*.generated-draft.screen.json`

The current generator is intentionally narrow:

- supports only `overlay-modal` family intake files
- writes `draft` preview manifests only
- does not mutate accepted `.stitch/contracts/screens/*.json`
- stops instead of guessing when `openQuestions` remain

## Known Gotchas

These are the issues hit during the 2026-04-19 JG Stitch session.
Keep them in mind before assuming the SDK or MCP path is broken.

### 1. API key may be set in Windows user env but not in the current shell

If Stitch commands say `STITCH_API_KEY is not set`, first check whether the key
exists only in the Windows `User` environment scope.

Fastest way to hydrate the current PowerShell session:

```powershell
$env:STITCH_API_KEY = [Environment]::GetEnvironmentVariable("STITCH_API_KEY", "User")
```

Do this before running any `npm run stitch:*` command.

### 2. `create_design_system` does not accept a plain markdown string

The Stitch tool expects a structured `designSystem` object.
A plain markdown string caused `Request contains an invalid argument`.

Working shape:

```json
{
  "displayName": "JG Tactical UI",
  "theme": {
    "colorMode": "DARK",
    "headlineFont": "SPACE_GROTESK",
    "bodyFont": "IBM_PLEX_SANS",
    "roundness": "ROUND_EIGHT",
    "customColor": "#F59E0B",
    "colorVariant": "EXPRESSIVE",
    "designMd": "..."
  }
}
```

Repo helper note:
- `tools/stitch/stitch-inspect.mjs` already uses this structured payload
- `create-design-system` also calls `update` immediately after create

### 3. `generate_screen_from_text` response shape is not stable enough to trust the SDK projection blindly

During the session, the generated screen did exist, but the SDK helper path
threw `Incomplete API response from generate_screen_from_text`.

The reliable approach in this repo is:
- call the raw tool through `StitchToolClient`
- extract the first generated screen from `outputComponents[*].design.screens[0]`
- keep the returned `projectId` and `screenId` immediately

Repo helper note:
- `tools/stitch/stitch-inspect.mjs` now extracts the generated screen from the raw tool response

### 4. `list-screens` may show zero even when a generated screen can still be fetched directly

Observed behavior:
- `list-screens` returned `0`
- `getScreen(projectId, screenId)` still succeeded
- `fetch-screen --project <id> --screen <id>` also succeeded

Operational rule:
- do not rely on `list-screens` alone right after generation
- treat the returned `screenId` from the generation response as the source of truth
- archive immediately with `fetch-screen`

### 5. Keep probe projects and production candidates separate

During debugging, several short-lived probe projects were created.
Before continuing a later session, identify:
- the intended working project id
- the specific generated screen ids that are worth keeping

For JG, storing the exported files under `artifacts/stitch/<projectId>/<screenId>/`
is the safest way to preserve a usable handoff even if Stitch project listing is inconsistent.

### 6. `screen-intake -> draft manifest` generation is intentionally conservative

Repo helper note:

- `tools/stitch/generate-overlay-draft-manifest.mjs` only supports `overlay-modal`
- it requires a resolved `screen-intake` JSON under `.stitch/contracts/intakes/`
- it writes preview output under `artifacts/stitch/generated-drafts/`
- if `openQuestions` remain, generation fails on purpose instead of inventing CTA or target data
- `--compare` prints a small diff summary against an accepted screen manifest so contract review is faster
