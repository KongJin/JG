import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";

import { lintRepository } from "../lib.mjs";
import { getFixturePath, writeFile } from "./test-helpers.mjs";

test("reports docs/index status label mismatches", async () => {
  const result = await lintRepository(getFixturePath("index-status-mismatch"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "index-status-mismatch"),
  );
});

test("reports docs/index registry and status entry drift", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-index-registry-drift-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

## Doc ID Registry

| doc_id | 파일명 | 상태 |
|---|---|---|
| docs.index | index.md | active |
| ops.demo | owners/operations/demo.md | active |

## Status Label Entries

- \`reference\`: [demo.md](./owners/operations/demo.md) - demo owner
`,
    );
    await writeFile(
      repoRoot,
      "docs/owners/operations/demo.md",
      `# Demo

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.demo
> role: ssot
> owner_scope: fixture owner
> upstream: docs.index
> artifacts: none
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "index-registry-status-entry-mismatch"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports docs/index entries that are missing from the registry", async () => {
  const result = await lintRepository(getFixturePath("index-missing-entry"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "index-missing-entry"));
});

