import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";

import { lintRepository } from "../lib.mjs";
import { writeFile } from "./test-helpers.mjs";

test("checks repo skill import manifest as a managed document", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-skill-import-manifest-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-05-05
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

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      ".codex/skills/IMPORT_MANIFEST.md",
      `# Skill Import Manifest

> 마지막 업데이트: 2026-05-05
> 상태: active
> doc_id: skill.import-manifest
> role: reference
> owner_scope: fixture import manifest
> upstream: docs.index
> artifacts: none

[Skill](docx/SKILL.md)
`,
    );
    await writeFile(
      repoRoot,
      ".codex/skills/docx/SKILL.md",
      `---
name: docx
description: Test imported skill.
---

# Imported Skill
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.equal(result.errors.length, 0);
    assert.ok(result.managedDocPaths.includes(".codex/skills/IMPORT_MANIFEST.md"));
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

