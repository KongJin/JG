# Unity Evidence Index

> status: current-index
> updated: 2026-05-05

Use `current-index.json` first when looking for current Unity evidence.

Current PNG evidence uses fixed output paths and is overwritten by default where the tool owns the path. Use `tools/workflow/Limit-PngArtifacts.ps1` only for manual capture cleanup.

## Current

- `current/` contains the latest flat Unity evidence files.
- Owner-scoped directories such as `game-flow/`, `uitk-page-routing-refactor/`, `set-b-stepwise/`, and `garage-humanoid-weapon-current/` keep their own evidence grouping.

## Archive

- `archive/flat-legacy-20260505.zip` contains the previous top-level flat evidence files.
- `archive/flat-legacy-20260505.manifest.json` is a compact summary. Use the zip entry list only when a historical artifact is explicitly requested.
- Do not scan or summarize archive contents for current decisions unless a historical artifact is explicitly requested.

New generic Unity evidence should be written under `artifacts/unity/current/` or a narrow owner-scoped subdirectory.
