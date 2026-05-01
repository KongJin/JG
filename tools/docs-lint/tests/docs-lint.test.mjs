import test from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import { execFile } from "node:child_process";
import { fileURLToPath } from "node:url";
import { promisify } from "node:util";

import { lintRepository } from "../lib.mjs";

const fixturesRoot = fileURLToPath(new URL("./fixtures", import.meta.url));
const execFileAsync = promisify(execFile);

function getFixturePath(name) {
  return path.join(fixturesRoot, name);
}

async function writeFile(root, repoRelativePath, content) {
  const absolutePath = path.join(root, repoRelativePath);
  await fs.mkdir(path.dirname(absolutePath), { recursive: true });
  await fs.writeFile(absolutePath, content, "utf8");
}

const recurrenceCoverageText = [
  "active plan artifact owner shape drift",
  "active plan concrete artifact owner collision",
  "progress evidence overload",
].join("; ");

async function writeMinimalRecurrenceRepo(root, {
  rootCause,
  blockedReason = "",
  prevention = `Added owner-doc and lint coverage for ${recurrenceCoverageText}.`,
  verification = "Ran rules:lint in the same change.",
}) {
  await writeFile(
    root,
    "AGENTS.md",
    `# AGENTS

Plan Mode 또는 Codex 운영 작업은 \`docs/index.md\`를 통해 \`rule-operations\` owner 문서로 라우팅한다.
해당 lane에서는 mutation 금지.
`,
  );
  await writeFile(
    root,
    "docs/index.md",
    `# Docs Index

Plan Mode / Codex 운영 규칙 확인: \`rule-operations\`
`,
  );
  await writeFile(
    root,
    "artifacts/rules/issue-recurrence-closeout.json",
    `${JSON.stringify({
      schemaVersion: 1,
      updatedAt: "2026-04-30",
      scope: "rules-only",
      issueDetected: true,
      declaredLane: "rules-only recurrence prevention",
      observedMutationClass: "docs and lint policy",
      acceptanceEvidenceClass: "rules-only closeout",
      escalationRequired: false,
      rootCause,
      prevention,
      verification,
      blockedReason,
      changedPaths: [
        "docs/index.md",
        "artifacts/rules/issue-recurrence-closeout.json",
      ],
    }, null, 2)}\n`,
  );
}

async function writeMinimalActivePlanArtifactRepo(root, artifactMetadata) {
  await writeFile(
    root,
    "AGENTS.md",
    `# AGENTS

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: fixture entry
> upstream: none
> artifacts: none
`,
  );
  await writeFile(
    root,
    "docs/index.md",
    `# Docs Index

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

- \`active\`: [demo.md](./plans/demo.md) - active plan
`,
  );
  await writeFile(
    root,
    "docs/plans/demo.md",
    `# Demo Plan

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: plans.demo
> role: plan
> owner_scope: fixture active plan
> upstream: docs.index
> artifacts: ${artifactMetadata}

Fixture active plan.
`,
  );
}

test("reports missing metadata", async () => {
  const result = await lintRepository(getFixturePath("missing-meta"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "missing-meta"));
});

test("reports missing metadata in managed rule-harness prompts", async () => {
  const result = await lintRepository(getFixturePath("prompt-managed-missing-meta"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some(
      (error) =>
        error.code === "missing-meta" &&
        error.path === "tools/rule-harness/prompts/review.md",
    ),
  );
});

test("reports duplicate doc_id values", async () => {
  const result = await lintRepository(getFixturePath("duplicate-doc-id"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "duplicate-doc-id"));
});

test("reports broken relative markdown links", async () => {
  const result = await lintRepository(getFixturePath("broken-relative-link"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "broken-relative-link"),
  );
});

test("reports broken relative markdown links in external global rule skills", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-global-rule-links-"));
  const globalRuleSkillsRoot = await fs.mkdtemp(path.join(os.tmpdir(), "global-rule-skills-"));
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
`,
    );
    await writeFile(
      globalRuleSkillsRoot,
      "rule-demo/SKILL.md",
      `# Demo

[Broken](missing.md)
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
      globalRuleSkillsRoot,
    });
    assert.ok(
      result.errors.some((error) => error.code === "global-rule-broken-relative-link"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
    await fs.rm(globalRuleSkillsRoot, { recursive: true, force: true });
  }
});

test("accepts valid relative markdown links in external global rule skills", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-global-rule-links-valid-"));
  const globalRuleSkillsRoot = await fs.mkdtemp(path.join(os.tmpdir(), "global-rule-skills-valid-"));
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
`,
    );
    await writeFile(globalRuleSkillsRoot, "rule-demo/SKILL.md", "[Valid](reference.md)\n");
    await writeFile(globalRuleSkillsRoot, "rule-demo/reference.md", "# Reference\n");

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
      globalRuleSkillsRoot,
    });
    assert.equal(result.errors.length, 0);
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
    await fs.rm(globalRuleSkillsRoot, { recursive: true, force: true });
  }
});

test("reports docs/index status label mismatches", async () => {
  const result = await lintRepository(getFixturePath("index-status-mismatch"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "index-status-mismatch"),
  );
});

test("reports docs/index entries that are missing from the registry", async () => {
  const result = await lintRepository(getFixturePath("index-missing-entry"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "index-missing-entry"));
});

test("reports doc_id prefixes that do not match the document path owner", async () => {
  const result = await lintRepository(getFixturePath("doc-id-path-prefix-mismatch"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "doc-id-path-prefix-mismatch"),
  );
});

test("reports completed plan wording left in draft plans", async () => {
  const result = await lintRepository(getFixturePath("completed-draft-plan"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "completed-draft-plan"));
});

test("reports stale Garage owners in active module data structure SSOT", async () => {
  const result = await lintRepository(getFixturePath("stale-module-data-structure"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "stale-module-data-structure"),
  );
});

test("reports missing module data structure guard anchors", async () => {
  const result = await lintRepository(getFixturePath("stale-module-data-structure-missing-anchors"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "stale-module-data-structure"),
  );
});

test("reports reference closeout wording left in active plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-reference-closeout"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "active-plan-reference-closeout"),
  );
});

test("reports bare plan rereview clean without checked scope", async () => {
  const result = await lintRepository(getFixturePath("bare-plan-rereview-clean"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "bare-plan-rereview-clean"),
  );
});

test("reports Unity meta artifacts under docs", async () => {
  const result = await lintRepository(getFixturePath("docs-meta-artifact"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "docs-meta-artifact"));
});

test("reports too many active non-progress plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-budget"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "active-plan-budget"));
});

test("accepts active non-progress plan count at budget", async () => {
  const result = await lintRepository(getFixturePath("active-plan-budget-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports mutual references between active non-progress plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-mutual-reference"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "active-plan-mutual-reference"),
  );
});

test("accepts one-way references between active non-progress plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-one-way-reference-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports active plan concrete artifact owner collisions", async () => {
  const result = await lintRepository(getFixturePath("active-plan-artifact-owner-collision"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "active-plan-artifact-owner-collision"),
  );
});

test("accepts distinct evidence directories across active plans", async () => {
  const result = await lintRepository(getFixturePath("active-plan-artifact-directory-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports broad active plan artifact owner shapes", async () => {
  const rejectedArtifactShapes = [
    "`docs/plans/*.md`",
    "`Assets/**`",
    "`Build/**`",
    "`Assets/UI/UIToolkit/AccountSync/`",
    "`artifacts/unity/`",
    "`artifacts/unity/*uitk*`",
  ];

  for (const artifactMetadata of rejectedArtifactShapes) {
    const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-artifact-shape-"));
    try {
      await writeMinimalActivePlanArtifactRepo(repoRoot, artifactMetadata);
      const result = await lintRepository(repoRoot, {
        includeGeneralChecks: true,
        includePolicyChecks: false,
      });
      assert.ok(
        result.errors.some((error) => error.code === "active-plan-artifact-shape"),
        `Expected active-plan-artifact-shape for: ${artifactMetadata}`,
      );
    } finally {
      await fs.rm(repoRoot, { recursive: true, force: true });
    }
  }
});

test("accepts active plan evidence artifact shapes", async () => {
  const acceptedArtifactShapes = [
    "`artifacts/unity/game-flow/actual-flow/`",
    "`artifacts/nova1492/`",
    "`artifacts/unity/game-flow/*.png`",
    "`artifacts/unity/account-sync-uitk-preview-gameview.png`",
    "none",
  ];

  for (const artifactMetadata of acceptedArtifactShapes) {
    const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-artifact-shape-valid-"));
    try {
      await writeMinimalActivePlanArtifactRepo(repoRoot, artifactMetadata);
      const result = await lintRepository(repoRoot, {
        includeGeneralChecks: true,
        includePolicyChecks: false,
      });
      assert.equal(
        result.errors.length,
        0,
        `Expected no lint errors for: ${artifactMetadata}; got ${result.errors.map((error) => error.code).join(", ")}`,
      );
    } finally {
      await fs.rm(repoRoot, { recursive: true, force: true });
    }
  }
});

test("reports progress evidence artifact overload", async () => {
  const result = await lintRepository(getFixturePath("progress-evidence-overload"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "progress-evidence-overload"),
  );
});

test("accepts progress evidence artifact count at budget", async () => {
  const result = await lintRepository(getFixturePath("progress-evidence-overload-valid"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("ignores excluded .system skill files", async () => {
  const result = await lintRepository(getFixturePath("excluded-system-skill-ignored"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports stale inline repo paths in skill markdown", async () => {
  const result = await lintRepository(getFixturePath("stale-inline-path-in-skill"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "missing-inline-path"));
});

test("reports legacy agent paths in repo-local skills", async () => {
  const result = await lintRepository(getFixturePath("legacy-agent-token-in-skill"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "legacy-owner-path"));
});

test("reports deprecated historical stitch paths in repo-local skills", async () => {
  const result = await lintRepository(getFixturePath("deprecated-stitch-path-in-skill"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "deprecated-skill-inline-path"),
  );
});

test("reports deprecated historical stitch path mentions in repo-local skills", async () => {
  const result = await lintRepository(getFixturePath("deprecated-stitch-mention-in-skill"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "deprecated-skill-path-mention"),
  );
});

test("reports historical stitch links inside active documents", async () => {
  const result = await lintRepository(getFixturePath("historical-link-in-active-doc"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "historical-link-in-active-doc"),
  );
});

test("reports missing concrete contract artifacts", async () => {
  const result = await lintRepository(getFixturePath("missing-contract-artifact"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-contract-artifact"),
  );
});

test("reports deleted skill references still mentioned from SKILL.md", async () => {
  const result = await lintRepository(getFixturePath("deleted-reference-mentioned"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(result.errors.some((error) => error.code === "missing-inline-path"));
});

test("reports inline owner doc_id references that do not resolve", async () => {
  const result = await lintRepository(getFixturePath("missing-doc-id-reference"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-doc-id-reference"),
  );
});

test("reports metadata upstream owner doc_id references that do not resolve", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-upstream-doc-id-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-04-30
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

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: repo.agents
> artifacts: none

- \`active\`: [Demo](./ops/demo.md) - demo owner
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/demo.md",
      `# Demo

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: ops.demo
> role: ssot
> owner_scope: fixture owner
> upstream: docs.index, ops.missing-owner
> artifacts: none
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-upstream-doc-id-reference"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports path-style metadata upstream owner references", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-upstream-path-"));
  try {
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS

> 마지막 업데이트: 2026-04-30
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

> 마지막 업데이트: 2026-04-30
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: fixture index
> upstream: \`AGENTS.md\`
> artifacts: none
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-upstream-doc-id-reference"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports global rule skill references missing from the skill routing registry", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-global-skill-registry-"));
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

### \`ops/\`

- \`active\`: [skill_routing_registry.md](./ops/skill_routing_registry.md) - skill registry
- \`active\`: [demo.md](./ops/demo.md) - demo owner
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/skill_routing_registry.md",
      `# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: fixture skill route registry
> upstream: docs.index
> artifacts: none

| Skill | Scope |
|---|---|
| \`rule-operations\` | operations |
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/demo.md",
      `# Demo

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.demo
> role: ssot
> owner_scope: fixture owner
> upstream: docs.index
> artifacts: none

Use \`rule-validation\` for a fixture route.
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-global-skill-registry-entry"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("accepts global rule skill references listed in the skill routing registry", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-global-skill-registry-valid-"));
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

### \`ops/\`

- \`active\`: [skill_routing_registry.md](./ops/skill_routing_registry.md) - skill registry
- \`active\`: [demo.md](./ops/demo.md) - demo owner
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/skill_routing_registry.md",
      `# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: fixture skill route registry
> upstream: docs.index
> artifacts: none

| Skill | Scope |
|---|---|
| \`rule-operations\` | operations |
| \`rule-unity\` | unity |
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/demo.md",
      `# Demo

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.demo
> role: ssot
> owner_scope: fixture owner
> upstream: docs.index
> artifacts: none

Use \`rule-unity\` for a fixture route.
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.equal(result.errors.length, 0);
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports skill trigger matrix routes that do not match managed skills", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-skill-trigger-unknown-"));
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

### \`ops/\`

- \`active\`: [skill_routing_registry.md](./ops/skill_routing_registry.md) - skill registry
- \`active\`: [skill_trigger_matrix.md](./ops/skill_trigger_matrix.md) - skill trigger matrix
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/skill_routing_registry.md",
      `# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: fixture skill route registry
> upstream: docs.index
> artifacts: none

| Skill | Scope |
|---|---|
| \`rule-operations\` | operations |
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/skill_trigger_matrix.md",
      `# Skill Trigger Matrix

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-trigger-matrix
> role: reference
> owner_scope: fixture skill trigger matrix
> upstream: docs.index, ops.skill-routing-registry
> artifacts: none

| ID | Prompt signal | Expected skills |
|---|---|---|
| T01 | fixture | \`jg-missing\`, \`rule-operations\` |
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "unknown-skill-trigger-route"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports repo-local skills missing from the skill trigger matrix", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-skill-trigger-missing-"));
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

### \`ops/\`

- \`active\`: [skill_routing_registry.md](./ops/skill_routing_registry.md) - skill registry
- \`active\`: [skill_trigger_matrix.md](./ops/skill_trigger_matrix.md) - skill trigger matrix
`,
    );
    await writeFile(
      repoRoot,
      ".codex/skills/jg-demo/SKILL.md",
      `---
name: jg-demo
description: Fixture demo skill.
---

# Demo Skill

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: skill.jg-demo
> role: skill-entry
> owner_scope: fixture skill
> upstream: docs.index
> artifacts: none
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/skill_routing_registry.md",
      `# Skill Routing Registry

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-routing-registry
> role: reference
> owner_scope: fixture skill route registry
> upstream: docs.index
> artifacts: none

| Skill | Scope |
|---|---|
| \`rule-operations\` | operations |
`,
    );
    await writeFile(
      repoRoot,
      "docs/ops/skill_trigger_matrix.md",
      `# Skill Trigger Matrix

> 마지막 업데이트: 2026-05-01
> 상태: active
> doc_id: ops.skill-trigger-matrix
> role: reference
> owner_scope: fixture skill trigger matrix
> upstream: docs.index, ops.skill-routing-registry
> artifacts: none

| ID | Prompt signal | Expected skills |
|---|---|---|
| T01 | fixture | \`rule-operations\` |
`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: true,
      includePolicyChecks: false,
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-skill-trigger-fixture"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("ignores valid search-pattern inline code", async () => {
  const result = await lintRepository(getFixturePath("valid-search-pattern-ignored"), {
    includeGeneralChecks: true,
    includePolicyChecks: false,
  });
  assert.equal(result.errors.length, 0);
});

test("reports missing Plan Mode routing in AGENTS.md", async () => {
  const result = await lintRepository(getFixturePath("missing-plan-routing-agents"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-plan-mode-routing"),
  );
});

test("reports missing Plan Mode owner route in docs/index.md", async () => {
  const result = await lintRepository(getFixturePath("missing-plan-routing-index"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-plan-mode-owner-route"),
  );
});

test("reports missing inspection-only clause in mutating repo-local skill", async () => {
  const result = await lintRepository(getFixturePath("missing-skill-inspection-clause"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-skill-inspection-clause"),
  );
});

test("reports missing cohesion/coupling owner route in repo-local skill", async () => {
  const result = await lintRepository(getFixturePath("missing-skill-owner-route"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-skill-owner-route"),
  );
});

test("reports missing root cause investigation contract in acceptance guardrails", async () => {
  const result = await lintRepository(getFixturePath("missing-root-cause-investigation-contract"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-root-cause-investigation-contract"),
  );
});

test("reports missing owner route in issue investigation skill", async () => {
  const result = await lintRepository(getFixturePath("missing-issue-investigation-owner-route"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-issue-investigation-owner-route"),
  );
});

test("reports missing recurrence closeout artifact for rules-only changes", async () => {
  const result = await lintRepository(getFixturePath("missing-recurrence-closeout-artifact"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: ["docs/index.md"],
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-recurrence-closeout-artifact"),
  );
});

test("reports recurrence closeout artifact that was not updated in the same change", async () => {
  const result = await lintRepository(getFixturePath("valid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: ["docs/index.md"],
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-recurrence-closeout-update"),
  );
});

test("does not require recurrence closeout for ordinary plan document changes", async () => {
  const result = await lintRepository(getFixturePath("valid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: ["docs/plans/progress.md"],
  });
  assert.equal(result.errors.length, 0);
});

test("does not require recurrence closeout for feature tool implementation changes", async () => {
  const result = await lintRepository(getFixturePath("valid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "tools/audio-mcp/server.mjs",
      "tools/audio-mcp/README.md",
      "package-lock.json",
    ],
  });
  assert.equal(result.errors.length, 0);
});

test("auto-detects dirty recurrence closeout changes without changedFiles or env", async () => {
  const previousChangedFilesEnv = process.env.RULES_LINT_CHANGED_FILES;
  delete process.env.RULES_LINT_CHANGED_FILES;

  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-dirty-"));
  try {
    await execFileAsync("git", ["init"], { cwd: repoRoot });
    await writeFile(
      repoRoot,
      "AGENTS.md",
      `# AGENTS.md

> 마지막 업데이트: 2026-04-28
> 상태: active
> doc_id: repo.agents
> role: entry
> owner_scope: test entry
> upstream: none
> artifacts: none

Plan Mode / Codex 운영 규칙은 docs/index.md에서 current path 확인 후 rule-operations owner 문서로 라우팅하고, 그 lane에서는 mutation 금지.
`,
    );
    await writeFile(
      repoRoot,
      "docs/index.md",
      `# Docs Index

> 마지막 업데이트: 2026-04-28
> 상태: active
> doc_id: docs.index
> role: entry
> owner_scope: test index
> upstream: repo.agents
> artifacts: none

Plan Mode / Codex 운영 규칙 확인: rule-operations owner 문서.
`,
    );
    await writeFile(
      repoRoot,
      "artifacts/rules/issue-recurrence-closeout.json",
      `${JSON.stringify({
        schemaVersion: 1,
        updatedAt: "2026-04-28",
        scope: "rules-only",
        issueDetected: false,
        declaredLane: "test",
        observedMutationClass: "docs",
        acceptanceEvidenceClass: "rules lint",
        escalationRequired: false,
        rootCause: "",
        prevention: "",
        verification: "rules lint",
        blockedReason: "",
        changedPaths: ["artifacts/rules/issue-recurrence-closeout.json"],
      }, null, 2)}\n`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
    });
    assert.ok(
      result.errors.some((error) => error.code === "recurrence-closeout-missing-changed-path"),
    );
  } finally {
    if (previousChangedFilesEnv === undefined) {
      delete process.env.RULES_LINT_CHANGED_FILES;
    } else {
      process.env.RULES_LINT_CHANGED_FILES = previousChangedFilesEnv;
    }
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports missing required recurrence closeout fields when issueDetected is true", async () => {
  const result = await lintRepository(getFixturePath("invalid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "docs/index.md",
      "artifacts/rules/issue-recurrence-closeout.json",
    ],
  });
  assert.ok(
    result.errors.some((error) => error.code === "missing-recurrence-closeout-field"),
  );
});

test("reports uncertain rootCause without blockedReason", async () => {
  const result = await lintRepository(getFixturePath("uncertain-root-cause-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "docs/index.md",
      "artifacts/rules/issue-recurrence-closeout.json",
    ],
  });
  assert.ok(
    result.errors.some((error) => error.code === "uncertain-root-cause-without-blocked-reason"),
  );
});

test("reports recurrence closeout missing hard-fail coverage wording", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-closeout-coverage-"));
  try {
    await writeMinimalRecurrenceRepo(repoRoot, {
      rootCause: "A rules-only change introduced a documentation issue.",
      prevention: "Added owner-doc and lint coverage.",
    });
    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
      changedFiles: [
        "docs/index.md",
        "artifacts/rules/issue-recurrence-closeout.json",
      ],
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-recurrence-closeout-coverage"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports recurrence closeout verification without concrete evidence anchor", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-closeout-verification-"));
  try {
    await writeMinimalRecurrenceRepo(repoRoot, {
      rootCause: "A rules-only change introduced a documentation issue.",
      verification: "Checked the change.",
    });
    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
      changedFiles: [
        "docs/index.md",
        "artifacts/rules/issue-recurrence-closeout.json",
      ],
    });
    assert.ok(
      result.errors.some((error) => error.code === "missing-recurrence-closeout-verification-evidence"),
    );
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});

test("reports expanded uncertain rootCause expressions without blockedReason", async () => {
  const expressions = [
    "maybe the changed-path sync missed a case",
    "the route appears to skip the owner doc",
    "원인이 아직 가능성 수준이다",
    "로그상 누락으로 보임",
    "로그상 누락으로 보인다",
    "로그상 누락으로 보여",
    "아직 검증 전인 듯하다",
    "검증이 덜 된 것 같다",
  ];

  for (const rootCause of expressions) {
    const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-uncertain-"));
    try {
      await writeMinimalRecurrenceRepo(repoRoot, { rootCause });
      const result = await lintRepository(repoRoot, {
        includeGeneralChecks: false,
        includePolicyChecks: true,
        changedFiles: [
          "docs/index.md",
          "artifacts/rules/issue-recurrence-closeout.json",
        ],
      });
      assert.ok(
        result.errors.some((error) => error.code === "uncertain-root-cause-without-blocked-reason"),
        `Expected uncertain rootCause error for: ${rootCause}`,
      );
    } finally {
      await fs.rm(repoRoot, { recursive: true, force: true });
    }
  }
});

test("accepts uncertain rootCause when blockedReason explains missing verification", async () => {
  const result = await lintRepository(getFixturePath("uncertain-root-cause-blocked-valid"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "docs/index.md",
      "artifacts/rules/issue-recurrence-closeout.json",
    ],
  });
  assert.equal(result.errors.length, 0);
});

test("accepts valid recurrence closeout artifact for rules-only changes", async () => {
  const result = await lintRepository(getFixturePath("valid-recurrence-closeout"), {
    includeGeneralChecks: false,
    includePolicyChecks: true,
    changedFiles: [
      "docs/index.md",
      "artifacts/rules/issue-recurrence-closeout.json",
    ],
  });
  assert.equal(result.errors.length, 0);
});

test("accepts sharded recurrence closeout artifact for rules-only changes", async () => {
  const repoRoot = await fs.mkdtemp(path.join(os.tmpdir(), "docs-lint-sharded-closeout-"));
  try {
    await writeMinimalRecurrenceRepo(repoRoot, {
      rootCause: `A rules-only change introduced coverage for ${recurrenceCoverageText}.`,
      prevention: `Added owner-doc and lint coverage for ${recurrenceCoverageText}.`,
      verification: "node --test tools/docs-lint/tests/docs-lint.test.mjs",
    });
    const shardPath = "artifacts/rules/issue-recurrence-closeout.d/local-fixture.json";
    await writeFile(
      repoRoot,
      shardPath,
      `${JSON.stringify({
        schemaVersion: 1,
        updatedAt: "2026-05-01",
        scope: "rules-only",
        issueDetected: true,
        declaredLane: "rules-only recurrence prevention",
        observedMutationClass: `sharded closeout coverage for ${recurrenceCoverageText}`,
        acceptanceEvidenceClass: "docs-lint fixture tests",
        escalationRequired: false,
        rootCause: `A rules-only change introduced coverage for ${recurrenceCoverageText}.`,
        prevention: `Added sharded closeout coverage for ${recurrenceCoverageText}.`,
        verification: "node --test tools/docs-lint/tests/docs-lint.test.mjs",
        blockedReason: "",
        changedPaths: [
          shardPath,
          "docs/index.md",
        ],
      }, null, 2)}\n`,
    );

    const result = await lintRepository(repoRoot, {
      includeGeneralChecks: false,
      includePolicyChecks: true,
      changedFiles: [
        "docs/index.md",
        shardPath,
      ],
    });
    assert.equal(result.errors.length, 0);
  } finally {
    await fs.rm(repoRoot, { recursive: true, force: true });
  }
});
